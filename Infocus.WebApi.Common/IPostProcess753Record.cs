using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess753Record
    {
        void OnPostProcess753Record(SOrder sorder, Edi850HeaderRecord edi850Record, Edi753HeaderRecord edi753Record);
    }
}
