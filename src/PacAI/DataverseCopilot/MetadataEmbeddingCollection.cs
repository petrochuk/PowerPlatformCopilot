using Azure;
using Azure.AI.OpenAI;
using bolt.cli;
using bolt.dataverse.model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace DataverseCopilot;

internal class MetadataEmbeddingCollection
{
    private const string EntityMetadataFields = $"{nameof(EntityMetadataModel.LogicalName)},{nameof(EntityMetadataModel.LogicalCollectionName)},{nameof(EntityMetadataModel.EntitySetName)},{nameof(EntityMetadataModel.DisplayName)},{nameof(EntityMetadataModel.DisplayCollectionName)}";
    private const string AttributeMetadataFields = $"{nameof(AttributeMetadataModel.LogicalName)},{nameof(AttributeMetadataModel.DisplayName)},{nameof(AttributeMetadataModel.AttributeType)}";

    private ConcurrentDictionary<string, MetadataEmbedding> _items = new ();
    private string _environmentFileName;
    private string _metadataEmbeddingsFolder;
    private Thread? _backgroundRefreshThread;
    private OpenAIClient _openAIClient;
    private AppSettings _appSettings;
    private JsonSerializerOptions _jsonSerializerOptions = new();
    private bool _stopRefresh = false;

    public static MetadataEmbeddingCollection Load(AppSettings appSettings)
    {
        _ = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        var metadataEmbeddingsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Assembly.GetExecutingAssembly().GetName().Name!,
            "MetadataEmbeddings");
        if (!Directory.Exists(metadataEmbeddingsFolder))
            Directory.CreateDirectory(metadataEmbeddingsFolder);
        var environmentFileName = Path.Combine(metadataEmbeddingsFolder, appSettings.DataverseEnvironmentUri!.Host + ".json");

        using var openStream = File.OpenRead(environmentFileName);

        MetadataEmbeddingCollection? collection = null;
        try
        {
            collection = JsonSerializer.Deserialize<MetadataEmbeddingCollection>(openStream);
        }
        catch (JsonException)
        {
        }

        if (collection == null)
            collection = new MetadataEmbeddingCollection();

        collection._environmentFileName = environmentFileName;
        collection._metadataEmbeddingsFolder = metadataEmbeddingsFolder;
        collection._appSettings = appSettings;
        collection._jsonSerializerOptions.WriteIndented = true;

        collection._openAIClient = new OpenAIClient(
            new Uri(appSettings.OpenApiEmbeddingsEndPoint!),
            new AzureKeyCredential(appSettings.OpenApiEmbeddingsKey!));

        return collection;
    }

    public MetadataEmbeddingCollection()
    {
    }

    public ConcurrentDictionary<string, MetadataEmbedding> Items { get => _items; set => _items = value; }

    public async Task<IReadOnlyList<float>> GetEmbeddingVector(string text)
    {
        var response = await _openAIClient.GetEmbeddingsAsync(_appSettings.OpenApiEmbeddingsModel, new EmbeddingsOptions(text)).ConfigureAwait(false);

        return response.Value.Data.First().Embedding;
    }

    /// <summary>
    /// Returns the top N most similar entities to the given vector
    /// </summary>
    /// <param name="top"></param>
    /// <returns></returns>
    public IList<MetadataEmbedding> GetTopSimilarities(IReadOnlyList<float> vector, int top = 5)
    {
        if (_items.Count <= 0)
            return new List<MetadataEmbedding>();

        var similarities = new SortedList<double, MetadataEmbedding>();
        foreach (var item in _items.Values)
        {
            var similarity = CosineSimilarityOperation.CosineSimilarity(new ReadOnlySpan<float>(vector.ToArray()), new ReadOnlySpan<float>(item.Vector!.ToArray()));
            if (similarities.Count < top)
                similarities.Add(similarity, item);
            else if (similarities.Keys[0] < similarity)
            {
                similarities.RemoveAt(0);
                similarities.Add(similarity, item);
            }
        }

        return similarities.Values;
    }

    public void Refresh()
    {
        if (_backgroundRefreshThread != null)
            return;

        // Start a background thread to refresh the embeddings
        _backgroundRefreshThread = new Thread(BackgroundRefresh)
        {
            IsBackground = false,
            Priority = ThreadPriority.BelowNormal
        };
        _stopRefresh = false;
        _backgroundRefreshThread.Start();
    }

    private void BackgroundRefresh()
    {
        var authenticatedClientFactory = App.ServiceProvider.GetRequiredService<IAuthenticatedClientFactory>();
        var authenticatedHttpClient = authenticatedClientFactory.CreateHttpClient(_appSettings.DataverseEnvironmentUri, _appSettings.DataverseEnvironmentUri);
        var authProfilesManager = App.ServiceProvider.GetRequiredService<IAuthProfilesManager>();
        var authProfile = authProfilesManager.GetCurrentWithPreference(AuthKind.Dataverse);

        var allEntityMetadata = authenticatedHttpClient.Get<ODataResponse<EntityMetadataModel>>(
            BuildQueryUri($"EntityDefinitions?$select={EntityMetadataFields}"), authProfile).GetAwaiter().GetResult();

        // Get all the attributes for each entity if they are not cached already
        foreach (var entityMetadata in allEntityMetadata.value)
        {
            if (_items.ContainsKey(entityMetadata.LogicalName))
                continue;

            Task.Run(async () =>
            {
                var entityMetadataWithAttributes = await authenticatedHttpClient.Get<EntityMetadataModel>(
                    BuildQueryUri($"EntityDefinitions(LogicalName='{entityMetadata.LogicalName}')?$select={EntityMetadataFields}&$expand=Attributes($select={AttributeMetadataFields})"), authProfile).ConfigureAwait(false);

                var metadataEmbedding = new MetadataEmbedding(entityMetadataWithAttributes);

                var embeddingsResponse = await _openAIClient.GetEmbeddingsAsync(_appSettings.OpenApiEmbeddingsModel,
                    new EmbeddingsOptions(metadataEmbedding.Prompt)).ConfigureAwait(false);

                metadataEmbedding.Vector = embeddingsResponse.Value.Data.First().Embedding;

                _items.TryAdd(entityMetadata.LogicalName, metadataEmbedding);

                using FileStream fileStream = File.Create(_environmentFileName);
                await JsonSerializer.SerializeAsync(fileStream, this, _jsonSerializerOptions);
                await fileStream.DisposeAsync();
            }).GetAwaiter().GetResult();

            if (_stopRefresh)
                break;
        }

        _backgroundRefreshThread = null;
    }

    public void StopRefresh()
    {
        _stopRefresh = true;
        _backgroundRefreshThread?.Join();
    }

    [DebuggerStepThrough]
    private Uri BuildQueryUri(string query)
    {
        return new Uri($"api/data/v9.0/{query}", UriKind.Relative);
    }
}
