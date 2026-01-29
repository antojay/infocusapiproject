using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common
{
    public class AddressInformation : BaseUserDefinedObject
    {
        public String FullName
        {
            get;
            set;
        }
        public String Attention
        {
            get;
            set;
        }
        public String AddressLine1
        {
            get;
            set;
        }
        public String AddressLine2
        {
            get;
            set;
        }
        public String City
        {
            get;
            set;
        }
        public String State
        {
            get;
            set;
        }
        public String ZipCode
        {
            get;
            set;
        }
        public String Country
        {
            get;
            set;
        }
        public String StoreNumber
        {
            get;
            set;
        }
        public String PhoneNumber
        {
            get;
            set;
        }
    }
}
