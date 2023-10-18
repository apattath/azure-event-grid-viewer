using System.Collections.Generic;
using System;
using viewer.Controllers;
using Azure.Communication.Messages;

namespace viewer
{
    public class EnvironmentSpecificParams
    {
        public TargetEnvironment TargetEnvironment;
        public string ChannelRegistrationId;
        public IList<string> RecipientList;
        public string AcsConnectionString;
        public string CpmEndpoint;
        public string AccessKey;
        public NotificationMessagesClient notificationMessagesClient;
    }

    public enum TargetEnvironment
    {
        LOCALINT = 0,
        LOCALPPE,
        INT,
        PPE,
        PROD
    }

    public class EnvironmentManagerService
    {
        private TargetEnvironment currentTargetEnvironment = TargetEnvironment.INT;
        private EnvironmentSpecificParams localIntParams = new EnvironmentSpecificParams()
        {
            TargetEnvironment = TargetEnvironment.LOCALINT,
            ChannelRegistrationId = "52b11371-748c-4757-a89c-911cd6b81aca",
            AcsConnectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_INT"),
            CpmEndpoint = "https://localhost:8997/",
            AccessKey = ParseAccessKeyFromConnectionString(Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_INT")),
        };

        private EnvironmentSpecificParams localPpeParams = new EnvironmentSpecificParams()
        {
            TargetEnvironment = TargetEnvironment.LOCALPPE,
            ChannelRegistrationId = "873a641f-637e-47bd-8cf0-6dc7bfb52a8f",
            AcsConnectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_PPE"),
            CpmEndpoint = "https://localhost:8997/",
            AccessKey = ParseAccessKeyFromConnectionString(Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_PPE")),
        };

        private EnvironmentSpecificParams intParams = new EnvironmentSpecificParams()
        {
            TargetEnvironment = TargetEnvironment.INT,
            ChannelRegistrationId = "52b11371-748c-4757-a89c-911cd6b81aca",
            AcsConnectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_INT"),
            CpmEndpoint = ParseEndpointFromConnectionString(Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_INT")),
            AccessKey = ParseAccessKeyFromConnectionString(Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_INT")),
        };

        private EnvironmentSpecificParams ppeParams = new EnvironmentSpecificParams()
        {
            TargetEnvironment = TargetEnvironment.PPE,
            ChannelRegistrationId = "873a641f-637e-47bd-8cf0-6dc7bfb52a8f",
            AcsConnectionString = Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_PPE"),
            CpmEndpoint = ParseEndpointFromConnectionString(Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_PPE")),
            AccessKey = ParseAccessKeyFromConnectionString(Environment.GetEnvironmentVariable("COMMUNICATION_SERVICES_CONNECTION_STRING_PPE")),
        };

        private EnvironmentSpecificParams currentSelectedParams = default;

        private string conversationId = default;
        public string ConversationId
        {
            get => conversationId;
            set
            {
                conversationId = value;
            }
        }   

        public EnvironmentManagerService()
        {
            currentSelectedParams = currentTargetEnvironment switch
            {
                TargetEnvironment.LOCALINT => localIntParams,
                TargetEnvironment.LOCALPPE => localPpeParams,
                TargetEnvironment.INT => intParams,
                TargetEnvironment.PPE => ppeParams,
                _ => throw new ArgumentException($"Invalid target environment: {currentTargetEnvironment}"),
            };
            conversationId = default;
        }

        public EnvironmentSpecificParams GetCurrentEnvironment() => currentSelectedParams;

        public void SetEnvironment(string environment, string channelRegistrationId, string phoneNumber)
        {
            currentTargetEnvironment = environment.ToLower() switch
            {
                "localint" => TargetEnvironment.LOCALINT,
                "localppe" => TargetEnvironment.LOCALPPE,
                "int" => TargetEnvironment.INT,
                "ppe" => TargetEnvironment.PPE,
                _ => throw new ArgumentException($"Invalid target environment: {environment}"),
            };

            currentSelectedParams = currentTargetEnvironment switch
            {
                TargetEnvironment.LOCALINT => localIntParams,
                TargetEnvironment.LOCALPPE => localPpeParams,
                TargetEnvironment.INT => intParams,
                TargetEnvironment.PPE => ppeParams,
                _ => throw new ArgumentException($"Invalid target environment: {currentTargetEnvironment}"),
            };

            conversationId = default;

            if (!string.IsNullOrWhiteSpace(currentSelectedParams.AcsConnectionString))
            {
                currentSelectedParams.notificationMessagesClient = new NotificationMessagesClient(currentSelectedParams.AcsConnectionString);
            }

            if (!string.IsNullOrWhiteSpace(channelRegistrationId))
            {
                currentSelectedParams.ChannelRegistrationId = channelRegistrationId;
            }
            else
            {
                throw new ArgumentException($"Invalid channel registration id: {channelRegistrationId}");
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                currentSelectedParams.RecipientList = new List<string> { phoneNumber };
            }
            else
            {
                throw new ArgumentException($"Invalid phone number: {phoneNumber}");
            }
        }

        private static string ParseEndpointFromConnectionString(string connectionString)
        {
            // split string using ; first and then by '=' and get the second element
            string[] connectionStringParts = connectionString.Split(';');
            string[] endpointParts = connectionStringParts[0].Split('=');
            return endpointParts[1];
        }

        private static string ParseAccessKeyFromConnectionString(string connectionString)
        {
            string[] parts = connectionString.Split(';');
            if (parts[1].StartsWith("accesskey="))
            {
                return parts[1].Substring("accesskey=".Length);
            }
            else
            {
                throw new ArgumentException("Connection string missing required 'accesskey' attribute.");
            }
        }

        internal bool IsEventForCurrentSelectedParams(string phoneNumber, string channelRegistrationId)
        {
            if (currentSelectedParams == default)
            {
                return false;
            }

            if (currentSelectedParams.RecipientList.Contains(phoneNumber) && 
                currentSelectedParams.ChannelRegistrationId.Equals(channelRegistrationId, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }
    }
}
