using DataverseCopilot.Extensions;
using DataverseCopilot.Prompt;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Options;
using static System.Windows.Forms.Design.AxImporter;

namespace DataverseCopilot.TextToSpeech;

/// <summary>
/// https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup-voice#speaking-styles-and-roles
/// </summary>
public class SpeechAssistant : ISpeechAssistant
{
    SpeechSynthesizer _speechSynthesizer;
    AppSettings _pacAppSettings;

    public SpeechAssistant(IOptions<AppSettings> pacAppSettings)
    {
        _pacAppSettings = pacAppSettings.Value;

        var speechConfig = SpeechConfig.FromSubscription(
            _pacAppSettings.SpeechSubscriptionKey,
        _pacAppSettings.SpeechSubscriptionRegion);

        speechConfig.SpeechSynthesisVoiceName = _pacAppSettings.SpeechSynthesisVoiceName;
        _speechSynthesizer = new SpeechSynthesizer(speechConfig);
    }

    public async Task Speak(string text)
    {
        await _speechSynthesizer.SpeakSsmlAsync(FormatSsml(text));
    }

    private string FormatSsml(string text, SpeechStyle speechStyle = SpeechStyle.Whispering)
    {
        return @$"
        <speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xmlns:mstts='https://www.w3.org/2001/mstts' xml:lang='en-US'>
            <voice name='{_pacAppSettings.SpeechSynthesisVoiceName}'>
                <mstts:express-as style='{speechStyle.DescriptionAttr()}'>
                    {text}
                </mstts:express-as>
            </voice>
        </speak>";
    }
}
