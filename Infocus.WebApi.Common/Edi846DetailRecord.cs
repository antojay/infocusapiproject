using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi846DetailRecord 
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
       
       // 01-29-2019 begin
        public String ItemUPC
        {
            get;
            set;
        }
        // 01-29-2019 end

        // 07-01-2019 begin
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
        // 07-01-2019 end
        // 07-08-2019 begin
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
        // 07-08-2019 end

    }
}
