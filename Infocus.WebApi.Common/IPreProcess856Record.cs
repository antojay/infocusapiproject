using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public interface IPreProcess856Record
    {
        Boolean OnPreProcess856Record(Delivery deliveryLine, Edi850HeaderRecord record, DateTime trxDate, DateTime cutoffDt);
        Boolean OnPreProcess856Record(Delivery deliveryLine, Edi940HeaderRecord record, DateTime trxDate, DateTime cutoffDt); // 08-10-2023
    }
}
