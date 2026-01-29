using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class AddSalesOrderRequest : BaseWebApiRequest
    {
        public SalesOrder SalesOrder
        {
            get;
            set;
        }
    }
}
