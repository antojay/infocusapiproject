using System;
using SAPbouiCOM;
using Infocus.Framework.SapBone;
using log4net;
using Infocus.Framework.SapBone.Forms;
using SAPbobsCOM;

namespace Infocus.Edi.Bone.Forms
{
    internal sealed class Import940OkButton : BoneItem
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Import940OkButton));
        public Import940OkButton()
            : base()
        {
            FormType = SAPUIConstants.IMPORT940_FORM_TYPE;
            ItemUID = "BtOk";
        }
        [BoneListener(BoEventTypes.et_ITEM_PRESSED, true)]
        public Boolean OnBeforeItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            try
            {
                String oStatus = form.DataSources.UserDataSources.Item("UdsStatus").Value;
                if (String.IsNullOrWhiteSpace(oStatus) || (oStatus != "RUNNING" && oStatus != "IDLE"))
                {
                    BoneConnectionContext.Application.SetStatusBarMessage("Invalid Status -> status must be RUNNING or IDLE", BoMessageTime.bmt_Short, true);
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
                String oStatus = form.DataSources.UserDataSources.Item("UdsStatus").Value;

                rs.DoQuery("select top 1 * from [@INFO_940_IMPORT] ");
                rs.MoveFirst();
                String sql;
                if (rs.EoF)
                {
                    sql = String.Format("INSERT INTO [@INFO_940_IMPORT] ([Code],[Name],[U_InfoStatus]) VALUES ('Import940Status', '940 Import Status' , '{0}')", oStatus);
                }
                else
                {
                    sql = String.Format("UPDATE [@INFO_940_IMPORT] SET U_InfoStatus = '{0}'  WHERE Code = 'Import940Status'", oStatus);
                }
                rs.DoQuery(sql);
                BoneConnectionContext.Application.SetStatusBarMessage("Successfully Updated 940 Import Status", BoMessageTime.bmt_Short, false);
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
