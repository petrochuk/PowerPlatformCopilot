using System.Diagnostics;
using System.Text.Json.Serialization;

namespace PowerAppGenerator.AppModel;

[DebuggerDisplay("{Title}")]
internal class Screen : ControlInfo
{
    [JsonIgnore]
    public string? Title { get; set; }

    [JsonIgnore]
    public string? Description { get; set; }

    public Screen(string name) : base(name)
    {
        StyleName = "defaultScreenStyle";

        Rules.Add(new Rule { Property = "Fill", InvariantScript = "Color.White" });
        Rules.Add(new Rule { Property = "ImagePosition", InvariantScript = "ImagePosition.Fit" });
        Rules.Add(new Rule { Property = "Height", InvariantScript = "Max(App.Height, App.MinScreenHeight)" });
        Rules.Add(new Rule { Property = "Width", InvariantScript = "Max(App.Width, App.MinScreenWidth)" });
        Rules.Add(new Rule { Property = "Size", InvariantScript = "1 + CountRows(App.SizeBreakpoints) - CountIf(App.SizeBreakpoints, Value >= Self.Width)" });
        Rules.Add(new Rule { Property = "Orientation", InvariantScript = "If(Self.Width < Self.Height, Layout.Vertical, Layout.Horizontal)" });
        Rules.Add(new Rule { Property = "LoadingSpinner", InvariantScript = "LoadingSpinner.None" });
        Rules.Add(new Rule { Property = "LoadingSpinnerColor", InvariantScript = "RGBA(56, 96, 178, 1)" });

        ControlPropertyState = new List<object> { "Fill", "ImagePosition", "Height", "Width", "Size", "Orientation", "LoadingSpinner", "LoadingSpinnerColor" };
    }
}
