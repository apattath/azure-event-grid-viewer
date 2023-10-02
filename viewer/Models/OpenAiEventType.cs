using System.Runtime.Serialization;

namespace viewer.Models;

/// <summary>
/// The type of the Event Grid event
/// </summary>
public enum OpenAiEventType
{
    [EnumMember(Value = "AIFunctionCallRequested")]
    AIFunctionCallRequested,

    [EnumMember(Value = "AIGeneratedMessageSent")]
    AIGeneratedMessageSent,

    [EnumMember(Value = "AIDisengaged")]
    AIDisengaged,
}