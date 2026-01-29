using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi856PalletRecord
    {
        public String PalletNo
        {
            get;
            set;
        }
        public String PalletQualifier
        {
            get;
            set;
        }

        public Double Quantity
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
       
        public String WeightQualifier
        {
            get;
            set;
        }
        public Double Weight
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
        private List<Edi856PackRecord> _pack = new List<Edi856PackRecord>();
        public List<Edi856PackRecord> Pack
        {
            get
            {
                return _pack;
            }
            set
            {
                _pack = value;
            }
        }
    }
}
