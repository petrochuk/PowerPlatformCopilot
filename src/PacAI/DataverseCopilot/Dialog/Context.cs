using Azure;
using Azure.AI.OpenAI;
using DataverseCopilot.Graph;
using DataverseCopilot.Intent;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks.Dataflow;

namespace DataverseCopilot.Dialog;

internal class Context
{
    #region Constructors

    const string ResourcesFileName = "Resources.json";
    Dictionary<string, Resource> _resources;
    readonly OpenAIClient _embeddingsClient;
    readonly AppSettings _appSettings;
    Iterator _iterator;

    public Context(AppSettings appSettings)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _embeddingsClient = new OpenAIClient(
            new Uri(appSettings.OpenApiEmbeddingsEndPoint!),
            new AzureKeyCredential(appSettings.OpenApiEmbeddingsKey!));

        LoadResources();

        // Queue messages to be processed to get embeddings
        MessageQueue = new(async m =>
        {
            var response = await _embeddingsClient.GetEmbeddingsAsync(
                _appSettings.OpenApiEmbeddingsModel, 
                new EmbeddingsOptions(m.ToEmbedding())
            ).ConfigureAwait(false);
            
            if (response != null && response.Value != null && response.Value.Data.First() != null)
            {
                Messages.TryAdd(response.Value.Data.First().Embedding, m);
            }
        });
    }

    private void LoadResources()
    {
        try
        {
            var options = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                IncludeFields = true
            };
            using var openStream = File.OpenRead(Path.Combine(App.DataFolder, ResourcesFileName));
            _resources = JsonSerializer.Deserialize<Dictionary<string, Resource>>(openStream, options);
        }
        catch (IOException) { }
        catch (JsonException) { }
        catch (InvalidOperationException) { }

        if (_resources == null)
        {
            // Well known resources
            _resources = new Dictionary<string, Resource>
            {
                { Resource.Email.Name, Resource.Email },
                { Resource.Calendar.Name, Resource.Calendar },
                { Resource.Contacts.Name, Resource.Contacts },
                { Resource.Tasks.Name, Resource.Tasks },
                { Resource.Dataverse.Name, Resource.Dataverse },
                { Resource.Files.Name, Resource.Files }
            };
            try
            {
                using var saveStream = File.Create(Path.Combine(App.DataFolder, ResourcesFileName));
                JsonSerializer.Serialize(saveStream, _resources);
            }
            catch (IOException) { }

            Task.Run(async () =>
            {
                bool isUpdated = false;
                foreach (var resource in _resources.Values)
                {
                    if (resource.IntentActionsCollection == null)
                        continue;

                    foreach (var item in resource.IntentActionsCollection.Items)
                    {
                        foreach (var alias in item.Aliases)
                        {
                            if (alias.Vector != null)
                                continue;

                            var response = await _embeddingsClient.GetEmbeddingsAsync(
                                    _appSettings.OpenApiEmbeddingsModel, new EmbeddingsOptions(alias.Alias)
                                                                                                                         ).ConfigureAwait(false);
                            if (response != null && response.Value != null && response.Value.Data.First() != null)
                            {
                                isUpdated = true;
                                alias.Vector = response.Value.Data.First().Embedding;
                            }
                        }
                    }
                }

                if (isUpdated)
                {
                    try
                    {
                        using var saveStream = File.Create(Path.Combine(App.DataFolder, ResourcesFileName));
                        JsonSerializer.Serialize(saveStream, _resources);
                    }
                    catch (IOException) { }
                }

            }).ConfigureAwait(false);
        }
    }

    #endregion

    #region Resources

    public IReadOnlyCollection<Resource> Resources 
    { 
        get => _resources.Values;
    }

    public IReadOnlyCollection<string> ResourceKeys
    { 
        get => _resources.Keys;
    }

    #endregion

    #region User

    public User? UserProfile { get; set; }

    #endregion

    #region Messages/Emails

    public ConcurrentDictionary<IReadOnlyList<float>, Message> Messages { get; private set; } = new ();

    private ActionBlock<Message> MessageQueue { get; set; }

    public void AddMessage(Message message)
    {
        MessageQueue.Post(message);
    }

    public async Task<Message> FindRelevantMessage(string filter)
    {
        var embeddingFilter = await _embeddingsClient.GetEmbeddingsAsync(
            _appSettings.OpenApiEmbeddingsModel,
            new EmbeddingsOptions(filter)).ConfigureAwait(false);

        var filterVector = embeddingFilter.Value.Data.First().Embedding.ToArray();

        double highestSimilarity = double.MinValue;
        Message? mostRelevantMessage = null;

        foreach (var item in Messages)
        {
            var similarity = CosineSimilarityOperation.CosineSimilarity(
                new ReadOnlySpan<float>(filterVector), 
                new ReadOnlySpan<float>(item.Key!.ToArray()));
            if (highestSimilarity < similarity)
            {
                mostRelevantMessage = item.Value;
                highestSimilarity = similarity;
            }
        }

        return mostRelevantMessage;
    }

    public async Task<IntentAction> FindBestAction(IntentResponse intentResponse)
    {
        var embeddingFilter = await _embeddingsClient.GetEmbeddingsAsync(
            _appSettings.OpenApiEmbeddingsModel,
            new EmbeddingsOptions(intentResponse.Action)).ConfigureAwait(false);

        var bestSimilarity = double.MinValue;
        IntentAction bestAction = null;
        foreach (var action in intentResponse.ResourceObject.IntentActionsCollection.Items)
        {
            foreach (var actionAlias in action.Aliases)
            {
                var similarity = CosineSimilarityOperation.CosineSimilarity(
                                   new ReadOnlySpan<float>(embeddingFilter.Value.Data.First().Embedding.ToArray()),
                                                  new ReadOnlySpan<float>(actionAlias.Vector!.ToArray()));
                if (bestSimilarity < similarity)
                {
                    bestSimilarity = similarity;
                    bestAction = action;
                }
            }
        }

        return bestAction;
    }

    /// <summary>
    /// Message confirmed by the user
    /// </summary>
    public Message? ConfirmedMessage { get; set; }

    /// <summary>
    /// Message suggested by the system
    /// </summary>
    public Message? SuggestedMessage { get; set; }

    #endregion

    #region Resource

    /// <summary>
    /// Current resource user is interacting with
    /// </summary>
    public Resource? CurrentResource { get; set; }

    #endregion

    #region Chat history

    public List<Azure.AI.OpenAI.ChatMessage> ChatHistory { get; set; } = new();

    #endregion
}
