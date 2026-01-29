using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess856PRecord 
    {
        void OnPostProcess856Pack(Delivery delivery, Edi850HeaderRecord edi850Record, Edi856PHeaderRecord edi856PRecord, String pIs3PL, String[] pTrackNos);
    }
}
