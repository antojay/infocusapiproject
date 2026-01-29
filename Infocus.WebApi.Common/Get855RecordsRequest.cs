using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get855RecordsRequest : BaseWebApiRequest
    {
        public String CardCode
        {
            get;
            set;
        }

        // 02-22-2022 begin
        public String LastRecTrxDT
        {
            get;
            set;
        }
        // 02-22-2022 end
        // 02-23-2024 begin
        public String SourceTrx
        {
            get;
            set;
        }
        // 02-23-2024 end
    }
}
