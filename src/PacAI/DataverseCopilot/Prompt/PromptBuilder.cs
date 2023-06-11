using Azure.AI.OpenAI;
using DataverseCopilot.Graph;
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

    public PromptBuilder(bool addAssistantGrounding = false, bool addIntentGrounding = false)
    {
        if (addAssistantGrounding)
            AddAssistantGrounding();
        if (addIntentGrounding)
            AddIntentGrounding();
    }

    IList<ChatMessage> _messages = new List<ChatMessage>();

    public IList<ChatMessage> ChatMessages => _messages;
    public IEnumerable<string> Messages {
        get {
            return _messages.Select(m => m.Content); 
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
        messages.Add(new ChatMessage(ChatRole.System, TablesPromptPrefix));
        foreach (var metadataEmbedding in metadataEmbeddings)
        {
            messages.Add(new ChatMessage(ChatRole.System, metadataEmbedding.Prompt));
        }
        messages.Add(new ChatMessage(ChatRole.System, UserPromptPrefix));
        messages.Add(new ChatMessage(ChatRole.User, $"{prompt}.{Environment.NewLine}"));
    }

    public void AddAssistantGrounding()
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"You are my personal assistant"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You help me with my daily tasks"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You provide me with useful and actionable information"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Your responses are short"));
    }

    public void AddIntentGrounding()
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"You are an assistant who understands and extracts user intent"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Intent can be either a GET or a SET"));
        _messages.Add(new ChatMessage(ChatRole.System, $"GET - get, find, search, query more information, details, data"));
        _messages.Add(new ChatMessage(ChatRole.System, $"SET - perform an action such as send, save, delete, copy, move"));
        _messages.Add(new ChatMessage(ChatRole.System, $"GET can have source - Email, FileSystem, Dataverse, Calendar, Task"));
        _messages.Add(new ChatMessage(ChatRole.System, $"SET can have target - Email, FileSystem, Dataverse, Calendar, Task"));
        _messages.Add(new ChatMessage(ChatRole.System, $"you respond with intent:, source:, target:, filter:"));
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

    public void AddUserProfile(Microsoft.Graph.Models.User? userProfile)
    {
        if (userProfile == null)
            return;

        if (!string.IsNullOrWhiteSpace(userProfile.DisplayName))
            _messages.Add(new ChatMessage(ChatRole.System, $"My Name: {userProfile.DisplayName}"));
        if (!string.IsNullOrWhiteSpace(userProfile.GivenName))
            _messages.Add(new ChatMessage(ChatRole.System, $"My Given name: {userProfile.GivenName}"));
        if (!string.IsNullOrWhiteSpace(userProfile.Surname))
            _messages.Add(new ChatMessage(ChatRole.System, $"My Surname: {userProfile.Surname}"));
        if (!string.IsNullOrWhiteSpace(userProfile.Mail))
            _messages.Add(new ChatMessage(ChatRole.System, $"My email: {userProfile.Mail}"));
        if (!string.IsNullOrWhiteSpace(userProfile.UserPrincipalName))
            _messages.Add(new ChatMessage(ChatRole.System, $"My User principal name: {userProfile.UserPrincipalName}"));
        if (!string.IsNullOrWhiteSpace(userProfile.MobilePhone))
            _messages.Add(new ChatMessage(ChatRole.System, $"My Mobile phone: {userProfile.MobilePhone}"));
        if (!string.IsNullOrWhiteSpace(userProfile.JobTitle))
            _messages.Add(new ChatMessage(ChatRole.System, $"My Job title: {userProfile.JobTitle}"));
        if (!string.IsNullOrWhiteSpace(userProfile.OfficeLocation))
            _messages.Add(new ChatMessage(ChatRole.System, $"My Office location: {userProfile.OfficeLocation}"));
    }

    internal void Add(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        _messages.Add(new ChatMessage(ChatRole.User, text));
    }

    internal void Avoid(IList<GreetingHistory.Item> items)
    {
        if (items == null || items.Count == 0)
            return;

        foreach (var item in items)
        {
            _messages.Add(new ChatMessage(ChatRole.User, $"Say something different from: '{item.Text}'"));
        }
    }
}
