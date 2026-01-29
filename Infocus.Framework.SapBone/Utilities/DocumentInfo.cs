using System;
using System.Collections.Generic;
using System.Text;

namespace Infocus.Framework.SapBone.Utilities
{
    public class DocumentInfo
    {
        public Int32 ObjectType
        {
            get;
            set;
        }

        public Int32 DocEntry
        {
            get;
            set;
        }

        public Int32 DocNum
        {
            get;
            set;
        }

        public Boolean Canceled
        {
            get;
            set;
        }

        public String DocStatus
        {
            get;
            set;
        }

        public String CardCode
        {
            get;
            set;
        }

        public Int32 PaymentGroupCode
        {
            get;
            set;
        }
    }
}
