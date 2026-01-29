using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get810CRecordsResponse : BaseWebApiResponse
    {
        private List<Edi810CHeaderRecord> _edi810CRecords = new List<Edi810CHeaderRecord>();
        public List<Edi810CHeaderRecord> Edi810CRecords
        {
            get
            {
                return _edi810CRecords;
            }
            set
            {
                _edi810CRecords = value;
            }
        }
    }
}
