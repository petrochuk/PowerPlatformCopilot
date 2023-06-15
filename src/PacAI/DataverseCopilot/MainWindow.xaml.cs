#region usings
using Azure.AI.OpenAI;
using bolt.cli;
using bolt.dataverse.model;
using bolt.module.copilot;
using CsvHelper;
using DataverseCopilot.AzureAI;
using DataverseCopilot.Dialog;
using DataverseCopilot.Graph;
using DataverseCopilot.Intent;
using DataverseCopilot.Prompt;
using DataverseCopilot.TextToSpeech;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Win32;
using System.ComponentModel;
using System.Formats.Asn1;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using static System.Net.WebRequestMethods;
#endregion

namespace DataverseCopilot;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    #region Construction

    IAuthenticatedHttpClient _authenticatedHttpClient;
    GraphServiceClient _graphClient;
    IAuthProfilesManager _authProfilesManager;
    AuthProfile _authProfile;
    IOptions<AppSettings> _options;
    XmlSerializer _fetchXmlModelSerializer = new XmlSerializer(typeof(FetchXmlModel));
    Dictionary<string, GridViewColumn>? _columnMap;
    GridView _gridView = new GridView();
    MetadataEmbeddingCollection _metadataEmbeddingCollection;
    Context _context;
    Client _aiClient;
    GreetingHistory _greetingHistory;

    ISpeechAssistant _speechAssistant;

    const string ReadyInitialMessage = $"Ready to chat with OpenAI";
    const string ReadyMessage = $"Ready";

    public MainWindow()
    {
        InitializeComponent();

        _listView.View = _gridView;

        _options = App.ServiceProvider.GetRequiredService<IOptions<AppSettings>>();
        _authProfilesManager = App.ServiceProvider.GetRequiredService<IAuthProfilesManager>();
        _authProfile = _authProfilesManager.GetCurrentWithPreference(AuthKind.Universal);
        _authProfile.Resource = AuthResource.Parse(_options.Value.DataverseEnvironmentUri);
        var authenticatedClientFactory = App.ServiceProvider.GetRequiredService<IAuthenticatedClientFactory>();
        _authenticatedHttpClient = authenticatedClientFactory.CreateHttpClient(_authProfile.Resource.Resource, _authProfile.Resource.Resource);
        _graphClient = App.ServiceProvider.GetRequiredService<GraphServiceClient>();
        _speechAssistant = App.ServiceProvider.GetRequiredService<ISpeechAssistant>();

        _context = new (_options.Value);
        _metadataEmbeddingCollection = MetadataEmbeddingCollection.Load(_options.Value);
        _greetingHistory = GreetingHistory.Load();
        _metadataEmbeddingCollection.Refresh();

        _aiClient = new Client(_options);

        Initialize();
    }

    private void Initialize()
    {
        _gridView.Columns.Clear();
        _listView.Items.Clear();
        _history.Items.Clear();
        Dispatcher.Invoke(() => _prompt.Text = string.Empty);

        Dispatcher.Invoke(() => _statusBarText.Text = ReadyInitialMessage);
    }

    #endregion

    FetchXmlCleaner? _fetchXmlCleaner;
    public FetchXmlCleaner FetchXmlCleaner => _fetchXmlCleaner ??= new FetchXmlCleaner(_authenticatedHttpClient, _authProfile);

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Initialize();
    }


    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            _context.UserProfile = await _graphClient.Me.GetAsync();
            var inbox = await _graphClient.Me.MailFolders["Inbox"].GetAsync();
            var emails = await _graphClient.Me.MailFolders["Inbox"].Messages.GetAsync(
                q => 
                { 
                    q.QueryParameters.Top = 10; 
                    q.QueryParameters.Filter = "isRead eq false";
                }
            );

            var welcomePrompt = new PromptBuilder(addPersonalAssistantGrounding: true);
            welcomePrompt.AddToday();
            welcomePrompt.AddUserProfile(_context.UserProfile);
            welcomePrompt.Avoid(_greetingHistory.Items);
            welcomePrompt.Add("Hello assistant.");

            var welcomeResponse = await _aiClient.GetResponse(welcomePrompt);
            welcomeResponse = welcomeResponse.Replace($", {_context.UserProfile.GivenName}!", $" {_context.UserProfile.GivenName}!");
            _greetingHistory.Add(welcomeResponse);

            var emailPrompt = new PromptBuilder(addPersonalAssistantGrounding: true);
            emailPrompt.Add($"I received these new emails: ");
            int emailCount = 0;
            var pageIterator = PageIterator<Message, MessageCollectionResponse>.CreatePageIterator(
            _graphClient, emails,
            (m) =>
            {
                _context.AddMessage(m);
                emailCount++;
                emailPrompt.Add($"Email {emailCount}");
                emailPrompt.Add($"From: {m.From.EmailAddress.Name}");
                emailPrompt.Add($"Subject: {m.Subject.CleanupSubject()}");
                return true;
            });
            await pageIterator.IterateAsync();

            // Starting resource is email
            _context.CurrentResource = Resource.Email;

            emailPrompt.Add($"Give me a short summary about my emails. Don't talk about each one: ");
            var emailResponse = await _aiClient.GetResponse(emailPrompt);
            await _speechAssistant.Speak($"{welcomeResponse}.");
            await _speechAssistant.Speak($"{emailResponse}.");
            _context.ChatHistory.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.Assistant, emailResponse));
        }
        catch (ODataError ex) when (ex.Error != null)
        {
            MessageBox.Show(this, $"{ex.Error.Message}", "Internal error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"{ex.Message}", "Internal error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_prompt.Text))
                return;

            var prompt = _prompt.Text;
            _history.Items.Add(_prompt.Text);
            Dispatcher.Invoke(() => _prompt.Text = string.Empty);

            var intentPrompt = new PromptBuilder();
            intentPrompt.AddIntentGrounding(_context.ResourceKeys);
            intentPrompt.AddAssistantHistory(_context.ChatHistory);
            intentPrompt.Add(prompt);
            _context.ChatHistory.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.User, prompt));
            var intentResponse = new IntentResponse(await _aiClient.GetResponse(intentPrompt), _context.Resources);
            var action = await _context.FindBestAction(intentResponse);

            if (string.Compare(intentResponse.ResourceObject.Name, Resource.Email.Name, StringComparison.OrdinalIgnoreCase) == 0)
            {
                await ActOnEmail(action, intentResponse.Filter);
            }
            else if (string.Compare(intentResponse.ResourceObject.Name, Resource.Email.Name, StringComparison.OrdinalIgnoreCase) == 0)
            { 
                await ActOnDataverse(intentResponse.Action, intentResponse.Filter);
            }
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => MessageBox.Show(this, ex.Message, "Internal error", MessageBoxButton.OK, MessageBoxImage.Error));
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
        }
    }

    private async Task ActOnEmail(IntentAction action, string filter)
    {
        switch (action.Name)
        {
            case IntentAction.Details:
                await GetEmail(filter);
                break;
            case IntentAction.EmailReply:
                await ReplyEmail();
                _context.SuggestedMessage = null;
                break;
            case IntentAction.EmailDelete:
                await _graphClient.Me.Messages[_context.SuggestedMessage.Id].DeleteAsync();
                _context.SuggestedMessage = null;
                break;
        }
    }

    public async Task ReplyEmail()
    {
        var replyAllBody = new Microsoft.Graph.Me.Messages.Item.ReplyAll.ReplyAllPostRequestBody()
        {
            Comment = DataToHtml()
        };

        await _graphClient.Me.Messages[_context.SuggestedMessage.Id].ReplyAll.PostAsync(replyAllBody);
    }

    public async Task GetEmail(string filter)
    {
        _context.SuggestedMessage = await _context.FindRelevantMessage(filter);

        var confirmationPrompt = new PromptBuilder();
        confirmationPrompt.AddConfirmationGrounding(_context.CurrentResource);
        confirmationPrompt.Add(_context.SuggestedMessage);
        var confirmationResponse = await _aiClient.GetResponse(confirmationPrompt);
        _context.ChatHistory.Add(new Azure.AI.OpenAI.ChatMessage(ChatRole.Assistant, confirmationResponse));
        await _speechAssistant.Speak($"{confirmationResponse}.");
    }

    public async Task ActOnDataverse(string action, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter))
            return;

        _gridView.Columns.Clear();
        _listView.Items.Clear();

        Dispatcher.Invoke(() => _statusBarText.Text = $"Preparing prompt to call OpenAI");
        var embeddingVector = await _metadataEmbeddingCollection.GetEmbeddingVector(filter);
        var topEmbeddings = _metadataEmbeddingCollection.GetTopSimilarities(embeddingVector);

        FetchXmlModel fetchXmlModel = await GetFetchXmlModelFromAI(filter, topEmbeddings);
        if (fetchXmlModel == null)
            return;

        Dispatcher.Invoke(() => _statusBarText.Text = $"Preparing to call Dataverse");

        var cleanFetchXmlModel = await FetchXmlCleaner.PrepareModelAsync(fetchXmlModel);
        if (cleanFetchXmlModel == null)
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, "Failed to clean FetchXml", "Internal error", MessageBoxButton.OK, MessageBoxImage.Error)
            );
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
            return;
        }

        // Get data from FetchXML
        var writerSettings = new XmlWriterSettings() { Indent = true, NamespaceHandling = NamespaceHandling.OmitDuplicates };
        using var fileWriter = new StringWriter();
        using var xmlWriter = XmlWriter.Create(fileWriter, writerSettings);
        var xmlNamespaces = new XmlSerializerNamespaces();
        xmlNamespaces.Add(string.Empty, string.Empty);

        _fetchXmlModelSerializer.Serialize(xmlWriter, cleanFetchXmlModel, xmlNamespaces);

        xmlWriter.Flush();
        var fetchXml = fileWriter.ToString();
        Debug.WriteLine("");
        Debug.WriteLine("Clean FetchXML");
        Debug.WriteLine(fetchXml);

        var entityMetadata = FetchXmlCleaner.CachedEntityMetadata[cleanFetchXmlModel.Entity.Name];

        Dispatcher.Invoke(() => _statusBarText.Text = $"Waiting for Dataverse response");
        var result = await _authenticatedHttpClient.Execute(
            new Uri($"api/data/v9.0/{entityMetadata.LogicalCollectionName}?fetchXml={System.Net.WebUtility.UrlEncode(fetchXml)}", UriKind.Relative),
            HttpMethod.Get, _authProfile);
        if (!result.IsSuccessStatusCode)
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, $"Failed to read data {result.StatusCode}", "Internal error", MessageBoxButton.OK, MessageBoxImage.Error)
            );
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
            return;
        }
        var resultContent = await result.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<ODataResponse<Dictionary<string, object>>>(resultContent);

        if (responseData.value.Count <= 0)
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, $"No results", "No results", MessageBoxButton.OK, MessageBoxImage.Information)
            );
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
            return;
        }

        PopulateDataGrid(responseData, cleanFetchXmlModel.Entity.Name, cleanFetchXmlModel);

        Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
    }

    private void PopulateDataGrid(ODataResponse<Dictionary<string, object>> responseData, string mainEntityName, FetchXmlModel cleanFetchXmlModel)
    {
        FetchXmlCleaner.CachedEntityMetadata.TryGetValue(mainEntityName, out var entityMetadataModel);

        _columnMap = new ();
        foreach (var row in responseData.value)
        {
            foreach (var column in row)
            {
                if (column.Key.Contains("odata.etag", StringComparison.OrdinalIgnoreCase))
                    continue;
                if (column.Key.StartsWith("_", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Detect aliased columns
                AttributeMetadataModel attributeMetadataModel = null;
                string? aliasEntityDisplayName = null;
                if (column.Key.Contains("."))
                {
                    var alias = column.Key.Split('.')[0];
                    var attributeName = column.Key.Split('.')[1];
                    var entityName = MapAliasToEntityName(alias, cleanFetchXmlModel.Entity);
                    if (entityName == null)
                        continue;

                    if (!FetchXmlCleaner.CachedEntityMetadata.TryGetValue(entityName, out var aliasEntityMetadataModel))
                        continue;
                    aliasEntityDisplayName = aliasEntityMetadataModel.DisplayName.UserLocalizedLabel.Label;
                    aliasEntityMetadataModel.AttributesDictionary.TryGetValue(attributeName, out attributeMetadataModel);
                }
                else
                {
                    if (entityMetadataModel.AttributesDictionary != null)
                        entityMetadataModel.AttributesDictionary.TryGetValue(column.Key, out attributeMetadataModel);
                }

                if (attributeMetadataModel != null && attributeMetadataModel.AttributeType == bolt.dataverse.model.AttributeType.Uniqueidentifier)
                    continue;

                if (!_columnMap.ContainsKey(column.Key))
                {
                    string columnHeader = string.Empty;
                    string? columnFormat = null;

                    if (aliasEntityDisplayName != null)
                        columnHeader = $"{aliasEntityDisplayName} - ";
                    if (attributeMetadataModel != null)
                    {
                        switch (attributeMetadataModel.AttributeType)
                        {
                            case bolt.dataverse.model.AttributeType.Money:
                                columnFormat = $"{{0:c}}";
                                break;
                        }
                        if (attributeMetadataModel.DisplayName.UserLocalizedLabel != null)
                            columnHeader += attributeMetadataModel.DisplayName.UserLocalizedLabel.Label;
                        else
                            columnHeader += attributeMetadataModel.LogicalName;
                    }
                    else
                    {
                        columnHeader += column.Key;
                    }

                    var gridViewColumn = new GridViewColumn()
                    {
                        Header = columnHeader,
                        DisplayMemberBinding = new Binding(column.Key) 
                        { StringFormat = columnFormat },
                    };
                    _columnMap.Add(column.Key, gridViewColumn);
                    _gridView.Columns.Add(gridViewColumn);
                }
            }
            _listView.Items.Add(new DynamicRow(row));
        }
    }

    private string MapAliasToEntityName(string alias, FetchEntity fetchEntity)
    {
        foreach (var linkedEntity in fetchEntity.LinkEntities)
        {
            if (string.Compare(linkedEntity.Alias, alias, StringComparison.OrdinalIgnoreCase) == 0)
                return linkedEntity.Name;

            var entityName = MapAliasToEntityName(alias, linkedEntity);
            if (entityName != null)
                return entityName;
        }

        return null;
    }

    private async Task<FetchXmlModel?> GetFetchXmlModelFromAI(string prompt, IList<MetadataEmbedding> metadataEmbeddings)
    {
        var fetchXmlPrompt = new PromptBuilder();
        fetchXmlPrompt.AddFetchXmlGrounding();
        fetchXmlPrompt.AddUserProfile(_context.UserProfile);
        fetchXmlPrompt.AddTablesMetadata(metadataEmbeddings);
        fetchXmlPrompt.Add(prompt);

        Dispatcher.Invoke(() => _statusBarText.Text = $"Waiting for OpenAI's response");

        var fetchXmlResponse = await _aiClient.GetResponse(fetchXmlPrompt);
        if (string.IsNullOrWhiteSpace(fetchXmlResponse))
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, "No completions returned from the OpenAI service", "No Response", MessageBoxButton.OK, MessageBoxImage.Error)
            );
            return null;
        }

        Debug.WriteLine(fetchXmlResponse);

        var fetchStart = fetchXmlResponse.IndexOf("<fetch", StringComparison.OrdinalIgnoreCase);
        var fetchEnd = fetchXmlResponse.IndexOf("</fetch>", StringComparison.OrdinalIgnoreCase);
        if (fetchStart < 0 || fetchEnd < 0)
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, fetchXmlResponse, "AI Resonse", 
                MessageBoxButton.OK, MessageBoxImage.Question)
            );
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
            return null;
        }

        // Read fetchxml
        var fetchXmlFromOpenAI = fetchXmlResponse.Substring(fetchStart, fetchEnd - fetchStart + "</fetch>".Length);
        var readerSettings = new XmlReaderSettings() { IgnoreComments = true, IgnoreProcessingInstructions = true, IgnoreWhitespace = true };
        using var fileReader = new StringReader(fetchXmlFromOpenAI);
        using var xmlReader = XmlReader.Create(fileReader, readerSettings);

        try
        {
            return _fetchXmlModelSerializer.Deserialize(xmlReader) as FetchXmlModel;
        }
        catch (InvalidOperationException ex)
        {
            throw new VerbExecutionException(ex.Message, ex);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        Hide();

        base.OnClosing(e);

        _metadataEmbeddingCollection?.StopRefresh();
    }

    private void Prompt_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Return)
        {
            Submit_Click(sender, e);
        }
    }

    private void SaveAsExcel_Click(object sender, RoutedEventArgs e)
    {
        // Open file dialog
        var saveFileDialog = new SaveFileDialog()
        {
            Filter = "CSV File (*.csv)|*.csv",
            Title = "Save as CSV file",
            FileName = "Results.csv",
        };

        if (saveFileDialog.ShowDialog() == true)
        {
            // Save file
            using var streamWriter = new StreamWriter(saveFileDialog.FileName);
            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
            _gridView.Columns.ToList().ForEach(c => csvWriter.WriteField(c.Header));
            foreach (var row in _listView.Items.Cast<DynamicRow>())
            {
                csvWriter.NextRecord();
                foreach (var column in _gridView.Columns)
                {
                    csvWriter.WriteField(row.Get(((Binding)column.DisplayMemberBinding).Path.Path));
                }
            }
        }
    }

    private string DataToHtml()
    {
        var html = new StringBuilder();

        html.AppendLine("<table style='border-collapse: collapse;'>");
        
        // Header
        html.AppendLine("<tr>");
        foreach (var column in _gridView.Columns)
        {
            html.AppendLine($"<th style='border: 1px solid black;'>{column.Header}</th>");
        }
        html.AppendLine("</tr>");

        foreach (var row in _listView.Items.Cast<DynamicRow>())
        {
            html.AppendLine("<tr>");
            foreach (var column in _gridView.Columns)
            {
                html.AppendLine($"<td style='border: 1px solid black;'>{row.Get(((Binding)column.DisplayMemberBinding).Path.Path)}</td>");
            }
            html.AppendLine("</tr>");
        }

        html.AppendLine("</table>");

        return html.ToString();
    }
}
