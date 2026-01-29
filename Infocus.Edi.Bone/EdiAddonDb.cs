using Infocus.Framework.SapBone;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.Edi.Bone
{
    public class EdiAddonDb : BoneDatabase
    {
        public EdiAddonDb()
        {
            BoneDatabaseValidValuesList transportationMethodList = new BoneDatabaseValidValuesList();
            transportationMethodList.Add("M", "Motor");
            transportationMethodList.Add("R", "Rail");
            transportationMethodList.Add("U", "Private Parcel");
            transportationMethodList.Add("LT", "Less Than Trailer Load (LTL)"); // 07-12-2019 
            Tables = new BoneDatabaseTable[]
            {
                new BoneDatabaseTable(SAPUIConstants.SETTINGS_TABLE_NAME, "Infocus EDI Settings", SAPbobsCOM.BoUTBTableType.bott_NoObject)
            };
            Columns = new BoneDatabaseColumn[]
            {
                new BoneDatabaseColumn(SAPUIConstants.SETTINGS_TABLE_NAME,
                    SAPUIConstants.SETTINGS_DB_USERNAME,
                    "Database User",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                new BoneDatabaseColumn(SAPUIConstants.SETTINGS_TABLE_NAME,
                    SAPUIConstants.SETTINGS_DB_PASSWORD,
                    "Database Password",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "InfoW2ShNm",
                    "EDI Ship To Name",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "InfoW2ShAt",
                    "EDI Ship To Attention",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "InfoW2Mop",
                    "EDI Method Of Payment",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "InfoW2Cc",
                    "EDI Carrier Code",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "InfoW2Lc",
                    "EDI Location Code",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                // 05-31-2017 begin
                      new BoneDatabaseColumn("ORDR",
                    "Info_Pro",
                    "EDI Location Code",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1),
                // 05-31-2017 end
                new BoneDatabaseColumn("ORDR",
                    "InfoW2Cnt",
                    "EDI Contact Name",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    100,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "InfoW2Cnn",
                    "EDI Contact Number",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    100,
                    null,
                    -1),

                new BoneDatabaseColumn("RDR1",
                    "InfoW2LNo",
                    "EDI Line Number",
                    BoFieldTypes.db_Numeric,
                    BoFldSubTypes.st_None,
                    11,
                    null,
                    -1),
                new BoneDatabaseColumn("OSHP",
                    "InfoW2Tm",
                    "EDI Transporation Method",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    1,
                    transportationMethodList.ToArray(),
                    -1),
                    // 02-27-2022 begin
                new BoneDatabaseColumn("OSHP",
                    "COR_Carrier",
                    "Carrier",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    10,
                   null,
                    -1),
                    // 02-27-2022 end
               new BoneDatabaseColumn("ORDR",
                    "Info_BOL",
                    "EDI Bill Of Lading",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    50,
                    null,
                    -1),
                new BoneDatabaseColumn("ORDR",
                    "Info_PRO",
                    "PRO #",
                    BoFieldTypes.db_Alpha,
                    BoFldSubTypes.st_None,
                    75,
                    null,
                    -1)
            };
        }
    }
}


