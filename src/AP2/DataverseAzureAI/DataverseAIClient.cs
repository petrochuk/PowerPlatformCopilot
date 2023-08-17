#region using
using AP2.DataverseAzureAI.Metadata;
using AP2.DataverseAzureAI.OData;
using AP2.DataverseAzureAI.Settings;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
#endregion

namespace AP2.DataverseAzureAI;

/// <summary>
/// https://learn.microsoft.com/en-us/azure/ai-services/openai/how-to/function-calling
/// </summary>
public partial class DataverseAIClient
{
    #region Constants

    const string TableNotFound = "Table not found";
    const string PropertyNotFound = "Property not found";
    const string AttributeNotFound = "Attribute not found";
    const string First = "First";
    const string Last = "Last";

    #endregion

    #region Fields

    private readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    private Lazy<OpenAIClient> _openAIClient;
    private Lazy<GraphServiceClient> _graphClient;

    private readonly HttpClient _httpClient;
    private IList<EntityMetadataModel>? _entityMetadataModels;
    private readonly Lazy<Task<IList<AppModule>>> _appModules;
    private readonly Lazy<Task<IList<CanvasApp>>> _canvasApps;
    private readonly Lazy<Task<IList<Solution>>> _solutions;
    private string? PowerPlatformApiPrefix;

    private readonly IOptions<AzureAISettings> _azureAISettings;
    private readonly AIFunctionCollection _aIFunctionsCollection = new(typeof(DataverseAIClient));
    private readonly ChatCompletionsOptions _chatOptions = new ()
    {
        Temperature = 1,
        MaxTokens = 800,
        NucleusSamplingFactor = (float)0.95,
        FrequencyPenalty = 0,
        PresencePenalty = 0,
    };

    #endregion

    #region Constructors & Initialization

    public DataverseAIClient(HttpClient httpClient, IOptions<AzureAISettings> azureAISettings, 
        IAuthenticationProvider authenticationProvider)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _azureAISettings = azureAISettings;

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

        _appModules = new Lazy<Task<IList<AppModule>>>(async () =>
        {
            if (EnvironmentId == Guid.Empty)
                throw new InvalidOperationException($"{nameof(EnvironmentId)} is not set.");

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"appmodules/Microsoft.Dynamics.CRM.RetrieveUnpublishedMultiple()"));
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var appModules = JsonSerializer.Deserialize<ODataContext<AppModule>>(contentStream, _jsonSerializerOptions);
            if (appModules == null)
                throw new InvalidOperationException("Failed to get list of PowerApps.");
            return appModules.Values;
        });

        _canvasApps = new Lazy<Task<IList<CanvasApp>>>(async () =>
        {
            if (EnvironmentId == Guid.Empty)
                throw new InvalidOperationException($"{nameof(EnvironmentId)} is not set.");

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildApiQueryUri($"powerapps/apps?%24expand=permissions%28%24filter%3DmaxAssignedTo%28%27{UserObjectId}%27%29%29&%24filter=classification+eq+%27SharedWithMeApps%27+and+environment+eq+%27{EnvironmentId}%27&api-version=1"));
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var appModules = JsonSerializer.Deserialize<ODataContext<CanvasApp>>(contentStream, _jsonSerializerOptions);
            if (appModules == null)
                throw new InvalidOperationException("Failed to get list of PowerApps.");
            return appModules.Values;
        });

        _solutions = new Lazy<Task<IList<Solution>>>(async () =>
        {
            if (EnvironmentId == Guid.Empty)
                throw new InvalidOperationException($"{nameof(EnvironmentId)} is not set.");

            using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"solutions?$expand=createdby,modifiedby"));
            var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var solutions = JsonSerializer.Deserialize<ODataContext<Solution>>(contentStream, _jsonSerializerOptions);
            if (solutions == null)
                throw new InvalidOperationException("Failed to get list of solutions.");
            return solutions.Values;
        });
    }

    public async Task LoadMetadata()
    {
        if (EnvironmentId == Guid.Empty)
            throw new InvalidOperationException($"{nameof(EnvironmentId)} is not set.");

        // Get environment details from Global Discovery Service
        using var gdsRequest = new HttpRequestMessage(HttpMethod.Get, $"https://globaldisco.crm.dynamics.com/api/discovery/v2.0/Instances?$filter=EnvironmentId eq '{EnvironmentId}'");
        var gdsResponse = await _httpClient.SendAsync(gdsRequest).ConfigureAwait(false);
        gdsResponse.EnsureSuccessStatusCode();
        var parsedToken = new JwtSecurityToken(jwtEncodedString: gdsRequest.Headers.Authorization?.Parameter);
        GivenName = parsedToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;
        FullName = parsedToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value;
        UserObjectId = parsedToken.Claims.FirstOrDefault(c => c.Type == "oid")?.Value;
        var gdsContentStream = await gdsResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var environments = JsonSerializer.Deserialize<ODataContext<EnvironmentInstance>>(gdsContentStream, _jsonSerializerOptions);
        if (environments == null || environments.Values.Count <= 0)
            throw new InvalidOperationException($"EnvironmentId '{EnvironmentId}' was not found.");
        EnvironmentInstance = environments.Values.First();
        PowerPlatformApiPrefix = EnvironmentInstance.EnvironmentId.Replace("-", "");
        PowerPlatformApiPrefix = PowerPlatformApiPrefix.Insert(PowerPlatformApiPrefix.Length - 2, ".");

        // Get Dataverse Environment Metadata
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"EntityDefinitions"));
        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

        var metadata = JsonSerializer.Deserialize<ODataContext<EntityMetadataModel>>(contentStream, _jsonSerializerOptions);
        if (metadata == null || metadata.Values.Count <= 0)
            throw new InvalidOperationException("No metadata was returned.");
        _entityMetadataModels = metadata.Values;

        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"You are an assistant, helping '{FullName}' interact with Microsoft Power Platform."));
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"You are assisting **{FullName}**"));

        var listOfProperties = string.Join(", ", EntityMetadataModel.Properties.Keys.ToList());
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Each Dataverse table or entity has following properties: {listOfProperties}"));

        listOfProperties = string.Join(", ", Solution.Properties.Keys.ToList());
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Each Dataverse solution has following properties: {listOfProperties}"));

        listOfProperties = string.Join(", ", CanvasAppProperties.Properties.Keys.ToList());
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Each canvas app has following properties: {listOfProperties}"));
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Call a function if you need to get updated information"));
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Each function can be called multiple times"));
    }

    #endregion

    #region Properties

    public Uri? OpenApiEndPoint { get; set; }

    public string? OpenApiKey { get; set; }

    public string? OpenApiModel { get; set; }

    public string? OpenApiModelInternal
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

    public string? GivenName { get; private set; }

    public string? FullName { get; private set; }

    public string? UserObjectId { get; private set; }

    #endregion

    #region Chat

    public async Task<string> GetChatCompletionAsync(string prompt)
    {
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.User, prompt));
        var response = await _openAIClient.Value.GetChatCompletionsAsync(OpenApiModelInternal, _chatOptions).ConfigureAwait(false);
        if (response == null || response.Value == null || response.Value.Choices == null || response.Value.Choices.Count <= 0)
            throw new ApplicationException("Unable to get Azure Open AI response");

        var responseMessage = response.Value.Choices[0].Message;
        while (responseMessage.FunctionCall != null && responseMessage.Role == ChatRole.Assistant)
        {
            if (!_aIFunctionsCollection.Functions.TryGetValue(responseMessage.FunctionCall.Name, out var aiFunction))
                throw new ApplicationException($"Unable to find function {responseMessage.FunctionCall.Name}");

            var functionResponse = await aiFunction.Invoke(this, responseMessage.FunctionCall.Arguments).ConfigureAwait(false);

            _chatOptions.Messages.Add(
                new ChatMessage(ChatRole.Function, functionResponse)
                {
                    Name = responseMessage.FunctionCall.Name
                }
            );
            response = await _openAIClient.Value.GetChatCompletionsAsync(OpenApiModelInternal, _chatOptions).ConfigureAwait(false);
            if (response == null || response.Value == null || response.Value.Choices == null || response.Value.Choices.Count <= 0)
                throw new ApplicationException("Unable to get Azure Open AI response");
            responseMessage = response.Value.Choices[0].Message;
        }
        _chatOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, response.Value.Choices[0].Message.Content));

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
        return new Uri(EnvironmentInstance!.ApiUrl, $"api/data/v9.2/{query}");
    }

    [DebuggerStepThrough]
    private Uri BuildApiQueryUri(string query)
    {
        var baseUri = new Uri($"https://{PowerPlatformApiPrefix}.environment.api.powerplatform.com");
        return new Uri(baseUri, query);
    }

    #endregion
}
