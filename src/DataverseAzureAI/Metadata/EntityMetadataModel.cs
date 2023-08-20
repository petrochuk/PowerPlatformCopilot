using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

// Classes used for serialization/deserialization
#pragma warning disable CA1056 // URI-like properties should not be strings
#pragma warning disable CA1819 // Properties should not return arrays
#pragma warning disable CA2227 // Collection properties should be read only

public enum AttributeType
{
    /// <summary>
    /// A Boolean attribute.
    /// </summary>
    Boolean = 0,
    /// <summary>
    /// An attribute that represents a customer.
    /// </summary>
    Customer = 1,
    /// <summary>
    /// A date/time attribute.
    /// </summary>
    DateTime = 2,
    /// <summary>
    /// A decimal attribute.
    /// </summary>
    Decimal = 3,
    /// <summary>
    /// A double attribute.
    /// </summary>
    Double = 4,
    /// <summary>
    /// An integer attribute.
    /// </summary>
    Integer = 5,
    /// <summary>
    /// A lookup attribute.
    /// </summary>
    Lookup = 6,
    /// <summary>
    /// A memo attribute.
    /// </summary>
    Memo = 7,
    /// <summary>
    /// A money attribute.
    /// </summary>
    Money = 8,
    /// <summary>
    /// An owner attribute.
    /// </summary>
    Owner = 9,
    /// <summary>
    /// A partylist attribute.
    /// </summary>
    PartyList = 10,
    /// <summary>
    /// A picklist attribute.
    /// </summary>
    Picklist = 11,
    /// <summary>
    /// A state attribute.
    /// </summary>
    State = 12,
    /// <summary>
    /// A status attribute.
    /// </summary>
    Status = 13,
    /// <summary>
    /// A string attribute.
    /// </summary>
    String = 14,
    /// <summary>
    /// An attribute that is an ID.
    /// </summary>
    Uniqueidentifier = 15,
    /// <summary>
    /// An attribute that contains calendar rules.
    /// </summary>
    CalendarRules = 16,
    /// <summary>
    /// An attribute that is created by the system at run time.
    /// </summary>
    Virtual = 17,
    /// <summary>
    /// A big integer attribute.
    /// </summary>
    BigInt = 18,
    /// <summary>
    /// A managed property attribute.
    /// </summary>
    ManagedProperty = 19,
    /// <summary>
    /// An entity name attribute.
    /// </summary>
    EntityName = 20
}

[DebuggerDisplay("{LogicalName}")]
public class EntityMetadataModel
{
    public static Dictionary<string, PropertyInfo> Properties { get; } =
        typeof(EntityMetadataModel).GetProperties().ToDictionary(p => p.Name, p => p, StringComparer.OrdinalIgnoreCase);

    [JsonIgnore]
    public string DisplayOrLogicalName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(DisplayName?.UserLocalizedLabel?.Label))
                return DisplayName.UserLocalizedLabel.Label;

            return LogicalName;
        }
    }

    public int ActivityTypeMask { get; set; }
    public bool AutoRouteToOwnerQueue { get; set; }
    public bool CanTriggerWorkflow { get; set; }
    public bool EntityHelpUrlEnabled { get; set; }
    public string? EntityHelpUrl { get; set; }
    public bool IsDocumentManagementEnabled { get; set; }
    public bool IsOneNoteIntegrationEnabled { get; set; }
    public bool IsInteractionCentricEnabled { get; set; }
    public bool IsKnowledgeManagementEnabled { get; set; }
    public bool IsSLAEnabled { get; set; }
    public bool IsBPFEntity { get; set; }
    public bool IsDocumentRecommendationsEnabled { get; set; }
    public bool IsMSTeamsIntegrationEnabled { get; set; }
    public string SettingOf { get; set; }
    public string DataProviderId { get; set; }
    public string DataSourceId { get; set; }
    public bool AutoCreateAccessTeams { get; set; }
    public bool IsActivity { get; set; }
    public bool IsActivityParty { get; set; }
    public bool IsRetrieveAuditEnabled { get; set; }
    public bool IsRetrieveMultipleAuditEnabled { get; set; }
    public bool IsArchivalEnabled { get; set; }
    public bool IsRetentionEnabled { get; set; }
    public bool IsAvailableOffline { get; set; }
    public bool IsChildEntity { get; set; }
    public bool IsAIRUpdated { get; set; }
    public object IconLargeName { get; set; }
    public string IconMediumName { get; set; }
    public string IconSmallName { get; set; }
    public string IconVectorName { get; set; }
    public bool IsCustomEntity { get; set; }
    public bool IsBusinessProcessEnabled { get; set; }
    public bool SyncToExternalSearchIndex { get; set; }
    public bool IsOptimisticConcurrencyEnabled { get; set; }
    public bool ChangeTrackingEnabled { get; set; }
    public bool IsImportable { get; set; }
    public bool IsIntersect { get; set; }
    public bool IsManaged { get; set; }
    public bool IsEnabledForCharts { get; set; }
    public bool IsEnabledForTrace { get; set; }
    public bool IsValidForAdvancedFind { get; set; }
    public int DaysSinceRecordLastModified { get; set; }
    public string MobileOfflineFilters { get; set; }
    public bool IsReadingPaneEnabled { get; set; }
    public bool IsQuickCreateEnabled { get; set; }
    public string LogicalName { get; set; }
    public int ObjectTypeCode { get; set; }
    public string OwnershipType { get; set; }
    public string PrimaryNameAttribute { get; set; }
    public string PrimaryImageAttribute { get; set; }
    public string PrimaryIdAttribute { get; set; }
    public string RecurrenceBaseEntityLogicalName { get; set; }
    public string ReportViewName { get; set; }
    public string SchemaName { get; set; }
    public string IntroducedVersion { get; set; }
    public bool IsStateModelAware { get; set; }
    public bool EnforceStateTransitions { get; set; }
    public string ExternalName { get; set; }
    public string EntityColor { get; set; }
    public string? LogicalCollectionName { get; set; }
    public string ExternalCollectionName { get; set; }
    public string CollectionSchemaName { get; set; }
    public string EntitySetName { get; set; }
    public bool IsEnabledForExternalChannels { get; set; }
    public bool IsPrivate { get; set; }
    public bool UsesBusinessDataLabelTable { get; set; }
    public bool IsLogicalEntity { get; set; }
    public bool HasNotes { get; set; }
    public bool HasActivities { get; set; }
    public bool HasFeedback { get; set; }
    public bool IsSolutionAware { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
    public bool HasEmailAddresses { get; set; }
    public object OwnerId { get; set; }
    public int OwnerIdType { get; set; }
    public object OwningBusinessUnit { get; set; }
    public string TableType { get; set; }
    [Key]
    public string MetadataId { get; set; }
    public object HasChanged { get; set; }
    public Label Description { get; set; }
    public Label DisplayCollectionName { get; set; }
    public Label DisplayName { get; set; }
    public BooleanValue IsAuditEnabled { get; set; }
    public BooleanValue IsValidForQueue { get; set; }
    public BooleanValue IsConnectionsEnabled { get; set; }
    public BooleanValue IsCustomizable { get; set; }
    public BooleanValue IsRenameable { get; set; }
    public BooleanValue IsMappable { get; set; }
    public BooleanValue IsDuplicateDetectionEnabled { get; set; }
    public BooleanValue CanCreateAttributes { get; set; }
    public BooleanValue CanCreateForms { get; set; }
    public BooleanValue CanCreateViews { get; set; }
    public BooleanValue CanCreateCharts { get; set; }
    public BooleanValue CanBeRelatedEntityInRelationship { get; set; }
    public BooleanValue CanBePrimaryEntityInRelationship { get; set; }
    public BooleanValue CanBeInManyToMany { get; set; }
    public BooleanValue CanBeInCustomEntityAssociation { get; set; }
    public BooleanValue CanEnableSyncToExternalSearchIndex { get; set; }
    public BooleanValue CanModifyAdditionalSettings { get; set; }
    public BooleanValue CanChangeHierarchicalRelationship { get; set; }
    public BooleanValue CanChangeTrackingBeEnabled { get; set; }
    public BooleanValue IsMailMergeEnabled { get; set; }
    public BooleanValue IsVisibleInMobile { get; set; }
    public BooleanValue IsVisibleInMobileClient { get; set; }
    public BooleanValue IsReadOnlyInMobileClient { get; set; }
    public BooleanValue IsOfflineInMobileClient { get; set; }
    public Privilege[] Privileges { get; set; }
    public object[] Settings { get; set; }
    public IList<AttributeMetadataModel> Attributes { get; set; }

    [JsonIgnore]
    public Dictionary<string, AttributeMetadataModel> AttributesDictionary { get; set; }
}

public class Label
{
    public LocalizedLabel[] LocalizedLabels { get; set; }
    public LocalizedLabel? UserLocalizedLabel { get; set; }

    public override string ToString()
    {
        if (UserLocalizedLabel != null)
            return UserLocalizedLabel.Label;

        if (LocalizedLabels.Length > 0)
            return LocalizedLabels[0].Label;

        return base.ToString();
    }
}

[DebuggerDisplay("{Label} - {LanguageCode}")]
public class LocalizedLabel
{
    public string? Label { get; set; }
    public int LanguageCode { get; set; }
    public bool IsManaged { get; set; }
    public string MetadataId { get; set; }
    public bool? HasChanged { get; set; }
}

[DebuggerDisplay("{Value}")]
public class BooleanValue
{
    public bool Value { get; set; }
    public bool CanBeChanged { get; set; }
    public required string ManagedPropertyLogicalName { get; set; }

    public override string ToString()
    {
        return Value.ToString();
    }
}

public class Privilege
{
    public bool CanBeBasic { get; set; }
    public bool CanBeDeep { get; set; }
    public bool CanBeGlobal { get; set; }
    public bool CanBeLocal { get; set; }
    public bool CanBeEntityReference { get; set; }
    public bool CanBeParentEntityReference { get; set; }
    public bool CanBeRecordFilter { get; set; }
    public string Name { get; set; }
    public string PrivilegeId { get; set; }
    public string PrivilegeType { get; set; }
}

[DebuggerDisplay("{LogicalName}")]
public class AttributeMetadataModel
{
    public string odatatype { get; set; }
    public string LogicalName { get; set; }
    public string MetadataId { get; set; }
    public Label DisplayName { get; set; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public AttributeType AttributeType { get; set; }
}
