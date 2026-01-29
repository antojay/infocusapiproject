using System;
using System.Runtime.Serialization;

namespace Infocus.Common
{
    public class InfocusException : Exception
    {
        public InfocusException() : base()
        {
        }
        public InfocusException(string message)
            : base(message)
        {
            
        }
        public InfocusException(string message, Exception innerException)
            : base(message, innerException)
        {
            
        }
        protected InfocusException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            
        }
         
    }
}
