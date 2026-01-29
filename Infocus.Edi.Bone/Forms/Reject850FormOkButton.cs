using System;
using System.Linq;
using System.Data.SqlClient;
using SAPbouiCOM;
using Infocus.Framework.SapBone;
using log4net;
using Infocus.Framework.SapBone.Forms;
using Infocus.WebApi.Data;
using Infocus.WebApi.Common.Bone;

namespace Infocus.Edi.Bone.Forms
{
    internal sealed class Reject850FormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Reject850FormOkButton));
        public Reject850FormOkButton()
            : base()
        {
            FormType = SAPUIConstants.REJECT_850_FORM_TYPE;
            ItemUID = "BtOk";
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            form.Items.Item("BtOk").Enabled = false;

            //form.Freeze(true);
            ProgressBar progressBar = null;
            try
            {
                SAPbouiCOM.Item oItem = form.Items.Item("3");
                SAPbouiCOM.Matrix oMatrix = (SAPbouiCOM.Matrix)oItem.Specific;
                int noRows = oMatrix.RowCount;
                oItem = form.Items.Item("5");
                EditText oEditTxt = (EditText)oItem.Specific;
                string rejectReason = "";
                try
                {
                    rejectReason = oEditTxt.Value.ToString();
                }
                catch (Exception rr)
                {
                    rejectReason = "";
                    _logger.Debug("Error getting reject reason =>" + rr.Message);
                }
                if (String.IsNullOrWhiteSpace(rejectReason))
                {
                    rejectReason = "Order Rejected";
                    _logger.Debug("Reject reason not set -- set default reason 'Order Rejected'");
                }
                // 03-29-2022 begin
                try
                {
                    rejectReason = rejectReason.Trim() + " rejected by user: " + BoneConnectionContext.Application.Company.UserName;
                }
                catch (Exception rr)
                {
                    _logger.Error("Error setting username =>" + rr.Message);
                }
                // 03-29-2022 end
                progressBar = BoneConnectionContext.Application.StatusBar.CreateProgressBar("Processing Records. Please wait", noRows, true);

                SAPbouiCOM.Columns oColumns = oMatrix.Columns;
                String connectionString = AppSettings.Instance.GetConnectionString();
                try
                {
                    using (WebApiDbContext apiContext = new WebApiDbContext(connectionString))
                    {
                        for (int ii = 1; ii <= noRows; ii++)
                        {
                            bool bIsSelected = oMatrix.IsRowSelected(ii);
                            Column oCol = oMatrix.Columns.Item("V_F");
                            Cells oCells = oCol.Cells;
                            CheckBox oChkBx = (CheckBox)oCells.Item(ii).Specific;
                            bool bIsChecked = oChkBx.Checked;
                            if (bIsChecked)
                            {
                                Column oCol2 = oMatrix.Columns.Item("V_1");
                                Cells oColCells = oCol2.Cells;
                                EditText oEdit = ((EditText)(oColCells.Item(ii).Specific));
                                string purchaseOrd = oEdit.Value;
                                Column oCol1 = oMatrix.Columns.Item("V_0");
                                Cells oCol1Cells = oCol1.Cells;
                                oEdit = ((EditText)(oCol1Cells.Item(ii).Specific));
                                string headerId = oEdit.Value;
                                // 08-26-2019 begin
                                try
                                {
                                    rejectReason = rejectReason.Replace("'", "''");
                                }
                                catch (Exception rr)
                                {
                                    _logger.Debug("Error updating apostrophe in reject reason text =>" + rr.Message);
                                }
                                // 08-26-2019 end
                                if (!String.IsNullOrWhiteSpace(purchaseOrd) && purchaseOrd.Trim().Length > 0
                                    && !String.IsNullOrWhiteSpace(headerId) && headerId.Trim().Length > 0)
                                {
                                    using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                                    {
                                        sqlConnection.Open();
                                        string oCmd = "UPDATE InfocusEdi850HeaderRecord set Processed=1, Processed856=1, Processed810=1, ErrorMessage = '" + rejectReason +
                                          "' where PurchaseOrderReference = '" + purchaseOrd.Trim() + "' and HeaderId = " + headerId;
                                        using (SqlCommand command = new SqlCommand(oCmd, sqlConnection))
                                        {
                                            command.ExecuteNonQuery();
                                            String msg = "Rejected EDI Trx # " + headerId + " for purchase order " + purchaseOrd.Trim();
                                            _logger.Debug(msg);
                                            BoneConnectionContext.Application.StatusBar.SetSystemMessage(msg, BoMessageTime.bmt_Medium, BoStatusBarMessageType.smt_Success);
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex2)
                {
                    _logger.Error(ex2);
                    String oErrMsg = ex2.Message;
                    System.Windows.Forms.MessageBox.Show(oErrMsg);
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
                form.Freeze(false);
                form.Close();
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
}
