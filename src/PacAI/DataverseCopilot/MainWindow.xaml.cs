using Azure;
using Azure.AI.OpenAI;
using bolt.cli;
using bolt.dataverse.model;
using bolt.module.ai;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.ComponentModel;
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

namespace DataverseCopilot;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    IAuthenticatedHttpClient _authenticatedHttpClient;
    IAuthProfilesManager _authProfilesManager;
    AuthProfile _authProfile;
    IOptions<PacAppSettings> _options;
    XmlSerializer _fetchXmlModelSerializer = new XmlSerializer(typeof(FetchXmlModel));
    Dictionary<string, GridViewColumn>? _columnMap;
    GridView _gridView = new GridView();
    ChatCompletionsOptions? _openAiChatCompletionsOptions;
    CompletionsOptions? _openAiCompletionsOptions;
    MetadataEmbeddingCollection _metadataEmbeddingCollection;
    StringBuilder _userPromptHistory;

    const string SystemPrompt =
        @"
                - You are an assistant who translates language to FetchXML query against Dataverse environment
                - You do not add extra filters, conditions and links to the query unless the user asks you to
                - You can use any FetchXML function, operator, attribute, table, entity
                - You avoid adding all-attributes
                - You add only minimum number of attributes
                - You do not provide any tips, suggestions or possible queries
                - You can ask clarifying questions about which Dataverse table, attribute, etc. to use
        ";
    const string UserPromptPrefix = "Write a query which returns: ";
    const string ReadyInitialMessage = $"Ready to chat with OpenAI";
    const string ReadyMessage = $"Ready";

    public MainWindow()
    {
        InitializeComponent();

        _listView.View = _gridView;

        _options = App.ServiceProvider.GetRequiredService<IOptions<PacAppSettings>>();
        _authProfilesManager = App.ServiceProvider.GetRequiredService<IAuthProfilesManager>();
        _authProfile = _authProfilesManager.GetCurrentWithPreference(AuthKind.Universal);
        _authProfile.Resource = AuthResource.Parse(_options.Value.DataverseEnvironmentUri);
        var authenticatedClientFactory = App.ServiceProvider.GetRequiredService<IAuthenticatedClientFactory>();
        _authenticatedHttpClient = authenticatedClientFactory.CreateHttpClient(_authProfile.Resource.Resource, _authProfile.Resource.Resource);

        _metadataEmbeddingCollection = MetadataEmbeddingCollection.Load(_options.Value);
        _metadataEmbeddingCollection.Refresh();

        Initialize();
    }

    OpenAIClient? _openAIClient;
    public OpenAIClient OpenAIClient
    {
        get
        {
            if (_openAIClient != null)
                return _openAIClient;

            if (string.IsNullOrWhiteSpace(_options.Value.OpenApiEndPoint) || string.IsNullOrWhiteSpace(_options.Value.OpenApiKey))
                throw new InvalidOperationException("OpenAI endpoint or key are not configured");

            _openAIClient = new OpenAIClient(
                new Uri(_options.Value.OpenApiEndPoint),
                new AzureKeyCredential(_options.Value.OpenApiKey));

            return _openAIClient;
        }
    }

    FetchXmlCleaner? _fetchXmlCleaner;
    public FetchXmlCleaner FetchXmlCleaner => _fetchXmlCleaner ??= new FetchXmlCleaner(_authenticatedHttpClient, _authProfile);

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        Initialize();
    }

    private void Initialize()
    {
        _gridView.Columns.Clear();
        _listView.Items.Clear();
        _history.Items.Clear();
        Dispatcher.Invoke(() => _prompt.Text = string.Empty);
        _userPromptHistory = new ();

        _openAiChatCompletionsOptions = new ChatCompletionsOptions()
        {
            Temperature = 0f,
            MaxTokens = 2000,
            NucleusSamplingFactor = 0f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };
        _openAiCompletionsOptions = new CompletionsOptions()
        {
            Prompts = { SystemPrompt },
            Temperature = 0f,
            MaxTokens = 2000,
            NucleusSamplingFactor = 0f,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
            LogProbabilityCount = 20,
        };

        Dispatcher.Invoke(() => _statusBarText.Text = ReadyInitialMessage);
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_prompt.Text))
                return;

            _gridView.Columns.Clear();
            _listView.Items.Clear();

            var prompt = _prompt.Text;
            _history.Items.Add(_prompt.Text);
            _userPromptHistory.Append(' ');
            _userPromptHistory.Append(prompt);
            Dispatcher.Invoke(() => _prompt.Text = string.Empty);

            Dispatcher.Invoke(() => _statusBarText.Text = $"Preparing prompt to call OpenAI");
            var embeddingVector = await _metadataEmbeddingCollection.GetEmbeddingVector(_userPromptHistory.ToString());
            var topEmbeddings = _metadataEmbeddingCollection.GetTopSimilarities(embeddingVector);

            var fetchXmlModel = await GetFetchXmlModelFromAI(topEmbeddings, _userPromptHistory.ToString());
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
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => MessageBox.Show(this, ex.Message, "Internal error", MessageBoxButton.OK, MessageBoxImage.Error));
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
        }
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

                if (attributeMetadataModel != null && attributeMetadataModel.AttributeType == AttributeType.Uniqueidentifier)
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
                            case AttributeType.Money:
                                columnFormat = $"{{0:c}}";
                                break;
                        }
                        columnHeader += attributeMetadataModel.DisplayName.UserLocalizedLabel.Label;
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

    private async Task<FetchXmlModel?> GetFetchXmlModelFromAI(IList<MetadataEmbedding> metadataEmbeddings, string prompt)
    {
        if (_options.Value.UseCompletionAPI)
        {
            var completionPrompt = new StringBuilder();
            completionPrompt.Append(SystemPrompt);
            foreach (var metadataEmbedding in metadataEmbeddings)
            {
                completionPrompt.Append(metadataEmbedding.Prompt);
            }
            completionPrompt.Append(UserPromptPrefix);
            completionPrompt.Append($"{prompt}.{Environment.NewLine}");
            // Important to add '.' at the end of the prompt to make sure the AI doesn't try to complete the query with suggestions
            _openAiCompletionsOptions!.Prompts[0] = completionPrompt.ToString();
        }
        else
        {
            _openAiChatCompletionsOptions!.Messages.Clear();
            _openAiChatCompletionsOptions!.Messages.Add(new ChatMessage(ChatRole.Assistant, SystemPrompt));
            foreach (var metadataEmbedding in metadataEmbeddings)
            {
                _openAiChatCompletionsOptions!.Messages.Add(new ChatMessage(ChatRole.Assistant, metadataEmbedding.Prompt));
            }
            _openAiChatCompletionsOptions!.Messages.Add(new ChatMessage(ChatRole.Assistant, UserPromptPrefix));
            _openAiChatCompletionsOptions!.Messages.Add(new ChatMessage(ChatRole.User, $"{prompt}.{Environment.NewLine}"));
        }

        Dispatcher.Invoke(() => _statusBarText.Text = $"Waiting for OpenAI's response");

        string? openAiResponseContent = null;
        if (_options.Value.UseCompletionAPI)
        {
            var openAiResponse = await OpenAIClient.GetCompletionsAsync(
                               _options.Value.OpenApiModel, _openAiCompletionsOptions).ConfigureAwait(false);
            if (openAiResponse != null && openAiResponse.Value != null && openAiResponse.Value.Choices != null && openAiResponse.Value.Choices.Count > 0)
                openAiResponseContent = openAiResponse.Value.Choices[0].Text;
        }
        else
        {
            var openAiResponse = await OpenAIClient.GetChatCompletionsAsync(
                _options.Value.OpenApiModel, _openAiChatCompletionsOptions).ConfigureAwait(false);
            if (openAiResponse != null && openAiResponse.Value != null && openAiResponse.Value.Choices != null && openAiResponse.Value.Choices.Count > 0)
                openAiResponseContent = openAiResponse.Value.Choices[0].Message.Content;
        }

        if (string.IsNullOrWhiteSpace(openAiResponseContent))
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, "No completions returned from the OpenAI service", "No Response", MessageBoxButton.OK, MessageBoxImage.Error)
            );
            return null;
        }

        Debug.WriteLine(openAiResponseContent);

        var fetchStart = openAiResponseContent.IndexOf("<fetch", StringComparison.OrdinalIgnoreCase);
        var fetchEnd = openAiResponseContent.IndexOf("</fetch>", StringComparison.OrdinalIgnoreCase);
        if (fetchStart < 0 || fetchEnd < 0)
        {
            Dispatcher.Invoke(
                () => MessageBox.Show(this, openAiResponseContent, "AI Resonse", 
                MessageBoxButton.OK, MessageBoxImage.Question)
            );
            Dispatcher.Invoke(() => _statusBarText.Text = ReadyMessage);
            return null;
        }

        // Read fetchxml
        var fetchXmlFromOpenAI = openAiResponseContent.Substring(fetchStart, fetchEnd - fetchStart + "</fetch>".Length);
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
}
