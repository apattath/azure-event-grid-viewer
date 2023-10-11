namespace viewer.Models
{
    internal class AIDisengagedEventData : EventGridPayloadObject
    {
        /// <summary>
        /// Reason for diengaging AI
        /// </summary>
        public AIDisengagementReason AIDisengagementReason { get; set; }
    }
}