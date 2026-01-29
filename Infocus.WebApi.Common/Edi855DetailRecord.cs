using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi855DetailRecord 
    {
        public Int32 LineNumber
        {
            get;
            set;
        }
        public Double Quantity
        {
            get;
            set;
        }
        public String UnitOfMeasure
        {
            get;
            set;
        }
        public String VendorItemCode
        {
            get;
            set;
        }
        public String BuyerItemCode
        {
            get;
            set;
        }
         // 02-14-2019 begin
        public Decimal UnitPrice
        {
            get;
            set;
        }
        public String ItemUPC
        {
            get;
            set;
        }
        // 02-14-2019 end
        public String ItemDescription
        {
            get;
            set;
        }
        public Double QuantityShipped
        {
            get;
            set;
        }

        // 07-01-2019 begin
        public String OrderNumber
        {
            get;
            set;
        }
        // 07-01-2019 end
        public String Item855Status
        {
            get;
            set;
        }

        public String ItemReasonCode855
        {
            get;
            set;
        }
        public DateTime? ExpectedLnDeliveryDate
        {
            get;
            set;
        }
        /*
        public DateTime? ExpectedLnShipDate
        {
            get;
            set;
        }
     */
    }
}
