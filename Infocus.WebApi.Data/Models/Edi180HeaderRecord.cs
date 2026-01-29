using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi180HeaderRecord")]
    public partial class Edi180HeaderRecord
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
        public String ErrorMessage
        {
            get;
            set;
        }
        public Int32 ReturnOrderKey
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

        public Boolean ProcessedReturn180
        {
            get;
            set;
        }
        public DateTime? Processed180DateTime
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
        public String PurchaseOrderReference
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

        [MaxLength(100)]
        public String VendorNumber
        {
            get;
            set;
        }
        public DateTime? RequestDate
        {
            get;
            set;
        }
        public String ReferenceId
        {
            get;
            set;
        }
        public String ChargeId
        {
            get;
            set;
        }

        public String ChargeCode
        {
            get;
            set;
        }

        public Double ChargeAmount
        {
            get;
            set;
        }
        public String ChargeReference
        {
            get;
            set;
        }
        public String TrxStatus
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

    
        public virtual ICollection<Edi180DetailRecord> Details
        {
            get;
            set;
        }
    }
}
