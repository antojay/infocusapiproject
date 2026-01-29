using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class Edi846WHeaderRecord
    {
        public String ReportId
        {
            get;
            set;
        }
        public String CardCode
        {
            get;
            set;
        }
     
        public String VendorNumber
        {
            get;
            set;
        }
      
       
        public DateTime InventoryDate
        {
            get;
            set;
        }
      
        private List<Edi846WDetailRecord> _details = new List<Edi846WDetailRecord>();
        public List<Edi846WDetailRecord> Details
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
