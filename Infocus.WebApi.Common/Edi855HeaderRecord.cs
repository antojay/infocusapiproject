using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi855HeaderRecord
    {
        public String CardCode
        {
            get;
            set;
        }
        // 02-22-2022 begin
        public DateTime TrxDateTime // 03-10-2022 changed from string to datetime
        {
            get;
            set;
        }
        // 02-22-2022 end
        // 02-09-2022 begin
        public String SalesOrder
        {
            get;
            set;
        }
        // 02-09-2022 end

        public String PurchaseOrderReference
        {
            get;
            set;
        }

        public DateTime? PurchaseOrderDate
        {
            get;
            set;
        }

        public String VendorNumber
        {
            get;
            set;
        }
        // 06-28-2019 begin
        public String ShipToName
        {
            get;
            set;
        }
        public String ShipToAttention
        {
            get;
            set;
        }
        public String ShipToLocationCode
        {
            get;
            set;
        }

        public String ShipToStoreLocation
        {
            get;
            set;
        }

        public String ShipToAddress1
        {
            get;
            set;
        }
        public String ShipToAddress2
        {
            get;
            set;
        }
        public String ShipToCity
        {
            get;
            set;
        }
        public String ShipToState
        {
            get;
            set;
        }

        public String ShipToZip
        {
            get;
            set;
        }
        public String ShipToCountry
        {
            get;
            set;
        }
        // 06-29-2019 end
        public DateTime? RequestedDeliveryDate
        {
            get;
            set;
        }
        public DateTime? RequestedShipDate
        {
            get;
            set;
        }
        public String PaymentMethod
        {
            get;
            set;
        }
        public String PromotionChargeCode
        {
            get;
            set;
        }
        public Int32 ConfirmationNo
        {
            get;
            set;
        }
        // 07-19-2019 begin
        public Double TotalDue
        {
            get;
            set;
        }
        public String ServiceLevel
        {
            get;
            set;
        }

        // 07-19-2019 end

        //Here are the unique ones
        public DateTime AsnShipDate
        {
            get;
            set;
        }

        private List<Edi855DetailRecord> _details = new List<Edi855DetailRecord>();
        public List<Edi855DetailRecord> Details
        {
            get
            {
                return _details;
            }
            set
            {
                _details = value;
            }
        }
    }
}
