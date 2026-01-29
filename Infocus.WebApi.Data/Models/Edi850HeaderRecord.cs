using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi850HeaderRecord")]
    public partial class Edi850HeaderRecord
    {
        /****************************/
        /* Begin System Information */
        /****************************/
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Int32 HeaderId
        {
            get;
            set;
        }
        public DateTime? RecordDate
        {
            get;
            set;
        }
        public Boolean Processed
        {
            get;
            set;
        }
        // 08-03-2020 begin
        public Boolean Processed753
        {
            get;
            set;
        }

        public DateTime? Processed753DateTime
        {
            get;
            set;
        }

        public String Last753TrxType
        {
            get;
            set;
        }
        // 08-03-2020 end
        public Boolean Processed856
        {
            get;
            set;
        }
        public Boolean Processed810
        {
            get;
            set;
        }
        // 08-20-2017 begin
        public Boolean Processed810C
        {
            get;
            set;
        }
        public DateTime? Processed810CDateTime
        {
            get;
            set;
        }
        // 08-20-2017 end
        public String ErrorMessage
        {
            get;
            set;
        }
        public Int32 SalesOrderKey
        {
            get;
            set;
        }
        public DateTime? ReceivedDateTime
        {
            get;
            set;
        }
        public DateTime? ProcessedDateTime
        {
            get;
            set;
        }
        public DateTime? Processed856DateTime
        {
            get;
            set;
        }
        // 02-25-2022 begin
        public DateTime? Orig856ProcessedDateTime
        {
            get;
            set;
        }
        // 02-25-2022 end

        public DateTime? Processed810DateTime
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
        // 05-31-2017 end
        // 08-26-2022 begin
        public String IgnoreTrxRequest
        {
            get;
            set;
        }
        // 08-26-2022 end
        // 09-25-2024 begin
        public String NonSAPSO
        {
            get;
            set;
        }
        // 09-25-2024 end
        // 01-17-2018 begin
        public Boolean Processed855
        {
            get;
            set;
        }
        public DateTime? Processed855DateTime
        {
            get;
            set;
        }
        // 02-25-2022 begin
        public DateTime? Orig855ProcessedDateTime
        {
            get;
            set;
        }
        // 02-25-2022 end
        // 03-11-2024 begin
        public Boolean? ProcessedPreSo855
        {
            get;
            set;
        }
        public DateTime? ProcessedPreSo855DateTime
        {
            get;
            set;
        }

        public DateTime? OrigProcessedPreSo855DateTime
        {
            get;
            set;
        }
        // 03-11-2024 end
        [MaxLength(30)]
        public String ContractNumber
        {
            get;
            set;
        }
        // 01-17-2018 end
        // 08-6-2019 begin
        public Boolean HasOpen856
        {
            get;
            set;
        }
        // 08-06-2019 end

        /****************************/
        /*  End System Information  */
        /****************************/
        [MaxLength(30)]
        public String CardCode
        {
            get;
            set;
        }
        // 01-17-2018 begin
        [MaxLength(30)]
        public String SBOCardCode
        {
            get;
            set;
        }
        // 01-17-2018 end
        [MaxLength(100)]
        public String PurchaseOrderReference
        {
            get;
            set;
        }
        // 01-17-2018 begin
        public String Last855Status
        {
            get;
            set;
        }
        // 01-17-2018 end

        // 05-31-2017 begin
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
        public DateTime? PurchaseOrderDate
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String Department
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String VendorNumber
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ReplenishmentNumber
        {
            get;
            set;
        }
        [MaxLength(255)]
        public String BuyerName
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String DeliveryPhoneNumber
        {
            get;
            set;
        }
        // 07-01-2019 begin
        public String DeliveryFaxNo
        {
            get;
            set;
        }

        public String PoolPointId
        {
            get;
            set;
        }

        public String PoolPointName
        {
            get;
            set;
        }

        public String CrossDockId
        {
            get;
            set;
        }

        public String CrossDockName
        {
            get;
            set;
        }
        public String DeliveryAgentId
        {
            get;
            set;
        }

        public String DeliveryAgentName
        {
            get;
            set;
        }
        // 07-01-2019 end

        [MaxLength(120)]
        public String TruckLoadNumber
        {
            get;
            set;
        }
        [MaxLength(100)]
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
        [MaxLength(255)]
        public String ShipToName
        {
            get;
            set;
        }
        [MaxLength(255)]
        public String ShipToAttention
        {
            get;
            set;
        }
        [MaxLength(100)]
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
        [MaxLength(100)]
        public String ShipToAddress1
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipToAddress2
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipToCity
        {
            get;
            set;
        }
        [MaxLength(3)]
        public String ShipToState
        {
            get;
            set;
        }
        [MaxLength(20)]
        public String ShipToZip
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipToCountry
        {
            get;
            set;
        }
        // 11-02-2016 end

        // 03-25-2019 begin

        [MaxLength(50)]
        public String CustomerOrderNumber
        {
            get;
            set;
        }
        [MaxLength(30)]
        public String InternalControlNo
        {
            get;
            set;
        }
        public Double? PriorityShippingFee
        {
            get;
            set;
        }

        // 03-25-2019 end

        // 03-06-2019 begin
        [MaxLength(255)]
        public String BillToName
        {
            get;
            set;
        }

        [MaxLength(100)]
        public String BillToAddress1
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String BillToAddress2
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String BillToCity
        {
            get;
            set;
        }
        [MaxLength(3)]
        public String BillToState
        {
            get;
            set;
        }
        [MaxLength(20)]
        public String BillToZip
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String BillToCountry
        {
            get;
            set;
        }
        [MaxLength(50)]
        public String BillToCode
        {
            get;
            set;
        }
        // 03-06-2019 end

        // 07-01-2019 begin
        public String BillToContact
        {
            get;
            set;
        }

        public String BillToPhoneNo
        {
            get;
            set;
        }

        public String BillToFaxNo
        {
            get;
            set;
        }

        public String BillToEmail
        {
            get;
            set;
        }
        // 07-01-2019 end

        // 07-08-2019 begin
        public String BillingId
        {
            get;
            set;
        }

        public String BillingText
        {
            get;
            set;
        }

        public String ShippingAccount
        {
            get;
            set;
        }

        // 02-12-2022 begin
        public String Shipper3PL
        {
            get;
            set;
        }
        // 02-12-2022 end
        public String SupplierName
        {
            get;
            set;
        }

        public String SupplierFax
        {
            get;
            set;
        }

        public String SupplierPhone
        {
            get;
            set;
        }
        public Double? TotalExpectedCost
        {
            get;
            set;
        }
        // 07-08-2019 end

        // 01-29-2021 begin
        public String ProcessingCode
        {
            get;
            set;
        }
        public String PackSlipTemplate
        {
            get;
            set;
        }

        public String FileId
        {
            get;
            set;
        }
        // 01-29-2021 end

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

        // 07-01-2019 begin
        public DateTime? ShipAfterDate
        {
            get;
            set;
        }

        public DateTime? ShipCutOffDate
        {
            get;
            set;
        }

        public String BrandName
        {
            get;
            set;
        }
        // 07-01-2019 end
        // 09-14-2021 begin
        public String BusinessRuleCd
        {
            get;
            set;
        }

        public String ShipmentCd
        {
            get;
            set;
        }
        // 09-14-2021 end
        public String PaymentMethod
        {
            get;
            set;
        }
        // 07-03-2019 begin
        public String AllowanceCode
        {
            get;
            set;
        }
        public Double? AllowanceRate
        {
            get;
            set;
        }
        // 07-03-2019
        // 06-01-2019 begub
        public String PaymentDescription
        {
            get;
            set;
        }
        // 06-01-2019 end
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
        // 07-17-2017 begin
        [MaxLength(100)]
        public String OrderBuyCode
        {
            get;
            set;
        }
        // 07-17-2017 end
        // 06-27-2017 begin
        [MaxLength(75)]
        public String OrderBuyName
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String OrderBuyAddr1
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String OrderBuyAddr2
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String OrderBuyCity
        {
            get;
            set;
        }
        [MaxLength(3)]
        public String OrderBuyState
        {
            get;
            set;
        }
        [MaxLength(20)]
        public String OrderBuyZip
        {
            get;
            set;
        }
        [MaxLength(3)]
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
        // 07-03-2017 begin
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
        // 08-30-2017 begin
        public String TermsType
        {
            get;
            set;
        }
        public String TermsBasisCode
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
        public Int32? TermsDayofMonth
        {
            get;
            set;
        }
        // 08-30-2017 end
        // 02-03-19 begin  
        public String BOLNotes
        {
            get;
            set;
        }

        public String PackingNotes
        {
            get;
            set;
        }

        public String TransportMethod
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
        public String ShipmentZone
        {
            get;
            set;
        }
        public String WebSite
        {
            get;
            set;
        }
        // 07-17-2019 end
        public String ServiceLevel
        {
            get;
            set;
        }

        public String HandlingCode
        {
            get;
            set;
        }

        // 06-01-2019 begin
        public String HandlingDescription
        {
            get;
            set;
        }

        public String ReferenceId
        {
            get;
            set;
        }

        public String MessageText
        {
            get;
            set;
        }
        // 06-01-2019 end
        //10-27-2019 begin
        public String VendorNote1
        {
            get;
            set;
        }
        public String VendorNote2
        {
            get;
            set;
        }
        // 10-27-2019 end
        public String DeliveryContact
        {
            get;
            set;
        }

        public String DeliveryEmail
        {
            get;
            set;
        }
        // 08-10-2021 begin
        public String ThirdPtyBTName
        {
            get;
            set;
        }
        public String ThirdPtyBTAddr
        {
            get;
            set;
        }
        public String ThirdPtyBTCity
        {
            get;
            set;
        }
        public String ThirdPtyBTState
        {
            get;
            set;
        }
        public String ThirdPtyBTZip
        {
            get;
            set;
        }
        public String ThirdPtyBTCountry
        {
            get;
            set;
        }
        // 08-10-2021 end
        // 02-12-2022 begin
        public String ThirdPtyAcct
        {
            get;
            set;
        }
        public String OrderPriority3PL
        {
            get;
            set;
        }
        // 02-12-2022 end
        public Boolean Processed860
        {
            get;
            set;
        }
        public DateTime? Processed860DateTime
        {
            get;
            set;
        }
        public String Last860Status
        {
            get;
            set;
        }
        // 02-02-2019 end
        // 05-12-2021 begin
        public String CustomerRef
        {
            get;
            set;
        }
        public String OrderType3PL
        {
            get;
            set;
        }

        public String ConsumerPO
        {
            get;
            set;
        }

        public DateTime? ConsumerPODate
        {
            get;
            set;
        }

        public String FreightBillType
        {
            get;
            set;
        }

        public String ShipFromStore
        {
            get;
            set;
        }
        public String ShipFromName
        {
            get;
            set;
        }
        public String POPurposeCode
        {
            get;
            set;
        }
        public String POType3PL
        {
            get;
            set;
        }
        public String PackingStoreCode
        {
            get;
            set;
        }
        public String ReceiptId
        {
            get;
            set;
        }
        public String CustCode3PL
        {
            get;
            set;
        }
        public String VendorMessage
        {
            get;
            set;
        }
        public String OrderMessage
        {
            get;
            set;
        }
        public String GiftMessage
        {
            get;
            set;
        }
        public String DiscountDescr
        {
            get;
            set;
        }

        public String ItemTaxType
        {
            get;
            set;
        }
        public String TaxComponent
        {
            get;
            set;
        }
        // 05-12-2021 end
        public virtual ICollection<Edi850DetailRecord> Details
        {
            get;
            set;
        }

        //12-28-2022 begin
        public virtual ICollection<Delivery> Deliveries
        {
            get;
            set;
        }
        // 12-28-2022 end
    }
}
