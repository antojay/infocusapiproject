using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;

namespace Infocus.Framework.SapBone
{
    public sealed class BoneFrameworkException : Exception
    {
        public BoneFrameworkException()
        {
            
        }
        public BoneFrameworkException(string message)
            : base(message)
        {
            
        }
        public BoneFrameworkException(string message, Exception innerException)
            : base(message, innerException)
        {
            
        }
    }
}
