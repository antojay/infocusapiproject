using System;
using System.Collections;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using Infocus.Common;
using Infocus.WebApi.Common.Bone;
using log4net;

namespace Infocus.Edi.AutoProcess
{

    class InfocusEdiAutoProcess
    {
        public static SqlConnection oSqlConnection;
        public static string oServerName;
        public static string oDatabaseName;
        public static string oDbUser;
        public static string oDbPassword;
        public static Int32 oImpDelay; // 01-31-2023
        public static Int32 oMaxRows; // 08-08-2023
        public static Int32 oMaxTD; // 08-14-2023
        public static string oSBO_User;
        public static string oSBO_Pw;
        public static string sPath = System.IO.Directory.GetCurrentDirectory().ToString();
        public static SAPbobsCOM.Company oCompany;

        // 2026-0115 lar begin change location of log files
        //public static string sLogPath = Environment.ExpandEnvironmentVariables("%ProgramData%\\Infocus\\EDI\\Logs\\Autoimport");
        public static string sLogPath = Environment.ExpandEnvironmentVariables("\\trison-sql\\B1_SHR\\Infocus\\EDI\\Logs\\Autoimport");
        // 2026-0115 lar end

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] arg)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // 07-24-2023 begin
            string orderSource = "850";
           // arg = new string[] {"940"};
            if (arg.Length > 0)
            {
                orderSource = arg[0];
            }
            // 07-24-2023 end
            // 04-21-2022 remove username from path
            /*
            // 04-20-2022 begin
            String oUserName = System.Environment.UserName;
            if (String.IsNullOrWhiteSpace(oUserName))
            {
                oUserName = "Default";
            }
            sLogPath = sLogPath + "\\" + oUserName;
            // 04-20-2022 end
            */
            // 04-21-2022 end
            //Import_Log _Logger = new Import_Log(sLogPath);
            Import_Log _logger = new Import_Log(sLogPath, orderSource.Trim() + "_AutoImport_" ); // 08-04-2023

            // 07-24-2023 begin
            if (String.IsNullOrWhiteSpace(orderSource))
            {
                orderSource = "850";
            }
            //Import_Log.LogEntry("**** Starting 850 Import ****");
            Import_Log.LogEntry("**** Starting " + orderSource.Trim() + " Import ****");
            // 07-24-2023 end

            string[] oDBData = InfocusEdiAutoProcess.getDbInfo();
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
            Import_Log.LogEntry("Server: " + oServerName + ", SBO Database: " + oDatabaseName);
            InfocusEdiAutoProcess.oImpDelay = get850Delay(); // 01-31-2023
            InfocusEdiAutoProcess.oMaxRows = getMaxRows(); // 08-08-2023
            InfocusEdiAutoProcess.oMaxTD = getMaxTD(); // 08-14-2023
            Form oForm = new DummyForm(orderSource);
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

        // 01-31-2023 begin
        public static Int32 get850Delay()
        {
            Int32 oDelaySecs = 0;
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oSqlString = "Server=" + oServerName + ";";
                oSqlString += "Database=" + oDatabaseName + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + oDbUser + ";Password=" + oDbPassword;
                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "select IsNull(U_850Delay,0) as DelaySec from dbo.[@INFO_W2_SETTINGS] where Code = 'DEFAULT'";
                    using (SqlCommand command = new SqlCommand(oQuery, oConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oDelaySecs = 0;
                            }
                            else
                            {
                                string oValue = reader["DelaySec"].ToString();
                                oDelaySecs = 2;
                                try
                                {
                                    oDelaySecs = Convert.ToInt32(oValue);
                                }
                                catch
                                {
                                    oDelaySecs = 2;
                                }
                            }

                        }
                    }
                }
                catch (Exception e2)
                {
                    string oErr = e2.Message;
                }
                finally
                {
                    oConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
            }
            return oDelaySecs;
        }
        // 01-31-2023 end

        // 08-08-2023 begin
        public static Int32 getMaxRows()
        {
            Int32 oMaxRows = 0;
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oSqlString = "Server=" + oServerName + ";";
                oSqlString += "Database=" + oDatabaseName + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + oDbUser + ";Password=" + oDbPassword;
                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "select IsNull(U_MaxRows,0) as MaxRows from dbo.[@INFO_W2_SETTINGS] where Code = 'DEFAULT'";
                    using (SqlCommand command = new SqlCommand(oQuery, oConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oMaxRows = 0;
                            }
                            else
                            {
                                string oValue = reader["MaxRows"].ToString();
                                oMaxRows = 350;
                                try
                                {
                                    oMaxRows = Convert.ToInt32(oValue);
                                }
                                catch
                                {
                                    oMaxRows = 350;
                                }
                            }

                        }
                    }
                }
                catch (Exception e2)
                {
                    string oErr = e2.Message;
                }
                finally
                {
                    oConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
            }
            return oMaxRows;
        }
        // 08-08-2023 end
        // 08-14-2023 begin
        public static Int32 getMaxTD()
        {
            Int32 oMaxTD = 0;
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oSqlString = "Server=" + oServerName + ";";
                oSqlString += "Database=" + oDatabaseName + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + oDbUser + ";Password=" + oDbPassword;
                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "select IsNull(U_MaxTD,0) as MaxTD from dbo.[@INFO_W2_SETTINGS] where Code = 'DEFAULT'";
                    using (SqlCommand command = new SqlCommand(oQuery, oConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oMaxTD = 0;
                            }
                            else
                            {
                                oMaxTD = 21;
                                string oValue = reader["MaxTD"].ToString();
                                try
                                {
                                    oMaxTD = Convert.ToInt32(oValue);
                                }
                                catch
                                {
                                    oMaxTD = 21;
                                }
                            }

                        }
                    }
                }
                catch (Exception e2)
                {
                    string oErr = e2.Message;
                }
                finally
                {
                    oConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
            }
            return oMaxTD;
        }
        // 08-14-2023 end
        public static void updateEDILog(string cardCode, int ediDoc, int headerId, string ediTrxResult)
        {
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oSqlString = "Server=" + oServerName + ";";
                oSqlString += "Database=" + oDatabaseName + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + oDbUser + ";Password=" + oDbPassword;

                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "insert into InfocusEDI.dbo.[EDI_Trx_Log] ([Key_SeqNo], [SBOCardCode],[EDI_Document],[EDI_Trx_Type],[EDI_Trx_DateTime],[EDI_Trx_HeaderId],[EDI_Trx_Result]) VALUES (" +
                        "(select coalesce(Max(Key_SeqNo),0)+1 from  InfocusEDI.dbo.[EDI_Trx_Log]), " +
                        "'" + cardCode.Trim() + "', " + ediDoc + ", 'Auto Import','" + DateTime.Now.ToString() + "'," + headerId + ", '" + ediTrxResult + "')";
                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, InfocusEdiAutoProcess.oSqlConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception e2)
                {
                    string oErr = e2.Message;
                }
                finally
                {
                    oConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
            }
        }

        // 06-29=2019 begin
        public static void setErrorMessage(int headerId, string errorMesg, bool setProcessed)
        {
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oSqlString = "Server=" + oServerName + ";";
                oSqlString += "Database=" + oDatabaseName + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + oDbUser + ";Password=" + oDbPassword;

                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    // 04-21-2022 begin
                    //string oQuery = "UPDATE InfocusEdi850HeaderRecord set ErrorMessage = '" + errorMesg.Trim() + "' where HeaderId = " + headerId;
                    string oQuery = "UPDATE InfocusEdi850HeaderRecord set ErrorMessage = '" + errorMesg.Trim() + "'";
                    if (setProcessed == true)
                    {
                        oQuery = oQuery + ", Processed = 1 ";
                    }
                    oQuery = oQuery + " where HeaderId = " + headerId;
                    //04-21-2022 end
                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, InfocusEdiAutoProcess.oSqlConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                }
                catch (Exception e2)
                {
                    string oErr = e2.Message;
                }
                finally
                {
                    oConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
            }
        }
        // 06-29-2019 end
    }
}