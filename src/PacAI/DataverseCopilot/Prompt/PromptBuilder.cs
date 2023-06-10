using Azure.AI.OpenAI;
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

    IList<ChatMessage> _messages = new List<ChatMessage>();

    public IList<ChatMessage> ChatMessages => _messages;
    public IEnumerable<string> Messages {
        get {
            return _messages.Select<ChatMessage, string>(m => m.Content); 
        }
    }

    public string Build(string prompt, IList<MetadataEmbedding> metadataEmbeddings)
    {
        var completionPrompt = new StringBuilder();
        completionPrompt.AppendLine(SystemPrompt);
        //completionPrompt.AppendLine(UserProfilePrompt);
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
        messages.Add(new ChatMessage(ChatRole.System, SystemPrompt));
        //messages.Add(new ChatMessage(ChatRole.System, UserProfilePrompt));
        messages.Add(new ChatMessage(ChatRole.System, TablesPromptPrefix));
        foreach (var metadataEmbedding in metadataEmbeddings)
        {
            messages.Add(new ChatMessage(ChatRole.System, metadataEmbedding.Prompt));
        }
        messages.Add(new ChatMessage(ChatRole.System, UserPromptPrefix));
        messages.Add(new ChatMessage(ChatRole.User, $"{prompt}.{Environment.NewLine}"));
    }

    public void AddToday()
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"Today is {TimeOfDay(DateTime.Now)} on {DateTime.Now.DayOfWeek}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"It is {TimeOfYear(DateTime.Now)}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Year {DateTime.Now.Year}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Month {DateTime.Now:MMMM}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Day number {DateTime.Now.Day}"));
    }

    private string TimeOfDay(DateTime dateTime)
    {
        if (dateTime.Hour < 3)
            return "night";
        if (dateTime.Hour < 6)
            return "early morning";
        if (dateTime.Hour < 12)
            return "morning";
        if (dateTime.Hour < 17)
            return "afternoon";
        if (dateTime.Hour < 22)
            return "night";

        return "evening";
    }

    private string TimeOfYear(DateTime dateTime)
    {
        if (dateTime.Month < 3)
            return "winter";
        if (dateTime.Month < 6)
            return "spring";
        if (dateTime.Month < 9)
            return "summer";
        if (dateTime.Month < 12)
            return "autumn";

        return "winter";
    }

    public void AddUserProfile(Microsoft.Graph.Models.User? userProfile, bool addAssisting = true)
    {
        if (userProfile == null)
            return;

        if (addAssisting)
            _messages.Add(new ChatMessage(ChatRole.System, "You are assisting following person:"));
        if (!string.IsNullOrWhiteSpace(userProfile.DisplayName))
            _messages.Add(new ChatMessage(ChatRole.System, $"Name: {userProfile.DisplayName}"));
        if (!string.IsNullOrWhiteSpace(userProfile.GivenName))
            _messages.Add(new ChatMessage(ChatRole.System, $"Given name: {userProfile.GivenName}"));
        if (!string.IsNullOrWhiteSpace(userProfile.Surname))
            _messages.Add(new ChatMessage(ChatRole.System, $"Surname: {userProfile.Surname}"));
        if (!string.IsNullOrWhiteSpace(userProfile.Mail))
            _messages.Add(new ChatMessage(ChatRole.System, $"email: {userProfile.Mail}"));
        if (!string.IsNullOrWhiteSpace(userProfile.UserPrincipalName))
            _messages.Add(new ChatMessage(ChatRole.System, $"User principal name: {userProfile.UserPrincipalName}"));
        if (!string.IsNullOrWhiteSpace(userProfile.MobilePhone))
            _messages.Add(new ChatMessage(ChatRole.System, $"Mobile phone: {userProfile.MobilePhone}"));
        if (!string.IsNullOrWhiteSpace(userProfile.JobTitle))
            _messages.Add(new ChatMessage(ChatRole.System, $"Job title: {userProfile.JobTitle}"));
        if (!string.IsNullOrWhiteSpace(userProfile.OfficeLocation))
            _messages.Add(new ChatMessage(ChatRole.System, $"Office location: {userProfile.OfficeLocation}"));
    }

    internal void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _messages.Add(new ChatMessage(ChatRole.User, text));
    }
}
