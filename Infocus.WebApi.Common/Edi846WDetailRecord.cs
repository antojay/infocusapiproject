using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi846WDetailRecord 
    {
     
        public Double Quantity1
        {
            get;
            set;
        }
        public String Quantity1Qualifier
        {
            get;
            set;
        }
        public String VendorItemCode
        {
            get;
            set;
        }
        public String BuyerItemCode
        {
            get;
            set;
        }
        public String ItemDescription
        {
            get;
            set;
        }
        public String Description
        {
            get;
            set;
        }
        public Double UnitPrice
        {
            get;
            set;
        }
       
        public String ItemUPC
        {
            get;
            set;
        }
        public DateTime? DateAvailable
        {
            get;
            set;
        }

        public Double QtyOnOrder
        {
            get;
            set;
        }

        public String ItemDiscontinued
        {
            get;
            set;
        }

        public String ProductId
        {
            get;
            set;
        }
        public String AvailCode
        {
            get;
            set;
        }
        public String MaintType
        {
            get;
            set;
        }
        public String LeadTimeCode
        {
            get;
            set;
        }
        public String LeadTimePeriod
        {
            get;
            set;
        }
        public Int32 LeadTimeQty
        {
            get;
            set;
        }
        public String UOM
        {
            get;
            set;
        }

        private List<Edi846WWhsRecord> _whsDetail = new List<Edi846WWhsRecord>();
        public List<Edi846WWhsRecord> WhsDetail
        {
            get
            {
                return _whsDetail;
            }
            set
            {
                _whsDetail = value;
            }
        }
    }
}
