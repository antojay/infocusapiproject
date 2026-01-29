using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("ORDR")]
    public class SOrder
    {
        public SOrder()
        {
            SOLines = new List<SOLine>();
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

        // 06-18-2021 begin
        public string DocStatus
        {
            get;
            set;
        }
        // 06-18-2021 end

        public String Canceled
        {
            get;
            set;
        }

        // 07-03-2019 begin
        public String Address // Bill To Address
        {
            get;
            set;
        }

        public String Address2 // Ship To Address 
        {
            get;
            set;
        }
        // 07-03-2019 end
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
        public Int16? TrnspCode
        {
            get;
            set;
        }
        // 03-17-2021 begin
        public String U_InfoW2753
        {
            get;
            set;
        }

        // 03-17-2021 end
       
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
        public String U_InfoOrdType
        {
            get;
            set;
        }
        public String U_InfoTrxPurpose
        {
            get;
            set;
        }
        public String U_InfoOrdStatus
        {
            get;
            set;
        }
        public String U_InfoReasonCd
        {
            get;
            set;
        }
        // 07-17-2017 begin
        public String U_InfoW2BCode
        {
            get;
            set;
        }
        // 07-17-2017 end
        // 06-27-2017 begin
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
        // 06-27-2017 end

        // 07-03-2017  begin
        public DateTime? U_InfoW2EDelDate
        {
            get;
            set;
        }

        public DateTime? U_InfoW2EShDate
        {
            get;
            set;
        }
        // 07-03-2017 end
        // 01-27-2023 begin
        public DateTime? U_ZSPS_SchedShpDt
        {
            get;
            set;
        }
        // 01-27-2023 end
        // 07-19-2017 begin
        public Decimal U_InfoW2SWgt
        {
            get;
            set;
        }
        public Int32 U_InfoW2CnNo
        {
            get;
            set;
        }
        // 07-19-2017 end
        // 08-14-2017 begin
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
        // 08-14-2017 end
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

        // 05-12-2021 begin
       /*public String U_InfoCustomerRef
        {
            get;
            set;
        }*/
        public String U_Info3PLOrdType
        {
            get;
            set;
        }
        public String U_InfoFrtBillType
        {
            get;
            set;
        }
        public String U_InfoConsumerPO
        {
            get;
            set;
        }
        public DateTime? U_InfoConsumerPODate
        {
            get;
            set;
        }
        public String U_InfoShipFromStore
        {
            get;
            set;
        }
        public String U_InfoShipFromName
        {
            get;
            set;
        }
        public String U_InfoPurposeCode
        {
            get;
            set;
        }
        public String U_Info3PLPOType
        {
            get;
            set;
        }
        public String U_InfoPackStoreCd
        {
            get;
            set;
        }
        public String U_InfoReceiptId
        {
            get;
            set;
        }
        public String U_Info3PLCustCode
        {
            get;
            set;
        }
        public String U_InfoVendorMessage
        {
            get;
            set;
        }
        public String U_InfoOrderMessage
        {
            get;
            set;
        }
        public String U_InfoGiftMessage
        {
            get;
            set;
        }
        public String U_InfoItemTaxType
        {
            get;
            set;
        }
        public String U_InfoTaxComponent
        {
            get;
            set;
        }
        public String U_InfoDiscountDesc
        {
            get;
            set;
        }
        // 05-12-2021 end

        public virtual ICollection<SOLine> SOLines
        {
            get;
            set;
        }
    }
}
