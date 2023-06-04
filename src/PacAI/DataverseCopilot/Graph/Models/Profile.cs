namespace DataverseCopilot.Graph.Models;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

public class Profile
{
    public string[] businessPhones { get; set; }
    public string DisplayName { get; set; }
    public string givenName { get; set; }
    public string jobTitle { get; set; }
    public string mail { get; set; }
    public string mobilePhone { get; set; }
    public string officeLocation { get; set; }
    public object preferredLanguage { get; set; }
    public string surname { get; set; }
    public string userPrincipalName { get; set; }
    public string id { get; set; }
}
