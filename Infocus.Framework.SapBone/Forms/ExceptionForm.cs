using System;
using SAPbouiCOM;

namespace Infocus.Framework.SapBone.Forms
{
    public class ExceptionForm : BoneForm
    {
        public static void ShowModal(Exception ex)
        {
            ShowModal(null, ex);
        }

        public static void ShowModal(String message, Exception ex)
        {
            String messageToDisplay =
                String.IsNullOrWhiteSpace(message)
                ? ex.Message
                : message;

            if(BoneConnectionContext.Application != null)
            {
                try
                {
                    Form tempForm = BoneConnectionContext.Application.Forms.Item(BoneFormConstants.ExceptionFormUid); //Form is already present
                    if(tempForm != null)
                    {
                        return;
                    }
                }
                catch(Exception)
                {
                }
                String xml = Properties.Resources.ExceptionForm;
                BoneUserInterfaceManager.Instance.LoadXml(xml);
                if(ex == null)
                {
                    ex = new Exception();
                }
                Form form = BoneConnectionContext.Application.Forms.Item(BoneFormConstants.ExceptionFormUid);

                Folder folder = form.Items.Item("8").Specific as Folder;
                folder.Select();

                folder = form.Items.Item("9").Specific as Folder;
                folder.GroupWith("8");

                form.Freeze(true);
                try
                {
                    form.PaneLevel = 1;

                    Item item = form.Items.Item(BoneFormConstants.ExceptionFormSourceEdit);
                    EditText et = item.Specific as EditText;
                    et.Value = ex.Source;
                    ((EditText)form.Items.Item(BoneFormConstants.ExceptionFormDetailsEdit).Specific).Active = true;
                    item.Enabled = false;

                    item = form.Items.Item(BoneFormConstants.ExceptionFormMessageEdit);

                    et = item.Specific as EditText;
                    et.Value = messageToDisplay;
                    ((EditText)form.Items.Item(BoneFormConstants.ExceptionFormDetailsEdit).Specific).Active = true;
                    item.Enabled = false;

                    item = form.Items.Item(BoneFormConstants.ExceptionFormDetailsEdit);

                    et = item.Specific as EditText;
                    et.Value = ex != null ? ex.ToString() : "";
                    ((EditText)form.Items.Item(BoneFormConstants.ExceptionFormExceptionEdit).Specific).Active = true;
                    item.Enabled = false;

                    item = form.Items.Item(BoneFormConstants.ExceptionFormExceptionEdit);

                    et = item.Specific as EditText;
                    et.Value = GetExceptionText(ex);
                    ((EditText)form.Items.Item(BoneFormConstants.ExceptionFormSourceEdit).Specific).Active = true;
                    item.Enabled = false;

                    item = form.Items.Item(BoneFormConstants.ExceptionFormStackTraceEdit);

                    et = item.Specific as EditText;
                    et.Value = GetStackTraceText(ex);
                }
                finally
                {
                    form.Freeze(false);
                }
            }
            else
            {
                System.Windows.Forms.MessageBox.Show(messageToDisplay + ": " + ex.Message,
                    "Essentials Error",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            }
        }

        public ExceptionForm()
        {
            FormType = BoneFormConstants.ExceptionFormType;
        }

        private static String GetExceptionText(Exception e)
        {
            String str = null;
            if(e != null)
            {
                str = e.Message;
                Exception workingException = e;
                while(workingException != null)
                {
                    str = workingException.Message;
                    workingException = workingException.InnerException;
                }
            }

            return str;
        }

        private static String GetStackTraceText(Exception e)
        {
            String str = null;
            if(e != null)
            {
                str = e.StackTrace;
                Exception workingException = e;
                while(workingException != null)
                {
                    str = workingException.StackTrace;
                    workingException = workingException.InnerException;
                }
            }
            return str;
        }
    }

    public class ExceptionFormExceptionFolderItem : BoneItem
    {
        public ExceptionFormExceptionFolderItem()
        {
            FormType = BoneFormConstants.ExceptionFormType;
            ItemUID = "8";
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, true)]
        public virtual Boolean OnBeforeItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            form.PaneLevel = 1;
            return true;
        }
    }

    public class ExceptionFormStackTraceFolderItem : BoneItem
    {
        public ExceptionFormStackTraceFolderItem()
        {
            FormType = BoneFormConstants.ExceptionFormType;
            ItemUID = "9";
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, true)]
        public virtual Boolean OnBeforeItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            form.PaneLevel = 2;
            return true;
        }
    }
}
