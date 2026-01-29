using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get753RecordsResponse : BaseWebApiResponse
    {
        private List<Edi753HeaderRecord> _edi753Records = new List<Edi753HeaderRecord>();
        public List<Edi753HeaderRecord> Edi753Records
        {
            get
            {
                return _edi753Records;
            }
            set
            {
                _edi753Records = value;
            }
        }
    }
}
