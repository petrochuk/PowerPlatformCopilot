using DataverseCopilot.Extensions;
using Microsoft.VisualBasic;
using System.IO;
using System.Text.Json;

namespace DataverseCopilot.Intent;

public class IntentActionsCollection
{
    public IntentActionsCollection()
    {

    }

    public IntentActionsCollection(IReadOnlyList<IntentAction>? actions = null)
    {
        if (actions != null)
            Items.AddRange(actions);
    }

    public List<IntentAction> Items { get; set; } = new ();
}
