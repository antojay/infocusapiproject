using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi856PackRecord 
    {
        public String PackNo
        {
            get;
            set;
        }

        public Double Length
        {
            get;
            set;
        }
        public Double Width
        {
            get;
            set;
        }
        public Double Height
        {
            get;
            set;
        }
        public String DimUOM
        {
            get;
            set;
        }
        public Double Quantity
        {
            get;
            set;
        }
        public Double Weight
        {
            get;
            set;
        }
        public String WeightQualifier
        {
            get;
            set;
        }
        public String WeightUOM
        {
            get;
            set;
        }
        public String TrackingNo
        {
            get;
            set;
        }
        public String TrackNoQualifier
        {
            get;
            set;
        }
        private List<Edi856ItemDetailRecord> _items = new List<Edi856ItemDetailRecord>();
        public List<Edi856ItemDetailRecord> Items
        {
            get
            {
                return _items;
            }
            set
            {
                _items = value;
            }
        } 
    }
}
