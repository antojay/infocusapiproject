using System;
using System.Linq;
using SAPbouiCOM;
using System.Xml;
using SAPbobsCOM;
using System.Reflection;
using System.ComponentModel;

namespace Infocus.Framework.SapBone
{
    public static class BoneUtilities
    {
        private static String[] DocumentTypeObjects = { "13", "14", "15", "16", "17", "23", "540000006", "112" };

        public static String GetObjectKeyFromXmlType(String xml, String objectType)
        {
            String key = null;
            if(xml.Contains("DocumentParams") && xml.Contains("DocEntry"))
            {
                key = GetObjectKeyFromXml(xml, "DocumentParams/DocEntry");
            }
            else if(objectType == "2")
            {
                key = GetObjectKeyFromXml(xml, "BusinessPartnerParams/CardCode");
            }
            else if(objectType == "4")
            {
                key = GetObjectKeyFromXml(xml, "ItemParams/ItemCode");
            }
            else if(objectType == "24" || objectType == "46")
            {
                key = GetObjectKeyFromXml(xml, "PaymentParams/DocEntry");
            }
            else
            {
                throw new BoneFrameworkException("GetObjectKeyFromXml Invalid Object Type: " + objectType);
            }
            return key;
        }

        public static BoObjectTypes GetBoObjectTypesFromString(String strType)
        {
            Int32 intType;
            if(Int32.TryParse(strType, out intType))
            {
                return (BoObjectTypes)intType;
            }
            throw new BoneFrameworkException(String.Format("Invalid String Objec Type {0}. Cannot convert to BoObjectTypes.", strType));
        }

        public static Object GetItemValue(Item item)
        {
            Object value = null;
            if(item.Type == BoFormItemTypes.it_CHECK_BOX)
            {
                value = ((CheckBox)item.Specific).Checked ? "Y" : "N";
            }
            else if(item.Type == BoFormItemTypes.it_COMBO_BOX)
            {
                value = ((ComboBox)item.Specific).Selected.Value;
            }
            else if(item.Type == BoFormItemTypes.it_EDIT)
            {
                value = ((EditText)item.Specific).Value;
            }
            else
            {
                throw new BoneFrameworkException(String.Format("Invalid item type for substition {0}", item.Type));
            }
            return value;
        }

        private static String GetObjectKeyFromXml(String xmlString, String keyName)
        {
            XmlDocument xDoc = new XmlDocument();

            xDoc.LoadXml(xmlString);
            return xDoc.SelectSingleNode(keyName).InnerText;
        }

        public static void RefreshForegroundFormData(Application application)
        {
            application.ActivateMenuItem("1288");
            application.ActivateMenuItem("1289");
        }

        public static void NavigateFormToLastRecord(Application application)
        {
            application.ActivateMenuItem("1291");
        }

        public static void NavigateFormToRecord(Application application, Form form, String itemKeyUid, String id)
        {
            try
            {
                application.ActivateMenuItem("1281");
            }
            catch(Exception)
            {
            }

            form.ActiveItem = itemKeyUid;
            application.SendKeys(id);
            application.SendKeys("{ENTER}");
        }

        public static T RecordsetToObject<T>(Recordset recordset)
        {
            if(recordset.EoF)
            {
                return default(T);
            }
            T record = Activator.CreateInstance<T>();
            Type type = typeof(T);
            PropertyInfo[] propertyInfoArray = type.GetProperties();
            for(int xx = 0; xx < recordset.Fields.Count; xx++)
            {
                SAPbobsCOM.Field field = recordset.Fields.Item(xx);
                PropertyInfo propertyInfo = (from t in propertyInfoArray
                                             where t.Name == field.Name
                                             select t).FirstOrDefault();
                if(propertyInfo != null)
                {
                    propertyInfo.SetValue(record, Convert.ChangeType(field.Value, propertyInfo.PropertyType), null);
                }
            }

            return record;
        }

        //private static Object ToPropertyValue(PropertyInfo propertyInfo, Object value)
        //{
        //    if(propertyInfo.PropertyType == typeof(String))
        //    {
        //        return Convert.ToString(value);
        //    }
        //    else if(propertyInfo.PropertyType == typeof(Int32))
        //    {
        //        return Convert.ToInt32(value);
        //    }
        //    else if(propertyInfo.PropertyType == typeof(Double))
        //    {
        //        return Convert.ToDouble(value);
        //    }
        //    else if(propertyInfo.PropertyType == typeof(Decimal))
        //    {
        //        return Convert.ToDecimal(value);
        //    }
        //    else if(propertyInfo.PropertyType == typeof(DateTime))
        //    {
        //        return Convert.ToDateTime(value);
        //    }
        //    else if(propertyInfo.PropertyType == typeof(Int16))
        //    {
        //        return Convert.ToInt16(value);
        //    }
        //    return Convert.ToString(value);
        //}
        public static BoneDatabaseValidValuesList EnumToValidValuesList<T>(int totalWidth, String removePrefix = null)
        {
            BoneDatabaseValidValuesList list = new BoneDatabaseValidValuesList();
            Type enumType = typeof(T);
            Array values = Enum.GetValues(enumType);
            foreach(T val in values)
            {

                String stringValue = Convert.ToInt16(val).ToString().PadLeft(totalWidth, '0');
                BoneDatabaseValidValue vv = new BoneDatabaseValidValue(stringValue, GetEnumDescriptionOrName<T>(val, removePrefix));
                list.Add(vv);
            }
            return list;
        }

        public static BoneDatabaseValidValuesList EnumToValidValuesList<T>(String removePrefix = null)
        {
            BoneDatabaseValidValuesList list = new BoneDatabaseValidValuesList();
            Type enumType = typeof(T);
            Array values = Enum.GetValues(enumType);
            foreach(T val in values)
            {
                Int16 int16Value = Convert.ToInt16(val);
                BoneDatabaseValidValue vv = new BoneDatabaseValidValue(int16Value.ToString(), GetEnumDescriptionOrName<T>(val, removePrefix));
                list.Add(vv);
            }
            return list;
        }

        private static String GetEnumDescriptionOrName<T>(T value, String removePrefix)
        {
            String name;
            DescriptionAttribute[] d = (DescriptionAttribute[])(typeof(T).GetField(value.ToString()).GetCustomAttributes(typeof(DescriptionAttribute), false));
            if(d != null && d.Length > 0)
            {
                name = d[0].Description;
            }
            else
            {
                name = Enum.GetName(typeof(T), value);
            }
            if(name != null && removePrefix != null)
            {
                name = name.Replace(removePrefix, "");
            }
            return name;
        }

        public static String ToUdfColumn(String columnName)
        {
            if(!columnName.StartsWith("U_", StringComparison.InvariantCultureIgnoreCase))
            {
                return "U_" + columnName;
            }
            return columnName;
        }

        public static void ClearValidValues(SAPbouiCOM.ValidValues vv)
        {
            while(vv.Count > 0)
            {
                vv.Remove(0, BoSearchKey.psk_Index);
            }
        }

        public static void PopulateCountries(SAPbouiCOM.ValidValues validValues)
        {
            ClearValidValues(validValues);

            CountriesService cs =
                BoneConnectionContext.Company.GetCompanyService()
                .GetBusinessService(SAPbobsCOM.ServiceTypes.CountriesService) as CountriesService;
            CountriesParams cp = cs.GetCountryList();

            for(int xx = 0; xx < cp.Count; xx++)
            {
                CountryParams param = cp.Item(xx);
                validValues.Add(param.Code.Trim(), param.Name);
            }
        }
        public static void PopulateStates(SAPbouiCOM.ValidValues validValues, String countryCode)
        {
            ClearValidValues(validValues);
            Recordset rs = BoneConnectionContext.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery(String.Format("SELECT Code, Name FROM OCST With(NOLOCK) WHERE RTRIM(LTRIM(Country)) = '{0}'", countryCode.Trim()));
                rs.MoveFirst();
                while(!rs.EoF)
                {
                    validValues.Add(((String)rs.Fields.Item("Code").Value).Trim(), ((String)rs.Fields.Item("Name").Value).Trim());
                    rs.MoveNext();
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }

        public static Item CloneItem(Form form, String newUid, Item originalItem, BoFormItemTypes itemType)
        {
            Item clonedItem = form.Items.Add(newUid, itemType);

            clonedItem.Left = originalItem.Left;
            clonedItem.Top = originalItem.Top;
            clonedItem.Width = originalItem.Width;
            clonedItem.Height = originalItem.Height;
            clonedItem.FromPane = originalItem.FromPane;
            clonedItem.AffectsFormMode = originalItem.AffectsFormMode;
            clonedItem.DisplayDesc = originalItem.DisplayDesc;
            clonedItem.Description = originalItem.Description;
            clonedItem.Enabled = originalItem.Enabled;
            clonedItem.LinkTo = originalItem.LinkTo;
            clonedItem.RightJustified = originalItem.RightJustified;
            clonedItem.ToPane = originalItem.ToPane;
            clonedItem.Visible = originalItem.Visible;

            return clonedItem;
        }

        public static String ConvertToEditDateString(DateTime dt)
        {
            return dt.ToString("yyyyMMdd");
        }

        public static DateTime EditDateStringToDate(String str)
        {
            if(String.IsNullOrWhiteSpace(str) || str.Length != 8)
            {
                throw new BoneFrameworkException("Invalid date string: " + str);
            }
            Int32 year = Int32.Parse(str.Substring(0, 4));
            Int32 month = Int32.Parse(str.Substring(4, 2));
            Int32 day = Int32.Parse(str.Substring(6, 2));
            return new DateTime(year, month, day);
        }

        public static SAPbobsCOM.Company ConnectToCompany(String dbServer, String dbName, String dbUser, String dbPassword, String boneUser, String bonePassword, String dbType = "2008")
        {
            SAPbobsCOM.Company company = null;
            try
            {
                company = new SAPbobsCOM.Company();
                company.CompanyDB = dbName;
                company.DbPassword = dbPassword;
                // 01-22-2026 lar begin
                if(dbType.Contains("2022"))
                {
                    company.DbServerType = BoDataServerTypes.dst_MSSQL2019;
                }
                else if(dbType.Contains("2019"))
                {
                    company.DbServerType = BoDataServerTypes.dst_MSSQL2019;
                }
                else if(dbType.Contains("2016"))
                {
                    company.DbServerType = BoDataServerTypes.dst_MSSQL2016;
                }
                // 01-22-2026 lar end
                else if(dbType.Contains("2014"))
                {
                    company.DbServerType = BoDataServerTypes.dst_MSSQL2014;
                }
                else if(dbType.Contains("2012"))
                {
                    company.DbServerType = BoDataServerTypes.dst_MSSQL2012;
                }
                else
                {
                    company.DbServerType = BoDataServerTypes.dst_MSSQL2008;
                }
                company.DbUserName = dbUser;
                company.Password = bonePassword;
                company.Server = dbServer;
                company.UserName = boneUser;
                company.UseTrusted = false;
                company.LicenseServer = dbServer + ":30000";
                company.language = BoSuppLangs.ln_English;
                if(company.Connect() != 0)
                {
                    String msg = "Could not connect to company. " + company.GetLastErrorDescription();
                    //_logger.Fatal(msg);
                    throw new BoneFrameworkException(msg);
                }
            }
            catch(Exception ex)
            {
                company = null;
                throw ex;
            }
            return company;
        }

    }
}
