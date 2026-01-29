using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public abstract class BaseUserDefinedObject
    {
        public void ResetUserDefinedVariables()
        {
            UserDefined1 = String.Empty;
            UserDefined2 = String.Empty;
            UserDefined3 = String.Empty;
            UserDefined4 = String.Empty;
            UserDefined5 = String.Empty;
        }
        public virtual String UserDefined1
        {
            get;
            set;
        }
        public virtual String UserDefined2
        {
            get;
            set;
        }
        public virtual String UserDefined3
        {
            get;
            set;
        }
        public virtual String UserDefined4
        {
            get;
            set;
        }
        public virtual String UserDefined5
        {
            get;
            set;
        }
    }
}
