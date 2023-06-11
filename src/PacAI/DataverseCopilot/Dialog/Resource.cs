using System.ComponentModel;

namespace DataverseCopilot.Dialog;

public enum Resource
{
    Email,
    Calendar,
    Contacts,
    Tasks,
    Files,
    [Description("Dataverse Environment")]
    Dataverse
}
