using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("INV1")]
    public class InvoiceLine
    {
        [Key, Column(Order = 0)]
        [ForeignKey("Invoice")]
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
        public String ItemCode
        {
            get;
            set;
        }
        public String Dscription
        {
            get;
            set;
        }
        public Decimal Quantity
        {
            get;
            set;
        }
        public DateTime? ShipDate
        {
            get;
            set;
        }

        public Decimal Price
        {
            get;
            set;
        }
        public String UomCode
        {
            get;
            set;
        }
        public Decimal LineTotal
        {
            get;
            set;
        }
        // 08-29-2017 begin
        public String U_InfoW2MPId
        {
            get;
            set;
        }
        // 08-29-2017 end
       
        // 01-20-2018 begin
        public String TreeType
        {
            get;
            set;
        }
        public String SubCatNum
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
        // 07-31-2019 begin
        [Column("U_InfoW2TrackNo")]
        public String TrackingNo
        {
            get;
            set;
        }
        // 07-31-2019 end
        // 01-20-2018 begin
        [Column("U_InfoW2LNo")]
        public Int32? LineNumber850
        {
            get;
            set;
        }
        [Required]
        public virtual Invoice Invoice
        {
            get;
            set;
        }
    }
}
