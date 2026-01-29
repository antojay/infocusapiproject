using System;

namespace Infocus.WebApi.Common
{
    public class SalesOrderLine : BaseUserDefinedObject
    {
        public Int32 LineNumber
        {
            get;
            set;
        }
        public String ItemCode
        {
            get;
            set;
        }
        public String ItemDescription
        {
            get;
            set;
        }
        // 01-18-2018 begin
        public String SubCatNum
        {
            get;
            set;
        }
        public String TreeType
        {
            get;
            set;
        }
        // 01-18-2018 end
        /*
        // 02-21-2018 begin
        public String U_InfoVendorItem
        {
            get;
            set;
        }
        // 02-21-2018 end
        */
        public String VendorItemCode
        {
            get;
            set;
        }
        public Double Quantity
        {
            get;
            set;
        }
        public String UnitOfMeasure
        {
            get;
            set;
        }
        public Double UnitPrice
        {
            get;
            set;
        }
        // 08-29-2017 begin
        public String U_InfoW2MPId
        {
            get;
            set;
        }
        // 08-29-2017 end
        // 02-05-2019 begin
        public String U_InfoW2PackNote
        {
            get;
            set;
        }
        public String U_InfoW2ItemUPC
        {
            get;
            set;
        }
        // 02-05-2019 end
        // 05-14-2020 begin
        public Int32 ShippingMethod
        {
            get;
            set;
        }
        // 05-14-2020 end

        //JBB

        //END JBB
    }
}
