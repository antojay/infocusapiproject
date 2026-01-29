using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess855Record
    {
        void OnPostProcess855Record(SOrder sorder, Edi850HeaderRecord edi850Record, Edi855HeaderRecord edi855Record);
    }
}
