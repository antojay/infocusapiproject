using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get860RecordsResponse : BaseWebApiResponse
    {
        private List<Edi860HeaderRecord> _edi870Records = new List<Edi860HeaderRecord>();
        public List<Edi860HeaderRecord> Edi860Records
        {
            get
            {
                return _edi860Records;
            }
            set
            {
                _edi860Records = value;
            }
        }
    }
}
