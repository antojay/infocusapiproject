using System;
using System.Linq;
using Infocus.Framework.SapBone;
using log4net;
using SAPbobsCOM;
using SAPbouiCOM;
using Infocus.WebApi.Data;
using Infocus.Common;

namespace Infocus.Edi.Bone.Forms
{
    public sealed class Reject850Form : BoneForm
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Reject850Form));
        public Reject850Form()
        {
            FormType = SAPUIConstants.REJECT_850_FORM_TYPE;
        }

        internal static void InitializeForm(Form form)
        {
            String connectionString = AppSettings.Instance.GetConnectionString();
            try
            {
                   using (WebApiDbContext apiContext = new WebApiDbContext(connectionString))
                    {
                        form.Freeze(true);
                        // Format Matrix
                        SAPbouiCOM.Item oItem = form.Items.Item("3");
                        SAPbouiCOM.Matrix oMatrix = (SAPbouiCOM.Matrix)oItem.Specific;
                        SAPbouiCOM.Columns oColumns = oMatrix.Columns;
                        //oMatrix.AutoResizeColumns();
                        form.DataSources.DataTables.Add("Open850s");
                        form.DataSources.DataTables.Item("Open850s").Clear();
                        EdiAddon.LoadOpen850s(form, oMatrix);
                        oItem.AffectsFormMode = false;
                        form.Mode = BoFormMode.fm_OK_MODE;
                        form.Freeze(false);
                        form.Mode = BoFormMode.fm_OK_MODE;
                        form.Refresh();
                        if (oMatrix.RowCount == 0)
                        {
                            System.Windows.Forms.MessageBox.Show("There are no unprocessed 850s");
                            form.Close();
                        }
                    }
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                throw new InfocusException("Could not connect to database", ex);
            }
        }
    }
}
