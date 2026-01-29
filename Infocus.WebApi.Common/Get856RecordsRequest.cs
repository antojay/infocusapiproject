using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public sealed class Get856RecordsRequest : BaseWebApiRequest
    {
        public String CardCode
        {
            get;
            set;
        }
        // 02-15-2022 begin
        public String LastRecTrxDT // date & time of last trx received --- process all transactions after that date & time
        {
            get;
            set;
        }
        // 02-15-2022 end

        // 04-08-2022 begin
        public String LastTrxCutoff
        {
            get;
            set;
        }
        // 04-08-2022 end

        // 08-10-2023 begin
        public String SourceTrx
        {
            get;
            set;
        }
        // 08-10-2023 end

        // 12-20-2023 begin
        public String PartnerId
        {
            get;
            set;
        }
        // 12-20-2023 end
    }
}
