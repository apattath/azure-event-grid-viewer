using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using viewer.BusinessLogic;
using viewer.Models;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace viewer.Shared;
using SDKNamespace = Azure.Communication.Messages;

internal class JsonExtractionUtils
{
    const string EnumValuesDescriber = "enum";
    const string StringValue = "string";
    const string IntegerValue = "integer";
    const string ObjectValue = "object";
    const string TypeDescriber = "type";
    const string TypePropertiesDescriber = "properties";
    const string PatternPropertiesDescriber = "patternProperties";
    const string AdditionalPropertiesDescriber = "additionalProperties";
    const string RequiredParametersDescriber = "required";
    const string PropertyDescriptionDescriber = "description";
    const string ListParameterValue = "array";
    const string ListParameterItemsValue = "items";

    //private readonly IKernel chatCompletionsKernel;

    //public JsonExtractionSkill(IKernel kernel)
    //{
    //    chatCompletionsKernel = kernel;
    //}

    //public async Task<T?> GenerateJsonBasedOnHistory<T>(ChatHistoryPassAround chatConversationState, string jsonExtractionPrompt)
    //{
    //    chatConversationState.AddUserMessage($"{jsonExtractionPrompt}:\r\n{GetPropertyDescription(typeof(T))}");
    //    var outputJson = await chatCompletionsKernel.GetService<IChatCompletion>().GenerateMessageAsync(chatConversationState);
    //    return await SafeExtractJsonFromLLMResponse<T>(
    //        chatConversationState,
    //        outputJson,
    //        GetPropertyDescription(typeof(T)).ToString());
    //}

    //public async Task<string?> GenerateJsonBasedOnHistoryUsingFunctions<T>(ChatHistoryPassAround chatConversationState)
    //{
    //    var openAIClient = new OpenAIClient(new Uri("https://intelligent-routing-fhl.openai.azure.com/"), new Azure.AzureKeyCredential("e8da844bf4e344c48cae6343de3192d1"));
    //    chatConversationState.AddUserMessage("Break down the provided description into sentences and produce [THOUGHT PROCESS] for each sentence " +
    //        "arriving at the requested [FINAL ANSWER], also include the [FINAL ANSWER].\r\n" +
    //        $"Place the information from last [FINAL ANSWER] blob into the following JSON schema:\r\n{GetPropertyDescription(typeof(T))}");
    //    var chatCompletionOptions = new ChatCompletionsOptions(chatConversationState.Select(c => new ChatMessage(c.Role.ToString(), c.Content)));

    //    chatCompletionOptions.Functions.Add(GetGPTFunctionRepresentationForType<T>());
    //    var completions = await openAIClient.GetChatCompletionsAsync("test", chatCompletionOptions);
    //    return completions.Value.Choices[0].ToString();
    //}

    public static AIFunctionDto GetFunctionDefinition(string functionName, string functionDescription, Type type)
    {
        var properties = type.GetProperties();
        AIFunctionParameterDto[] parameterDtos = new AIFunctionParameterDto[properties.Length];
        int count = 0;
        foreach (var property in properties)
        {
            parameterDtos[count++] = GetAIFunctionParameterDto(property);
        }

        var functionDefinition = new AIFunctionDto()
        {
            Name = functionName,
            Description = functionDescription,
            Parameters = parameterDtos,
        };

        return functionDefinition;
    }

    public static object? SafeExtractJsonFromLLMResponse(Type type, string outputJson)
    {
        return SafeExtractJsonFromLLMResponse(
            type,
            outputJson,
            GetPropertyDescription(type).ToString());
    }

    public static object[] GetPropertiesAsObjectArrayForType(Type type, object obj)
    {
        var properties = type.GetProperties();
        var propertyArray = new object[properties.Length];
        for (int i = 0; i < properties.Length; i++)
        {
            propertyArray[i] = properties[i].GetValue(obj);
        }

        return propertyArray;
    }

    // Creates a JSON schema that describes the parameter list for a function
    public static AIFunctionParameterDto GetAIFunctionParameterDto(PropertyInfo propertyInfo)
    {
        Type type = propertyInfo.PropertyType;
        var aIFunctionParameterDto = new AIFunctionParameterDto();
        aIFunctionParameterDto.Name = propertyInfo.Name;
        aIFunctionParameterDto.Description = propertyInfo.GetCustomAttribute<PropertyDescriptionAttribute>()?.AIDescription ?? propertyInfo.Name;
        aIFunctionParameterDto.IsRequired = false;
        if (propertyInfo.GetCustomAttribute<JsonRequiredAttribute>() != null)
        {
            aIFunctionParameterDto.IsRequired = true;
        }

        if (type.IsPrimitive)
        {
            if (type.Name.Equals("Int32"))
            {
                aIFunctionParameterDto.Type = SDKNamespace.AIFunctionParameterType.Number.ToString();
                return aIFunctionParameterDto;
            }
            else
            {

                aIFunctionParameterDto.Type = Enum.Parse<SDKNamespace.AIFunctionParameterType>(type.Name).ToString();
                return aIFunctionParameterDto;
            }
        }

        if (typeof(string).IsAssignableFrom(type))
        {
            aIFunctionParameterDto.Type = SDKNamespace.AIFunctionParameterType.String.ToString();
            return aIFunctionParameterDto;
        }

        if (type.IsEnum)
        {
            aIFunctionParameterDto.Type = SDKNamespace.AIFunctionParameterType.String.ToString();

            var enumValues = new List<string>();
            foreach (var enumvalue in type.GetEnumValues())
            {
                enumValues.Add(enumvalue.ToString());
            }

            aIFunctionParameterDto.EnumValues = enumValues;

            return aIFunctionParameterDto;
        }

        // ToDo: Add support for arrays 
        //if (typeof(IEnumerable).IsAssignableFrom(type))
        //{
        //    var dictInterface = type.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("IDictionary"));
        //    if (dictInterface != default)
        //    {
        //        aIFunctionParameterDto.Type = AIFunctionParameterType.Object;
        //        return aIFunctionParameterDto;
        //    }

        //    var listInterface = type.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("IEnumerable"));
        //    if (listInterface != default && listInterface.GetGenericArguments()[0] != type)
        //    {
        //        aIFunctionParameterDto.Type = AIFunctionParameterType.Array;
        //        Type underlyingType = listInterface.GetGenericArguments()[0];
        //        var underlyingTypeProperty = new PropertyInfo();
        //        aIFunctionParameterDto.ArrayItemParameter = GetAIFunctionParameterDto(properties.FirstOrDefault());
        //        return aIFunctionParameterDto;
        //    }

        //    aIFunctionParameterDto.Type = AIFunctionParameterType.Object;
        //    return aIFunctionParameterDto;
        //}

        //aIFunctionParameterDto.Type = SDKNamespace.AIFuncionParameterType.Object.ToString();
        //var objectParameterProperties = new List<AIFunctionParameterDto>();

        //foreach (var property in type.GetProperties())
        //{
        //    objectParameterProperties.Add(GetAIFunctionParameterDto(property));
        //}

        //aIFunctionParameterDto.ObjectParameterProperties = objectParameterProperties;

        return aIFunctionParameterDto;
    }

    // Creates a JSON schema that describes the parameter list for a function
    public static JObject GetPropertyDescription(Type type)
    {
        var propertyInfoJson = new JObject();

        if (type.IsPrimitive)
        {
            if (type.Name.Equals("Int32"))
            {
                propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, JsonExtractionUtils.IntegerValue);
                return propertyInfoJson;
            }
            else
            {

                propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, type.Name);
                return propertyInfoJson;
            }
        }

        if (typeof(string).IsAssignableFrom(type))
        {
            propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, JsonExtractionUtils.StringValue);
            return propertyInfoJson;
        }

        if (type.IsEnum)
        {
            propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, JsonExtractionUtils.StringValue);

            var enumArray = new JArray();

            foreach (var enumvalue in type.GetEnumValues())
            {
                enumArray.Add(enumvalue.ToString());
            }

            propertyInfoJson.Add(JsonExtractionUtils.EnumValuesDescriber, enumArray);

            return propertyInfoJson;
        }

        if (typeof(IEnumerable).IsAssignableFrom(type))
        {
            var dictInterface = type.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("IDictionary"));
            if (dictInterface != default)
            {
                propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, JsonExtractionUtils.ObjectValue);
                return propertyInfoJson;
            }

            var listInterface = type.GetInterfaces().FirstOrDefault(i => i.Name.StartsWith("IEnumerable"));
            if (listInterface != default && listInterface.GetGenericArguments()[0] != type)
            {
                propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, ListParameterValue);
                propertyInfoJson.Add(JsonExtractionUtils.ListParameterItemsValue, GetPropertyDescription(listInterface.GetGenericArguments()[0]));
                return propertyInfoJson;
            }

            propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, "object");
            return propertyInfoJson;
        }

        propertyInfoJson.Add(JsonExtractionUtils.TypeDescriber, JsonExtractionUtils.ObjectValue);

        JObject propertyObjectProperties = new JObject();
        propertyInfoJson.Add(JsonExtractionUtils.TypePropertiesDescriber, propertyObjectProperties);

        var objectRequiredParameters = new JArray();
        propertyInfoJson.Add(JsonExtractionUtils.RequiredParametersDescriber, objectRequiredParameters);

        foreach (var property in type.GetProperties())
        {
            var propertyName = property.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? property.Name;
            var childPropertyJson = GetPropertyDescription(property.PropertyType);

            childPropertyJson.Add(JsonExtractionUtils.PropertyDescriptionDescriber,
                property.GetCustomAttribute<PropertyDescriptionAttribute>()?.AIDescription ?? propertyName);

            propertyObjectProperties.Add(propertyName, childPropertyJson);

            if (property.GetCustomAttribute<JsonRequiredAttribute>() != null)
            {
                objectRequiredParameters.Add(propertyName);
            }
        }

        return propertyInfoJson;
    }

    public static object? SafeExtractJsonFromLLMResponse(Type type, string outputJson, string jsonSchema)
    {
        try
        {
            return ExtractJsonFromLLMResponse(type, outputJson);
        }
        catch (JsonException)
        {
            //chatHistory.AddAssistantMessage(outputJson);

            //chatHistory.AddUserMessage("I asked for a JSON");

            //await Task.Delay(TimeSpan.FromSeconds(2));
            //outputJson = await chatCompletionsKernel
            //     .GetService<IChatCompletion>()
            //     .GenerateMessageAsync(chatHistory);

            //chatHistory.RemoveAt(chatHistory.Count - 1);
            //chatHistory.RemoveAt(chatHistory.Count - 1);

            return ExtractJsonFromLLMResponse(type, outputJson);
        }
        //catch (JsonSerializationException)
        //{
        //    chatHistory.AddAssistantMessage(outputJson);

        //    chatHistory.AddUserMessage($"I asked for a JSON with this instruction:\n {jsonFormat}\ncan you please make sure to match schema?");

        //    await Task.Delay(TimeSpan.FromSeconds(2));
        //    outputJson = await chatCompletionsKernel
        //         .GetService<IChatCompletion>()
        //         .GenerateMessageAsync(chatHistory);

        //    chatHistory.RemoveAt(chatHistory.Count - 1);
        //    chatHistory.RemoveAt(chatHistory.Count - 1);

        //    return ExtractJsonFromLLMResponse<T>(outputJson);
        //}
    }

    private static object? ExtractJsonFromLLMResponse(Type type, string? response)
    {
        if (response == null)
        {
            return default;
        }

        var jsonStartIndex = -1;
        var jsonEndIndex = -1;
        var startindex = 0;

        var braceTrackingStack = new Stack<char>();

        foreach (var character in response)
        {
            if (character == '{')
            {
                if (braceTrackingStack.Count == 0)
                {
                    jsonStartIndex = startindex;
                }
                braceTrackingStack.Push(character);
            }
            else if (character == '}')
            {
                braceTrackingStack.Pop();
                if (braceTrackingStack.Count == 0)
                {
                    jsonEndIndex = startindex;
                }
            }
            startindex++;
        }

        if (jsonEndIndex < 0 || jsonStartIndex < 0)
        {
            throw new JsonException();
        }

        return JsonConvert.DeserializeObject(response.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1), type);
    }
}
