using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess870Record
    {
       void OnPostProcess870Record(Infocus.WebApi.Data.Models.SOrder sorder, Edi850HeaderRecord edi850Record, Edi870HeaderRecord edi870Record);
    }
}
