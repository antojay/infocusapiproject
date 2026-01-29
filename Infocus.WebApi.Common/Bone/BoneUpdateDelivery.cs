using System;
using System.Collections;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using Infocus.Common;
using Infocus.WebApi.Common.Bone;
using log4net;
using SAPbobsCOM;
using SAPbouiCOM;

namespace Infocus.WebApi.Common.Bone
{

    public sealed class BoneUpdateDelivery
    {
        private Company _company = null;
        public static string oServerName;
        public static string oDatabaseName;
        public static string oDbUser;
        public static string oDbPassword;
        public static string oSBO_User;
        public static string oSBO_Pw;
        public static string sPath = System.IO.Directory.GetCurrentDirectory().ToString();
        public static SAPbobsCOM.Company oCompany;
        private static ILog _logger = LogManager.GetLogger(typeof(BoneUpdateDelivery));

        public BoneUpdateDelivery()
        {
            string[] oDBData = BoneUpdateDelivery.getDbInfo();
            if (oDBData.Length >= 6)
            {
                string[] oValues = oDBData[0].Split('=');
                oServerName = oValues[1];
                oValues = oDBData[1].Split('=');
                oDatabaseName = oValues[1];
                oValues = oDBData[2].Split('=');
                oDbUser = oValues[1];
                oValues = oDBData[3].Split('=');
                oDbPassword = oValues[1];
                oValues = oDBData[4].Split('=');
                oSBO_User = oValues[1];
                oValues = oDBData[5].Split('=');
                oSBO_Pw = oValues[1];
            }
            _company = company;
        }

        public Company getCompany()
        {
            return _company;
        }

        public bool ProcessDeliveryRecord(Delivery pDelivery)
        {
            _logger.Debug("Entering ProcessDeliveryRecord");
            bool bUpdated = false;
            Documents oDelivery = _company.GetBusinessObject(BoObjectTypes.oDeliveryNotes) as Documents;
            try
            {
                bool bFound = oDelivery.GetByKey(pDelivery.DocEntry);
                if (bFound == true)
                {
                    oDelivery.UserFields.Fields.Item("U_InfoW2856").Value = "Y";

                    int oRet = oDelivery.Update();
                    if (oRet != 0)
                    {
                        String msg = _company.GetLastErrorDescription();
                        int oErrCode = _company.GetLastErrorCode();
                        _logger.Error(msg);
                    }
                    else
                    {
                        bUpdated = true;

                    }
                    _logger.Debug("Leaving ProcessDeliveryRecord.  Delivery# " + pDelivery.DocNum.ToString());
                }
                return bUpdated;
            }
            catch (Exception soEx)
            {
                String oErrMsg = soEx.Message;
                _logger.Debug("Error in BoneUpdateDelivery: " + oErrMsg + " (" + soEx.InnerException.Message + ")");
                return false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(oDelivery);
            }
        }

        public static bool DbConnect()
        {
            bool bConnected = false;
            oSqlConnection = new System.Data.SqlClient.SqlConnection();
            string oSqlString = "Server=" + oServerName + ";";

            oSqlString += "Database=" + oDatabaseName + ";";
            oSqlString += "Trusted_Connection=true;";
            oSqlString += "User Id= " + oDbUser + ";Password=" + oDbPassword;

            oSqlConnection.ConnectionString = oSqlString;
            Import_Log.LogEntry("SQL Connection String: " + oSqlString);

            try
            {
                oSqlConnection.Open();
                bConnected = true;
            }
            catch (Exception e)
            {
                string oError = e.Message;
                Import_Log.LogEntry("Server: " + oServerName + ", SBO Database: " + oDatabaseName + ": " + oError);
                bConnected = false;
            }
            return bConnected;
        }

        public static string[] getDbInfo()
        {
            string[] oLines = new string[6];
            int counter = 0;
            string oInputString;
            // Read lines one - three two of the file 
            string oFileName = sPath + @"\Properties.txt";
            System.IO.StreamReader oPropertiesFile = new System.IO.StreamReader(oFileName);
            while ((oInputString = oPropertiesFile.ReadLine()) != null && counter < 6)
            {
                oLines[counter] = oInputString;
                counter++;
            }
            oPropertiesFile.Close();
            return oLines;
        }
    }
}
