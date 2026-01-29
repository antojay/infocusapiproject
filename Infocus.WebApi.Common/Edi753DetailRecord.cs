using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi753DetailRecord
    {
        public Int32 LineNumber
        {
            get;
            set;
        }
        public String ShipToLocationCode
        {
            get;
            set;
        }

        public String ShipToName
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
        public String ShipToCountry
        {
            get;
            set;
        }

        public String ReadyToShipDate
        {
            get;
            set;
        }

        public String PickUpTime
        {
            get;
            set;
        }
       
        public Int32 PackageCount
        {
            get;
            set;
        }
        public String PackageCode
        {
            get;
            set;
        }
        public String Stackable
        {
            get;
            set;
        }
        public String PurchaseOrderReference
        {
            get;
            set;
        }
        public String WeightUOMCode
        {
            get;
            set;
        }
        public Double ShipmentWeight
        {
            get;
            set;
        }

        public String VolumeQual
        {
            get;
            set;
        }

        public Double ShipmentVolume
        {
            get;
            set;
        }
        
    }
}
