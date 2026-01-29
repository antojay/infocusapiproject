using Infocus.WebApi.Common;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Infocus.WebApi.Controllers
{
    public abstract class BaseWebApiController : ApiController
    {
        [HttpGet]
        public String Ping()
        {
            return "OK";
        }

        protected Boolean Authorize(BaseWebApiRequest request)
        {
            String masterUserName = ConfigurationManager.AppSettings["MasterUserName"];
            String masterPassword = ConfigurationManager.AppSettings["MasterPassword"];
            if(String.IsNullOrWhiteSpace(masterUserName))
            {
                throw new WebApiException("No Master User Name found in APPSETTINGS");
            }
            if(String.IsNullOrWhiteSpace(masterPassword))
            {
                throw new WebApiException("No Master Password found in APPSETTINGS");
            }
            if(masterUserName.Equals(request.SecurityInformation.UserName, StringComparison.InvariantCultureIgnoreCase)
                && masterPassword == request.SecurityInformation.Password)
            {
                return true;
            }
            return false;
        }
    }
}
