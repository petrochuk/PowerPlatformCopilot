using Azure;
using Azure.AI.OpenAI;
using bolt.cli;
using bolt.dataverse.model;
using bolt.module.ai;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;

namespace DataverseCopilot
{
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
        Dictionary<string, GridViewColumn> _columnMap;
        GridView _gridView = new GridView();
        ChatCompletionsOptions _openAiChatCompletionsOptions;
        CompletionsOptions _openAiCompletionsOptions;

        const string SystemPrompt =
            @"
                - You are an assistant who translates language to FetchXML query against Dataverse environment
                - You do not add extra filters, conditions and links to the query unless the user asks you to
                - You can use any FetchXML function, operator, attribute, table, entity
                - You avoid adding all-attributes
                - You add only minimum number of attributes
                - You do not provide any tips, suggestions or possible queries
                - You can ask clarifying questions about which Dataverse table, attribute, etc. to use

                User asks you to write a query which returns: 
            ";

        public MainWindow()
        {
            InitializeComponent();

            _listView.View = _gridView;

            _options = App.ServiceProvider.GetRequiredService<IOptions<PacAppSettings>>();
            _authProfilesManager = App.ServiceProvider.GetRequiredService<IAuthProfilesManager>();
            _authProfile = _authProfilesManager.GetCurrentWithPreference(AuthKind.Universal);
            _authProfile.Resource = AuthResource.Parse(_options.Value.DataverseEnvironment);
            var authenticatedClientFactory = App.ServiceProvider.GetRequiredService<IAuthenticatedClientFactory>();
            _authenticatedHttpClient = authenticatedClientFactory.CreateHttpClient(_authProfile.Resource.Resource, _authProfile.Resource.Resource);

            Initialize();
        }

        OpenAIClient? _openAIClient;
        public OpenAIClient? OpenAIClient
        {
            get
            {
                if (_openAIClient != null)
                    return _openAIClient;

                _openAIClient = new OpenAIClient(
                    new Uri(_options.Value.OpenApiEndPoint),
                    new AzureKeyCredential(_options.Value.OpenApiKey));

                return _openAIClient;
            }
        }

        FetchXmlCleaner _fetchXmlCleaner;
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

            _openAiChatCompletionsOptions = new ChatCompletionsOptions()
            {
                Messages = { new ChatMessage(ChatRole.System, SystemPrompt) },
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

            Dispatcher.Invoke(() => _statusBarText.Text = $"Ready to chat with OpenAI");
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_prompt.Text))
                    return;

                _gridView.Columns.Clear();
                _listView.Items.Clear();

                var fetchXmlModel = await GetFetchXmlModelFromAI();
                if (fetchXmlModel == null)
                    return;

                Dispatcher.Invoke(() => _statusBarText.Text = $"Preparing to call Dataverse");

                var cleanFetchXmlModel = await FetchXmlCleaner.PrepareModelAsync(fetchXmlModel);
                if (cleanFetchXmlModel == null)
                {
                    Dispatcher.Invoke(
                        () => MessageBox.Show(this, "Failed to clean FetchXml", "Internal error", MessageBoxButton.OK, MessageBoxImage.Error)
                    );
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
                    return;
                }
                var resultContent = await result.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<ODataResponse>(resultContent);

                if (responseData.value.Count <= 0)
                {
                    Dispatcher.Invoke(
                        () => MessageBox.Show(this, $"No results", "No results", MessageBoxButton.OK, MessageBoxImage.Information)
                    );
                    return;
                }

                PopulateDataGrid(responseData, cleanFetchXmlModel.Entity.Name, cleanFetchXmlModel);

                Dispatcher.Invoke(() => _statusBarText.Text = $"Ready");
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => MessageBox.Show(this, ex.Message, "Internal error", MessageBoxButton.OK, MessageBoxImage.Error));
            }
        }

        private void PopulateDataGrid(ODataResponse responseData, string mainEntityName, FetchXmlModel cleanFetchXmlModel)
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

        private async Task<FetchXmlModel?> GetFetchXmlModelFromAI()
        {
            if (OpenAIClient == null)
                return null;

            if (_options.Value.UseCompletionAPI)
                _openAiCompletionsOptions.Prompts[0] += Environment.NewLine + _prompt.Text;
            else
                _openAiChatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, _prompt.Text));

            _history.Items.Add(_prompt.Text);
            Dispatcher.Invoke(() => _prompt.Text = string.Empty);

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
            _openAiChatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.Assistant, openAiResponseContent));

            var fetchStart = openAiResponseContent.IndexOf("<fetch", StringComparison.OrdinalIgnoreCase);
            var fetchEnd = openAiResponseContent.IndexOf("</fetch>", StringComparison.OrdinalIgnoreCase);
            if (fetchStart < 0 || fetchEnd < 0)
            {
                Dispatcher.Invoke(
                    () => MessageBox.Show(this, openAiResponseContent, "AI Resonse", 
                    MessageBoxButton.OK, MessageBoxImage.Question)
                );
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

        private void Prompt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                Submit_Click(sender, e);
            }
        }
    }
}
