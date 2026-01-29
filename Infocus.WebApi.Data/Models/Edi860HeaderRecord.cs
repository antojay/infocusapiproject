using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi860HeaderRecord")]
    public partial class Edi860HeaderRecord
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
         public DateTime? PurchaseOrderDate
        {
            get;
            set;
        }
       
        public DateTime? ChangeRequestDate
        {
            get;
            set;
        }

        public String ChangeDateCode
        {
            get;
            set;
        }
        public DateTime? ChangeShipDate
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
       
        public virtual ICollection<Edi860DetailRecord> Details
        { 
            get; set;
        }
    }
}
