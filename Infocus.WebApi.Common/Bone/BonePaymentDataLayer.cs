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
    public sealed class BonePaymentDataLayer
    {
        private Company _company = null;
        private static ILog _logger = LogManager.GetLogger(typeof(BonePaymentDataLayer));

        public BonePaymentDataLayer(Company company)
        {
            _company = company;
        }

        public Company getCompany()
        {
            return _company;
        }
      
        public DocumentKey Process820Record(Edi820HeaderRecord edi820HeaderRecord, bool autoImport)
        {
            if (autoImport)
            {
                Import_Log.LogEntry("Entering Process820Record");
            }
            else
            {
                _logger.Debug("Entering Process820Record");
            }
            Documents document = _company.GetBusinessObject(BoObjectTypes.oReturns) as Documents;
            try
            {
                bool bIsDuplicate = CheckExistingReturns(edi820HeaderRecord.PurchaseOrderReference, edi820HeaderRecord.SBOCardCode); //11-30/2020 added CardCode
                    if (bIsDuplicate == false)
                    {
                        if (edi820HeaderRecord.SBOCardCode == null || edi820HeaderRecord.SBOCardCode.Trim().Length == 0)
                        {  
                            document.CardCode = edi820HeaderRecord.CardCode;
                            
                        }
                        else
                        {
                            document.CardCode = edi820HeaderRecord.SBOCardCode;
                        } 
                      /*  document.NumAtCard = edi820HeaderRecord.PurchaseOrderReference;
                        if (edi820HeaderRecord.RequestDate.HasValue && edi820HeaderRecord.RequestDate.Value > DateTime.MinValue)
                        {
                            document.DocDueDate = edi820HeaderRecord.RequestDate.Value;
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
                        foreach (Edi820DetailRecord detailRecord in edi820HeaderRecord.Details)
                        {
                            if (lineCounter > 0)
                            {
                                document.Lines.Add();
                            }
                            lineCounter++;
                            if (!String.IsNullOrWhiteSpace(detailRecord.BuyerItemCode)
                            && (String.IsNullOrWhiteSpace(detailRecord.VendorItemCode) || edi820HeaderRecord.CardCode == "LowesNet"
                            || edi820HeaderRecord.CardCode == "TSCCL" 
                            ))
                            {
                                _logger.Debug("Looking up Item Code " + detailRecord.BuyerItemCode + " from Business Partner");
                                if (edi820HeaderRecord.SBOCardCode == null || edi820HeaderRecord.SBOCardCode.Trim().Length == 0)
                                {  
                                    document.Lines.ItemCode = LookupItemCode(edi820HeaderRecord.CardCode, detailRecord.BuyerItemCode);
                                    
                                }
                                else
                                {
                                    
                                    if (edi820HeaderRecord.CardCode == "LowesNet")
                                    {
                                        document.Lines.ItemCode = LookupItemCode(edi820HeaderRecord.CardCode, detailRecord.BuyerItemCode);
                                        if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode)))
                                        {
                                            document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                        }
                                    }
                                    else
                                    { 
                                        document.Lines.ItemCode = LookupItemCode(edi820HeaderRecord.SBOCardCode, detailRecord.BuyerItemCode);
                                    } 
                                } 

                                if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                {
                                    if (edi820HeaderRecord.SBOCardCode == null || edi820HeaderRecord.SBOCardCode.Trim().Length == 0)
                                    {  
                                        throw new WebApiException("Could not locate Vendor Item " + detailRecord.BuyerItemCode + " for CardCode " + edi820HeaderRecord.CardCode);
                                        
                                    }
                                    else
                                    {
                                        throw new WebApiException("Could not locate Vendor Item " + detailRecord.BuyerItemCode + " for CardCode " + edi820HeaderRecord.SBOCardCode);

                                    } 
                                }
                            }
                            else if (!String.IsNullOrWhiteSpace(detailRecord.VendorItemCode))
                            {
                                _logger.Debug("Assigning Item Code Directly");
                                
                                if (edi820HeaderRecord.CardCode == "LowesNet" ||
                                    edi820HeaderRecord.CardCode == "TSCCL" || 
                                    edi820HeaderRecord.CardCode == "HDCL")
                                {
                                    document.Lines.ItemCode = LookupItemCode(edi820HeaderRecord.CardCode, detailRecord.BuyerItemCode);
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
                            
                            document.Lines.Quantity = detailRecord.Quantity;
                            if (detailRecord.UnitPrice.HasValue && detailRecord.UnitPrice.Value > 0)
                            {
                                document.Lines.UnitPrice = detailRecord.UnitPrice.Value;
                            }
                            document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = detailRecord.LineNumber;
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
                       */
                        DocumentKey documentKey = new DocumentKey();
                        String strDocEntry = _company.GetNewObjectKey();
                        documentKey.DocEntry = Int32.Parse(strDocEntry);
                        documentKey.DocNum = GetDocNum(documentKey.DocEntry);
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Leaving Process820Record. Created Document " + documentKey.DocNum);
                        }
                        else
                        {
                            _logger.Debug("Leaving Process820Record.  Created Document " + documentKey.DocNum);
                        }
                        return documentKey;
                        
                    } 
                    else
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Leaving Process820Record. Skipped duplicate PO# " + edi820HeaderRecord.PurchaseOrderReference);
                        }
                        else
                        {
                            _logger.Debug("Leaving Process820Record.  Skipped duplicate PO# " + edi820HeaderRecord.PurchaseOrderReference);
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
                    Import_Log.LogEntry("Error in BonePaymentDataLayer: " + oErrMsg + "(" + soEx.InnerException.Message + ")");
                }
                else
                {
                    _logger.Debug("Error in BonePaymentDataLayer: " + oErrMsg + " (" + soEx.InnerException.Message + ")");
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
               // rs.DoQuery("select count(DocEntry) NoOrds from ORDN where Canceled = 'N' and NumAtCard  = '" + pPONo.Trim() + "'");
                rs.DoQuery("select count(DocEntry) NoOrds from ORDN With(NOLOCK) where Canceled = 'N' and NumAtCard  = '" + pPONo.Trim() + "' and CardCode = '" + pCardCode.Trim() + "'"); //11-30/2020
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
