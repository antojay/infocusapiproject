using System;
using Infocus.WebApi.Data.Models;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess810Record
    {
        void OnPostProcess810Record(Invoice invoice, Edi850HeaderRecord edi850HeaderRecord, Edi810HeaderRecord edi856HeaderRecord);
    }
}
