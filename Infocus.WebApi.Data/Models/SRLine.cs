using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("RDN1")]
    public class SRLine
    {
        [Key, Column(Order = 0)]
        [ForeignKey("SOReturn")]
        public Int32 DocEntry
        {
            get;
            set;
        }
        [Key, Column(Order = 1)]
        public Int32 LineNum
        {
            get;
            set;
        }
        public Int32? BaseEntry
        {
            get;
            set;
        }
        public Int32? BaseType
        {
            get;
            set;
        }
        public Int32? BaseLine
        {
            get;
            set;
        }
        public Int32? TargetType
        {
            get;
            set;
        }
        public Int32? TrgetEntry
        {
            get;
            set;
        }

        public String ItemCode
        {
            get;
            set;
        }
        public String SubCatNum
        {
            get;
            set;
        }
        public String Dscription
        {
            get;
            set;
        }
        public String UomCode
        {
            get;
            set;
        }
        public Decimal Quantity
        {
            get;
            set;
        }
        public Decimal DelivrdQty
        {
            get;
            set;
        }
        public DateTime ShipDate
        {
            get;
            set;
        }

        public Decimal Price
        {
            get;
            set;
        }
        public String U_InfoW2PackNote
        {
            get;
            set;
        }
        public String U_InfoW2ItemUPC
        {
            get;
            set;
        }
        public String U_InfoW2Routing
        {
            get;
            set;
        }
        public String U_InfoW2ServLev
        {
            get;
            set;
        }
        public String U_InfoW2TrkText
        {
            get;
            set;
        }
        public String U_InfoW2DelConf
        {
            get;
            set;
        }
        public String U_InfoW2DelText
        {
            get;
            set;
        }
        public String U_InfoW2PurchaserItem
        {
            get;
            set;
        }
        public String U_InfoW2TrackNo
        {
            get;
            set;
        }
        public Decimal LineTotal
        {
            get;
            set;
        }
        [Column("U_InfoW2LNo")]
        public Int32 LineNumber850
        {
            get;
            set;
        }
        [Column("U_InfoItmStatus")]
        public String LineItemStatus
        {
            get;
            set;
        }
        [Column("U_InfoItmRsn")]
        public String LineItemRsnCd
        {
            get;
            set;
        }
        [Column("U_InfoW2ELnDelDate")]
        public DateTime? ExpectedLnDeliveryDate
        {
            get;
            set;
        }
        [Column("U_InfoW2ELnShpDate")]
        public DateTime? ExpectedLnShipDate
        {
            get;
            set;
        }

        [Column("TreeType")]
        public String TreeType
        {
            get;
            set;
        }

        [Required]
        public virtual SReturn SOReturn
        {
            get;
            set;
        }
    }
}
