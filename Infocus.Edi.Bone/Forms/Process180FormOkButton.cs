using System;
using System.Linq;
using System.Data.SqlClient; // 06-29-2019
using SAPbouiCOM;
using Infocus.Framework.SapBone;
using log4net;
using Infocus.Framework.SapBone.Forms;
using Infocus.WebApi.Data;
using Infocus.WebApi.Common.Bone;

namespace Infocus.Edi.Bone.Forms
{

    internal sealed class Process180FormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Process180FormOkButton));
        public Process180FormOkButton()
            : base()
        {
            FormType = SAPUIConstants.PROCESS_180_FORM_TYPE;
            ItemUID = "BtOk";
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            form.Items.Item("BtOk").Enabled = false;
            BoneReturnDataLayer returnDAL = new BoneReturnDataLayer(BoneConnectionContext.Company);
            ProgressBar progressBar = null;
            try
            {
                string oConnectionStr = AppSettings.Instance.GetConnectionString();
                using (WebApiDbContext webApiDbContext = new WebApiDbContext(AppSettings.Instance.GetConnectionString()))
                {
                    var records = (from recs in webApiDbContext.Edi180HeaderRecords.OrderBy(lor => lor.HeaderId)
                                   where recs.Processed == false
                                   select recs).ToList();

                    progressBar = BoneConnectionContext.Application.StatusBar.CreateProgressBar("Processing Records. Please wait", records.Count(), false);
                    int ctr = 0;
                    foreach (var record in records)
                    {
                        _logger.Debug("Processing 180 with HeaderId " + record.HeaderId);
                        try
                        {
                            var documentKey = returnDAL.Process180Record(record, false);
                            int iNext = ++ctr;
                            try
                            {
                                //progressBar.Value = ++ctr;
                                progressBar.Value = iNext;

                            }
                            catch (Exception pb)
                            {
                                string oErrMesg = pb.Message;
                                progressBar.Stop();
                                progressBar = BoneConnectionContext.Application.StatusBar.CreateProgressBar("Processing Records. Please wait", records.Count(), false);
                            }
                            if (documentKey.DocEntry > 0)
                            {
                                _logger.Debug("Successfully created sales order");
                                DateTime now = DateTime.Now;
                                record.Processed = true;
                                record.ProcessedDateTime = now;
                                record.ReturnOrderKey = documentKey.DocEntry;
                                webApiDbContext.SaveChanges();
                                String msg = "Sucessfully processed 180 Key #" + record.HeaderId + ", Return #" + documentKey.DocNum;
                                _logger.Debug(msg);
                                BoneConnectionContext.Application.StatusBar.SetSystemMessage(msg, BoMessageTime.bmt_Medium, BoStatusBarMessageType.smt_Success);
                                Int32 ordersCreated = Int32.Parse(form.DataSources.UserDataSources.Item("UdsOrdCre").Value);
                                ordersCreated++;
                                form.DataSources.UserDataSources.Item("UdsOrdCre").Value = ordersCreated.ToString();
                                _setErrorMessage(record.HeaderId, " ");
                            }
                            else
                            {
                                _setErrorMessage(record.HeaderId, Infocus.Framework.SapBone.BoneConnectionContext._850Error);
                            }
                        }
                        catch (Exception s0)
                        {
                            string oErrMesg = s0.Message;
                            String msg = "Error processing 180 HeaderId #" + record.HeaderId + "=> " + oErrMesg;
                            _logger.Debug(msg);
                        }
                        Int32 recordsProcessed = Int32.Parse(form.DataSources.UserDataSources.Item("UdsRecPro").Value);
                        recordsProcessed += 1;
                        form.DataSources.UserDataSources.Item("UdsRecPro").Value = recordsProcessed.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                // ExceptionForm.ShowModal(ex);
                String oErrMsg = ex.Message;
                System.Windows.Forms.MessageBox.Show(oErrMsg);
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
                    string oQuery = "UPDATE InfocusEdi180HeaderRecord set ErrorMessage = '" + errorMesg.Trim() + "'";

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

    }
}
