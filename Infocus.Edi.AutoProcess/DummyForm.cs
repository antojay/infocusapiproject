using Infocus.WebApi.Common.Bone;
using log4net;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Text;
using System.Windows.Forms;


namespace Infocus.Edi.AutoProcess
{
    public partial class DummyForm : Form
    {
        public DummyForm(string ordSource)
        {
            try
            {
                InitializeComponent();

                bool bConnected = InfocusEdiAutoProcess.DbConnect();

                if (bConnected == true)
                {
                    bool bUpdatedStatus = false; // 07-14-2021
                         
                    try
                    {
                        InfocusEdiAutoProcess.oCompany = new SAPbobsCOM.Company();
                        InfocusEdiAutoProcess.oCompany.CompanyDB = InfocusEdiAutoProcess.oDatabaseName;
                        InfocusEdiAutoProcess.oCompany.Server = InfocusEdiAutoProcess.oServerName;
                        // 03-24-2026 lrussell begin
                        if (InfocusEdiAutoProcess.oDbVersion == "2022")
                        {
                            InfocusEdiAutoProcess.oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2022;
                        }
                        else if (InfocusEdiAutoProcess.oDbVersion == "2019")
                        {
                            InfocusEdiAutoProcess.oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2019;
                        }
                        else if (InfocusEdiAutoProcess.oDbVersion == "2017")
                        {
                            InfocusEdiAutoProcess.oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2017;
                        }
                        else
                        { // 03-24-2026 lrussell end
                            InfocusEdiAutoProcess.oCompany.DbServerType = SAPbobsCOM.BoDataServerTypes.dst_MSSQL2016;
                        }
                        InfocusEdiAutoProcess.oCompany.UserName = InfocusEdiAutoProcess.oSBO_User;
                        InfocusEdiAutoProcess.oCompany.Password = InfocusEdiAutoProcess.oSBO_Pw;
                        InfocusEdiAutoProcess.oCompany.UseTrusted = false;
                        InfocusEdiAutoProcess.oCompany.DbUserName = InfocusEdiAutoProcess.oDbUser;
                        InfocusEdiAutoProcess.oCompany.DbPassword = InfocusEdiAutoProcess.oDbPassword;

                        InfocusEdiAutoProcess.oCompany.UseTrusted = false;

                        int oResult = InfocusEdiAutoProcess.oCompany.Connect();
                        if (oResult != 0)
                        {
                            String sErrMsg = "";
                            InfocusEdiAutoProcess.oCompany.GetLastError(out oResult, out sErrMsg);
                            Import_Log.LogEntry("Error Connecting to SAP B1: " + sErrMsg);
                        }
                        bool oCompConnected = InfocusEdiAutoProcess.oCompany.Connected;
                        if (oCompConnected)
                        {
                            Import_Log.LogEntry("Connected to Company=" + InfocusEdiAutoProcess.oCompany.CompanyName + " User=" + InfocusEdiAutoProcess.oCompany.UserName);
                            Import_Log.LogEntry("Begin processing " + ordSource.Trim() + " transactions ");
                            // 07-12-2021 begin
                            try
                            {
                                if (get850ImportStatus() == "RUNNING")
                                {
                                    Import_Log.LogEntry("Import is currently running -- import aborted");
                                }
                                else if (get850ImportStatus() == "ERROR")
                                {
                                    Import_Log.LogEntry("Unable to get Import status -- import aborted");
                                }
                                else
                                {
                                    Import_Log.LogEntry("Automatic Processing set Import Status to RUNNING");
                                    set850ImportStatus("RUNNING");
                                    bUpdatedStatus = true;
                                    // 07-13-2021 end

                                    if (ordSource == "850" || ordSource == "") // 07-24-2023
                                    { 
                                        Import850.DoUpdate();
                                    }
                                    // 07-24-2023 begin
                                    else if (ordSource == "940")
                                    {
                                        Import940.DoUpdate();
                                    }
                                    // 07-24-2023 end
                                    // 07-13-2021  begin
                                    Import_Log.LogEntry("Automatic Processing set Import Status to IDLE");
                                    set850ImportStatus("IDLE");
                                }
                            }
                            catch (Exception r)
                            {
                                Import_Log.LogEntry("Error => " + r.Message);
                                if (bUpdatedStatus == true)
                                {
                                    set850ImportStatus("IDLE");
                                }
                            }
                                // 07-13-2021 end
                        }
                        else
                        {
                            Import_Log.LogEntry("Unable to open a connection to SAP Business One company");
                        }
                        this.Close();
                        InfocusEdiAutoProcess.oSqlConnection.Close();
                    }
                    catch (Exception ex2)
                    {
                        Import_Log.LogEntry(ex2.Message + ": " + ex2);
                        if (bUpdatedStatus == true)
                        {
                            set850ImportStatus("IDLE");
                        }
                        if (InfocusEdiAutoProcess.oCompany.Connected)
                        {
                            InfocusEdiAutoProcess.oCompany.Disconnect();
                        }
                        if (bConnected)
                        {
                            this.Close();
                            InfocusEdiAutoProcess.oSqlConnection.Close();
                        }
                    }
                }
                else
                {
                    string oErrorMesg = "Unable to establish connection to database! ";
                    if (InfocusEdiAutoProcess.oServerName != null)
                    {
                        oErrorMesg = oErrorMesg + " Server=>" + InfocusEdiAutoProcess.oServerName;
                    }
                    if (InfocusEdiAutoProcess.oDatabaseName != null)
                    {
                        oErrorMesg = oErrorMesg + " Database=>" + InfocusEdiAutoProcess.oDatabaseName;
                    }
                    Import_Log.LogEntry(oErrorMesg);
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                Import_Log.LogEntry(ex.Message);
                 if (InfocusEdiAutoProcess.oCompany.Connected)
                {
                    InfocusEdiAutoProcess.oCompany.Disconnect();
                }
                this.Close();
            }
            finally
            {

            }
            Import_Log.LogEntry("**** Exit Edi Automatic Processing ****");
        }

        private void DummyForm_Load(object sender, EventArgs e)
        {

        }

        //07-13-2021
        public static void set850ImportStatus(String pStatus)
        {
            try
            {
                System.Data.SqlClient.SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                // 03-24-2026 lrussell begin
                //String oSqlString = InfocusEdiAutoProcess.oSqlConnection.ConnectionString;
                string oSqlString = "Server=" + InfocusEdiAutoProcess.oServerName + ";";
                oSqlString += "Database=" + InfocusEdiAutoProcess.oDatabaseName + ";";
                 oSqlString += "User Id= " + InfocusEdiAutoProcess.oDbUser + ";Password=" + InfocusEdiAutoProcess.oDbPassword;
                // 03-24-2026 lrussell end 

                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "UPDATE [@INFO_850_IMPORT] set U_InfoStatus = '" + pStatus.Trim() + "'";
                    using (oConnection)
                    {
                        using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(oQuery, oConnection))
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

        public static String get850ImportStatus()
        {
            String Import850Status = "ERROR";
            try
            {
                System.Data.SqlClient.SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                /*string oSqlString = "Server=" + Infocus.Framework.SapBone.BoneConnectionContext.Company.Server + ";";
                oSqlString += "Database=" + oConnection.Database + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + AppSettings.Instance.DatabaseUser + ";Password=" + AppSettings.Instance.DatabasePassword; */
                // String oSqlString = InfocusEdiAutoProcess.oSqlConnection.ConnectionString;
                // 03-24-2026 lrussell begin
                string oSqlString = "Server=" + InfocusEdiAutoProcess.oServerName + ";";
                oSqlString += "Database=" + InfocusEdiAutoProcess.oDatabaseName + ";";
                oSqlString += "User Id= " + InfocusEdiAutoProcess.oDbUser + ";Password=" + InfocusEdiAutoProcess.oDbPassword;
                // 03-24-2026 lrussell end
                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "select IsNull(U_InfoStatus,'IDLE') as '850Status' from [@INFO_850_IMPORT] ";

                    using (oConnection)
                    {
                        using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(oQuery, oConnection))
                        {
                            using (System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Could get 850 Import status");
                                }
                                Import850Status = (String)reader[0];
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
            return Import850Status;
        }
        // 07-13-2021 end
    }
}