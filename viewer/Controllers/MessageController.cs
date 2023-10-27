using Azure;
using Azure.Communication.Messages;
using Azure.Core.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using viewer.Auth;
using viewer.BusinessLogic;
using viewer.Models;
using viewer.Shared;
using viewer.Views.Message;

using SDKNamespace = Azure.Communication.Messages;

namespace viewer.Controllers
{
    public class MessageController : Controller
    {
        private readonly string MessagingAIStartEndpoint = "messages/conversations:engageAI";
        private readonly string MessagingAIElevateEndpoint = "messages/conversations:engageAI";
        private readonly string MessagingAIDeElevateEndpoint = "messages/conversations/{0}:disengageAI";
        private readonly string MessagingAIDeliveryFunctionResultEndpoint = "messages/conversations/{0}:deliverFunctionResults";
        private readonly string MessagingAIApiVersion = "api-version=2023-11-01-preview";

        private readonly HttpClient httpClient;
        private readonly IHttpAuthenticator httpAuthenticator;
        private readonly EnvironmentManagerService environmentManagerService;

        // contructor
        public MessageController(
            IHttpClientFactory httpClientFactory,
            IHttpAuthenticator httpAuthenticator,
            EnvironmentManagerService environmentManagerService)
        {
            this.httpAuthenticator = httpAuthenticator ?? throw new ArgumentNullException(nameof(httpAuthenticator));

            _ = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            httpClient = httpClientFactory.CreateClient("MessagingAIClient");
            this.environmentManagerService = environmentManagerService ?? throw new ArgumentNullException(nameof(environmentManagerService));
        }

        // Action method for SetEnvironment
        public IActionResult SetEnvironment(string environment, string channelRegistrationId, string phoneNumber, bool shouldUseAISdk, bool detectFunctionsOneByOne)
        {
            environmentManagerService.SetEnvironment(
                environment,
                channelRegistrationId,
                phoneNumber,
                shouldUseAISdk,
                detectFunctionsOneByOne);
            return ReturnChatView();
        }

        public IActionResult Chat()
        {
            return View();
        }

        public async Task<IActionResult> Send(string message)
        {
            var sendTextMessageResult = await SendMessageToUserAsync(message);

            if (sendTextMessageResult.GetRawResponse().IsError)
            {
                ViewData["Message"] = $"\nError sending message: {sendTextMessageResult.GetRawResponse().ReasonPhrase}";
            }
            else
            {
                ViewData["Message"] = $"\nMessage sent successfully.";
            }

            return ReturnChatView();
        }

        public async Task<IActionResult> StartAIConversationWithText(string initialMessage)
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            if (environmentManagerService.UseAISdk)
            {
                var options = GetBusinessInitiatedConversationOptions(
                    SDKNamespace.BusinessInitiatedMessageType.BusinessTextMessage,
                    currentSelectedParams,
                    initialMessage: initialMessage);

                var engageAIResponse = await currentSelectedParams.NotificationMessagesOpenAIClient.EngageAIEnabledConversationAsync(options);
                var engageAIRawResponse = engageAIResponse.GetRawResponse();

                if (!engageAIRawResponse.IsError)
                {
                    ViewData["StartAITextMessage"] = "Enabled";
                    environmentManagerService.ConversationId = engageAIResponse.Value.ConversationId;
                }
                else
                {
                    ViewData["StartAITextMessage"] = $"Error elevating to AI conversation: {engageAIRawResponse.Content}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in engageAIRawResponse.Headers)
                {
                    ViewData["StartAITextMessage"] = ViewData["StartAITextMessage"]?.ToString() + $"\n{header.Name}: {header.Value}";
                }

            }
            else
            {
                var fullApiUri = GetFullApiUri(currentSelectedParams.CpmEndpoint, MessagingAIStartEndpoint);

                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, fullApiUri);
                httpRequestMessage.Content = new StringContent(
                    GetElevateOrStartAIRequestBody(currentSelectedParams, businessInitiatedMessageType: Models.BusinessMessageKind.TextMessage, initialMessage: initialMessage),
                    Encoding.UTF8,
                    "application/json");
                httpRequestMessage.Headers.Add("x-ms-client-request-id", Guid.NewGuid().ToString());
                if (environmentManagerService.DetectFunctionsOneByOne)
                {
                    httpRequestMessage.Headers.Add(EnvironmentManagerService.DetectFunctionsOptionsHeaderName, "true");
                }

                // Add HMAC auth, set content, method, requestUri before calling this method
                await httpAuthenticator.AddAuthenticationAsync(httpRequestMessage, currentSelectedParams.AccessKey);

                // Send a notification to user about adding an AI agent.
                await SendMessageToUserAsync("[An AI agent has been added to this conversation.]");

                // Send the request and get the response
                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                    httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    ViewData["StartAITextMessage"] = "Enabled";
                    environmentManagerService.ConversationId = JsonConvert.DeserializeObject<AIEngagementResponse>(responseContent).ConversationId;
                }
                else if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    ViewData["StartAITextMessage"] = "Already enabled";
                }
                else
                {
                    ViewData["StartAITextMessage"] = $"Error starting AI-enabled conversation: {responseContent}";
                }
            }

            return ReturnChatView();
        }

        public async Task<IActionResult> StartAIConversationWithTemplate(string templateName)
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            if (environmentManagerService.UseAISdk)
            {
                // Send Sample Template sample_template
                MessageTemplate sampleTemplate = AssembleSampleTemplate(templateName);

                var options = GetBusinessInitiatedConversationOptions(
                    SDKNamespace.BusinessInitiatedMessageType.BusinessTemplateMessage,
                    currentSelectedParams,
                    messageTemplate: sampleTemplate);

                var engageAIResponse = await currentSelectedParams.NotificationMessagesOpenAIClient.EngageAIEnabledConversationAsync(options);
                var engageAIRawResponse = engageAIResponse.GetRawResponse();

                if (!engageAIRawResponse.IsError)
                {
                    ViewData["StartAITemplateMessage"] = "Enabled";
                    environmentManagerService.ConversationId = engageAIResponse.Value.ConversationId;
                }
                else
                {
                    ViewData["StartAITemplateMessage"] = $"Error elevating to AI conversation: {engageAIRawResponse.Content}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in engageAIRawResponse.Headers)
                {
                    ViewData["StartAITemplateMessage"] = ViewData["StartAITemplateMessage"]?.ToString() + $"\n{header.Name}: {header.Value}";
                }

            }
            else
            {
                var fullApiUri = GetFullApiUri(currentSelectedParams.CpmEndpoint, MessagingAIStartEndpoint);

                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, fullApiUri);
                httpRequestMessage.Content = new StringContent(
                    GetElevateOrStartAIRequestBody(
                        currentSelectedParams,
                        businessInitiatedMessageType: Models.BusinessMessageKind.TemplateMessage,
                        template: GetCheckupConfirmationTemplateJson(
                            templateName,
                            "Daniela",
                            "Daniela wants to schedule a doctor appointment.",
                            "Daniela doesn't want to schedule a doctor appointment at this time.")),
                    Encoding.UTF8,
                    "application/json");
                httpRequestMessage.Headers.Add("x-ms-client-request-id", Guid.NewGuid().ToString());
                if (environmentManagerService.DetectFunctionsOneByOne)
                {
                    httpRequestMessage.Headers.Add(EnvironmentManagerService.DetectFunctionsOptionsHeaderName, "true");
                }

                // Add HMAC auth, set content, method, requestUri before calling this method
                await httpAuthenticator.AddAuthenticationAsync(httpRequestMessage, currentSelectedParams.AccessKey);

                // Send a notification to user about adding an AI agent.
                await SendMessageToUserAsync("[An AI agent has been added to this conversation.]");

                // Send the request and get the response
                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                    httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    ViewData["StartAITemplateMessage"] = "Enabled";
                    environmentManagerService.ConversationId = JsonConvert.DeserializeObject<AIEngagementResponse>(responseContent).ConversationId;
                }
                else if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    ViewData["StartAITemplateMessage"] = "Already enabled";
                }
                else
                {
                    ViewData["StartAITemplateMessage"] = $"Error starting AI-enabled conversation: {responseContent}";
                }
            }

            return ReturnChatView();
        }

        public async Task<IActionResult> Elevate(string initialMessage)
        {
            // set a default if no initial message was sent.
            if (string.IsNullOrEmpty(initialMessage))
            {
                initialMessage = "Hi, I need some assistance.";
            }

            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            if (environmentManagerService.UseAISdk)
            {
                var options = GetUserInitiatedConversationOptions(initialMessage, currentSelectedParams);

                var engageAIResponse = await currentSelectedParams.NotificationMessagesOpenAIClient.EngageAIEnabledConversationAsync(options);
                var engageAIRawResponse = engageAIResponse.GetRawResponse();

                if (!engageAIRawResponse.IsError)
                {
                    ViewData["AIEngagementStatus"] = "Enabled";
                    environmentManagerService.ConversationId = engageAIResponse.Value.ConversationId;
                }
                else
                {
                    ViewData["AIEngagementStatus"] = $"Error elevating to AI conversation: {engageAIRawResponse.Content}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in engageAIRawResponse.Headers)
                {
                    ViewData["AIEngagementStatus"] = ViewData["AIEngagementStatus"]?.ToString() + $"\n{header.Name}: {header.Value}";
                }

            }
            else
            {
                // Create a HttpRequestMessage object with the POST method and the MessagingAIElevateEndpoint as the relative path and api-version as query params
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetFullApiUri(currentSelectedParams.CpmEndpoint, MessagingAIElevateEndpoint));
                httpRequestMessage.Content = new StringContent(
                    GetElevateOrStartAIRequestBody(currentSelectedParams, userInitiatedMessageType: Models.UserMessageKind.TextMessage, initialMessage: initialMessage),
                    Encoding.UTF8,
                    "application/json");
                httpRequestMessage.Headers.Add("x-ms-client-request-id", Guid.NewGuid().ToString());
                if (environmentManagerService.DetectFunctionsOneByOne)
                {
                    httpRequestMessage.Headers.Add(EnvironmentManagerService.DetectFunctionsOptionsHeaderName, "true");
                }

                // Add HMAC auth, set content, method, requestUri before calling this method
                await httpAuthenticator.AddAuthenticationAsync(httpRequestMessage, currentSelectedParams.AccessKey);

                // Send a notification to user about adding an AI agent.
                await SendMessageToUserAsync("[An AI agent has been added to this conversation.]");

                // Send the request and get the response
                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);
                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                    httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    ViewData["AIEngagementStatus"] = "Enabled";
                    environmentManagerService.ConversationId = JsonConvert.DeserializeObject<AIEngagementResponse>(responseContent).ConversationId;
                }
                else if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    ViewData["AIEngagementStatus"] = "Already enabled";
                }
                else
                {
                    ViewData["AIEngagementStatus"] = $"Error elevating to AI conversation: {responseContent}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in httpResponseMessage.Headers)
                {
                    ViewData["AIEngagementStatus"] = ViewData["AIEngagementStatus"]?.ToString() + $"\n{header.Key}: {header.Value.FirstOrDefault()}";
                }
            }

            return ReturnChatView();
        }

        public async Task<IActionResult> DeElevate(string initialMessage)
        {
            if (string.IsNullOrEmpty(initialMessage))
            {
                initialMessage = "Thank you for your help.";
            }

            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            if (string.IsNullOrEmpty(environmentManagerService.ConversationId))
            {
                ViewData["AIDisengagementStatus"] = "Error: conversationId is empty. Please call \"EngageAI\" method first.";
                return View("Chat");
            }

            if (environmentManagerService.UseAISdk)
            {
                var disengageAIResponse = await currentSelectedParams.NotificationMessagesOpenAIClient.DisengageAIFromConversationAsync(
                    environmentManagerService.ConversationId,
                    new AIDisengagementOptions(SDKNamespace.AIDisengagementReason.ConversationCompleted));

                if (!disengageAIResponse.IsError)
                {
                    ViewData["AIDisengagementStatus"] = "Disengaged successfully.";
                    await SendMessageToUserAsync("[AI agent has been removed from this conversation.]");
                }
                else
                {
                    ViewData["AIDisengagementStatus"] = $"Error de-elevating AI conversation: {disengageAIResponse.Content}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in disengageAIResponse.Headers)
                {
                    ViewData["AIDisengagementStatus"] = ViewData["AIDisengagementStatus"]?.ToString() + $"\n{header.Name}: {header.Value}";
                }
            }
            else
            {
                var endpointPath = string.Format(MessagingAIDeElevateEndpoint, environmentManagerService.ConversationId);
                var fullApiUri = GetFullApiUri(currentSelectedParams.CpmEndpoint, endpointPath);

                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, fullApiUri);
                httpRequestMessage.Content = new StringContent(
                    GetDeElevateToAIRequestBody(currentSelectedParams, initialMessage),
                    Encoding.UTF8,
                    "application/json");
                httpRequestMessage.Headers.Add("x-ms-client-request-id", Guid.NewGuid().ToString());

                // Add HMAC auth, set content, method, requestUri before calling this method
                await httpAuthenticator.AddAuthenticationAsync(httpRequestMessage, currentSelectedParams.AccessKey);

                // Send the request and get the response
                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                                   httpResponseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
                {
                    ViewData["AIDisengagementStatus"] = "Disengaged successfully.";
                    await SendMessageToUserAsync("[AI agent has been removed from this conversation.]");
                }
                else if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ViewData["AIDisengagementStatus"] = "Already disengaged.";
                }
                else
                {
                    var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                    ViewData["AIDisengagementStatus"] = $"Error de-elevating AI conversation: {responseContent}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in httpResponseMessage.Headers)
                {
                    ViewData["AIDisengagementStatus"] = ViewData["AIDisengagementStatus"]?.ToString() + $"\n{header.Key}: {header.Value.FirstOrDefault()}";
                }
            }

            return ReturnChatView();
        }

        public async Task<IActionResult> DeliverFunctionResult(string functionResult, string functionName)
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            if (string.IsNullOrEmpty(environmentManagerService.ConversationId))
            {
                ViewData["DeliverFunctionResultStatus"] = "Error: conversationId is empty. Please call \"EngageAI\" method first.";
                return View("Chat");
            }

            if (environmentManagerService.UseAISdk)
            {
                var options = new AIDeliverFunctionResultOptions(
                    currentSelectedParams.RecipientList.FirstOrDefault(),
                    functionName,
                    functionResult);

                var deliverFunctionResultResponse = await currentSelectedParams.NotificationMessagesOpenAIClient.DeliverFunctionResultToAIConversationAsync(
                    environmentManagerService.ConversationId,
                    options);

                ViewData["functionName"] = functionName;
                if (!deliverFunctionResultResponse.IsError)
                {
                    ViewData["DeliverFunctionResultStatus"] = "Delivered result of function call";
                }
                else
                {
                    ViewData["DeliverFunctionResultStatus"] = $"Error delivering function result: {deliverFunctionResultResponse.Content}";
                }

                // print all the headers in httpResponseMessage
                foreach (var header in deliverFunctionResultResponse.Headers)
                {
                    ViewData["DeliverFunctionResultStatus"] = ViewData["DeliverFunctionResultStatus"]?.ToString() + $"\n{header.Name}: {header.Value}";
                }
            }
            else
            {
                var endpointPath = string.Format(MessagingAIDeliveryFunctionResultEndpoint, environmentManagerService.ConversationId);
                var fullApiUri = GetFullApiUri(currentSelectedParams.CpmEndpoint, endpointPath);

                // Create a HttpRequestMessage object with the POST method and the MessagingAIElevateEndpoint as the relative path and api-version as query params
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, fullApiUri);
                httpRequestMessage.Content = new StringContent(
                    GetDeliveryFunctionResultRequestBody(currentSelectedParams, functionResult, functionName),
                    Encoding.UTF8,
                    "application/json");
                httpRequestMessage.Headers.Add("x-ms-client-request-id", Guid.NewGuid().ToString());

                // Add HMAC auth, set content, method, requestUri before calling this method
                await httpAuthenticator.AddAuthenticationAsync(httpRequestMessage, currentSelectedParams.AccessKey);

                // Send the request and get the response
                HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

                ViewData["functionName"] = functionName;
                if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                    httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
                {
                    ViewData["DeliverFunctionResultStatus"] = "Delivered result of function call";
                }
                else
                {
                    var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                    ViewData["DeliverFunctionResultStatus"] = $"Error delivering function result: {responseContent}";
                }
            }

            return ReturnChatView();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<Response<SendMessageResult>> SendMessageToUserAsync(string message)
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            var sendTextMessageOptions = new SendMessageOptions(currentSelectedParams.ChannelRegistrationId, currentSelectedParams.RecipientList, message);
            Response<SendMessageResult> sendTextMessageResult = await currentSelectedParams.NotificationMessagesClient.SendMessageAsync(sendTextMessageOptions);

            return sendTextMessageResult;
        }

        private string GetDeliveryFunctionResultRequestBody(EnvironmentSpecificParams currentSelectedParams, object functionResult, string functionName)
        {
            var returnObj = new
            {
                To = currentSelectedParams.RecipientList.FirstOrDefault(),
                FunctionName = functionName,
                FunctionResult = functionResult,
            };

            return JsonConvert.SerializeObject(returnObj);
        }

        private Uri GetFullApiUri(string baseEndpoint, string relativePath)
        {
            UriBuilder uriBuilder = new UriBuilder(baseEndpoint);
            uriBuilder.Path = relativePath;
            uriBuilder.Query = MessagingAIApiVersion;
            return uriBuilder.Uri;
        }

        private IActionResult ReturnChatView()
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment();

            ViewData["Environment"] = currentSelectedParams.TargetEnvironment;
            ViewData["ChannelRegistrationId"] = currentSelectedParams.ChannelRegistrationId;
            ViewData["PhoneNumber"] = currentSelectedParams.RecipientList[0];
            ViewData["ConversationId"] = environmentManagerService.ConversationId;
            ViewData["UseAISdk"] = environmentManagerService.UseAISdk;
            ViewData["DetectFunctionsOneByOne"] = environmentManagerService.DetectFunctionsOneByOne;

            return View("Chat");
        }

        private object GetCheckupConfirmationTemplateJson(string templateName, string patientName, string yesPayload, string noPayload)
        {
            var templateObject = new
            {
                name = templateName,
                language = "en_us",
                values = new
                {
                    name = new
                    {
                        kind = "text",
                        text = new
                        {
                            text = patientName,
                        },
                    },
                    Yes = new
                    {
                        kind = "quick_action",
                        quickAction = new
                        {
                            text = (string)null,
                            payload = yesPayload,
                        },
                    },
                    No = new
                    {
                        kind = "quick_action",
                        quickAction = new
                        {
                            text = (string)null,
                            payload = noPayload,
                        },
                    },
                },
                bindings = new
                {
                    whatsapp = new
                    {
                        header = (string)null,
                        body = new[]
                        {
                            new
                            {
                                refValue = "name",
                            },
                        },
                        footer = (string)null,
                        button = new[]
                        {
                            new
                            {
                                refValue = "Yes",
                                subType = "quickReply",
                            },
                            new
                            {
                                refValue = "No",
                                subType = "quickReply",
                            },
                        },
                    },
                },
            };

            return templateObject;
        }

        private string GetElevateOrStartAIRequestBody(
            EnvironmentSpecificParams currentSelectedParams,
            Models.UserMessageKind? userInitiatedMessageType = default,
            Models.BusinessMessageKind? businessInitiatedMessageType = default,
            string initialMessage = default,
            object template = default)
        {
            var businessInitiatedMessage = (businessInitiatedMessageType is null) ? default : new
            {
                Content = initialMessage,
                Kind = businessInitiatedMessageType.ToString(),
                Template = template,
            };

            var userInitiatedMessage = (userInitiatedMessageType is null) ? default : new
            {
                Content = initialMessage,
                Kind = userInitiatedMessageType.ToString(),
                Template = template,
            };

            var returnObj = new
            {
                ChannelRegistrationId = currentSelectedParams.ChannelRegistrationId,
                To = currentSelectedParams.RecipientList.FirstOrDefault(),
                AgentConfiguration = new AIAgentConfigurationDto
                {
                    DeploymentModel = "test",
                    Endpoint = new Uri("https://intelligent-routing-fhl.openai.azure.com/"),
                    ApiVersion = "2023-07-01-preview",
                    Greeting = "Hi, I'm Kai, your virtual assistant. How can I help you today?",
                    Functions = PatientRegistrationMethods.GetFunctionDefinitions(),
                },
                MessageToSend = businessInitiatedMessage,
                MessageReceived = userInitiatedMessage,
            };

            var returnString = JsonConvert.SerializeObject(returnObj);
            return returnString;
        }

        private static UserInitiatedConversationOptions GetUserInitiatedConversationOptions(
            string initialMessage,
            EnvironmentSpecificParams currentSelectedParams)
        {
            var aiAgentConfiguration = new AgentConfiguration(
                "https://intelligent-routing-fhl.openai.azure.com/",
                "test",
                "Hi, I'm Kai, your virtual assistant. How can I help you today?",
                "2023-07-01-preview");

            foreach (var function in PatientRegistrationMethods.GetFunctionDefinitions())
            {
                aiAgentConfiguration.Functions.Add(function.ToAIFunctionDefinition());
            }

            var userInitiatedMessage = new UserInitiatedMessage(SDKNamespace.UserInitiatedMessageType.UserTextMessage);
            userInitiatedMessage.Content = initialMessage;
            userInitiatedMessage.Template = default;

            var userInitiatedConversationOptions = new UserInitiatedConversationOptions(
                channelRegistrationId: currentSelectedParams.ChannelRegistrationId,
                to: currentSelectedParams.RecipientList.FirstOrDefault(),
                agentConfiguration: aiAgentConfiguration,
                userInitiatedMessage: userInitiatedMessage);
            return userInitiatedConversationOptions;
        }

        private static BusinessInitiatedConversationOptions GetBusinessInitiatedConversationOptions(
            SDKNamespace.BusinessInitiatedMessageType businessInitiatedMessageType,
            EnvironmentSpecificParams currentSelectedParams,
            string initialMessage = default,
            MessageTemplate messageTemplate = default)
        {
            var aiAgentConfiguration = new AgentConfiguration(
                "https://intelligent-routing-fhl.openai.azure.com/",
                "test",
                "Hi, I'm Kai, your virtual assistant. How can I help you today?",
                "2023-07-01-preview");

            foreach (var function in PatientRegistrationMethods.GetFunctionDefinitions())
            {
                aiAgentConfiguration.Functions.Add(function.ToAIFunctionDefinition());
            }

            var businessInitiatedMessage = new BusinessInitiatedMessage(businessInitiatedMessageType);
            businessInitiatedMessage.Content = initialMessage;
            businessInitiatedMessage.Template = messageTemplate;

            var businessInitiatedConversationOptions = new BusinessInitiatedConversationOptions(
                channelRegistrationId: currentSelectedParams.ChannelRegistrationId,
                to: currentSelectedParams.RecipientList.FirstOrDefault(),
                agentConfiguration: aiAgentConfiguration,
                businessInitiatedMessage: businessInitiatedMessage);
            return businessInitiatedConversationOptions;
        }

        private string GetDeElevateToAIRequestBody(EnvironmentSpecificParams currentSelectedParams, string initialMessage)
        {
            var returnObj = new
            {
                aiDisengagementReason = SDKNamespace.AIDisengagementReason.ConversationCompleted.ToString(),
            };

            return JsonConvert.SerializeObject(returnObj);
        }

        private static MessageTemplate AssembleSampleTemplate(string templateName)
        {
            string templateLanguage = "en_us";

            var name = new MessageTemplateText(name: "name", text: "Daniela");
            var yes = new MessageTemplateQuickAction(name: "Yes", payload: "Daniela said yes");
            var no = new MessageTemplateQuickAction(name: "No", payload: "Daniela said no");

            IEnumerable<MessageTemplateValue> values = new List<MessageTemplateValue>
            {
                name,
                yes,
                no
            };
            var bindings = new MessageTemplateWhatsAppBindings(
                body: new[] { name.Name },
                button: new Dictionary<string, MessageTemplateValueWhatsAppSubType> {
                    { yes.Name, MessageTemplateValueWhatsAppSubType.QuickReply },
                    { no.Name, MessageTemplateValueWhatsAppSubType.QuickReply }
                });

            var issueResolutionTemplate = new MessageTemplate(templateName, templateLanguage, values, bindings);

            return issueResolutionTemplate;
        }
    }
}
