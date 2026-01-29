using Infocus.WebApi.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public interface IPreProcess846WRecord
    {
        Boolean OnPreProcess846WRecord(Infocus.WebApi.Data.Models.SOrder deliveryLine, Edi850HeaderRecord record);
    }
}
