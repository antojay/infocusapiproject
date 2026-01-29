using System;
using SAPbouiCOM;
using Infocus.Framework.SapBone;
using log4net;
using Infocus.Framework.SapBone.Forms;
using SAPbobsCOM;

namespace Infocus.Edi.Bone.Forms
{
    internal sealed class SettingsFormOkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SettingsFormOkButton));
        public SettingsFormOkButton()
            : base()
        {
            FormType = SAPUIConstants.SETTINGS_FORM_TYPE;
            ItemUID = "BtOk";
        }
        [BoneListener(BoEventTypes.et_ITEM_PRESSED, true)]
        public Boolean OnBeforeItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            try
            {
                String userName = form.DataSources.UserDataSources.Item("UdsUser").Value;
                String password = form.DataSources.UserDataSources.Item("UdsPass").Value;
                Int32 oDelaySec = 0;
                try
                {
                    oDelaySec = Int32.Parse(form.DataSources.UserDataSources.Item("ImpDelay").Value); // 01-31-2022
                }
                catch
                {
                    oDelaySec = 0;
                }
                Int32 oMaxRows = 0;
                try
                {
                    oMaxRows = Int32.Parse(form.DataSources.UserDataSources.Item("MaxRows").Value); // 08-08-2023
                }
                catch
                {
                    oMaxRows = 0;
                }
                Int32 oMaxTD = 0;
                try
                {
                    oMaxTD = Int32.Parse(form.DataSources.UserDataSources.Item("MaxTD").Value); // 08-14-2023
                }
                catch
                {
                    oMaxTD = 0;
                }

                if (String.IsNullOrWhiteSpace(userName))
                {
                    BoneConnectionContext.Application.SetStatusBarMessage("Invalid User Name", BoMessageTime.bmt_Short, true);
                    return false;
                }
                else if (String.IsNullOrWhiteSpace(password))
                {
                    BoneConnectionContext.Application.SetStatusBarMessage("Invalid Password", BoMessageTime.bmt_Short, true);
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                ExceptionForm.ShowModal(ex);
                return false;
            }
            return true;
        }
        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            Recordset rs = BoneConnectionContext.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                String userName = form.DataSources.UserDataSources.Item("UdsUser").Value;
                String password = form.DataSources.UserDataSources.Item("UdsPass").Value;
                Int32 oDelaySec = 0;
                try
                {
                    oDelaySec = Int32.Parse(form.DataSources.UserDataSources.Item("ImpDelay").Value); // 01-31-2022
                }
                catch
                {
                    oDelaySec = 0;
                }
                Int32 oMaxRows = 0;
                try
                {
                    oMaxRows = Int32.Parse(form.DataSources.UserDataSources.Item("MaxRows").Value); // 08-08-2023
                }
                catch
                {
                    oMaxRows = 0;
                }
                Int32 oMaxTD = 0;
                try
                {
                    oMaxTD = Int32.Parse(form.DataSources.UserDataSources.Item("MaxTD").Value); // 08-14-2023
                }
                catch
                {
                    oMaxTD = 0;
                }
                rs.DoQuery("select top 1 * from [@INFO_W2_SETTINGS] where code = 'DEFAULT'");
                rs.MoveFirst();
                String sql;
                if (rs.EoF)
                {
                    //sql = String.Format("INSERT INTO [@INFO_W2_SETTINGS] ([Code],[Name],[U_DbUser],[U_DbPass]) VALUES ('DEFAULT', 'DEFAULT' , '{0}', '{1}')", userName, password);
                    //sql = String.Format("INSERT INTO [@INFO_W2_SETTINGS] ([Code],[Name],[U_DbUser],[U_DbPass], [U_850Delay]) VALUES ('DEFAULT', 'DEFAULT' , '{0}', '{1}','{2}')", userName, password,  oDelaySec); // 01-31-2023
                    sql = String.Format("INSERT INTO [@INFO_W2_SETTINGS] ([Code],[Name],[U_DbUser],[U_DbPass], [U_850Delay],[U_MaxRows], [U_MaxTD]) VALUES ('DEFAULT', 'DEFAULT' , '{0}', '{1}','{2}','{3}')", userName, password, oDelaySec, oMaxRows, oMaxTD); // 08-14-2023
                }
                else
                {
                    // sql = String.Format("UPDATE [@INFO_W2_SETTINGS] SET U_DbUser = '{0}', U_DbPass = '{1}'  WHERE Code = 'DEFAULT'", userName, password);
                    //sql = String.Format("UPDATE [@INFO_W2_SETTINGS] SET U_DbUser = '{0}', U_DbPass = '{1}', U_850Delay = '{2}'  WHERE Code = 'DEFAULT'", userName, password, oDelaySec); // 01-31-2023
                    sql = String.Format("UPDATE [@INFO_W2_SETTINGS] SET U_DbUser = '{0}', U_DbPass = '{1}', U_850Delay = '{2}', U_MaxRows = '{3}', U_MaxTD = '{4}'  WHERE Code = 'DEFAULT'", userName, password, oDelaySec, oMaxRows, oMaxTD); // 08-14-2023
                }
                rs.DoQuery(sql);
                BoneConnectionContext.Application.SetStatusBarMessage("Successfully Updated EDI Settings", BoMessageTime.bmt_Short, false);
                form.Close();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                ExceptionForm.ShowModal(ex);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }
    }
}
