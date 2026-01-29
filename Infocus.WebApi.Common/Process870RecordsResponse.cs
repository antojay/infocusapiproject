using System;
using System.Collections.Generic;

namespace Infocus.WebApi.Common
{
    public class Process870RecordsResponse : BaseWebApiResponse
    {
        private List<Int32> _salesOrderNumbers = new List<int>();
        public List<Int32> SalesOrderNumbers
        {
            get
            {
                return _salesOrderNumbers;
            }
            set
            {
                _salesOrderNumbers = value;
            }
        }
    }
}
