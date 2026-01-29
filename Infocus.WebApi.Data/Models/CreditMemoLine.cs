using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("RIN1")]
    public class CreditMemoLine
    {
        [Key, Column(Order = 0)]
        [ForeignKey("CreditMemo")]
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
        public Int32 BaseEntry
        {
            get;
            set;
        }
        public Int32 BaseType
        {
            get;
            set;
        }
        public Int32 BaseLine
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
        public String UomCode
        {
            get;
            set;
        }
        // 01-20-2018 begin
        public String TreeType
        {
            get;
            set;
        }
        // 01-20-2018 end
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
        [Required]
        public virtual CreditMemo CreditMemo
        {
            get;
            set;
        }
    }
}
