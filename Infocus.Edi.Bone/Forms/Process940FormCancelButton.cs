using System;
using System.Collections.Generic;
using SAPbouiCOM;
using Infocus.Framework.SapBone;

namespace Infocus.Edi.Bone.Forms
{
    public sealed class Process940FormCancelButton : BoneItem
    {
        public Process940FormCancelButton()
            : base()
        {
            FormType = SAPUIConstants.PROCESS_940_FORM_TYPE;
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
