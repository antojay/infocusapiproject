using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess846Record
    {
        void OnPostProcess846Record(Edi846HeaderRecord edi846Record);
    }
}
