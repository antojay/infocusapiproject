using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi180RHeaderRecord
    {
        public Int32 HeaderId
        {
            get;
            set;
        }
        public DateTime? RecordDate
        {
            get;
            set;
        }
        public Boolean Processed
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
        public Int32 ReturnOrderKey
        {
            get;
            set;
        }
        public DateTime? ReceivedDateTime
        {
            get;
            set;
        }
        public DateTime? ProcessedDateTime
        {
            get;
            set;
        }

        public Boolean ProcessedReturn180
        {
            get;
            set;
        }
        public DateTime? Processed180DateTime
        {
            get;
            set;
        }

        /****************************/
        /*  End System Information  */
        /****************************/
        public String CardCode
        {
            get;
            set;
        }

        public String SBOCardCode
        {
            get;
            set;
        }

        public String PurchaseOrderReference
        {
            get;
            set;
        }
        public String TrxPurpose
        {
            get;
            set;
        }

        public String OrderType
        {
            get;
            set;
        }

        public String VendorNumber
        {
            get;
            set;
        }
        /* public DateTime? PurchaseOrderDate
         {
             get;
             set;
         }*/

        public Double? ReturnTotal
        {
            get;
            set;
        }
        public DateTime? RequestDate
        {
            get;
            set;
        }
        public String ReferenceId
        {
            get;
            set;
        }
        public String ChargeId
        {
            get;
            set;
        }

        public String ChargeCode
        {
            get;
            set;
        }

        public Double ChargeAmount
        {
            get;
            set;
        }
        public String ChargeReference
        {
            get;
            set;
        }
        public String TrxStatus
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


        private List<Edi180RDetailRecord> _details = new List<Edi180RDetailRecord>();
        public List<Edi180RDetailRecord> Details
        {
            get
            {
                return _details;
            }
            set
            {
                _details = value;
            }
        }
    }
}
