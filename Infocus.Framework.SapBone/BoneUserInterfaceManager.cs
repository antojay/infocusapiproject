using SAPbouiCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Infocus.Framework.SapBone
{
    public class BoneUserInterfaceManager
    {
        private const String XmlFormUidPath = "Application/forms/action/form/@uid";
        private const String XmlFormTypePath = "Application/forms/action/form/@FormType";
        private String _modalFormType;
        internal Boolean ModalFormExists
        {
            get
            {
                return String.IsNullOrWhiteSpace(_modalFormType) == false;
            }
        }

        internal String GetModalFormType()
        {
            return _modalFormType;
        }

        internal void ResetModalState()
        {
            _modalFormType = null;
        }

        private static BoneUserInterfaceManager _instance = new BoneUserInterfaceManager();
        private BoneUserInterfaceManager()
        {
        }

        public static BoneUserInterfaceManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public void LoadXml(String xml)
        {
            String passedXml = xml.ToString();
            try
            {
            BoneConnectionContext.Application.LoadBatchActions(ref passedXml);
        }
            catch (Exception e)
            {
                String errMesg = e.Message;
                System.Windows.Forms.MessageBox.Show(errMesg);
            }
        }

        public Form ShowModal(String formXml, String formType)
        {
            if(BoneConnectionContext.Application != null
                && BoneConnectionContext.Company != null)
            {
                if(!ModalFormExists)
                {
                    //form.OnFormClosed += OnModalFormClosed;
                    Form launchedForm = Show(false, formXml, formType); 
                    _modalFormType = launchedForm.TypeEx;
                    if(launchedForm != null && launchedForm.TypeEx == formType)
                    {
                        return launchedForm;
                    }
                }
                return BoneConnectionContext.Application.Forms.GetForm(formType, 1);
            }
            try
            {
                return BoneConnectionContext.Application.Forms.GetForm(formType, 1);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public Form Show(Boolean allowMultipleForms, String formXml, String formType)
        {
            if(BoneConnectionContext.Application != null && !ModalFormExists)
            {
                try
                {
                    if(!allowMultipleForms)
                    {
                        Form tempForm = BoneConnectionContext.Application.Forms.GetForm(formType, 1);
                        if(tempForm != null)
                        {
                            if(tempForm.Visible == false)
                            {
                                tempForm.Visible = true;
                            }
                            tempForm.Select();
                            return tempForm;
                        }
                    }
                }
                catch(Exception f)
                {
                    String ErrMsg =f.Message;
                }
                Form launchedForm = null;
                
                
                LoadXml(formXml);
                
                launchedForm = BoneConnectionContext.Application.Forms.ActiveForm;
                if(launchedForm != null)
                {
                    if(launchedForm.TypeEx != formType)
                    {
                        launchedForm = BoneConnectionContext.Application.Forms.GetForm(formType, 1);
                    }
                }
                
                return launchedForm;
            }
            return null;
        }

        public System.Windows.Forms.DialogResult ShowMessageBox(String message, System.Windows.Forms.MessageBoxButtons buttons = System.Windows.Forms.MessageBoxButtons.OK)
        {
            String btn1Caption = null;
            String btn2Caption = null;
            String btn3Caption = null;
            int defaultButton = 1;
            switch(buttons)
            {
                case System.Windows.Forms.MessageBoxButtons.OKCancel:
                    btn1Caption = "OK";
                    btn2Caption = "Cancel";
                    defaultButton = 2;
                    break;
                case System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore:
                    btn1Caption = "Abort";
                    btn2Caption = "Retry";
                    btn3Caption = "Ignore";
                    defaultButton = 1;
                    break;
                case System.Windows.Forms.MessageBoxButtons.RetryCancel:
                    btn1Caption = "Retry";
                    btn2Caption = "Cancel";
                    defaultButton = 2;
                    break;
                case System.Windows.Forms.MessageBoxButtons.YesNo:
                    btn1Caption = "Yes";
                    btn2Caption = "No";
                    defaultButton = 2;
                    break;
                case System.Windows.Forms.MessageBoxButtons.YesNoCancel:
                    btn1Caption = "Yes";
                    btn2Caption = "No";
                    btn3Caption = "Cancel";
                    defaultButton = 3;
                    break;
                default:
                    btn1Caption = "OK";
                    defaultButton = 1;
                    break;
            }
            int clickedButton = BoneConnectionContext.Application.MessageBox(message, defaultButton, btn1Caption, btn2Caption, btn3Caption);
            switch(buttons)
            {
                case System.Windows.Forms.MessageBoxButtons.OKCancel:
                    if(clickedButton == 1)
                    {
                        return System.Windows.Forms.DialogResult.OK;
                    }
                    else
                    {
                        return System.Windows.Forms.DialogResult.Cancel;
                    }
                case System.Windows.Forms.MessageBoxButtons.AbortRetryIgnore:
                    if(clickedButton == 1)
                    {
                        return System.Windows.Forms.DialogResult.Abort;
                    }
                    else if(clickedButton == 2)
                    {
                        return System.Windows.Forms.DialogResult.Retry;
                    }
                    else
                    {
                        return System.Windows.Forms.DialogResult.Ignore;
                    }
                case System.Windows.Forms.MessageBoxButtons.RetryCancel:
                    if(clickedButton == 1)
                    {
                        return System.Windows.Forms.DialogResult.Retry;
                    }
                    else
                    {
                        return System.Windows.Forms.DialogResult.Cancel;
                    }
                case System.Windows.Forms.MessageBoxButtons.YesNo:
                    if(clickedButton == 1)
                    {
                        return System.Windows.Forms.DialogResult.Yes;
                    }
                    else
                    {
                        return System.Windows.Forms.DialogResult.No;
                    }
                case System.Windows.Forms.MessageBoxButtons.YesNoCancel:
                    if(clickedButton == 1)
                    {
                        return System.Windows.Forms.DialogResult.Yes;
                    }
                    else if(clickedButton == 2)
                    {
                        return System.Windows.Forms.DialogResult.No;
                    }
                    else
                    {
                        return System.Windows.Forms.DialogResult.Cancel;
                    }
                default:
                    return System.Windows.Forms.DialogResult.OK;
            }
        }
        public void SetStatusBarText(String message, Boolean isError = true)
        {
            BoneConnectionContext.Application.SetStatusBarMessage(message, BoMessageTime.bmt_Short, isError);
        }
        public void UpdateForm(Form form, String xml)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            doc.SelectSingleNode(XmlFormUidPath).Value = form.UniqueID;
            doc.SelectSingleNode(XmlFormTypePath).Value = form.TypeEx;
            LoadXml(doc.InnerXml);
        }
        public void SuspendModalFunctionality(Form form)
        {
            if(_modalFormType == form.TypeEx)
            {
                _modalFormType = null;
            }
        }
        public void ReinstateModalFunctionality(Form form)
        {
            if(_modalFormType == null)
            {
                form.Select();
                _modalFormType = form.TypeEx;
            }
        }
        public void ReactivateForm(Form form)
        {
            Form activeForm = BoneConnectionContext.Application.Forms.ActiveForm;
            if(activeForm != null && activeForm.UniqueID == form.UniqueID)
            {
                for(int xx = 0; xx < BoneConnectionContext.Application.Forms.Count; xx++)
                {
                    Form checkForm = BoneConnectionContext.Application.Forms.Item(xx);
                    if(checkForm.UniqueID != form.UniqueID && checkForm.Visible)
                    {
                        try
                        {
                            checkForm.Select();
                            break;
                        }
                        catch(Exception)
                        {
                        }
                    }
                }
            }
            try
            {
                form.Select();
            }
            catch(Exception)
            {
            }
        }
        public void ReplaceUidAndLoadToB1(String xml, String uid)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode node = doc.DocumentElement.SelectSingleNode(XmlFormUidPath);
            doc.SelectSingleNode(XmlFormUidPath).Value = uid;
            LoadXml(doc.InnerXml);
        }
    }
}
