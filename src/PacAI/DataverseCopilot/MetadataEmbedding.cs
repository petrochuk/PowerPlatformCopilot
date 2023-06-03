using bolt.dataverse.model;
using System.Text;
using System.Text.Json.Serialization;

namespace DataverseCopilot;

[DebuggerDisplay("{EntityMetadataModel.LogicalName}")]
internal class MetadataEmbedding
{
    public MetadataEmbedding(EntityMetadataModel entityMetadataModel)
    {
        EntityMetadataModel = entityMetadataModel;
    }

    public IReadOnlyList<float>? Vector { get; set; }

    public EntityMetadataModel EntityMetadataModel { get; init; }

    [JsonIgnore]
    public string Prompt
    {
        get
        {
            var prompt = new StringBuilder();

            prompt.Append("Table/Entity name:");
            AppendWithSpace(prompt, EntityMetadataModel.LogicalName);
            AppendWithSpace(prompt, EntityMetadataModel.LogicalCollectionName);
            if (EntityMetadataModel.DisplayName.UserLocalizedLabel != null)
                AppendWithSpace(prompt, EntityMetadataModel.DisplayName.UserLocalizedLabel.Label, addQuotes: true);
            if (EntityMetadataModel.DisplayCollectionName.UserLocalizedLabel != null)
                AppendWithSpace(prompt, EntityMetadataModel.DisplayCollectionName.UserLocalizedLabel.Label, addQuotes: true);

            prompt.AppendLine();
            prompt.Append("with columns/attributes:");
            foreach (var attribute in EntityMetadataModel.Attributes)
            {
                AppendWithSpace(prompt, attribute.LogicalName);
                if (attribute.DisplayName.UserLocalizedLabel != null)
                    AppendWithSpace(prompt, attribute.DisplayName.UserLocalizedLabel.Label, addQuotes: true);
            }

            return prompt.ToString();
        }
    }

    private void AppendWithSpace(StringBuilder prompt, string value, bool addQuotes = false)
    {
        if (addQuotes)
            prompt.Append(" \"");
        else
            prompt.Append(' ');

        prompt.Append(value);

        if (addQuotes)
            prompt.Append('"');
    }
}
