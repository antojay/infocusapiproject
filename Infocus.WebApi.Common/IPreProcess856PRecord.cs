using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public interface IPreProcess856PRecord
    {
        Boolean OnPreProcess856PRecord(Delivery deliveryLine, Edi850HeaderRecord record);
    }
}
