using Infocus.WebApi.Data.Models;
using SAPbobsCOM;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infocus.WebApi.Common.Bone
{
    public sealed class BoneReturnDataLayer
    {
        private Company _company = null;
        private static ILog _logger = LogManager.GetLogger(typeof(BoneReturnDataLayer));

        public BoneReturnDataLayer(Company company)
        {
            _company = company;
        }

        public Company getCompany()
        {
            return _company;
        }

        public DocumentKey Process180Record(Edi180HeaderRecord edi180HeaderRecord, bool autoImport)
        {
            if (autoImport)
            {
                Import_Log.LogEntry("Entering Process180Record");
            }
            else
            {
                _logger.Debug("Entering Process180Record");
            }
            Documents document = _company.GetBusinessObject(BoObjectTypes.oReturns) as Documents;
            try
            {
                bool bIsDuplicate = CheckExistingReturns(edi180HeaderRecord.PurchaseOrderReference, edi180HeaderRecord.SBOCardCode); //11-30/2020 added CardCode
                if (bIsDuplicate == false)
                {
                    if (edi180HeaderRecord.SBOCardCode == null || edi180HeaderRecord.SBOCardCode.Trim().Length == 0)
                    {
                        document.CardCode = edi180HeaderRecord.CardCode;

                    }
                    else
                    {
                        document.CardCode = edi180HeaderRecord.SBOCardCode;
                    }
                    document.NumAtCard = edi180HeaderRecord.PurchaseOrderReference;
                    if (edi180HeaderRecord.RequestDate.HasValue && edi180HeaderRecord.RequestDate.Value > DateTime.MinValue)
                    {
                        document.DocDueDate = edi180HeaderRecord.RequestDate.Value;
                    }
                    else
                    {
                        document.DocDueDate = DateTime.Today;
                    }

                    document.TaxDate = DateTime.Today;


                    if (autoImport)
                    {
                        Import_Log.LogEntry("Processing Detail Lines");
                    }
                    else
                    {
                        _logger.Debug("Processing Detail Lines");
                    }
                    int lineCounter = 0;
                    foreach (Edi180DetailRecord detailRecord in edi180HeaderRecord.Details)
                    {
                        if (lineCounter > 0)
                        {
                            document.Lines.Add();
                        }
                        lineCounter++;
                        int DelEntry = 0;
                        int DelLine = -1;
                        string[] delDetail = getDelivery(edi180HeaderRecord.PurchaseOrderReference, detailRecord.LineNumber, edi180HeaderRecord.SBOCardCode);
                        if (String.IsNullOrWhiteSpace(delDetail[0]) || String.IsNullOrWhiteSpace(delDetail[1]))
                        {
                            String oMesg = "Need to add check for Invoice";
                            _logger.Debug(oMesg);
                        }
                        else
                        {
                            try
                            {
                                DelEntry = Convert.ToInt32(delDetail[0]);
                            }
                            catch
                            {
                                DelEntry = 0;
                                                            }
                            try
                            {
                                DelLine = Convert.ToInt32(delDetail[1]);
                            }
                            catch
                            {
                                DelLine = -1;
                            }
                        }
                        if (DelLine > -1 && DelEntry > 0)
                        {
                            document.Lines.BaseType = 15;
                            document.Lines.BaseEntry = DelEntry;
                            document.Lines.BaseLine = DelLine;
                        }
                        else
                        {
                            if (!String.IsNullOrWhiteSpace(detailRecord.BuyerItemCode)
                            && (String.IsNullOrWhiteSpace(detailRecord.VendorItemCode) || edi180HeaderRecord.CardCode == "LowesNet"
                            || edi180HeaderRecord.CardCode == "TSCCL"
                            ))
                            {
                                _logger.Debug("Looking up Item Code " + detailRecord.BuyerItemCode + " from Business Partner");
                                if (edi180HeaderRecord.SBOCardCode == null || edi180HeaderRecord.SBOCardCode.Trim().Length == 0)
                                {
                                    document.Lines.ItemCode = LookupItemCode(edi180HeaderRecord.CardCode, detailRecord.BuyerItemCode);

                                }
                                else
                                {

                                    if (edi180HeaderRecord.CardCode == "LowesNet")
                                    {
                                        document.Lines.ItemCode = LookupItemCode(edi180HeaderRecord.CardCode, detailRecord.BuyerItemCode);
                                        if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode)))
                                        {
                                            document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                        }
                                    }
                                    else
                                    {
                                        document.Lines.ItemCode = LookupItemCode(edi180HeaderRecord.SBOCardCode, detailRecord.BuyerItemCode);
                                    }
                                }

                                if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                {
                                    if (edi180HeaderRecord.SBOCardCode == null || edi180HeaderRecord.SBOCardCode.Trim().Length == 0)
                                    {
                                        throw new WebApiException("Could not locate Vendor Item " + detailRecord.BuyerItemCode + " for CardCode " + edi180HeaderRecord.CardCode);

                                    }
                                    else
                                    {
                                        throw new WebApiException("Could not locate Vendor Item " + detailRecord.BuyerItemCode + " for CardCode " + edi180HeaderRecord.SBOCardCode);

                                    }
                                }
                            }
                            else if (!String.IsNullOrWhiteSpace(detailRecord.VendorItemCode))
                            {
                                _logger.Debug("Assigning Item Code Directly");

                                if (edi180HeaderRecord.CardCode == "LowesNet" ||
                                    edi180HeaderRecord.CardCode == "TSCCL" ||
                                    edi180HeaderRecord.CardCode == "HDCL")
                                {
                                    document.Lines.ItemCode = LookupItemCode(edi180HeaderRecord.CardCode, detailRecord.BuyerItemCode);
                                    if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode)))
                                    {
                                        document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                    }
                                }
                                else
                                {
                                    document.Lines.ItemCode = detailRecord.VendorItemCode;
                                }

                                if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode)))
                                {
                                    document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                }

                            }
                            else
                            {
                                document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                {
                                    throw new WebApiException("ItemCode (VendorItemCode) is required");
                                }
                            }

                            if (!String.IsNullOrWhiteSpace(detailRecord.VendorItemCode))
                            {
                                document.Lines.FreeText = detailRecord.VendorItemCode;
                            }
                            if (detailRecord.UnitPrice.HasValue && detailRecord.UnitPrice.Value > 0)
                            {
                                document.Lines.UnitPrice = detailRecord.UnitPrice.Value;
                            }
                            document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = detailRecord.LineNumber;
                        }
                        document.Lines.Quantity = detailRecord.Quantity;
                       document.Lines.UserFields.Fields.Item("U_InfoItmStatus").Value = detailRecord.ReturnCode;
                        document.Lines.UserFields.Fields.Item("U_InfoItmRsn").Value = detailRecord.ReturnReasonCode;
                        document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = detailRecord.LineNumber;

                    }
                    string temp = (string)document.UserFields.Fields.Item("U_InfoW2ShNm").Value;
                    int oRet = document.Add();
                    if (oRet != 0)
                    {
                        String msg = _company.GetLastErrorDescription();
                        int oErrCode = _company.GetLastErrorCode();
                        if (oRet == 5009 || oRet == -5009)
                        {
                            msg = "Missing Item Code => " + msg;
                        }
                        if (autoImport)
                        {
                            Import_Log.LogEntry(msg);
                        }
                        else
                        {
                            _logger.Error(msg);
                        }
                        throw new WebApiException(msg);
                    }
                    DocumentKey documentKey = new DocumentKey();
                    String strDocEntry = _company.GetNewObjectKey();
                    documentKey.DocEntry = Int32.Parse(strDocEntry);
                    documentKey.DocNum = GetDocNum(documentKey.DocEntry);
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Leaving Process180Record. Created Document " + documentKey.DocNum);
                    }
                    else
                    {
                        _logger.Debug("Leaving Process180Record.  Created Document " + documentKey.DocNum);
                    }
                    return documentKey;
                }
                else
                {
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Leaving Process180Record. Skipped duplicate PO# " + edi180HeaderRecord.PurchaseOrderReference);
                    }
                    else
                    {
                        _logger.Debug("Leaving Process180Record.  Skipped duplicate PO# " + edi180HeaderRecord.PurchaseOrderReference);
                    }
                    DocumentKey documentKey = new DocumentKey();
                    return documentKey;
                }

            }
            catch (Exception soEx)
            {
                String oErrMsg = soEx.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error in BoneReturnDataLayer: " + oErrMsg + "(" + soEx.InnerException.Message + ")");
                }
                else
                {
                    _logger.Debug("Error in BoneReturnDataLayer: " + oErrMsg + " (" + soEx.InnerException.Message + ")");
                }
                return null;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(document);
            }
        }
        private String LookupItemCode(String cardCode, String vendorItemCode)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select ItemCode from OSCN With(NOLOCK) where Substitute = '" + vendorItemCode + "'");
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    return (String)rs.Fields.Item("ItemCode").Value;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return String.Empty;
        }

        private String ValidateItemCode(String pItemCode)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select ItemCode from OITM With(NOLOCK) where ItemCode  = '" + pItemCode.Trim() + "'");
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    return (String)rs.Fields.Item("ItemCode").Value;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return String.Empty;
        }
        private bool CheckExistingReturns(String pPONo, String pCardCode) //11-30/2020 added CardCode
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bDuplicate = false;
            try
            {
                rs.DoQuery("select count(DocEntry) NoOrds from ORDN With(NOLOCK) where Canceled = 'N' and NumAtCard  = '" + pPONo.Trim() + "' and CardCode = '" + pCardCode.Trim() + "'"); //11-30/2020 added CardCode
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    string oValue = rs.Fields.Item("NoOrds").Value.ToString();
                    int oCount = 0;
                    try
                    {
                        oCount = Convert.ToInt32(oValue);
                        if (oCount > 0)
                        {
                            bDuplicate = true;
                        }
                    }
                    catch
                    {

                    }
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return bDuplicate;
        }

        public string[] getDelivery(String pPoNo, Int32 pLineNo, String pCardCode)
        {
            string[] deliveryDetail = new string[2];
            try
            {
                Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                try
                {
                    string oQuery = "select t0.DocEntry, LineNum from DLN1 t0 With(NOLOCK) left join ODLN t1 With(NOLOCK) on t0.DocEntry = t1.DocEntry " +
                               "where t1.Canceled = 'N' and t1.NumAtCard = '" + pPoNo.Trim() + "' and t1.CardCode = '" + pCardCode.Trim() +
                               "' and t0.U_InfoW2LNo = " + pLineNo;
                    rs.DoQuery(oQuery);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        deliveryDetail[0] = (String)rs.Fields.Item("DocEntry").Value.ToString();
                        deliveryDetail[1] = (String)rs.Fields.Item("LineNum").Value.ToString();
                    }
                }
                catch (Exception del)
                {
                    _logger.Error("Could not find Delivery => " + del.Message);
                    return null;
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                }
               
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                _logger.Error("Could not find Delivery", e);
                _logger.Debug("Could not find Delivery =>" + oErrMesg);
                return null;
            }
            return deliveryDetail;
        }

        private Int32 GetDocNum(Int32 docEntry)
        {
            Int32 docNum = 0;
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select top 1 DocNum from ORDN With(NOLOCK) where DocEntry = " + docEntry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    docNum = (Int32)rs.Fields.Item(0).Value;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return docNum;
        }
    }
}
