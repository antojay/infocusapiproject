using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public class Add820RecordRequest : BaseWebApiRequest
    {
        private Edi820HeaderRecord _edi820HeaderRecord = new Edi820HeaderRecord();
        public Edi820HeaderRecord Edi820HeaderRecord
        {
            get
            {
                return _edi820HeaderRecord;
            }
            set
            {
                _edi820HeaderRecord = value;
            }
        }
    }
}
