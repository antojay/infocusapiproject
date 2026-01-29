using System;
using System.Runtime.Serialization;

namespace Infocus.WebApi.Common
{
    public class WebApiException : Exception
    {
        public WebApiException()
        {
        }

        public WebApiException(String message) : base(message)
        {
        }

        public WebApiException(String message, Exception innerException) : base(message, innerException)
        {
        }

        protected WebApiException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
