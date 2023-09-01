using AP2.DataverseAzureAI.OData;
using Microsoft.Graph.Models;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

public class Environment
{
    public string Id { get; set; }
    public required string Name { get; set; }
    public string Type { get; set; }
    public EnvironmentProperties Properties { get; set; }
    public string Location { get; set; }

    public Lazy<Task<IList<Solution>>> Solutions { get; private set; }
    public Lazy<Task<IList<CanvasApp>>> CanvasApps { get; private set; }
    public Lazy<Task<IList<AppModule>>> AppModules { get; private set; }
    public Lazy<Task<IList<SystemUser>>> SystemUsers { get; private set; }
    public Lazy<Task<IList<Role>>> Roles { get; private set; }

    [JsonIgnore]
    public IHttpClientFactory HttpClientFactory { get; set; }

    [JsonIgnore]
    public User User { get; set; }

    private string? _environmentApiPrefix;
    [JsonIgnore]
    public string EnvironmentApiPrefix 
    { 
        get
        {
            if (_environmentApiPrefix != null)
                return _environmentApiPrefix;

            _environmentApiPrefix = Name.Replace("-", "");
            _environmentApiPrefix = EnvironmentApiPrefix.Insert(EnvironmentApiPrefix.Length - 2, ".");
            return _environmentApiPrefix;
        }
    }

    public Environment()
    {
        RefreshSolutions();

        CanvasApps = new Lazy<Task<IList<CanvasApp>>>(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildEnvironmentApiQueryUri($"powerapps/apps?%24expand=permissions%28%24filter%3DmaxAssignedTo%28%27{User!.Id}%27%29%29&%24filter=classification+eq+%27SharedWithMeApps%27+and+environment+eq+%27{Name}%27&api-version=1"));
            var httpClient = HttpClientFactory!.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var appModules = JsonSerializer.Deserialize<ODataContext<CanvasApp>>(contentStream, DataverseAIClient.JsonSerializerOptions);
            if (appModules == null)
                throw new InvalidOperationException("Failed to get list of PowerApps.");
            return appModules.Values;
        });

        AppModules = new Lazy<Task<IList<AppModule>>>(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"appmodules/Microsoft.Dynamics.CRM.RetrieveUnpublishedMultiple()"));
            var httpClient = HttpClientFactory!.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var appModules = JsonSerializer.Deserialize<ODataContext<AppModule>>(contentStream, DataverseAIClient.JsonSerializerOptions);
            if (appModules == null)
                throw new InvalidOperationException("Failed to get list of PowerApps.");
            return appModules.Values;
        });

        SystemUsers = new Lazy<Task<IList<SystemUser>>>(async () =>
        {
            var systemUsers = new List<SystemUser>();
            var queryUri = BuildOrgQueryUri($"systemusers");

            do
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, queryUri);
                var httpClient = HttpClientFactory!.CreateClient(nameof(DataverseAIClient));
                var response = await httpClient.SendAsync(request).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                var systemUsersResponse = JsonSerializer.Deserialize<ODataContext<SystemUser>>(contentStream, DataverseAIClient.JsonSerializerOptions);
                if (systemUsersResponse == null)
                    throw new InvalidOperationException("Failed to get list system users.");

                if (!string.IsNullOrWhiteSpace(systemUsersResponse.NextLink))
                    queryUri = new Uri(systemUsersResponse.NextLink);
                else
                    queryUri = null;

                systemUsers.AddRange(systemUsersResponse.Values);
            }
            while (queryUri != null);

            return systemUsers;
        });

        Roles = new Lazy<Task<IList<Role>>>(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"roles?$expand=businessunitid"));
            var httpClient = HttpClientFactory.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var roles = JsonSerializer.Deserialize<ODataContext<Role>>(contentStream, DataverseAIClient.JsonSerializerOptions);
            if (roles == null)
                throw new InvalidOperationException("Failed to get list roles.");
            return roles.Values;
        });
    }

    public async Task<SystemUser?> GetSystemUser(string internalEmailAddress)
    {
        if(!SystemUsers.IsValueCreated)
        {
            SystemUsers = new Lazy<Task<IList<SystemUser>>>(() =>
            {
                return Task.FromResult<IList<SystemUser>>(new List<SystemUser>());
            });
        }
        var systemUsers = await SystemUsers.Value.ConfigureAwait(false);
        var systemUser = systemUsers.FirstOrDefault(s => string.Equals(s.InternalEmailAddress, internalEmailAddress, StringComparison.OrdinalIgnoreCase));
        if (systemUser != null)
            return systemUser;

        var queryUri = BuildOrgQueryUri($"systemusers?$filter=windowsliveid eq '{internalEmailAddress}'");

        using var request = new HttpRequestMessage(HttpMethod.Get, queryUri);
        var httpClient = HttpClientFactory!.CreateClient(nameof(DataverseAIClient));
        var response = await httpClient.SendAsync(request).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        var systemUsersResponse = JsonSerializer.Deserialize<ODataContext<SystemUser>>(contentStream, DataverseAIClient.JsonSerializerOptions);
        if (systemUsersResponse == null)
            throw new InvalidOperationException("Failed to get list system users.");
        systemUser = systemUsersResponse.Values.FirstOrDefault();
        if (systemUser == null)
            return null;

        // Cache it
        systemUsers.Add(systemUser);

        return systemUser;
    }

    public void RefreshSolutions()
    {
        Solutions = new Lazy<Task<IList<Solution>>>(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, BuildOrgQueryUri($"solutions?$expand=createdby,modifiedby,publisherid&$filter=isvisible eq true"));
            var httpClient = HttpClientFactory!.CreateClient(nameof(DataverseAIClient));
            var response = await httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var contentStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);

            var solutions = JsonSerializer.Deserialize<ODataContext<Solution>>(contentStream, DataverseAIClient.JsonSerializerOptions);
            if (solutions == null)
                throw new InvalidOperationException("Failed to get list of solutions.");
            return solutions.Values;
        });
    }

    [DebuggerStepThrough]
    private Uri BuildEnvironmentApiQueryUri(string query)
    {
        var baseUri = new Uri($"https://{EnvironmentApiPrefix}.environment.api.powerplatform.com");
        return new Uri(baseUri, query);
    }

    [DebuggerStepThrough]
    private Uri BuildOrgQueryUri(string query)
    {
        return new Uri(Properties.LinkedEnvironmentMetadata.InstanceApiUrl, $"api/data/v9.2/{query}");
    }

    override public string ToString()
    {
        return Properties.DisplayName;
    }
}

public class EnvironmentProperties
{
    public string azureRegionHint { get; set; }
    public string DisplayName { get; set; }
    public string description { get; set; }
    public DateTime createdTime { get; set; }
    public Createdby createdBy { get; set; }
    public DateTime lastModifiedTime { get; set; }
    public string provisioningState { get; set; }
    public string creationType { get; set; }
    public string environmentSku { get; set; }
    public bool isDefault { get; set; }
    public Permissions permissions { get; set; }
    public Clienturis clientUris { get; set; }
    public Runtimeendpoints runtimeEndpoints { get; set; }
    public string databaseType { get; set; }
    public LinkedEnvironmentMetadata LinkedEnvironmentMetadata { get; set; }
    public string retentionPeriod { get; set; }
    public string lifecycleAuthority { get; set; }
    public States states { get; set; }
    public Updatecadence updateCadence { get; set; }
    public object[] connectedGroups { get; set; }
    public Protectionstatus protectionStatus { get; set; }
    public string trialScenarioType { get; set; }
    public Cluster cluster { get; set; }
    public Governanceconfiguration governanceConfiguration { get; set; }
    public Notificationmetadata notificationMetadata { get; set; }
    public string perEnvironmentSharedApiPrimaryRuntimeUrl { get; set; }
    public string perEnvironmentCustomApiPrimaryRuntimeUrl { get; set; }
    public Lastmodifiedby lastModifiedBy { get; set; }
    public Provisioningdetails provisioningDetails { get; set; }
}

public class Createdby
{
    public string id { get; set; }
    public string displayName { get; set; }
    public string email { get; set; }
    public string type { get; set; }
    public string tenantId { get; set; }
    public string userPrincipalName { get; set; }
}

public class Permissions
{
    public Createpowerapp CreatePowerApp { get; set; }
    public Readenvironment ReadEnvironment { get; set; }
    public Generateresourcestorage GenerateResourceStorage { get; set; }
    public Creategateway CreateGateway { get; set; }
    public Createflow CreateFlow { get; set; }
    public Createcustomapi CreateCustomApi { get; set; }
    public Exportenvironmentpackage ExportEnvironmentPackage { get; set; }
    public Importenvironmentpackage ImportEnvironmentPackage { get; set; }
    public Createfunction CreateFunction { get; set; }
    public Createsharepointcustomformcanvasapp CreateSharePointCustomFormCanvasApp { get; set; }
    public Createdatabase CreateDatabase { get; set; }
    public Adminreadenvironment AdminReadEnvironment { get; set; }
    public Updateenvironment UpdateEnvironment { get; set; }
    public Deleteenvironment DeleteEnvironment { get; set; }
    public Setdlppolicy SetDLPPolicy { get; set; }
    public Listanypowerapp ListAnyPowerApp { get; set; }
    public Listanyflow ListAnyFlow { get; set; }
    public Deleteanypowerapp DeleteAnyPowerApp { get; set; }
    public Deleteanyflow DeleteAnyFlow { get; set; }
    public Addenvironmentroleassignment AddEnvironmentRoleAssignment { get; set; }
    public Readenvironmentroleinformation ReadEnvironmentRoleInformation { get; set; }
    public Removeenvironmentroleassignment RemoveEnvironmentRoleAssignment { get; set; }
    public Modifydatabaseroleassignments ModifyDatabaseRoleAssignments { get; set; }
    public Modifydatabaseroledefinitions ModifyDatabaseRoleDefinitions { get; set; }
    public Listdatabaseentities ListDatabaseEntities { get; set; }
    public Createdatabaseentities CreateDatabaseEntities { get; set; }
    public Updatedatabaseentities UpdateDatabaseEntities { get; set; }
    public Deletedatabaseentities DeleteDatabaseEntities { get; set; }
    public Manageanypowerapp ManageAnyPowerApp { get; set; }
    public Manageanyconnection ManageAnyConnection { get; set; }
    public Managetalentenvironmentsettings ManageTalentEnvironmentSettings { get; set; }
    public Managedatabaseusers ManageDatabaseUsers { get; set; }
    public Manageanycustomapi ManageAnyCustomApi { get; set; }
    public Listanyfunction ListAnyFunction { get; set; }
    public Deleteanyfunction DeleteAnyFunction { get; set; }
    public Readconsumption ReadConsumption { get; set; }
    public Move Move { get; set; }
    public Copyfrom CopyFrom { get; set; }
    public Copyto CopyTo { get; set; }
    public Restoreto RestoreTo { get; set; }
    public Restorefrom RestoreFrom { get; set; }
    public Manageprotectionkeys ManageProtectionKeys { get; set; }
    public Reset Reset { get; set; }
    public Enable Enable { get; set; }
    public Disable Disable { get; set; }
}

public class Createpowerapp
{
    public string displayName { get; set; }
}

public class Readenvironment
{
    public string displayName { get; set; }
}

public class Generateresourcestorage
{
    public string displayName { get; set; }
}

public class Creategateway
{
    public string displayName { get; set; }
}

public class Createflow
{
    public string displayName { get; set; }
}

public class Createcustomapi
{
    public string displayName { get; set; }
}

public class Exportenvironmentpackage
{
    public string displayName { get; set; }
}

public class Importenvironmentpackage
{
    public string displayName { get; set; }
}

public class Createfunction
{
    public string displayName { get; set; }
}

public class Createsharepointcustomformcanvasapp
{
    public string displayName { get; set; }
}

public class Createdatabase
{
    public string displayName { get; set; }
}

public class Adminreadenvironment
{
    public string displayName { get; set; }
}

public class Updateenvironment
{
    public string displayName { get; set; }
}

public class Deleteenvironment
{
    public string displayName { get; set; }
}

public class Setdlppolicy
{
    public string displayName { get; set; }
}

public class Listanypowerapp
{
    public string displayName { get; set; }
}

public class Listanyflow
{
    public string displayName { get; set; }
}

public class Deleteanypowerapp
{
    public string displayName { get; set; }
}

public class Deleteanyflow
{
    public string displayName { get; set; }
}

public class Addenvironmentroleassignment
{
    public string displayName { get; set; }
}

public class Readenvironmentroleinformation
{
    public string displayName { get; set; }
}

public class Removeenvironmentroleassignment
{
    public string displayName { get; set; }
}

public class Modifydatabaseroleassignments
{
    public string displayName { get; set; }
}

public class Modifydatabaseroledefinitions
{
    public string displayName { get; set; }
}

public class Listdatabaseentities
{
    public string displayName { get; set; }
}

public class Createdatabaseentities
{
    public string displayName { get; set; }
}

public class Updatedatabaseentities
{
    public string displayName { get; set; }
}

public class Deletedatabaseentities
{
    public string displayName { get; set; }
}

public class Manageanypowerapp
{
    public string displayName { get; set; }
}

public class Manageanyconnection
{
    public string displayName { get; set; }
}

public class Managetalentenvironmentsettings
{
    public string displayName { get; set; }
}

public class Managedatabaseusers
{
    public string displayName { get; set; }
}

public class Manageanycustomapi
{
    public string displayName { get; set; }
}

public class Listanyfunction
{
    public string displayName { get; set; }
}

public class Deleteanyfunction
{
    public string displayName { get; set; }
}

public class Readconsumption
{
    public string displayName { get; set; }
}

public class Move
{
    public string displayName { get; set; }
}

public class Copyfrom
{
    public string displayName { get; set; }
}

public class Copyto
{
    public string displayName { get; set; }
}

public class Restoreto
{
    public string displayName { get; set; }
}

public class Restorefrom
{
    public string displayName { get; set; }
}

public class Manageprotectionkeys
{
    public string displayName { get; set; }
}

public class Reset
{
    public string displayName { get; set; }
}

public class Enable
{
    public string displayName { get; set; }
}

public class Disable
{
    public string displayName { get; set; }
}

public class Clienturis
{
    public string admin { get; set; }
    public string maker { get; set; }
}

public class Runtimeendpoints
{
    public string microsoftBusinessAppPlatform { get; set; }
    public string microsoftCommonDataModel { get; set; }
    public string microsoftPowerApps { get; set; }
    public string microsoftPowerAppsAdvisor { get; set; }
    public string microsoftPowerVirtualAgents { get; set; }
    public string microsoftApiManagement { get; set; }
    public string microsoftFlow { get; set; }
}

public class LinkedEnvironmentMetadata
{
    public string resourceId { get; set; }
    public string friendlyName { get; set; }
    public string uniqueName { get; set; }
    public string domainName { get; set; }
    public string version { get; set; }
    public Uri InstanceUrl { get; set; }
    public Uri InstanceApiUrl { get; set; }
    public int baseLanguage { get; set; }
    public string instanceState { get; set; }
    public DateTime createdTime { get; set; }
    public string platformSku { get; set; }
    public string securityGroupId { get; set; }
}

public class States
{
    public Management management { get; set; }
    public Runtime runtime { get; set; }
}

public class Management
{
    public string id { get; set; }
}

public class Runtime
{
    public string runtimeReasonCode { get; set; }
    public Requestedby requestedBy { get; set; }
    public string id { get; set; }
}

public class Requestedby
{
    public string displayName { get; set; }
    public string type { get; set; }
}

public class Updatecadence
{
    public string id { get; set; }
}

public class Protectionstatus
{
    public string keyManagedBy { get; set; }
}

public class Cluster
{
    public string category { get; set; }
    public string number { get; set; }
    public string uriSuffix { get; set; }
    public string geoShortName { get; set; }
    public string environment { get; set; }
}

public class Governanceconfiguration
{
    public string protectionLevel { get; set; }
    public Settings settings { get; set; }
}

public class Settings
{
    public Extendedsettings extendedSettings { get; set; }
}

public class Extendedsettings
{
    public string isGroupSharingDisabled { get; set; }
    public string maxLimitUserSharing { get; set; }
    public string makerOnboardingUrl { get; set; }
    public string makerOnboardingTimestamp { get; set; }
    public string makerOnboardingMarkdown { get; set; }
}

public class Notificationmetadata
{
    public string state { get; set; }
    public string branding { get; set; }
}

public class Lastmodifiedby
{
    public string id { get; set; }
    public string displayName { get; set; }
    public string email { get; set; }
    public string type { get; set; }
    public string tenantId { get; set; }
    public string userPrincipalName { get; set; }
}

public class Provisioningdetails
{
    public string message { get; set; }
    public Operation[] operations { get; set; }
}

public class Operation
{
    public string name { get; set; }
    public string httpStatus { get; set; }
    public string code { get; set; }
}
