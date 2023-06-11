using Azure;
using Azure.AI.OpenAI;
using DataverseCopilot.Extensions;
using DataverseCopilot.Prompt;
using Microsoft.Extensions.Options;
using System.Text;

namespace DataverseCopilot.AzureAI;

internal class Client
{
    AppSettings _appSettings;
    OpenAIClient _openAIClient;

    public Client(IOptions<AppSettings> pacAppSettings)
    {
        _appSettings = pacAppSettings?.Value ?? throw new ArgumentNullException(nameof(pacAppSettings));
        if (string.IsNullOrWhiteSpace(_appSettings.OpenApiEndPoint))
            throw new InvalidOperationException("OpenAI endpoint is not configured");

        if(string.IsNullOrWhiteSpace(_appSettings.OpenApiKey))
            throw new InvalidOperationException("OpenAI key is not configured");

        _openAIClient = new OpenAIClient(
            new Uri(_appSettings.OpenApiEndPoint),
            new AzureKeyCredential(_appSettings.OpenApiKey));
    }

    public async Task<string> GetResponse(PromptBuilder prompt)
    {
        if (_appSettings.UseCompletionAPI)
        {
            var completionOptions = new CompletionsOptions()
            {
                Temperature = 1,
                MaxTokens = 1000,
                NucleusSamplingFactor = 0.5f,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
                GenerationSampleCount = 1,
            };
            var promptText = new StringBuilder();
            foreach (var message in prompt.Messages)
            {
                promptText.AppendLine(message);
            }
            completionOptions.Prompts.Add(promptText.ToString());

            var openAiResponse = await _openAIClient.GetCompletionsAsync(
                _appSettings.OpenApiModel, completionOptions).ConfigureAwait(false);
            if (openAiResponse != null && openAiResponse.Value != null && openAiResponse.Value.Choices != null && openAiResponse.Value.Choices.Count > 0)
                return openAiResponse.Value.Choices[0].Text;
        }
        else
        {
            var chatOptions = new ChatCompletionsOptions()
            {
                Temperature = 0f,
                MaxTokens = 2000,
                NucleusSamplingFactor = 0f,
                FrequencyPenalty = 0,
                PresencePenalty = 0,
            };
            chatOptions.Messages.AddRange(prompt.ChatMessages);

            var openAiResponse = await _openAIClient.GetChatCompletionsAsync(
                _appSettings.OpenApiModel, chatOptions).ConfigureAwait(false);
            if (openAiResponse != null && openAiResponse.Value != null && openAiResponse.Value.Choices != null && openAiResponse.Value.Choices.Count > 0)
                return openAiResponse.Value.Choices[0].Message.Content;
        }

        throw new InvalidOperationException("OpenAI response is empty");
    }
}
