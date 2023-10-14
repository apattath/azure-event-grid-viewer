using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Text;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc;
using viewer.Hubs;
using viewer.Models;
using viewer.BusinessLogic;
using viewer.Shared;

namespace viewer.Controllers
{
    [Route("api/[controller]")]
    public class UpdatesController : Controller
    {
        #region Data Members

        private bool EventTypeSubcriptionValidation
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "SubscriptionValidation";

        private bool EventTypeNotification
            => HttpContext.Request.Headers["aeg-event-type"].FirstOrDefault() ==
               "Notification";

        private readonly IHubContext<GridEventsHub> _hubContext;
        private readonly EnvironmentManagerService environmentManagerService;

        #endregion

        #region Constructors

        public UpdatesController(
            IHubContext<GridEventsHub> gridEventsHubContext,
            EnvironmentManagerService environmentManagerService)
        {
            this._hubContext = gridEventsHubContext;
            this.environmentManagerService = environmentManagerService ?? throw new ArgumentNullException(nameof(environmentManagerService));
        }

        #endregion

        #region Public Methods

        [HttpOptions]
        public async Task<IActionResult> Options()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var webhookRequestOrigin = HttpContext.Request.Headers["WebHook-Request-Origin"].FirstOrDefault();
                var webhookRequestCallback = HttpContext.Request.Headers["WebHook-Request-Callback"];
                var webhookRequestRate = HttpContext.Request.Headers["WebHook-Request-Rate"];
                HttpContext.Response.Headers.Add("WebHook-Allowed-Rate", "*");
                HttpContext.Response.Headers.Add("WebHook-Allowed-Origin", webhookRequestOrigin);
            }

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> Post()
        {
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var jsonContent = await reader.ReadToEndAsync();

                // Check the event type.
                // Return the validation code if it's 
                // a subscription validation request. 
                if (EventTypeSubcriptionValidation)
                {
                    return await HandleValidation(jsonContent);
                }
                else if (EventTypeNotification)
                {
                    // Check to see if this is passed in using
                    // the CloudEvents schema
                    if (IsCloudEvent(jsonContent))
                    {
                        return await HandleCloudEvent(jsonContent);
                    }

                    return await HandleGridEvents(jsonContent);
                }

                return BadRequest();
            }
        }

        #endregion

        #region Private Methods

        private async Task<JsonResult> HandleValidation(string jsonContent)
        {
            var gridEvent =
                JsonConvert.DeserializeObject<List<GridEvent<Dictionary<string, string>>>>(jsonContent)
                    .First();

            await this._hubContext.Clients.All.SendAsync(
                "gridupdate",
                gridEvent.Id,
                gridEvent.EventType,
                gridEvent.Subject,
                gridEvent.EventTime.ToLongTimeString(),
                jsonContent.ToString());

            // Retrieve the validation code and echo back.
            var validationCode = gridEvent.Data["validationCode"];
            return new JsonResult(new
            {
                validationResponse = validationCode
            });
        }

        private async Task<IActionResult> HandleGridEvents(string jsonContent)
        {
            var events = JArray.Parse(jsonContent);
            foreach (var e in events)
            {
                // Invoke a method on the clients for 
                // an event grid notiification.                        
                var details = JsonConvert.DeserializeObject<GridEvent<dynamic>>(e.ToString());
                await this._hubContext.Clients.All.SendAsync(
                    "gridupdate",
                    details.Id,
                    details.EventType,
                    details.Subject,
                    details.EventTime.ToLongTimeString(),
                    e.ToString());

                switch (details.EventType.ToLower())
                {
                    case "microsoft.communication.advancedmessagereceived":
                        return HandleAdvancedMessageReceivedEvent(details);
                        break;
                    case "microsoft.communication.advancedmessagedeliverystatusupdated":
                        return HandleAdvancedMessageDeliveryStatusUpdatedEvent(details);
                        break;
                    case "microsoft.communication.aigeneratedmessagesent":
                        return HandleAIMessageSentEvent(details);
                        break;
                    case "microsoft.communication.aifunctioncallrequested":
                        return HandleAIFunctionCallRequestedEvent(details);
                        break;
                    case "microsoft.communication.aidisengaged":
                        return HandleAIDisengagedEvent(details);
                        break;
                    case "microsoft.communication.experimental":
                    case "microsoft.communication.experimentalevent":
                        return HandleExperimentalAIEvents(details);
                        break;
                    default:
                        throw new Exception($"Unknown event type: {details.EventType}");
                }
            }

            return Ok();
        }

        private IActionResult HandleAIDisengagedEvent(GridEvent<dynamic> details)
        {
            // Deserialize details.Data into AIDisengagedEventData. 
            AIDisengagedEventData eventData = JsonConvert.DeserializeObject<AIDisengagedEventData>(details.Data.ToString());

            // If this event is not for the current selected combination of 'To' and 'ChannelRegistrationId', then ignore it.
            if (!environmentManagerService.IsEventForCurrentSelectedParams(eventData.To, eventData.ChannelRegistrationId.ToString()))
            {
                return Ok();
            }

            var disengagedReason = eventData.AIDisengagementReason;
            Console.WriteLine($"AI Disengaged: {disengagedReason}");

            return Ok();
        }

        private IActionResult HandleExperimentalAIEvents(GridEvent<dynamic> details)
        {
            // Deserialize details.Data into AIEventType. Get the OpenAIEventType and return the appropriate view.
            var eventData = JsonConvert.DeserializeObject<AIEventType>(details.Data.ToString());
            var openAIEventType = eventData.OpenAIEventType;
            switch (openAIEventType)
            {
                case OpenAiEventType.AIFunctionCallRequested:
                    return HandleAIFunctionCallRequestedEvent(details);
                    break;
                case OpenAiEventType.AIGeneratedMessageSent:
                    break;
                case OpenAiEventType.AIDisengaged:
                    break;
            }

            return Ok();
        }

        private IActionResult HandleAIFunctionCallRequestedEvent(GridEvent<dynamic> details)
        {
            // Deserialize details.Data into AIFunctionCallRequestedEventData. 
            AIFunctionCallRequestedEventData eventData = JsonConvert.DeserializeObject<AIFunctionCallRequestedEventData>(details.Data.ToString());

            // If this event is not for the current selected combination of 'To' and 'ChannelRegistrationId', then ignore it.
            if (!environmentManagerService.IsEventForCurrentSelectedParams(eventData.To, eventData.ChannelRegistrationId.ToString()))
            {
                return Ok();
            }

            // Extract the function name and parameters from the eventData.
            var funcName = eventData.FunctionName;
            var parameters = eventData.FunctionParameters.ToString();

            var availableFunctions = PatientRegistrationMethods.GetAvailableFunctions();
            var availableFunction = availableFunctions[funcName];

            // Invoke the function and return the result.
            var functionArgs = JsonExtractionUtils.SafeExtractJsonFromLLMResponse(availableFunction.Item2, parameters);
            object[] functionArgsArray = JsonExtractionUtils.GetPropertiesAsObjectArrayForType(availableFunction.Item2, functionArgs);
            PatientRegistrationMethods.FunctionResponse functionResultData = availableFunction.Item1.Invoke(null, functionArgsArray.ToArray()) as PatientRegistrationMethods.FunctionResponse;

            // if funcName is EndOfConversation, then additionally disengage AI also
            if (funcName.Equals(nameof(PatientRegistrationMethods.DetectedEndOfConversation), StringComparison.OrdinalIgnoreCase))
            {
                var resultOfDeelevate = RedirectToAction(
                    "deelevate",
                    "Message",
                    new
                    {
                        initialMessage = functionArgsArray?[0]?.ToString(),
                    });

                return resultOfDeelevate;
            }
            else
            {
                var resultOfDeliverFunctionResult = RedirectToAction(
                    "DeliverFunctionResult",
                    "Message",
                    new
                    {
                        functionResult = JsonConvert.SerializeObject(functionResultData),
                        functionName = funcName
                    });

                return resultOfDeliverFunctionResult;
            }
        }

        private IActionResult HandleAIMessageSentEvent(GridEvent<dynamic> details)
        {
            // Deserialize details.Data into AIMessageSentEventData. 
            var eventData = JsonConvert.DeserializeObject<AIMessageSentEventData>(details.Data.ToString());

            // If this event is not for the current selected combination of 'To' and 'ChannelRegistrationId', then ignore it.
            if (!environmentManagerService.IsEventForCurrentSelectedParams(eventData.To, eventData.ChannelRegistrationId.ToString()))
            {
                return Ok();
            }

            // print Content
            var content = eventData.Content;
            Console.WriteLine($"Content: {content}");

            return Ok();
        }

        private IActionResult HandleAdvancedMessageDeliveryStatusUpdatedEvent(GridEvent<dynamic> details)
        {
            return Ok();
        }

        private IActionResult HandleAdvancedMessageReceivedEvent(GridEvent<dynamic> details)
        {
            AdvancedMessageReceivedEventData eventData = JsonConvert.DeserializeObject<AdvancedMessageReceivedEventData>(details.Data.ToString());

            // If this event is not for the current selected combination of 'To' and 'ChannelRegistrationId', then ignore it.
            if (!environmentManagerService.IsEventForCurrentSelectedParams(eventData.From, eventData.To))
            {
                return Ok();
            }

            if (eventData.Content is not null)
            {
                ViewData["Message"] = ViewData["Message"]?.ToString() + $"\nCustomer: {eventData.Content}";

                IList<string> escalationWords = new List<string> { "urgent", "escalate", "emergency" };
                IList<string> deescalationWords = new List<string> { "resolved", "bye", "resolution" };

                if (escalationWords.Any(word => eventData.Content.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    return RedirectToAction("elevate", "Message", new { initialMessage = eventData.Content });
                }
                else if (deescalationWords.Any(word => eventData.Content.Contains(word, StringComparison.OrdinalIgnoreCase)))
                {
                    return RedirectToAction("deelevate", "Message", new { initialMessage = eventData.Content });
                }
            }

            return Ok();
        }

        private async Task<IActionResult> HandleCloudEvent(string jsonContent)
        {
            var details = JsonConvert.DeserializeObject<CloudEvent<dynamic>>(jsonContent);
            var eventData = JObject.Parse(jsonContent);

            await this._hubContext.Clients.All.SendAsync(
                "gridupdate",
                details.Id,
                details.Type,
                details.Subject,
                details.Time,
                eventData.ToString()
            );

            return Ok();
        }

        private static bool IsCloudEvent(string jsonContent)
        {
            // Cloud events are sent one at a time, while Grid events
            // are sent in an array. As a result, the JObject.Parse will 
            // fail for Grid events. 
            try
            {
                // Attempt to read one JSON object. 
                var eventData = JObject.Parse(jsonContent);

                // Check for the spec version property.
                var version = eventData["specversion"].Value<string>();
                if (!string.IsNullOrEmpty(version)) return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return false;
        }

        #endregion
    }
}