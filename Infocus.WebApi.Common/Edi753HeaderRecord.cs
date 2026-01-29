using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi753HeaderRecord
    {
        public String CardCode
        {
            get;
            set;
        }

        public String TransactionCode
        {
            get;
            set;
        }
        public String PurchaseOrderReference
        {
            get;
            set;
        }
        public String TransactionDate
        {
            get;
            set;
        }
        public String TransactionTime
        {
            get;
            set;
        }
        public String ShipperContact
        {
            get;
            set;
        }
        public String ShipperPhone
        {
            get;
            set;
        }
        public String ShipperEmail
        {
            get;
            set;
        }
               
        public String VendorNumber
        {
            get;
            set;
        }

        public String ShipFromName
        {
            get;
            set;
        }

        public String ShipFromAddress1
        {
            get;
            set;
        }
        public String ShipFromAddress2
        {
            get;
            set;
        }
        public String ShipFromCity
        {
            get;
            set;
        }
        public String ShipFromState
        {
            get;
            set;
        }

        public String ShipFromZip
        {
            get;
            set;
        }
        public String ShipFromCountry
        {
            get;
            set;
        }
     
        private List<Edi753DetailRecord> _details = new List<Edi753DetailRecord>();
        public List<Edi753DetailRecord> Details
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
