using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get846WRecordsResponse : BaseWebApiResponse
    {
        private List<Edi846WHeaderRecord> _edi846WRecords = new List<Edi846WHeaderRecord>();
        public List<Edi846WHeaderRecord> Edi846WRecords
        {
            get
            {
                return _edi846WRecords;
            }
            set
            {
                _edi846WRecords = value;
            }
        }
    }
}
