using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get855RecordsResponse : BaseWebApiResponse
    {
        private List<Edi855HeaderRecord> _edi855Records = new List<Edi855HeaderRecord>();
        public List<Edi855HeaderRecord> Edi855Records
        {
            get
            {
                return _edi855Records;
            }
            set
            {
                _edi855Records = value;
            }
        }
    }
}
