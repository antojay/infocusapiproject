using System;
using System.Collections.Generic;
using SAPbouiCOM;
using Infocus.Framework.SapBone;

namespace Infocus.Edi.Bone.Forms
{
    public sealed class ProcessEdiFormCancelButton : BoneItem
    {
        public ProcessEdiFormCancelButton()
            : base()
        {
            FormType = SAPUIConstants.PROCESS_EDI_FORM_TYPE;
             ItemUID = "BtCancel";
        }
        [BoneListener(BoEventTypes.et_ITEM_PRESSED, false)]
        public void OnAfterItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            form.Close();
        }
    }
}
