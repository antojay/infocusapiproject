using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi810CHeaderRecord
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
        // 05-30-2017 begin
        public Boolean Processed870
        {
            get;
            set;
        }
        public DateTime? Processed870DateTime
        {
            get;
            set;
        }
        public String Last870Status
        {
            get;
            set;
        }

        public String TrxPurpose
        {
            get;
            set;
        }

        public String OrderType
        {
            get;
            set;
        }
        // 05-30-2017 end
        // 01-17-2018 begin
        public Boolean vProcessed855
        {
            get;
            set;
        }
        public DateTime? Processed855DateTime
        {
            get;
            set;
        }
        public String Last855Status
        {
            get;
            set;
        }
        // 01-17-2018 end
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

        // 11-02-2016 begin
        public String ShipToStoreLocation
        {
            get;
            set;
        }
        // 11-02-2016 end
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

        public Double FreightCharge
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
        public String FinalShipToLocation
        {
            get;
            set;
        }
        public String TermsType
        {
            get;
            set;
        }
        public String TermsDescription
        {
            get;
            set;
        }
        public Int32? NetDaysDue
        {
            get;
            set;
        }
        public Double TotalDue
        {
            get;
            set;
        }
        public String ShipmentPaymentMethod
        {
            get;
            set;
        }
        public String BillOfLading
        {
            get;
            set;
        }
        public String CarrierName
        {
            get;
            set;
        }
        public Int32 CreditMemoNumber
        {
            get;
            set;
        }

        // 05-15-2019 begin
        public Int32 InvoiceNumber
        {
            get;
            set;
        }
        // 05-15-2019 end

        // 07-17-2017 begin
        public String OrderBuyCode
        {
            get;
            set;
        }
        public Int32? ConfirmationNo
        {
            get;
            set;
        }
        // 07-17-2017 end
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
        // 07-17-2017 begin
        public String DocumentType
        {
            get;
            set;
        }
        public Decimal? ShipmentWeight
        {
            get;
            set;
        }
        // 07-17-2017 end

        // 08-18-2017 begin
        public DateTime? DiscountDueDt
        {
            get;
            set;
        }
        public DateTime? CreditMemoDueDt
        {
            get;
            set;
        }
        public Decimal? TermsDiscountAmt
        {
            get;
            set;
        }
        // 08-18-2017 end

        private List<Edi810CDetailRecord> _details = new List<Edi810CDetailRecord>();
        public List<Edi810CDetailRecord> Details
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
