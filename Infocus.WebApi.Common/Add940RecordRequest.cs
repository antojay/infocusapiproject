using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public class Add940RecordRequest : BaseWebApiRequest
    {
        private Edi940HeaderRecord _edi940HeaderRecord = new Edi940HeaderRecord();
        public Edi940HeaderRecord Edi940HeaderRecord
        {
            get
            {
                return _edi940HeaderRecord;
            }
            set
            {
                _edi940HeaderRecord = value;
            }
        }
    }
}
