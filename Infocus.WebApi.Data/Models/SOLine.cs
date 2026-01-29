using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("RDR1")]
    public class SOLine
    {
        [Key, Column(Order = 0)]
        [ForeignKey("SOrder")]
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
        // 01-18-2018 begin
        public String SubCatNum
        {
            get;
            set;
        }
        // 01-18-2018 end
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
        
        // 02-05-2019 begin
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
        // 02-05-2019 end

        // 04-03-2019 begin
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
        // 04-02-2019 end
        public Decimal LineTotal
        {
            get;
            set;
        }
        [Column("U_InfoW2LNo")]
        public Int32? LineNumber850
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
        // 07-30-2017  begin
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
        // 07-03-2017 end
        /*
        // 02-21-2018 begin
        [Column("U_InfoVendorItem")]
        public String U_InfoVendorItem
        {
            get;
            set;
        }
        // 02-21-2018 end
        */
        // 01-18-2018 begin
        [Column("TreeType")]
        public String TreeType
        {
            get;
            set;
        }
        // 01-18-2018 end

        // 05-12-2021 begin
        public String U_InfoCustItemCode
        {
            get;
            set;
        }  
        // 05-12-2021 end

        [Required]
        public virtual SOrder SOrder
        {
            get;
            set;
        }
    }
}
