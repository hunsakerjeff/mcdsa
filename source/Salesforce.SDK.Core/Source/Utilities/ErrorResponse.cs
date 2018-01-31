using Newtonsoft.Json;

namespace Salesforce.SDK.Utilities
{
    public class ErrorResponse
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }
    }
}