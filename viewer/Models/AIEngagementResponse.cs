using System.Text.Json.Serialization;

namespace viewer.Models;

public class AIEngagementResponse
{
    [JsonPropertyName("conversationId")]
    public string ConversationId { get; set; }
}
