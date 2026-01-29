using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi820HeaderRecord")]
    public partial class Edi820HeaderRecord
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
        public Int32 PaymentKey
        {
            get;
            set;
        }
        public Boolean ProcessedReturn820
        {
            get;
            set;
        }
        public DateTime? Processed820DateTime
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
        public Double PaymentAmount
        {
            get;
            set;
        }
        public String PaymentMethod
        {
            get;
            set;
        }

        public String PaymentAccount
        {
            get;
            set;
        }

        public String TrxHandlingCd
        {
            get;
            set;
        }

        public String CreditDebit
        {
            get;
            set;
        }
        public String ReferenceIdCd
        {
            get;
            set;
        }

        public String ReferenceId
        {
            get;
            set;
        }

        public DateTime? PaymentDate
        {
            get;
            set;
        }

        public String Payee
        {
            get;
            set;
        }

        public String VendorNumber
        {
            get;
            set;
        }

        public String Payer
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

    
        public virtual ICollection<Edi820DetailRecord> Details
        {
            get;
            set;
        }
    }
}
