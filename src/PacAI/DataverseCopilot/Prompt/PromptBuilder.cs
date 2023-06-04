using Azure.AI.OpenAI;
using DataverseCopilot.Graph.Models;
using System.Text;

namespace DataverseCopilot.Prompt;

internal class PromptBuilder
{
    public const string SystemPrompt =
    @"
                - You are an assistant who translates language to FetchXML query against Dataverse environment
                - You can add most commonly used columns to the query
                - You can use any FetchXML function, operator, attribute, table, entity
                - You do not use all-attributes
                - You can return only one query
                - You can ask clarifying questions about which Dataverse table, attribute, etc. to use
        ";
    public const string TablesPromptPrefix = "User has following tables in addition to many others: ";
    public const string UserPromptPrefix = "Write a query which returns: ";

    public Profile? UserProfile { get; set; }

    public string Build(string prompt, IList<MetadataEmbedding> metadataEmbeddings)
    {
        var completionPrompt = new StringBuilder();
        completionPrompt.AppendLine(UserProfilePrompt);
        completionPrompt.AppendLine(SystemPrompt);
        completionPrompt.AppendLine(TablesPromptPrefix);
        foreach (var metadataEmbedding in metadataEmbeddings)
        {
            completionPrompt.Append(metadataEmbedding.Prompt);
        }
        completionPrompt.Append(UserPromptPrefix);
        completionPrompt.Append($"{prompt}.{Environment.NewLine}");

        return completionPrompt.ToString();
    }

    public void Build(IList<ChatMessage> messages, string prompt, IList<MetadataEmbedding> metadataEmbeddings)
    {
        messages.Clear();
        messages.Add(new ChatMessage(ChatRole.System, UserProfilePrompt));
        messages.Add(new ChatMessage(ChatRole.System, SystemPrompt));
        messages.Add(new ChatMessage(ChatRole.System, TablesPromptPrefix));
        foreach (var metadataEmbedding in metadataEmbeddings)
        {
            messages.Add(new ChatMessage(ChatRole.System, metadataEmbedding.Prompt));
        }
        messages.Add(new ChatMessage(ChatRole.System, UserPromptPrefix));
        messages.Add(new ChatMessage(ChatRole.User, $"{prompt}.{Environment.NewLine}"));
    }

    private string UserProfilePrompt
    {
        get
        {
            if (UserProfile == null)
                return string.Empty;

            var prompt = new StringBuilder();
            prompt.AppendLine($"- User name: {UserProfile.DisplayName}");
            prompt.AppendLine($"- User given name: {UserProfile.givenName}");
            prompt.AppendLine($"- User surname: {UserProfile.surname}");
            prompt.AppendLine($"- User email: {UserProfile.mail}");
            prompt.AppendLine($"- User principla name or email:  {UserProfile.userPrincipalName}");
            if (string.IsNullOrWhiteSpace(UserProfile.mobilePhone))
                prompt.AppendLine($"- User mobile phone: {UserProfile.mail}");
            return prompt.ToString();
        }
    }
}
