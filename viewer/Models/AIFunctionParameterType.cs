using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;

namespace viewer.Models;

[JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy), new object[0], false)]
public enum AIFunctionParameterType
{
    [EnumMember(Value = "string")]
    String = 0,

    [EnumMember(Value = "number")]
    Number,

    [EnumMember(Value = "boolean")]
    Boolean,

    [EnumMember(Value = "enum")]
    Enum,

    [EnumMember(Value = "object")]
    Object,

    [EnumMember(Value = "array")]
    Array,
}