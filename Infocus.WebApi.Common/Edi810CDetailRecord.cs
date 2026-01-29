using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi810CDetailRecord : Edi850DetailRecord
    {
        public Double QuantityShipped
        {
            get;
            set;
        }
        
    }
}
