using Azure.AI.OpenAI;
using LiteDB;
using System.Diagnostics;

namespace AP2.DataverseAzureAI.Model;

[DebuggerDisplay("{Message}")]
public class WelcomeMessage
{
    public const int Limit = 10;
    public const int MaxLength = 256;

    public ObjectId? Id { get; set; }
    public string? Message { get; set; }
    public DateTimeOffset CreatedOn { get; set; } = DateTimeOffset.UtcNow;

    public static WelcomeMessage Default = new ("Welcome to the Power Platform");

    public WelcomeMessage()
    {

    }

    public WelcomeMessage(string messge)
    {
        if (string.IsNullOrWhiteSpace(messge))
            throw new ArgumentNullException(nameof(messge));

        Message = messge;
    }

    public static implicit operator string(WelcomeMessage? message)
    {
        if (message == null)
            return string.Empty;

        return message.Message!;
    }

    public static async Task NextWelcomeMessage(LiteDatabase liteDatabase, OpenAIClient openAIClient, string openApiModel)
    {
        var chatOptions = new ChatCompletionsOptions()
        {
            Temperature = 1.95f,
            MaxTokens = 200,
            NucleusSamplingFactor = 1,
            FrequencyPenalty = 2,
            PresencePenalty = 2,
        };

        chatOptions.Messages.Add(new ChatMessage(ChatRole.System, DataverseAIClient.MainSystemPrompt));
        chatOptions.Messages.Add(new ChatMessage(ChatRole.System, "You only generate new short one sentence welcome messages for a user"));
        chatOptions.Messages.Add(new ChatMessage(ChatRole.System, "Do not repeat already generated messages"));
        chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Already generated: {WelcomeMessage.Default}"));
        var welcomeMessages = liteDatabase.GetCollection<WelcomeMessage>();
        foreach (var welcomeMessage in welcomeMessages.FindAll())
        {
            chatOptions.Messages.Add(new ChatMessage(ChatRole.System, $"Already generated: {welcomeMessage}"));
        }
        chatOptions.Messages.Add(new ChatMessage(ChatRole.User, "A brand new welcome message:"));

        var response = await openAIClient.GetChatCompletionsAsync(openApiModel, chatOptions).ConfigureAwait(false);
        if (response == null || response.Value == null || response.Value.Choices == null || response.Value.Choices.Count <= 0)
            return;
        var nextWelcomeMessage = response.Value.Choices[0].Message.Content;
        if (string.IsNullOrWhiteSpace(nextWelcomeMessage))
            return;
        nextWelcomeMessage = nextWelcomeMessage.Trim();
        nextWelcomeMessage = nextWelcomeMessage.Trim('\'');
        nextWelcomeMessage = nextWelcomeMessage.Trim('"');
        if (string.IsNullOrWhiteSpace(nextWelcomeMessage) || nextWelcomeMessage.Length > WelcomeMessage.MaxLength)
            return;

        welcomeMessages.Insert(new WelcomeMessage(nextWelcomeMessage));
        while (welcomeMessages.Count() > WelcomeMessage.Limit)
        {
            var oldestWelcomeMessage = welcomeMessages.FindAll().OrderBy(w => w.CreatedOn).First();
            welcomeMessages.Delete(oldestWelcomeMessage.Id);
        }
    }
}
