using System;
using Infocus.WebApi.Data.Models;

namespace Infocus.WebApi.Common
{
    public class BasePreProcess856PRecord : IPreProcess856PRecord
    {
        public bool OnPreProcess856PRecord(Delivery deliveryLine, Edi850HeaderRecord record)
        {
            if(record.Processed856 == false)
            {
                return true;
            }
            return false;
        }
    }
}
