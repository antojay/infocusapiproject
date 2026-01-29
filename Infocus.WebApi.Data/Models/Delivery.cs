using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("ODLN")]
    public class Delivery
    {
        public Delivery()
        {
            DeliveryLines = new List<DeliveryLine>();
        }
        // 12-28-2022 begin
        public virtual Edi850HeaderRecord Edi850HeaderRecord
        {
            get;
            set;
        }
        // 12-28-2022 end
/*
        // 08-10-2023 begin
        public virtual Edi940HeaderRecord Edi940HeaderRecord
        {
            get;
            set;
        }
        //  08-10-2023 end
*/
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
        public Decimal DocTotal
        {
            get;
            set;
        }

        // 02-12-2021 begin
        public String NumAtCard
        {
            get;
            set;
        }
        // 02-12-2021 end

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
        // 07-17-2017 begin
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
        // 07-17-2017 end

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
        // 07-31-2019 begin
        public String U_Info_Pro
        {
            get;
            set;
        }
        // 07-31-2019 end
        // 08-06-2019 begin
        public String U_InfoW2856
        {
            get;
            set;
        }
        // 08-06-2019 end
        //12-28-2022 begin
        public Int32? U_Info850HdrId
        {
            get;
            set;
        }
        // 12-28-2022 end
        // 02-02-2023 begin
        public DateTime? U_COR_ActShipDt
        {
            get;
            set;
        }
        // 02-02-2023 end
        // 01-27-2023 begin
        public DateTime CreateDate
        {
            get;
            set;
        }
        // 01-27-2023 end
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

        public Decimal? U_InfoW2Cartons
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
        // 05-25-2021 begin
        public Decimal? U_C3_FreightCst
        {
            get;
            set;
        }
        public String U_C3_MstrBOL
        {
            get;
            set;
        } 
        // 05-25-2021 end

        public virtual ICollection<DeliveryLine> DeliveryLines
        {
            get;
            set;
        }
    }
}
