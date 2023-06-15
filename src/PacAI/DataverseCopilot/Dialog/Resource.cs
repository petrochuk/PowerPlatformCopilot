using DataverseCopilot.Intent;
using Microsoft.Graph.Models;

namespace DataverseCopilot.Dialog;

public class Resource
{
    public static Resource Email { get; set; } = new Resource("Email", description: "Microsoft Office 365 Outlook")
    {
        IntentActionsCollection = new IntentActionsCollection(
            new []{
                new IntentAction(IntentAction.Iterate, new[] { nameof(IntentAction.Iterate), "Loop", "Cycle" }),
                new IntentAction(IntentAction.EmailReply, new[] { nameof(IntentAction.EmailReply), "Respond", "Answer" }),
                new IntentAction(IntentAction.EmailForward, new[] { nameof(IntentAction.EmailForward) }),
                new IntentAction(IntentAction.EmailDelete, new[] { nameof(IntentAction.EmailDelete), "Trash" })
            }
        )
    };

    public static Resource Calendar { get; set; } = new Resource("Calendar", description: "Microsoft Office 365 Outlook Calendar");
    public static Resource Contacts { get; set; } = new Resource("Contacts", description: "Microsoft Office 365 Outlook Contacts");
    public static Resource Tasks { get; set; } = new Resource("Tasks", description: "Microsoft Office 365 To-Do Tasks");
    public static Resource Dataverse { get; set; } = new Resource("Dataverse", description: "Microsoft Dataverse Environment");
    public static Resource Files { get; set; } = new Resource("Files", description: "File System");

    public Resource(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public string Name { get; set; }

    public string Description { get; set; }

    public IntentActionsCollection IntentActionsCollection { get; set; }
}
