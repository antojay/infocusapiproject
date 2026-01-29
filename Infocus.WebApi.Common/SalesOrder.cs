using System;
using System.Collections.Generic;

namespace Infocus.WebApi.Common
{
    public class SalesOrder : BaseUserDefinedObject
    {
        public String CardCode
        {
            get;
            set;
        }
       
        public DateTime? DocDate
        {
            get;
            set;
        }
        public DateTime? DeliveryDate
        {
            get;
            set;
        }
        public String Comments
        {
            get;
            set;
        }
        public String PurchaseOrderNumber
        {
            get;
            set;
        }

        //JBB
        public String PurchaseOrderDate
        {
            get;
            set;
        }
        public String ReplenishmentNumber
        {
            get;
            set;
        }
        public String BuyerName
        {
            get;
            set;
        }
        public String CarrierCode
        {
            get;
            set;
        }
        public String ConditionDescription
        {
            get;
            set;
        }
        public String Department
        {
            get;
            set;
        }
        //END JBB
        // 08-14-2017 begin
        public String U_InfoW2TCd
        {
            get;
            set;
        }
        public Decimal? U_InfoW2TDisc
        {
            get;
            set;
        }
        public String U_InfoW2TDesc
        {
            get;
            set;
        }
        public Int16? U_InfoW2TDiscDays
        {
            get;
            set;
        }
        public Int16? U_InfoW2TDays
        {
            get;
            set;
        }
        public String U_InfoW2Notes
        {
            get;
            set;
        }
        public String U_InfoW2PrdDesc
        {
            get;
            set;
        }
        // 08-14-2017 end
        // 02-05-2019 begin
        public String U_InfoW2BOLNotes
        {
            get;
            set;
        }

        public String U_InfoW2PackNote
        {
            get;
            set;
        }

        public String U_InfoW2TMethod
        {
            get;
            set;
        }

        public String U_InfoW2ServiceLev
        {
            get;
            set;
        }

        public String U_InfoW2HandlingCode
        {
            get;
            set;
        }

        public String U_InfoW2DelContact
        {
            get;
            set;
        }

        public String U_InfoW2DelEmail
        {
            get;
            set;
        }
        // 02-05-2019 end
        public String ShippingMethod
        {
            get;
            set;
        }
        public String PaymentMethod
        {
            get;
            set;
        }

        // 03-17-2021 begin
        public String U_InfoW2753
        {
            get;
            set;
        }

        // 03-17-2021 end

        // 10-27-2019 begin
        public String U_InfoW2VNote1
        {
            get;
            set;
        }
        public String U_InfoW2VNote2
        {
            get;
            set;
        }
        // 10-27-2019 end

        private List<SalesOrderLine> _salesOrderLines = new List<SalesOrderLine>();
        public List<SalesOrderLine> SalesOrderLines
        {
            get
            {
                return _salesOrderLines;
            }
            set
            {
                _salesOrderLines = value;
            }
        }
        private AddressInformation _shipFromAddress = new AddressInformation();
        public AddressInformation ShipToAddress
        {
            get
            {
                return _shipFromAddress;
            }
            set
            {
                _shipFromAddress = value;
            }
        }
    }
}
