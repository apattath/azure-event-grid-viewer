using System;

namespace viewer.Models;

public class AIMessageSentEventData : EventGridPayloadObject
{
    /// <summary>
    /// Function Name
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Function Parameters
    /// </summary>
    public object ConversationSentimentScore { get; set; }
}