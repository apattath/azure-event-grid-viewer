using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;

namespace viewer.Models;

/// <summary>
/// The type of user message. Supports text, image, template.
/// </summary>
[JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy), new object[0], false)]
public enum UserInitiatedMessageType
{
    [EnumMember(Value = "user-text-message")]
    UserInitiatedTextMessage,

    [EnumMember(Value = "user-response")]
    UserResponseMessage,
}
