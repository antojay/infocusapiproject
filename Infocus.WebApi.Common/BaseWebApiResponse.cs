using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public abstract class BaseWebApiResponse
    {
        public Boolean Successful
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }
}
