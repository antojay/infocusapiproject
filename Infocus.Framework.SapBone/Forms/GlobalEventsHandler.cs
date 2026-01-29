using System;
using System.Security.Permissions;
using System.Threading;

using SAPbouiCOM;
using System.Collections.Generic;

namespace Infocus.Framework.SapBone.Forms
{
    public abstract class GlobalEventsHandler : BoneEvent
    {
        private static List<String> _modelFormList = new List<String>() { BoneFormConstants.ExceptionFormUid };
        public GlobalEventsHandler()
        {
        }

        public static void RegisterModelForm(String formUid)
        {
            if(!_modelFormList.Contains(formUid))
            {
                _modelFormList.Add(formUid);
            }
        }

        public static void UnregisterModelForm(String formUid)
        {
            if(_modelFormList.Contains(formUid))
            {
                _modelFormList.Remove(formUid);
            }
        }

        [BoneListener(BoEventTypes.et_CLICK, true)]
        public virtual Boolean OnBeforeClick(ItemEvent itemEvent)
        {
            return CheckForModal(itemEvent.FormUID);
        }

        [BoneListener(BoEventTypes.et_FORM_ACTIVATE, true)]
        public virtual Boolean OnBeforeFormActivate(ItemEvent itemEvent)
        {
            return CheckForModal(itemEvent.FormUID);
        }

        [BoneListener(BoEventTypes.et_FORM_ACTIVATE, false)]
        public virtual void OnAfterFormActivate(ItemEvent itemEvent)
        {
            CheckForModal(itemEvent.FormUID);
        }

        [BoneListener(BoEventTypes.et_MENU_CLICK, true)]
        public virtual Boolean OnBeforeMenuClick(MenuEvent ev)
        {
            String currentFormUid = String.Empty;
            try
            {
                if(BoneConnectionContext.Application.Forms.ActiveForm != null)
                {
                    currentFormUid = BoneConnectionContext.Application.Forms.ActiveForm.TypeEx;
                }
            }
            catch(Exception)
            {
            }
            return CheckForModal(currentFormUid);
        }
        // 08-20-2019 begin
        [BoneListener(BoEventTypes.et_MENU_CLICK, false)]
        public virtual void OnAfterMenuClick(MenuEvent ev)
        {
            String currentFormUid = String.Empty;
            try
            {
                if (BoneConnectionContext.Application.Forms.ActiveForm != null)
                {
                    currentFormUid = BoneConnectionContext.Application.Forms.ActiveForm.TypeEx;
                }
            }
            catch (Exception)
            {
            }
         }
            

    /*    [BoneListener(BoEventTypes.et_FORM_CLOSE, false)]
        public virtual void OnAfterFormClose(ItemEvent ev)
        {
            String modalFormType = BoneUserInterfaceManager.Instance.GetModalFormType();
            if(modalFormType != null && ev.FormTypeEx.Equals(modalFormType, StringComparison.InvariantCultureIgnoreCase))
            {
                BoneUserInterfaceManager.Instance.ResetModalState();
            }
        }*/
        // 08-20-2019 end
        private static Boolean CheckForModal(String formType)
        {
            if(BoneUserInterfaceManager.Instance.ModalFormExists)
            {
                String processFormType = formType ?? String.Empty;
                string modalType = BoneUserInterfaceManager.Instance.GetModalFormType();
                if(!processFormType.Equals(modalType) && modalType != null)
                {
                    try
                    {
                        Form form = BoneConnectionContext.Application.Forms.GetForm(modalType, 1);
                        if(form != null)
                        {
                            form.Select();
                            return false;
                        }
                    }
                    catch(Exception)
                    {
                    }
                }
            }

            return true;
        }

        //private static Boolean CheckForModal(String currentFormUid)
        //{
        //    if(_modelFormList.Count > 0)
        //    {
        //        foreach(String uid in _modelFormList)
        //        {
        //            try
        //            {
        //                Form form = BoneConnectionContext.Application.Forms.GetForm(uid, 1);
        //                if(form != null && form.UniqueID != currentFormUid)
        //                {
        //                    form.Select();
        //                    return false;
        //                }
        //            }
        //            catch(Exception)
        //            {
        //            }
        //        }
        //    }
        //if(itemEvent.FormUID != BoneFormConstants.ExceptionFormUid)
        //{
        //    try
        //    {
        //        Form form = BoneConnectionContext.Application.Forms.GetForm(BoneFormConstants.ExceptionFormUid, 1);
        //        if(form != null)
        //        {
        //            form.Select();
        //            return false;
        //        }
        //    }
        //    catch(Exception)
        //    {
        //    }
        //}
        //    return true;
        //}
    }
}

