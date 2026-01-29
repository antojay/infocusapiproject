using System;
using System.Linq;
using System.Data.SqlClient; // 06-29-2019
using SAPbouiCOM;
using SAPbobsCOM;
using Infocus.Framework.SapBone;
using log4net;
using Infocus.Framework.SapBone.Forms;
using Infocus.WebApi.Data;
using Infocus.WebApi.Common.Bone;
//using SAPbobsCOM; // 02-02-2022

namespace Infocus.Edi.Bone.Forms
{

    internal sealed class Process940FormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Process940FormOkButton));
        public Process940FormOkButton()
            : base()
        {
            FormType = SAPUIConstants.PROCESS_940_FORM_TYPE;
            ItemUID = "BtOk";
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            // 04-23-2020 don't allow start of import if current time is +/- 10 minutes of autoimport start
            DateTime oCurrentTime = DateTime.Now;
            String Import940Status = get940ImportStatus(); 
            int oMinutes = oCurrentTime.TimeOfDay.Minutes; 
            String o940Status = get940ImportStatus(); 
            if (System.Diagnostics.Process.GetProcessesByName("Infocus.Edi.AutoProcess").Length > 0)
            {
                BoneConnectionContext.Application.MessageBox("Automated Import is currently running -- please try again later", 1, "Ok");
            }

            // Check import status to see if import is already running
            else if (o940Status == "ERROR")
            {
                BoneConnectionContext.Application.MessageBox("Unable to get Import status -- please correct the issue and retry", 1, "Ok");
            }
            else if (o940Status == "RUNNING")
            {
                BoneConnectionContext.Application.MessageBox("Import is currently running -- please try again later", 1, "Ok");
            }
          
            else
            {
                Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
                form.Items.Item("BtOk").Enabled = false;
                BoneSalesOrderDataLayer salesOrderDAL = new BoneSalesOrderDataLayer(BoneConnectionContext.Company);
                ProgressBar progressBar = null;
                bool bUpdatedStatus = false; // 07-13-2021
                try
                {
                    set940ImportStatus("RUNNING"); 
                    bUpdatedStatus = true;  
                 
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
        
                    string oConnectionStr = AppSettings.Instance.GetConnectionString();
                    using (WebApiDbContext webApiDbContext = new WebApiDbContext(AppSettings.Instance.GetConnectionString()))
                    {
                        var records = (from recs in webApiDbContext.Edi940HeaderRecords.OrderBy(lor => lor.HeaderId)
                                       where recs.Processed == false  
                                       select recs).ToList();

                        progressBar = BoneConnectionContext.Application.StatusBar.CreateProgressBar("Processing Records. Please wait", records.Count(), false);
                        int ctr = 0;
                        Int32 lastRecsProcessed = 0; 
                        foreach (var record in records)
                        {
                            _logger.Debug("Processing 940 with Record Key " + record.HeaderId);
                            try  
                            {
                                var documentKey = salesOrderDAL.Process940Record(record, false, "940");
                                int iNext = ++ctr;
                               
                                if (documentKey != null && documentKey.DocEntry != null && documentKey.DocEntry > 0)
                                {  
                                    _logger.Debug("Successfully created sales order");
                                    DateTime now = DateTime.Now;
                                    record.Processed = true;
                                    record.ProcessedDateTime = now;
                                    record.SalesOrderKey = documentKey.DocEntry;
                                    webApiDbContext.SaveChanges();
                                    String msg = "Sucessfully processed 940 Key #" + record.HeaderId + ", Sales Order #" + documentKey.DocNum;
                                    _logger.Debug(msg);
                                    BoneConnectionContext.Application.StatusBar.SetSystemMessage(msg, BoMessageTime.bmt_Medium, BoStatusBarMessageType.smt_Success);
                                    Int32 ordersCreated = Int32.Parse(form.DataSources.UserDataSources.Item("UdsOrdCre").Value);
                                    ordersCreated++;
                                    form.DataSources.UserDataSources.Item("UdsOrdCre").Value = ordersCreated.ToString();
                                    _setErrorMessage(record.HeaderId, " ", true); 
                                    int salesOrder = documentKey.DocEntry;
                                    UpdateSOLineNo(BoneConnectionContext.Company, salesOrder); 
                                } 
                            }
                            catch (Exception s0)  
                            {
                                string oErrMesg = s0.Message;
                                String msg = "Error processing 940 Key #" + record.HeaderId + "=> " + oErrMesg;
                                _logger.Debug(msg);
                            }
                           

                            Int32 recordsProcessed = 0;
                            try
                            {
                               recordsProcessed = Int32.Parse(form.DataSources.UserDataSources.Item("UdsRecPro").Value);
                            }
                            catch (Exception rp)
                            {
                                String oErr = rp.Message;
                                recordsProcessed = lastRecsProcessed; 
                            }
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
                               System.Threading.Tasks.Task.Delay(oDelaySec); 
                            }
                            catch
                            {

                            }
                        }
                    }
                    if (bUpdatedStatus == true)
                    {
                        set940ImportStatus("IDLE");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    String oErrMsg = ex.Message;
                    System.Windows.Forms.MessageBox.Show(oErrMsg);
                     if (bUpdatedStatus == true)
                    {
                        set940ImportStatus("IDLE");
                    }
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
                    String oQuery = "UPDATE InfocusEdi940HeaderRecord set ErrorMessage = '" + pErrorMessage.Trim() +
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
                    string oQuery = "UPDATE InfocusEdi940HeaderRecord set ErrorMessage = '" + errorMesg.Trim() + "'";

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
         public static void set940ImportStatus(String pStatus)
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

        public static String get940ImportStatus()
        {
            String Import940Status = "ERROR";
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
                                    throw new Exception("Could get 940 Import status");
                                }
                                Import940Status = (String)reader[0];
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
            return Import940Status;
        }
         public static void UpdateSOLineNo(SAPbobsCOM.Company _company, int docEntry)
        {
            _company.StartTransaction(); 
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
                _logger.Debug("Error updating null U_InfoW2LNo for 940 with SalesOrderKey = " + docEntry.ToString() + " => " + e.Message);
            }
                        finally
            {
                if (_company.InTransaction)
                {
                    _company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
            }
            
        }
      
    }
}
