using AP2.DataverseAzureAI.Extensions;
using AP2.DataverseAzureAI.Globalization;
using AP2.DataverseAzureAI.Metadata;
using AP2.DataverseAzureAI.Metadata.Actions;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace AP2.DataverseAzureAI;

public partial class DataverseAIClient
{
    #region Assistant skill functions for Tables

    [Description("Returns description for Dataverse table or entity")]
    public async Task<string> GetTableDescription(
        [Description("Power Platform environment")]
        string environment,
        [Description("Exact or partial name of Dataverse table or entity")]
        string tableName)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        var entityMetadataModels = await SelectedEnvironment!.EntityMetadataModels.Value.ConfigureAwait(false);

        var entityMetadataModel = GetEntityMetadataModel(entityMetadataModels, tableName);
        if (entityMetadataModel == null)
            return TableNotFound;

        _hyperlinks[entityMetadataModel.DisplayOrLogicalName] = new Uri($"https://make.powerapps.com/environments/{SelectedEnvironment.Name}/entities/{entityMetadataModel.MetadataId}");

        return $"Found table {entityMetadataModel.DisplayOrLogicalName} with description: {entityMetadataModel.Description.UserLocalizedLabel!.Label}";
    }

    [Description("Returns filtered list of tables based on specified property value")]
    public async Task<string> ListOfTablesByPropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Property name")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(propertyName) || !EntityMetadataModel.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;

        var entityMetadataModels = await SelectedEnvironment!.EntityMetadataModels.Value.ConfigureAwait(false);
        var result = new List<string>();
        foreach (var entityMetadataModel in entityMetadataModels)
        {
            if (propertyInfo.Equals(entityMetadataModel, propertyValueFilter, _timeProvider))
            {
                result.Add(entityMetadataModel.DisplayOrLogicalName);
                _hyperlinks[entityMetadataModel.DisplayOrLogicalName] = new Uri($"https://make.powerapps.com/environments/{SelectedEnvironment.Name}/entities/{entityMetadataModel.MetadataId}");
            }
        }

        if (result.Count == 0)
            return "Not found any tables matching specified filter";

        return $"Found following table(s) {string.Join(", ", result)}";
    }

    [Description("Returns property value for specified table")]
    public async Task<string> GetTablePropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Exact or partial name of Dataverse table")]
        string tableName,
        [Description("Property name")]
        string propertyName)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        var entityMetadataModels = await SelectedEnvironment!.EntityMetadataModels.Value.ConfigureAwait(false);
        var entityMetadataModel = GetEntityMetadataModel(entityMetadataModels, tableName);
        if (entityMetadataModel == null)
            return TableNotFound;

        if (string.IsNullOrWhiteSpace(propertyName))
            return PropertyNotFound;

        if (EntityMetadataModel.Properties.TryGetValue(propertyName, out var property))
        {
            return property.GetValue(entityMetadataModel, FullName, _timeProvider);
        }

        return PropertyNotFound;
    }

    #endregion

    #region Assistant skill functions for Power Apps

    const string PowerAppsNotFound = "Not found any PowerApps matching filter criteria";

    [Description("Returns filtered list of Model-driven PowerApps based on specified property value")]
    public async Task<string> ListOfModelDrivenAppsByPropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Property name or empty to return all Model-driven PowerApps")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        var appModules = await SelectedEnvironment!.AppModules.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "List of Model-driven apps: " + string.Join(", ", appModules.Select(x => x.Name));
        }

        if (!AppModule.Properties.TryGetValue(propertyName, out var propertyInfo))
        {
            return PropertyNotFound;
        }

        var result = new List<string>();
        foreach (var appModule in appModules)
        {
            if (propertyInfo.Equals(appModule, propertyValueFilter, _timeProvider))
                result.Add(appModule.Name);
        }

        if (result.Count == 0)
            return PowerAppsNotFound;

        return string.Join(", ", result);
    }

    [Description("Finds a canvas app based on specified property value")]
    public async Task<string> FindCanvasApp(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("Property name")]
        string propertyName,
        [Required, Description("Property value")]
        string propertyValue)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        var canvasApps = await SelectedEnvironment.CanvasApps.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "List of canvas apps: " + string.Join(", ", canvasApps.Select(x => x.Properties.DisplayName));
        }

        if (!CanvasAppProperties.Properties.TryGetValue(propertyName, out var propertyInfo))
        {
            return PropertyNotFound;
        }

        if (Strings.Last.ContainsKey(propertyValue))
        {
            CanvasApp? singleResult = null;
            foreach (var canvasApp in canvasApps)
            {
                if (singleResult == null)
                {
                    singleResult = canvasApp;
                    continue;
                }

                if (0 < propertyInfo.CompareTo(canvasApp.Properties, singleResult.Properties))
                    singleResult = canvasApp;
            }

            return singleResult == null ? PowerAppsNotFound : $"Found: {singleResult.Properties.DisplayName}";
        }
        else
        {
            var result = new List<string>();
            foreach (var canvasApp in canvasApps)
            {
                if (propertyInfo.Equals(canvasApp.Properties, propertyValue, _timeProvider))
                    result.Add(canvasApp.Properties.DisplayName);
            }

            if (result.Count == 0)
                return PowerAppsNotFound;

            return "List of canvas apps: " + string.Join(", ", result);
        }
    }

    [Description("Returns all or filtered list of canvas apps based on specified property value")]
    public async Task<string> ListOfCanvasAppsByPropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Property name or empty to return all canvas PowerApps user has access to")]
        string propertyName,
        [Description("Property value")]
        string propertyValueFilter,
        [Description("Optional limit like last, first, top 10, bottom 3")]
        string sortOrder)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        var canvasApps = await SelectedEnvironment!.CanvasApps.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            foreach (var canvasApp in canvasApps)
            {
                _hyperlinks[canvasApp.Properties.DisplayName] = canvasApp.Properties.AppPlayUri;
            }
            return $"'{SelectedEnvironment}' has {canvasApps.Count} canvas app(s). DisplayName(s): " + string.Join(", ", canvasApps.Select(x => $"'{x.Properties.DisplayName}'"));
        }

        if (!CanvasAppProperties.Properties.TryGetValue(propertyName, out var propertyInfo))
        {
            return PropertyNotFound;
        }
        
        if (propertyValueFilter != null && Strings.Last.ContainsKey(propertyValueFilter))
        {
            CanvasApp? singleResult = null;
            foreach (var canvasApp in canvasApps)
            {
                if (singleResult == null)
                {
                    singleResult = canvasApp;
                    continue;
                }

                if (0 < propertyInfo.CompareTo(canvasApp.Properties, singleResult.Properties))
                    singleResult = canvasApp;
            }

            if (singleResult != null)
                _hyperlinks[singleResult.Properties.DisplayName] = singleResult.Properties.AppPlayUri;
            return singleResult == null ? PowerAppsNotFound : $"Found this canvas app: '{singleResult.Properties.DisplayName}'";
        }
        else
        {
            var result = new List<string>();
            foreach (var canvasApp in canvasApps)
            {
                if (propertyInfo.Equals(canvasApp.Properties, propertyValueFilter, _timeProvider))
                {
                    _hyperlinks[canvasApp.Properties.DisplayName] = canvasApp.Properties.AppPlayUri;
                    result.Add($"'{canvasApp.Properties.DisplayName}'");
                }
            }

            if (result.Count == 0)
                return PowerAppsNotFound;

            return "Found these canvas apps matching filter: " + string.Join(", ", result);
        }
    }

    [Description($"Returns property value for a canvas app")]
    public async Task<string> GetCanvasAppPropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Canvas app name")]
        string canvasAppName,
        [Description("Property name")]
        string propertyName)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(canvasAppName))
            return "Canvas app name is required. Ask for name";

        if (string.IsNullOrWhiteSpace(propertyName) || !CanvasAppProperties.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;

        var canvasApps = await SelectedEnvironment!.CanvasApps.Value.ConfigureAwait(false);
        foreach (var canvasApp in canvasApps)
        {
            if (string.Equals(canvasAppName, canvasApp.Properties.DisplayName, StringComparison.OrdinalIgnoreCase))
                return propertyInfo.GetValue(canvasApp.Properties, FullName, _timeProvider);
        }

        return $"Canvas app '{canvasAppName}' was not found";
    }

    #endregion

    #region Assistant skill functions for Dataverse Solutions

    [Description("Takes in query text as a parameter and converts it to SQL query and executes the SQL query on Dataverse and sends the result back. It knows about systemuser, teams and roles. It can convert any natural language query to SQL query and return result of that SQL query.")]
    public async Task<string?> NaturalLanguageToSqlQuery(
        [Description("Power Platform environment")]
        string environment,
        [Description("Natural language query text which will be converted to SQL query and executed.")]
        string queryText)
    {
        if (!EnsureSelectedEnvironment(environment, out var response))
            return response;

        var copilotResponse = await CallDataverseCopilot(queryText).ConfigureAwait(false);
        if (copilotResponse == null || copilotResponse.QueryResult == null)
            return "Unable to convert natural language query to SQL query. Please try again with different text.";

        if (copilotResponse.QueryResult.Results == null || copilotResponse.QueryResult.Results.Count == 0)
            return "No results found.";

        var responseText = new StringBuilder();
        foreach (var result in copilotResponse.QueryResult.Results)
        {
            if (!string.IsNullOrWhiteSpace(result.name))
            {
                responseText.Append($"{result.name}");
                if (result.RecordLinks != null && result.RecordLinks.Count > 0)
                    _hyperlinks[result.name] = new Uri(result.RecordLinks[0]);
            }
            else if (!string.IsNullOrWhiteSpace(result.fullname))
            {
                responseText.Append($"{result.fullname}");
                if (result.RecordLinks != null && result.RecordLinks.Count > 0)
                    _hyperlinks[result.fullname] = new Uri(result.RecordLinks[0]);
            }
            else
                continue;

            responseText.AppendLine(",");
        }

        return "Here is the list:"+ responseText.ToString();
    }
    
    [Description("Returns all solutions or filtered list of installed/imported based on specified property value and optional user")]
    public async Task<string> ListOSolutionsByPropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Property name or empty to return all Dataverse solutions")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter,
        [Description("Optional personal pronoun such as 'I', 'me', co-worker's first, last name, or full name for filters in addition to CreatedOn, ModifiedOn")]
        string userFirstLastOrPronoun)
    {
        if (!EnsureSelectedEnvironment(environment, out var response))
            return response;

        var solutions = await SelectedEnvironment!.Solutions.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            if (!string.IsNullOrWhiteSpace(propertyValueFilter))
                return "Property name is required when filtering by property value";
            return "List of all solutions: " + string.Join(", ", solutions.Select(s => s.FriendlyName));
        }

        if (!Solution.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;

        var result = new List<string>();
        foreach (var solution in solutions)
        {
            if (propertyInfo.Equals(solution, propertyValueFilter, _timeProvider))
            {
                if (!string.IsNullOrWhiteSpace(userFirstLastOrPronoun))
                {
                    PropertyInfo? userPropertyInfo = null;
                    if (propertyInfo.Name == nameof(Solution.ModifiedOn))
                        Solution.Properties.TryGetValue(nameof(Solution.ModifiedBy), out userPropertyInfo);
                    else if (propertyInfo.Name == nameof(Solution.CreatedOn))
                        Solution.Properties.TryGetValue(nameof(Solution.CreatedBy), out userPropertyInfo);

                    if (string.Compare(userFirstLastOrPronoun, "I", StringComparison.OrdinalIgnoreCase) == 0 ||
                        string.Compare(userFirstLastOrPronoun, "me", StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        if (userPropertyInfo == null || !userPropertyInfo.Equals(solution, FullName, _timeProvider))
                            continue;
                    }
                }

                result.Add(solution.FriendlyName);
            }
        }

        if (result.Count == 0)
            return $"Not found any solutions with {propertyName} equals {propertyValueFilter} filter";

        return $"Solution(s) with {propertyName} equals {propertyValueFilter}: {string.Join(", ", result)}";
    }

    [Description("Returns list of components inside Dataverse solutions. It can filter on component type, name or other properties")]
    public async Task<string> ListOSolutionComponents(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("Dataverse solution friendly, or unique name, or solution id")]
        string solutionName,
        [Description("Component type or empty to return all components")]
        string componentType,
        [Description("Component filter value")]
        string componentFilterFilter,
        [Description("Optional personal pronoun such as 'I', 'me', co-worker's first, last name, or full name for filters in addition to CreatedOn, ModifiedOn")]
        string userFirstLastOrPronoun)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required. Ask for it.";
        var solutions = await SelectedEnvironment!.Solutions.Value.ConfigureAwait(false);

        foreach (var solution in solutions)
        {
            if (string.Compare(solution.FriendlyName, solutionName, StringComparison.OrdinalIgnoreCase) != 0)
                continue;

            solution.Components ??= await LoadSolutionComponents(solution.SolutionId);
            var response = new StringBuilder();
            foreach (var componentsByType in solution.Components)
            {
                if (Enum.IsDefined(typeof(SolutionComponentType), componentsByType.Key))
                    response.AppendLine($"{componentsByType.Key}(s): {componentsByType.Value.Count}");
            }
            return $"{solutionName} contains: {response}";
        }

        return "Not implemented yet";
    }


    [Description("Adds component to Dataverse solution")]
    public async Task<string> AddSolutionComponent(
        [Description("Power Platform environment")]
        string environment,
        [Description("Dataverse solution friendly, unique name or id")]
        string solutionName,
        [Description("Component type")]
        string componentType,
        [Description("Component name")]
        string componentName)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required. Ask for it.";
        var solutions = await SelectedEnvironment!.Solutions.Value.ConfigureAwait(false);

        Solution? selectedSolution = null;
        foreach (var solution in solutions)
        {
            if (string.Compare(solution.FriendlyName, solutionName, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (string.Compare(solution.UniqueName, solutionName, StringComparison.OrdinalIgnoreCase) != 0)
                    continue;
            }

            selectedSolution = solution;
            break;
        }

        if (selectedSolution == null)
            return $"Solution '{solutionName}' was not found. Ask for different name or if you need to create new one.";

        if (!Enum.TryParse(componentType, true, out SolutionComponentType componentTypeValue) || componentTypeValue != SolutionComponentType.CanvasApp)
            return $"Component type '{componentType}' is not supported.";

        var canvasApps = await SelectedEnvironment!.CanvasApps.Value.ConfigureAwait(false);
        foreach (var canvasApp in canvasApps)
        {
            if (!string.Equals(componentName, canvasApp.Properties.DisplayName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!ConfirmAction($"Do you want to add {canvasApp.Properties.DisplayName} to '{selectedSolution.FriendlyName}'?"))
                return UserDeclinedAction;

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildOrgDataQueryUri($"AddSolutionComponent"));
            var addSolutionComponent = new AddSolutionComponent
            {
                ComponentId = canvasApp.Name,
                ComponentType = (int)componentTypeValue,
                SolutionUniqueName = selectedSolution.UniqueName
            };
            request.Content = new StringContent(JsonSerializer.Serialize(addSolutionComponent, JsonSerializerOptions), Encoding.UTF8, "application/json");
            var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            return $"Canvas app '{canvasApp.Properties.DisplayName}' added to '{selectedSolution.FriendlyName}' solution";
        }

        return $"Canvas app '{componentName}' was not found.";
    }

    [Description("Returns property value for specified Dataverse solution")]
    public async Task<string> GetSolutionPropertyValue(
        [Description("Power Platform environment")]
        string environment,
        [Description("Dataverse solution friendly, unique name or id")]
        string solutionName,
        [Description("Property name")]
        string propertyName)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;
        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required. Ask for it.";
        if (string.IsNullOrWhiteSpace(propertyName) || !Solution.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;
        if (propertyInfo.Name == nameof(Solution.Components))
            return $"call {nameof(ListOSolutionComponents)}";

        solutionName = solutionName.Trim();

        var solutions = await SelectedEnvironment!.Solutions.Value.ConfigureAwait(false);
        foreach (var solution in solutions)
        {
            if (string.Compare(solution.FriendlyName, solutionName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(solution.UniqueName, solutionName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(solution.SolutionId.ToString(), solutionName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return propertyInfo.GetValue(solution, FullName, _timeProvider);
            }
        }

        return $"Not found any solutions matching '{solutionName}' filter";
    }

    [Description("Exports Power Platform solution and saves it to local file")]
    public async Task<string> ExportSolution(
        [Description("Power Platform environment")]
        string environment,
        [Description("Dataverse solution friendly, unique name or id")]
        string solutionName,
        [Description("Folder name or file name")]
        string saveLocation)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;
        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required. Ask for it.";

        var solutions = await SelectedEnvironment!.Solutions.Value.ConfigureAwait(false);
        foreach (var solution in solutions)
        {
            if (string.Compare(solution.FriendlyName, solutionName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(solution.UniqueName, solutionName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(solution.SolutionId.ToString(), solutionName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (!FindLocalFolder(saveLocation, out var filePath, out var response))
                    return response;

                filePath = Path.Combine(filePath, $"{solution.FriendlyName}.zip");
                if (!ConfirmAction($"Do you want to save solution '{solution.FriendlyName}' to '{filePath}'?"))
                    return UserDeclinedAction;

                var exportSolution = new ExportSolution() { SolutionName = solution.UniqueName };
                using var request = new HttpRequestMessage(HttpMethod.Post, BuildOrgDataQueryUri($"ExportSolution"));
                request.Content = new StringContent(JsonSerializer.Serialize(exportSolution, JsonSerializerOptions), Encoding.UTF8, "application/json");
                var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
                var httpResponse = await httpClient.SendAsync(request).ConfigureAwait(false);
                httpResponse.EnsureSuccessStatusCode();
                var exportSolutionResponse = JsonSerializer.Deserialize<ExportSolutionResponse>(await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false), JsonSerializerOptions);
                using (var fs = new FileStream(filePath, FileMode.CreateNew))
                {
                    fs.Write(Convert.FromBase64String(exportSolutionResponse.ExportSolutionFile));
                }

                return $"Solution '{solution.FriendlyName}' saved to '{filePath}'";
            }
        }

        return $"Not found any solutions matching '{solutionName}' name";
    }

    #endregion

    #region Send email

    [Description("Sends an email or shares a link to an item, record or anything else inside PowerPlatform including but not limited to app, solution, table, component")]
    public async Task<string> SendEmailOrShareLinkWithSomeone(
        [Required, Description("Type of an item to send")]
        string itemType,
        [Required, Description("Name for an item")]
        string itemName,
        [Required, Description("Personal pronoun such as 'I', 'me', co-worker's first, last name, or full name to send email to or share link with")]
        string userFirstLastOrPronoun,
        [Required, Description("Suggested email or message subject/title")]
        string emailTitle,
        [Required, Description("Suggested email or message body text")]
        string emailBody)
    {
        if (string.IsNullOrWhiteSpace(userFirstLastOrPronoun))
            return "Person name is required. Ask for it.";

        var person = await FindPersonViaGraph(userFirstLastOrPronoun).ConfigureAwait(false);
        if (person == null) 
            return $"Unable to find {userFirstLastOrPronoun}";

        var message = new Message
        {
            Subject = emailTitle,
            Body = new ItemBody
            {
                ContentType = BodyType.Html,
                Content = emailBody
            },
            ToRecipients = new List<Recipient>
            {
                new Recipient { EmailAddress = new EmailAddress { Address = person.UserPrincipalName } },
            }
        };

        Microsoft.Graph.Me.SendMail.SendMailPostRequestBody body = new()
        {
            Message = message,
            SaveToSentItems = false
        };
        await _graphClient.Value.Me.SendMail.PostAsync(body);
        return $"Sent email message to {person.DisplayName} ({person.UserPrincipalName})";
    }

    #endregion

    #region Save to file system

    [Description("Save to file system: text output, apps, solutions, lists or any other Power Platform component")]
    public async Task<string> SaveToFileSystem(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("Type of an item to save")]
        string itemType,
        [Required, Description("Name for an item")]
        string itemName,
        [Description("Folder name or file name")]
        string saveLocation)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return "Type of an item is required";
        if (string.IsNullOrWhiteSpace(itemName))
            return "Name for an item is required";

        itemType = itemType.Trim().Replace(" ", "");
        if (string.Compare(itemType, SolutionComponentType.CanvasApp.ToString(), StringComparison.OrdinalIgnoreCase) == 0)
        {
            if (string.IsNullOrWhiteSpace(saveLocation))
                return "You need to ask user for directory name to save the app";

            if (!FindLocalFolder(saveLocation, out var filePath, out var response))
                return response;

            if (!EnsureSelectedEnvironment(environment, out var errorResponse))
                return errorResponse;

            var canvasApps = await SelectedEnvironment!.CanvasApps.Value.ConfigureAwait(false);
            foreach (var canvasApp in canvasApps)
            {
                if (string.Equals(itemName, canvasApp.Properties.DisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    filePath = Path.Combine(filePath, $"{canvasApp.Properties.DisplayName}.msapp");

                    if (!ConfirmAction($"Do you want to save Canvas App '{itemName}' to '{filePath}'?"))
                        return UserDeclinedAction;

                    // Download the app to file
                    var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
                    var responseStream = await httpClient.GetStreamAsync(canvasApp.Properties.AppUris.documentUri.value);
                    using var fileStream = new FileStream(filePath, FileMode.Create);
                    responseStream.CopyTo(fileStream);
                    Console.WriteLine($"Done");
                    return $"Canvas App '{itemName}' was saved.";
                }
            }
            return $"Canvas App '{itemName}' is not found.";
        }
        return $"Don't know how to save {itemType} type of item yet";
    }

    #endregion

    #region Create

    [Description("Creates new objects inside Power Platform including but not limited to apps, solutions, tables, users, components, records")]
    public async Task<string> CreateItemInsidePowerPlatform(
        [Description("Power Platform environment")]
        string environment,
        [Description("Type of an item to create")]
        string itemType,
        [Description("Name for an item")]
        string itemName)
    {
        if (string.IsNullOrWhiteSpace(itemType))
            return "Type of an item is required. Ask for it";
        if (string.IsNullOrWhiteSpace(itemName))
            return "Item name is required. Ask for it";

        if (string.Equals(itemType, "solution", StringComparison.OrdinalIgnoreCase))
        {
            if (!EnsureSelectedEnvironment(environment, out var errorResponse))
                return errorResponse;
            if (!ConfirmAction($"Do you want to create {itemType} named '{itemName}'?"))
                return UserDeclinedAction;

            using var request = new HttpRequestMessage(HttpMethod.Post, BuildOrgDataQueryUri($"solutions"));
            var solutionCreate = new SolutionCreate() { FriendlyName = itemName, UniqueName = itemName.Replace(" ", "") };
            request.Content = new StringContent(JsonSerializer.Serialize(solutionCreate, JsonSerializerOptions), Encoding.UTF8, "application/json");
            var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            SelectedEnvironment!.RefreshSolutions();
            return $"Solution '{itemName}' was successfully created";
        }

        return $"Don't know how to create {itemType}";
    }

    #endregion

    #region Roles / Permissions
    
    [Description("List of role member")]
    public async Task<string> ListOfRoleMembers(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("Role name")]
        string roleName,
        [Description("Optional business unit the role belongs to")]
        string businessUnit,
        [Required, Description("Person's first name, or last name, or full name, or a email")]
        string personName)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(roleName))
            return "Role name is required. Ask for role name.";

        // Send async requests in parallel
        var roles = SelectedEnvironment!.Roles.Value;
        Task<Person?>? person = null;
        if (!string.IsNullOrWhiteSpace(personName))
        {
            person = FindPersonViaGraph(personName);
            await Task.WhenAll(person, roles);
            if (person.Result == null && !string.IsNullOrWhiteSpace(personName))
                return $"Unable to find {personName}";
        }
        else
            await Task.WhenAll(roles);

        if (string.IsNullOrWhiteSpace(businessUnit))
            businessUnit = SelectedEnvironment.Properties.LinkedEnvironmentMetadata.domainName;

        var role = roles.Result.FirstOrDefault(r => 
            string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
        if (role == null)
        {
            role = roles.Result.FirstOrDefault(r => 
                r.Name.Contains(roleName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
            if (role == null)
                return $"Unable to find role {roleName} in {SelectedEnvironment.Properties.DisplayName}";
        }

        if (role.SystemUsers == null)
        {
            role.SystemUsers = await LoadRoleUsers(role.RoleId);
        }

        _hyperlinks[role.Name] = new Uri($"https://admin.powerplatform.microsoft.com/environments/{SelectedEnvironment.Properties.LinkedEnvironmentMetadata.resourceId}/securityroles/{role.RoleId}/members");

        if (person != null && person.Result != null)
        {
            // First pass, match by domainname (email)
            foreach (var systemUser in role.SystemUsers)
            {
                if (string.Equals(systemUser.DomainName, person.Result.UserPrincipalName, StringComparison.OrdinalIgnoreCase))
                    return $"{person.Result.DisplayName} is a member of {role.BusinessUnit.Name}/{role.Name} role";
            }

            // Second pass, match by fullname
            foreach (var systemUser in role.SystemUsers)
            {
                if (string.Equals(systemUser.FullName, person.Result.DisplayName, StringComparison.OrdinalIgnoreCase))
                    return $"{person.Result.DisplayName} is a member of {role.BusinessUnit.Name}/{role.Name} role";
            }

            return $"{person.Result.DisplayName} is not a member of {role.BusinessUnit.Name}/{role.Name} role";
        }

        // Return list of members
        return $"{role.BusinessUnit.Name}/{role.Name} role has {role.SystemUsers.Count} member(s): {string.Join(", ", role.SystemUsers.Select(u => u.FullName))}";
    }

    [Description("Count of roles privileges")]
    public async Task<string> CountOfRolePrivileges(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("Role name")]
        string roleName,
        [Description("Optional business unit the role belongs to")]
        string businessUnit)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(roleName))
            return "Role name is required. Ask for role name.";

        // Send async requests in parallel
        var roles = SelectedEnvironment!.Roles.Value;
        await Task.WhenAll(roles);

        if (string.IsNullOrWhiteSpace(businessUnit))
            businessUnit = SelectedEnvironment.Properties.LinkedEnvironmentMetadata.domainName;

        var role = roles.Result.FirstOrDefault(r =>
            string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
        if (role == null)
        {
            role = roles.Result.FirstOrDefault(r =>
                r.Name.Contains(roleName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
            if (role == null)
                return $"Unable to find role {roleName} in {SelectedEnvironment.Properties.DisplayName}";
        }

        if (role.RolePrivileges == null)
        {
            role.RolePrivileges = await LoadRolePrivileges(role.RoleId);
        }

        return $"{role.BusinessUnit.Name}/{role.Name} role has {role.RolePrivileges.Count} privilege(s)";
    }

    [Description("Updates user roles inside Power Platform")]
    public async Task<string> UpdateUserPermission(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("Grant or Revoke permission")]
        string changeType,
        [Required, Description("Person's first name, or last name, or full name, or a email")]
        string personName,
        [Required, Description("Role name")]
        string roleName,
        [Description("Optional business unit role belongs to")]
        string businessUnit)
    {
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        if (string.IsNullOrWhiteSpace(changeType))
            return "Change type is required. Ask for Grant or Revoke";

        if (string.IsNullOrWhiteSpace(roleName))
            return "Role name is required. Ask for role name.";

        // Send async requests in parallel
        var person = FindPersonViaGraph(personName);
        var roles = SelectedEnvironment!.Roles.Value;

        await Task.WhenAll(person, roles);
        if (person.Result == null)
            return $"Unable to find {personName}";

        var systemUser = await SelectedEnvironment!.GetSystemUser(person.Result.UserPrincipalName);
        if (systemUser == null)
            return $"Unable to find {personName} in {SelectedEnvironment.Properties.DisplayName}";

        if (string.IsNullOrWhiteSpace(businessUnit))
            businessUnit = SelectedEnvironment.Properties.LinkedEnvironmentMetadata.domainName;

        var role = roles.Result.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase) &&
                                             string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
        if (role == null)
        {
            role = roles.Result.FirstOrDefault(r => r.Name.Contains(roleName, StringComparison.OrdinalIgnoreCase) &&
                                                 string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
            if (role == null)
                return $"Unable to find role {roleName} in {SelectedEnvironment.Properties.DisplayName}";
        }

        if (!ConfirmAction($"Do you want to grant '{role.BusinessUnit.Name}/{role.Name}' role to {systemUser.FullName}?"))
            return UserDeclinedAction;

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildOrgDataQueryUri($"systemusers({systemUser.SystemUserId})/systemuserroles_association/$ref"));
        request.Content = new StringContent($"{{\"@odata.id\":\"{BuildOrgDataQueryUri($"roles({role.RoleId})")}\"}}", Encoding.UTF8, "application/json");
        var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            return responseContent;

        return $"Granted '{role.BusinessUnit.Name}/{role.Name}' role to {systemUser.FullName} in {SelectedEnvironment.Properties.DisplayName}";
    }

    #endregion

    #region Share

    [Description("Share canvas app inside Power Platform")]
    public async Task<string> ShareCanvasApp(
        [Description("Power Platform environment")]
        string environment,
        [Required, Description("App name or list of comma separated names")]
        string appNames,
        [Required, Description("Person's first name, last name or a email to share with")]
        string personName)
    {
        if (string.IsNullOrWhiteSpace(appNames))
            return "App name(s) is required";
        if (string.IsNullOrWhiteSpace(personName))
            return "Person name is required";
        if (!EnsureSelectedEnvironment(environment, out var errorResponse))
            return errorResponse;

        // Send async requests in parallel
        var person = FindPersonViaGraph(personName);
        var canvasApps = SelectedEnvironment!.CanvasApps.Value;

        await Task.WhenAll(person, canvasApps);
        if (person.Result == null)
            return $"Unable to find {personName}";

        // First, exact match by name
        var canvasAppsToShare = canvasApps.Result.Where(c => string.Equals(c.Properties.DisplayName, appNames, StringComparison.OrdinalIgnoreCase));
        if (canvasAppsToShare == null || !canvasAppsToShare.Any())
        {
            var appNamesList = appNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            canvasAppsToShare = canvasApps.Result.Where(c => appNamesList.Any(a => string.Equals(c.Properties.DisplayName, a.Trim(), StringComparison.OrdinalIgnoreCase)));
            if (canvasAppsToShare == null || !canvasAppsToShare.Any())
                return $"Unable to find {appNames} in {EnvironmentInstance.FriendlyName}";
        }

        var appNamesToShare = string.Join(", ", canvasAppsToShare.Select(c => c.Properties.DisplayName));
        if (!ConfirmAction($"Do you want to share '{appNamesToShare}' with {person.Result.DisplayName}?"))
            return UserDeclinedAction;

        var updatePermissionRequest = new UpdatePermissionRequest();
        updatePermissionRequest.Put.Add(new CanvasPermission()
        {
            Properties = new PermissionProperty()
            {
                RoleName = "CanView",
                NotifyShareTargetOption = "DoNotNotify",
                Principal = new Principal()
                {
                    Email = person.Result.UserPrincipalName!,
                    Id = person.Result.Id!,
                    Type = "User"
                }
            }
        });

        foreach (var canvasApp in canvasAppsToShare)
        {
            Console.WriteLine($"Sharing '{canvasApp.Properties.DisplayName}' with {person.Result.DisplayName}");
            var requestBody = JsonSerializer.Serialize(updatePermissionRequest, JsonSerializerOptions);
            using var request = new HttpRequestMessage(HttpMethod.Post, SelectedEnvironment.BuildEnvironmentApiQueryUri($"powerapps/apps/{canvasApp.Name}/modifyPermissions?%24filter=environment+eq+%27{SelectedEnvironment!.Name}%27&api-version=1"));
            request.Content = new StringContent(JsonSerializer.Serialize(updatePermissionRequest, JsonSerializerOptions), Encoding.UTF8, "application/json");
            var httpClient = _httpClientFactory.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
        }

        return FunctionCompletedSuccessfully;
    }

    [Description("Returns list of Azure Active Directory security groups person member of by calling Microsoft Graph API")]
    public async Task<string> ListActiveDirectorySecurityGroups(
        [Required, Description("Person's first name, last name or a email")]
        string personName)
    {
        if (string.IsNullOrWhiteSpace(personName))
            return "Person name is required. Ask for it";

        var person = await FindPersonViaGraph(personName).ConfigureAwait(false);
        if (person == null)
            return $"Unable to find {personName}";

        var stringBuilder = new StringBuilder();
        var groups = await _graphClient.Value.Users[person.Id].MemberOf.GetAsync();
        var pageIterator = PageIterator<DirectoryObject, DirectoryObjectCollectionResponse>.CreatePageIterator(_graphClient.Value, groups!,
            (g) => { 
                if (g is Group group)
                {
                    if (group.SecurityEnabled == true && 
                        string.IsNullOrWhiteSpace(group.MembershipRule) &&
                        !string.IsNullOrWhiteSpace(group.DisplayName))
                    {
                        if (stringBuilder.Length > 0)
                            stringBuilder.Append(", ");
                        stringBuilder.Append($"{group.DisplayName}");
                    }
                }
                return true; 
            }
        );

        await pageIterator.IterateAsync().ConfigureAwait(false);

        return "Person name is required. Ask for it";
    }

    #endregion
}
