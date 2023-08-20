using System.Text.Json.Serialization;

namespace PowerAppGenerator.AppModel
{
    public class Label : ControlInfo
    {
        public Label(string name) : base(name)
        {
            Template = new Template
            {
                Id = "http://microsoft.com/appmagic/label",
                Version = "2.5.1",
                LastModifiedTimestamp = "0",
                Name = "label",
                FirstParty = true,
                IsPremiumPcfControl = false,
                IsCustomGroupControlTemplate = false,
                CustomGroupControlTemplateName = "",
                IsComponentDefinition = false,
                OverridableProperties = new OverridableProperties()
            };

            HasDynamicProperties = true;
            DynamicProperties = new()
            {
                new DynamicProperty()
                {
                    PropertyName = "FillPortions",
                    ControlPropertyState = "FillPortions",
                    Rule = new Rule()
                    {
                        Property = "FillPortions",
                        InvariantScript = "0"
                    }
                },
                new DynamicProperty()
                {
                    PropertyName = "AlignInContainer",
                    ControlPropertyState = "AlignInContainer",
                    Rule = new Rule()
                    {
                        Property = "AlignInContainer",
                        InvariantScript = "AlignInContainer.Stretch"
                    }
                },
                new DynamicProperty()
                {
                    PropertyName = "LayoutMinWidth",
                    ControlPropertyState = "LayoutMinWidth",
                    Rule = new Rule()
                    {
                        Property = "LayoutMinWidth",
                        InvariantScript = "150"
                    }
                },
                new DynamicProperty()
                {
                    PropertyName = "LayoutMinHeight",
                    ControlPropertyState = "LayoutMinHeight",
                    Rule = new Rule()
                    {
                        Property = "LayoutMinHeight",
                        InvariantScript = "50"
                    }
                }
            };

            StyleName = "defaultLabelStyle";

            AddRulesAndPropState();
        }

        [JsonIgnore]
        public Rule Text { get; private set; }

        [JsonIgnore]
        public Rule Align { get; private set; }

        private void AddRulesAndPropState()
        {
            Rules.Add(new Rule { Property = "Live", Category = "Data", InvariantScript = "Live.Off" });
            Rules.Add(Text = new Rule { Property = "Text", Category = "Data", InvariantScript = "" });
            Rules.Add(new Rule { Property = "Role", Category = "Data", InvariantScript = "TextRole.Default" });
            Rules.Add(new Rule { Property = "Overflow", InvariantScript = "Overflow.Hidden" });
            Rules.Add(Color = new Rule { Property = "Color", InvariantScript = "RGBA(0, 0, 0, 1)" });
            Rules.Add(new Rule { Property = "DisabledColor", InvariantScript = "RGBA(166, 166, 166, 1)" });
            Rules.Add(new Rule { Property = "PressedColor", InvariantScript = "Self.Color" });
            Rules.Add(new Rule { Property = "HoverColor", InvariantScript = "Self.Color" });
            Rules.Add(new Rule { Property = "BorderColor", InvariantScript = "RGBA(0, 18, 107, 1)" });
            Rules.Add(new Rule { Property = "DisabledBorderColor", InvariantScript = "RGBA(56, 56, 56, 1)" });
            Rules.Add(new Rule { Property = "PressedBorderColor", InvariantScript = "Self.BorderColor" });
            Rules.Add(new Rule { Property = "HoverBorderColor", InvariantScript = "Self.BorderColor" });
            Rules.Add(new Rule { Property = "BorderStyle", InvariantScript = "BorderStyle.Solid" });
            Rules.Add(new Rule { Property = "FocusedBorderColor", InvariantScript = "Self.BorderColor" });
            Rules.Add(Fill = new Rule { Property = "Fill", InvariantScript = "RGBA(0, 0, 0, 0)" });
            Rules.Add(new Rule { Property = "DisabledFill", InvariantScript = "RGBA(0, 0, 0, 0)" });
            Rules.Add(new Rule { Property = "PressedFill", InvariantScript = "Self.Fill" });
            Rules.Add(new Rule { Property = "HoverFill", InvariantScript = "Self.Fill" });
            Rules.Add(new Rule { Property = "Font", InvariantScript = "Font.'Open Sans'" });
            Rules.Add(new Rule { Property = "FontWeight", InvariantScript = "FontWeight.Normal" });
            Rules.Add(Align = new Rule { Property = "Align", InvariantScript = "Align.Left" });
            Rules.Add(new Rule { Property = "VerticalAlign", InvariantScript = "VerticalAlign.Middle" });
            Rules.Add(new Rule { Property = "X", InvariantScript = "0" });
            Rules.Add(new Rule { Property = "Y", InvariantScript = "0" });
            Rules.Add(new Rule { Property = "Width", InvariantScript = "Parent.Width" });
            Rules.Add(new Rule { Property = "Height", InvariantScript = "40" });
            Rules.Add(new Rule { Property = "DisplayMode", InvariantScript = "DisplayMode.Edit" });
            Rules.Add(ZIndex = new Rule { Property = "ZIndex", InvariantScript = "1" });
            Rules.Add(new Rule { Property = "LineHeight", InvariantScript = "1.2" });
            Rules.Add(new Rule { Property = "BorderThickness", InvariantScript = "0" });
            Rules.Add(new Rule { Property = "FocusedBorderThickness", InvariantScript = "0" });
            Rules.Add(new Rule { Property = "Size", InvariantScript = "13" });
            Rules.Add(new Rule { Property = "Italic", InvariantScript = "false" });
            Rules.Add(new Rule { Property = "Underline", InvariantScript = "false" });
            Rules.Add(new Rule { Property = "Strikethrough", InvariantScript = "false" });
            Rules.Add(new Rule { Property = "PaddingTop", InvariantScript = "5" });
            Rules.Add(new Rule { Property = "PaddingRight", InvariantScript = "5" });
            Rules.Add(new Rule { Property = "PaddingBottom", InvariantScript = "5" });
            Rules.Add(new Rule { Property = "PaddingLeft", InvariantScript = "5" });

            ControlPropertyState = new List<object> { "Live", "Overflow", "Text", "Role", "Color", 
                "DisabledColor", "PressedColor", "HoverColor", "BorderColor", "DisabledBorderColor", "PressedBorderColor", "HoverBorderColor", "BorderStyle", "FocusedBorderColor", 
                "Fill", "DisabledFill", "PressedFill", "HoverFill", "Font", "FontWeight", "Align", "VerticalAlign", 
                "X", "Y", "Width", "Height", "DisplayMode", "ZIndex", "LineHeight", "BorderThickness", "FocusedBorderThickness", 
                "Size", "Italic", "Underline", "Strikethrough", "PaddingTop", "PaddingRight", "PaddingBottom", "PaddingLeft" };
        }
    }
}
