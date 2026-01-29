using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get810RecordsResponse : BaseWebApiResponse
    {
        private List<Edi810HeaderRecord> _edi810Records = new List<Edi810HeaderRecord>();
        public List<Edi810HeaderRecord> Edi810Records
        {
            get
            {
                return _edi810Records;
            }
            set
            {
                _edi810Records = value;
            }
        }
    }
}
