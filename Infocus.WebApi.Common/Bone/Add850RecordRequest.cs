using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public class Add850RecordRequest : BaseWebApiRequest
    {
        private Edi850HeaderRecord _edi850HeaderRecord = new Edi850HeaderRecord();
        public Edi850HeaderRecord Edi850HeaderRecord
        {
            get
            {
                return _edi850HeaderRecord;
            }
            set
            {
                _edi850HeaderRecord = value;
            }
        }
    }
}
