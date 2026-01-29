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
    internal sealed class Resend856FormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Resend856FormOkButton));
        public Resend856FormOkButton()
            : base()
        {
            FormType = SAPUIConstants.RESEND_856_FORM_TYPE;
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
                                if (purchaseOrd.Trim().Length > 0)
                                {
                                    using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                                    {
                                        sqlConnection.Open();
                                        using (SqlCommand command = new SqlCommand("UPDATE InfocusEdi850HeaderRecord set Processed856 = 0 where PurchaseOrderReference = '" + purchaseOrd.Trim() + "' ", sqlConnection))
                                        {
                                            command.ExecuteNonQuery();
                                            String msg = "Set purchase order " + purchaseOrd.Trim() + " to resend 856";
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
