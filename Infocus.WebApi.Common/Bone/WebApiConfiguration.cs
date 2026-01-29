using System;
using System.Xml.Serialization;

namespace Infocus.WebApi.Common.Bone
{
    [XmlTypeAttribute(AnonymousType = true)]
    [XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "WebApiConfiguration")]
    public partial class WebApiConfiguration
    {
        private BusinessOneCompany[] companiesField;

        [XmlArrayItemAttribute("Company", IsNullable = false)]
        public BusinessOneCompany[] Companies
        {
            get
            {
                return companiesField;
            }
            set
            {
                companiesField = value;
            }
        }
    }

    [XmlTypeAttribute(AnonymousType = true, TypeName = "Company")]
    public partial class BusinessOneCompany
    {

        private String licenseServerField;

        private string companyUserField;

        private string companyPasswordField;

        private string databaseTypeField;

        private string databaseServerField;

        private string databaseField;

        private bool databaseUseTrustedField;

        private string databaseUserField;

        private string databasePasswordField;

        private string idField;

        /// <remarks/>
        public String LicenseServer
        {
            get
            {
                return licenseServerField;
            }
            set
            {
                licenseServerField = value;
            }
        }

        /// <remarks/>
        public string CompanyUser
        {
            get
            {
                return companyUserField;
            }
            set
            {
                companyUserField = value;
            }
        }

        /// <remarks/>
        public string CompanyPassword
        {
            get
            {
                return companyPasswordField;
            }
            set
            {
                companyPasswordField = value;
            }
        }

        /// <remarks/>
        public string DatabaseType
        {
            get
            {
                return databaseTypeField;
            }
            set
            {
                databaseTypeField = value;
            }
        }

        /// <remarks/>
        public string DatabaseServer
        {
            get
            {
                return databaseServerField;
            }
            set
            {
                databaseServerField = value;
            }
        }

        /// <remarks/>
        public string Database
        {
            get
            {
                return databaseField;
            }
            set
            {
                databaseField = value;
            }
        }

        /// <remarks/>
        public bool DatabaseUseTrusted
        {
            get
            {
                return databaseUseTrustedField;
            }
            set
            {
                databaseUseTrustedField = value;
            }
        }

        /// <remarks/>
        public string DatabaseUser
        {
            get
            {
                return databaseUserField;
            }
            set
            {
                databaseUserField = value;
            }
        }

        /// <remarks/>
        public string DatabasePassword
        {
            get
            {
                return databasePasswordField;
            }
            set
            {
                databasePasswordField = value;
            }
        }

        /// <remarks/>
        [XmlAttributeAttribute()]
        public string Id
        {
            get
            {
                return idField;
            }
            set
            {
                idField = value;
            }
        }
    }
}
