using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;

namespace viewer.Models
{
    /// <summary>
    /// The type of business message. Supports text, image, template.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy), new object[0], false)]
    public enum BusinessInitiatedMessageType
    {
        [EnumMember(Value = "business-template-message")]
        BusinessTemplateMessage = 1,

        [EnumMember(Value = "business-text-message")]
        BusinessTextMessage,
    }
}
