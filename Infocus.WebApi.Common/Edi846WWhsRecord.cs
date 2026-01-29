using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi846WWhsRecord
    {

        public String WhsQualifier // 3 characters
        {
            get;
            set;
        }
        public String Warehouse // 30 characters
        {
            get;
            set;
        }
        public Double WhsQuantity // qty in warehouse
        {
            get;
            set;
        }
        // 09-24-2025 begin
        public DateTime? WhsNextAvailDt
        {
            get;
            set;
        }
        // 09-24-2025 end
        // 10-21-2025 begin
        public Double WhsNextAvailQty
        {
            get;
            set;
        }
        // 10-21-2025 end
    }
}
