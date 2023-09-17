namespace AP2.DataverseAzureAI.Metadata;

public class RolePrivilege
{
    public string Depth { get; set; }
    public string PrivilegeId { get; set; }
    public string BusinessUnitId { get; set; }
    public string PrivilegeName { get; set; }
    public string RecordFilterId { get; set; }
    public string RecordFilterUniqueName { get; set; }

    override public string ToString()
    {
        return $"{PrivilegeName}";
    }
}

public class RetrieveRolePrivilegesRoleResponse
{
    public List<RolePrivilege> RolePrivileges { get; set; }
}

public enum PrivilegeAction
{
    Create,
    Read,
    Write,
    Delete,
    Append,
    AppendTo,
    Assign,
    Share
}