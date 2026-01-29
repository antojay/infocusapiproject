using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi940DetailRecord")]
    public partial class Edi940DetailRecord
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
        public virtual Edi940HeaderRecord Edi940HeaderRecord
        {
            get;
            set;
        }

        public Int32 LineNumber
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
        public String OldItemCode
        {
            get;
            set;
        }
        public String ItemUPC
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
        public String ItemDescription
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
        public String VendorItemDescription
        {
            get;
            set;
        }
        public String PackingNotes
        {
            get;
            set;
        }
        public String PackSize
        {
            get;
            set;
        }
        public String InnerPackSize
        {
            get;
            set;
        }

        public Double? GrossPkgWeight
        {
            get;
            set;
        }   

        public String Comments
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
