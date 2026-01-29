using System;
using System.Collections.Generic;
using SAPbouiCOM;
using Infocus.Framework.SapBone;

namespace Infocus.Edi.Bone.Forms
{
    public sealed class Import940StatusFormCancelButton : BoneItem
    {
        public Import940StatusFormCancelButton()
            : base()
        {
            FormType = SAPUIConstants.IMPORT940_FORM_TYPE;
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
