using Azure;
using Azure.AI.OpenAI;
using DataverseCopilot.Graph;
using Microsoft.Graph.Models;
using Microsoft.SemanticKernel.AI.Embeddings.VectorOperations;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks.Dataflow;

namespace DataverseCopilot.Dialog;

internal class Context
{
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

    public User? UserProfile { get; set; }

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

        var similarities = new SortedList<double, MetadataEmbedding>();
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
}
