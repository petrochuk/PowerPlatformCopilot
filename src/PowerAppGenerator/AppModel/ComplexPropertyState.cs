namespace PowerAppGenerator.AppModel;

public class ComplexPropertyState
{
    public string InvariantPropertyName { get; set; } = "";
    public bool AutoRuleBindingEnabled { get; set; } = false;
    public string AutoRuleBindingString { get; set; } = "";
    public string NameMapSourceSchema { get; set; } = "?";
    public bool IsLockable { get; set; } = false;
    public string AFDDataSourceName { get; set; } = "";
}
