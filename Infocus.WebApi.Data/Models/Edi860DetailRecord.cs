using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi860DetailRecord")]
    public partial class Edi860DetailRecord
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
        public virtual Edi860HeaderRecord Edi860HeaderRecord
        {
            get;
            set;
        }

        public Int32 LineNumber
        {
            get;
            set;
        }

        public String ChangeTypeCode
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
