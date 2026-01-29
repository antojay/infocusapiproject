using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi870HeaderRecord
    {
        public String CardCode
        {
            get;
            set;
        }
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
        public String Department
        {
            get;
            set;
        }
        public String VendorNumber
        {
            get;
            set;
        }
        public String ReplenishmentNumber
        {
            get;
            set;
        }
        public String BuyerName
        {
            get;
            set;
        }
        public String DeliveryPhoneNumber
        {
            get;
            set;
        }
        public String TruckLoadNumber
        {
            get;
            set;
        }
        public String CarrierCode
        {
            get;
            set;
        }
        public String ConditionDescription
        {
            get;
            set;
        }
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
        public String UserDefined01
        {
            get;
            set;
        }
        public String UserDefined02
        {
            get;
            set;
        }
        public String UserDefined03
        {
            get;
            set;
        }
        public String UserDefined04
        {
            get;
            set;
        }
        public String UserDefined05
        {
            get;
            set;
        }
        public String UserDefined06
        {
            get;
            set;
        }
        public String UserDefined07
        {
            get;
            set;
        }
        public String UserDefined08
        {
            get;
            set;
        }
        public String UserDefined09
        {
            get;
            set;
        }
        public String UserDefined10
        {
            get;
            set;
        }
        public Int32 ConfirmationNo
        {
            get;
            set;
        }

        // 06-27-2017 begin
        public String OrderBuyName
        {
            get;
            set;
        }
        public String OrderBuyAddr1
        {
            get;
            set;
        }
        public String OrderBuyAddr2
        {
            get;
            set;
        }
        public String OrderBuyCity
        {
            get;
            set;
        }
        public String OrderBuyState
        {
            get;
            set;
        }
        public String OrderBuyZip
        {
            get;
            set;
        }
        public String OrderBuyCountryCd
        {
            get;
            set;
        }
        public String JobNumber
        {
            get;
            set;
        }
        // 06-27-2017 end
        // 07-03-2017 
        public DateTime? ExpectedDeliveryDate
        {
            get;
            set;
        }
        public DateTime? ExpectedShipDate
        {
            get;
            set;
        }
        // 07-03-2017 end
        // 07-17-2017 begin
        public Decimal? ShipmentWeight
        {
            get;
            set;
        }
        // 07-17-2017 end

        //Here are the unique ones
        public DateTime AsnShipDate
        {
            get;
            set;
        }
        public String TransportationMethod
        {
            get;
            set;
        }
        public String BillOfLading
        {
            get;
            set;
        }

        public String Last870Status
        {
            get;
            set;
        }

        public String ReasonCode870
        {
            get;
            set;
        }

        private List<Edi870DetailRecord> _details = new List<Edi870DetailRecord>();
        public List<Edi870DetailRecord> Details
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
