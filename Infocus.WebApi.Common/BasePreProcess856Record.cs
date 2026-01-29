using System;
using Infocus.WebApi.Data.Models;

namespace Infocus.WebApi.Common
{
    public class BasePreProcess856Record : IPreProcess856Record
    {
        public bool OnPreProcess856Record(Delivery deliveryLine, Edi850HeaderRecord record, DateTime pTrxDate, DateTime pCutOffDt)
        {
            if(record.Processed856 == false 
                // 04-08-2022 begin
               //|| record.Processed856DateTime > pTrxDate // 02-15-2022
               || ( record.Orig856ProcessedDateTime > pTrxDate 
                && record.Orig856ProcessedDateTime < pCutOffDt))
                // 04-08-2022 end
            {
                return true;
            }
            return false;
        }

        // 08-11-2023 begin
        public bool OnPreProcess856Record(Delivery deliveryLine, Edi940HeaderRecord record, DateTime pTrxDate, DateTime pCutOffDt)
        {
            if (record.Processed856 == false
               || (record.Orig856ProcessedDateTime > pTrxDate
                && record.Orig856ProcessedDateTime < pCutOffDt))
            {
                return true;
            }
            return false;
        }
        // 08-11-2023 end
    }
}
