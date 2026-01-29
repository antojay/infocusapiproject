using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public interface IPostProcess856Record
    {
        void OnPostProcess856Record(Delivery delivery, Edi850HeaderRecord edi850Record, Edi856HeaderRecord edi856Record, String pIs3PL, String[] pTrackNos);
        void OnPostProcess856Record(Delivery delivery, Edi850HeaderRecord edi850Record, Edi856HeaderRecord edi856Record, String pIs3PL);
        // 08-16-2023 begin
        void OnPostProcess856Record(Delivery delivery, Edi940HeaderRecord edi940Record, Edi856HeaderRecord edi856Record, String pIs3PL, String[] pTrackNos);
        void OnPostProcess856Record(Delivery delivery, Edi940HeaderRecord edi940Record, Edi856HeaderRecord edi856Record, String pIs3PL);
        // 08-16-2023 end
    }
}
