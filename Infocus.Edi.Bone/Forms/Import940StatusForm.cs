using System;
using Infocus.Framework.SapBone;
using log4net;
using SAPbobsCOM;
using SAPbouiCOM;

namespace Infocus.Edi.Bone.Forms
{
    public sealed class Import940StatusForm : BoneForm
    {
        public Import940StatusForm()
        {
            FormType = SAPUIConstants.IMPORT940_FORM_TYPE;
        }

        internal static void InitializeForm(Form form)
        {
            Recordset rs = BoneConnectionContext.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select top 1 * from [@INFO_940_IMPORT] With(NOLOCK)");
                rs.MoveFirst();
                if(!rs.EoF)
                {
                    form.DataSources.UserDataSources.Item("UdsStatus").Value = StringUtility.EmptyStringIfNull((String)rs.Fields.Item("U_InfoStatus").Value);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }
    }
}
