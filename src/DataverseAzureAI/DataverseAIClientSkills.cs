using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using AP2.DataverseAzureAI.Extensions;
using AP2.DataverseAzureAI.Globalization;
using AP2.DataverseAzureAI.Metadata;
using Microsoft.Graph.Models;

namespace AP2.DataverseAzureAI;

public partial class DataverseAIClient
{
    #region Assistant skill functions for Tables

    [Description("Find Dataverse table or entity by exact or partial name")]
    public Task<string> FindTableByName(
        [Description("Exact or partial name of Dataverse table or entity")]
        string tableName)
    {
        var entityMetadataModel = GetEntityMetadataModel(tableName);
        if (entityMetadataModel == null)
            return Task.FromResult(TableNotFound);

        return Task.FromResult($"Found table {tableName} with description: {entityMetadataModel.Description.UserLocalizedLabel!.Label}");
    }

    [Description("Returns filtered list of tables based on specified property value")]
    public Task<string> ListOfTablesByPropertyValue(
        [Description("Property name")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter)
    {
        if (string.IsNullOrWhiteSpace(propertyName) || !EntityMetadataModel.Properties.TryGetValue(propertyName, out var propertyInfo))
            return Task.FromResult(PropertyNotFound);

        if (_entityMetadataModels == null)
            throw new InvalidOperationException("Metadata has not been loaded.");

        var result = new List<string>();
        foreach (var entityMetadataModel in _entityMetadataModels)
        {
            if (propertyInfo.Equals(entityMetadataModel, propertyValueFilter, _timeProvider))
                result.Add(entityMetadataModel.DisplayOrLogicalName);
        }

        if (result.Count == 0)
            return Task.FromResult("Not found any tables matching specified filter");

        return Task.FromResult($"Found following table(s) {string.Join(", ", result)}");
    }

    [Description("Returns property value for specified table")]
    public Task<string> GetTablePropertyValue(
        [Description("Exact or partial name of Dataverse table")]
        string tableName,
        [Description("Property name")]
        string propertyName)
    {
        var entityMetadataModel = GetEntityMetadataModel(tableName);
        if (entityMetadataModel == null)
            return Task.FromResult(TableNotFound);

        if (string.IsNullOrWhiteSpace(propertyName))
            return Task.FromResult(PropertyNotFound);

        if (EntityMetadataModel.Properties.TryGetValue(propertyName, out var property))
        {
            return Task.FromResult(property.GetValue(entityMetadataModel, FullName, _timeProvider));
        }

        return Task.FromResult(PropertyNotFound);
    }

    #endregion

    #region Assistant skill functions for Power Apps

    const string PowerAppsNotFound = "Not found any PowerApps matching filter criteria";

    [Description("Returns filtered list of Model-driven PowerApps based on specified property value")]
    public async Task<string> ListOfModelDrivenAppsByPropertyValue(
        [Description("Property name or empty to return all Model-driven PowerApps")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter)
    {
        var appModules = await _appModules.Value.ConfigureAwait(false);
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
        [Required, Description("Property name")]
        string propertyName,
        [Required, Description("Property value")]
        string propertyValue)
    {
        var canvasApps = await _canvasApps.Value.ConfigureAwait(false);
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

    [Description("Returns filtered list of canvas apps based on specified property value")]
    public async Task<string> ListOfCanvasAppsByPropertyValue(
        [Description("Property name or empty to return all canvas PowerApps")]
        string propertyName,
        [Description("Property value")]
        string propertyValueFilter,
        [Description("Optional limit like last, first, top 10, bottom 3")]
        string sortOrder)
    {
        var canvasApps = await _canvasApps.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return "List of canvas apps: " + string.Join(", ", canvasApps.Select(x => x.Properties.DisplayName));
        }

        if (!CanvasAppProperties.Properties.TryGetValue(propertyName, out var propertyInfo))
        {
            return PropertyNotFound;
        }
        
        if (Strings.Last.ContainsKey(propertyValueFilter))
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
                if (propertyInfo.Equals(canvasApp.Properties, propertyValueFilter, _timeProvider))
                    result.Add(canvasApp.Properties.DisplayName);
            }

            if (result.Count == 0)
                return PowerAppsNotFound;

            return "List of canvas apps: " + string.Join(", ", result);
        }
    }

    [Description($"Returns property value for a canvas app")]
    public async Task<string> GetCanvasAppPropertyValue(
        [Description("Canvas app name")]
        string canvasAppName,
        [Description("Property name")]
        string propertyName)
    {
        if (string.IsNullOrWhiteSpace(canvasAppName))
            return "Canvas app name is required";

        if (string.IsNullOrWhiteSpace(propertyName) || !CanvasAppProperties.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;

        var canvasApps = await _canvasApps.Value.ConfigureAwait(false);
        foreach (var canvasApp in canvasApps)
        {
            if (string.Equals(canvasAppName, canvasApp.Properties.DisplayName, StringComparison.OrdinalIgnoreCase))
                return propertyInfo.GetValue(canvasApp.Properties, FullName, _timeProvider);
        }

        return $"Canvas app '{canvasAppName}' was not found";
    }

    #endregion

    #region Assistant skill functions for Dataverse Solutions

    [Description("Returns filtered list of solutions installed/imported based on specified property value and optional user")]
    public async Task<string> ListOSolutionsByPropertyValue(
        [Description("Property name or empty to return all Dataverse solutions")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter,
        [Description("Optional personal pronoun such as 'I', 'me', co-worker's first, last name, or full name for filters in addition to CreatedOn, ModifiedOn")]
        string userFirstLastOrPronoun)
    {
        var solutions = await _solutions.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(propertyName))
            return "List of all solutions: " + string.Join(", ", solutions.Select(s => s.FriendlyName));

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
            return $"Not found any solutions matching '{propertyName} = {propertyValueFilter}' filter";

        return $"Solution(s) matching: {string.Join(", ", result)}";
    }

    [Description("Returns list of components inside Dataverse solutions. It can filter on component type, name or other properties")]
    public async Task<string> ListOSolutionComponents(
        [Required, Description("Dataverse solution friendly, or unique name, or solution id")]
        string solutionName,
        [Description("Component type or empty to return all components")]
        string componentType,
        [Description("Component filter value")]
        string componentFilterFilter,
        [Description("Optional personal pronoun such as 'I', 'me', co-worker's first, last name, or full name for filters in addition to CreatedOn, ModifiedOn")]
        string userFirstLastOrPronoun)
    {
        var solutions = await _solutions.Value.ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required";

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

    [Description("Returns property value for specified Dataverse solution")]
    public async Task<string> GetSolutionPropertyValue(
        [Description("Dataverse solution friendly, unique name or id")]
        string solutionName,
        [Description("Property name")]
        string propertyName)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required";
        if (string.IsNullOrWhiteSpace(propertyName) || !Solution.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;
        if (propertyInfo.Name == nameof(Solution.Components))
            return $"call {nameof(ListOSolutionComponents)}";

        solutionName = solutionName.Trim();

        var solutions = await _solutions.Value.ConfigureAwait(false);
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

    #endregion

    #region Send email

    [Description("Sends an email or shares a link to an item, record or anything else inside PowerPlatform including but not limited to app, solution, table, component")]
    public async Task<string> SendEmailOrShareLinkWithSomeone(
        [Required, Description("Type of an item to send")]
        string itemType,
        [Required, Description("Name for an item")]
        string itemName,
        [Required, Description("Person's first name, last name or a email to send email to or share link with")]
        string personName,
        [Required, Description("Suggested email or message subject/title")]
        string emailTitle,
        [Required, Description("Suggested email or message body text")]
        string emailBody)
    {
        if (string.IsNullOrWhiteSpace(personName))
            return "Person name is required";

        var person = await FindPersonViaGraph(personName).ConfigureAwait(false);
        if (person == null) 
            return $"Unable to find {personName}";

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
        return $"Sent message to {person.DisplayName} ({person.UserPrincipalName})";
    }

    #endregion

    #region Save to file system

    [Description("Save to file system: text output, apps, solutions, lists or any other Power Platform component")]
    public async Task<string> SaveToFileSystem(
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

            var filePath = string.Empty;
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
                return $"Directory '{saveLocation}' doesn't exist. You need to ask user for different directory name";

            var canvasApps = await _canvasApps.Value.ConfigureAwait(false);
            foreach (var canvasApp in canvasApps)
            {
                if (string.Equals(itemName, canvasApp.Properties.DisplayName, StringComparison.OrdinalIgnoreCase))
                {
                    filePath = Path.Combine(filePath, $"{canvasApp.Properties.DisplayName}.msapp");
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine();
                    Console.Write($"Do you want to save Canvas App '{itemName}' to '{filePath}'? [Yes]/No:");
                    Console.ResetColor();
                    var response = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(response) ||
                        response.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                        response.Equals("Y", StringComparison.OrdinalIgnoreCase))
                    {
                        // Download the app to file
                        var responseStream = await _httpClient.GetStreamAsync(canvasApp.Properties.AppUris.documentUri.value);
                        using var fileStream = new FileStream(filePath, FileMode.Create);
                        responseStream.CopyTo(fileStream);
                        Console.WriteLine($"Done");
                        return $"Canvas App '{itemName}' was saved.";
                    }
                }
            }
            return $"Canvas App '{itemName}' is not found.";
        }
        return $"Don't know how to save {itemType} type of item yet";
    }

    #endregion

    #region Create

    [Description("Creates new items, records or anything else inside PowerPlatform including but not limited to apps, solutions, tables, users, components")]
    public Task<string> CreateItemInsidePowerPlatform(
        [Description("Type of an item to create")]
        string itemType,
        [Description("Name for an item")]
        string itemName)
    {
        return Task.FromResult("Not implemented yet");
    }

    #endregion

    #region Update permissions

    [Description("Updates user permission inside Power Platform")]
    public async Task<string> UpdateUserPermission(
        [Required, Description("Grant or Revoke permission")]
        string changeType,
        [Required, Description("Person's first name, last name or a email to send email to or share link with")]
        string personName,
        [Required, Description("Role name")]
        string roleName,
        [Description("Optional business unit role belongs to")]
        string businessUnit)
    {
        if (string.IsNullOrWhiteSpace(changeType))
            return "Change type is required";

        if (string.IsNullOrWhiteSpace(roleName))
            return "Role name is required";

        // Send async requests in parallel
        var systemUsers = _systemUsers.Value;
        var person = FindPersonViaGraph(personName);
        var roles = _roles.Value;

        await Task.WhenAll(systemUsers, person, roles);
        if (person.Result == null)
            return $"Unable to find {personName}";

        var systemUser = systemUsers.Result.FirstOrDefault(s => string.Equals(s.InternalEmailAddress, person.Result.UserPrincipalName, StringComparison.OrdinalIgnoreCase));
        if (systemUser == null)
            return $"Unable to find {personName} in {EnvironmentInstance.FriendlyName}";

        if (string.IsNullOrWhiteSpace(businessUnit))
            businessUnit = EnvironmentInstance.UrlName;

        var role = roles.Result.FirstOrDefault(r => string.Equals(r.Name, roleName, StringComparison.OrdinalIgnoreCase) &&
                                             string.Equals(r.BusinessUnit.Name, businessUnit, StringComparison.OrdinalIgnoreCase));
        if (role == null)
            return $"Unable to find role {roleName} in {EnvironmentInstance.FriendlyName}";

        if (!ConfirmAction($"Do you want to grant '{role.BusinessUnit.Name}/{role.Name}' role to {systemUser.FullName}?"))
            return UserDeclinedAction;

        using var request = new HttpRequestMessage(HttpMethod.Post, BuildOrgQueryUri($"systemusers({systemUser.SystemUserId})/systemuserroles_association/$ref"));
        request.Content = new StringContent($"{{\"@odata.id\":\"{BuildOrgQueryUri($"roles({role.RoleId})")}\"}}", Encoding.UTF8, "application/json");
        var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return $"Granted '{role.BusinessUnit.Name}/{role.Name}' role to {systemUser.FullName}";
    }

    #endregion
}
