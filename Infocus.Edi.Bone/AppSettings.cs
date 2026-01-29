using Infocus.Common;
using Infocus.Framework.SapBone;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.Edi.Bone
{
   
    internal class AppSettings
    {
        private static AppSettings _instance;
        public static AppSettings Instance
        {
            get
            {
                return _instance;
            }
        }
        public static void Load()
        {
            Recordset rs = BoneConnectionContext.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                //rs.DoQuery("select Code, Name, U_DbUser, U_DbPass from dbo.[@INFO_W2_SETTINGS] where Code = 'DEFAULT'");
                rs.DoQuery("select Code, Name, U_DbUser, U_DbPass, U_850Delay from dbo.[@INFO_W2_SETTINGS] where Code = 'DEFAULT'"); // 01-31-2023
                rs.MoveFirst();
                if(rs.EoF)
                {
                    throw new InfocusException("No settings specified in Settings File");
                }
                _instance = new AppSettings();
                _instance.DatabaseUser = (String)rs.Fields.Item("U_DbUser").Value;
                _instance.DatabasePassword = (String)rs.Fields.Item("U_DbPass").Value;
                _instance.ImpDelay = (Int32)rs.Fields.Item("U_850Delay").Value; // 01-31-2023
            }
            catch (Exception e)
            {
                String oErr = e.Message;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
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
