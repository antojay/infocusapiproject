using SAPbobsCOM;
using log4net;
using System;
using System.IO;
using System.Web;
using System.Xml.Serialization;

namespace Infocus.WebApi.Common.Bone
{
    public class BusinessOneRuntimeContext
    {
        private static readonly BusinessOneRuntimeContext _instance = new BusinessOneRuntimeContext();
        private Object _lockObj = new object();
        private const String COMPANY_STATE_KEY = "B1COMPANY";
        private const String COMPANY_DATABASE_KEY = "B1DB";
        private static readonly ILog _logger = LogManager.GetLogger(typeof(BusinessOneRuntimeContext));
        public static BusinessOneRuntimeContext Instance
        {
            get
            {
                return _instance;
            }
        }
        private BusinessOneRuntimeContext()
        {

        }

        public Company GetCompany()
        {
            if(HttpContext.Current == null)
            {
                return null;
            }
            Company company = (Company)HttpContext.Current.Application[COMPANY_STATE_KEY];
            if(company == null)
            {
                RecycleCompanyObject();
                company = (Company)HttpContext.Current.Application[COMPANY_STATE_KEY];
            }
            else
            {
                if(!company.Connected)
                {
                    RecycleCompanyObject();
                    company = (Company)HttpContext.Current.Application[COMPANY_STATE_KEY];
                }
            }
            return company;
        }
        private void SetCompany(Company company)
        {
            HttpContext.Current.Application[COMPANY_STATE_KEY] = company;
        }
        public void InitializeRuntime()
        {
            _logger.Debug("Initializing Runtime");
            try
            {
                RecycleCompanyObject();
            }
            catch(Exception ex)
            {
                _logger.Fatal("Error recycling company object", ex);
            }
        }
        public void Terminate()
        {
            UnloadCompany();
        }
        private void UnloadCompany()
        {
            if(HttpContext.Current == null || HttpContext.Current.Application == null)
            {
                return;
            }
            Company company = (Company)HttpContext.Current.Application[COMPANY_STATE_KEY];
            if(company != null)
            {
                DisposeCompany(company);
            }
        }
        private void RecycleCompanyObject()
        {
            lock (_lockObj)
            {
                UnloadCompany();
                String companyDb = null;
                if(HttpContext.Current.Session != null) //Running from Web Server
                {
                    companyDb = (String)HttpContext.Current.Session[COMPANY_DATABASE_KEY];
                }
                Company company = LoadCompanyFromConfigurationFile(companyDb);
                SetCompany(company);
            }
        }

        private void DisposeCompany(Company company)
        {
            _logger.Debug("Disposing Company object");
            if(company == null)
            {
                return;
            }
            if(company.Connected)
            {
                try
                {
                    company.Disconnect();
                }
                catch(Exception ex)
                {
                    _logger.Error("Error while disconnecting company", ex);
                }
            }
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(company);
            }
            catch
            {
            }
            finally
            {
                SetCompany(null);
            }
        }

        private Company LoadCompanyFromConfigurationFile(TextReader reader, String overrideCompanyName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(WebApiConfiguration));
            Company company = null;
            WebApiConfiguration configuration = (WebApiConfiguration)serializer.Deserialize(reader);
            if(configuration != null && configuration.Companies.Length > 0)
            {
                try
                {
                    company = new Company();
                    company.CompanyDB = overrideCompanyName ?? configuration.Companies[0].Database;
                    company.DbPassword = configuration.Companies[0].DatabasePassword;
                    // 01-22-2026 lar begin
                   /* if (configuration.Companies[0].DatabaseType.Contains("2022"))
                    {
                        company.DbServerType = BoDataServerTypes.dst_MSSQL2022;
                    }
                    else*/ if (configuration.Companies[0].DatabaseType.Contains("2019"))
                    {
                        //company.DbServerType = BoDataServerTypes.dst_MSSQL2019;
                    } 
                    else if (configuration.Companies[0].DatabaseType.Contains("2016"))
                    {
                        company.DbServerType = BoDataServerTypes.dst_MSSQL2016;
                    }
                    // 01-22-2026 lar end
                    else if(configuration.Companies[0].DatabaseType.Contains("2014"))
                    {
                        company.DbServerType = BoDataServerTypes.dst_MSSQL2014;
                    }
                    else if(configuration.Companies[0].DatabaseType.Contains("2012"))
                    {
                        company.DbServerType = BoDataServerTypes.dst_MSSQL2012;
                    }
                    else if(configuration.Companies[0].DatabaseType.IndexOf("HANA", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    {
                        company.DbServerType = BoDataServerTypes.dst_HANADB;
                    }
                    else
                    {
                        company.DbServerType = BoDataServerTypes.dst_MSSQL2008;
                    }
                    company.DbUserName = configuration.Companies[0].DatabaseUser;
                    company.Password = configuration.Companies[0].CompanyPassword;
                    company.Server = configuration.Companies[0].DatabaseServer;
                    company.UserName = configuration.Companies[0].CompanyUser;
                    company.UseTrusted = configuration.Companies[0].DatabaseUseTrusted;
                    company.language = BoSuppLangs.ln_English;
                    if(!String.IsNullOrWhiteSpace(configuration.Companies[0].LicenseServer))
                    {
                        company.LicenseServer = configuration.Companies[0].LicenseServer;
                    }
                    _logger.Debug("Connecting to source company...");
                    _logger.Debug("Company: " + company.CompanyName + ", Server: " + company.Server + ", DbUser:"  +   company.DbUserName.ToString() + ", User:" + company.UserName);
                    // 01-22-2026 lar 
                    // added try / catch when connecting
                    try
                    {
                        if (company.Connect() != 0)
                        {
                            String msg = "Error Desc: " + company.GetLastErrorDescription();
                            _logger.Error(msg);
                            //msg = "Error Context: " + company.GetLastErrorContext();
                            _logger.Error(msg);
                            msg = "Error Context: " + company.GetLastErrorCode();
                            _logger.Error(msg);                         
                            msg = "Error connecting to company...";
                           //_logger.Fatal(msg);
                            throw new WebApiException(msg);
                        }
                    }
                    catch (Exception cc)
                    {
                        String ErrMsg = "Could not connect to source company. Error: " + cc.Message;
                        //_logger.Fatal(msg);
                        throw new WebApiException(ErrMsg);
                    }
                    _logger.Debug("Source company connected successfully");
                }
                catch(Exception ex)
                {
                    company = null;
                    _logger.Fatal("Company connection Failure", ex);
                    throw ex;
                }
            }
            else
            {
                _logger.Fatal("No Company Connection File found or no companies present.");
            }
            return company;
        }

        private Company LoadCompanyFromConfigurationFile(String overrideCompanyName)
        {
            String workingDirectory;
            if(HttpContext.Current.Server != null)
            {
                workingDirectory = HttpContext.Current.Server.MapPath("~/");
            }
            else
            {
                workingDirectory = Environment.CurrentDirectory;
            }
            String filePath = Path.Combine(workingDirectory, "WebApiConfiguration.xml");
            _logger.Debug("Loading Company from configuration file " + filePath);
            using(TextReader reader = new StreamReader(filePath))
            {
                return LoadCompanyFromConfigurationFile(reader, overrideCompanyName);
            }
        }
    }
}
