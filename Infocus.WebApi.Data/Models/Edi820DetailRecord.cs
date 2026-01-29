using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi820DetailRecord")]
    public partial class Edi820DetailRecord
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
        public virtual Edi820HeaderRecord Edi820HeaderRecord
        {
            get;
            set;
        }

        public String EntityAssignedId
        {
            get;
            set;
        }

        public String EntityIdCd
        {
            get;
            set;
        }

        public String EntityType
        {
            get;
            set;
        }

        public String EntityName
        {
            get;
            set;
        }

        public Double AdjustmentAmt
        {
            get;
            set;
        }

        public String AdjustmentReasonCd
        {
            get;
            set;
        }

        public String AdjustmentRefId
        {
            get;
            set;
        }

        public DateTime? AdjustmentDate
        {
            get;
            set;
        }

        public String PurchaseOrderReference
        {
            get;
            set;
        }

        public String ReturnAuthNo
        {
            get;
            set;
        }

        public String InvoiceNo
        {
            get;
            set;
        }

        public DateTime? InvoiceDate
        {
            get;
            set;
        }

        public Double AmountPaid
        {
            get;
            set;
        }

        public Double InvoiceAmount
        {
            get;
            set;
        }

        public Double DiscountAmount
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
