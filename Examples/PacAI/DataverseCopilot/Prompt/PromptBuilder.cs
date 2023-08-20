using Azure.AI.OpenAI;
using DataverseCopilot.Dialog;
using DataverseCopilot.Extensions;
using DataverseCopilot.Graph;
using Microsoft.VisualBasic;
using System.Text;

namespace DataverseCopilot.Prompt;

internal class PromptBuilder
{
    public const string TablesPromptPrefix = "I have following tables in addition to many others: ";
    public const string UserPromptPrefix = "Write a query which returns: ";

    public PromptBuilder(bool addPersonalAssistantGrounding = false)
    {
        if (addPersonalAssistantGrounding)
            AddPersonalAssistantGrounding();
    }

    IList<ChatMessage> _messages = new List<ChatMessage>();

    public IList<ChatMessage> ChatMessages => _messages;
    public IEnumerable<string> Messages {
        get {
            return _messages.Select(m => m.Content); 
        }
    }

    public void AddPersonalAssistantGrounding()
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"You are my personal assistant"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You help me with my daily tasks"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You provide me with useful and actionable information"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Your responses are short"));
    }

    public void AddIntentGrounding(IReadOnlyCollection<string> resourceNames)
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"You are an assistant who understands and extracts latest user intent"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Intent should be split into three parts"));
        _messages.Add(new ChatMessage(ChatRole.System, $"First, object or list of objects to perform action on {IntentResponse.ObjectKey} {string.Join(",", resourceNames)}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Second, {IntentResponse.ActionKey} to perform on object or list of objects"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Third, optional object filter string for search queries"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You respond with {IntentResponse.ObjectKey}, {IntentResponse.ActionKey}, {IntentResponse.FilterKey}"));
    }

    public void AddFetchXmlGrounding()
    {
        _messages.Add(new ChatMessage(ChatRole.System, "You are an assistant who translates language to FetchXML query against Dataverse environment"));
        _messages.Add(new ChatMessage(ChatRole.System, "You can add most commonly used columns to the query"));
        _messages.Add(new ChatMessage(ChatRole.System, "You can use any FetchXML function, operator, attribute, table, entity"));
        _messages.Add(new ChatMessage(ChatRole.System, "You do not use all-attributes"));
        _messages.Add(new ChatMessage(ChatRole.System, "You can return only one query"));
        _messages.Add(new ChatMessage(ChatRole.System, "FetchXML should be ready to execute without any custom operator values"));
        _messages.Add(new ChatMessage(ChatRole.System, "You can ask clarifying questions about which Dataverse table, attribute, etc. to use"));
    }

    public void AddConfirmationGrounding(Resource? resource)
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"You do not add greetings"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You an assistant who asks user clarifying questions"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You need to inquire about user's intent"));
        _messages.Add(new ChatMessage(ChatRole.System, $"You need to ask user what to do next"));

        if (resource != null && string.Compare(resource.Name, Resource.Email.Name, StringComparison.OrdinalIgnoreCase) == 0)
        {
            _messages.Add(new ChatMessage(ChatRole.System, $"User can do typical actions - read, send, recieve, delete, reply, or forward"));
            _messages.Add(new ChatMessage(ChatRole.System, $"You need to summarize email"));
            _messages.Add(new ChatMessage(ChatRole.System, $"You need to ask if email you found is correct"));
            _messages.Add(new ChatMessage(ChatRole.System, $"You need to ask what to do with it next"));
        }
        //_messages.Add(new ChatMessage(ChatRole.System, $"User can lookup, add, update, delete entries in Dataverse tables"));
        //_messages.Add(new ChatMessage(ChatRole.System, $"User can view, schedule, decline caledndar events"));
        //_messages.Add(new ChatMessage(ChatRole.System, $"User can view, create, complete, postpone tasks"));
    }

    public void AddToday()
    {
        _messages.Add(new ChatMessage(ChatRole.System, $"Today is {DateTime.Now.ToTimeOfDay()} on {DateTime.Now.DayOfWeek}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"It is {DateTime.Now.ToTimeOfYear()}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Year {DateTime.Now.Year}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Month {DateTime.Now:MMMM}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Day number {DateTime.Now.Day}"));
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

    internal void Add(Microsoft.Graph.Models.Message message)
    {
        if (message == null)
            return;

        _messages.Add(new ChatMessage(ChatRole.System, $"User received {message.ReceivedDateTime.Value.LocalDateTime.ToRelativeSentence()} an email from {message.From.EmailAddress.Name}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Email subject: {message.Subject.CleanupSubject()}"));
        _messages.Add(new ChatMessage(ChatRole.System, $"Email preview: {message.BodyPreview}"));

    }

    internal void AddAssistantHistory(IList<ChatMessage> chatHistory)
    {
        foreach (var chatMessage in chatHistory)
        {
            _messages.Add(chatMessage);
        }
    }

    public void AddTablesMetadata(IList<MetadataEmbedding> metadataEmbeddings)
    {
        _messages.Add(new ChatMessage(ChatRole.System, TablesPromptPrefix));
        foreach (var metadataEmbedding in metadataEmbeddings)
        {
            _messages.Add(new ChatMessage(ChatRole.System, metadataEmbedding.Prompt));
        }
        _messages.Add(new ChatMessage(ChatRole.System, UserPromptPrefix));
    }
}
