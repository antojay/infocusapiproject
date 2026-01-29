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
    internal sealed class ResendSelFormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ResendSelFormOkButton));
        public ResendSelFormOkButton()
            : base()
        {
            FormType = SAPUIConstants.REJECT_SEL_FORM_TYPE;
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
                SAPbouiCOM.Item oItem = form.Items.Item("2");
                EditText oEdit = (EditText)oItem.Specific;
                string date1 = oEdit.Value.ToString();
                oItem = form.Items.Item("3");
                oEdit = (EditText)oItem.Specific;
                string date2 = oEdit.Value.ToString();
                if (String.IsNullOrWhiteSpace(date1))
                {
                    date1 = "2001-JAN-01 12:00:00";
                }
                try
                {
                    EdiAddon.startDate = Convert.ToDateTime(date1);
                }
                catch (Exception dt)
                {
                    string errMsg = dt.Message;
                }
                if (String.IsNullOrWhiteSpace(date2))
                {
                    date2 = "2001-JAN-01 12:00:00";
                }
                try
                {
                    EdiAddon.endDate = Convert.ToDateTime(date2);
                }
                catch (Exception dt)
                {
                    string errMsg = dt.Message;
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
