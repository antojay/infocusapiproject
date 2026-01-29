using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get870RecordsResponse : BaseWebApiResponse
    {
        private List<Edi870HeaderRecord> _edi870Records = new List<Edi870HeaderRecord>();
        public List<Edi870HeaderRecord> Edi870Records
        {
            get
            {
                return _edi870Records;
            }
            set
            {
                _edi870Records = value;
            }
        }
    }
}
