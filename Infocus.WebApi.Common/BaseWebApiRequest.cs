using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public abstract class BaseWebApiRequest
    {
        private SecurityInformation _securityInformation = new SecurityInformation();
        public SecurityInformation SecurityInformation
        {
            get
            {
                return _securityInformation;
            }
            set
            {
                _securityInformation = value;
            }
        }
    }
}
