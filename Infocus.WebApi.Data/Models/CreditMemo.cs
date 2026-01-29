using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Infocus.WebApi.Data.Models
{
    [Table("ORIN")]
    public class CreditMemo
    {
        public CreditMemo()
        {
            CreditMemoLines = new List<CreditMemoLine>();
        }
        [Key]
        public Int32 DocEntry
        {
            get;
            set;
        }
        public Int32 DocNum
        {
            get;
            set;
        }
        public String Canceled
        {
            get;
            set;
        }
        public String CardCode
        {
            get;
            set;
        }
        public DateTime DocDueDate
        {
            get;
            set;
        }
        public DateTime DocDate
        {
            get;
            set;
        }

        public Decimal DocTotal
        {
            get;
            set;
        }

        public String TrackNo
        {
            get;
            set;
        }
        public Int16 TrnspCode
        {
            get;
            set;
        }
        public Int16 GroupNum
        {
            get;
            set;
        }
        public Decimal TotalExpns
        {
            get;
            set;
        }
        public String U_Info_BOL
        {
            get;
            set;
        }
        public String U_InfoW2Cc
        {
            get;
            set;
        }
        public Decimal? U_InfoW2SWgt
        {
            get;
            set;
        }
        public Int32? U_InfoW2CnNo
        {
            get;
            set;
        }
        public String U_InfoW2BCode
        {
            get;
            set;
        }
        public String U_InfoW2BName
        {
            get;
            set;
        }
        public String U_InfoW2BAd1
        {
            get;
            set;
        }
        public String U_InfoW2BAd2
        {
            get;
            set;
        }
        public String U_InfoW2BCity
        {
            get;
            set;
        }
        public String U_InfoW2BState
        {
            get;
            set;
        }
        public String U_InfoW2BZip
        {
            get;
            set;
        }
        public String U_InfoW2BCntry
        {
            get;
            set;
        }
        public String U_InfoW2Job
        {
            get;
            set;
        }
        public String U_InfoW2TCd
        {
            get;
            set;
        }
        public Decimal? U_InfoW2TDisc
        {
            get;
            set;
        }
        public String U_InfoW2TDesc
        {
            get;
            set;
        }
        public Int16? U_InfoW2TDiscDays
        {
            get;
            set;
        }
        public Int16? U_InfoW2TDays
        {
            get;
            set;
        }
        public String U_InfoW2Notes
        {
            get;
            set;
        }
        public String U_InfoW2PrdDesc
        {
            get;
            set;
        }
        // 02-05-2019 bgin
        public String U_InfoW2BOLNotes
        {
            get;
            set;
        }

        public String U_InfoW2PackNote
        {
            get;
            set;
        }

        public String U_InfoW2TMethod
        {
            get;
            set;
        }

        public String U_InfoW2ServiceLev
        {
            get;
            set;
        }

        public String U_InfoW2HandlingCode
        {
            get;
            set;
        }

        public String U_InfoW2DelContact
        {
            get;
            set;
        }

        public String U_InfoW2DelEmail
        {
            get;
            set;
        }
        // 02-05-2019 end

        // 10-27-2019 begin
        public String U_InfoW2VNote1
        {
            get;
            set;
        }
        public String U_InfoW2VNote2
        {
            get;
            set;
        }
        // 10-27-2019 end
        public virtual ICollection<CreditMemoLine> CreditMemoLines
        {
            get;
            set;
        }
    }
}
