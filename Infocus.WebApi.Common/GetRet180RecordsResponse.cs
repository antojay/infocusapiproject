using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class GetRet180RecordsResponse : BaseWebApiResponse
    {
        private List<Edi180RHeaderRecord> _ediRet180Records = new List<Edi180RHeaderRecord>();
        public List<Edi180RHeaderRecord> EdiRet180Records
        {
            get
            {
                return _ediRet180Records;
            }
            set
            {
                _ediRet180Records = value;
            }
        }
    }
}
