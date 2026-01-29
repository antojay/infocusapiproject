using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Data.Models
{
    [Table("InfocusEdi850DetailRecord")]
    public partial class Edi850DetailRecord
    {
        /****************************/
        /* Begin System Information */
        /****************************/
        [Key]
        public Int32 DetailId
        {
            get;
            set;
        }
        public Int32 HeaderId
        {
            get;
            set;
        }
        public virtual Edi850HeaderRecord Edi850HeaderRecord
        {
            get;
            set;
        }

        public Int32 LineNumber
        {
            get;
            set;
        }
        public Double Quantity
        {
            get;
            set;
        }

        // 07-03-2017 begin
        public DateTime? ExpectedLnDeliveryDate
        {
            get;
            set;
        }
        public DateTime? ExpectedLnShipDate
        {
            get;
            set;
        }
        // 07-03-2017 end
        public String UnitOfMeasure
        {
            get;
            set;
        }
        public String BuyerItemCode
        {
            get;
            set;
        }
        public String VendorItemCode
        {
            get;
            set;
        }
        public Double? UnitPrice
        {
            get;
            set;
        }
        public String ItemDescription
        {
            get;
            set;
        }
        // 06-01-2019 begin
        public String VendorItemDescription
        {
            get;
            set;
        }

        public Double? GrossPkgWeight
        {
            get;
            set;
        }
        // 06-01-2019 end

        //07-01-2019 begin
        public String SalesEventName
        {
            get;
            set;
        }

        public String SalesEventText
        {
            get;
            set;
        }

        public String AllowanceType
        {
            get;
            set;
        }

        public String AllowanceId
        {
            get;
            set;
        }

        public String Comments
        {
            get;
            set;
        }
        // 07-01-2019 end
        /*
                // 10-31-2019 begin
                public String Comments2
                {
                    get;
                    set;
                }
                // 10-31-2019 end
         */
        public String UserDefined01
        {
            get;
            set;
        }
        public String UserDefined02
        {
            get;
            set;
        }
        public String UserDefined03
        {
            get;
            set;
        }
        public String UserDefined04
        {
            get;
            set;
        }
        public String UserDefined05
        {
            get;
            set;
        }
        public String UserDefined06
        {
            get;
            set;
        }
        public String UserDefined07
        {
            get;
            set;
        }
        public String UserDefined08
        {
            get;
            set;
        }
        public String UserDefined09
        {
            get;
            set;
        }
        public String UserDefined10
        {
            get;
            set;
        }

        // 01-29-2019 begin
        public String ItemUPC
        {
            get;
            set;
        }
        // 01-29-2019 end

        // 02-03-2019 begin
        public Double? RetailPrice
        {
            get;
            set;
        }
        public String PackingNotes
        {
            get;
            set;
        }
        // 02-03-2019 end
        // 03-25-2019 begin

        public String PurchaserItemCode
        {
            get;
            set;
        }
        public String Routing
        {
            get;
            set;
        }
        public String ServiceLevel
        {
            get;
            set;
        }
        public String TrackingNumber
        {
            get;
            set;
        }
        public String TrackingNoText
        {
            get;
            set;
        }
        public String DeliveryConfirmation
        {
            get;
            set;
        }
        public String DeliveryText
        {
            get;
            set;
        }
        public String AssignedId
        {
            get;
            set;
        }
        public String AssignedId2
        {
            get;
            set;
        }
        // 03-25-2019 end
        // 05-12-2021 begin
        public String CustItemCode
        {
            get;
            set;
        }
        // 05-12-2021 end

    }
}
