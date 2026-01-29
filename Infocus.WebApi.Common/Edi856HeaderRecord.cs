using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi856HeaderRecord
    {
        // 02-22-2022 begin
        public DateTime TrxDateTime // 03-10-2022 changed from string to datetime
        {
            get;
            set;
        }
        // 02-22-2022 end
        public String BillOfLading
        {
            get;
            set;
        }
        public String BuyerName
        {
            get;
            set;
        }
        public String CardCode
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

        // 07-17-2017 begin
        public Int32? ConfirmationNo
        {
            get;
            set;
        }
        // 07-17-2017 end
        public String Department
        {
            get;
            set;
        }
        // 02-09-2021 begin
        public Int32 DeliveryNumber
        {
            get;
            set;
        }
        // 02-09-2021 end
        public String DeliveryPhoneNumber
        {
            get;
            set;
        }

        // 07-17-2017 begin
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

        // 05-25-2021 begin
        public Decimal FreightCost
        {
            get;
            set;
        }
        // 05-25-2021 end

        public String JobNumber
        {
            get;
            set;
        }

        // 05-31-2017 begin
        public String Last870Status
        {
            get;
            set;
        }
        // 05-15-2021 begin
        public String MasterBOL
        {
            get;
            set;
        }
        // 05-25-2021 end
        public String OrderType
        {
            get;
            set;
        }

        // 07-17-2017 begin
        public String OrderBuyCode
        {
            get;
            set;
        }
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
        // 09-15-2023 begin
        public String PartnerId
        {
            get;
            set;
        }
        // 09-15-2023 end
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

        // 02-12-2019 begin
        public String ProNumber
        {
            get;
            set;
        }
        // 02-12-2019 end
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

        public String ReplenishmentNumber
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
        // 05-31-2017 end
        
        // 12-12-2023 begin
        public String ReceiverId
        {
            get;
            set;
        }
        public String ReceiverQual
        {
            get;
            set;
        }
        public String SenderId
        {
            get;
            set;
                    }
        public String SenderQual
        {
            get;
            set;
        }
        // 12-12-2023 end

        // 02-12-2019 begin
        public String ServiceLevel
        {
            get;
            set;
        }
        public Decimal? ShipmentCartons
        {
            get;
            set;
        }
        public String ShipMethod
        {
            get;
            set;
        }
        // 02-12-2019 end
        // 10-31-2019 begin
        public String ShipmentNumber
        {
            get;
            set;
        }
        // 10-31-2019 end

        // 03-04-2019 begin
        public String ShipFromAddress1
        {
            get;
            set;
        }
        public String ShipFromAddress2
        {
            get;
            set;
        }
        public String ShipFromCity
        {
            get;
            set;
        }
        public String ShipFromCode
        {
            get;
            set;
        }
        public String ShipFromCountry
        {
            get;
            set;
        }
        public String ShipFromName
        {
            get;
            set;
        }
        public String ShipFromState
        {
            get;
            set;
        }
        public String ShipFromZip
        {
            get;
            set;
        }
        // 03-04-2019 end

        // 07-08-2019 begin
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
        public String ShipToAttention
        {
            get;
            set;
        }
        public String ShipToCity
        {
            get;
            set;
        }
        public String ShipToCountry
        {
            get;
            set;
        }
        public String ShipToLocationCode
        {
            get;
            set;
        }
        public String ShipToName
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
        public Double? ShipmentVolume
        {
            get;
            set;
        }
        public Decimal? ShipmentWeight
        {
            get;
            set;
        }
        public String ShipmentZone
        {
            get;
            set;
        }
        // 02-09-2021 begin
        public String Structure
        {
            get;
            set;
        }
        // 02-08-2021 end
        // 08-30-2017 begin
        public String TermsBasisCode
        {
            get;
            set;
        }
        public Int32? TermsDayofMonth
        {
            get;
            set;
        }
        public String TermsDescription
        {
            get;
            set;
        }
        public Int32? TermsDiscount
        {
            get;
            set;
        }
        public Int32? TermsDiscountDays
        {
            get;
            set;
        }
        public DateTime? TermsNetDueDate
        {
            get;
            set;
        }
        public Int32? TermsNetDays
        {
            get;
            set;
        }
        public String TermsType
        {
            get;
            set;
        }
        // 08-30-2017 end

        // 07-01-2019 begin
        public String TrackingProNo
        {
            get;
            set;
        }
        // 07-01-2019 end
        public String TransportationMethod // U=Parcel, M=Motor, R=Rail, LT= LTL, etc.
        {
            get;
            set;
        }
        // 07-17-2019 begin
        public String TransportRouting
        {
            get;
            set;
        }

        // 07-17-2019 end

        public String TruckLoadNumber
        {
            get;
            set;
        }
        public String TrxPurpose
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
        public String VendorNumber
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

        // private List<Edi856ItemDetailRecord> _details = new List<Edi856ItemDetailRecord>();
        private List<object> _details = new List<object>();
        // public List<Edi856ItemDetailRecord> Details
        public List<object> Details
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
