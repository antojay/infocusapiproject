using System;
using Infocus.WebApi.Data.Models;
namespace Infocus.WebApi.Common
{
    public interface IPreProcess810Record
    {
        Boolean OnPreProcess810Record(Invoice invoice, Edi850HeaderRecord record);
    }
}
