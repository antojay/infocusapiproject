using System;
using SAPbouiCOM;
using Infocus.Framework.SapBone;
namespace Infocus.Framework.SapBone.Forms
{
    public class ExceptionFormOk : BoneItem
    {
        public ExceptionFormOk()
        {
            FormType = BoneFormConstants.ExceptionFormType;
            ItemUID = BoneFormConstants.ExceptionFormOkButton;
        }

        [BoneListener(BoEventTypes.et_ITEM_PRESSED, true)]
        public virtual Boolean OnBeforeItemPressed(ItemEvent itemEvent)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(itemEvent.FormUID);
            form.Close();
            return true;
        }
    }
}
