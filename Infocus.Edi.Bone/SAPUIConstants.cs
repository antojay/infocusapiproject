using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.Edi.Bone
{
    public static class SAPUIConstants
    {
        public const String MENU_EDI = "InfoW2M1";
        public const String MENU_EDI_PROCESS_850 = "InfoW2M2";
        public const String MENU_SETUP = "InfoW2M3";
        public const String MENU_ITEM_SETUP = "InfoW2M4";
        public const String MENU_850_IMPORT_STATUS = "InfoW2MSt"; // 07-14-2021

        // 08-07-2023 begin
        public const String PROCESS_940_FORM_TYPE = "InfoW2940";
        public const String MENU_EDI_PROCESS_940= "InfoW2M10"; 
        public const String MENU_940_IMPORT_STATUS = "InfoW2940";
        // 08-07-2023 end
     
        public const String PROCESS_EDI_FORM_TYPE = "InfoW2Pro";
        // 04-26-2019 begin
        public const String MENU_EDI_PROCESS_820 = "InfoW2M7";
        public const String PROCESS_820_FORM_TYPE = "InfoW2820";
        public const String MENU_EDI_PROCESS_180 = "InfoW2M8";
        public const String PROCESS_180_FORM_TYPE = "InfoW2180";
        // 04-26-2019 end
        public const String MENU_EDI_RESEND_856 = "InfoW2M5"; // 02-26-2019
        public const String RESEND_856_FORM_TYPE = "InfoW2R856"; // 02-26-2019
        public const String MENU_EDI_RESEND_810 = "InfoW2M6"; // 02-26-2019
        public const String RESEND_810_FORM_TYPE = "InfoW2R810"; // 02-26-2019
        public const String MENU_EDI_REJECT_850 = "InfoW2M9"; // 08-20-2019
        public const String REJECT_850_FORM_TYPE = "InfoW2R850"; // 08-20-2019
        public const String REJECT_SEL_FORM_TYPE = "InfoW2Sel"; // 08-20-2019
     
     
        public const String SETTINGS_TABLE_NAME = "INFO_W2_SETTINGS";
        public const String SETTINGS_DB_USERNAME = "DbUser";
        public const String SETTINGS_DB_PASSWORD = "DbPass";

        public const String SETTINGS_FORM_TYPE = "InfoW2Set";

        public const String IMPORT850_FORM_TYPE = "InfoW2IStatus"; // 07-14-2021

        public const String IMPORT940_FORM_TYPE = "InfoW2940Status"; // 08-07-2023
    }
}
