using Newtonsoft.Json;
using System.Collections.Generic;
using System;

namespace viewer.Models;

public class AIAgentConfigurationDto
{
    /// <summary>
    /// Deployment Name that is assoicated with a specific model.
    /// </summary>
    [JsonProperty("deploymentModel", Required = Required.Always)]
    public string DeploymentModel { get; set; }

    /// <summary>
    /// Azure Open AI endpoint
    /// </summary>
    [JsonProperty("endpoint", Required = Required.Always)]
    public Uri Endpoint { get; set; }

    /// <summary>
    /// Azure Open AI api version
    /// </summary>
    [JsonProperty("apiVersion")]
    public string? ApiVersion { get; set; }

    /// <summary>
    /// The initial greeting from AI assistant to the user
    /// </summary>
    [JsonProperty("greeting", Required = Required.Always)]
    public string? Greeting { get; set; }

    /// <summary>
    /// An optional list of functions that can be invoked by the AI assistant to complete a task.
    /// </summary>
    public IEnumerable<AIFunctionDto>? Functions { get; set; }
}
