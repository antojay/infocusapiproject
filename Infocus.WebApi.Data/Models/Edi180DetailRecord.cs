using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi180DetailRecord")]
    public partial class Edi180DetailRecord
    {
        /****************************/
        /* Begin System Information */
        /****************************/
        [Key]
        public Int32 DetailId
        {
            get;
            set;
        }
        public Int32 HeaderId
        {
            get;
            set;
        }
        public virtual Edi180HeaderRecord Edi180HeaderRecord
        {
            get;
            set;
        }

        public Int32 LineNumber
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

        public String ItemReference
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
    }
}
