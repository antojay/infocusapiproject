using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi870DetailRecord : Edi850DetailRecord
    {
        public Double QuantityShipped
        {
            get;
            set;
        }

        public String Item870Status
        {
            get;
            set;
        }

        public String ItemReasonCode870
        {
            get;
            set;
        }
        // 06-27-2017 
        public new DateTime? ExpectedLnDeliveryDate
        {
            get;
            set;
        }
        public new DateTime? ExpectedLnShipDate
        {
            get;
            set;
        }
        // 06-27-2017 end

    }
}
