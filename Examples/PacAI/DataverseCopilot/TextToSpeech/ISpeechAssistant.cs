namespace DataverseCopilot.TextToSpeech;

internal interface ISpeechAssistant
{
    Task Speak(string text);
}
