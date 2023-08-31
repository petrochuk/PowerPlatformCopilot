using System.Diagnostics;
using System.Text.Json.Serialization;

namespace AP2.DataverseAzureAI.Metadata;

[DebuggerDisplay("{Name}")]
public class Role
{
    public string solutionid { get; set; }
    public string RoleIdUnique { get; set; }
    public DateTime? modifiedon { get; set; }
    public string _parentrootroleid_value { get; set; }
    public string _parentroleid_value { get; set; }
    public Guid RoleId { get; set; }
    public object overriddencreatedon { get; set; }
    public bool ismanaged { get; set; }
    public object importsequencenumber { get; set; }
    public string _businessunitid_value { get; set; }
    public object _modifiedonbehalfby_value { get; set; }
    public string organizationid { get; set; }
    public int componentstate { get; set; }
    public int isinherited { get; set; }
    public string _roletemplateid_value { get; set; }
    public string Name { get; set; }
    public long versionnumber { get; set; }
    public object _createdonbehalfby_value { get; set; }
    public string _modifiedby_value { get; set; }
    public DateTime createdon { get; set; }
    public DateTime overwritetime { get; set; }
    public string _createdby_value { get; set; }
    public BooleanValue CanBeDeleted { get; set; }
    public BooleanValue IsCustomizable { get; set; }
    [JsonPropertyName("businessunitid")]
    public BusinessUnit BusinessUnit { get; set; }
}
