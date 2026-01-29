using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SAPbouiCOM;
using SAPbobsCOM;
using System.Runtime.InteropServices;

namespace Infocus.Framework.SapBone.Utilities
{
    public static class DocumentUtility
    {
        private static Int32[] DocumentFormTypes = { 139, 140, 133, 179, 180, 112 };
        public static DocumentInfo GetDocumentInfo(Form form)
        {
            if(form.Mode == BoFormMode.fm_ADD_MODE)
            {
                throw new BoneFrameworkException("Cannot retrieve Document Information on form that has not been added");
            }
            DocumentInfo di = new DocumentInfo();
            String tableName = GetPrimaryTableNameFromForm(form);
            String docNum = ((EditText)form.Items.Item("8").Specific).Value;
            di.DocNum = Int32.Parse(docNum);

            Recordset rs = BoneConnectionContext.Company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery(String.Format("SELECT DocEntry, DocStatus, Canceled, CardCode, ObjType, GroupNum FROM {0} WHERE DocNum = {1}", tableName, docNum));
                rs.MoveFirst();
                if(!rs.EoF)
                {
                    di.Canceled = ((String)rs.Fields.Item("Canceled").Value == "Y");
                    di.CardCode = (String)rs.Fields.Item("CardCode").Value;
                    di.DocEntry = (Int32)rs.Fields.Item("DocEntry").Value;
                    di.DocStatus = (String)rs.Fields.Item("DocStatus").Value;
                    di.PaymentGroupCode = (Int32)rs.Fields.Item("GroupNum").Value;
                    di.ObjectType = Int32.Parse((String)rs.Fields.Item("ObjType").Value);
                }
                else
                {
                    throw new BoneFrameworkException("Could not find document " + docNum);
                }
            }
            finally
            {
                Marshal.ReleaseComObject(rs);
            }

            return di;
        }

        public static String GetPrimaryTableNameFromForm(Form form)
        {
            if(form.TypeEx == "139")
            {
                return "ORDR";
            }
            else if(form.TypeEx == "140")
            {
                return "ODLN";
            }
            else if(form.TypeEx == "133")
            {
                return "OINV";
            }
            else if(form.TypeEx == "179")
            {
                return "ORIN";
            }
            else if(form.TypeEx == "180")
            {
                return "ORDN";
            }
            return null;
        }

        public static DraftDocumentFormDetails GetDraftDocumentFormDetails(Form form, String formTitleSearch = null)
        {
            DraftDocumentFormDetails details = new DraftDocumentFormDetails();
            if(!DocumentFormTypes.Contains(form.Type))
            {
                throw new BoneFrameworkException("Form is not a valid Document Type Form.");
            }

            Boolean hasDraftTitle = form.Title.IndexOf("Draft", StringComparison.InvariantCultureIgnoreCase) > -1;
            if(!hasDraftTitle && !String.IsNullOrWhiteSpace(formTitleSearch))
            {
                hasDraftTitle = form.Title.Equals(formTitleSearch, StringComparison.InvariantCulture);
            }
            String status = ((ComboBox)form.Items.Item("81").Specific).Value;
            Boolean hasDraftStatus = status == "6";

            if(hasDraftTitle || hasDraftStatus)
            {
                details.IsDraftDocumentForm = true;
            }

            if(hasDraftStatus)
            {
                details.IsSaved = true;
            }

            return details;
        }
    }
    public sealed class DraftDocumentFormDetails
    {
        public Boolean IsDraftDocumentForm
        {
            get;
            set;
        }

        public Boolean IsSaved
        {
            get;
            internal set;
        }
    }
}
