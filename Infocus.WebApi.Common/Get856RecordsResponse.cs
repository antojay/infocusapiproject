using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get856RecordsResponse : BaseWebApiResponse
    {
        private List<Edi856HeaderRecord> _edi856Records = new List<Edi856HeaderRecord>();
       
        public List<Edi856HeaderRecord> Edi856Records
        {
            get
            {
                return _edi856Records;
            }
            set
            {
                _edi856Records = value;
            }
        }

    }
}
