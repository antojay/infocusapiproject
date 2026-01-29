using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public class Add860RecordRequest : BaseWebApiRequest
    {
        private Edi860HeaderRecord _edi860HeaderRecord = new Edi860HeaderRecord();
        public Edi860HeaderRecord Edi860HeaderRecord
        {
            get
            {
                return _edi860HeaderRecord;
            }
            set
            {
                _edi860HeaderRecord = value;
            }
        }
    }
}
