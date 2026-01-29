using Infocus.Edi.Bone.Forms;
using Infocus.Framework.SapBone;
using Infocus.Framework.SapBone.Forms;
using SAPbouiCOM;
using System;

namespace Infocus.Edi.Bone
{
    internal sealed class EdiGlobalEventsHandler : GlobalEventsHandler
    {
        [BoneListener(BoEventTypes.et_MENU_CLICK, false)]
        public override void OnAfterMenuClick(MenuEvent ev)
        {
            if (ev.MenuUID == SAPUIConstants.MENU_EDI_PROCESS_850)
            {
                Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.ProcessEdiForm, SAPUIConstants.PROCESS_EDI_FORM_TYPE);
                try
                {
                    ProcessEdiForm.InitializeForm(form);
                }
                finally
                {
                    if (!form.Visible)
                    {
                        form.Visible = true;
                    }
                }
            }
            // 08-07-2023 begin
            else if (ev.MenuUID == SAPUIConstants.MENU_EDI_PROCESS_940)
            {
                Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Process940Form, SAPUIConstants.PROCESS_940_FORM_TYPE);
                try
                {
                    Process940Form.InitializeForm(form);
                }
                finally
                {
                    if (!form.Visible)
                    {
                        form.Visible = true;
                    }
                }
            }                
            // 02-26-2019 begin
            else if (ev.MenuUID == SAPUIConstants.MENU_EDI_RESEND_856)
            {
                Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Resend856Form, SAPUIConstants.RESEND_856_FORM_TYPE);
                try
                {
                    Resend856Form.InitializeForm(form);
                }
                finally
                {
                    if (!form.Visible)
                    {
                        form.Visible = true;
                    }
                }
            }
            else if (ev.MenuUID == SAPUIConstants.MENU_EDI_RESEND_810)
            {
                Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Resend810Form, "InfoW2R810");
               // Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.ResendSelForm, "InfoW2Sel");
                try
                {
                    //ResendSelForm.InitializeForm(form);
                    Resend810Form.InitializeForm(form);
                }
                finally
                {
                    if (!form.Visible)
                    {
                        form.Visible = true;
                    }
                }
            }
            // 02-26-2019 end
          
            // 07-23-2019 begin
            else if (ev.MenuUID == SAPUIConstants.MENU_EDI_PROCESS_180)
            {
                Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Process180Form, "InfoW2180");
                try
                {
                    Process180Form.InitializeForm(form);
                }
                finally
                {
                    if (!form.Visible)
                    {
                        form.Visible = true;
                    }
                }
            }
            // 07-23-2019 end
            // 08-20-2019 begin
            else if (ev.MenuUID == SAPUIConstants.MENU_EDI_REJECT_850)
            {
                Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Reject850Form, SAPUIConstants.REJECT_850_FORM_TYPE);
                try
                {
                    Reject850Form.InitializeForm(form);
                }
                finally
                {
                    if (!form.Visible)
                    {
                        form.Visible = true;
                    }
                }
            }
            // 08-20-2019 end
            else if (ev.MenuUID == SAPUIConstants.MENU_ITEM_SETUP)
            {
                ShowSettingsForm();
            }
            // 07-14-2021 begin
            else if (ev.MenuUID == SAPUIConstants.MENU_850_IMPORT_STATUS)
            {
                Show850ImportForm();
            }
            // 07-14-2021 end
        }

        public static Form ShowSettingsForm()
        {
            Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.SettingsForm, SAPUIConstants.SETTINGS_FORM_TYPE);
            try
            {
                SettingsForm.InitializeForm(form);
            }
            finally
            {
                if (!form.Visible)
                {
                    form.Visible = true;
                }
            }
            return form;
        }
        // 07-14-2021 begin
        public static Form Show850ImportForm()
        {
            Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Import850StatusForm, SAPUIConstants.IMPORT850_FORM_TYPE);
            try
            {
                Import850StatusForm.InitializeForm(form);
            }
            finally
            {
                if (!form.Visible)
                {
                    form.Visible = true;
                }
            }
            return form;
        }
        // 07-14-2021 end

        // 08-07-2023 begin

        public static Form Show940ImportForm()
        {
            Form form = BoneUserInterfaceManager.Instance.Show(false, Properties.Resources.Import940StatusForm, SAPUIConstants.IMPORT940_FORM_TYPE);
            try
            {
                Import850StatusForm.InitializeForm(form);
            }
            finally
            {
                if (!form.Visible)
                {
                    form.Visible = true;
                }
            }
            return form;
        }
        // 08-07-2023 end
    }
}
