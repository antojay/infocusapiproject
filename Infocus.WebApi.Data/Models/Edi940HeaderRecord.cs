using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi940HeaderRecord")]
    public partial class Edi940HeaderRecord
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
        public DateTime? ReceivedDateTime
        {
            get;
            set;
        }

        public Boolean Processed
        {
            get;
            set;
        }
        public DateTime? ProcessedDateTime
        {
            get;
            set;
        }
        public Boolean Processed856
        {
            get;
            set;
        }
        public DateTime? Processed856DateTime
        {
            get;
            set;
        }

        public DateTime? Orig856ProcessedDateTime
        {
            get;
            set;
        }

        public Boolean HasOpen856
        {
            get;
            set;
        }

        public Boolean Processed810
        {
            get;
            set;
        }

        public DateTime? Processed810DateTime
        {
            get;
            set;
        }

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

        public String IgnoreTrxRequest
        {
            get;
            set;
        }
        public String TrxPurpose
        {
            get;
            set;
        }
        public String TrxSource
        {
            get;
            set;
        }

        /****************************/
        /*  End System Information  */
        /****************************/
        [MaxLength(30)]
        public String CardCode
        {
            get;
            set;
        }

        [MaxLength(30)]
        public String SBOCardCode
        {
            get;
            set;
        }

        [MaxLength(100)]
        public String OrderNumber
        {
            get;
            set;
        }

        [MaxLength(100)]
        public String DepositorOrderNo
        {
            get;
            set;
        }

        [MaxLength(100)]
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
        public DateTime? CancelDate
        {
            get;
            set;
        }
        public DateTime? RequestedShipDate
        {
            get;
            set;
        }
        public String TransactionType
        {
            get;
            set;
        }

        public String ShipId
        {
            get;
            set;
        }

        [MaxLength(50)]
        public String MerchTypeCd
        {
            get;
            set;
        }
        public String StoreNumber
        {
            get;
            set;
        }
        public String CustCode3PL
        {
            get;
            set;
        }
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
        public String ShipToContact
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipToPhoneNo
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipToIdCdQualifier
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipToIdentificationCd
        {
            get;
            set;
        }
        public String ShipFromName
        {
            get;
            set;
        }
        public String ShipFromAttention
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipFromLocationCode
        {
            get;
            set;
        }

        [MaxLength(100)]
        public String ShipFromAddress1
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipFromAddress2
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipFromCity
        {
            get;
            set;
        }
        [MaxLength(3)]
        public String ShipFromState
        {
            get;
            set;
        }
        [MaxLength(20)]
        public String ShipFromZip
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipFromCountry
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipFromIdCdQualifier
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String ShipFromIdentificationCd
        {
            get;
            set;
        }
        [MaxLength(255)]
        public String BillToName
        {
            get;
            set;
        }
        [MaxLength(255)]
        public String BillToAttention
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String BillToLocationCode
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
        [MaxLength(100)]
        public String BillToIdCdQualifier
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String BillToIdentificationCd
        {
            get;
            set;
        }
        public String WhsName
        {
            get;
            set;
        }
        public String WhsAttention
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String WhsLocationCode
        {
            get;
            set;
        }

        [MaxLength(100)]
        public String WhsAddress1
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String WhsAddress2
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String WhsCity
        {
            get;
            set;
        }
        [MaxLength(3)]
        public String WhsState
        {
            get;
            set;
        }
        [MaxLength(20)]
        public String WhsZip
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String WhsCountry
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String WhsIdCdQualifier
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String WhsIdentificationCd
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String DistributionCenter
        {
            get;
            set;
        }
        [MaxLength(100)]
        public String DepartmentNumber
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
        public String PaymentMethod
        {
            get;
            set;
        }

        public String TransportMethod
        {
            get;
            set;
        }
        public String Routing
        {
            get;
            set;
        }

        public String Notes
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

        [MaxLength(30)]
        public String InternalControlNo
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
        public virtual ICollection<Edi940DetailRecord> Details
        {
            get;
            set;
        }

/*        public virtual ICollection<Delivery> Shipments
        {
            get;
            set;
        }
        */
    }
}
