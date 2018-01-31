using Newtonsoft.Json.Linq;
using System;
using Salesforce.SDK.Net;

namespace Salesforce.SDK.Utilities
{
    public class ErrorResponseException : Exception
    {
        public ErrorResponseException(HttpCall call)
        {
            ErrorResponseBody = call.ResponseBody;
            try
            {
                var reponseArray = JArray.Parse(call.ResponseBody);
                Error = reponseArray[0].ToObject<ErrorResponse>();
            }
            catch
            {

            }
        }

        public override string Message
        {
            get
            {
                return Error != null
                        ? $"Message: {Error.Message.Trim()} \nErrorCode:{Error.ErrorCode}"
                        : ErrorResponseBody;
            }
        }

        public ErrorResponse Error { get; private set; }
        public string ErrorResponseBody { get; private set; }
    }
}
