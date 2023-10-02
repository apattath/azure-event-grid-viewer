using Microsoft.VisualBasic;
using Newtonsoft.Json;
using System;

namespace viewer.Models
{
    internal class AdvancedMessageReceivedEventData
    {
        [JsonProperty("content")]
        public string Content { get; set; }

        [JsonProperty("channelType")]
        public string ChannelType { get; set; }

        [JsonProperty("from")]
        public string From { get; set; }

        [JsonProperty("to")]
        public string To { get; set; }

        [JsonProperty("receivedTimestamp")]
        public DateTime ReceivedTimestamp { get; set; }
    }
}