using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Options;
using PowerAppGenerator.AppModel;
using PowerAppGenerator.Settings;
using System.Text.Json;

namespace PowerAppGenerator;

public partial class MainPage : ContentPage
{
    OpenAIClient _openAIClient;
    string _openAIModelId;

    public MainPage(IOptions<AppSettings> appSettings)
    {
        if (appSettings is null || appSettings.Value is null)
        {
            throw new ArgumentNullException(nameof(appSettings));
        }
        if (string.IsNullOrWhiteSpace(appSettings.Value.OpenApiEndPoint) || string.IsNullOrWhiteSpace(appSettings.Value.OpenApiKey) || string.IsNullOrWhiteSpace(appSettings.Value.OpenApiModel))
        {
            throw new ArgumentException("AI end point is not configured");
        }

        InitializeComponent();
        _openAIClient = new OpenAIClient(
            new Uri(appSettings.Value.OpenApiEndPoint),
            new AzureKeyCredential(appSettings.Value.OpenApiKey));
        _openAIModelId = appSettings.Value.OpenApiModel;
    }

    private void AddSystemMessage(IList<ChatMessage> messages)
    {
        messages.Add(new ChatMessage(ChatRole.System, "You are an assistant helping a customer to create a new Microsoft Power App."));
        messages.Add(new ChatMessage(ChatRole.User, _prompt.Text + "."));
    }

    private async void OnGenerateClicked(object sender, EventArgs e)
    {
        try
        {
            _progress.Text = "Ready";
            _generate.IsEnabled = false;

            var powerApp = new PowerApp();
            var screens = await GenerateScreens();

            await GenerateControls(screens);

            powerApp.Add(screens);
            _progress.Text = "Saving PowerApp ...";
            powerApp.SaveAs(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "PowerApp.msapp"));
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _generate.IsEnabled = true;
            _progress.Text = "Ready";
        }
    }

    private async Task<IList<Screen>> GenerateScreens()
    {
        _progress.Text = "Generating screens ...";
        var jsonArray = await GetChatCompletionAsJson("For this app create a list of sceens in single Json array format with Name, Title and Description");

        var screens = new List<Screen>();
        foreach (var item in jsonArray.EnumerateArray())
        {
            if (!item.TryGetProperty("Name", out var nameProp))
                continue;
            if (nameProp.ValueKind != JsonValueKind.String)
                continue;
            var name = nameProp.GetString();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var title = item.GetProperty("Title").GetString();
            var description = item.GetProperty("Description").GetString();

            screens.Add(new Screen(name) { 
                Title = title, 
                Description = description 
            });
        }

        return screens;
    }

    private async Task GenerateControls(IList<Screen> screens)
    {
        var screenIdx = 0;
        var idx = 0;
        foreach (var screen in screens)
        {
            _progress.Text = $"Generating '{screen.Title}'...";
            var container = new Container(screen.Name + "Container1");
            container.Parent = screen.Name;
            container.PublishOrderIndex = idx++;
            screen.Index = screenIdx++;
            screen.Children.Add(container);

            try
            {
                var jsonArray = await GetChatCompletionAsJson($"'{screen.Title}' should have following fields in Json array format with Name, Title and Type:");
                foreach (var item in jsonArray.EnumerateArray())
                {
                    if (!item.TryGetProperty("Name", out var nameProp))
                        continue;
                    if (nameProp.ValueKind != JsonValueKind.String)
                        continue;
                    var name = nameProp.GetString();
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    var title = item.GetProperty("Title").GetString();
                    var type = item.GetProperty("Type").GetString();

                    // Check if we already have a control with the same name
                    if (IsControlExists(screens, name, title, type))
                        continue;

                    var label = new AppModel.Label($"{screen.Name}_{name}Label1");
                    label.Parent = container.Name;
                    label.PublishOrderIndex = idx++;
                    label.Text!.InvariantScript = $"\"{title}\"";
                    label.ZIndex!.InvariantScript = $"{label.PublishOrderIndex}";
                    container.Children.Add(label);

                    var textInput = new AppModel.TextInput($"{screen.Name}_{name}TextInput1");
                    textInput.Parent = container.Name;
                    textInput.PublishOrderIndex = idx++;
                    textInput.ZIndex!.InvariantScript = $"{textInput.PublishOrderIndex}";
                    container.Children.Add(textInput);
                }
            }
            catch
            {

            }
        }
    }

    private bool IsControlExists(IList<Screen> screens, string name, string? title, string? type)
    {
        foreach (var screen in screens)
        {
            var container = screen.Children.FirstOrDefault();
            if (container == null)
                continue;

            foreach (var item in container.Children)
            {
                if (string.Compare(item.Name, name, StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
                if (item is AppModel.Label label && string.Compare(label.Text!.InvariantScript, $"\"{title}\"", StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }
        }

        return false;
    }

    private async Task<JsonElement> GetChatCompletionAsJson(string extraPrompt)
    {
        var chatOptions = new ChatCompletionsOptions()
        {
            Temperature = (float)0.7,
            MaxTokens = 800,
            NucleusSamplingFactor = (float)0.95,
            FrequencyPenalty = 0,
            PresencePenalty = 0,
        };

        AddSystemMessage(chatOptions.Messages);
        chatOptions.Messages.Add(new ChatMessage(ChatRole.User, extraPrompt));
        var response = await _openAIClient.GetChatCompletionsAsync(_openAIModelId, chatOptions);
        if (response == null || response.Value == null || response.Value.Choices == null || response.Value.Choices.Count <= 0)
            throw new ApplicationException("Unable to get Azure Open AI response");

        var jsonArray = response.Value.Choices[0].Message.Content.ExtractJsonArray();
        if (jsonArray == null)
            throw new ApplicationException(response.Value.Choices[0].Message.Content);

        var document = JsonDocument.Parse(jsonArray);
        var root = document.RootElement;
        if (root.ValueKind == JsonValueKind.Array)
            return root;

        // Sometimes the root element is an object with a single property which is an array
        var firstProperty = root.EnumerateObject().FirstOrDefault().Value;
        if (firstProperty.ValueKind == JsonValueKind.Array)
            return firstProperty;

        throw new ApplicationException("Root element is not an array");
    }
}