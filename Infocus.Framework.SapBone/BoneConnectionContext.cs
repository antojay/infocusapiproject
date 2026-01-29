using System;
using System.Linq;
using SAPbouiCOM;
using SAPbobsCOM;
using System.Xml.Serialization;
using System.IO;
using Infocus.Common;

namespace Infocus.Framework.SapBone
{
    public sealed class BoneConnectionContext
    {
        /// <summary>
        /// Reference to the SAPbouiCOM.Application we are currently connected (UI API).
        /// </summary>
        ///       
        public static string _850Error; // 06-29-2019
        public static Application Application;
        private static readonly String USER_QUERY =
@"select t0.User_Code as UserCode
    ,t0.SUPERUSER as SuperUser
    ,t0.U_NAME as DisplayName
    ,t0.E_Mail
from ousr t0 With(NOLOCK)
where t0.USERID = {0}";
        private static readonly String USER_AUTH_QUERY =
@"select UserLink, PermId, Permission
from usr3 With(NOLOCK)
where PermId like 'INF_%'
    and Permission <> 'V'
	and UserLink = {0}";
        /// <summary>
        /// Reference to the SAPbobsCOM.Company we are currently connected (DI API).
        /// </summary>
        public static SAPbobsCOM.Company Company;

        public static BoneUserInfo UserInfo
        {
            get;
            private set;
        }

        /// <summary>
        /// Connection Type chosen by the user.
        /// </summary>
        public static BoneConnectionType ConnectionType = BoneConnectionType.SSO;

        /// <summary>
        /// Connection String used in Debug Mode to connect to the UI API 
        /// <c> SboGuiAPI.Connect(ConnectionString) </c>
        /// </summary>
        public static string ConnectionString =
          "0030002C0030002C00530041005000420044005F00440061007400650076002C0050004C006F006D0056004900490056";

        /// <summary>
        /// Unique string allowing the License Service to recognize your add-ons
        /// </summary>
        public static string addOnIdentifierStr;


        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Empty Constructor
        /// </summary>
        private BoneConnectionContext()
        {
        }

        /// <summary>
        /// Connects to the UI API, new method replacing deprecated one with the same name. 
        /// <para>Connects to the DI API if "diRequired".</para>
        /// </summary>
        /// <param name="ConnectionString">Connection String</param>
        /// <param name="addOnIdStr">AddOn Identifier String</param>
        /// <param name="cnxType">Connection type required</param>
        /// <returns>Return code from the call SAPbobsCOM.Company.Connect()</returns>
        public static int Initialize(string connStr, string addOnIdStr, BoneConnectionType cnxType)
        {
            try
            {
                SboGuiApi gui = new SboGuiApi
                {
                    AddonIdentifier = addOnIdentifierStr = addOnIdStr
                };
                gui.Connect(connStr);
                BoneConnectionContext.Application = gui.GetApplication(-1);
            }
            catch(System.Runtime.InteropServices.COMException err)
            {
                // ERROR
                throw err;
            }
            BoneConnectionContext.ConnectionType = cnxType;
            return Reinit();
        }

        /// <summary>
        /// Connects to the UI API. 
        /// Deprecated method, new one accepting a BoneConnectionType enum must be used.
        /// <para>Connects to the DI API if "diRequired".</para>
        /// </summary>
        /// <param name="ConnectionString">Connection String</param>
        /// <param name="addOnIdStr">AddOn Identifier String</param>
        /// <param name="diRequired">FALSE: No DI API connection required</param>
        /// <returns>Return code from the call SAPbobsCOM.Company.Connect()</returns>
        public static int Initialize(string connStr, string addOnIdStr, bool diRequired)
        {
            try
            {
                SboGuiApi gui = new SboGuiApi
                {
                    AddonIdentifier = addOnIdentifierStr = addOnIdStr
                };
                gui.Connect(connStr);
                BoneConnectionContext.Application = gui.GetApplication(-1);
            }
            catch(System.Runtime.InteropServices.COMException err)
            {
                // ERROR
                throw err;
            }
            if(diRequired)
                ConnectionType = BoneConnectionType.SSO;
            else
                ConnectionType = BoneConnectionType.OnlyUI;

            return Reinit();
        }

        /// <summary>
        /// Connects to the DI current company used by the UI API. 
        /// <para>Disconnects first if it was already connected to a company.</para>
        /// </summary>
        /// <returns>Return code from the DI API connection.</returns>
        public static int Reinit()
        {
            int retCode = 0;

            if(ConnectionType == BoneConnectionType.SSO ||
              ConnectionType == BoneConnectionType.MultipleAddOns)
            {

                if(ConnectionType == BoneConnectionType.MultipleAddOns)
                {
                    Company = (SAPbobsCOM.Company)Application.Company.GetDICompany();
                }
                else
                {

                    if(Company != null && Company.Connected == true)
                    {
                        Company.Disconnect();

                        Company = null;
                    }

                    Company = new SAPbobsCOM.Company();

                    string cookie = Company.GetContextCookie();
                    string connInfo = Application.Company.GetConnectionContext(cookie);

                    retCode = Company.SetSboLoginContext(connInfo);
                    if(retCode != 0)
                    {
                        // ERROR!
                        return retCode;
                    }

                    Company.AddonIdentifier = addOnIdentifierStr;
                    retCode = Company.Connect();

                    if(retCode != 0)
                    {
                        return retCode;
                    }
                    LoadUserInfo();
                }
            }
            return retCode;
        }

        public static PermissionType GetPermission(String permissionId, PermissionType defaultPermission = PermissionType.None)
        {
            if(!UserInfo.IsSuperUser)
            {
                var permission = (from v in UserInfo.UserPermissions
                                  where v.Code == permissionId
                                  select v).FirstOrDefault();
                if(permission == null)
                {
                    return defaultPermission;
                }
                return permission.Permission;
            }
            else
            {
                return PermissionType.Full;
            }
        }

        private static void LoadUserInfo()
        {
            UserInfo = new BoneUserInfo();
            UserInfo.UserName = Company.UserName;
            UserInfo.UserId = Company.UserSignature;

            Recordset rs = Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery(String.Format(USER_QUERY, Company.UserSignature));
                rs.MoveFirst();
                UserInfo.IsSuperUser = ((String)rs.Fields.Item("SuperUser").Value) == "Y";
                UserInfo.DisplayName = (String)rs.Fields.Item("DisplayName").Value;
                UserInfo.EmailAddress = (String)rs.Fields.Item("E_Mail").Value;
                rs.DoQuery(String.Format(USER_AUTH_QUERY, Company.UserSignature));
                rs.MoveFirst();
                while(!rs.EoF)
                {
                    BoneUserPermissionInfo userPermission = new BoneUserPermissionInfo((String)rs.Fields.Item("PermId").Value);
                    String authType = (String)rs.Fields.Item("Permission").Value;
                    if(authType == "F")
                    {
                        userPermission.Permission = PermissionType.Full;
                    }
                    else if(authType == "R")
                    {
                        userPermission.Permission = PermissionType.Readonly;
                    }
                    else
                    {
                        userPermission.Permission = PermissionType.None;
                    }
                    UserInfo.UserPermissions.Add(userPermission);
                    rs.MoveNext();
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }

        public static void RefreshUserInfo()
        {
            LoadUserInfo();
        }

        private static BoneConfiguration GetConfigurationFromFile()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BoneConfiguration));
            String filePath = Path.Combine(SystemUtility.GetWorkingDirectory(), "BoneConfiguration.xml");
            BoneConfiguration configuration = null;
            using(TextReader reader = new StreamReader(filePath))
            {
                configuration = (BoneConfiguration)serializer.Deserialize(reader);
            }
            return configuration;
        }

        public static void LoadCompanyFromConfigurationFile()
        {
            String filePath = Path.Combine(SystemUtility.GetWorkingDirectory(), "BoneConfiguration.xml");
            using(TextReader reader = new StreamReader(filePath))
            {
                LoadCompanyFromConfigurationFile(reader);
            }
        }
        public static void LoadCompanyFromConfigurationFile(TextReader configurationReader)
        {
            LoadCompanyFromConfigurationFile(configurationReader, null);
        }
        public static void LoadCompanyFromConfigurationFile(TextReader reader, String overrideCompanyName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(BoneConfiguration));

            BoneConfiguration configuration = (BoneConfiguration)serializer.Deserialize(reader);
            if(configuration != null && configuration.Companies.Length > 0)
            {
                if(Company != null && Company.Connected)
                {
                    try
                    {
                        Company.Disconnect();
                        Company = null;
                    }
                    catch
                    {
                    }
                }
                try
                {
                    Company = new SAPbobsCOM.Company();
                    Company.CompanyDB = overrideCompanyName ?? configuration.Companies[0].Database;
                    Company.DbPassword = configuration.Companies[0].DatabasePassword;
                    // 08-11-2020 begin
                   /* if (configuration.Companies[0].DatabaseType.Contains("2019"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2019;
                            
                    }
                    else*/
                    // 01-22-2026 lar begin
                    if (configuration.Companies[0].DatabaseType.Contains("2022"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2019;
                            
                    }
                    else if (configuration.Companies[0].DatabaseType.Contains("2019"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2019;
                            
                    }
                    else if (configuration.Companies[0].DatabaseType.Contains("2017"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2016;
                            
                    }
                    else if (configuration.Companies[0].DatabaseType.Contains("2016"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2016;
                    }
                        // 01-22-2026 lar end
                    else
                        // 08-11-2020 end
                    if(configuration.Companies[0].DatabaseType.Contains("2014"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2014;
                    }
                    else if(configuration.Companies[0].DatabaseType.Contains("2012"))
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2012;
                    }
                    else
                    {
                        Company.DbServerType = BoDataServerTypes.dst_MSSQL2008;
                    }
                    Company.DbUserName = configuration.Companies[0].DatabaseUser;
                    Company.Password = configuration.Companies[0].CompanyPassword;
                    Company.Server = configuration.Companies[0].DatabaseServer;
                    Company.UserName = configuration.Companies[0].CompanyUser;
                    Company.UseTrusted = configuration.Companies[0].DatabaseUseTrusted;
                    Company.language = BoSuppLangs.ln_English;
                    if(!String.IsNullOrWhiteSpace(configuration.Companies[0].LicenseServer))
                    {
                        Company.LicenseServer = configuration.Companies[0].LicenseServer;
                    }
                    if(Company.Connect() != 0)
                    {
                        String msg = "Could not connect to source company. " + Company.GetLastErrorDescription();
                        throw new InfocusException(msg);
                    }
                }
                catch(Exception ex)
                {
                    Company = null;
                    throw ex;
                }
            }
            else
            {
                throw new InfocusException("No Company Connection File found or no companies present.");
            }
        }

        public static void LoadCompanyFromConfigurationFile(String overrideCompanyName)
        {
            String filePath = Path.Combine(SystemUtility.GetWorkingDirectory(), "BoneConfiguration.xml");
            using(TextReader reader = new StreamReader(filePath))
            {
                LoadCompanyFromConfigurationFile(reader, overrideCompanyName);
            }
        }
    }
}
