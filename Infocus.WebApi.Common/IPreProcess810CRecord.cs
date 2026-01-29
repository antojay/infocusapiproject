using System;
using Infocus.WebApi.Data.Models;
namespace Infocus.WebApi.Common
{
    public interface IPreProcess810CRecord
    {
        Boolean OnPreProcess810CRecord(CreditMemo creditMemo, Edi850HeaderRecord record);
    }
}
