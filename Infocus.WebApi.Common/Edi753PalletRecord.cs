using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi753PalletRecord 
    {
        public Int32 LineNumber
        {
            get;
            set;
        }
      
        public String ShipToName
        {
            get;
            set;
        }
        public String ShipToLocationCode
        {
            get;
            set;
        }
        public String ShipToStoreLocation
        {
            get;
            set;
        }
        public String ShipToAddress1
        {
            get;
            set;
        }
        public String ShipToAddress2
        {
            get;
            set;
        }
         public String ShipToCity
        {
            get;
            set;
        }
        public String ShipToCountry
        {
            get;
            set;
        }
        

        public String ShipToState
        {
            get;
            set;
        }

        public String ShipToZip
        {
            get;
            set;
        }

        public DateTime? EstShipDate
        {
            get;
            set;
        }


        private List<Edi753DetailRecord> _details = new List<Edi753DetailRecord>();
        public List<Edi753DetailRecord> Palets
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
