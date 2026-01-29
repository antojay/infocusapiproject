using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get856RecordsPResponse : BaseWebApiResponse
    {
        
        private List<Edi856PHeaderRecord> _edi856PRecords = new List<Edi856PHeaderRecord>();
        public List<Edi856PHeaderRecord> Edi856Records
        {
            get
            {
                return _edi856PRecords;
            }
            set
            {
                _edi856PRecords = value;
            }
        }
  
    }
}
