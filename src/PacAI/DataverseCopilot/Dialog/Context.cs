using Azure;
using Azure.AI.OpenAI;
using DataverseCopilot.Graph;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
using System.Collections.Concurrent;
using System.Threading.Tasks.Dataflow;

namespace DataverseCopilot.Dialog;

internal class Context
{
    #region Constructors

    OpenAIClient _openAIClient;
    AppSettings _appSettings;

    public Context(AppSettings appSettings)
    {
        _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        _openAIClient = new OpenAIClient(
            new Uri(appSettings.OpenApiEmbeddingsEndPoint!),
            new AzureKeyCredential(appSettings.OpenApiEmbeddingsKey!));

        // Queue messages to be processed to get embeddings
        MessageQueue = new(async m =>
        {
            var response = await _openAIClient.GetEmbeddingsAsync(
                _appSettings.OpenApiEmbeddingsModel, 
                new EmbeddingsOptions(m.ToEmbedding())
            ).ConfigureAwait(false);
            
            if (response != null && response.Value != null && response.Value.Data.First() != null)
            {
                Messages.TryAdd(response.Value.Data.First().Embedding, m);
            }
        });
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
        var embeddingFilter = await _openAIClient.GetEmbeddingsAsync(
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
