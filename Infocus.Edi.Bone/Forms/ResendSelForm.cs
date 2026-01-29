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
    public sealed class ResendSelForm : BoneForm
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ResendSelForm));
        public ResendSelForm()
        {
            FormType = SAPUIConstants.REJECT_SEL_FORM_TYPE;
        }

        internal static void InitializeForm(Form form)
        {
            String connectionString = AppSettings.Instance.GetConnectionString();
            try
            {
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }
    }
}
