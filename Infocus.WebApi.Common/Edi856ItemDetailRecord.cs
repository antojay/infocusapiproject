using Infocus.WebApi.Data.Models;
using System;

namespace Infocus.WebApi.Common
{
    public class Edi856ItemDetailRecord
    {
        public Int32 LineNumber
        {
            get;
            set;
        }
        public String BuyerItemCode
        {
            get;
            set;
        }
        public String ItemDescription
        {
            get;
            set;
        }
        // 04-08-2019 begin
        public String ItemReason
        {
            get;
            set;
        }
        public String ItemStatus
        {
            get;
            set;
            // 04-08-2019 end
        }
        // 04-08-2019 end
        // 09-27-2021 begin
        public String ItemUOM
        {
            get;
            set;
        }
        // 09-27-2021 end
        // 02-14-2019 begin
        public String ItemUPC
        {
            get;
            set;
        }
        // 02-14-2019 end
        public String LineItemReason
        {
            get;
            set;
        }
        public String LineItemStatus
        {
            get;
            set;
        }
        // 06-01-2019 begin
        public Double QtyOrdered
        {
            get;
            set;
        }
        // 06-01-2019 end
        public Double Quantity // quantity shipped
        {
            get;
            set;
        }
        public String SerialNumber
        {
            get;
            set;
        }
        // 02-22-2022 begin
        public String SSCC
        {
            get;
            set;
        }
        // 02-22-2022 end
        // 07-17-2019 begin
        public Double ShipmentCartons // # units shipped
        {
            get;
            set;
        }
        // 07-17-2019
        // 02-22-2019 begin
        public String TrackingNumber
        {
            get;
            set;
        }
        // 02-22-2019 end

        // 02-14-2019 begin
        public Decimal UnitPrice
        {
            get;
            set;
        }
        // 02-14-2019 end
        public String VendorItemCode
        {
            get;
            set;
        }

        // 07-17-2019 begin
        public String Warehouse
        {
            get;
            set;
        }
        // 07-17-2019 end

        // 05-20-2021 begin
        public String GrossPkgWeight
        {
            get;
            set;
        }
        // 05-20-2021 end

        // 07-30-2019 begin
        public String FreightClass
        {
            get;
            set;
        }

        public String NMFC
        {
            get;
            set;
        }
        // 07-30-2019 end

    }
}
