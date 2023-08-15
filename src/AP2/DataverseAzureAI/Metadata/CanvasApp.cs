using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

[DebuggerDisplay("{Properties.DisplayName}")]
public class CanvasApp
{
    public string Name { get; set; }
    public string Id { get; set; }
    public string Type { get; set; }
    public Tags Tags { get; set; }
    public CanvasAppProperties Properties { get; set; }
    public string LogicalName { get; set; }
    public string AppLocation { get; set; }
    public bool IsAppComponentLibrary { get; set; }
    public string AppType { get; set; }
}

public class Tags
{
    public string PrimaryDeviceWidth { get; set; }
    public string PrimaryDeviceHeight { get; set; }
    public string SupportsPortrait { get; set; }
    public string SupportsLandscape { get; set; }
    public string PrimaryFormFactor { get; set; }
    public string ShowStatusBar { get; set; }
    public string PublisherVersion { get; set; }
    public string MinimumRequiredApiVersion { get; set; }
    public string HasComponent { get; set; }
    public string HasUnlockedComponent { get; set; }
    public string IsUnifiedRootApp { get; set; }
    public string SienaVersion { get; set; }
    public string DeviceCapabilities { get; set; }
}

public class CanvasAppProperties
{
    public static Dictionary<string, PropertyInfo> Properties
    { get; } = typeof(CanvasAppProperties).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    public DateTime AppVersion { get; set; }
    public DateTime LastDraftVersion { get; set; }
    public string LifeCycleId { get; set; }
    public string Status { get; set; }
    public AppVersion CreatedByClientVersion { get; set; }
    public AppVersion MinClientVersion { get; set; }
    public AppUser Owner { get; set; }
    public AppUser CreatedBy { get; set; }
    [JsonPropertyName("LastModifiedBy")]
    public AppUser ModifiedBy { get; set; }
    public AppUser LastPublishedBy { get; set; }
    public string BackgroundColor { get; set; }
    public string BackgroundImageUri { get; set; }
    public string TeamsColorIconUrl { get; set; }
    public string TeamsOutlineIconUrl { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string CommitMessage { get; set; }
    public string Publisher { get; set; }
    public Appuris AppUris { get; set; }
    [JsonPropertyName("CreatedTime")]
    public DateTime CreatedOn { get; set; }
    [JsonPropertyName("LastModifiedTime")]
    public DateTime ModifiedOn { get; set; }
    public DateTime LastPublishTime { get; set; }
    public int SharedGroupsCount { get; set; }
    public int SharedUsersCount { get; set; }
    public string appOpenProtocolUri { get; set; }
    public string appOpenUri { get; set; }
    public string appPlayUri { get; set; }
    public string appPlayEmbeddedUri { get; set; }
    public string appPlayTeamsUri { get; set; }
    public object[] authorizationReferences { get; set; }
    public Databasereferences databaseReferences { get; set; }
    public Permission[] permissions { get; set; }
    public Userappmetadata UserAppMetadata { get; set; }
    public bool isFeaturedApp { get; set; }
    public bool bypassConsent { get; set; }
    public bool isHeroApp { get; set; }
    public Environment environment { get; set; }
    public Apppackagedetails appPackageDetails { get; set; }
    public string almMode { get; set; }
    public bool performanceOptimizationEnabled { get; set; }
    public string unauthenticatedWebPackageHint { get; set; }
    public bool canConsumeAppPass { get; set; }
    public bool enableModernRuntimeMode { get; set; }
    public Executionrestrictions executionRestrictions { get; set; }
    public string appPlanClassification { get; set; }
    public bool usesPremiumApi { get; set; }
    public bool usesOnlyGrandfatheredPremiumApis { get; set; }
    public bool usesCustomApi { get; set; }
    public bool usesOnPremiseGateway { get; set; }
    public bool usesPcfExternalServiceUsage { get; set; }
    public bool isCustomizable { get; set; }
    public Appdocumentcomplexity appDocumentComplexity { get; set; }
}

public class AppVersion
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public int Build { get; set; }
    public int Revision { get; set; }
    public int MajorRevision { get; set; }
    public int MinorRevision { get; set; }
}

public class AppUser
{
    public string Id { get; set; }
    public string DisplayName { get; set; }
    public string Email { get; set; }
    public string Type { get; set; }
    public string TenantId { get; set; }
    public string UserPrincipalName { get; set; }

    public override string ToString()
    {
        return DisplayName;
    }
}

public class Appuris
{
    public Documenturi documentUri { get; set; }
    public object[] imageUris { get; set; }
    public object[] additionalUris { get; set; }
}

public class Documenturi
{
    public string value { get; set; }
    public string readonlyValue { get; set; }
}

public class Databasereferences
{
    public DefaultCds defaultcds { get; set; }
}

public class DefaultCds
{
    public Databasedetails databaseDetails { get; set; }
    public Datasources dataSources { get; set; }
    public string[] actions { get; set; }
}

public class Databasedetails
{
    public string referenceType { get; set; }
    public string environmentName { get; set; }
}

public class Datasources
{
    public Datasource Contacts { get; set; }
    public Datasource ReopenPhases { get; set; }
    public Datasource EmployeeSentiment { get; set; }
    public Datasource EmployeeAttestations { get; set; }
    public Datasource Facilities { get; set; }
    public Datasource FacilityGroups { get; set; }
    public Datasource SolutionSettings { get; set; }
    public Datasource EmployeeFacilitySearches { get; set; }
    public Datasource EmployeeBookings { get; set; }
    public Datasource Areas { get; set; }
    public Datasource Floors { get; set; }
    public Datasource Reopening { get; set; }
    public Datasource ProcessStages { get; set; }
    public Datasource DailyOccupancies { get; set; }
    public Datasource BulkDeleteFailures { get; set; }
    public Datasource EmployeeCases { get; set; }
    public Datasource GuestRegistrations { get; set; }
    public Datasource Users { get; set; }
    public Datasource ShareGuestRegistrations { get; set; }
    public Datasource AccessActions { get; set; }
    public Datasource Notifications { get; set; }
}

public class Datasource
{
    public string EntitySetName { get; set; }
    public string LogicalName { get; set; }
}

public class Userappmetadata
{
    public string favorite { get; set; }
    public bool includeInAppsList { get; set; }
}

public class Environment
{
    public string id { get; set; }
    public string name { get; set; }
}

public class Apppackagedetails
{
    public Playerpackage playerPackage { get; set; }
    public Webpackage webPackage { get; set; }
    public Unauthenticatedwebpackage unauthenticatedWebPackage { get; set; }
    public AppVersion DocumentServerVersion { get; set; }
    public string appPackageResourcesKind { get; set; }
    public string packagePropertiesJson { get; set; }
    public string id { get; set; }
}

public class Playerpackage
{
    public string value { get; set; }
    public string readonlyValue { get; set; }
}

public class Webpackage
{
    public string value { get; set; }
    public string readonlyValue { get; set; }
}

public class Unauthenticatedwebpackage
{
    public string value { get; set; }
}

public class Executionrestrictions
{
    public bool isTeamsOnly { get; set; }
    public Datalosspreventionevaluationresult dataLossPreventionEvaluationResult { get; set; }
}

public class Datalosspreventionevaluationresult
{
    public string status { get; set; }
    public DateTime lastEvaluationDate { get; set; }
    public Violation[] violations { get; set; }
    public Violationsbypolicy[] violationsByPolicy { get; set; }
    public string violationErrorMessage { get; set; }
    public Additionalinfo additionalInfo { get; set; }
}

public class Additionalinfo
{
    public string policyWikiUrl { get; set; }
}

public class Violation
{
    public string policyId { get; set; }
    public string policyDisplayName { get; set; }
    public string type { get; set; }
    public string[] parameters { get; set; }
}

public class Violationsbypolicy
{
    public Policyref policyRef { get; set; }
    public Violation1[] violations { get; set; }
}

public class Policyref
{
    public string policyId { get; set; }
    public string policyDisplayName { get; set; }
}

public class Violation1
{
    public string policyId { get; set; }
    public string policyDisplayName { get; set; }
    public string type { get; set; }
    public string[] parameters { get; set; }
}

public class Appdocumentcomplexity
{
    public int controlCount { get; set; }
    public int pcfControlCount { get; set; }
    public int uiComponentsCount { get; set; }
    public int totalRuleLengthOnStart { get; set; }
    public int dataSourceCount { get; set; }
    public int[] totalRuleLengthHistogram { get; set; }
    public bool blocksOnStart { get; set; }
    public int namedFormulasCount { get; set; }
    public bool startScreenUsed { get; set; }
}

public class Permission
{
    public string name { get; set; }
    public string id { get; set; }
    public string type { get; set; }
    public Properties1 properties { get; set; }
}

public class Properties1
{
    public string roleName { get; set; }
    public Principal principal { get; set; }
    public string scope { get; set; }
    public string notifyShareTargetOption { get; set; }
    public bool inviteGuestToTenant { get; set; }
    public DateTime createdOn { get; set; }
    public string createdBy { get; set; }
}

public class Principal
{
    public string id { get; set; }
    public string type { get; set; }
}

