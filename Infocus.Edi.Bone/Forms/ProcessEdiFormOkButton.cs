using System;
using System.Linq;
using System.Data.SqlClient; // 06-29-2019
using SAPbouiCOM;
using Infocus.Framework.SapBone;
using log4net;
using Infocus.Framework.SapBone.Forms;
using Infocus.WebApi.Data;
using Infocus.WebApi.Common.Bone;
using SAPbobsCOM; // 02-02-2022

namespace Infocus.Edi.Bone.Forms
{

    internal sealed class ProcessEdiFormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ProcessEdiFormOkButton));
        public ProcessEdiFormOkButton()
            : base()
        {
            FormType = SAPUIConstants.PROCESS_EDI_FORM_TYPE;
            ItemUID = "BtOk";
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            // 04-23-2020 don't allow start of import if current time is +/- 10 minutes of autoimport start
            DateTime oCurrentTime = DateTime.Now;
            String Import850Status = get850ImportStatus(); // 07-13-2021
            int oMinutes = oCurrentTime.TimeOfDay.Minutes; // 04-23-2020
            String o850Status = get850ImportStatus(); // 07-14-2021
            Int32 oMaxRows = get850MaxRows(); // 09-18-2023
            if (System.Diagnostics.Process.GetProcessesByName("Infocus.Edi.AutoProcess").Length > 0)
            {
                BoneConnectionContext.Application.MessageBox("Automated Import is currently running -- please try again later", 1, "Ok");
            }
            // 07-13-2021 begin remove time restriction
            // 04-23-2020 begin 
            /* else if (oMinutes > 30 && oMinutes < 40 && BoneConnectionContext.Application.Company.DatabaseName != "CORTRI_PROD") {
                 BoneConnectionContext.Application.MessageBox("Automated Import scheduled to run soon -- please try again later", 1, "Ok");
             } // 04-23-2020 end*/
            // 07-12-2021 begin do not allow manual import to be run if auto-import is scheduled in the then next 10 minutes
            /*
            else if (oMinutes > 30 && oMinutes < 40 && BoneConnectionContext.Application.Company.DatabaseName != "CORTRI_PROD")
            {
                 BoneConnectionContext.Application.MessageBox("Automated Import is running -> manual import cannot be run between between 30 past the hour and 40 past the hour", 1, "Ok");
            }
           */
            // Check import status to see if import is already running
            else if (o850Status == "ERROR")
            {
                BoneConnectionContext.Application.MessageBox("Unable to get 850 Import status -- please correcthe issue and retry", 1, "Ok");
            }
            else if (o850Status == "RUNNING")
            {
                BoneConnectionContext.Application.MessageBox("850 Import is currently running -- please try again later", 1, "Ok");
            }
            // 07-13-2021 end
            else
            {
                Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
                form.Items.Item("BtOk").Enabled = false;
                BoneSalesOrderDataLayer salesOrderDAL = new BoneSalesOrderDataLayer(BoneConnectionContext.Company);
                ProgressBar progressBar = null;
                bool bUpdatedStatus = false; // 07-13-2021
                try
                {
                    set850ImportStatus("RUNNING"); // 07-13-2021
                    bUpdatedStatus = true;  // 07-13-2021
                    // 01-31-2023 begin 
                    int oDelaySec = 0;
                    try
                    {
                        oDelaySec = AppSettings.Instance.ImpDelay;
                    }
                    catch
                    {
                        oDelaySec = 0;
                    }
                    oDelaySec = oDelaySec * 1000;
                    // 01-31-2023 end
                    string oConnectionStr = AppSettings.Instance.GetConnectionString();
                    using (WebApiDbContext webApiDbContext = new WebApiDbContext(AppSettings.Instance.GetConnectionString()))
                    {
                        var records = (from recs in webApiDbContext.Edi850HeaderRecords.OrderBy(lor => lor.HeaderId)
                                       where recs.Processed == false //&& recs.HeaderId == 149778
                                       //&& recs.CardCode == "TeeZed" 
                                       select recs).ToList();
                        // 09-18-2023 begin
                        Int32 oLastRec = records.Count();
                        if (records.Count() > oMaxRows && oMaxRows > 0)
                        {
                            oLastRec = oMaxRows;
                        }
                        Int32 oCurrRow = 0;
                        // 09-18-2023 begin
                        progressBar = BoneConnectionContext.Application.StatusBar.CreateProgressBar("Processing Records. Please wait", records.Count(), false);
                        int ctr = 0;
                        Int32 lastRecsProcessed = 0; // 03-13-2022
                        foreach (var record in records)
                        {
                            if (oCurrRow < oMaxRows)
                            { // 09-18-2023
                                _logger.Debug("Processing 850 with Record Key " + record.HeaderId);
                                try  // 05-21-2018  end
                                {
                                    var documentKey = salesOrderDAL.Process850Record(record, false, "850");
                                    // 05-21-2018  begin
                                    int iNext = ++ctr;
                                    oCurrRow = oCurrRow++; // 09-18-2023
                                    /*
                                    try
                                    {
                                        //progressBar.Value = ++ctr;
                                        progressBar.Value = iNext;
                                    }
                                    catch (Exception pb)
                                    {
                                        string oErrMesg = pb.Message;
                                        try
                                        {
                                            progressBar.Stop();
                                        }
                                        catch
                                        {
                                            _logger.Debug("Unable to stop progress bar");
                                        }
                                        progressBar = BoneConnectionContext.Application.StatusBar.CreateProgressBar("Processing Records. Please wait", records.Count(), false);
                                    }
                                     */
                                    // 05-21-2018  end
                                    // 07-16-2018 begin
                                    if (documentKey != null && documentKey.DocEntry != null && documentKey.DocEntry > 0)
                                    {  // 07-16-2018 end
                                        _logger.Debug("Successfully created sales order");
                                        DateTime now = DateTime.Now;
                                        record.Processed = true;
                                        record.ProcessedDateTime = now;
                                        record.SalesOrderKey = documentKey.DocEntry;
                                        webApiDbContext.SaveChanges();
                                        String msg = "Sucessfully processed 850 Key #" + record.HeaderId + ", Sales Order #" + documentKey.DocNum;
                                        _logger.Debug(msg);
                                        BoneConnectionContext.Application.StatusBar.SetSystemMessage(msg, BoMessageTime.bmt_Medium, BoStatusBarMessageType.smt_Success);
                                        Int32 ordersCreated = Int32.Parse(form.DataSources.UserDataSources.Item("UdsOrdCre").Value);
                                        ordersCreated++;
                                        form.DataSources.UserDataSources.Item("UdsOrdCre").Value = ordersCreated.ToString();
                                        _setErrorMessage(record.HeaderId, " ", true); // 07-21-2019
                                        int salesOrder = documentKey.DocEntry;//02-02-2022
                                        UpdateSOLineNo(BoneConnectionContext.Company, salesOrder); //02-02-2022
                                    } // 07-16-2018 
                                    // 03-15-2022 begin
                                    /*
                                // 06-29-2019 begin
                                else
                                {
                                    _setErrorMessage(record.HeaderId, Infocus.Framework.SapBone.BoneConnectionContext._850Error);
                                }
                                // 06-29-2019 end
                                     * */
                                    // 03-15-2022 end
                                }
                                catch (Exception s0)  // 05-21-2018  begin
                                {
                                    string oErrMesg = s0.Message;
                                    String msg = "Error processing 850 Key #" + record.HeaderId + "=> " + oErrMesg;
                                    _logger.Debug(msg);
                                }
                                // 05-21-2018 end
                                // 03-13-2022 begin

                                Int32 recordsProcessed = 0;
                                try
                                {
                                    //Int32 recordsProcessed = Int32.Parse(form.DataSources.UserDataSources.Item("UdsRecPro").Value);
                                    recordsProcessed = Int32.Parse(form.DataSources.UserDataSources.Item("UdsRecPro").Value);
                                }
                                catch (Exception rp)
                                {
                                    String oErr = rp.Message;
                                    recordsProcessed = lastRecsProcessed; // 03-13-2022
                                }
                                // 03-13-2022 end
                                recordsProcessed += 1;
                                lastRecsProcessed = recordsProcessed;
                                try
                                {
                                    form.DataSources.UserDataSources.Item("UdsRecPro").Value = recordsProcessed.ToString();
                                }
                                catch (Exception fd)
                                {
                                    String oErr = fd.Message;
                                }

                                try
                                {
                                    //System.Threading.Tasks.Task.Delay(4000); // pause between creating sales orders per Dan 01-23-2023
                                    System.Threading.Tasks.Task.Delay(oDelaySec); // 01-31-2023
                                }
                                catch
                                {

                                }
                            } // 09-18-2023
                        }
                    }
                    // 07-14-2021 begin
                    if (bUpdatedStatus == true)
                    {
                        set850ImportStatus("IDLE");
                    }
                    // 07-14-2021 end
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    // ExceptionForm.ShowModal(ex);
                    String oErrMsg = ex.Message;
                    System.Windows.Forms.MessageBox.Show(oErrMsg);
                    // 07-14-2021 begin
                    if (bUpdatedStatus == true)
                    {
                        set850ImportStatus("IDLE");
                    }
                    // 07-14-2021 end
                }
                finally
                {
                    if (progressBar != null)
                    {
                        try
                        {
                            progressBar.Stop();
                        }
                        catch
                        {

                        }
                    }
                }
            }
        }
        // 03-13-2022 begin
        public static void _setErrorMessage(int pHeaderId, string pErrorMessage, bool bOK)
        {
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oConnectionStr = AppSettings.Instance.GetConnectionString();
                oConnection.ConnectionString = oConnectionStr;
                try
                {
                    oConnection.Open();
                    String oQuery = "UPDATE InfocusEdi850HeaderRecord set ErrorMessage = '" + pErrorMessage.Trim() +
                 "' where HeaderId = " + pHeaderId;

                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, oConnection))
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
        // 03-13-2022 end
        // 06-29=2019 begin
        public static void _setErrorMessage(int headerId, string errorMesg)
        {
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oSqlString = "Server=" + Infocus.Framework.SapBone.BoneConnectionContext.Company.Server + ";";
                oSqlString += "Database=" + oConnection.Database + ";";
                oSqlString += "Trusted_Connection=true;";
                oSqlString += "User Id= " + AppSettings.Instance.DatabaseUser + ";Password=" + AppSettings.Instance.DatabasePassword;

                oConnection.ConnectionString = oSqlString;
                try
                {
                    oConnection.Open();
                    string oQuery = "UPDATE InfocusEdi850HeaderRecord set ErrorMessage = '" + errorMesg.Trim() + "'";

                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, oConnection))
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

        //07-13-2021
        public static void set850ImportStatus(String pStatus)
        {
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oConnectionStr = AppSettings.Instance.GetConnectionString();
                oConnection.ConnectionString = oConnectionStr;
                try
                {
                    oConnection.Open();
                    string oQuery = "UPDATE [@INFO_850_IMPORT] set U_InfoStatus = '" + pStatus.Trim() + "'";
                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, oConnection))
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
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oConnectionStr = AppSettings.Instance.GetConnectionString();
                oConnection.ConnectionString = oConnectionStr;
                try
                {
                    oConnection.Open();
                    string oQuery = "select IsNull(U_InfoStatus,'IDLE') as '850Status' from [@INFO_850_IMPORT] ";

                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, oConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
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

        // 02-02-2022 begin
        public static void UpdateSOLineNo(SAPbobsCOM.Company _company, int docEntry)
        {
            _company.StartTransaction(); // 02-16-2022
            try
            {
                _logger.Debug("Check & Update null U_InfoW2LNo for SO DocEntry = " + docEntry.ToString());
                Documents salesOrder = _company.GetBusinessObject(BoObjectTypes.oOrders) as Documents;
                salesOrder.GetByKey(docEntry);
                Document_Lines soLines = salesOrder.Lines;
                int noLines = soLines.Count;
                for (int i = salesOrder.Lines.Count - 1; i >= 0; i--)
                {
                    salesOrder.Lines.SetCurrentLine(i);
                    string lineNo = salesOrder.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value.ToString();
                    if (String.IsNullOrWhiteSpace(lineNo))
                    {
                        salesOrder.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = 0;
                        salesOrder.Update();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Debug("Error updating null U_InfoW2LNo for 850 with SalesOrderKey = " + docEntry.ToString() + " => " + e.Message);
            }
            // 02-16-2022 begin
            finally
            {
                if (_company.InTransaction)
                {
                    _company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
            }
            // 02-16-2022 end
        }
        // 02-02-2022 end

        // 09-18-2023 begin
        public static Int32 get850MaxRows()
        {
            Int32 oMaxRows = 0;
            try
            {
                SqlConnection oConnection = new System.Data.SqlClient.SqlConnection();
                string oConnectionStr = AppSettings.Instance.GetConnectionString();
                oConnection.ConnectionString = oConnectionStr;
                try
                {
                    oConnection.Open();
                    string oQuery = "select IsNull(U_MaxRows,0) as 'MaxRows' from [@INFO_W2_SETTINGS]";

                    using (oConnection)
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, oConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    throw new Exception("Could get 850 MaxRows");
                                }
                                try
                                {
                                    string oValue = reader[0].ToString();
                                    oMaxRows = Convert.ToInt32(oValue);
                                }
                                catch
                                {
                                    oMaxRows = 0;
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
        // 09-18-2023 end
    }
}
