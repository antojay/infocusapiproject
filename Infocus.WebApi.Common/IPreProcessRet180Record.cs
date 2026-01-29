using System;
using Infocus.WebApi.Data.Models;
namespace Infocus.WebApi.Common
{
    public interface IPreProcessRet180Record
    {
        Boolean OnPreProcessRet180Record(SReturn soReturn, Edi180HeaderRecord record);
    }
}
