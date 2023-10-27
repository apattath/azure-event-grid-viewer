using Azure.Communication.Messages;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;

namespace viewer.Models;

public class AIFunctionDto
{
    /// <summary>
    /// The name of the function that can be called by the AI assistant
    /// </summary>
    [JsonPropertyName("name")]
    [Required]
    public string Name { get; set; }

    /// <summary>
    /// The description of the function that can be called by the AI assistant
    /// </summary>
    [JsonPropertyName("description")]
    [Required]
    public string Description { get; set; }

    /// <summary>
    /// The list of parameters for the function that can be called by the AI assistant
    /// </summary>
    [JsonPropertyName("parameters")]
    [Required]
    public IEnumerable<AIFunctionParameterDto> Parameters { get; set; }

    public AIFunctionDefinition ToAIFunctionDefinition()
    {
        return new AIFunctionDefinition(
            Name,
            Description,
            Parameters.Select(p => p.ToAIFunctionParameterDefinition()).ToList());
    }
}