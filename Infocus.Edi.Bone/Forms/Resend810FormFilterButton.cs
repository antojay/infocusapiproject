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
    public sealed class Resend810FormFilterButton : BoneItem
    {
        public Resend810FormFilterButton()
            : base()
        {
            FormType = "InfoW2R810";
            ItemUID = "99";
        }
        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            String connectionString = AppSettings.Instance.GetConnectionString();

            try
            {
                using (WebApiDbContext apiContext = new WebApiDbContext(connectionString))
                {
                    form.Freeze(true);
                    // Format Matrix
                    DateTime startDate = DateTime.Now;
                    DateTime endDate = startDate;
                    SAPbouiCOM.Item oItem = form.Items.Item("3");
                    SAPbouiCOM.Matrix oMatrix = (SAPbouiCOM.Matrix)oItem.Specific;
                    oItem = form.Items.Item("5");
                    EditText oEdit = (EditText)oItem.Specific;
                    string ostart = oEdit.Value.ToString();
                    if (String.IsNullOrWhiteSpace(ostart))
                    {
                        ostart = "01/01/1970 00:00:01";
                    }

                    oItem = form.Items.Item("7");
                    oEdit = (EditText)oItem.Specific;
                    string oend = oEdit.Value.ToString();
                    if (String.IsNullOrWhiteSpace(oend))
                    {
                        oend = "12/31/2999";
                    }
                    oItem = form.Items.Item("10");
                    oEdit = (EditText)oItem.Specific;
                    string cardCode = oEdit.Value.ToString();
                    //form.DataSources.DataTables.Add("Sent810s");
                    form.DataSources.DataTables.Item("Sent810s").Clear();
                    if (startDate == endDate && String.IsNullOrWhiteSpace(oend) && String.IsNullOrWhiteSpace(ostart))
                    {
                        EdiAddon.LoadSent810s(form, oMatrix);
                    }
                    else
                    {
                        EdiAddon.LoadSent810s(form, oMatrix, ostart, oend, cardCode);
                    }
                    oItem.AffectsFormMode = false;
                    form.Mode = BoFormMode.fm_OK_MODE;
                    form.Freeze(false);
                    form.Mode = BoFormMode.fm_OK_MODE;
                    form.Refresh();
                    if (oMatrix.RowCount == 0)
                    {
                        System.Windows.Forms.MessageBox.Show("There are no deliverys with 856 already sent");
                        form.Close();
                    }
                }
            }
            catch (Exception ex)
            {
               // _logger.Error(ex);
                throw new Infocus.Common.InfocusException("Could not connect to database", ex);
            }
        }
    }
}
