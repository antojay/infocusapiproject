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
    public sealed class Process940Form : BoneForm
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Process940Form));
        public Process940Form()
        {
            FormType = SAPUIConstants.PROCESS_940_FORM_TYPE;
        }

        internal static void InitializeForm(Form form)
        {
            String connectionString = AppSettings.Instance.GetConnectionString();
            try
            {
                if (form.TypeEx == "InfoW2940")
                {  
                    using (WebApiDbContext apiContext = new WebApiDbContext(connectionString))
                    {
                        Int32 recordCount = (from records in apiContext.Edi940HeaderRecords
                                             where records.Processed == false
                                             select records).Count();
                        form.DataSources.UserDataSources.Item("UdsCurRec").Value = recordCount.ToString();
                        form.DataSources.UserDataSources.Item("UdsRecPro").Value = "0";
                        form.DataSources.UserDataSources.Item("UdsOrdCre").Value = "0";
                        if (recordCount == 0)
                        {
                            form.Items.Item("BtOk").Enabled = false;
                        }
                        else
                        {
                            form.Items.Item("BtOk").Enabled = true;
                        }
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
