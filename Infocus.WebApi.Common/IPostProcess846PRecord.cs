using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess846WRecord
    {
        void OnPostProcess846WRecord(Edi846WHeaderRecord edi846WRecord);
    }
}
