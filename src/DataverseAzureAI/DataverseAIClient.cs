#region using
using AP2.DataverseAzureAI.Extensions;
using AP2.DataverseAzureAI.Metadata;
using AP2.DataverseAzureAI.Model;
using AP2.DataverseAzureAI.OData;
using AP2.DataverseAzureAI.Settings;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Text.Json;
#endregion

namespace AP2.DataverseAzureAI;

/// <summary>
/// https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling
/// </summary>
public partial class DataverseAIClient : IDisposable
{
    #region Constants

    public const string LocalAppDataFolderName = "PowerPlatformAI";
    public const string MainSystemPrompt = "You are an assistant, helping interact with Microsoft Power Platform.";

    const string TableNotFound = "Table not found";
    const string PropertyNotFound = "Property not found";
    const string AttributeNotFound = "Attribute not found";
    const string First = "First";
    const string Last = "Last";
    const string UserDeclinedAction = "User declined to perform the action";
    const string FunctionCompletedSuccessfully = "Function completed successfully";

    #endregion

    #region Fields

    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };
    private Lazy<OpenAIClient> _openAIClient;
    private Lazy<GraphServiceClient> _graphClient;
    TimeProvider _timeProvider;
    private LiteDB.LiteDatabase _liteDatabase;
    Task<User>? _user;
    Task<Organization>? _organization;
    Task<IList<Metadata.Environment>> _environments;
    Metadata.Environment? _selectedEnvironment;
    Settings.UserSettings _userSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    private IList<EntityMetadataModel>? _entityMetadataModels;
    private string? PowerPlatformTenantApiPrefix;
    private string? PowerPlatformEnvironmentApiPrefix;
    private bool disposedValue;
    private readonly IOptions<AzureAISettings> _azureAISettings;
    private readonly AIFunctionCollection _aIFunctionsCollection = new(typeof(DataverseAIClient));
    private readonly ChatCompletionsOptions _chatOptions = new ()
    {
        Temperature = 1,
        MaxTokens = 500,
        NucleusSamplingFactor = (float)0.95,
        FrequencyPenalty = 0,
        PresencePenalty = 0,
    };

    #endregion

    #region Constructors & Initialization

    public DataverseAIClient(
        IHttpClientFactory httpClientFactory, 
        IOptions<AzureAISettings> azureAISettings, 
        IAuthenticationProvider authenticationProvider, TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _userSettings = Settings.UserSettings.Load(AppDataFolderName);
        _liteDatabase = new LiteDB.LiteDatabase(Path.Combine(AppDataFolderName, $"{nameof(DataverseAIClient)}.db"));
        var lastWelcomeMessage = _liteDatabase.GetCollection<WelcomeMessage>().FindOne(LiteDB.Query.All(LiteDB.Query.Descending));
        if (lastWelcomeMessage != null)
            WelcomeMessage = lastWelcomeMessage;

        _azureAISettings = azureAISettings;
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));

        _openAIClient = new Lazy<OpenAIClient>(() =>
        {
            AzureKeyCredential azureKeyCredential;
            if (!string.IsNullOrWhiteSpace(OpenApiKey))
                azureKeyCredential = new AzureKeyCredential(OpenApiKey);
            else if (!string.IsNullOrWhiteSpace(azureAISettings.Value.OpenApiKey))
                azureKeyCredential = new AzureKeyCredential(azureAISettings.Value.OpenApiKey);
            else
                azureKeyCredential = new AzureKeyCredential(string.Empty);

            return new OpenAIClient(
                OpenApiEndPoint == null ? azureAISettings.Value.OpenApiEndPoint : OpenApiEndPoint,
                azureKeyCredential
            );
        });

        _graphClient = new Lazy<GraphServiceClient>(() =>
        {
            var graphClient = new GraphServiceClient(authenticationProvider);
            return graphClient;
        });

        foreach (var functionDefinition in _aIFunctionsCollection.Definitions)
            _chatOptions.Functions.Add(functionDefinition);
        _chatOptions.FunctionCall = FunctionDefinition.Auto;
    }

    public async Task LoadMetadata()
    {
        if (EnvironmentId == Guid.Empty)
            throw new InvalidOperationException($"{nameof(EnvironmentId)} is not set.");

        // Get environment details from Global Discovery Service
        using var gdsRequest = new HttpRequestMessage(HttpMethod.Get, $"https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances?$filter=EnvironmentId eq '{EnvironmentId}'");
        var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
        var gdsResponse = await httpClient.SendAsync(gdsRequest).ConfigureAwait(false);
        gdsResponse.EnsureSuccessStatusCode();
        var parsedToken = new JwtSecurityToken(jwtEncodedString: gdsRequest.Headers.Authorization?.Parameter);
        FullName = parsedToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        UserObjectId = parsedToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        var gdsContentStream = await gdsResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var environments = JsonSerializer.Deserialize<ODataContext<EnvironmentInstance>>(gdsContentStream, JsonSerializerOptions);
        if (environments == null || environments.Values.Count <= 0)
            throw new InvalidOperationException($"EnvironmentId '{EnvironmentId}' was not found.");
        EnvironmentInstance = environments.Values.First();
        PowerPlatformEnvironmentApiPrefix = EnvironmentInstance.EnvironmentId.Replace("-", "");
        PowerPlatformEnvironmentApiPrefix = PowerPlatformEnvironmentApiPrefix.Insert(PowerPlatformEnvironmentApiPrefix.Length - 2, ".");

        // Get Dataverse Environment Metadata
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"EntityDefinitions"));
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var metadata = JsonSerializer.Deserialize<ODataContext<EntityMetadataModel>>(contentStream, JsonSerializerOptions);
        if (metadata == null || metadata.Values.Count <= 0)
            throw new InvalidOperationException("No metadata was returned.");
        _entityMetadataModels = metadata.Values;

        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, MainSystemPrompt));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"You are assisting **{FullName}**"));

        var listOfProperties = string.Join(", ", EntityMetadataModel.Properties.Values.ToBrowsableProperties());
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each Dataverse table or entity has following properties: {listOfProperties}"));

        listOfProperties = string.Join(", ", Solution.Properties.Values.ToBrowsableProperties());
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each Dataverse solution has following properties: {listOfProperties}"));

        listOfProperties = string.Join(", ", CanvasAppProperties.Properties.Values.ToBrowsableProperties());
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each canvas app has following properties: {listOfProperties}"));

        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Call a function if you need to get updated information"));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each function can be called multiple times"));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"You can ask clarifying questions if function needs required parameter"));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Do not try to predict required parameter"));
    }

    #endregion

    #region Run asyncronous tasks

    /// <summary>
    /// Starts asyncronous tasks which collect and cache data from Power Platform
    /// </summary>
    public void Run()
    {
        // First start asyncronous tasks
        _organization = Task.Run(GetOrganization);
        _user = Task.Run(GetMe);
        _environments = Task.Run(GetEnvironments);
        Task.Run(() => WelcomeMessage.NextWelcomeMessage(_liteDatabase, _openAIClient.Value, OpenApiModelInternal));

        // Initial prompt
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, MainSystemPrompt));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"You are assisting **{_userSettings.DisplayName}**"));

        var listOfProperties = string.Join(", ", EntityMetadataModel.Properties.Values.ToBrowsableProperties());
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each Dataverse table or entity has following properties: {listOfProperties}"));

        listOfProperties = string.Join(", ", Solution.Properties.Values.ToBrowsableProperties());
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each Dataverse solution has following properties: {listOfProperties}"));

        listOfProperties = string.Join(", ", CanvasAppProperties.Properties.Values.ToBrowsableProperties());
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each canvas app has following properties: {listOfProperties}"));

        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Call a function if you need to get updated information"));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Each function can be called multiple times"));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"You can ask clarifying questions if function needs required parameter"));
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.System, $"Do not try to predict required parameter"));
    }

    private async Task<IList<Metadata.Environment>> GetEnvironments()
    {
        if (_organization == null)
            throw new InvalidOperationException("Organization is not requested.");
        if (_user == null)
            throw new InvalidOperationException("User is not requested.");

        // Make sure organization and user is loaded
        _organization.Wait();
        _user.Wait();

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildTenantApiQueryUri($"powerapps/environments?&expand=properties.permissions&api-version=1&$filter=minimumAppPermission eq 'CanEdit' and properties.environmentSku ne 'Teams'"));
        var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var environments = JsonSerializer.Deserialize<ODataContext<Metadata.Environment>>(contentStream, JsonSerializerOptions);
        if (environments == null)
            throw new InvalidOperationException("Failed to get list of environments.");

        // Inject authenticated HttpClient
        foreach (var environment in environments.Values)
        {
            environment.HttpClientFactory = _httpClientFactory;
            environment.User = _user.Result;
            if (_userSettings.LastUsedEnvironmentId == environment.Name)
                SelectedEnvironment = environment;
        }

        return environments.Values;
    }

    private async Task<User> GetMe()
    {
        var user = await _graphClient.Value.Me.GetAsync();
        if (user == null)
            throw new InvalidOperationException("Failed to get user details.");

        _userSettings.GivenName = user.GivenName;
        _userSettings.DisplayName = user.DisplayName;
        _userSettings.EntraId = user.Id;
        _userSettings.UserPrincipalName = user.UserPrincipalName;
        _userSettings.Save();

        return user;
    }

    private async Task<Organization> GetOrganization()
    {
        var organizationResponse = await _graphClient.Value.Organization.GetAsync((c) =>
        {
            c.QueryParameters.Select = new string[] { "id", "city", "country", "countryLetterCode", "displayName", "state", "street" };
        });
        if (organizationResponse == null || organizationResponse.Value == null || organizationResponse.Value.Count <= 0)
            throw new InvalidOperationException("Failed to get organization details.");

        var organization = organizationResponse.Value.First();

        PowerPlatformTenantApiPrefix = organization.Id!.Replace("-", "");
        PowerPlatformTenantApiPrefix = PowerPlatformTenantApiPrefix.Insert(PowerPlatformTenantApiPrefix.Length - 2, ".");

        return organization;
    }

    #endregion

    #region Properties

    public Uri? OpenApiEndPoint { get; set; }

    public string? OpenApiKey { get; set; }

    public string? OpenApiModel { get; set; }

    public string OpenApiModelInternal
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(OpenApiModel))
                return OpenApiModel;

            if (_azureAISettings != null && !string.IsNullOrWhiteSpace(_azureAISettings.Value.OpenApiModel))
                return _azureAISettings.Value.OpenApiModel;

            throw new InvalidOperationException("OpenApiModel is not set");
        }
    }

    public Lazy<OpenAIClient> OpenAIClient
    {
        get => _openAIClient;
        set => _openAIClient = value ?? throw new ArgumentNullException(nameof(OpenAIClient));
    }

    public Guid EnvironmentId { get; set; }

    public EnvironmentInstance EnvironmentInstance { get; private set; }

    public string? GivenName 
    { 
        get => _userSettings.GivenName;
    } 

    public string? FullName { get; private set; }

    public string? UserObjectId { get; private set; }

    public WelcomeMessage WelcomeMessage { get; private set; } = WelcomeMessage.Default;

    public string AppDataFolderName { get; private set; } = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), LocalAppDataFolderName);

    public Metadata.Environment? SelectedEnvironment
    {
        get => _selectedEnvironment;
        set
        {
            if (value == null)
            {
                if (_userSettings.LastUsedEnvironmentId != null)
                {
                    _userSettings.LastUsedEnvironmentId = null;
                    _userSettings.Save();
                }
            }
            else
            {
                if (_userSettings.LastUsedEnvironmentId != value.Name)
                {
                    _userSettings.LastUsedEnvironmentId = value.Name;
                    _userSettings.Save();
                }
            }
            _selectedEnvironment = value;
        }
    }

    #endregion

    #region Chat

    public async Task<string> GetChatCompletionAsync(string prompt)
    {
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.User, prompt));
        var response = await _openAIClient.Value.GetChatCompletionsAsync(OpenApiModelInternal, _chatOptions).ConfigureAwait(false);
        if (response == null || response.Value == null || response.Value.Choices == null || response.Value.Choices.Count <= 0)
            throw new ApplicationException("Unable to get Azure Open AI response");

        var responseMessage = response.Value.Choices[0].Message;
        while (responseMessage.FunctionCall != null && responseMessage.Role == ChatRole.Assistant)
        {
            if (_aIFunctionsCollection.Functions.TryGetValue(responseMessage.FunctionCall.Name, out var aiFunction))
            {
                var functionResponse = await aiFunction.Invoke(this, responseMessage.FunctionCall.Arguments).ConfigureAwait(false);
                _chatOptions.Messages.Add(
                    new Azure.AI.OpenAI.ChatMessage(ChatRole.Function, functionResponse)
                    {
                        Name = responseMessage.FunctionCall.Name
                    }
                );
            }
            else
            {
                _chatOptions.Messages.Add(
                    new Azure.AI.OpenAI.ChatMessage(ChatRole.Function, $"Function '{responseMessage.FunctionCall.Name}' doesn't exist")
                    {
                        Name = responseMessage.FunctionCall.Name
                    }
                );
            }

            response = await _openAIClient.Value.GetChatCompletionsAsync(OpenApiModelInternal, _chatOptions).ConfigureAwait(false);
            if (response == null || response.Value == null || response.Value.Choices == null || response.Value.Choices.Count <= 0)
                throw new ApplicationException("Unable to get Azure Open AI response");
            responseMessage = response.Value.Choices[0].Message;
        }
        _chatOptions.Messages.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.Assistant, response.Value.Choices[0].Message.Content));

        return response.Value.Choices[0].Message.Content;
    }

    #endregion

    #region Helpers

    private EntityMetadataModel? GetEntityMetadataModel(string tableName)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            return null;

        if (_entityMetadataModels == null)
            throw new InvalidOperationException("Metadata has not been loaded.");

        // 1. Exact match on logical name
        var entityMetadataModel = _entityMetadataModels!.FirstOrDefault(
            em => string.Compare(em.LogicalName, tableName, StringComparison.OrdinalIgnoreCase) == 0);
        if (entityMetadataModel != null)
            return entityMetadataModel;

        // 2. Exact match on logical collection name
        entityMetadataModel = _entityMetadataModels!.FirstOrDefault(
            em => string.Compare(em.LogicalCollectionName, tableName, StringComparison.OrdinalIgnoreCase) == 0);
        if (entityMetadataModel != null)
            return entityMetadataModel;

        // 3. Exact matching without publisher prefix
        var tableNameWithoutPublisher = tableName.TrimPublisher();
        entityMetadataModel = _entityMetadataModels!.FirstOrDefault(em =>
            {
                if (string.Compare(em.LogicalName.TrimPublisher(), tableNameWithoutPublisher, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;

                if (em.LogicalCollectionName == null)
                    return false;

                return string.Compare(em.LogicalCollectionName.TrimPublisher(), tableNameWithoutPublisher, StringComparison.OrdinalIgnoreCase) == 0;
            });
        if (entityMetadataModel != null)
            return entityMetadataModel;

        // 4. Exact matching on display name
        entityMetadataModel = _entityMetadataModels!.FirstOrDefault(em =>
            {
                return em.DisplayName.LocalizedLabels.Any
                (
                    ll => string.Compare(ll.Label, tableNameWithoutPublisher, StringComparison.OrdinalIgnoreCase) == 0
                );
            });
        if (entityMetadataModel != null)
            return entityMetadataModel;

        // 5. Exact matching on display collection name
        entityMetadataModel = _entityMetadataModels!.FirstOrDefault(em =>
            {
                return em.DisplayCollectionName.LocalizedLabels.Any
                (
                    ll => string.Compare(ll.Label, tableNameWithoutPublisher, StringComparison.OrdinalIgnoreCase) == 0
                );
            });
        if (entityMetadataModel != null)
            return entityMetadataModel;

        return null;
    }

    [DebuggerStepThrough]
    private Uri BuildOrgQueryUri(string query)
    {
        _ = SelectedEnvironment ?? throw new InvalidOperationException("No environment selected");

        return new Uri(SelectedEnvironment.Properties.LinkedEnvironmentMetadata.InstanceApiUrl, $"api/data/v9.2/{query}");
    }

    [DebuggerStepThrough]
    private Uri BuildTenantApiQueryUri(string query)
    {
        var baseUri = new Uri($"https://{PowerPlatformTenantApiPrefix}.tenant.api.powerplatform.com");
        return new Uri(baseUri, query);
    }

    [DebuggerStepThrough]
    private Uri BuildEnvironmentApiQueryUri(string query)
    {
        var baseUri = new Uri($"https://{PowerPlatformEnvironmentApiPrefix}.environment.api.powerplatform.com");
        return new Uri(baseUri, query);
    }

    public async Task<Dictionary<SolutionComponentType, Dictionary<Guid, SolutionComponent>>> LoadSolutionComponents(Guid solutionId)
    {
        var solutionComponents = new Dictionary<SolutionComponentType, Dictionary<Guid, SolutionComponent>>();
        var query = $"solutioncomponents?$filter=_solutionid_value eq '{solutionId}'";
        var uri = BuildOrgQueryUri(query);
        var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
        var response = await httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var solutionComponentsData = JsonSerializer.Deserialize<ODataContext<SolutionComponent>>(contentStream, JsonSerializerOptions);
        if (solutionComponentsData == null)
            throw new InvalidOperationException("Failed to get list of solution components.");

        foreach (var solutionComponent in solutionComponentsData.Values)
        {
            if (solutionComponent.ComponentType == null)
                continue;

            if (!solutionComponents.TryGetValue(solutionComponent.ComponentType.Value, out var solutionComponentType))
            {
                solutionComponentType = new Dictionary<Guid, SolutionComponent>();
                solutionComponents.Add(solutionComponent.ComponentType.Value, solutionComponentType);
            }
            solutionComponentType.Add(solutionComponent.ObjectId, solutionComponent);
        }

        return solutionComponents;
    }

    public async Task<List<SystemUser>> LoadRoleUsers(Guid roleId)
    {
        var query = $"roles({roleId})?$expand=businessunitid($select=name),systemuserroles_association($select=fullname,domainname,systemuserid),teamroles_association($select=teamid,name,teamtype)";
        var uri = BuildOrgQueryUri(query);
        var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
        var response = await httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var usersData = JsonSerializer.Deserialize<Role>(contentStream, JsonSerializerOptions);
        if (usersData == null)
            throw new InvalidOperationException("Failed to get list of role users.");

        return usersData.SystemUsers;
    }

    public async Task<Person?> FindPersonViaGraph(string personName)
    {
        if (string.IsNullOrWhiteSpace(personName))
            throw new ArgumentNullException(nameof(personName));

        personName = personName.Trim();
        if (string.Compare(personName, "I", StringComparison.OrdinalIgnoreCase) == 0 ||
            string.Compare(personName, "me", StringComparison.OrdinalIgnoreCase) == 0)
        {
            _user!.Wait();
            return new Person() { DisplayName = _user.Result.DisplayName, UserPrincipalName = _user.Result.UserPrincipalName };
        }

        var people = await _graphClient.Value.Me.People.GetAsync((c) =>
        {
            c.QueryParameters.Top = 300;
        }).ConfigureAwait(false);
        if (people == null || people.Value == null || people.Value.Count <= 0)
            return null;

        // Exact match on person name
        foreach (var person in people.Value)
        {
            if (string.Compare(person.DisplayName, personName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(person.GivenName, personName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(person.Surname, personName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return person;
            }
        }

        var personNameParts = personName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var personNamePart in personNameParts) 
        { 
            foreach (var person in people.Value)
            {
                if (string.Compare(person.DisplayName, personNamePart, StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(person.GivenName, personNamePart, StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(person.Surname, personNamePart, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    return person;
                }
            }
        }

        return null;
    }

    public bool ConfirmAction(string action)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine();
        Console.Write($"{action} [Yes]/No: ");
        Console.ResetColor();
        var response = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(response))
            return true;

        response = response.Trim();

        return response.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
               response.Equals("Y", StringComparison.OrdinalIgnoreCase);
    }

    public bool EnsureSelectedEnvironment(string environmentHint, out string response)
    {
        response = string.Empty;

        if (string.IsNullOrWhiteSpace(environmentHint))
        {
            if (SelectedEnvironment != null)
                return true;
            response = "Power Platform environment is required. Ask for a name";
            return false;
        }

        _environments.Wait();

        // 1. Exact match on name
        foreach (var environment in _environments.Result)
        {
            if (environment == null)
                continue;

            if (string.Compare(environment.Properties.DisplayName, environmentHint, StringComparison.OrdinalIgnoreCase) == 0)
            {
                SelectedEnvironment = environment;
                return true;
            }
        }

        // 2. Exact substring match on name
        foreach (var environment in _environments.Result)
        {
            if (environment == null)
                continue;

            if (environment.Properties.DisplayName.Contains(environmentHint, StringComparison.OrdinalIgnoreCase))
            {
                SelectedEnvironment = environment;
                return true;
            }
        }

        if (SelectedEnvironment != null)
            return true;

        response = "Power Platform environment is required. Ask for a name";
        return false;
    }

    public bool FindLocalFolder(string saveLocation, out string filePath, out string response)
    {
        response = string.Empty;
        filePath = string.Empty;
        if (!Directory.Exists(saveLocation))
        {
            var dirs = Directory.GetDirectories(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                if (dir.EndsWith(saveLocation, StringComparison.OrdinalIgnoreCase))
                {
                    filePath = dir;
                    break;
                }
            }
        }
        else
            filePath = saveLocation;

        if (string.IsNullOrWhiteSpace(filePath))
        {
            response = $"Directory '{saveLocation}' doesn't exist. You need to ask user for different directory name";
            return false;
        }

        return true;
    }

    #endregion

    #region IDisposable

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // Dispose managed objects
                _liteDatabase.Dispose();
            }

            // Free unmanaged resources (unmanaged objects) and override finalizer
            // Set large fields to null
            disposedValue = true;
        }
    }

    // Override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~DataverseAIClient()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    #endregion
}
