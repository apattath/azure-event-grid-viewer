using System;

namespace viewer.Models;

public class AIFunctionCallRequestedEventData : EventGridPayloadObject
{
    /// <summary>
    /// Function Name
    /// </summary>
    public string FunctionName { get; set; }

    /// <summary>
    /// Function Parameters
    /// </summary>
    public object FunctionParameters { get; set; }

    /// <summary>
    /// Channel RegistrationId
    /// </summary>
    public Guid ChannelRegistrationId { get; set; }

    /// <summary>
    /// Open AI event type to publish
    /// </summary>
    public OpenAiEventType OpenAiEventType { get; set; }
}