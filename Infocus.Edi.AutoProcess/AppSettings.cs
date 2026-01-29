using Infocus.Common;
using Infocus.WebApi.Common.Bone;
using Infocus.Framework.SapBone;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Infocus.Edi.AutoProcess
{
   
    internal class AppSettings
    {
        private static AppSettings _instance;
      
        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    AppSettings.Load();
                }
                return _instance;
            }
        }
        public static void Load()
        {
           try
            {
                _instance = new AppSettings();
                _instance.DatabaseUser = InfocusEdiAutoProcess.oDbUser;
                _instance.DatabasePassword = InfocusEdiAutoProcess.oDbPassword;
                _instance.ImpDelay = InfocusEdiAutoProcess.get850Delay(); // 01-31-2023
                _instance.MaxRows = InfocusEdiAutoProcess.getMaxRows(); // 08-08-2023
                _instance.MaxTD = InfocusEdiAutoProcess.getMaxTD(); // 08-14-2023
            }
            catch (Exception ex)
            {
                string oErrMsg = ex.Message;
                Import_Log.LogEntry("AppSettings => " + oErrMsg);
            }
        }
        private AppSettings()
        {

        }

        
        public String DatabaseUser
        {
            get;
            set;
        }
        public String DatabasePassword
        {
            get;
            set;
        }

        // 01-31-2023 begin
        public Int32 ImpDelay
        {
            get;
            set;
        }
        // 01-31-2023 end
        // 08-08-2023 begin
        public Int32 MaxRows
        {
            get;
            set; 
        }
        // 08-08-2023 end
        // 08-14-2023 begi
        public Int32 MaxTD  
        {
            get;
            set;
        }
        // 08-14-2023 end
        public String GetConnectionString()
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder();
            sqlBuilder.DataSource = BoneConnectionContext.Company.Server;
            sqlBuilder.InitialCatalog = BoneConnectionContext.Company.CompanyDB;
            sqlBuilder.UserID = DatabaseUser;
            sqlBuilder.Password = DatabasePassword;
            String providerString = sqlBuilder.ToString();
            if(!providerString.EndsWith(";"))
            {
                providerString += ";";
            }
            providerString += "MultipleActiveResultSets=true";
            return providerString;
        }
    }
}
