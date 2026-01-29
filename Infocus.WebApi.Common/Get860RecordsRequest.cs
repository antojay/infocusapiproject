using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get860RecordsRequest : BaseWebApiRequest
    {
        public String CardCode
        {
            get;
            set;
        }
    }
}
