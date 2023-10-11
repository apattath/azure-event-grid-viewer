using System.Runtime.Serialization;

namespace viewer.Models;

public enum AIDisengagementReason
{
    [EnumMember(Value = "text")]
    Unknown = 1,

    // To be supported
    [EnumMember(Value = "conversationExpired")]
    ConversationExpired,

    [EnumMember(Value = "escalatedToHuman")]
    EscalatedToHuman,

    [EnumMember(Value = "conversationCompleted")]
    ConversationCompleted,
}
