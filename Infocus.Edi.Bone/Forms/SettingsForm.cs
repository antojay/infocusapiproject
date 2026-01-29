using System;
using Infocus.Framework.SapBone;
using log4net;
using SAPbobsCOM;
using SAPbouiCOM;

namespace Infocus.Edi.Bone.Forms
{
    public sealed class SettingsForm : BoneForm
    {
        public SettingsForm()
        {
            FormType = SAPUIConstants.SETTINGS_FORM_TYPE;
        }

        internal static void InitializeForm(Form form)
        {
            Recordset rs = BoneConnectionContext.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select top 1 * from [@INFO_W2_SETTINGS] where Code = 'DEFAULT'");
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    form.DataSources.UserDataSources.Item("UdsUser").Value = StringUtility.EmptyStringIfNull((String)rs.Fields.Item("U_DbUser").Value);
                    form.DataSources.UserDataSources.Item("UdsPass").Value = StringUtility.EmptyStringIfNull((String)rs.Fields.Item("U_DbPass").Value);
                    // 01-31-2023 begin
                    try
                    {
                        Int32 oDelaySec = Convert.ToInt32(rs.Fields.Item("U_850Delay").Value); 
                        form.DataSources.UserDataSources.Item("ImpDelay").Value = oDelaySec.ToString();
                    }
                    catch (Exception d0)
                    {
                        form.DataSources.UserDataSources.Item("ImpDelay").Value = Convert.ToString(0);
                    }
                    // 01-31-2023 end
                    // 08-08-2023 begin
                    try
                    {
                        Int32 oMaxRows = Convert.ToInt32(rs.Fields.Item("U_MaxRows").Value);
                        form.DataSources.UserDataSources.Item("MaxRows").Value = oMaxRows.ToString();
                    }
                    catch (Exception d1)
                    {
                        form.DataSources.UserDataSources.Item("MaxRows").Value = Convert.ToString(0);
                    }
                    // 08-08-2023 end
                    // 08-14-2023 begin
                    try
                    {
                        Int32 oMaxTD = Convert.ToInt32(rs.Fields.Item("U_MaxTD").Value);
                        form.DataSources.UserDataSources.Item("MaxTD").Value = oMaxTD.ToString();
                    }
                    catch (Exception d2)
                    {
                        form.DataSources.UserDataSources.Item("MaxTD").Value = Convert.ToString(0);
                    }
                    // 08-14-2023 end
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }
    }
}
