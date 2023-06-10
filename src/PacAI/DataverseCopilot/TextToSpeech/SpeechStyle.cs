using System.ComponentModel;

namespace DataverseCopilot.TextToSpeech
{
    enum SpeechStyle
    {
        /// <summary>
        /// Expresses an excited and high-energy tone for promoting a product or service.
        /// </summary>
        [Description("advertisement_upbeat")]
        AdvertisementUpbeat,
        /// <summary>
        /// Expresses a warm and affectionate tone, with higher pitch and vocal energy. The speaker is in a state of attracting the attention of the listener. The personality of the speaker is often endearing in nature.
        /// </summary>
        [Description("affectionate")]
        Affectionate,
        /// <summary>
        /// Expresses an angry and annoyed tone.
        /// </summary>
        [Description("angry")]
        Angry,
        /// <summary>
        /// Expresses a warm and relaxed tone for digital assistants.
        /// </summary>
        [Description("assistant")]
        Assistant,
        /// <summary>
        /// Expresses a cool, collected, and composed attitude when speaking. Tone, pitch, and prosody are more uniform compared to other types of speech.
        /// </summary>
        [Description("calm")]
        Calm,
        /// <summary>
        /// Expresses a casual and relaxed tone.
        /// </summary>
        [Description("chat")]
        Chat,
        /// <summary>
        /// Expresses a positive and happy tone.
        /// </summary>
        [Description("cheerful")]
        Cheerful,
        /// <summary>
        /// Expresses a friendly and helpful tone for customer support.
        /// </summary>
        [Description("customerservice")]
        Customerservice,
        /// <summary>
        /// Expresses a melancholic and despondent tone with lower pitch and energy.
        /// </summary>
        [Description("depressed")]
        Depressed,
        /// <summary>
        /// Expresses a disdainful and complaining tone. Speech of this emotion displays displeasure and contempt.
        /// </summary>
        [Description("disgruntled")]
        Disgruntled,
        /// <summary>
        /// Narrates documentaries in a relaxed, interested, and informative style suitable for dubbing documentaries, expert commentary, and similar content.
        /// </summary>
        [Description("documentary-narration")]
        DocumentaryNarration,
        /// <summary>
        /// Expresses an uncertain and hesitant tone when the speaker is feeling uncomfortable.
        /// </summary>
        [Description("embarrassed")]
        Embarrassed,
        /// <summary>
        /// Expresses a sense of caring and understanding.
        /// </summary>
        [Description("empathetic")]
        Empathetic,
        /// <summary>
        /// Expresses a tone of admiration when you desire something that someone else has.
        /// </summary>
        [Description("envious")] 
        Envious,
        /// <summary>
        /// Expresses an upbeat and hopeful tone. It sounds like something great is happening and the speaker is really happy about that.
        /// </summary>
        [Description("excited")]
        Excited,
        /// <summary>
        /// Expresses a scared and nervous tone, with higher pitch, higher vocal energy, and faster rate. The speaker is in a state of tension and unease.
        /// </summary>
        [Description("fearful")]
        Fearful,
        /// <summary>
        /// Expresses a pleasant, inviting, and warm tone. It sounds sincere and caring.
        /// </summary>
        [Description("friendly")]
        Friendly,
        /// <summary>
        /// Expresses a mild, polite, and pleasant tone, with lower pitch and vocal energy.
        /// </summary>
        [Description("gentle")]
        Gentle,
        /// <summary>
        /// Expresses a warm and yearning tone. It sounds like something good will happen to the speaker.
        /// </summary>
        [Description("hopeful")]
        Hopeful,
        /// <summary>
        /// Expresses emotions in a melodic and sentimental way.
        /// </summary>
        [Description("lyrical")]
        Lyrical,
        /// <summary>
        /// Expresses a professional, objective tone for content reading.
        /// </summary>
        [Description("narration-professional")]
        NarrationProfessional,
        /// <summary>
        /// Express a soothing and melodious tone for content reading.
        /// </summary>
        [Description("narration-relaxed")]
        NarrationRelaxed,
        /// <summary>
        /// Expresses a formal and professional tone for narrating news.
        /// </summary>
        [Description("newscast")]
        Newscast,
        /// <summary>
        /// Expresses a versatile and casual tone for general news delivery.
        /// </summary>
        [Description("newscast-casual")]
        NewscastCasual,
        /// <summary>
        /// Expresses a formal, confident, and authoritative tone for news delivery.
        /// </summary>
        [Description("newscast-formal")]
        NewscastFormal,
        /// <summary>
        /// Expresses an emotional and rhythmic tone while reading a poem.
        /// </summary>
        [Description("poetry-reading")]
        PoetryReading,
        /// <summary>
        /// Expresses a sorrowful tone.
        /// </summary>
        [Description("sad")]
        Sad,
        /// <summary>
        /// Expresses a strict and commanding tone. Speaker often sounds stiffer and much less relaxed with firm cadence.
        /// </summary>
        [Description("serious")]
        Serious,
        /// <summary>
        /// Speaks like from a far distant or outside and to make self be clearly heard
        /// </summary>
        [Description("shouting")]
        Shouting,
        /// <summary>
        /// Expresses a relaxed and interesting tone for broadcasting a sports event.
        /// </summary>
        [Description("sports_commentary")]
        SportsCommentary,
        /// <summary>
        /// Expresses an intensive and energetic tone for broadcasting exciting moments in a sports event.
        /// </summary>
        [Description("sports_commentary_excited")]
        SportsCommentaryExcited,
        /// <summary>
        /// Speaks very softly and make a quiet and gentle sound
        /// </summary>
        [Description("whispering")]
        Whispering,
        /// <summary>
        /// Expresses a very scared tone, with faster pace and a shakier voice. It sounds like the speaker is in an unsteady and frantic status.
        /// </summary>
        [Description("terrified")]
        Terrified,
        /// <summary>
        /// Expresses a cold and indifferent tone.
        /// </summary>
        [Description("unfriendly")]
        Unfriendly
    }
}
