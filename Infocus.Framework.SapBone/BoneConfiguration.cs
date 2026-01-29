using System;
using System.Collections.Generic;

namespace Infocus.Framework.SapBone
{
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false, ElementName = "BoneConfiguration")]
    public partial class BoneConfiguration
    {
        private BoneCompany[] companiesField;

        [System.Xml.Serialization.XmlArrayItemAttribute("Company", IsNullable = false)]
        public BoneCompany[] Companies
        {
            get
            {
                return this.companiesField;
            }
            set
            {
                this.companiesField = value;
            }
        }
    }

    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true, TypeName = "Company")]
    public partial class BoneCompany
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
                return this.licenseServerField;
            }
            set
            {
                this.licenseServerField = value;
            }
        }

        /// <remarks/>
        public string CompanyUser
        {
            get
            {
                return this.companyUserField;
            }
            set
            {
                this.companyUserField = value;
            }
        }

        /// <remarks/>
        public string CompanyPassword
        {
            get
            {
                return this.companyPasswordField;
            }
            set
            {
                this.companyPasswordField = value;
            }
        }

        /// <remarks/>
        public string DatabaseType
        {
            get
            {
                return this.databaseTypeField;
            }
            set
            {
                this.databaseTypeField = value;
            }
        }

        /// <remarks/>
        public string DatabaseServer
        {
            get
            {
                return this.databaseServerField;
            }
            set
            {
                this.databaseServerField = value;
            }
        }

        /// <remarks/>
        public string Database
        {
            get
            {
                return this.databaseField;
            }
            set
            {
                this.databaseField = value;
            }
        }

        /// <remarks/>
        public bool DatabaseUseTrusted
        {
            get
            {
                return this.databaseUseTrustedField;
            }
            set
            {
                this.databaseUseTrustedField = value;
            }
        }

        /// <remarks/>
        public string DatabaseUser
        {
            get
            {
                return this.databaseUserField;
            }
            set
            {
                this.databaseUserField = value;
            }
        }

        /// <remarks/>
        public string DatabasePassword
        {
            get
            {
                return this.databasePasswordField;
            }
            set
            {
                this.databasePasswordField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string Id
        {
            get
            {
                return this.idField;
            }
            set
            {
                this.idField = value;
            }
        }
    }

}
