using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;

namespace viewer.Models
{
    /// <summary>
    /// The type of business message. Supports text, template.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy), new object[0], false)]
    public enum BusinessMessageKind
    {
        [EnumMember(Value = "templateMessage")]
        TemplateMessage = 1,

        [EnumMember(Value = "textMessage")]
        TextMessage,
    }
}
