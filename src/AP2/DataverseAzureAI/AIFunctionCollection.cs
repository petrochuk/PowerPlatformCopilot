using Azure.AI.OpenAI;
using System.ComponentModel;
using System.Reflection;
using System.Text;

namespace AP2.DataverseAzureAI;

internal class AIFunctionCollection
{
    private readonly Dictionary<string, AIFunction> _functionsMap = new (StringComparer.OrdinalIgnoreCase);
    private readonly IList<FunctionDefinition> _functionDefinitions = new List<FunctionDefinition> ();
    private const int MaxAzureAIFunctionCount = 64;

    public AIFunctionCollection(Type hostType)
    {
        _ = hostType ?? throw new ArgumentNullException(nameof(hostType));

        foreach (var method in hostType.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            var descriptionAttribute = method.GetCustomAttribute<DescriptionAttribute>();
            if (descriptionAttribute == null)
                continue;

            var functionDefinition = new FunctionDefinition
            {
                Description = descriptionAttribute.Description,
                Name = method.Name
            };

            var parameters = new StringBuilder("{ \"type\": \"object\", \"properties\": {");
            var firstParameter = true;
            foreach (var parameter in method.GetParameters())
            {
                var parameterDescriptionAttribute = parameter.GetCustomAttribute<DescriptionAttribute>();
                if (parameterDescriptionAttribute == null)
                    continue;

                if (!firstParameter)
                    parameters.Append(", ");
                else
                    firstParameter = false;

                parameters.Append($"\"{parameter.Name}\": {{ \"type\": \"{parameter.ParameterType.ToJsonType()}\",\"description\":\"{parameterDescriptionAttribute.Description}\"}}");
            }
            parameters.Append($"}} }}");

            functionDefinition.Parameters = BinaryData.FromString(parameters.ToString());

            Add(new AIFunction() {
                Name = method.Name,
                Description = descriptionAttribute.Description,
                MethodInfo = method,
                FunctionDefinition = functionDefinition
            });

            if (_functionsMap.Count >= MaxAzureAIFunctionCount)
                throw new InvalidOperationException($"The maximum number of Azure AI functions is {MaxAzureAIFunctionCount}. Please reduce the number of functions.");
        }
    }

    public void Add(AIFunction function)
    {
        _ = function ?? throw new ArgumentNullException(nameof(function));

        _functionsMap.Add(function.Name, function);
        _functionDefinitions.Add(function.FunctionDefinition);
    }

    public IReadOnlyDictionary<string, AIFunction> Functions
    {
        get => _functionsMap;
    }

    public IReadOnlyList<FunctionDefinition> Definitions
    {
        get => _functionDefinitions.AsReadOnly();
    }
}
