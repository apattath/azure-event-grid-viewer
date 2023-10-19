using Azure.Communication.Messages;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

using SDKNamespace = Azure.Communication.Messages;

namespace viewer.Models;

public class AIFunctionParameterDto
{
    /// <summary>
    /// The name of the parameter for a function that can be called by the AI assistant
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// The description of the parameter for a function that can be called by the AI assistant
    /// </summary>
    [JsonPropertyName("description")]
    [Required]
    public string Description { get; set; }

    /// <summary>
    /// Indicates whether this parameter is required or not.
    /// </summary>
    [JsonPropertyName("isRequired")]
    [Required]
    public bool IsRequired { get; set; }

    /// <summary>
    /// The type of the parameter for a function that can be called by the AI assistant
    /// </summary>
    [JsonPropertyName("type")]
    [Required]
    public SDKNamespace.AIFuncionParameterType Type { get; set; } = SDKNamespace.AIFuncionParameterType.String;

    /// <summary>
    /// Only applicable if type is Enum.
    /// </summary>
    [JsonPropertyName("enumValues")]
    public IEnumerable<string>? EnumValues { get; set; }

    // TODO: Add support for Object and Array types later

    /// <summary>
    /// Only applicable if the type is Object.
    /// </summary>
    [JsonPropertyName("objectParameterProperties")]
    public IEnumerable<AIFunctionParameterDto>? ObjectParameterProperties { get; set; }

    /// <summary>
    /// Only applicable if the type is Array.
    /// </summary>
    [JsonPropertyName("arrayItemParameter")]
    public AIFunctionParameterDto? ArrayItemParameter { get; set; }

    internal AIFunctionParameterDefinition ToAIFunctionParameterDefinition()
    {
        var definition = new AIFunctionParameterDefinition(
            Name,
            Description,
            IsRequired,
            Type);
        EnumValues?.ToList().ForEach(x => definition.EnumValues.Add(x));

        return definition;
    }
}
