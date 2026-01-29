using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public class Add180RecordRequest : BaseWebApiRequest
    {
        private Edi180HeaderRecord _edi180HeaderRecord = new Edi180HeaderRecord();
        public Edi180HeaderRecord Edi180HeaderRecord
        {
            get
            {
                return _edi180HeaderRecord;
            }
            set
            {
                _edi180HeaderRecord = value;
            }
        }
    }
}
