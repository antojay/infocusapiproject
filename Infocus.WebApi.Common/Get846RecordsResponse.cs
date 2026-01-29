using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get846RecordsResponse : BaseWebApiResponse
    {
        private List<Edi846HeaderRecord> _edi846Records = new List<Edi846HeaderRecord>();
        public List<Edi846HeaderRecord> Edi846Records
        {
            get
            {
                return _edi846Records;
            }
            set
            {
                _edi846Records = value;
            }
        }
    }
}
