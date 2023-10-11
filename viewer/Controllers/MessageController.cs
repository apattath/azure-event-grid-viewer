using Azure;
using Azure.Communication.Messages;
using Azure.Core.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Diagnostics;
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

namespace viewer.Controllers
{
    public class MessageController : Controller
    {

        private readonly string MessagingAIElevateEndpoint = "messages/ai/elevate";
        private readonly string MessagingAIDeElevateEndpoint = "messages/ai/disengage";
        private readonly string MessagingAIDeliveryFunctionResultEndpoint = "messages/ai/deliverFunctionResults";
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
        public IActionResult SetEnvironment(string environment, string channelRegistrationId, string phoneNumber)
        {
            environmentManagerService.SetEnvironment(environment, channelRegistrationId, phoneNumber);
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment();

            ViewData["Environment"] = environment;
            ViewData["ChannelRegistrationId"] = currentSelectedParams.ChannelRegistrationId;
            ViewData["PhoneNumber"] = currentSelectedParams.RecipientList[0];

            return View("Chat");
        }

        public IActionResult Chat()
        {
            return View();
        }

        public async Task<IActionResult> Send(string message)
        {
            await SendMessageToUserAsync(message);

            return View("Chat");
        }

        public async Task<IActionResult> Elevate(string initialMessage)
        {
            // set a default if no initial message was sent.
            if (string.IsNullOrEmpty(initialMessage))
            {
                initialMessage = "Hi, I need some assistance.";
            }

            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            // Create a HttpRequestMessage object with the POST method and the MessagingAIElevateEndpoint as the relative path and api-version as query params
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetFullApiUri(currentSelectedParams.CpmEndpoint, MessagingAIElevateEndpoint));
            httpRequestMessage.Content = new StringContent(
                GetElevateToAIRequestBody(currentSelectedParams, initialMessage),
                Encoding.UTF8,
                "application/json");
            httpRequestMessage.Headers.Add("x-ms-client-request-id", Guid.NewGuid().ToString());

            // Add HMAC auth, set content, method, requestUri before calling this method
            await httpAuthenticator.AddAuthenticationAsync(httpRequestMessage, currentSelectedParams.AccessKey);

            // Send the request and get the response
            HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage);

            if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.OK ||
                httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Accepted)
            {
                ViewData["AIEngagementStatus"] = "Enabled";
                await SendMessageToUserAsync("[An AI agent has been added to this conversation.]");
            }
            else if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                ViewData["AIEngagementStatus"] = "Already enabled";
            }
            else
            {
                var responseContent = await httpResponseMessage.Content.ReadAsStringAsync();
                ViewData["AIEngagementStatus"] = $"Error elevating to AI conversation: {responseContent}";
            }

            // print all the headers in httpResponseMessage
            foreach (var header in httpResponseMessage.Headers)
            {
                ViewData["AIEngagementStatus"] = ViewData["AIEngagementStatus"]?.ToString() + $"\n{header.Key}: {header.Value.FirstOrDefault()}";
            }

            return View("Chat");
        }

        public async Task<IActionResult> DeElevate(string initialMessage)
        {
            if (string.IsNullOrEmpty(initialMessage))
            {
                ViewData["AIEngagementStatus"] = "Error: initial message is empty.";
                return View("Chat");
            }

            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            // Create a HttpRequestMessage object with the POST method and the MessagingAIElevateEndpoint as the relative path and api-version as query params
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetFullApiUri(currentSelectedParams.CpmEndpoint, MessagingAIDeElevateEndpoint));
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

            return View("Chat");
        }

        public async Task<IActionResult> DeliverFunctionResult(string functionResult, string functionName)
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            //var actualFunctionResult = functionResult switch
            //{
            //    "RetrievePatientRegistrationInfo" => "{\"errorResponse\": \"error retrieving patient information due to invalid ID.\"}",
            //    "RegisterPatient" => "{\"patientName\": \"John Doe\", \"insuranceID\":\"ID00000\"}",
            //    _ => "Unknown"
            //};

            // Create a HttpRequestMessage object with the POST method and the MessagingAIElevateEndpoint as the relative path and api-version as query params
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, GetFullApiUri(currentSelectedParams.CpmEndpoint, MessagingAIDeliveryFunctionResultEndpoint));
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

            return View("Chat");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task SendMessageToUserAsync(string message)
        {
            var currentSelectedParams = environmentManagerService.GetCurrentEnvironment() ?? throw new ArgumentNullException("No environment selected.");

            ViewData["Message"] = ViewData["Message"]?.ToString() + $"\nContoso: {message}";
            var sendTextMessageOptions = new SendMessageOptions(currentSelectedParams.ChannelRegistrationId, currentSelectedParams.RecipientList, message);
            Response<SendMessageResult> sendTextMessageResult = await currentSelectedParams.notificationMessagesClient.SendMessageAsync(sendTextMessageOptions);
        }

        private string GetDeliveryFunctionResultRequestBody(EnvironmentSpecificParams currentSelectedParams, object functionResult, string functionName)
        {
            var returnObj = new
            {
                ChannelRegistrationId = currentSelectedParams.ChannelRegistrationId,
                To = currentSelectedParams.RecipientList.FirstOrDefault(),
                FunctionName = functionName,
                FunctionResult = functionResult,
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(returnObj);
        }

        private Uri GetFullApiUri(string baseEndpoint, string relativePath)
        {
            UriBuilder uriBuilder = new UriBuilder(baseEndpoint);
            uriBuilder.Path = relativePath;
            uriBuilder.Query = MessagingAIApiVersion;
            return uriBuilder.Uri;
        }

        private string GetElevateToAIRequestBody(EnvironmentSpecificParams currentSelectedParams, string initialMessage)
        {
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
                    AssistantName = "Kai",
                    AssistantPersonality = "empathetic",
                    BusinessContext = "You are a virtual healthcare assistant for Lamna healthcare that operates in the Seattle area offering primary healthcare and urgent care." +
                    " You collect customers' information, validate their information and help them with scheduling doctor appointments.",
                    Functions = PatientRegistrationMethods.GetFunctionDefinitions(),
                },
                InitialMessage = new
                {
                    Text = initialMessage,
                    Type = "user-text-message"
                }
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(returnObj);
        }

        private string GetDeElevateToAIRequestBody(EnvironmentSpecificParams currentSelectedParams, string initialMessage)
        {
            var returnObj = new
            {
                ChannelRegistrationId = currentSelectedParams.ChannelRegistrationId,
                To = currentSelectedParams.RecipientList.FirstOrDefault(),
                aiDisengagementReason = AIDisengagementReason.ConversationCompleted,
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(returnObj);
        }

    }
}
