using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi180RDetailRecord
    {
        public Int32 LineNumber
        {
            get;
            set;
        }
        public Int32 ItemReference /*SAP B1 Line # */
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
        public String BuyerItemCode
        {
            get;
            set;
        }
        public String VendorItemCode
        {
            get;
            set;
        }
        public String ItemUPC
        {
            get;
            set;
        }

        public Double? UnitPrice
        {
            get;
            set;
        }
        public Double? RetailPrice
        {
            get;
            set;
        }
        public String ItemDescription
        {
            get;
            set;
        }
        public String ReturnCode
        {
            get;
            set;
        }
        public String ReturnReasonCode
        {
            get;
            set;
        }
        public Double? ReturnTotal
        {
            get;
            set;
        }
        public String PurchaseOrderReference
        {
            get;
            set;
        }
        public String IdAssigned
        {
            get;
            set;
        }
    }    
}
