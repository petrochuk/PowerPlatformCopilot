using System.ComponentModel;
using System.Reflection;
using AP2.DataverseAzureAI.Extensions;
using AP2.DataverseAzureAI.Metadata;

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
            if (propertyInfo.Equals(entityMetadataModel, propertyValueFilter))
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
            return Task.FromResult(property.GetValue(entityMetadataModel, FullName));
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
            if (propertyInfo.Equals(appModule, propertyValueFilter))
                result.Add(appModule.Name);
        }

        if (result.Count == 0)
            return PowerAppsNotFound;

        return string.Join(", ", result);
    }

    [Description("Returns filtered list of canvas PowerApps based on specified property value")]
    public async Task<string> ListOfCanvasAppsByPropertyValue(
        [Description("Property name or empty to return all canvas PowerApps")]
        string propertyName,
        [Description("Filter by property value")]
        string propertyValueFilter)
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

        var result = new List<string>();
        foreach (var canvasApp in canvasApps)
        {
            if (propertyInfo.Equals(canvasApp, propertyValueFilter))
                result.Add(canvasApp.Properties.DisplayName);
        }

        if (result.Count == 0)
            return PowerAppsNotFound;

        return string.Join(", ", result);
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
                return propertyInfo.GetValue(canvasApp, FullName);
        }

        return $"Canvas app '{canvasAppName}' was not found";
    }

    #endregion

    #region Assistant skill functions for Dataverse Solutions

    [Description("Returns filtered list of Dataverse solutions based on specified property value and optional user")]
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
            if (propertyInfo.Equals(solution, propertyValueFilter))
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
                        if (userPropertyInfo == null || !userPropertyInfo.Equals(solution, FullName))
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

    [Description("Returns property value for specified Dataverse solution")]
    public async Task<string> GetSolutionPropertyValue(
        [Description("Dataverse solution friendly, unique name or id")]
        string solutionName,
        [Description("Property name")]
        string propertyName)
    {
        if (string.IsNullOrWhiteSpace(solutionName))
            return "Solution name is required";
        if (!Solution.Properties.TryGetValue(propertyName, out var propertyInfo))
            return PropertyNotFound;
        solutionName = solutionName.Trim();

        var solutions = await _solutions.Value.ConfigureAwait(false);
        foreach (var solution in solutions)
        {
            if (string.Compare(solution.FriendlyName, solutionName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(solution.UniqueName, solutionName, StringComparison.OrdinalIgnoreCase) == 0 ||
                string.Compare(solution.SolutionId.ToString(), solutionName, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return propertyInfo.GetValue(solution, FullName);
            }
        }

        return $"Not found any solutions matching '{solutionName}' filter";
    }

    #endregion
}
