using System;
using Infocus.WebApi.Data.Models;

namespace Infocus.WebApi.Common
{
    public interface IPostProcessRet180Record
    {
        void OnPostProcessRet180Record(SReturn soRetrun, Edi180HeaderRecord edi180HeaderRecord, Edi180RHeaderRecord ediRet180HeaderRecord);
    }
}
