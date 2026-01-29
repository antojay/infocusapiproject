using System;
using Infocus.WebApi.Data.Models;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess810CRecord
    {
        void OnPostProcess810CRecord(CreditMemo creditMemo, Edi850HeaderRecord edi850HeaderRecord, Edi810CHeaderRecord edi856HeaderRecord);
    }
}
