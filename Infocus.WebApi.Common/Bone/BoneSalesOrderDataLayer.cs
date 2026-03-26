using Infocus.WebApi.Data.Models;
using SAPbobsCOM;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/* 08-26-2019 lar changed CardCode == to CardCode.StartsWith */
namespace Infocus.WebApi.Common.Bone
{
    public sealed class BoneSalesOrderDataLayer
    {
        private Company _company = null;
        private static ILog _logger = LogManager.GetLogger(typeof(BoneSalesOrderDataLayer));
        // 10-08-2019 begin
        private static readonly String TransportQuery =
@"select TrnspCode, TrnspName, WebSite as SCAC, U_InfoW2Tm as TransportMethod from OSHP WITH(NOLOCK) where Website = '{0}'";
        // 10=08-2019 end

        // 02-21-2018 begin
        /*   public bool setAltVendorItem (int pDocEntry)
           {
               Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
               try
               {
                   string oQuery = "UPDATE RDR1 set U_InfoVendorItem = left(ltrim(rtrim(cast(FreeTxt as nvarchar))),50) where DocEntry = " + 
                                    pDocEntry + " and len(ltrim(rtrim(coalesce(cast(FreeTxt as nvarchar),'')))) > 0";             
                   rs.DoQuery(oQuery);
               } catch (Exception ft)
               {

               }
               return true;
               }
               */
        // 02-21-2018 end
        public BoneSalesOrderDataLayer(Company company)
        {
            _company = company;
        }

        // 03-18-2019 begin
        public Company getCompany()
        {
            return _company;
        }
        // 03-18-2019 end


        // 07-31-2023 begin

        public DocumentKey Process940Record(Edi940HeaderRecord edi940HeaderRecord, bool autoImport, string ordSource)
        {
            if (autoImport)
            {
                Import_Log.LogEntry("Entering Process940Record");
            }
            else
            {
                _logger.Debug("Entering Process940Record");
            }
            Documents document = _company.GetBusinessObject(BoObjectTypes.oOrders) as Documents;
            try
            {
                Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                try
                {
                    String oQry1 = "update InfocusEdi940HeaderRecord set HasOpen856=1 where coalesce(HasOpen856,-1) < 0 and HeaderId =  " + edi940HeaderRecord.HeaderId + "; " +
                                   "update InfocusEdi940HeaderRecord set Processed=1 where len(ltrim(rtrim(coalesce(CardCode,'')))) = 0  and HeaderId =  " + edi940HeaderRecord.HeaderId + "; " +
                                   "update InfocusEdi940HeaderRecord set TrxPurpose='00' where len(ltrim(rtrim(coalesce(TrxPurpose,'')))) = 0 and HeaderId = " + edi940HeaderRecord.HeaderId + "; ";
                    rs.DoQuery(oQry1);
                }
                catch (Exception l)
                {
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error updating HasOpen856 and/or Processed =>" + l.Message);
                    }
                    else
                    {
                        _logger.Debug("Error updating HasOpen856 and/or Processed =>" + l.Message);
                    }
                }
                System.GC.Collect();
                try
                {
                    String oQry1 = "execute [Infocus_EDI940_Duplicates] " + edi940HeaderRecord.HeaderId + ";";
                    rs.DoQuery(oQry1);
                }
                catch (Exception l)
                {
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error updating Processed =>" + l.Message);
                    }
                    else
                    {
                        _logger.Debug("Error updating Processed =>" + l.Message);
                    }
                }
                string oIs3PL = "N";
                if (autoImport)
                {
                    Import_Log.LogEntry("Check to see if 3PL");
                }
                else
                {
                    _logger.Debug("Check to see if 3PL");
                }
                try
                {
                    string oQry1 = "select coalesce([3PL],'N') Is3PL from InfocusEdi.dbo.WebApiDbContext  WITH(NOLOCK) where CardCode = '" +
                                    edi940HeaderRecord.CardCode.Trim() + "' and SBOCardCode = '" + edi940HeaderRecord.SBOCardCode.Trim() + "'";
                    rs.DoQuery(oQry1);
                    if (!rs.EoF)
                    {
                        oIs3PL = rs.Fields.Item("Is3PL").Value.ToString();
                    }
                }
                catch (Exception pl)
                {
                    oIs3PL = "N";
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error setting Is3PL =>" + pl.Message);
                    }
                    else
                    {
                        _logger.Error("Error setting Is3PL =>" + pl.Message);
                    }
                }
                _logger.Debug("Is 3PL? " + oIs3PL);

                string oSTC = "";
                if (autoImport)
                {
                    Import_Log.LogEntry("Is 3PL? " + oIs3PL);
                }
                else
                {
                    _logger.Debug("Is 3PL? " + oIs3PL);
                }

                if (oIs3PL == "Y")
                {
                    try
                    {
                        string oQry1 = "select top 1 IsNull(T0.U_C3_CustAcct,'') as CustAcct FROM [dbo].[@C3_CASC] T0 With(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                        edi940HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = '" + edi940HeaderRecord.CustCode3PL.Trim() + "'";
                        if (edi940HeaderRecord.CardCode.StartsWith("INDOCOUNT") && edi940HeaderRecord.CustCode3PL == "KHDO" && edi940HeaderRecord.VendorNumber.Trim() == "078699963")
                        {
                            oQry1 = "select top 1 IsNull(T0.U_C3_CustAcct,'') as CustAcct FROM [dbo].[@C3_CASC] T0 With(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                       edi940HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = 'KHDO2' ";
                        }

                        try
                        {
                            rs.DoQuery(oQry1);
                            if (!rs.EoF)
                            {
                                if (edi940HeaderRecord.CardCode.StartsWith("INDOC") && edi940HeaderRecord.CustCode3PL == "KHDO" && edi940HeaderRecord.VendorNumber.Trim() == "078699963")
                                {
                                    oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC] T0 WITH(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                 edi940HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = 'KHDO2' ";
                                }
                                else
                                {
                                    oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC] T0 WITH(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                               edi940HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = '" + edi940HeaderRecord.CustCode3PL.Trim() + "'";
                                }
                            }
                            else
                            {
                                oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC] T0 WITH(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                        edi940HeaderRecord.SBOCardCode.Trim() + "' and  UPPER(LTRIM(RTRIM(ISNULL(T0.[U_C3_CustAcct],'')))) = 'DEFAULT'";
                            }
                        }
                        catch (Exception cacct)
                        {
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Error reading [@CASC] to validate account =>" + cacct.Message);
                            }
                            else
                            {
                                _logger.Error("Error reading [@CASC] to validate account =>" + cacct.Message);
                            }
                        }
                        rs.DoQuery(oQry1);
                        if (!rs.EoF)
                        {
                            oSTC = rs.Fields.Item("STC").Value.ToString().Trim();
                        }
                    }
                    catch (Exception stc)
                    {
                        oSTC = "";
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Error reading [@CASC] =>" + stc.Message);
                        }
                        else
                        {
                            _logger.Error("Error reading [@C3_CASC] => " + stc.Message);
                        }
                        set940Error("Invalic STC =>" + stc.Message, edi940HeaderRecord.HeaderId, autoImport, false);
                    }
                }
                else
                {
                    oSTC = "";
                }
                if (autoImport)
                {
                    Import_Log.LogEntry("STC => " + oSTC);
                }
                else
                {
                    _logger.Debug("STC => " + oSTC);
                }
                if (oIs3PL == "Y")
                {
                    if (String.IsNullOrWhiteSpace(oSTC) || oSTC.Trim().Length == 0)
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("STC is blank");
                        }
                        else
                        {
                            _logger.Error("STC is blank");
                        }
                        throw new Exception("STC is invalid");
                    }
                }
                string oVBUWhs = "";
                if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.VendorNumber))
                {
                    try
                    {
                        string oQry1 = "select U_WhsCode from [@INFO_VBU_WHS]  WITH(NOLOCK) where Code = '" + edi940HeaderRecord.VendorNumber + "' and U_CardCode = '" + edi940HeaderRecord.SBOCardCode + "'";
                        rs.DoQuery(oQry1);
                        if (!rs.EoF)
                        {
                            oVBUWhs = rs.Fields.Item("U_WhsCode").Value.ToString();
                        }
                    }
                    catch (Exception vbu)
                    {
                        oVBUWhs = "";
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Error reading [@INFO_VBU_WHS =>" + vbu.Message);
                        }
                        else
                        {
                            _logger.Error("Error reading [@INFO_VBU_WHS] => " + vbu.Message);
                        }
                    }
                }
                if (autoImport)
                {
                    Import_Log.LogEntry("Clearing 940 ErrorMessage");
                }
                else
                {
                    _logger.Debug("Clearing 940 ErrorMessage");
                }
                set940Error(String.Empty, edi940HeaderRecord.HeaderId, autoImport, false);

                if (String.IsNullOrWhiteSpace(edi940HeaderRecord.TrxPurpose))
                {
                    edi940HeaderRecord.TrxPurpose = "00";
                }
                if (!(edi940HeaderRecord.TrxPurpose.Trim() == "01"))
                {
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Check for duplicate orders");
                    }
                    else
                    {
                        _logger.Debug("Check for duplicate orders");
                    }
                    DateTime poDate = DateTime.Now;
                    DateTime recDate = DateTime.Now;
                    DateTime reqShipDate = DateTime.Now; // 02-29-2024
                    try
                    {
                        poDate = Convert.ToDateTime(edi940HeaderRecord.PurchaseOrderDate);
                    }
                    catch
                    {
                        poDate = DateTime.Now;
                    }
                    try
                    {
                        recDate = Convert.ToDateTime(edi940HeaderRecord.ReceivedDateTime);
                    }
                    catch
                    {
                        recDate = DateTime.Now;
                    }
                    // 02-29-2024 begin
                    try
                    {
                        reqShipDate = Convert.ToDateTime(edi940HeaderRecord.RequestedShipDate);
                    }
                    catch (Exception rsd)
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Error getting requested ship date: " + rsd.Message);
                        }
                        else
                        {
                            _logger.Debug("Error getting requested ship date: " + rsd.Message);
                        }
                    }
                    // 02-29-2024 end

                    //bool bIsDuplicate = checkExistingOrders(edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.CardCode, poDate, recDate, edi940HeaderRecord.PurchaseOrderReference, autoImport);
                    //bool bIsDuplicate = checkExistingOrders(edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.CardCode, poDate, recDate, edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.ShipToLocationCode, edi940HeaderRecord.StoreNumber, autoImport); // 09-01-2023
                    bool bIsDuplicate = checkExistingOrders(edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.CardCode, poDate, recDate, reqShipDate, edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.ShipToLocationCode, edi940HeaderRecord.StoreNumber, autoImport); // 02-29-2024
                    if (bIsDuplicate == false)
                    {
                        int oHeaderId = edi940HeaderRecord.HeaderId;
                        edi940HeaderRecord.ErrorMessage = String.Empty;
                        bool bValidPayment = true;
                        if ((!edi940HeaderRecord.CardCode.ToUpper().StartsWith("TeeZed") && oIs3PL == "Y")
                            || oIs3PL == "N")
                        {
                            // 03-24-2026 lrussell  begin
                            if (String.IsNullOrWhiteSpace(edi940HeaderRecord.UserDefined03) || String.IsNullOrEmpty(edi940HeaderRecord.UserDefined03))
                            {
                                edi940HeaderRecord.UserDefined03 = string.Empty;
                            }
                            // 03-24-2026 lrussell end
                            // 07-07-2023 bValidPayment was not being set with result from checkPaymentType
                            bValidPayment = checkPaymentType(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.UserDefined03, autoImport);
                        }
                        if (edi940HeaderRecord.CardCode.Trim().ToUpper().StartsWith("TEEZED"))
                        {
                            bValidPayment = true;
                            if (autoImport)
                            {
                                Import_Log.LogEntry("TeeZed set valid PaymentMethod = true");
                            }
                            else
                            {
                                _logger.Debug("TeeZed set valid PaymentMethod = true");
                            }
                        }
                        else
                            if (oIs3PL == "Y" && String.IsNullOrWhiteSpace(edi940HeaderRecord.PaymentMethod))
                            {
                                bValidPayment = false;
                            }

                        if (bValidPayment == true)
                        {
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Check for invalid items");
                            }
                            else
                            {
                                _logger.Debug("Check for invalid items");
                            }
                            bool bInvalidItems = checkItems(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, autoImport); ;
                            if (bInvalidItems == false)
                            {
                                bool bPriceVariance = false;

                                if (oIs3PL == "N")
                                {
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry("Check for price variance");
                                    }
                                    else
                                    {
                                        _logger.Debug("Check for price variance");
                                    }
                                    bPriceVariance = checkPrice(edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.HeaderId, autoImport); ;
                                }
                                if (bPriceVariance == false) // 05-19-2021 added check for 3PL
                                {
                                    edi940HeaderRecord.ErrorMessage = " ";
                                    string carrierCode = "";
                                    string serviceLevel = "";
                                    if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.CarrierCode))
                                    {
                                        carrierCode = edi940HeaderRecord.CarrierCode;
                                    }

                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry("Check SCAC => Carrier Code");
                                    }
                                    else
                                    {
                                        _logger.Debug("Check SCAC => Carrier Code");
                                    }
                                    bool bValidSCAC = checkSCAC(edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.SBOCardCode, carrierCode, serviceLevel, oIs3PL, autoImport);
                                    if (bValidSCAC == true)
                                    {
                                        bool bValidShipper = true;
                                        if (oIs3PL == "Y")
                                        {
                                            bValidShipper = checkC3Shipper(edi940HeaderRecord.SBOCardCode, carrierCode, oSTC, autoImport, oIs3PL);
                                        }
                                        if (bValidShipper == true)
                                        {
                                            string shipCode = carrierCode;
                                            if (oIs3PL == "Y")
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Get ShipCode");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Get ShipCode");
                                                }
                                                shipCode = get3PLSCAC(edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.CarrierCode, "", autoImport);

                                            }
                                            else
                                            {
                                                bool bUseServLev = checkServLev(edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.CardCode, autoImport);

                                                if (bUseServLev == true)
                                                {
                                                    shipCode = shipCode.Trim() + "_" + serviceLevel.Trim();
                                                }
                                            }
                                            if (autoImport)
                                            {
                                                Import_Log.LogEntry("Ship Code => " + shipCode);
                                            }
                                            else
                                            {
                                                _logger.Debug("Ship Code => " + shipCode);
                                            }
                                            document.UserFields.Fields.Item("U_InfoW2Cc").Value = shipCode;
                                            if (autoImport)
                                            {
                                                Import_Log.LogEntry("Set Carrier UDF");
                                            }
                                            else
                                            {
                                                _logger.Debug("Set Carrier UDF");
                                            }
                                            // 09-06-2023 begin
                                            document.UserFields.Fields.Item("U_C3_STC").Value = oSTC;
                                            if (edi940HeaderRecord.CardCode.StartsWith("IndoCnt"))
                                            {
                                                document.UserFields.Fields.Item("U_C3_Shipper").Value = "INDOCOUNT";
                                                document.UserFields.Fields.Item("U_InfoFrtBillType").Value = "COLLECT";
                                            }
                                            // 09-06-2023 end
                                            // 09-08-2023 begin
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.CustCode3PL))
                                            {
                                                document.UserFields.Fields.Item("U_Info3PLCustCode").Value = edi940HeaderRecord.CustCode3PL;
                                            }
                                            // 09-08-2023 end
                                            if (edi940HeaderRecord.SBOCardCode == null || edi940HeaderRecord.SBOCardCode.Trim().Length == 0)
                                            {
                                                document.CardCode = edi940HeaderRecord.CardCode;
                                            }
                                            else
                                            {
                                                document.CardCode = edi940HeaderRecord.SBOCardCode;
                                            }
                                            document.UserFields.Fields.Item("U_InfoOrdStatus").Value = "CC";
                                            document.UserFields.Fields.Item("U_Info850HdrId").Value = edi940HeaderRecord.HeaderId;
                                            // 09-01-2023 begin
                                            //document.NumAtCard = edi940HeaderRecord.PurchaseOrderReference;
                                            string oPOKey = edi940HeaderRecord.PurchaseOrderReference.Trim();
                                            try
                                            {
                                                if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToLocationCode))
                                                {
                                                    oPOKey = oPOKey + "_" + edi940HeaderRecord.ShipToLocationCode.Trim();
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.StoreNumber))
                                                {
                                                    oPOKey = oPOKey + "_" + edi940HeaderRecord.StoreNumber.Trim();
                                                }
                                                // 02-29-2024 begin
                                                //switch ToString requested ship date
                                                /*DateTime oDate = (DateTime)edi940HeaderRecord.ReceivedDateTime;
                                                string oRecDate = oDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                                if (!String.IsNullOrWhiteSpace(oRecDate))
                                                {
                                                    oPOKey = oPOKey + "_" + oRecDate.Trim();
                                                }
                                                 */
                                                DateTime oDate = (DateTime)edi940HeaderRecord.RequestedShipDate;
                                                string oReqShipDate = oDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                                if (!String.IsNullOrWhiteSpace(oReqShipDate))
                                                {
                                                    oPOKey = oPOKey + "_" + oReqShipDate.Trim();
                                                }
                                                // 02-29-2024 end
                                            }
                                            catch (Exception p)
                                            {
                                                String oErrMsg = "Error building po#: " + p.Message;
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry(oErrMsg);
                                                }
                                                else
                                                {
                                                    _logger.Debug(oErrMsg);
                                                }
                                            }
                                            document.NumAtCard = oPOKey;
                                            if (autoImport)
                                            {
                                                Import_Log.LogEntry("Cust Ref: " + oPOKey);
                                            }
                                            else
                                            {
                                                _logger.Debug("Cust Ref: " + oPOKey);
                                            }
                                            // 09-01-2023 end
                                            // 02-29-2024 begin
                                            if (edi940HeaderRecord.PurchaseOrderDate != null && edi940HeaderRecord.PurchaseOrderDate.ToString().Trim().Length > 0)
                                            {
                                                try
                                                {
                                                    document.UserFields.Fields.Item("U_C3_PoDate").Value = edi940HeaderRecord.PurchaseOrderDate;
                                                }
                                                catch (Exception pdt)
                                                {
                                                    string oErrMsg = pdt.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error setting PO Date: " + oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Error setting PO Date: " + oErrMsg);
                                                    }
                                                }
                                            }
                                            if (edi940HeaderRecord.OrderNumber != null && edi940HeaderRecord.OrderNumber.Trim().Length > 0)
                                            {
                                                try
                                                {
                                                    document.UserFields.Fields.Item("U_COR_ImprtDoc").Value = edi940HeaderRecord.OrderNumber.Trim();
                                                }
                                                catch (Exception ord)
                                                {
                                                    string oErrMsg = ord.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error setting U_COR_ImprtDoc: " + oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Error setting U_COR_ImprtDoc: " + oErrMsg);
                                                    }
                                                }
                                            }
                                            // 02-29-2024 end
                                            try
                                            {
                                                document.UserFields.Fields.Item("U_COR_ImpSrc").Value = "940";
                                            }
                                            catch (Exception c)
                                            {
                                                string oErrMsg = c.Message;
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Error setting U_COR_ImpSrc: " + oErrMsg);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Error setting U_COR_ImpSrc: " + oErrMsg);
                                                }
                                            }
                                            // 03-24-2026 lrussell begin
                                            // udf U_COR_SrcDoc not in database
                                            /*
                                            // 09-08-2023 begin
                                            try
                                            {
                                                document.UserFields.Fields.Item("U_COR_SrcDoc").Value = "940";
                                            }
                                            catch (Exception c)
                                            {
                                                string oErrMsg = c.Message;
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Error setting U_COR_SrcDoc: " + oErrMsg);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Error setting U_COR_SrcDoc: " + oErrMsg);
                                                }
                                            }
                                            // 09-08-2023 end
                                            */
                                            // 03-24-2026 lrussell end
                                            if (edi940HeaderRecord.RequestedShipDate.HasValue && edi940HeaderRecord.RequestedShipDate.Value > DateTime.MinValue)
                                            {
                                                document.DocDueDate = edi940HeaderRecord.RequestedShipDate.Value;
                                            }
                                            else
                                            {
                                                document.DocDueDate = DateTime.Today;
                                            }
                                            document.DocDate = DateTime.Today;
                                            document.TaxDate = DateTime.Today;

                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToName))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing Ship To Name");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Ship To Name");
                                                }
                                                document.UserFields.Fields.Item("U_InfoW2ShNm").Value = edi940HeaderRecord.ShipToName;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToAttention))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing Ship To Attention");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Ship To Attention");
                                                }
                                                document.UserFields.Fields.Item("U_InfoW2ShAt").Value = edi940HeaderRecord.ShipToAttention;
                                            }

                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToAddress1))
                                            {
                                                document.AddressExtension.ShipToStreet = edi940HeaderRecord.ShipToAddress1;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToAddress2))
                                            {
                                                document.AddressExtension.ShipToBlock = edi940HeaderRecord.ShipToAddress2;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToCity))
                                            {
                                                document.AddressExtension.ShipToCity = edi940HeaderRecord.ShipToCity;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToState))
                                            {
                                                try
                                                {
                                                    document.AddressExtension.ShipToState = edi940HeaderRecord.ShipToState;
                                                }
                                                catch (Exception eS)
                                                {
                                                    string oErrMsg = "3PL error setting ship to state =>" + eS.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry(oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Error(oErrMsg);
                                                    }
                                                    set940Error(oErrMsg, edi940HeaderRecord.HeaderId, autoImport, false);
                                                }
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToZip))
                                            {
                                                document.AddressExtension.ShipToZipCode = edi940HeaderRecord.ShipToZip;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToCountry))
                                            {
                                                try
                                                {
                                                    if (edi940HeaderRecord.ShipToCountry.Equals("USA", StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        document.AddressExtension.ShipToCountry = "US";
                                                    }
                                                    else if (edi940HeaderRecord.ShipToCountry.Equals("CAN", StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        document.AddressExtension.ShipToCountry = "CA";
                                                    }
                                                    else
                                                    {
                                                        document.AddressExtension.ShipToCountry = edi940HeaderRecord.ShipToCountry;
                                                    }
                                                }
                                                catch (Exception ctry)
                                                {
                                                    string oErrMsg = "3PL error setting ship to country =>" + ctry.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry(oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Error(oErrMsg);
                                                    }
                                                    set940Error(oErrMsg, edi940HeaderRecord.HeaderId, autoImport, false);
                                                }
                                            }
                                            else
                                            {
                                                document.AddressExtension.ShipToCountry = "US";
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToLocationCode))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing Ship To Location Code");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Ship To Location Code");
                                                }
                                                document.UserFields.Fields.Item("U_InfoW2Lc").Value = edi940HeaderRecord.ShipToLocationCode;
                                            }
                                            // 09-06-2023 begin
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.StoreNumber))
                                            {
                                                document.UserFields.Fields.Item("U_InfoW2Sc").Value = edi940HeaderRecord.StoreNumber;
                                            }
                                            // 09-06-2023 end
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToPhoneNo))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing Ship To Phone No.");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Ship To Phone No.");
                                                }

                                                edi940HeaderRecord.ShipToPhoneNo = edi940HeaderRecord.ShipToPhoneNo.Trim();
                                                document.UserFields.Fields.Item("U_InfoW2Cnn").Value = edi940HeaderRecord.ShipToPhoneNo;

                                                string delNo = edi940HeaderRecord.ShipToPhoneNo;
                                                delNo = delNo.Replace(" ", String.Empty);
                                                try
                                                {
                                                    Int32 oPhNo = 0;
                                                    bool result = Int32.TryParse(delNo, out oPhNo);
                                                    if (result == false)
                                                    {
                                                        delNo = System.Text.RegularExpressions.Regex.Replace(delNo, @"^[A-Za-z]+", "");
                                                        if (delNo.Contains("#"))
                                                        {
                                                            delNo = System.Text.RegularExpressions.Regex.Replace(delNo, "#", "");
                                                        }

                                                        if (delNo.Contains("("))
                                                        {
                                                            delNo = delNo.Replace('(', ' ');
                                                        }
                                                        if (delNo.Contains(")"))
                                                        {
                                                            delNo = delNo.Replace(')', ' ');
                                                        }
                                                        if (delNo.Contains("-"))
                                                        {
                                                            delNo = System.Text.RegularExpressions.Regex.Replace(delNo, "-", " ");
                                                        }
                                                        if (delNo.Contains(","))
                                                        {
                                                            delNo = delNo.Replace(",", String.Empty);
                                                        }
                                                        if (delNo.Contains("."))
                                                        {
                                                            delNo = delNo.Replace(".", String.Empty);
                                                        }
                                                        delNo = delNo.Replace(" ", String.Empty);
                                                    }
                                                    delNo = delNo.Replace(" ", String.Empty);
                                                }
                                                catch (Exception dp)
                                                {
                                                    String oErrMessage = dp.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error formatting ship to phone number =>" + oErrMessage);
                                                    }
                                                    else
                                                    {
                                                        _logger.Error("Error formatting ship to phone number =>" + oErrMessage);
                                                    }
                                                }
                                                delNo = delNo.Trim();
                                                if (delNo.Length > 10)
                                                {
                                                    delNo = delNo.Substring(0, 10);
                                                }
                                                document.UserFields.Fields.Item("U_InfoDelPhone").Value = delNo;

                                            }
                                            // populate ship to phone# with 000 000 0000 if none is sent per Dan
                                            // 06-11-2020 CHANGED to 0000000000 per Dan
                                            else
                                            {
                                                document.UserFields.Fields.Item("U_InfoDelPhone").Value = "0000000000";
                                            }

                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.OrderNumber))
                                            {
                                                _logger.Debug("Processing 3PL Customer Reference");
                                                document.UserFields.Fields.Item("U_Info3PLCustRef").Value = edi940HeaderRecord.OrderNumber;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.TransactionType))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing 3PL Order Type");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing 3PL Order Type");
                                                }
                                                document.UserFields.Fields.Item("U_Info3PLOrdType").Value = edi940HeaderRecord.TransactionType;
                                                document.UserFields.Fields.Item("U_C3_OrigShipType").Value = edi940HeaderRecord.TransactionType;
                                            }


                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipFromLocationCode))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing 3PL Ship From Location");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing 3PL Ship From Location");
                                                }
                                                document.UserFields.Fields.Item("U_InfoShipFromStore").Value = edi940HeaderRecord.ShipFromLocationCode;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipFromName))
                                            {
                                                _logger.Debug("Processing 3PL Ship From Name");
                                                document.UserFields.Fields.Item("U_InfoShipFromName").Value = edi940HeaderRecord.ShipFromName;
                                            }
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.TrxPurpose))
                                            {
                                                _logger.Debug("Processing Trx Purpose");
                                                document.UserFields.Fields.Item("U_InfoPurposeCode").Value = edi940HeaderRecord.TrxPurpose;
                                            }
                                            try
                                            {
                                                if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.TransportMethod))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Transport Method");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Transport Method");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2TMethod").Value = edi940HeaderRecord.TransportMethod;
                                                }
                                            }
                                            catch (Exception tc2)
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Error setting transportation method =>" + tc2.Message);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Error setting transport method =>" + tc2.Message);
                                                }
                                            }


                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.PaymentMethod))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing PaymentMethod");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing PaymentMethod");
                                                }// PP=Prepaid, TP=Third Party, etc.
                                                document.UserFields.Fields.Item("U_InfoW2Mop").Value = edi940HeaderRecord.PaymentMethod;
                                            }
                                            // 09-15-2023 begin
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.DepartmentNumber))
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing Department");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Department");
                                                }
                                                document.UserFields.Fields.Item("U_C3_Department").Value = edi940HeaderRecord.DepartmentNumber;
                                            }
                                            // 09-15-2023 end
                                            if (!String.IsNullOrWhiteSpace(edi940HeaderRecord.CarrierCode) && oIs3PL == "Y" && !String.IsNullOrWhiteSpace(shipCode))
                                            {
                                                document.UserFields.Fields.Item("U_C3_OrigShipType").Value = shipCode;
                                            }

                                            document.UserFields.Fields.Item("U_InfoW2CnNo").Value = "0";
                                            document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2S";
                                            string temp = (string)document.UserFields.Fields.Item("U_InfoW2ShNm").Value;
                                            string prdDesc = "";
                                            try
                                            {
                                                prdDesc = document.UserFields.Fields.Item("U_InfoW2PrdDesc").ToString();
                                            }
                                            catch
                                            {
                                                prdDesc = "";
                                            }
                                            if (edi940HeaderRecord.CardCode.StartsWith("Wayfair") && String.IsNullOrWhiteSpace(prdDesc))
                                            {
                                                document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                            }
                                            else if (edi940HeaderRecord.CardCode.StartsWith("TSC") && String.IsNullOrWhiteSpace(prdDesc))
                                            {
                                                document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                            }
                                            if (edi940HeaderRecord.CardCode.StartsWith("Wayfair") || edi940HeaderRecord.CardCode.Equals("WAYFAIR") || edi940HeaderRecord.CardCode.Equals("Wayfair"))
                                            {
                                                document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                            }
                                            if (edi940HeaderRecord.CardCode.StartsWith("HDCL") && String.IsNullOrWhiteSpace(prdDesc))
                                            {
                                                try
                                                {
                                                    if (String.IsNullOrWhiteSpace(edi940HeaderRecord.ShipToLocationCode))
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                    }
                                                    else
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2S";
                                                    }
                                                }
                                                catch (Exception h)
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry(h.Message);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug(h.Message);
                                                    }
                                                }
                                            }
                                            if (autoImport)
                                            {
                                                Import_Log.LogEntry("Processing 940 Detail Lines");
                                            }
                                            else
                                            {
                                                _logger.Debug("Processing 940 Detail Lines");
                                            }
                                            int lineCounter = 0;
                                            _logger.Debug(edi940HeaderRecord.Details.Count.ToString() + " Detail rows to process");
                                            foreach (Edi940DetailRecord detailRecord in edi940HeaderRecord.Details)
                                            {
                                                _logger.Debug("HeaderId: " + detailRecord.HeaderId.ToString() + " Line# " + detailRecord.LineNumber);
                                                if (lineCounter > 0)
                                                {
                                                    document.Lines.Add();
                                                    string oInfo = document.Lines.LineNum.ToString();
                                                }
                                                lineCounter++;

                                                if (!String.IsNullOrWhiteSpace(detailRecord.BuyerItemCode)
                                                && (String.IsNullOrWhiteSpace(detailRecord.VendorItemCode)
                                                || edi940HeaderRecord.CardCode.StartsWith("LowesNet")
                                                || edi940HeaderRecord.CardCode.StartsWith("Lowes2")
                                                || edi940HeaderRecord.CardCode.StartsWith("TSCCL")
                                                || edi940HeaderRecord.CardCode.StartsWith("HDCL")
                                                || edi940HeaderRecord.CardCode.StartsWith("WAYFAIR")
                                                || edi940HeaderRecord.CardCode.StartsWith("APEX")
                                                || edi940HeaderRecord.CardCode.StartsWith("Bravo")
                                                )
                                                 && !(edi940HeaderRecord.CardCode.StartsWith("3PL-"))
                                                    )
                                                {
                                                    _logger.Debug("Looking up Item Code " + detailRecord.BuyerItemCode + " from Business Partner");
                                                    if (edi940HeaderRecord.SBOCardCode == null || edi940HeaderRecord.SBOCardCode.Trim().Length == 0)
                                                    {
                                                        document.Lines.ItemCode = LookupBuyerItemCode(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.BuyerItemCode);
                                                    }
                                                    else
                                                    {
                                                        if (edi940HeaderRecord.CardCode.StartsWith("LowesNet"))
                                                        {
                                                            document.Lines.ItemCode = LookupBuyerItemCode(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.BuyerItemCode);
                                                        }
                                                        else
                                                        {
                                                            document.Lines.ItemCode = LookupBuyerItemCode(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.BuyerItemCode);
                                                        }
                                                    }

                                                    if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                    {
                                                        if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                        {
                                                            if (edi940HeaderRecord.SBOCardCode == null || edi940HeaderRecord.SBOCardCode.Trim().Length == 0)
                                                            {
                                                                set940Error("Invalid item found for PO# '" + edi940HeaderRecord.PurchaseOrderReference.Trim() + "', HeaderId = " + edi940HeaderRecord.HeaderId, edi940HeaderRecord.HeaderId, autoImport, false);
                                                                throw new WebApiException("Could not locate Item " + detailRecord.BuyerItemCode + " for CardCode " + edi940HeaderRecord.CardCode);
                                                            }
                                                            else
                                                            {
                                                                set940Error("PO# " + edi940HeaderRecord.PurchaseOrderReference + " invalid Item " + detailRecord.BuyerItemCode + " for CardCode " + edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.HeaderId, autoImport, false);
                                                                throw new WebApiException("Could not locate Item  for  PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                                            }
                                                        }
                                                    }
                                                }
                                                else if (!String.IsNullOrWhiteSpace(detailRecord.VendorItemCode))
                                                {
                                                    _logger.Debug("Assigning Item Code Directly");
                                                    if (edi940HeaderRecord.CardCode.StartsWith("LowesNet") ||
                                                        edi940HeaderRecord.CardCode.StartsWith("WAYFAIR") ||
                                                        edi940HeaderRecord.CardCode.StartsWith("TSCCL") ||
                                                        edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                                                    {
                                                        document.Lines.ItemCode = LookupVendorItemCode(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.VendorItemCode);
                                                        if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode, autoImport)))
                                                        {
                                                            document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                        }
                                                    }
                                                    else if (oIs3PL == "Y")
                                                    {
                                                        document.Lines.ItemCode = Lookup3PLItemCode(edi940HeaderRecord.HeaderId, edi940HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.VendorItemCode, autoImport);
                                                        // 09-07-2023 begin
                                                        if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                        {
                                                            set940Error("Invalid item found ", edi940HeaderRecord.HeaderId, autoImport, false);
                                                            throw new WebApiException("ItemCode (VendorItemCode) is required");
                                                        }
                                                        // 09-07-2023 end
                                                        if (detailRecord.RetailPrice.HasValue && detailRecord.RetailPrice.Value > 0)
                                                        {
                                                            try
                                                            {
                                                                double oRetailPrice = detailRecord.RetailPrice.Value;
                                                                document.Lines.UserFields.Fields.Item("U_C3_RetailPrc").Value = oRetailPrice;
                                                            }
                                                            catch (Exception Rt)
                                                            {
                                                                if (autoImport)
                                                                {
                                                                    Import_Log.LogEntry("Error saving retail price " + detailRecord.RetailPrice.ToString() + " for " + edi940HeaderRecord.PurchaseOrderReference + " => " + Rt.Message);
                                                                }
                                                                else
                                                                {
                                                                    _logger.Error("Error saving retail price " + detailRecord.RetailPrice.ToString() + " for " + edi940HeaderRecord.PurchaseOrderReference + " => " + Rt.Message);
                                                                }
                                                            }
                                                        }

                                                        try
                                                        {
                                                            if (detailRecord.GrossPkgWeight == null || detailRecord.GrossPkgWeight.ToString().Length == 0)
                                                            {

                                                                document.Lines.UserFields.Fields.Item("U_InfoGrossPkgWgt").Value = Convert.ToDouble("0.00");
                                                            }
                                                            else
                                                            {
                                                                try
                                                                {
                                                                    string oGrsPkgWgt = detailRecord.GrossPkgWeight.ToString();
                                                                    document.Lines.UserFields.Fields.Item("U_InfoGrossPkgWgt").Value = Convert.ToDouble(oGrsPkgWgt);
                                                                }
                                                                catch
                                                                {
                                                                    document.Lines.UserFields.Fields.Item("U_InfoGrossPkgWgt").Value = Convert.ToDouble("0.00");
                                                                }
                                                            }
                                                        }
                                                        catch (Exception pw)
                                                        {
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("Error setting gross package weight =>" + pw.Message);
                                                            }
                                                            else
                                                            {
                                                                Import_Log.LogEntry("Error setting gross package weight =>" + pw.Message);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                        document.Lines.ItemCode = detailRecord.VendorItemCode;
                                                    }
                                                    if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode, autoImport)))
                                                    {
                                                        document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                    }
                                                }
                                                else
                                                {
                                                    document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                    if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                    {
                                                        set940Error("Invalid item found", edi940HeaderRecord.HeaderId, autoImport, false);
                                                        throw new WebApiException("ItemCode (VendorItemCode) is required");
                                                    }
                                                }
                                                if (!String.IsNullOrWhiteSpace(oVBUWhs))
                                                {
                                                    document.Lines.WarehouseCode = oVBUWhs;
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

                                                if (!String.IsNullOrWhiteSpace(detailRecord.PackingNotes))
                                                {
                                                    document.Lines.UserFields.Fields.Item("U_InfoW2PackNote").Value = detailRecord.PackingNotes;
                                                }
                                                if (!String.IsNullOrWhiteSpace(detailRecord.ItemUPC))
                                                {
                                                    document.Lines.UserFields.Fields.Item("U_InfoW2ItemUPC").Value = detailRecord.ItemUPC;
                                                }


                                                if (!String.IsNullOrWhiteSpace(detailRecord.UnitOfMeasure) && oIs3PL == "Y")
                                                {
                                                    string oUOM = detailRecord.UnitOfMeasure;
                                                    if (oUOM.ToUpper() == "EA")
                                                    {
                                                        oUOM = "Each";
                                                    }
                                                    document.Lines.UoMEntry = getUnitOfMeasure(document.Lines.ItemCode, oUOM, autoImport);
                                                }

                                            }


                                            string oLineInfo = document.Lines.LineNum.ToString();
                                            int oNoLines = document.Lines.Count;
                                            bool bFoundDoc = checkExistingOrders(edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.SBOCardCode, edi940HeaderRecord.CardCode, poDate, recDate, reqShipDate, edi940HeaderRecord.PurchaseOrderReference, edi940HeaderRecord.ShipToLocationCode, edi940HeaderRecord.StoreNumber, autoImport);
                                            if (bFoundDoc == false)
                                            {
                                                String msg1 = _company.GetLastErrorDescription();
                                                int oErrCode1 = _company.GetLastErrorCode();
                                                int oRet = document.Add();
                                                if (oRet != 0)
                                                {
                                                    String msg = _company.GetLastErrorDescription();
                                                    int oErrCode = _company.GetLastErrorCode();
                                                    if (oRet == 5009 || oRet == -5009)
                                                    {
                                                        msg = "Error adding Sales Order - Missing Item Code => " + msg;
                                                    }
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry(msg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Error(msg);
                                                    }
                                                    set940Error(msg, edi940HeaderRecord.HeaderId, autoImport, false);
                                                    if (!msg.StartsWith("(402) The data types ntext and nvarchar"))
                                                    {
                                                        throw new WebApiException(msg);
                                                    }
                                                }

                                                DocumentKey documentKey = new DocumentKey();
                                                String strDocEntry = _company.GetNewObjectKey();
                                                documentKey.DocEntry = Int32.Parse(strDocEntry);
                                                documentKey.DocNum = GetDocNum(documentKey.DocEntry);
                                                // if VBU whs is not blank update the sales order lines with the VBU warehouse
                                                if (!String.IsNullOrWhiteSpace(oVBUWhs))
                                                {
                                                    try
                                                    {
                                                        document.GetByKey(documentKey.DocEntry);
                                                        if (!(document == null))
                                                        {

                                                            int noLines = document.Lines.Count;
                                                            int noLnUpdated = 0;
                                                            for (int i = 0; i < noLines; i++)
                                                            {
                                                                document.Lines.SetCurrentLine(i);
                                                                document.Lines.WarehouseCode = oVBUWhs;
                                                                noLnUpdated = noLnUpdated + 1;
                                                            }
                                                            if (noLnUpdated > 0)
                                                            {
                                                                document.Update();
                                                            }
                                                        }
                                                    }
                                                    catch (Exception dl)
                                                    {
                                                        string oErrMsg = dl.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error setting warehouse => " + oErrMsg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Error setting warehouse => " + oErrMsg);
                                                        }
                                                    }
                                                }
                                                try
                                                {
                                                    document.GetByKey(documentKey.DocEntry);

                                                    int noLnUpdated = 0;
                                                    int noLines = document.Lines.Count;
                                                    for (int i = 0; i < noLines; i++)
                                                    {
                                                        if (document.Lines.TreeType.ToString() == "I")
                                                        {
                                                            document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = 0;
                                                            noLnUpdated = noLnUpdated + 1;
                                                        }
                                                    }
                                                    if (noLnUpdated > 0)
                                                    {
                                                        document.Update();
                                                    }
                                                }
                                                catch (Exception dl)
                                                {
                                                    document.GetByKey(documentKey.DocEntry);
                                                    string oErrMsg = dl.Message;
                                                    Recordset rs2 = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error setting edi line number for child BOM items => " + oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Error("Error setting edi line number for child BOM items => " + oErrMsg);
                                                    }
                                                    /*
                                                    string oQry1 = "update rdr1 set U_InfoW2LNo = 0 where DocEntry = " + document.DocEntry.ToString() + " and TreeType = 'I'";
                                                    rs2.DoQuery(oQry1);
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Ran sql query to set edi line number for child BOM items SO DocEntry " + document.DocEntry.ToString());
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Ran sql query to set edi line number for child BOM items => " + document.DocEntry.ToString());
                                                    }
                                                     */
                                                }
                                                // check for null U_InfoW2LNo
                                                try
                                                {
                                                    document.GetByKey(documentKey.DocEntry);
                                                    if (!(document == null))
                                                    {
                                                        int noLnUpdated = 0;
                                                        int noLines = document.Lines.Count;
                                                        for (int i = 0; i < noLines; i++)
                                                        {
                                                            document.Lines.SetCurrentLine(i);
                                                            try
                                                            {
                                                                string oEdiLNo = document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value.ToString();
                                                                if (String.IsNullOrWhiteSpace(oEdiLNo))
                                                                {
                                                                    document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = 0;
                                                                    noLnUpdated = noLnUpdated + 1;
                                                                }
                                                            }
                                                            catch (Exception LNo)
                                                            {
                                                                string oErrMsg = LNo.Message;
                                                                if (autoImport)
                                                                {
                                                                    Import_Log.LogEntry("Error updating Ln# " + i.ToString() + " udf U_InfoW2LNo  => " + oErrMsg);
                                                                }
                                                                else
                                                                {
                                                                    _logger.Error("Error updating Ln# " + i.ToString() + " udf U_InfoW2LNo => " + oErrMsg);
                                                                }
                                                            }
                                                        }
                                                        if (noLnUpdated > 0)
                                                        {
                                                            document.Update();
                                                        }
                                                    }
                                                }
                                                catch (Exception dl)
                                                {
                                                    string oErrMsg = dl.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error updating U_InfoW2LNo  => " + oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Error("Error updating U_InfoW2LNo => " + oErrMsg);
                                                    }
                                                }

                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Leaving Process940Record. Created Document " + documentKey.DocNum);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Leaving Process940Record.  Created Document " + documentKey.DocNum);
                                                }
                                                return documentKey;
                                            }
                                            else
                                            {
                                                set940Error("Duplicate SO", edi940HeaderRecord.HeaderId, autoImport, false);
                                                edi940HeaderRecord.Processed = true;
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Leaving Process940Record. Skipped duplicate for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Leaving Process940Record.  Skipped duplicate for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                                }
                                                DocumentKey documentKey = new DocumentKey();
                                                return documentKey;
                                            }
                                        }
                                        else
                                        {
                                            if (autoImport)
                                            {
                                                Import_Log.LogEntry("Leaving Process940Record. Skipped invalid 3PL Shipper found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                            }
                                            else
                                            {
                                                _logger.Debug("Leaving Process940Record.  Skipped invalid 3PL Shipper found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                            }
                                            string ErrorDetail = "";
                                            if (String.IsNullOrWhiteSpace(serviceLevel))
                                            {
                                                ErrorDetail = "Carrier Code " + carrierCode;
                                            }
                                            else
                                            {
                                                ErrorDetail = "Carrier Code: " + carrierCode + ", Service Lv: " + serviceLevel + ", STC: " + oSTC;
                                            }
                                            set940Error("Invalid Invalid 3PL Shipper found => " + ErrorDetail, edi940HeaderRecord.HeaderId, autoImport, false);
                                            DocumentKey documentKey = new DocumentKey();
                                            return documentKey;
                                        }
                                    }
                                    else
                                    {
                                        if (autoImport)
                                        {
                                            Import_Log.LogEntry("Leaving Process940Record. Skipped invalid SCAC found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                        }
                                        else
                                        {
                                            _logger.Debug("Leaving Process940Record.  Skipped invalid SCAC found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                        }
                                        string ErrorDetail = "";
                                        if (String.IsNullOrWhiteSpace(serviceLevel))
                                        {
                                            ErrorDetail = "Carrier Code " + carrierCode;
                                        }
                                        else
                                        {
                                            ErrorDetail = "Carrier Code: " + carrierCode + ", Service Lv: " + serviceLevel;
                                        }
                                        set940Error("Invalid SCAC found => " + ErrorDetail, edi940HeaderRecord.HeaderId, autoImport, false);
                                        DocumentKey documentKey = new DocumentKey();
                                        return documentKey;
                                    }
                                }
                                else
                                {
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry("Leaving Process940Record. Skipped price mismatch found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                    }
                                    else
                                    {
                                        _logger.Debug("Leaving Process940Record.  Skipped price mismatch found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                    }
                                    if (oIs3PL == "N")
                                    {
                                        set940Error("Price variance found", edi940HeaderRecord.HeaderId, autoImport, false);
                                    }
                                    DocumentKey documentKey = new DocumentKey();
                                    return documentKey;
                                }
                            }
                            else
                            {
                                if (autoImport)
                                {
                                    Import_Log.LogEntry("Leaving Process940Record. Skipped invalid item(s) found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                }
                                else
                                {
                                    _logger.Debug("Leaving Process940Record.  Skipped invalid item(s) found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                                }
                                set940Error("Invalid item(s) found", edi940HeaderRecord.HeaderId, autoImport, false);
                                DocumentKey documentKey = new DocumentKey();
                                return documentKey;
                            }
                        }
                        else
                        {
                            String oErr = "PrePaid found";
                            String oErr2 = "PrePaid Error";
                            if (oIs3PL == "Y")
                            {
                                oErr = "Invalid PaymentMethod";
                                oErr2 = oErr;
                            }
                            /*
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Leaving Process940Record. Skipped PrePaid found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                            }
                            else
                            {
                                _logger.Debug("Leaving Process940Record.  Skipped PrePaid found for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                            }
                            set940Error("PrePaid Error", edi940HeaderRecord.HeaderId, autoImport);
                             * */
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Leaving Process940Record. " + oErr + " for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                            }
                            else
                            {
                                _logger.Debug("Leaving Process940Record. " + oErr + " for PO# " + edi940HeaderRecord.PurchaseOrderReference);
                            }
                            set940Error(oErr2, edi940HeaderRecord.HeaderId, autoImport, false);
                            DocumentKey documentKey = new DocumentKey();
                            return documentKey;
                        }
                    }
                    else
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Leaving Process940Record. Skipped duplicate PO# " + edi940HeaderRecord.PurchaseOrderReference);
                        }
                        else
                        {
                            _logger.Debug("Leaving Process940Record.  Skipped duplicate PO# " + edi940HeaderRecord.PurchaseOrderReference);
                        }
                        edi940HeaderRecord.Processed = true;
                        set940Error("Duplicate 940", edi940HeaderRecord.HeaderId, autoImport, false);
                        DocumentKey documentKey = new DocumentKey();
                        return documentKey;
                    }
                }
                else
                {
                    return null;
                }

            }
            catch (Exception soEx)
            {
                String oErrMsg = soEx.Message;
                edi940HeaderRecord.ErrorMessage = oErrMsg;
                //08-12-2023 begin
                String oInnerExMsg = "";
                if (soEx.InnerException != null && !String.IsNullOrWhiteSpace(soEx.InnerException.Message))
                {
                    oInnerExMsg = soEx.InnerException.Message;
                }
                if (autoImport)
                {
                    if (soEx.InnerException == null)
                    {
                        Import_Log.LogEntry("Error in BoneSalesOrderDataLayer processing PO# " + edi940HeaderRecord.PurchaseOrderReference + ": " + oErrMsg);

                    }
                    else
                    {
                        Import_Log.LogEntry("Error in BoneSalesOrderDataLayer processing PO# " + edi940HeaderRecord.PurchaseOrderReference + ": " + oErrMsg + "(" + soEx.InnerException.Message + ")");
                    }
                    // 08-12-2023 begin
                    String oErrSrc = soEx.Source;
                    if (oErrSrc != null && !String.IsNullOrWhiteSpace(oErrSrc))
                    {
                        Import_Log.LogEntry("Error Source: " + oErrSrc);
                    }
                    /* String oStackTrc = soEx.StackTrace;
                     if (oStackTrc != null && oStackTrc.Trim().Length > 0)
                     {
                         Import_Log.LogEntry("Stack Trace: " + oStackTrc);
                     }*/
                    // 08-12-2023 end
                }
                else
                {
                    if (soEx.InnerException == null)
                    {
                        _logger.Error("Error in BoneSalesOrderDataLayer processing PO# " + edi940HeaderRecord.PurchaseOrderReference + ": " + oErrMsg);
                    }
                    else
                    {
                        _logger.Error("Error in BoneSalesOrderDataLayer processing PO# " + edi940HeaderRecord.PurchaseOrderReference + ": " + oErrMsg + " (" + soEx.InnerException.Message + ")");
                    }
                    // 08-12-2023 begin
                    String oErrSrc = soEx.Source; // 08-12-2023
                    if (oErrSrc != null && !String.IsNullOrWhiteSpace(oErrSrc))
                    {
                        _logger.Error("Error Source: " + oErrSrc);
                    }
                    /*String oStackTrc = soEx.StackTrace;
                    if (oStackTrc != null && oStackTrc.Trim().Length > 0)
                    {
                        Import_Log.LogEntry("Stack Trace: " + oStackTrc);
                    }*/
                    // 08-12-2023 end
                }
                set940Error(oErrMsg, edi940HeaderRecord.HeaderId, autoImport, false);

                return null;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(document);
            }
        }
        // 07-31-2023 end

        public DocumentKey Process850Record(Edi850HeaderRecord edi850HeaderRecord, bool autoImport, string ordSource)
        {
            // 07-16-2024 begin
            if (!edi850HeaderRecord.CardCode.ToUpper().StartsWith("INDOCOUNT"))
            {
                // 07-16-2024 end
                if (autoImport)
                {
                    Import_Log.LogEntry("Entering Process850Record");
                }
                else
                {
                    _logger.Debug("Entering Process850Record");
                }
                Documents document = _company.GetBusinessObject(BoObjectTypes.oOrders) as Documents;
                try
                {
                    // 08-14-2019 begin
                    Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                    try
                    {
                        String oQry1 = "update InfocusEdi850HeaderRecord set HasOpen856=1 where coalesce(cast(HasOpen856 as int),-1) < 0 and HeaderId =  " + edi850HeaderRecord.HeaderId + "; " +
                                       "update InfocusEdi850HeaderRecord set Processed=1 where len(ltrim(rtrim(coalesce(CardCode,'')))) = 0  and HeaderId =  " + edi850HeaderRecord.HeaderId + "; " +
                                       "update InfocusEdi850HeaderRecord set ProcessedPreSo855 = 0 where IsNull(cast(ProcessedPreSo855 as int),-1) < 0 and HeaderId =  " + edi850HeaderRecord.HeaderId + "; " + // 03-11-2024
                                       "update InfocusEdi850HeaderRecord set TrxPurpose='00' where len(ltrim(rtrim(coalesce(TrxPurpose,'')))) = 0 and HeaderId = " + edi850HeaderRecord.HeaderId + "; ";// 05-19-2021
                        // 07-14-2021 begin
                        /*
                        // 06-18-2021 begin
                       "update InfocusEdi850HeaderRecord set Processed=1, ErrorMessage = 'Duplicate Transaction' where HeaderId = " + edi850HeaderRecord.HeaderId +
                        " and PurchaseOrderReference in (select PONo from [InfocusEdi850_Unprocessed_Duplicates] where CardCode = '" + edi850HeaderRecord.CardCode + "' and PoNo = '" +
                        edi850HeaderRecord.PurchaseOrderReference + "'  and FirstHeader <> " + edi850HeaderRecord.HeaderId + ");";
                                   // 06-18-2021 end
                         */
                        // "update InfocusEdi850HeaderRecord set Processed=1, ErrorMessage = 'Duplicate Transaction' where HeaderId =  " + edi850HeaderRecord.HeaderId + " and HeaderId in (select HeaderId from [InfocusEdi850_Unprocessed_Duplicates]);";
                        rs.DoQuery(oQry1);
                    }
                    catch (Exception l)
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Error updating HasOpen856 and/or Processed =>" + l.Message);
                        }
                        else
                        {
                            _logger.Debug("Error updating HasOpen856 and/or Processed =>" + l.Message);
                        }
                    }
                    // 08-14-2019 end
                    // 12-06-2021 begin
                    System.GC.Collect();
                    try
                    {
                        String oQry1 = "execute [Infocus_EDI850_Duplicates] " + edi850HeaderRecord.HeaderId + ";";
                        rs.DoQuery(oQry1);
                    }
                    catch (Exception l)
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Error updating Processed =>" + l.Message);
                        }
                        else
                        {
                            _logger.Debug("Error updating Processed =>" + l.Message);
                        }
                    }
                    // 12-06-2021 end
                    // 05-19-2021 begin
                    string oIs3PL = "N";
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Check to see if 3PL");
                    }
                    else
                    {
                        _logger.Debug("Check to see if 3PL");
                    }
                    try
                    {
                        string oQry1 = "select coalesce([3PL],'N') Is3PL from InfocusEdi.dbo.WebApiDbContext  WITH(NOLOCK) where CardCode = '" +
                                        edi850HeaderRecord.CardCode.Trim() + "' and SBOCardCode = '" + edi850HeaderRecord.SBOCardCode.Trim() + "'";
                        rs.DoQuery(oQry1);
                        if (!rs.EoF)
                        {
                            oIs3PL = rs.Fields.Item("Is3PL").Value.ToString();
                        }
                    }
                    catch (Exception pl)
                    {
                        oIs3PL = "N";
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Error setting Is3PL =>" + pl.Message);
                        }
                        else
                        {
                            _logger.Error("Error setting Is3PL =>" + pl.Message);
                        }
                    }
                    // 05-10-2021 end
                    // 05-25-2021 begin
                    string oSTC = "";
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Is 3PL? " + oIs3PL);
                    }
                    else
                    {
                        _logger.Debug("Is 3PL? " + oIs3PL);
                    }
                    // 2025-01 SCAC SOW quote comment
                    // set string oUseMatrix = 'N';
                    // if Is3PL = 'N' then get Bp UDF and set oUseMatrix 
                    // add or oUseMatrix = 'Y'

                    if (oIs3PL == "Y")
                    {
                        try
                        {
                            // 06-13-2021 begin
                            string oQry1 = "select top 1 IsNull(T0.U_C3_CustAcct,'') as CustAcct FROM [dbo].[@C3_CASC] T0 With(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                            edi850HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = '" + edi850HeaderRecord.CustCode3PL.Trim() + "'";
                            // 09-26-2022 begin
                            if (edi850HeaderRecord.CardCode.StartsWith("INDOCOUNT") && edi850HeaderRecord.CustCode3PL == "KHDO" && edi850HeaderRecord.VendorNumber.Trim() == "078699963")
                            {
                                oQry1 = "select top 1 IsNull(T0.U_C3_CustAcct,'') as CustAcct FROM [dbo].[@C3_CASC] T0 With(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                           edi850HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = 'KHDO2' ";
                            }
                            // 09-26-2022 end
                            //oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC]  T0 WHERE T0.[U_CardCode] = '" +
                            //         edi850HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = '" + edi850HeaderRecord.CustCode3PL.Trim() + "'";
                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CustCode3PL.Trim()) && edi850HeaderRecord.CustCode3PL.Trim() == "1068"
                                && !String.IsNullOrWhiteSpace(edi850HeaderRecord.ConsumerPO) && edi850HeaderRecord.ConsumerPO.Trim().Length >= 3
                                && !String.IsNullOrWhiteSpace(edi850HeaderRecord.ConsumerPO.Trim().Substring(0, 3)))  // 06-21-2021
                            {
                                oSTC = edi850HeaderRecord.ConsumerPO.Trim().Substring(0, 3);
                            }
                            else
                            {
                                try
                                {
                                    rs.DoQuery(oQry1);
                                    if (!rs.EoF)
                                    {
                                        // 09-26-2022 begin
                                        if (edi850HeaderRecord.CardCode.StartsWith("INDOCOUNT") && edi850HeaderRecord.CustCode3PL == "KHDO" && edi850HeaderRecord.VendorNumber.Trim() == "078699963")
                                        {
                                            oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC] T0 WITH(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                         edi850HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = 'KHDO2' ";
                                        }
                                        else
                                        {
                                            // 09-26-2022 end
                                            oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC] T0 WITH(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                                       edi850HeaderRecord.SBOCardCode.Trim() + "' and  T0.[U_C3_CustAcct] = '" + edi850HeaderRecord.CustCode3PL.Trim() + "'";
                                        } // 09-26-2022
                                    }
                                    else
                                    {
                                        oQry1 = "SELECT IsNull(T0.[U_Stc],'') STC FROM [dbo].[@C3_CASC] T0 WITH(NOLOCK) WHERE T0.[U_CardCode] = '" +
                                                edi850HeaderRecord.SBOCardCode.Trim() + "' and  UPPER(LTRIM(RTRIM(ISNULL(T0.[U_C3_CustAcct],'')))) = 'DEFAULT'";
                                    }
                                }
                                catch (Exception cacct)
                                {
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry("Error reading [@CASC] to validate account =>" + cacct.Message);
                                    }
                                    else
                                    {
                                        _logger.Error("Error reading [@CASC] to validate account =>" + cacct.Message);
                                    }
                                }
                                // 06-13-2021 end
                                rs.DoQuery(oQry1);
                                if (!rs.EoF)
                                {
                                    oSTC = rs.Fields.Item("STC").Value.ToString().Trim();
                                }
                            } // 06-13-2021
                        }
                        catch (Exception stc)
                        {
                            oSTC = "";
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Error reading [@CASC] =>" + stc.Message);
                            }
                            else
                            {
                                _logger.Error("Error reading [@C3_CASC] => " + stc.Message);
                            }
                            set850Error("Invalic STC =>" + stc.Message, edi850HeaderRecord.HeaderId, autoImport, false); // 03-14-2022
                        }
                    }
                    else
                    {
                        oSTC = "";
                    }
                    if (autoImport)
                    {
                        Import_Log.LogEntry("STC => " + oSTC);
                    }
                    else
                    {
                        _logger.Debug("STC => " + oSTC);
                    }
                    // 05-25-2021 end
                    // 06-21-2021 begin
                    if (oIs3PL == "Y")
                    {
                        if (String.IsNullOrWhiteSpace(oSTC) || oSTC.Trim().Length == 0)
                        {
                            if (autoImport)
                            {
                                Import_Log.LogEntry("STC is blank");
                            }
                            else
                            {
                                _logger.Error("STC is blank");
                            }
                            throw new Exception("STC is invalid");
                        }
                    }
                    // 06-21-2021 end
                    // 03-26-2021 begin
                    string oVBUWhs = "";
                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.VendorNumber))
                    {
                        try
                        {
                            string oQry1 = "select U_WhsCode from [@INFO_VBU_WHS]  WITH(NOLOCK) where Code = '" + edi850HeaderRecord.VendorNumber + "' and U_CardCode = '" + edi850HeaderRecord.SBOCardCode + "'";
                            rs.DoQuery(oQry1);
                            if (!rs.EoF)
                            {
                                oVBUWhs = rs.Fields.Item("U_WhsCode").Value.ToString();
                            }
                        }
                        catch (Exception vbu)
                        {
                            oVBUWhs = "";
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Error reading [@INFO_VBU_WHS =>" + vbu.Message);
                            }
                            else
                            {
                                _logger.Error("Error reading [@INFO_VBU_WHS] => " + vbu.Message);
                            }
                        }
                    }
                    // 03-26-2021 end
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Clearing 850 ErrorMessage");
                    }
                    else
                    {
                        _logger.Debug("Clearing 850 ErrorMessage");
                    }
                    set850Error(String.Empty, edi850HeaderRecord.HeaderId, autoImport, false); // 06-29-2019

                    /*if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.TrxPurpose)
                                && edi850HeaderRecord.TrxPurpose.Trim() == "01")
                    {
                        //Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                        try
                        {
                            string oQuery = "select DocEntry, DocNum from ORDR where Canceled = 'N' and CardCode = '" + edi850HeaderRecord.CardCode + "' and NumAtCard = '" + edi850HeaderRecord.PurchaseOrderReference + "'";
                            if (edi850HeaderRecord.SBOCardCode != null && edi850HeaderRecord.SBOCardCode.Trim().Length > 0)
                            {  // 01-17-2018
                                oQuery = "select DocEntry, DocNum from ORDR where Canceled = 'N' and CardCode = '" + edi850HeaderRecord.SBOCardCode + "' and NumAtCard = '" + edi850HeaderRecord.PurchaseOrderReference + "'";
                            } // 01-17-2018 end
                            rs.DoQuery(oQuery);
                            rs.MoveFirst();
                            if (!rs.EoF)
                            {
                                String oDocEntry = rs.Fields.Item("DocEntry").Value.ToString();
                                String oDocNum = rs.Fields.Item("DocNum").Value.ToString();
                                if (!String.IsNullOrWhiteSpace(oDocNum))
                                {
                                    if (edi850HeaderRecord.SBOCardCode == null || edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                                    {  // 01-17-2018
                                        rs.DoQuery("UPDATE ORDR SET U_InfoTrxPurpose = '" + edi850HeaderRecord.TrxPurpose + "' where Canceled = 'N' and CardCode = '" + edi850HeaderRecord.CardCode + "' and NumAtCard = '" + edi850HeaderRecord.PurchaseOrderReference + "'");
                                        // 01-17-2018 begin
                                    }
                                    else
                                    {
                                        rs.DoQuery("UPDATE ORDR SET U_InfoTrxPurpose = '" + edi850HeaderRecord.TrxPurpose + "' where Canceled = 'N' and CardCode = '" + edi850HeaderRecord.SBOCardCode + "' and NumAtCard = '" + edi850HeaderRecord.PurchaseOrderReference + "'");
                                    } // 01-17-2018 end
                                    DocumentKey documentKey = new DocumentKey();
                                    String strDocEntry = _company.GetNewObjectKey();
                                    documentKey.DocEntry = Int32.Parse(oDocEntry);
                                    documentKey.DocNum = Int32.Parse(oDocNum);
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry("Leaving Process850Record.  Updated Document " + documentKey.DocNum);
                                    }
                                    else
                                    {
                                        _logger.Debug("Leaving Process850Record.  Updated Document " + documentKey.DocNum);
                                    }
                                    return documentKey;
                                }
                                else
                                {
                                    return null;
                                }
                            }
                            return null;
                        }
                        finally
                        {
                            System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                        }
                    }
                    else */
                    // 06-07-2021 begin
                    if (String.IsNullOrWhiteSpace(edi850HeaderRecord.TrxPurpose))
                    {
                        edi850HeaderRecord.TrxPurpose = "00";
                    }
                    // 06-07-2021 end
                    if (!(edi850HeaderRecord.TrxPurpose.Trim() == "01"))
                    // 05-15-2020 end
                    {
                        // 07-16-2018 begin
                        //01-29-2021 begin 
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Check for duplicate orders");
                        }
                        else
                        {
                            _logger.Debug("Check for duplicate orders");
                        }
                        //bool bIsDuplicate = checkExistingOrders(edi850HeaderRecord.PurchaseOrderReference, edi850HeaderRecord.SBOCardCode); //11-30-2020 added CardCode
                        DateTime poDate = DateTime.Now;
                        DateTime recDate = DateTime.Now;
                        try
                        {
                            poDate = Convert.ToDateTime(edi850HeaderRecord.PurchaseOrderDate);
                        }
                        catch
                        {
                            poDate = DateTime.Now;
                        }

                        try
                        {
                            recDate = Convert.ToDateTime(edi850HeaderRecord.ReceivedDateTime);
                        }
                        catch
                        {
                            recDate = DateTime.Now;
                        }
                        // 02-24-2024 begin
                        DateTime reqShipDate = DateTime.Now;
                        try
                        {
                            reqShipDate = Convert.ToDateTime(edi850HeaderRecord.RequestedShipDate);
                            //  03-10-2025 begin
                            if (edi850HeaderRecord.CardCode.StartsWith("LowesNet") || edi850HeaderRecord.CardCode.StartsWith("LOWESNET"))
                            {
                                try
                                {
                                    string shipDateString = "";
                                    if (!edi850HeaderRecord.RequestedShipDate.HasValue || edi850HeaderRecord.RequestedShipDate.Value <= DateTime.MinValue)
                                    {
                                        shipDateString = "LowesNet Requested ship date is blank";
                                    }
                                    else
                                    {
                                        string dateString = reqShipDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                        shipDateString = "LowesNet Requested ship date: " + dateString;
                                    }
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry(shipDateString);
                                    }
                                    else
                                    {
                                        _logger.Debug(shipDateString);
                                    }
                                }
                                catch
                                {

                                }
                                try
                                {
                                    string shipDateString = "";
                                    if (!edi850HeaderRecord.RequestedDeliveryDate.HasValue || edi850HeaderRecord.RequestedDeliveryDate.Value <= DateTime.MinValue)
                                    {
                                        shipDateString = "LowesNet Requested Delivery date is blank";
                                    }
                                    else
                                    {
                                        DateTime oDate = edi850HeaderRecord.RequestedDeliveryDate.Value;
                                        string dateString = oDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                        shipDateString = "LowesNet Requested Delivery date: " + dateString;
                                    }
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry(shipDateString);
                                    }
                                    else
                                    {
                                        _logger.Debug(shipDateString);
                                    }
                                }
                                catch
                                {

                                }
                            }
                            // 03-10-2025 end
                        }
                        catch (Exception rsd)
                        {
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Error getting requested ship date: " + rsd.Message);
                            }
                            else
                            {
                                _logger.Debug("Error getting requested ship date: " + rsd.Message);
                            }
                        }
                        // 02-29-2024 end
                        //  03-10-2025 begin
                        if (edi850HeaderRecord.CardCode.StartsWith("LowesNet") || edi850HeaderRecord.CardCode.StartsWith("LOWESNET"))
                        {
                            try
                            {
                                string delDateString = "";
                                if (!edi850HeaderRecord.RequestedDeliveryDate.HasValue || edi850HeaderRecord.RequestedDeliveryDate.Value <= DateTime.MinValue)
                                {
                                    delDateString = "LowesNet Requested delivery date is blank";
                                }
                                else
                                {
                                    DateTime reqDeliveryDate = edi850HeaderRecord.RequestedDeliveryDate.Value;
                                    string dateString = reqDeliveryDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                    delDateString = "LowesNet Requested delivery date: " + dateString;
                                }
                                if (autoImport)
                                {
                                    Import_Log.LogEntry(delDateString);
                                }
                                else
                                {
                                    _logger.Debug(delDateString);
                                }
                            }
                            catch
                            {
                            }
                            try
                            {
                                string delDateString = "";
                                if (!edi850HeaderRecord.ExpectedDeliveryDate.HasValue || edi850HeaderRecord.ExpectedDeliveryDate.Value <= DateTime.MinValue)
                                {
                                    delDateString = "LowesNet expected delivery date is blank";
                                }
                                else
                                {
                                    DateTime expDeliveryDate = edi850HeaderRecord.ExpectedDeliveryDate.Value;
                                    string dateString = expDeliveryDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                    delDateString = "LowesNet expected delivery date: " + dateString;
                                }
                                if (autoImport)
                                {
                                    Import_Log.LogEntry(delDateString);
                                }
                                else
                                {
                                    _logger.Debug(delDateString);
                                }
                            }
                            catch
                            {
                            }
                            try
                            {
                                string delDateString = "";
                                if (!edi850HeaderRecord.ExpectedShipDate.HasValue || edi850HeaderRecord.ExpectedShipDate.Value <= DateTime.MinValue)
                                {
                                    delDateString = "LowesNet expected ship date is blank";
                                }
                                else
                                {
                                    DateTime expDeliveryDate = edi850HeaderRecord.ExpectedShipDate.Value;
                                    string dateString = expDeliveryDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
                                    delDateString = "LowesNet expected ship date: " + dateString;
                                }
                                if (autoImport)
                                {
                                    Import_Log.LogEntry(delDateString);
                                }
                                else
                                {
                                    _logger.Debug(delDateString);
                                }
                            }
                            catch
                            {
                            }
                        }
                        // 03-10-2025 end

                        bool bIsDuplicate = checkExistingOrders(edi850HeaderRecord.PurchaseOrderReference, edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CardCode, poDate, recDate, edi850HeaderRecord.CustomerOrderNumber, autoImport);
                        // 01-29-2021 end
                        if (bIsDuplicate == false)
                        {
                            int oHeaderId = edi850HeaderRecord.HeaderId;
                            edi850HeaderRecord.ErrorMessage = String.Empty; // 07-29-2019
                            // 10-22-2021 begin
                            bool bValidPayment = true;
                            if ((!edi850HeaderRecord.CardCode.ToUpper().StartsWith("TeeZed") && oIs3PL == "Y")
                                || oIs3PL == "N") // 01-27-2023
                            {
                                // 03-24-2026 lrussell  begin
                                if (String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined03) || String.IsNullOrEmpty(edi850HeaderRecord.UserDefined03))
                                {
                                    edi850HeaderRecord.UserDefined03 = string.Empty;
                                }
                                // 03-24-2026 lrussell end

                                // 07-07-2023 bValidPayment was not being set with result from checkPaymentType
                                bValidPayment = checkPaymentType(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.UserDefined03, autoImport);
                            }
                            // 03-09-2022 begin
                            if (edi850HeaderRecord.CardCode.Trim().ToUpper().StartsWith("TEEZED"))
                            {
                                bValidPayment = true;
                                if (autoImport)
                                {
                                    Import_Log.LogEntry("TeeZed set valid PaymentMethod = true");
                                }
                                else
                                {
                                    _logger.Debug("TeeZed set valid PaymentMethod = true");
                                }
                            }
                            else
                                // 03-09-2022 end
                                // 03- 01-2022 begin
                                if (oIs3PL == "Y" && String.IsNullOrWhiteSpace(edi850HeaderRecord.PaymentMethod))
                                {
                                    bValidPayment = false;
                                }

                            // 03-01-2022 end
                            if (bValidPayment == true)
                            {
                                // 10-22-2021 end
                                if (autoImport)
                                {
                                    Import_Log.LogEntry("Check for invalid items");
                                }
                                else
                                {
                                    _logger.Debug("Check for invalid items");
                                }
                                // 10-24-2019 begin
                                bool bInvalidItems = checkItems(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, autoImport); ;
                                if (bInvalidItems == false)
                                {
                                    // 10-24-2019 end
                                    bool bPriceVariance = false;

                                    if (oIs3PL == "N") // 06-03-2021
                                    {
                                        if (autoImport)
                                        {
                                            Import_Log.LogEntry("Check for price variance");
                                        }
                                        else
                                        {
                                            _logger.Debug("Check for price variance");
                                        }
                                        bPriceVariance = checkPrice(edi850HeaderRecord.PurchaseOrderReference, edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.HeaderId, autoImport); ;
                                    } // 06-03-2021
                                    if (bPriceVariance == false) // 05-19-2021 added check for 3PL
                                    {
                                        edi850HeaderRecord.ErrorMessage = " "; // 07-14-2021
                                        // 07-16-2018 end
                                        // 10-11-2019 begin
                                        string carrierCode = "";
                                        string serviceLevel = "";
                                        if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CarrierCode))
                                        {
                                            // 03-04-2022 begin
                                            /* if (oIs3PL == "Y")
                                             {
                                                 string oXrefCarrier = getTrgtCarrier(edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CarrierCode, oSTC, autoImport);
                                             }
                                             else
                                             { // 03-04-2022 end
                                             */
                                            carrierCode = edi850HeaderRecord.CarrierCode;
                                            // } // 03-04-2022
                                        }
                                        if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ServiceLevel))
                                        {
                                            serviceLevel = edi850HeaderRecord.ServiceLevel;
                                        }
                                        if (autoImport)
                                        {
                                            Import_Log.LogEntry("Check SCAC => Carrier Code / Service Level");
                                        }
                                        else
                                        {
                                            _logger.Debug("Check SCAC => Carrier Code / Service Level");
                                        }
                                        bool bValidSCAC = checkSCAC(edi850HeaderRecord.PurchaseOrderReference, edi850HeaderRecord.SBOCardCode, carrierCode, serviceLevel, oIs3PL, autoImport);
                                        if (bValidSCAC == true)
                                        {
                                            // 09-24-2022 begin
                                            bool bValidShipper = true;
                                            if (oIs3PL == "Y")
                                            {
                                                bValidShipper = checkC3Shipper(edi850HeaderRecord.SBOCardCode, carrierCode, oSTC, autoImport, oIs3PL);
                                            }
                                            if (bValidShipper == true)
                                            {
                                                // 09-24-2022 end
                                                string shipCode = carrierCode;
                                                // 05-19-2021 begin
                                                if (oIs3PL == "Y")
                                                {
                                                    // 07-07-2022 begin
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Get ShipCode");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Get ShipCode");
                                                    }
                                                    // 07-20-220 begin
                                                    /*  if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType3PL) &&
                                                          edi850HeaderRecord.CardCode.StartsWith("BRAVO") && edi850HeaderRecord.CarrierCode == "FEDH" &&
                                                         (edi850HeaderRecord.OrderType3PL == "RDC" || edi850HeaderRecord.OrderType3PL == ("STORE")))
                                                      {
                                                          shipCode = "FDEG";
                                                      }
                                                      else
                                                      {*/
                                                    // 07-20-2022 end
                                                    // 07-07-2022 end
                                                    shipCode = get3PLSCAC(edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CarrierCode, edi850HeaderRecord.ServiceLevel, autoImport);
                                                    // 07-20-2022 begin
                                                    //}
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType3PL)
                                                        && edi850HeaderRecord.CardCode.StartsWith("BRAVO") && edi850HeaderRecord.CarrierCode == "FEDH"
                                                        && (edi850HeaderRecord.OrderType3PL == "RDC" || edi850HeaderRecord.OrderType3PL == ("STORE")))
                                                    {
                                                        shipCode = "FDEG";
                                                    }
                                                    // 07-20-2022 end
                                                }
                                                else
                                                {
                                                    // 05-19-2021 end
                                                    bool bUseServLev = checkServLev(edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CardCode, autoImport);

                                                    if (bUseServLev == true)
                                                    {
                                                        shipCode = shipCode.Trim() + "_" + serviceLevel.Trim();
                                                    }
                                                } // 05-19-2021
                                                // 06-07-2021 begin
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Ship Code => " + shipCode);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Ship Code => " + shipCode);
                                                }
                                                // 06-07-2021 end
                                                // 07-07-2022 begin
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Set Carrier UDF");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Set Carrier UDF");
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType3PL) &&
                                                    edi850HeaderRecord.CardCode.StartsWith("BRAVO") && edi850HeaderRecord.CarrierCode == "FEDH" &&
                                                   (edi850HeaderRecord.OrderType3PL == "RDC" || edi850HeaderRecord.OrderType3PL == ("STORE")))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2Cc").Value = edi850HeaderRecord.CarrierCode;
                                                }
                                                else
                                                {
                                                    // 07-07-2022 end
                                                    document.UserFields.Fields.Item("U_InfoW2Cc").Value = shipCode; // 03-04-2022
                                                } // 07-07-2022
                                                // 10-11-2019 end
                                                if (edi850HeaderRecord.SBOCardCode == null || edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                                                {  // 01-17-2018
                                                    document.CardCode = edi850HeaderRecord.CardCode;
                                                    // 01-17-2018 begin
                                                }
                                                else
                                                {
                                                    document.CardCode = edi850HeaderRecord.SBOCardCode;
                                                } // 01-17-2018 end
                                                document.UserFields.Fields.Item("U_InfoOrdStatus").Value = "CC"; // 05-15-2020
                                                document.UserFields.Fields.Item("U_Info850HdrId").Value = edi850HeaderRecord.HeaderId; // 05-24-2022
                                                try
                                                {
                                                    document.UserFields.Fields.Item("U_COR_ImpSrc").Value = "850"; // 08-04-2023
                                                }
                                                catch (Exception c)
                                                {
                                                    string oErrMsg = c.Message;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error setting U_COR_ImpSrc: " + oErrMsg);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Error setting U_COR_ImpSrc: " + oErrMsg);
                                                    }
                                                }
                                                // 03-24-2026 lrussell begin
                                                // udf U_COR_SrcDoc not in database
                                                /*
                                                    // 09-08-2023 begin
                                                    try
                                                    {
                                                        document.UserFields.Fields.Item("U_COR_SrcDoc").Value = "850";
                                                    }
                                                    catch (Exception c)
                                                    {
                                                        string oErrMsg = c.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error setting U_COR_SrcDoc: " + oErrMsg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Error setting U_COR_SrcDoc: " + oErrMsg);
                                                        }
                                                    }
                                                    // 09-08-2023 end
                                                */
                                                // 03-24-2026 lrussell end
                                                    document.NumAtCard = edi850HeaderRecord.PurchaseOrderReference;
                                                    // 02-29-2024 begin
                                                    if (edi850HeaderRecord.PurchaseOrderDate != null && edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length > 0)
                                                    {
                                                        try
                                                        {
                                                            document.UserFields.Fields.Item("U_C3_PoDate").Value = edi850HeaderRecord.PurchaseOrderDate;
                                                        }
                                                        catch (Exception pdt)
                                                        {
                                                            string oErrMsg = pdt.Message;
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("Error setting PO Date: " + oErrMsg);
                                                            }
                                                            else
                                                            {
                                                                _logger.Debug("Error setting PO Date: " + oErrMsg);
                                                            }
                                                        }
                                                    }
                                                    // 02-29-2024 end
                                                    // 03-02-2023 begin
                                                    if (edi850HeaderRecord.CardCode.StartsWith("TeeZed") && edi850HeaderRecord.ExpectedDeliveryDate.HasValue && edi850HeaderRecord.ExpectedDeliveryDate.Value > DateTime.MinValue)
                                                    {
                                                        document.DocDueDate = edi850HeaderRecord.ExpectedDeliveryDate.Value;
                                                    }
                                                    else
                                                        // 03-02-2023 end
                                                        if (edi850HeaderRecord.RequestedShipDate.HasValue && edi850HeaderRecord.RequestedShipDate.Value > DateTime.MinValue)
                                                        {
                                                            document.DocDueDate = edi850HeaderRecord.RequestedShipDate.Value;

                                                        }
                                                        else if (edi850HeaderRecord.RequestedDeliveryDate.HasValue && edi850HeaderRecord.RequestedDeliveryDate.Value > DateTime.MinValue)
                                                        {
                                                            document.DocDueDate = edi850HeaderRecord.RequestedDeliveryDate.Value;
                                                        }
                                                        else
                                                        {
                                                            document.DocDueDate = DateTime.Today;
                                                        }

                                                    document.DocDate = DateTime.Today;
                                                    document.TaxDate = DateTime.Today;
                                                    // 12-14-2022 remove processing of expected delivery date field per Dan@Corsan
                                                    /*
                                                      // 06-01-2021 begin
                                                      if (oIs3PL == "Y")
                                                      {
                                                          if (autoImport)
                                                          {
                                                              Import_Log.LogEntry("3PL delivery date check");
                                                          }
                                                          else
                                                          {
                                                              _logger.Debug("3PL  delivery date check");
                                                          }
                                                          DateTime oToday = DateTime.Now;
                                                          oToday = oToday.AddDays(Convert.ToDouble("-1"));
                                                          if (edi850HeaderRecord.ExpectedDeliveryDate.HasValue && edi850HeaderRecord.ExpectedDeliveryDate.Value > DateTime.MinValue
                                                              && edi850HeaderRecord.ExpectedDeliveryDate.Value > oToday)
                                                          {
                                                              document.DocDueDate = edi850HeaderRecord.ExpectedDeliveryDate.Value;
                                                          }
                                                          else
                                                          {
                                                              if (autoImport)
                                                              {
                                                                  Import_Log.LogEntry("3PL delivery date prior to current date; setting delivery date to current date");
                                                              }
                                                              else
                                                              {
                                                                  _logger.Debug("3PL delivery date prior to current date; setting delivery date to current date");
                                                              }
                                                          }
                                                      }
                                                      // 06-01-2021 end
                                                      */
                                                // 09-16-2021 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BusinessRuleCd))
                                                {
                                                    try
                                                    {
                                                        string oBusinessRule = edi850HeaderRecord.BusinessRuleCd.Trim();
                                                        if (oBusinessRule.Length > 30)
                                                        {
                                                            oBusinessRule = oBusinessRule.Substring(0, 30);
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoBRuleCd").Value = oBusinessRule;
                                                    }
                                                    catch (Exception br)
                                                    {
                                                        string oErrMessage = br.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Setting Business Rule Cd => " + oErrMessage);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Setting Business Rule Cd => " + oErrMessage);
                                                        }
                                                    }
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipmentCd))
                                                {
                                                    try
                                                    {
                                                        string oShipCd = edi850HeaderRecord.ShipmentCd.Trim();
                                                        if (oShipCd.Length > 30)
                                                        {
                                                            oShipCd = oShipCd.Substring(0, 30);
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoShipmentCd").Value = oShipCd;
                                                    }
                                                    catch (Exception sc)
                                                    {
                                                        string oErrMessage = sc.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Setting Shipment Cd => " + oErrMessage);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Setting Shipment Cd => " + oErrMessage);
                                                        }
                                                    }
                                                }
                                                // 09-16-2021 end
                                                String oShipType = ""; // 03-04-2022
                                                // 08-10-2021 begin
                                                if (oIs3PL == "Y")
                                                {
                                                    String[] oShprData = new String[2]; // 03-04-2022
                                                    // 02-12-2022 begin
                                                    try
                                                    {
                                                        if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.Shipper3PL))
                                                        {
                                                            string oShipper = edi850HeaderRecord.Shipper3PL.Trim();
                                                            if (oShipper.Length > 10)
                                                            {
                                                                oShipper = oShipper.Substring(0, 10);
                                                            }
                                                        }
                                                        oShprData = getC3Shipper(edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CarrierCode, oSTC, autoImport, oIs3PL);
                                                        if (!String.IsNullOrWhiteSpace(oShprData[0]) && String.IsNullOrWhiteSpace(edi850HeaderRecord.Shipper3PL))
                                                        {
                                                            document.UserFields.Fields.Item("U_C3_Shipper").Value = oShprData[0];
                                                        }
                                                        // 03-01-2022 end
                                                        // 03-04-2022 begin
                                                        if (!String.IsNullOrWhiteSpace(oShprData[1]))
                                                        {
                                                            document.UserFields.Fields.Item("U_C3_ShpprAcct").Value = oShprData[1];
                                                        }
                                                        if (!String.IsNullOrWhiteSpace(oShprData[2]))
                                                        {
                                                            oShipType = oShprData[2];
                                                        }
                                                        // 03-04-2022 end
                                                        //}
                                                    }
                                                    catch (Exception ShipprAcc)
                                                    {
                                                        string oErrMessage = ShipprAcc.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("3PL error setting Shipper => " + oErrMessage);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("3PL error setting Shipper => " + oErrMessage);
                                                        }
                                                    }
                                                    // 03-10-2022 begin
                                                    if (!edi850HeaderRecord.CardCode.StartsWith("TeeZed")
                                                        && !String.IsNullOrWhiteSpace(edi850HeaderRecord.FreightBillType)
                                                        && edi850HeaderRecord.FreightBillType.ToUpper() == "THIRD PARTY")
                                                    {
                                                        // 03-10-2022 end
                                                        try
                                                        {
                                                            // 12-08-2022 begin 
                                                            bool isKohls = false;
                                                            if (edi850HeaderRecord.CardCode.StartsWith("INDOCOUNT")
                                                                && edi850HeaderRecord.CustCode3PL == "KHDO"
                                                                && edi850HeaderRecord.VendorNumber.Trim() == "078699963")
                                                            {
                                                                isKohls = true;
                                                            }
                                                            // 12-08-2022 end
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyAcct)
                                                                && isKohls == false) // 12-10-2022
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdPrtyAcct").Value = edi850HeaderRecord.ThirdPtyAcct.Trim();
                                                            }
                                                            // 03-01-2022 begin
                                                            else
                                                            {
                                                                // 03-04-2022 begin
                                                                if (String.IsNullOrWhiteSpace(oShipType))
                                                                {
                                                                    oShipType = edi850HeaderRecord.CarrierCode;
                                                                }
                                                                //String oThrdPtyAcct = getThirdPtyAcct(edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CarrierCode, edi850HeaderRecord.ServiceLevel, oSTC);
                                                                String oThrdPtyAcct = getThirdPtyAcct(edi850HeaderRecord.SBOCardCode, oShipType, oSTC, autoImport);
                                                                /* if (String.IsNullOrWhiteSpace(oThrdPtyAcct))
                                                                 {
                                                                     oThrdPtyAcct = getThirdPtyAcct(edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CarrierCode, oSTC);
                                                                 }*/
                                                                // 03-04-2022 END
                                                                if (!String.IsNullOrWhiteSpace(oThrdPtyAcct))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdPrtyAcct").Value = oThrdPtyAcct;
                                                                }
                                                            }
                                                            // 03-01-2022 end
                                                        }
                                                        catch (Exception ThrdPtAcct)
                                                        {
                                                            string oErrMessage = ThrdPtAcct.Message;
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("3PL error setting Third Party account => " + oErrMessage);
                                                            }
                                                            else
                                                            {
                                                                _logger.Error("3PL error setting Third Party Account => " + oErrMessage);
                                                            }
                                                        }
                                                    } // 03-10-2022
                                                    try
                                                    {
                                                        if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderPriority3PL))
                                                        {
                                                            document.UserFields.Fields.Item("U_C3_OrdPriority").Value = edi850HeaderRecord.OrderPriority3PL;
                                                        }
                                                    }
                                                    catch (Exception OrdH)
                                                    {
                                                        string oErrMessage = OrdH.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("3PL error setting Order Handling  => " + oErrMessage);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("3PL error setting Order Handling => " + oErrMessage);
                                                        }
                                                    }
                                                    // 02-12-2022 end
                                                    // 03-07-02022 begin
                                                    // remove 3-3-22 changes
                                                    /*
                                                    // 03-03-2022 begin
                                                    if (oIs3PL == "Y" && !String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToName) && edi850HeaderRecord.BillToName.Trim().Length > 0)
                                                    {
                                                        try
                                                        {
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToName))
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Nme").Value = edi850HeaderRecord.BillToName;
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Nme").Value = "";
                                                            }
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToAddress1))
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Addrss").Value = edi850HeaderRecord.BillToAddress1;
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Addrss").Value = "";
                                                            }
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToCity))
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Cty").Value = edi850HeaderRecord.BillToCity;
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Cty").Value = "";
                                                            }
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToState))
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_St").Value = edi850HeaderRecord.BillToState;
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_St").Value = "";
                                                            }
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToZip))
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = edi850HeaderRecord.BillToZip;
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = "";
                                                            }
                                                            if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToCountry))
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = edi850HeaderRecord.BillToCountry;
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = "";
                                                            }
                                                        }
                                                        catch (Exception BT)
                                                        {
                                                            string oErrMessage = BT.Message;
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("3PL error setting Third Party Bill To => " + oErrMessage);
                                                            }
                                                            else
                                                            {
                                                                _logger.Error("3PL error setting Third Party Bill To => " + oErrMessage);
                                                            }
                                                        }
                                                    }
                                                    else
                                                    {
                                                    //03-03-2022 end
                                                     */
                                                    // 03-07-2022 end
                                                    // 03-10-2022 begin
                                                    if (!edi850HeaderRecord.CardCode.StartsWith("TeeZed")
                                                        && !String.IsNullOrWhiteSpace(edi850HeaderRecord.FreightBillType)
                                                        && edi850HeaderRecord.FreightBillType.ToUpper() == "THIRD PARTY")
                                                    {

                                                        // 03-10-2022 end
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Third Party");
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Processing 3PL THird Party");
                                                        }
                                                        // 05-20-2022 begin
                                                        String[] oThirdPtyBT = new String[6];
                                                        oThirdPtyBT = getThirdPtyBT(edi850HeaderRecord.SBOCardCode, oShipType, oSTC, autoImport);
                                                        // 05-20-2022 end
                                                        // 03-08-2022 begin
                                                        if (String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTName))
                                                        {
                                                            // 05-20-2022 begin
                                                            /*
                                                            String[] oThirdPtyBT = new String[6];
                                                            oThirdPtyBT = getThirdPtyBT(edi850HeaderRecord.SBOCardCode, oShipType, oSTC, autoImport);
                                                             */
                                                            // 05-20-2022 end
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("3PL Third Party BT Name not sent");
                                                            }
                                                            else
                                                            {
                                                                _logger.Error("3PL THird Party BT Name not sent");
                                                            }
                                                            try
                                                            {
                                                                if (!String.IsNullOrWhiteSpace(oThirdPtyBT[0]))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Nme").Value = oThirdPtyBT[0];
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Nme").Value = "";
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(oThirdPtyBT[1]))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Addrss").Value = oThirdPtyBT[1];
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Addrss").Value = "";
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(oThirdPtyBT[2]))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Cty").Value = oThirdPtyBT[2];
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Cty").Value = "";
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(oThirdPtyBT[3]))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_St").Value = oThirdPtyBT[3];
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_St").Value = "";
                                                                }
                                                            } // 04-24-2022 begin
                                                            catch (Exception BT2)
                                                            {
                                                                String oErrMsg = "Error setting Third Party Bill To from [@C3_STCSA]  =>" + BT2.Message;
                                                                if (autoImport)
                                                                {
                                                                    Import_Log.LogEntry(oErrMsg);
                                                                }
                                                                else
                                                                {
                                                                    _logger.Error(oErrMsg);
                                                                }
                                                            }
                                                            // 04-22-2022 end
                                                            try
                                                            { // 04-22-2022
                                                                if (!String.IsNullOrWhiteSpace(oThirdPtyBT[4]))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = oThirdPtyBT[4];
                                                                    if (autoImport)
                                                                    {
                                                                        Import_Log.LogEntry("3PL Third Party Zip " + oThirdPtyBT[4]);
                                                                    }
                                                                    else
                                                                    {
                                                                        Import_Log.LogEntry("3PL Third Party Zip " + oThirdPtyBT[4]);
                                                                    }
                                                                }
                                                                // 06-09-2022 begin
                                                                else if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTZip))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = edi850HeaderRecord.ThirdPtyBTZip;
                                                                    if (autoImport)
                                                                    {
                                                                        Import_Log.LogEntry("3PL Third Party Zip Sent was " + edi850HeaderRecord.ThirdPtyBTZip);
                                                                    }
                                                                    else
                                                                    {
                                                                        Import_Log.LogEntry("3PL Third Party Zip  Sent was" + edi850HeaderRecord.ThirdPtyBTZip);
                                                                    }
                                                                }
                                                                // 06-09-2022 end
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = "";
                                                                    if (autoImport)
                                                                    {
                                                                        Import_Log.LogEntry("3PL Third Party Zip not found");
                                                                    }
                                                                    else
                                                                    {
                                                                        Import_Log.LogEntry("3PL Third Party Zip  not found");
                                                                    }
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(oThirdPtyBT[5]))
                                                                {
                                                                    if (oThirdPtyBT[5].ToUpper().Equals("USA"))
                                                                    {
                                                                        document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = "US";
                                                                    }
                                                                    else
                                                                    {
                                                                        document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = oThirdPtyBT[5];
                                                                    }
                                                                }
                                                                // 06-09-2022 begin
                                                                else if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTCountry))
                                                                {
                                                                    if (edi850HeaderRecord.ThirdPtyBTCountry.Trim().ToUpper().Equals("USA"))
                                                                    {
                                                                        document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = "US";
                                                                    }
                                                                    else
                                                                    {
                                                                        document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = edi850HeaderRecord.ThirdPtyBTCountry;
                                                                    }
                                                                }
                                                                // 06-09-2022 end
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = "US";
                                                                }
                                                            }
                                                            catch (Exception BT2)
                                                            {
                                                                String oErrMsg = "Error setting Third Party Bill To from [@C3_STCSA]  =>" + BT2.Message;
                                                                if (autoImport)
                                                                {
                                                                    Import_Log.LogEntry(oErrMsg);
                                                                }
                                                                else
                                                                {
                                                                    _logger.Error(oErrMsg);
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // 03-08-2022 end
                                                            try
                                                            {
                                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTName))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Nme").Value = edi850HeaderRecord.ThirdPtyBTName.Trim();
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Nme").Value = "";
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTAddr))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Addrss").Value = edi850HeaderRecord.ThirdPtyBTAddr;
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Addrss").Value = "";
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTCity))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Cty").Value = edi850HeaderRecord.ThirdPtyBTCity;
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Cty").Value = "";
                                                                }
                                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTState))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_St").Value = edi850HeaderRecord.ThirdPtyBTState;
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_St").Value = "";
                                                                }
                                                                // 05-20-2022 begin
                                                                if (oThirdPtyBT != null && oThirdPtyBT.Length >= 5 && !String.IsNullOrWhiteSpace(oThirdPtyBT[4]))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = oThirdPtyBT[4];
                                                                }
                                                                // 05-20-2022 end
                                                                else if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTZip))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = edi850HeaderRecord.ThirdPtyBTZip;
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Zip").Value = "";
                                                                }
                                                                // 05-20-2022 begin
                                                                if (oThirdPtyBT != null && oThirdPtyBT.Length >= 6 && !String.IsNullOrWhiteSpace(oThirdPtyBT[5]))
                                                                {
                                                                    if (oThirdPtyBT[5].ToUpper().Equals("USA"))
                                                                    {
                                                                        document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = "US";
                                                                    }
                                                                    else
                                                                    {
                                                                        document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = oThirdPtyBT[5];
                                                                    }
                                                                }
                                                                // 05-20-0222 end
                                                                else if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ThirdPtyBTCountry))
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = edi850HeaderRecord.ThirdPtyBTCountry;
                                                                }
                                                                else
                                                                {
                                                                    document.UserFields.Fields.Item("U_C3_ThrdBT_Country").Value = "";
                                                                }
                                                            }
                                                            catch (Exception BT)
                                                            {
                                                                string oErrMessage = BT.Message;
                                                                if (autoImport)
                                                                {
                                                                    Import_Log.LogEntry("3PL error setting Third Party Bill To => " + oErrMessage);
                                                                }
                                                                else
                                                                {
                                                                    _logger.Error("3PL error setting Third Party Bill To => " + oErrMessage);
                                                                }
                                                            }
                                                            // } // 03-03-2022
                                                        } // 03-08-2022
                                                    } // 03-10-2022
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToAddress1))
                                                {
                                                    document.AddressExtension.ShipToStreet = edi850HeaderRecord.ShipToAddress1;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToAddress2))
                                                {
                                                    document.AddressExtension.ShipToBlock = edi850HeaderRecord.ShipToAddress2;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToCity))
                                                {
                                                    document.AddressExtension.ShipToCity = edi850HeaderRecord.ShipToCity;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToState))
                                                {
                                                    try // 08-18-2021
                                                    {
                                                        document.AddressExtension.ShipToState = edi850HeaderRecord.ShipToState;
                                                        // 08-18-2021 begin
                                                    }
                                                    catch (Exception eS)
                                                    {
                                                        string oErrMsg = "3PL error setting ship to state =>" + eS.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(oErrMsg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error(oErrMsg);
                                                        }
                                                        set850Error(oErrMsg, edi850HeaderRecord.HeaderId, autoImport, false);
                                                    }
                                                    // 08-18-2021 end
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToZip))
                                                {
                                                    document.AddressExtension.ShipToZipCode = edi850HeaderRecord.ShipToZip;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToCountry))
                                                {
                                                    try
                                                    {
                                                        if (edi850HeaderRecord.ShipToCountry.Equals("USA", StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            document.AddressExtension.ShipToCountry = "US";
                                                        }
                                                        else if (edi850HeaderRecord.ShipToCountry.Equals("CAN", StringComparison.InvariantCultureIgnoreCase))
                                                        {
                                                            document.AddressExtension.ShipToCountry = "CA";
                                                        }
                                                        else
                                                        {
                                                            document.AddressExtension.ShipToCountry = edi850HeaderRecord.ShipToCountry;
                                                        }
                                                    }
                                                    catch (Exception ctry)
                                                    {
                                                        string oErrMsg = "3PL error setting ship to country =>" + ctry.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(oErrMsg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error(oErrMsg);
                                                        }
                                                        set850Error(oErrMsg, edi850HeaderRecord.HeaderId, autoImport, false);
                                                    }
                                                    // 08-18-2021 end
                                                }
                                                // 07-07-2022 begin
                                                else
                                                {
                                                    document.AddressExtension.ShipToCountry = "US";
                                                }
                                                // 07-07-2022 end

                                                // 03-06-2020 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShippingAccount))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2ShipAcct").Value = edi850HeaderRecord.ShippingAccount;
                                                }
                                                else
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2ShipAcct").Value = "";
                                                }
                                                // 03-06-2020 end
                                                // 08-26-2019 begin 
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillingText) && String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined08))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing set Notes from Billing Text if UDF#8 is blank");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing set Notes from Billing Text if UDF#8 is blank");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2Notes").Value = edi850HeaderRecord.BillingText;
                                                }
                                                // 03-29-2022 begin
                                                if (!edi850HeaderRecord.CardCode.StartsWith("INDOCOUNT"))
                                                {  // 03-29-2022 end
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToEmail) && String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined09))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing set Email from BillTo if UDF#9 is blank");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing set Email from Bill To Email if UDF#9 is blank");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoW2Email").Value = edi850HeaderRecord.BillToEmail;
                                                    }
                                                } // 03-29-2022
                                                // 03-29-2022 begin
                                                if (!edi850HeaderRecord.CardCode.StartsWith("INDOCOUNT"))
                                                {  // 03-29-2022 end
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToName))
                                                    {
                                                        document.AddressExtension.BillToStreet = edi850HeaderRecord.BillToName;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToAddress1))
                                                    {
                                                        document.AddressExtension.BillToBlock = edi850HeaderRecord.BillToAddress1;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToAddress2))
                                                    {
                                                        document.AddressExtension.BillToAddress2 = edi850HeaderRecord.BillToAddress2;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToCity))
                                                    {
                                                        document.AddressExtension.BillToCity = edi850HeaderRecord.BillToCity;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToState))
                                                    {
                                                        try
                                                        {  // 08-28-2021
                                                            document.AddressExtension.BillToState = edi850HeaderRecord.BillToState;
                                                            // 08-18-2021 begin
                                                        }
                                                        catch (Exception bS)
                                                        {
                                                            String oErrMsg = "Error setting bill to state code => " + bS.Message;
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry(oErrMsg);
                                                            }
                                                            else
                                                            {
                                                                _logger.Error(oErrMsg);
                                                            }
                                                            set850Error(oErrMsg, edi850HeaderRecord.HeaderId, autoImport, false);
                                                        }
                                                        // 08-18-2021 end
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToZip))
                                                    {
                                                        document.AddressExtension.BillToZipCode = edi850HeaderRecord.BillToZip;
                                                    }
                                                    // 05-13-2021 begin
                                                    // reformat country code
                                                    /*
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToCountry))
                                                    {
                                                        document.AddressExtension.BillToCountry = edi850HeaderRecord.BillToCountry;
                                                    }*/
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BillToCountry))
                                                    {
                                                        try
                                                        {
                                                            if (edi850HeaderRecord.BillToCountry.Equals("USA", StringComparison.InvariantCultureIgnoreCase))
                                                            {
                                                                document.AddressExtension.BillToCountry = "US";
                                                            }
                                                            else if (edi850HeaderRecord.BillToCountry.Equals("CAN", StringComparison.InvariantCultureIgnoreCase))
                                                            {
                                                                document.AddressExtension.BillToCountry = "CA";
                                                            }
                                                            else
                                                            {
                                                                document.AddressExtension.BillToCountry = edi850HeaderRecord.BillToCountry;
                                                            }
                                                            // 08-18-2021 begin
                                                        }
                                                        catch (Exception Ctry)
                                                        {
                                                            String oErrMsg = "Error setting bill to country code => " + Ctry.Message;
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry(oErrMsg);
                                                            }
                                                            else
                                                            {
                                                                _logger.Error(oErrMsg);
                                                            }
                                                            set850Error(oErrMsg, edi850HeaderRecord.HeaderId, autoImport, false);
                                                        }
                                                        // 08-18-2021 end
                                                    }

                                                    // 05-13-2021 end
                                                } // 03-29-2022
                                                // 08-26-2019 end
                                                // 02-03-2019 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BOLNotes))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing BOL Notes");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing BOL Notes");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2BOLNotes").Value = edi850HeaderRecord.BOLNotes;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.PackingNotes))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Pack Notes");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Pack Notes");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2PackNote").Value = edi850HeaderRecord.PackingNotes;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.DeliveryContact))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Delivery Contact");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Delivery Contact");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2DelContact").Value = edi850HeaderRecord.DeliveryContact;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.DeliveryEmail))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Delivery Email Address");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Delivery Email Address");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2DelEmail").Value = edi850HeaderRecord.DeliveryEmail;
                                                }
                                                // 10-08-2019 begin
                                                string transportCode = " ";
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ServiceLevel)  // edi850HeaderRecord.ServiceLevel.Trim().Length < 4)
                                                   || oIs3PL == "Y") // 05-19-2021
                                                {
                                                    Recordset rsT = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Lookup Shipping Type by Carrier Code & Service Level");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Lookup Shipping Type by Carrier Code & Service Level");
                                                    }
                                                    //String oQry = String.Format(TransportQuery, edi850HeaderRecord.ServiceLevel);
                                                    String oQry = String.Format(TransportQuery, shipCode); // 03-05-2020
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Executing SQL: " + oQry);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Executing SQL: " + oQry);
                                                    }
                                                    try
                                                    {
                                                        rs.DoQuery(oQry);
                                                        rs.MoveFirst();
                                                        if (!rs.EoF)
                                                        {
                                                            transportCode = (String)rs.Fields.Item("TrnspCode").Value.ToString();
                                                        }
                                                    }
                                                    catch (Exception t)
                                                    {
                                                        string oErrMesg = "";
                                                        if (!String.IsNullOrWhiteSpace(shipCode))
                                                        {
                                                            oErrMesg = "Error getting transportation code for" + shipCode + " =>" + t.Message;
                                                        }
                                                        else
                                                        {
                                                            oErrMesg = "Error getting transportation code =>" + t.Message;
                                                        }
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(oErrMesg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug(oErrMesg);
                                                        }
                                                        transportCode = " ";
                                                    }
                                                    // 03-05-2020 begin
                                                    if (String.IsNullOrWhiteSpace(transportCode))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error getting transportation code for " + shipCode);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Error getting transportation code for " + shipCode);
                                                        }
                                                    }
                                                    // 03-05-2020 end
                                                }
                                                else // 03-05-2020
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CarrierCode) && String.IsNullOrWhiteSpace(transportCode))
                                                    {
                                                        Recordset rsT = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Lookup Shipping Type by Carrier Code");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Lookup Shipping Type by Carrier Code");
                                                        }
                                                        String oQry = String.Format(TransportQuery, edi850HeaderRecord.CarrierCode);
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Executing SQL: " + oQry);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Executing SQL: " + oQry);
                                                        }
                                                        try
                                                        {
                                                            rs.DoQuery(oQry);
                                                            rs.MoveFirst();
                                                            if (!rs.EoF)
                                                            {
                                                                transportCode = (String)rs.Fields.Item("TrnspCode").Value.ToString();
                                                            }
                                                        }
                                                        catch (Exception t)
                                                        {
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("Error getting transportation code =>" + t.Message);
                                                            }
                                                            else
                                                            {
                                                                _logger.Debug("Error getting transportation code =>" + t.Message);
                                                            }
                                                            transportCode = " ";
                                                        }
                                                    }
                                                if (!String.IsNullOrWhiteSpace(transportCode))
                                                {
                                                    try
                                                    {
                                                        document.TransportationCode = Convert.ToInt32(transportCode);
                                                    }
                                                    catch (Exception tc)
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error setting transportation code =>" + tc.Message);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Error setting transportation code =>" + tc.Message);
                                                        }
                                                    }
                                                }
                                                // 10-08-2019 end
                                                try
                                                {
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.TransportMethod))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing Transport Method");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing Transport Method");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoW2TMethod").Value = edi850HeaderRecord.TransportMethod;
                                                    }
                                                }
                                                catch (Exception tc2)
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Error setting transportation method =>" + tc2.Message);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Error setting transport method =>" + tc2.Message);
                                                    }
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ServiceLevel))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Service Level");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Service Level");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2ServiceLev").Value = edi850HeaderRecord.ServiceLevel;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.HandlingCode))
                                                {
                                                    _logger.Debug("Processing HandlingCode");
                                                    document.UserFields.Fields.Item("U_InfoW2HandlingCode").Value = edi850HeaderRecord.HandlingCode;
                                                }
                                                // 02-03-2019 end
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing Root UDFs");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Root UDFs");
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToName))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Ship To Name");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Ship To Name");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2ShNm").Value = edi850HeaderRecord.ShipToName;
                                                    //_logger.Debug("Processing Root UDF 1.1");
                                                    //document.AddressExtension.ShipToAddress2 = record.ShipToName;
                                                    //_logger.Debug("Processing Root UDF 1.2");
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToAttention))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Ship To Attention");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Ship To Attention");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2ShAt").Value = edi850HeaderRecord.ShipToAttention;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.PaymentMethod))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing PaymentMethod");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing PaymentMethod");
                                                    }// PP=Prepaid, TP=Third Party, etc.
                                                    document.UserFields.Fields.Item("U_InfoW2Mop").Value = edi850HeaderRecord.PaymentMethod;
                                                }
                                                // 03-04-2022 begin
                                                // remove code & user FrtBillType instead
                                                /*
                                                // 03-03-2022 begin
                                                if (!edi850HeaderRecord.CardCode.StartsWith("TeeZed") && oIs3PL == "Y")
                                                { // 03-03-2022 end
                                                    // 03-01-2022 begin
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.PaymentMethod))
                                                    {
                                                        String oPaymentType = edi850HeaderRecord.PaymentMethod;
                                                        if (!String.IsNullOrWhiteSpace(oPaymentType))
                                                        {
                                                            if (oPaymentType.Trim().ToUpper() == "PP" || oPaymentType.Trim().ToUpper() == "PREPAID" || oPaymentType.Trim().ToUpper() == "PRE-PAID")
                                                            {
                                                                oPaymentType = "Sender";
                                                            }
                                                            _logger.Debug("U_PaymentType = " + oPaymentType);
                                                            //document.UserFields.Fields.Item("U_PaymentType").Value = oPaymentType;
                                                            document.UserFields.Fields.Item("U_InfoW2Mop").Value = oPaymentType; // 03-03-2022
                                                        }
                                                    }  // 03-01-2022 
                                                }
                                                // 03-03-2022 end
                                                */
                                                // 03-04-2022 end
                                                // 05-19-2021 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CarrierCode) && oIs3PL == "Y" && !String.IsNullOrWhiteSpace(shipCode))
                                                {
                                                    document.UserFields.Fields.Item("U_C3_OrigShipType").Value = shipCode;
                                                }
                                                else // 05-19-2021 end
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CarrierCode))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing Carrier Code");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing Carrier Code");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoW2Cc").Value = edi850HeaderRecord.CarrierCode;
                                                    }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToLocationCode))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Ship To Location Code");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Ship To Location Code");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2Lc").Value = edi850HeaderRecord.ShipToLocationCode;
                                                }

                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BuyerName)) // trading partner contact
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Buyer Name");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Buyer Name");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2Cnt").Value = edi850HeaderRecord.BuyerName;
                                                }
                                                document.UserFields.Fields.Item("U_InfoW2753").Value = "N"; // 03-23-2021
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.DeliveryPhoneNumber))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Root Delivery Phone No.");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Root Delivery Phone No.");
                                                    }

                                                    edi850HeaderRecord.DeliveryPhoneNumber = edi850HeaderRecord.DeliveryPhoneNumber.Trim(); // 06-03-2020
                                                    document.UserFields.Fields.Item("U_InfoW2Cnn").Value = edi850HeaderRecord.DeliveryPhoneNumber; //??
                                                    // 06-03-2020 begin

                                                    string delNo = edi850HeaderRecord.DeliveryPhoneNumber;
                                                    delNo = delNo.Replace(" ", String.Empty);
                                                    try
                                                    {
                                                        Int32 oPhNo = 0;
                                                        bool result = Int32.TryParse(delNo, out oPhNo);
                                                        if (result == false)
                                                        {
                                                            delNo = System.Text.RegularExpressions.Regex.Replace(delNo, @"^[A-Za-z]+", "");
                                                            if (delNo.Contains("#"))
                                                            {
                                                                delNo = System.Text.RegularExpressions.Regex.Replace(delNo, "#", "");
                                                            }

                                                            if (delNo.Contains("("))
                                                            {
                                                                delNo = delNo.Replace('(', ' ');
                                                            }
                                                            if (delNo.Contains(")"))
                                                            {
                                                                delNo = delNo.Replace(')', ' ');
                                                            }
                                                            if (delNo.Contains("-"))
                                                            {
                                                                delNo = System.Text.RegularExpressions.Regex.Replace(delNo, "-", " ");
                                                            }
                                                            if (delNo.Contains(","))
                                                            {
                                                                delNo = delNo.Replace(",", String.Empty);
                                                            }
                                                            if (delNo.Contains("."))
                                                            {
                                                                delNo = delNo.Replace(".", String.Empty);
                                                            }
                                                            delNo = delNo.Replace(" ", String.Empty);
                                                        }
                                                        delNo = delNo.Replace(" ", String.Empty);
                                                    }
                                                    catch (Exception dp)
                                                    {
                                                        String oErrMessage = dp.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error formatting delivery phone number =>" + oErrMessage);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Error formatting delivery phone number =>" + oErrMessage);
                                                        }
                                                    }
                                                    delNo = delNo.Trim();
                                                    if (delNo.Length > 10)
                                                    {
                                                        delNo = delNo.Substring(0, 10);
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoDelPhone").Value = delNo;

                                                    /*
                                                     // 10-02-2019 begin
                                                     if (edi850HeaderRecord.DeliveryPhoneNumber.Trim().Length > 25)
                                                     {
                                                         document.UserFields.Fields.Item("U_InfoDelPhone").Value = edi850HeaderRecord.DeliveryPhoneNumber.Substring(0, 25);
                                                     }
                                                     else
                                                     { // 10-02-2019 end
                                                         document.UserFields.Fields.Item("U_InfoDelPhone").Value = edi850HeaderRecord.DeliveryPhoneNumber.Trim(); // 03-18-2018
                                                     } // 10-02-2019  
                                                     */
                                                    // 06-03-2020 end
                                                }
                                                // 05-18-2020 begin
                                                // populate delivery phone# with 000 000 0000 if none is sent per Dan
                                                // 06-11-2020 CHANGED to 0000000000 per Dan
                                                else
                                                {
                                                    document.UserFields.Fields.Item("U_InfoDelPhone").Value = "0000000000";
                                                }
                                                // 05-18-2020 end
                                                // 03-21-2019 begin
                                                document.UserFields.Fields.Item("U_InfoW2CnNo").Value = "0";
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ServiceLevel))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Service Level");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Service Level");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2ServiceLev").Value = edi850HeaderRecord.ServiceLevel;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.HandlingCode))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Handling Code");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Handling Code");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2HandlingCode").Value = edi850HeaderRecord.HandlingCode;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.PackingNotes))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Packing Notes");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Packing Notes");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2PackNote").Value = edi850HeaderRecord.PackingNotes;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.BOLNotes))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing BOL Notes");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing BOL Notes");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2BOLNotes").Value = edi850HeaderRecord.BOLNotes;
                                                }
                                                // 11-02-2016 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToStoreLocation))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Ship To Store Location");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Ship To Store Location");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoW2Sc").Value = edi850HeaderRecord.ShipToStoreLocation;
                                                }
                                                // 11-02-2016 end
                                                // 06-27-2017 begin Temporarily remove SOS mods from Bone
                                                ///*
                                                // 05-31-2017 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType))
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Processing Order Type");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Processing Order Type");
                                                    }
                                                    document.UserFields.Fields.Item("U_InfoOrdType").Value = edi850HeaderRecord.OrderType;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.TrxPurpose))
                                                {
                                                    _logger.Debug("Processing Transaction Purpose");
                                                    document.UserFields.Fields.Item("U_InfoTrxPurpose").Value = edi850HeaderRecord.TrxPurpose;
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing Set Transaction Purpose equal '00'");
                                                    document.UserFields.Fields.Item("U_InfoTrxPurpose").Value = "00";
                                                }
                                                // 05-31-2017 end
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyName))
                                                {
                                                    _logger.Debug("Processing Order Buy Name");
                                                    document.UserFields.Fields.Item("U_InfoW2BName").Value = edi850HeaderRecord.OrderBuyName;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyAddr1))
                                                {
                                                    _logger.Debug("Processing Order Buy Address Ln 1");
                                                    document.UserFields.Fields.Item("U_InfoW2BAd1").Value = edi850HeaderRecord.OrderBuyAddr1;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyAddr2))
                                                {
                                                    _logger.Debug("Processing Order Buy Address Ln 2");
                                                    document.UserFields.Fields.Item("U_InfoW2BAd2").Value = edi850HeaderRecord.OrderBuyAddr2;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyCity))
                                                {
                                                    _logger.Debug("Processing Order Buy City");
                                                    document.UserFields.Fields.Item("U_InfoW2BCity").Value = edi850HeaderRecord.OrderBuyCity;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyState))
                                                {
                                                    _logger.Debug("Processing Order Buy State");
                                                    document.UserFields.Fields.Item("U_InfoW2BState").Value = edi850HeaderRecord.OrderBuyState;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyZip))
                                                {
                                                    _logger.Debug("Processing Order Buy Zip");
                                                    document.UserFields.Fields.Item("U_InfoW2BZip").Value = edi850HeaderRecord.OrderBuyZip;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyCountryCd))
                                                {
                                                    _logger.Debug("Processing Order Buy Country Code");
                                                    if (edi850HeaderRecord.OrderBuyCountryCd.Equals("USA", StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2BCntry").Value = "US";
                                                    }
                                                    else if (edi850HeaderRecord.OrderBuyCountryCd.Equals("CAN", StringComparison.InvariantCultureIgnoreCase))
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2BCntry").Value = "CA";
                                                    }
                                                    else
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2BCntry").Value = edi850HeaderRecord.OrderBuyCountryCd;
                                                    }
                                                }
                                                // 07-07-2022 begin
                                                else
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2BCntry").Value = "US";
                                                }
                                                // 07-07-2022 end


                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.JobNumber))
                                                {
                                                    _logger.Debug("Processing Job Number");
                                                    document.UserFields.Fields.Item("U_InfoW2Job").Value = edi850HeaderRecord.JobNumber;
                                                }
                                                //                    */
                                                // 06-24-2017 end
                                                // 07-17-2017  begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyCode))
                                                {
                                                    _logger.Debug("Processing Order Buy Code");
                                                    document.UserFields.Fields.Item("U_InfoW2BCode").Value = edi850HeaderRecord.OrderBuyCode;
                                                }
                                                // 07-17-2017 end
                                                // 07-19-2017 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderBuyName))
                                                {
                                                    _logger.Debug("Processing Set Order Buy Code equal Order Buy Name");
                                                    document.UserFields.Fields.Item("U_InfoW2BCode").Value = edi850HeaderRecord.OrderBuyName;
                                                }
                                                _logger.Debug("Processing Email Address from 850 UDF#9");
                                                // 07-19-2017 end
                                                // 03-18-2018 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined09))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2Email").Value = edi850HeaderRecord.UserDefined09;
                                                }
                                                // 03-18-2018 end
                                                // 08-15-2017 begin
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined03))
                                                {
                                                    _logger.Debug("Processing Terms Code from 850 UDF#3");
                                                    document.UserFields.Fields.Item("U_InfoW2TCd").Value = edi850HeaderRecord.UserDefined03;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined04))
                                                {
                                                    _logger.Debug("Processing Terms Discount from 850 UDF#4");
                                                    document.UserFields.Fields.Item("U_InfoW2TDisc").Value = edi850HeaderRecord.UserDefined04;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined05))
                                                {
                                                    _logger.Debug("Processing Terms Discount Days from 850 UDF#5");
                                                    document.UserFields.Fields.Item("U_InfoW2TDiscDays").Value = edi850HeaderRecord.UserDefined05;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined06))
                                                {
                                                    _logger.Debug("Processing Terms Days from 850 UDF#6");
                                                    document.UserFields.Fields.Item("U_InfoW2TDays").Value = edi850HeaderRecord.UserDefined06;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined07))
                                                {
                                                    _logger.Debug("Processing Terms Description from 850 UDF#7");
                                                    document.UserFields.Fields.Item("U_InfoW2TDesc").Value = edi850HeaderRecord.UserDefined07;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined08))
                                                {
                                                    _logger.Debug("Processing Notes from 850 UDF#8");
                                                    document.UserFields.Fields.Item("U_InfoW2Notes").Value = edi850HeaderRecord.UserDefined08;
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.UserDefined01) && edi850HeaderRecord.UserDefined01.Trim().Length > 0) // added length check
                                                {
                                                    _logger.Debug("Processing Product Description (Department) from 850 UDF#1");
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = edi850HeaderRecord.UserDefined01;
                                                    document.UserFields.Fields.Item("U_C3_Department").Value = edi850HeaderRecord.UserDefined01; // 09-15-2023
                                                }
                                                // 08-15-2017 end

                                                // 08-13-2019 begin
                                                if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                                {
                                                    try
                                                    {
                                                        if (String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToLocationCode))
                                                        {
                                                            document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                        }
                                                        else
                                                        {
                                                            document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2S";
                                                        }
                                                    }
                                                    catch (Exception h)
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(h.Message);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug(h.Message);
                                                        }
                                                    }
                                                }
                                                // 08-13-2019 end

                                                // 10-27-2019 begin
                                                if (edi850HeaderRecord.CardCode.StartsWith("Wayfair")
                                                    || edi850HeaderRecord.CardCode.StartsWith("TSC")) // 03-01-2023
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.VendorNote1))
                                                {
                                                    try
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2VNote1").Value = edi850HeaderRecord.VendorNote1;
                                                    }
                                                    catch (Exception h)
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error setting Vendor Note #1 =>" + h.Message);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Error setting Vendor Note #1 =>" + h.Message);
                                                        }
                                                    }
                                                }
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.VendorNote2))
                                                {
                                                    try
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2VNote2").Value = edi850HeaderRecord.VendorNote2;
                                                    }
                                                    catch (Exception h)
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error setting Vendor Note #2 =>" + h.Message);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Error setting Vendor Note #2 =>" + h.Message);
                                                        }
                                                    }
                                                }
                                                // 10-27-2019 end

                                                // 03-25-2019 begin 
                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ContractNumber))
                                                {
                                                    _logger.Debug("Processing Contract Number");
                                                    document.UserFields.Fields.Item("U_InfoW2Contract").Value = edi850HeaderRecord.ContractNumber;
                                                }

                                                if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CustomerOrderNumber))
                                                {
                                                    _logger.Debug("Processing Customer Order Number");
                                                    document.UserFields.Fields.Item("U_InfoW2CustOrdNo").Value = edi850HeaderRecord.CustomerOrderNumber;
                                                    // 06-29-2019 begin
                                                    if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2ShAt").Value = edi850HeaderRecord.CustomerOrderNumber;
                                                    }
                                                    // 06-29-2019 end
                                                }
                                                // 04-14-2022 begin
                                                try
                                                {
                                                    DateTime recDateTime = (DateTime)edi850HeaderRecord.ReceivedDateTime;
                                                    String oFmtRecDateTime = recDateTime.ToString("MM/dd/yyyy HH:mm:ss");
                                                    document.UserFields.Fields.Item("U_InfoW2DateRecd").Value = oFmtRecDateTime;
                                                }
                                                catch (Exception rdt)
                                                {
                                                    _logger.Error("Failed to set U_InfoW2DateRecd =>" + rdt.Message);
                                                }
                                                // 04-14-2022 end
                                                if (!(edi850HeaderRecord.PriorityShippingFee == null))
                                                {
                                                    _logger.Debug("Processing Priority Shipping Fee");
                                                    double shipFee = Convert.ToDouble("0");
                                                    try
                                                    {
                                                        shipFee = Convert.ToDouble(edi850HeaderRecord.PriorityShippingFee);
                                                    }
                                                    catch (Exception ps)
                                                    {
                                                        string oErr = ps.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error processing Priorty Shipping Fee: " + oErr);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Error processing Priorty Shipping Fee: " + oErr);
                                                        }
                                                        shipFee = Convert.ToDouble(0);
                                                    }
                                                }
                                                // 03-25-2019 end
                                                // 06-02-2020 begin
                                                if (edi850HeaderRecord.CardCode.StartsWith("Wayfair")
                                                    || edi850HeaderRecord.CardCode.Equals("WAYFAIR")
                                                    || edi850HeaderRecord.CardCode.Equals("Wayfair"))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                }
                                                // 06-02-2020 end
                                                // 03-01-2023 begin
                                                if (edi850HeaderRecord.CardCode.StartsWith("TSC"))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                }
                                                // 03-01-2023 end
                                                // 05-12-2021 begin
                                                if (oIs3PL == "Y")
                                                {
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Begin updating 3PL udfs");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Begin updating 3PL udfs");
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CustCode3PL))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Customer Code");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing 3PL Customer Code");
                                                        }
                                                        // 09-27-2022 begin
                                                        if (edi850HeaderRecord.CardCode.StartsWith("INDOCOUNT"))
                                                        //&& edi850HeaderRecord.CustCode3PL == "KHDO" && edi850HeaderRecord.VendorNumber == "078699963")
                                                        {
                                                            if (edi850HeaderRecord.CustCode3PL == "KHDO")
                                                            {
                                                                if (edi850HeaderRecord.VendorNumber.Trim() == "078699963")
                                                                {
                                                                    document.UserFields.Fields.Item("U_Info3PLCustCode").Value = "KHDO2";
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            // 09-27-2022 end
                                                            document.UserFields.Fields.Item("U_Info3PLCustCode").Value = edi850HeaderRecord.CustCode3PL;
                                                        } // 09-27-2022
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.CustomerRef))
                                                    {
                                                        _logger.Debug("Processing 3PL Customer Reference");
                                                        document.UserFields.Fields.Item("U_Info3PLCustRef").Value = edi850HeaderRecord.CustomerRef;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType3PL))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Order Type");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing 3PL Order Type");
                                                        }
                                                        document.UserFields.Fields.Item("U_Info3PLOrdType").Value = edi850HeaderRecord.OrderType3PL;
                                                        document.UserFields.Fields.Item("U_C3_OrigShipType").Value = edi850HeaderRecord.OrderType3PL;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ConsumerPO))
                                                    {

                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Consumer PO");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing 3PL Consumer PO");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoConsumerPO").Value = edi850HeaderRecord.ConsumerPO;
                                                    }
                                                    if (edi850HeaderRecord.ConsumerPODate.HasValue && edi850HeaderRecord.ConsumerPODate.Value > DateTime.MinValue)
                                                    {

                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Consumer PO Date");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing 3PL Consumer PO Date");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoConsumerPODate").Value = edi850HeaderRecord.ConsumerPODate.Value;
                                                    }
                                                    if (oIs3PL == "Y")
                                                    {
                                                        document.UserFields.Fields.Item("U_C3_FrghtCstReq").Value = "N";
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.FreightBillType))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Freight Bill Type");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing 3PL Freight Bill Type");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoFrtBillType").Value = edi850HeaderRecord.FreightBillType;
                                                        if (oIs3PL == "Y" && edi850HeaderRecord.CardCode.StartsWith("TeeZed"))
                                                        {
                                                            if (edi850HeaderRecord.FreightBillType.ToUpper() == "TZ ACCOUNT")
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_FrghtCstReq").Value = "Y";
                                                            }
                                                            else
                                                            {
                                                                document.UserFields.Fields.Item("U_C3_FrghtCstReq").Value = "N";
                                                            }
                                                        }
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipFromStore))
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Processing 3PL Ship From Store");
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Processing 3PL Ship From Store");
                                                        }
                                                        document.UserFields.Fields.Item("U_InfoShipFromStore").Value = edi850HeaderRecord.ShipFromStore;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipFromName))
                                                    {
                                                        _logger.Debug("Processing 3PL Ship From Name");
                                                        document.UserFields.Fields.Item("U_InfoShipFromName").Value = edi850HeaderRecord.ShipFromName;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.POPurposeCode))
                                                    {
                                                        _logger.Debug("Processing 3PL PO Purpose");
                                                        document.UserFields.Fields.Item("U_InfoPurposeCode").Value = edi850HeaderRecord.POPurposeCode;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.POType3PL))
                                                    {
                                                        _logger.Debug("Processing 3PL PO Type");
                                                        document.UserFields.Fields.Item("U_Info3PLPOType").Value = edi850HeaderRecord.POType3PL;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.PackingStoreCode))
                                                    {
                                                        _logger.Debug("Processing 3PL Packing Store Code");
                                                        document.UserFields.Fields.Item("U_InfoPackStoreCd").Value = edi850HeaderRecord.PackingStoreCode;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ReceiptId))
                                                    {
                                                        _logger.Debug("Processing 3PL Receipt Id");
                                                        document.UserFields.Fields.Item("U_InfoReceiptId").Value = edi850HeaderRecord.ReceiptId;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.VendorMessage))
                                                    {
                                                        _logger.Debug("Processing 3PL Vendor Message");
                                                        document.UserFields.Fields.Item("U_InfoVendorMessage").Value = edi850HeaderRecord.VendorMessage;
                                                    }

                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderMessage) && edi850HeaderRecord.OrderMessage.Trim().Length > 0)
                                                    {
                                                        _logger.Debug("Processing 3PL Order Message");
                                                        try
                                                        {
                                                            string orderMessage = edi850HeaderRecord.OrderMessage.ToString().Trim();
                                                            document.UserFields.Fields.Item("U_InfoOrderMessage").Value = Convert.ToString(orderMessage);
                                                            //document.UserFields.Fields.Item("U_InfoOrderMessage").Value = Convert.ToSByte(orderMessage);
                                                        }
                                                        catch (Exception sb)
                                                        {
                                                            String oErr = sb.Message;
                                                            _logger.Error("Error setting OrderMessage =>" + oErr);
                                                        }
                                                    }

                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.GiftMessage))
                                                    {
                                                        _logger.Debug("Processing 3PL Gift Message");
                                                        document.UserFields.Fields.Item("U_InfoGiftMessage").Value = edi850HeaderRecord.GiftMessage;
                                                    }

                                                    // 07-07-2021 begin
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.MessageText) && edi850HeaderRecord.MessageText.Trim().Length > 0)
                                                    {
                                                        try
                                                        {
                                                            _logger.Debug("Processing 3PL Message Text");
                                                            if (edi850HeaderRecord.MessageText.StartsWith("http"))
                                                            {
                                                                _logger.Debug("Skipped MessageText - http not imported");
                                                            }
                                                            else
                                                            {
                                                                string messageText = edi850HeaderRecord.MessageText.ToString().Trim();
                                                                document.UserFields.Fields.Item("U_InfoMessageText").Value = Convert.ToString(messageText);
                                                                //document.UserFields.Fields.Item("U_InfoMessageText").Value = messageText;
                                                            }
                                                        }
                                                        catch (Exception mt)
                                                        {
                                                            String oErr = mt.Message;
                                                            _logger.Error("Error setting Message Text =>" + oErr);
                                                        }
                                                    }
                                                    // 07-07-2021 end

                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.DiscountDescr))
                                                    {
                                                        _logger.Debug("Processing 3PL Discount Descr");
                                                        document.UserFields.Fields.Item("U_InfoDiscountDesc").Value = edi850HeaderRecord.DiscountDescr;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.ItemTaxType))
                                                    {
                                                        _logger.Debug("Processing 3PL Tax Type");
                                                        document.UserFields.Fields.Item("U_InfoItemTaxType").Value = edi850HeaderRecord.ItemTaxType;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.TaxComponent))
                                                    {
                                                        _logger.Debug("Processing 3PL Tax Component");
                                                        document.UserFields.Fields.Item("U_InfoTaxComponent").Value = edi850HeaderRecord.TaxComponent;
                                                    }
                                                    document.UserFields.Fields.Item("U_C3_STC").Value = oSTC; // 05-25-2021
                                                    // 06-01-2021 begin
                                                    if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType3PL) && edi850HeaderRecord.OrderType3PL.Trim().ToUpper() == "DROPSHIP")
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                    }
                                                    // 06-01-2021 end
                                                    // 02-12-2022 begin
                                                    else if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.OrderType3PL) &&
                                                        (edi850HeaderRecord.OrderType3PL.Trim().ToUpper() == "STORE" || edi850HeaderRecord.OrderType3PL.Trim().ToUpper() == "RDC"))
                                                    {
                                                        document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2S";
                                                    }
                                                    // 02-12-2022 end
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("End processing 3PL udfs");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("End processing 3PL udfs");
                                                    }
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Finished processing 3PL udfs");
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Finished Processing 3PL udfs");
                                                    }
                                                }

                                                // 05-12-2021 end

                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Processing 850 Detail Lines");
                                                }
                                                else
                                                {
                                                    _logger.Debug("Processing 850 Detail Lines");
                                                }
                                                int lineCounter = 0;
                                                _logger.Debug(edi850HeaderRecord.Details.Count.ToString() + " Detail rows to process");
                                                foreach (Edi850DetailRecord detailRecord in edi850HeaderRecord.Details)
                                                {
                                                    _logger.Debug("HeaderId: " + detailRecord.HeaderId.ToString() + " Line# " + detailRecord.LineNumber);
                                                    if (lineCounter > 0)
                                                    {
                                                        document.Lines.Add();
                                                        string oInfo = document.Lines.LineNum.ToString();
                                                    }
                                                    lineCounter++;

                                                    if (!String.IsNullOrWhiteSpace(detailRecord.BuyerItemCode)
                                                    && (String.IsNullOrWhiteSpace(detailRecord.VendorItemCode)
                                                    || edi850HeaderRecord.CardCode.StartsWith("LowesNet")
                                                    || edi850HeaderRecord.CardCode.StartsWith("Lowes2") // 06-19-2019
                                                    || edi850HeaderRecord.CardCode.StartsWith("TSCCL") // 02-05-2019
                                                    || edi850HeaderRecord.CardCode.StartsWith("HDCL")// 06-01-2019
                                                    || edi850HeaderRecord.CardCode.StartsWith("WAYFAIR") // 08-26-2019
                                                    || edi850HeaderRecord.CardCode.StartsWith("APEX") // 04-26-2021
                                                    || edi850HeaderRecord.CardCode.StartsWith("Bravo") // 04-26-2021
                                                    )
                                                     && !(edi850HeaderRecord.CardCode.StartsWith("3PL-")) // 05-12-2021
                                                        )
                                                    {
                                                        _logger.Debug("Looking up Item Code " + detailRecord.BuyerItemCode + " from Business Partner");
                                                        if (edi850HeaderRecord.SBOCardCode == null || edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                                                        {  // 01-17-2018
                                                            // 10-24-2019 begin
                                                            //document.Lines.ItemCode = LookupItemCode(edi850HeaderRecord.SBOCardCode, detailRecord.BuyerItemCode);
                                                            document.Lines.ItemCode = LookupBuyerItemCode(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.BuyerItemCode);
                                                            // 10-24-2019 end
                                                            // 01-17-2018 begin
                                                        }
                                                        else
                                                        {
                                                            // 02-21-2018 begin
                                                            if (edi850HeaderRecord.CardCode.StartsWith("LowesNet"))
                                                            {
                                                                // 10-24-2019 begin
                                                                //document.Lines.ItemCode = LookupItemCode(edi850HeaderRecord.SBOCardCode, detailRecord.BuyerItemCode);
                                                                document.Lines.ItemCode = LookupBuyerItemCode(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.BuyerItemCode);
                                                                /*
                                                                 if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode)))
                                                                 {
                                                                     document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                                 }
                                                                 */
                                                                // 10-24-2019 end
                                                            }
                                                            else
                                                            { // 02-21-2018 end
                                                                // 10-24-2019 begin
                                                                //document.Lines.ItemCode = LookupItemCode(edi850HeaderRecord.SBOCardCode, detailRecord.BuyerItemCode);
                                                                document.Lines.ItemCode = LookupBuyerItemCode(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.BuyerItemCode);
                                                                // 10-24-2019 end
                                                            }// 02-21-2018 
                                                        } // 01-17-2018 end

                                                        if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                        {
                                                            // 10-24-2019 begin
                                                            //document.Lines.ItemCode = LookupVendorItemCode(edi850HeaderRecord.SBOCardCode, detailRecord.VendorItemCode);
                                                            // 10-24-2019 end
                                                            if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                            {
                                                                if (edi850HeaderRecord.SBOCardCode == null || edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                                                                {  // 01-17-2018
                                                                    //Infocus.WebApi.Common.Bone.Import_Log._850Error = "Invalid item for PO# '" + edi850HeaderRecord.PurchaseOrderReference.Trim() + "'"; // 06-29-2019
                                                                    set850Error("Invalid item found for PO# '" + edi850HeaderRecord.PurchaseOrderReference.Trim() + "', HeaderId = " + edi850HeaderRecord.HeaderId, edi850HeaderRecord.HeaderId, autoImport, false); // 06-28-2019
                                                                    throw new WebApiException("Could not locate Item " + detailRecord.BuyerItemCode + " for CardCode " + edi850HeaderRecord.CardCode);
                                                                    // 01-17-2018 begin
                                                                }
                                                                else
                                                                {
                                                                    set850Error("PO# " + edi850HeaderRecord.PurchaseOrderReference + " invalid Item " + detailRecord.BuyerItemCode + " for CardCode " + edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.HeaderId, autoImport, false); // 06-29-2019
                                                                    throw new WebApiException("Could not locate Item  for  PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                                                } // 01-17-2018 end
                                                            }
                                                        }
                                                    }
                                                    else if (!String.IsNullOrWhiteSpace(detailRecord.VendorItemCode))
                                                    {
                                                        _logger.Debug("Assigning Item Code Directly");
                                                        // 02-21-2018 begin
                                                        if (edi850HeaderRecord.CardCode.StartsWith("LowesNet") ||
                                                            edi850HeaderRecord.CardCode.StartsWith("WAYFAIR") || // 08-26-2019
                                                            edi850HeaderRecord.CardCode.StartsWith("TSCCL") ||
                                                            edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                                        {
                                                            //document.Lines.ItemCode = LookupItemCode(edi850HeaderRecord.CardCode, detailRecord.BuyerItemCode);
                                                            // 10-24-2019 begin
                                                            //document.Lines.ItemCode = LookupXrefItemCode(edi850HeaderRecord.SBOCardCode, detailRecord.VendorItemCode); // 08-26-2019 
                                                            document.Lines.ItemCode = LookupVendorItemCode(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.VendorItemCode);
                                                            // 10-24-2019 end
                                                            if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode, autoImport)))
                                                            {
                                                                document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                            }
                                                        } // 02-21-2018 end
                                                        // 05-21-2021 begin
                                                        else if (oIs3PL == "Y")
                                                        {
                                                            document.Lines.ItemCode = Lookup3PLItemCode(edi850HeaderRecord.HeaderId, edi850HeaderRecord.SBOCardCode, detailRecord.LineNumber, detailRecord.VendorItemCode, autoImport);
                                                            if (detailRecord.RetailPrice.HasValue && detailRecord.RetailPrice.Value > 0)
                                                            {
                                                                try
                                                                {
                                                                    double oRetailPrice = detailRecord.RetailPrice.Value;
                                                                    //document.Lines.UserFields.Fields.Item("U_C3_RetailPrc").Value = detailRecord.RetailPrice;
                                                                    document.Lines.UserFields.Fields.Item("U_C3_RetailPrc").Value = oRetailPrice;
                                                                }
                                                                catch (Exception Rt)
                                                                {
                                                                    if (autoImport)
                                                                    {
                                                                        Import_Log.LogEntry("Error saving retail price " + detailRecord.RetailPrice.ToString() + " for " + edi850HeaderRecord.PurchaseOrderReference + " => " + Rt.Message);
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Error("Error saving retail price " + detailRecord.RetailPrice.ToString() + " for " + edi850HeaderRecord.PurchaseOrderReference + " => " + Rt.Message);
                                                                    }
                                                                }
                                                            }
                                                            if (!String.IsNullOrWhiteSpace(detailRecord.CustItemCode))
                                                            {
                                                                document.Lines.UserFields.Fields.Item("U_InfoCustItemCode").Value = detailRecord.CustItemCode;
                                                            }
                                                            try // 07-14-2021
                                                            {
                                                                if (detailRecord.GrossPkgWeight == null || detailRecord.GrossPkgWeight.ToString().Length == 0)
                                                                {

                                                                    document.Lines.UserFields.Fields.Item("U_InfoGrossPkgWgt").Value = Convert.ToDouble("0.00");
                                                                }
                                                                else
                                                                {
                                                                    try
                                                                    {
                                                                        string oGrsPkgWgt = detailRecord.GrossPkgWeight.ToString();
                                                                        document.Lines.UserFields.Fields.Item("U_InfoGrossPkgWgt").Value = Convert.ToDouble(oGrsPkgWgt);
                                                                    }
                                                                    catch
                                                                    {
                                                                        document.Lines.UserFields.Fields.Item("U_InfoGrossPkgWgt").Value = Convert.ToDouble("0.00");
                                                                    }
                                                                }
                                                            } // 07-14-2021 begin
                                                            catch (Exception pw)
                                                            {
                                                                if (autoImport)
                                                                {
                                                                    Import_Log.LogEntry("Error setting gross package weight =>" + pw.Message);
                                                                }
                                                                else
                                                                {
                                                                    Import_Log.LogEntry("Error setting gross package weight =>" + pw.Message);
                                                                }
                                                            }// 07-14-2021 end
                                                        }
                                                        // 05-12-2021 end
                                                        else
                                                        {
                                                            document.Lines.ItemCode = detailRecord.VendorItemCode;
                                                        }
                                                        // 02-21-2018 begin
                                                        if (String.IsNullOrWhiteSpace(ValidateItemCode(document.Lines.ItemCode, autoImport)))
                                                        {
                                                            document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                        }
                                                        // 02-21-2018 end
                                                    }
                                                    else
                                                    {
                                                        document.Lines.ItemCode = detailRecord.BuyerItemCode;
                                                        if (String.IsNullOrWhiteSpace(document.Lines.ItemCode))
                                                        {
                                                            set850Error("Invalid item found", edi850HeaderRecord.HeaderId, autoImport, false); // 06-28-2019
                                                            throw new WebApiException("ItemCode (VendorItemCode) is required");
                                                        }
                                                    }
                                                    // disable for now
                                                    /* // 03-12-2021 begin
                                                        if (!String.IsNullOrWhiteSpace(edi850HeaderRecord.VendorNumber)
                                                            && edi850HeaderRecord.VendorNumber == "111111")
                                                        {
                                                            document.Lines.WarehouseCode = "LouiseNC";
                                                            document.Lines.ChangeAssemlyBoMWarehouse = "Y";
                                                        }
                                                       // 03-24-2021 end
                                                     */
                                                    // 03-26-2021 begin
                                                    if (!String.IsNullOrWhiteSpace(oVBUWhs))
                                                    {
                                                        document.Lines.WarehouseCode = oVBUWhs;
                                                    }
                                                    // 03-26-2021 end
                                                    // 02-21-2018 begin
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.VendorItemCode))
                                                    {
                                                        document.Lines.FreeText = detailRecord.VendorItemCode;
                                                    }
                                                    // 02-21-2018 end
                                                    document.Lines.Quantity = detailRecord.Quantity;
                                                    if (detailRecord.UnitPrice.HasValue && detailRecord.UnitPrice.Value > 0)
                                                    {
                                                        document.Lines.UnitPrice = detailRecord.UnitPrice.Value;
                                                    }
                                                    document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = detailRecord.LineNumber;
                                                    // 02-08-2019 begin

                                                    if (!String.IsNullOrWhiteSpace(detailRecord.PackingNotes))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2PackNote").Value = detailRecord.PackingNotes;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.ItemUPC))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2ItemUPC").Value = detailRecord.ItemUPC;
                                                    }
                                                    // 02-08-2019 end
                                                    // 03-25-2019 begin

                                                    if (!String.IsNullOrWhiteSpace(detailRecord.Routing))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2Routing").Value = detailRecord.Routing;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.ServiceLevel))
                                                    {

                                                        document.Lines.UserFields.Fields.Item("U_InfoW2ServLev").Value = detailRecord.ServiceLevel;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.TrackingNumber))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2TrackNo").Value = detailRecord.TrackingNumber;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.TrackingNoText))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2TrkText").Value = detailRecord.TrackingNoText;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.DeliveryConfirmation))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2DelConf").Value = detailRecord.DeliveryConfirmation;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.DeliveryText))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2DelText").Value = detailRecord.DeliveryText;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.PurchaserItemCode))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoW2PurchaserItem").Value = detailRecord.PurchaserItemCode;
                                                    }
                                                    // 03-25-2019 END
                                                    // 05-12-2021 begin
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.CustItemCode))
                                                    {
                                                        document.Lines.UserFields.Fields.Item("U_InfoCustItemCode").Value = detailRecord.CustItemCode;
                                                    }
                                                    if (!String.IsNullOrWhiteSpace(detailRecord.UnitOfMeasure) && oIs3PL == "Y")
                                                    {
                                                        string oUOM = detailRecord.UnitOfMeasure;
                                                        if (oUOM.ToUpper() == "EA")
                                                        {
                                                            oUOM = "Each";
                                                        }
                                                        document.Lines.UoMEntry = getUnitOfMeasure(document.Lines.ItemCode, oUOM, autoImport);
                                                    }
                                                    // 05-12-2021 end
                                                    try
                                                    {
                                                        Int32 iTransportCode = Convert.ToInt32(transportCode);
                                                        document.Lines.ShippingMethod = iTransportCode;
                                                    }
                                                    catch (Exception tc)
                                                    {
                                                        String oErrMesg = tc.Message;
                                                        oErrMesg = "Error setting Shipping Method =>" + oErrMesg; // 09-191-2021
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(tc.Message);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error(tc.Message);
                                                        }
                                                    }
                                                    // 05-14-2020 end
                                                }
                                                string temp = (string)document.UserFields.Fields.Item("U_InfoW2ShNm").Value;
                                                // 11-20-2019 begin
                                                string prdDesc = "";
                                                try
                                                {
                                                    prdDesc = document.UserFields.Fields.Item("U_InfoW2PrdDesc").ToString();
                                                }
                                                catch
                                                {
                                                    prdDesc = "";
                                                }
                                                if (edi850HeaderRecord.CardCode.StartsWith("Wayfair") && String.IsNullOrWhiteSpace(prdDesc))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                }
                                                // 11-201-2019 end
                                                // 03-01-2023 begin
                                                else if (edi850HeaderRecord.CardCode.StartsWith("TSC") && String.IsNullOrWhiteSpace(prdDesc))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                }
                                                // 03-01-2023 end
                                                // 06-02-2020 begin
                                                if (edi850HeaderRecord.CardCode.StartsWith("Wayfair") || edi850HeaderRecord.CardCode.Equals("WAYFAIR") || edi850HeaderRecord.CardCode.Equals("Wayfair"))
                                                {
                                                    document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                }
                                                // 06-02-2020 end
                                                // 07-15-2021 begin
                                                if (edi850HeaderRecord.CardCode.StartsWith("HDCL") && String.IsNullOrWhiteSpace(prdDesc))
                                                {
                                                    try
                                                    {
                                                        if (String.IsNullOrWhiteSpace(edi850HeaderRecord.ShipToLocationCode))
                                                        {
                                                            document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2C";
                                                        }
                                                        else
                                                        {
                                                            document.UserFields.Fields.Item("U_InfoW2PrdDesc").Value = "D2S";
                                                        }
                                                    }
                                                    catch (Exception h)
                                                    {
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(h.Message);
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug(h.Message);
                                                        }
                                                    }
                                                }
                                                // 07-15-2021 end
                                                string oLineInfo = document.Lines.LineNum.ToString();
                                                int oNoLines = document.Lines.Count;
                                                // 04-21-2022 begin
                                                bool bFoundDoc = checkExistingOrders(edi850HeaderRecord.PurchaseOrderReference, edi850HeaderRecord.SBOCardCode, edi850HeaderRecord.CardCode, poDate, recDate, edi850HeaderRecord.CustomerOrderNumber, autoImport);
                                                if (bFoundDoc == false)
                                                {
                                                    // 04-21-2022 endre
                                                    String msg1 = _company.GetLastErrorDescription();
                                                    int oErrCode1 = _company.GetLastErrorCode();
                                                    int oRet = document.Add();
                                                    //if (document.Add() != 0)
                                                    if (oRet != 0)
                                                    {
                                                        String msg = _company.GetLastErrorDescription();
                                                        int oErrCode = _company.GetLastErrorCode();
                                                        if (oRet == 5009 || oRet == -5009)
                                                        {
                                                            msg = "Error adding Sales Order - Missing Item Code => " + msg;
                                                        }
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry(msg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error(msg);
                                                        }
                                                        //Infocus.WebApi.Common.Bone.Import_Log._850Error = msg; // 06-29-2019
                                                        set850Error(msg, edi850HeaderRecord.HeaderId, autoImport, false); // 06-28-2019
                                                        if (!msg.StartsWith("(402) The data types ntext and nvarchar")) // 08-28-2021
                                                        {
                                                            throw new WebApiException(msg);
                                                        } // 08-28-2021
                                                    }

                                                    DocumentKey documentKey = new DocumentKey();
                                                    String strDocEntry = _company.GetNewObjectKey();
                                                    documentKey.DocEntry = Int32.Parse(strDocEntry);
                                                    documentKey.DocNum = GetDocNum(documentKey.DocEntry);
                                                    // 03-26-2021 begin
                                                    // if VBU whs is not blank update the sales order lines with the VBU warehouse
                                                    if (!String.IsNullOrWhiteSpace(oVBUWhs))
                                                    {
                                                        try
                                                        {
                                                            document.GetByKey(documentKey.DocEntry);
                                                            if (!(document == null)) // 08-18-2021
                                                            {

                                                                int noLines = document.Lines.Count;
                                                                int noLnUpdated = 0; // 03-13-2022
                                                                for (int i = 0; i < noLines; i++)
                                                                {
                                                                    document.Lines.SetCurrentLine(i);
                                                                    document.Lines.WarehouseCode = oVBUWhs;
                                                                    // 03-13-2022 begin
                                                                    noLnUpdated = noLnUpdated + 1;
                                                                    //noLines = noLines + 1;
                                                                    // 03-13-2022 end
                                                                }
                                                                //  if (noLines > 0)
                                                                if (noLnUpdated > 0) // 03-13-2022
                                                                {
                                                                    document.Update();
                                                                }
                                                            } // 08-18-2021
                                                        }
                                                        catch (Exception dl)
                                                        {
                                                            string oErrMsg = dl.Message;
                                                            if (autoImport)
                                                            {
                                                                Import_Log.LogEntry("Error setting warehouse => " + oErrMsg);
                                                            }
                                                            else
                                                            {
                                                                _logger.Error("Error setting warehouse => " + oErrMsg);
                                                            }
                                                        }
                                                    }
                                                    // 03-26-2021 end
                                                    // 05-26-2021 begin
                                                    try
                                                    {
                                                        document.GetByKey(documentKey.DocEntry);

                                                        int noLnUpdated = 0;  // 07-12-2021
                                                        int noLines = document.Lines.Count;
                                                        for (int i = 0; i < noLines; i++)
                                                        {
                                                            if (document.Lines.TreeType.ToString() == "I")
                                                            {
                                                                document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = 0;
                                                                noLnUpdated = noLnUpdated + 1; // 07-12-2021
                                                            }
                                                        }
                                                        if (noLnUpdated > 0)
                                                        {  // 07-12-2021
                                                            document.Update();
                                                        }  // 07-12-2021
                                                    }
                                                    catch (Exception dl)
                                                    {
                                                        document.GetByKey(documentKey.DocEntry);
                                                        string oErrMsg = dl.Message;
                                                        Recordset rs2 = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error setting edi line number for child BOM items => " + oErrMsg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Error setting edi line number for child BOM items => " + oErrMsg);
                                                        }
                                                        /*
                                                        string oQry1 = "update rdr1 set U_InfoW2LNo = 0 where DocEntry = " + document.DocEntry.ToString() + " and TreeType = 'I'";
                                                        rs2.DoQuery(oQry1);
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Ran sql query to set edi line number for child BOM items SO DocEntry " + document.DocEntry.ToString());
                                                        }
                                                        else
                                                        {
                                                            _logger.Debug("Ran sql query to set edi line number for child BOM items => " + document.DocEntry.ToString());
                                                        }
                                                         */
                                                    }
                                                    // 05-26-2021 end
                                                    // 02-12-2022 begin
                                                    // check for null U_InfoW2LNo
                                                    try
                                                    {
                                                        document.GetByKey(documentKey.DocEntry);
                                                        if (!(document == null))
                                                        {
                                                            int noLnUpdated = 0; // 03-13-2022
                                                            int noLines = document.Lines.Count;
                                                            for (int i = 0; i < noLines; i++)
                                                            {
                                                                document.Lines.SetCurrentLine(i);
                                                                try
                                                                {
                                                                    string oEdiLNo = document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value.ToString();
                                                                    if (String.IsNullOrWhiteSpace(oEdiLNo))
                                                                    {
                                                                        document.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = 0;
                                                                        noLnUpdated = noLnUpdated + 1; // 03-13-2022
                                                                    }
                                                                }
                                                                catch (Exception LNo)
                                                                {
                                                                    string oErrMsg = LNo.Message;
                                                                    if (autoImport)
                                                                    {
                                                                        Import_Log.LogEntry("Error updating Ln# " + i.ToString() + " udf U_InfoW2LNo  => " + oErrMsg);
                                                                    }
                                                                    else
                                                                    {
                                                                        _logger.Error("Error updating Ln# " + i.ToString() + " udf U_InfoW2LNo => " + oErrMsg);
                                                                    }
                                                                }
                                                                //noLines = noLines + 1;
                                                            }
                                                            //if (noLines > 0)
                                                            if (noLnUpdated > 0) // 03-13-2022
                                                            {
                                                                document.Update();
                                                            }
                                                        }
                                                    }
                                                    catch (Exception dl)
                                                    {
                                                        string oErrMsg = dl.Message;
                                                        if (autoImport)
                                                        {
                                                            Import_Log.LogEntry("Error updating U_InfoW2LNo  => " + oErrMsg);
                                                        }
                                                        else
                                                        {
                                                            _logger.Error("Error updating U_InfoW2LNo => " + oErrMsg);
                                                        }
                                                    }
                                                    // 02-12-2022 end

                                                    //  setAltVendorItem(documentKey.DocEntry); // 02-21-2018
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Leaving Process850Record. Created Document " + documentKey.DocNum);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Leaving Process850Record.  Created Document " + documentKey.DocNum);
                                                    }
                                                    //5-14-2020 setLineTransportCode(strDocEntry, transportCode, autoImport); // 10-08-2019
                                                    return documentKey;
                                                    // 04-21-2022 begin
                                                }
                                                else
                                                {
                                                    set850Error("Duplicate SO", edi850HeaderRecord.HeaderId, autoImport, false);
                                                    edi850HeaderRecord.Processed = true;
                                                    if (autoImport)
                                                    {
                                                        Import_Log.LogEntry("Leaving Process850Record. Skipped duplicate for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                                    }
                                                    else
                                                    {
                                                        _logger.Debug("Leaving Process850Record.  Skipped duplicate for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                                    }
                                                    DocumentKey documentKey = new DocumentKey();
                                                    return documentKey;
                                                }
                                                // 04-21-2022 end
                                            } // 09-24-2022
                                            else
                                            {
                                                if (autoImport)
                                                {
                                                    Import_Log.LogEntry("Leaving Process850Record. Skipped invalid 3PL Shipper found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                                }
                                                else
                                                {
                                                    _logger.Debug("Leaving Process850Record.  Skipped invalid 3PL Shipper found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                                }
                                                string ErrorDetail = "";
                                                if (String.IsNullOrWhiteSpace(serviceLevel))
                                                {
                                                    ErrorDetail = "Carrier Code " + carrierCode;
                                                }
                                                else
                                                {
                                                    ErrorDetail = "Carrier Code: " + carrierCode + ", Service Lv: " + serviceLevel + ", STC: " + oSTC;
                                                }
                                                set850Error("Invalid Invalid 3PL Shipper found => " + ErrorDetail, edi850HeaderRecord.HeaderId, autoImport, false);
                                                DocumentKey documentKey = new DocumentKey();
                                                return documentKey;
                                            }
                                            // 09-24-2022 end
                                            // 10-11-2019 begin
                                        }
                                        else
                                        {
                                            if (autoImport)
                                            {
                                                Import_Log.LogEntry("Leaving Process850Record. Skipped invalid SCAC found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                            }
                                            else
                                            {
                                                _logger.Debug("Leaving Process850Record.  Skipped invalid SCAC found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                            }
                                            string ErrorDetail = "";
                                            if (String.IsNullOrWhiteSpace(serviceLevel))
                                            {
                                                ErrorDetail = "Carrier Code " + carrierCode;
                                            }
                                            else
                                            {
                                                ErrorDetail = "Carrier Code: " + carrierCode + ", Service Lv: " + serviceLevel;
                                            }
                                            set850Error("Invalid SCAC found => " + ErrorDetail, edi850HeaderRecord.HeaderId, autoImport, false); // 06-28-2019
                                            DocumentKey documentKey = new DocumentKey();
                                            return documentKey;
                                        } // 10-11-2019 end
                                        // 06-28-2019 begin
                                    }
                                    else
                                    {
                                        if (autoImport)
                                        {
                                            Import_Log.LogEntry("Leaving Process850Record. Skipped price mismatch found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                        }
                                        else
                                        {
                                            _logger.Debug("Leaving Process850Record.  Skipped price mismatch found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                        }
                                        if (oIs3PL == "N")  // 07-14-2021
                                        {
                                            set850Error("Price variance found", edi850HeaderRecord.HeaderId, autoImport, false); // 06-28-2019
                                        } // 07-14-2021
                                        DocumentKey documentKey = new DocumentKey();
                                        return documentKey;
                                    }
                                    // 06-28-2018 end

                                    // 10-24-2019 begin
                                }
                                else
                                {
                                    if (autoImport)
                                    {
                                        Import_Log.LogEntry("Leaving Process850Record. Skipped invalid item(s) found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                    }
                                    else
                                    {
                                        _logger.Debug("Leaving Process850Record.  Skipped invalid item(s) found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                    }
                                    set850Error("Invalid item(s) found", edi850HeaderRecord.HeaderId, autoImport, false);
                                    DocumentKey documentKey = new DocumentKey();
                                    return documentKey;
                                }
                                // 10-24-2019 end
                                // 10-22-2021 begin
                            }
                            else
                            {
                                // 03-02-2022 begin
                                String oErr = "PrePaid found";
                                String oErr2 = "PrePaid Error";
                                if (oIs3PL == "Y")
                                {
                                    oErr = "Invalid PaymentMethod";
                                    oErr2 = oErr;
                                }
                                /*
                                if (autoImport)
                                {
                                    Import_Log.LogEntry("Leaving Process850Record. Skipped PrePaid found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                }
                                else
                                {
                                    _logger.Debug("Leaving Process850Record.  Skipped PrePaid found for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                }
                                set850Error("PrePaid Error", edi850HeaderRecord.HeaderId, autoImport);
                                 * */
                                if (autoImport)
                                {
                                    Import_Log.LogEntry("Leaving Process850Record. " + oErr + " for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                }
                                else
                                {
                                    _logger.Debug("Leaving Process850Record. " + oErr + " for PO# " + edi850HeaderRecord.PurchaseOrderReference);
                                }
                                set850Error(oErr2, edi850HeaderRecord.HeaderId, autoImport, false);
                                // 03-02-2022 end
                                DocumentKey documentKey = new DocumentKey();
                                return documentKey;
                            }
                            // 10-22-2021 end
                        } // 07-16-2018 begin 
                        else
                        {
                            if (autoImport)
                            {
                                Import_Log.LogEntry("Leaving Process850Record. Skipped duplicate PO# " + edi850HeaderRecord.PurchaseOrderReference);
                            }
                            else
                            {
                                _logger.Debug("Leaving Process850Record.  Skipped duplicate PO# " + edi850HeaderRecord.PurchaseOrderReference);
                            }
                            edi850HeaderRecord.Processed = true; // 07-13-2021
                            set850Error("Duplicate 850", edi850HeaderRecord.HeaderId, autoImport, false);
                            DocumentKey documentKey = new DocumentKey();
                            return documentKey;
                        }
                        //07-16-2018 end
                    } // 05-31-2017
                    // 05-15-2020 begin
                    else
                    {
                        return null;
                    }
                    // 05-15-2020 end

                }
                // 06-27-2017 begin
                /*
                // 06-07-2017 begin
                catch (Exception soEx)
                {
                    String oErrMsg = soEx.Message;
                    _logger.Debug("Error in BoneSalesOrderDataLayer: " + oErrMsg + " (" + soEx.InnerException.Message + ")");
                    throw new WebApiException("ERROR :" + oErrMsg + " (" + soEx.InnerException.Message + ")");
                }
                // 06-07-2017 end
                */
                // 06-27-2017 
                // 02-21-2019 begin
                catch (Exception soEx)
                {
                    String oErrMsg = soEx.Message;
                    edi850HeaderRecord.ErrorMessage = oErrMsg; // 06-29-2019
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error in BoneSalesOrderDataLayer processing PO# " + edi850HeaderRecord.PurchaseOrderReference + ": " + oErrMsg + "(" + soEx.InnerException.Message + ")");
                    }
                    else
                    {
                        _logger.Error("Error in BoneSalesOrderDataLayer processing PO# " + edi850HeaderRecord.PurchaseOrderReference + ": " + oErrMsg + " (" + soEx.InnerException.Message + ")");
                    }
                    set850Error(oErrMsg, edi850HeaderRecord.HeaderId, autoImport, false);
                    return null;
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(document);
                }
                // 07-26-2024 begin
            }
            else
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Skipping INDOCOUNT 850 PO# " + edi850HeaderRecord.PurchaseOrderReference);
                }
                else
                {
                    _logger.Debug("Skipping INDOCOUNT 850 PO# " + edi850HeaderRecord.PurchaseOrderReference);
                }
                return null;
            } // 07-16-2024 end
        }
        // 10-24-2019 begin
        //private String LookupItemCode(String cardCode, String pItemCode)
        private String LookupBuyerItemCode(int headerId, String cardCode, int lineNumber, String pBuyerItemCode)

        // 10-24-2019 end
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                // 10-24-2019 begin
                //string oQry = "select ItemCode from OSCN where Substitute = '" + pItemCode.Trim() + "' and CardCode = '" + cardCode.Trim() + "'";
                string oQry = "select ItemCode from [Infocus_EDI_Item_Lookup]  WITH(NOLOCK) where BuyerItemCode = '" + pBuyerItemCode.Trim() + "' and SBOCardCode = '" + cardCode.Trim() +
                    "' and HeaderId = " + headerId + " and LineNumber = " + lineNumber;
                // 10-24-2019 end
                rs.DoQuery(oQry);
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
        // 10-24-2019 begin
        private String LookupVendorItemCode(int headerId, String cardCode, int lineNumber, String vendorItemCode)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oQry = "select ItemCode from [Infocus_EDI_Item_Lookup]  WITH(NOLOCK) where VendorItemCode = '" + vendorItemCode.Trim() + "' and CardCode = '" + cardCode.Trim() +
                    "' and HeaderId = " + headerId + " and LineNumber = " + lineNumber;
                rs.DoQuery(oQry);
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
        // 10-24-2019 end

        // 05-12-2021 begin
        private String get3PLSCAC(String pSBOCardCode, String pCarrierCode, String pServiceLevel, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            String oCarrier = "";
            try
            {
                string oQry = "select IsNull(U_TrgSCAC,'') ShipCode from [@C3_CSCACC]  WITH(NOLOCK) where U_CardCode = '" +
                   pSBOCardCode.Trim() + "' and U_SrcSCAC = '" + pCarrierCode.Trim() + "'";
                if (!String.IsNullOrWhiteSpace(pServiceLevel))
                {
                    oQry = oQry + " and IsNull(U_SrcService,'') = '" + pServiceLevel.Trim() + "'";
                }
                rs.DoQuery(oQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    oCarrier = (String)rs.Fields.Item("ShipCode").Value.ToString();
                }
            }
            catch (Exception e)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting 3PL SCAC => " + e.Message);
                }
                else
                {
                    _logger.Error("Error getting 3PL SCAC => " + e.Message);
                }
            }
            finally
            {
            }
            return oCarrier;
        }

        private String Lookup3PLItemCode(int headerId, String cardCode, int lineNumber, String vendorItemCode, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oItmPreFix = Get3PLItemPrefix(cardCode, autoImport);
                if (String.IsNullOrWhiteSpace(oItmPreFix))
                {
                    return String.Empty;
                }
                else
                {
                    string oQry = "select ItemCode from OITM WITH(NOLOCK) where ItemCode = '" + oItmPreFix.Trim() + "-" + vendorItemCode.Trim() + "'";
                    rs.DoQuery(oQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        return (String)rs.Fields.Item("ItemCode").Value;
                    }
                    // 09-07-2023 begin
                    else
                    {
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Invalid 3PL Item Code => " + oItmPreFix + "-" + vendorItemCode.Trim());
                        }
                        else
                        {
                            _logger.Error("Invalid 3PL Item Code => " + oItmPreFix + "-" + vendorItemCode.Trim());
                        }
                        return String.Empty;
                    }
                    // 09-07-2023 end
                }
            }
            catch (Exception e)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting 3PL Item Code => " + e.Message);
                }
                else
                {
                    _logger.Error("Error getting 3PL Item Code => " + e.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return String.Empty;
        }

        private String Get3PLItemPrefix(String cardCode, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oQry = "select IsNull(U_C3_ItmPrfx,'') ItmPrefix from OCRD  WITH(NOLOCK) where CardCode = '" + cardCode.Trim() + "'";
                rs.DoQuery(oQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    return (String)rs.Fields.Item("ItmPrefix").Value;
                }
            }
            catch (Exception e)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting Item Prefix => " + e.Message);
                }
                else
                {
                    _logger.Error("Error getting Item Prefix => " + e.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return String.Empty;
        }

        public Int32 getUnitOfMeasure(String pItemCode, String pUnitOfMeasure, bool autoImport)
        {
            Int32 oUom = -1;
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oQry = "SELECT T3.[UomEntry] UomEntry  FROM [dbo].[OITM] T0 WITH(NOLOCK) INNER JOIN [dbo].[OUGP] T1  WITH(NOLOCK) ON T0.[UgpEntry] = T1.[UgpEntry] " +
                              "INNER JOIN UGP1 T2  WITH(NOLOCK) ON T1.[UgpEntry] = T2.[UgpEntry] INNER JOIN OUOM T3  WITH(NOLOCK) ON T2.[UomEntry] = T3.[UomEntry] " +
                              "WHERE T0.[ItemCode] = '" + pItemCode.Trim() + "' and t3.[UOMName] = '" + pUnitOfMeasure.Trim() + "'";

                rs.DoQuery(oQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    return (Int32)rs.Fields.Item("UomEntry").Value;
                }
            }
            catch (Exception e)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting UOM => " + e.Message);
                }
                else
                {
                    _logger.Error("Error getting UOM => " + e.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return -1;
        }
        // 05-12-2021 end


        // 08-26-2019 begin
        private String LookupXrefItemCode(String cardCode, String vendorItemCode, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oQry = "select ItemCode from OSCN  WITH(NOLOCK) where U_EDI_XREF = '" + vendorItemCode.Trim() + "' and CardCode = '" + cardCode.Trim() + "'";
                rs.DoQuery(oQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    return (String)rs.Fields.Item("ItemCode").Value;
                }
            }
            catch (Exception e)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting xref ItemCode => " + e.Message);
                }
                else
                {
                    _logger.Error("Error getting Xref Item Code => " + e.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return String.Empty;
        }
        // 08-26-2019 end

        // 10-24-2019 begin
        /*
        // 06-29-2019 begin
        private String LookupVendorItemCode(String cardCode, String vendorItemCode)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oQry = "select ItemCode from OSCN where ItemCode = '" + vendorItemCode + "'";
                rs.DoQuery(oQry);
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
        // 06-29-2019 end
         */
        // 20-24-2019 end
        // 02-21-2018 begin
        private String ValidateItemCode(String pItemCode, bool autoImport)
        {

            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select ItemCode from OITM  WITH(NOLOCK) where ItemCode  = '" + pItemCode.Trim() + "'");
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    return (String)rs.Fields.Item("ItemCode").Value;
                }
            }
            catch (Exception e)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error validating Item Code => " + e.Message);
                }
                else
                {
                    _logger.Error("Error validating Item Code => " + e.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return String.Empty;
        }
        // 02-21-2018 end

        // 07-16-2018 begin
        //private bool checkExistingOrders(String pPONo, String pCardCode) //11-30-2020 added CardCode
        //private bool checkExistingOrders(String pPONo, String pCardCode, String pEdiCardCode, DateTime pPODate, DateTime pReceivedDateTime, bool autoImport) //01-29-2021 added po date and received date
        private bool checkExistingOrders(String pPONo, String pCardCode, String pEdiCardCode, DateTime pPODate, DateTime pReceivedDateTime, string pCustomerOrderNumber, bool autoImport) //08-01-2022 added customer order number
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bDuplicate = false;
            try
            {
                // 01-29-2021 begin

                DateTime cutOffDt = pReceivedDateTime.AddMonths(-9);
                //rs.DoQuery("select count(DocEntry) NoOrds from ORDR where Canceled = 'N' and NumAtCard  = '" + pPONo.Trim() + "'");
                string sqlQuery = "select count(DocEntry) NoOrds from ORDR  WITH(NOLOCK) where Canceled = 'N' and NumAtCard  = '" + pPONo.Trim() + "' and CardCode = '" + pCardCode.Trim() + "'";
                if (pCardCode == "HOMEDEPOT" || pCardCode.StartsWith("HDCL"))
                {
                    // 08-01-2022 begin
                    /*
                    // 09-09-2021 begin
                    int oNoMonths = getNoMonths(pEdiCardCode, autoImport);
                    oNoMonths = oNoMonths * -1;
                    cutOffDt = pReceivedDateTime.AddMonths(oNoMonths);
                    // 09-09-2021 end
                    sqlQuery = sqlQuery + " and (CreateDate > '" + cutOffDt + "')";
                    */
                    if (!String.IsNullOrWhiteSpace(pCustomerOrderNumber))
                    {
                        sqlQuery = sqlQuery + " and (U_InfoW2CustOrdNo = '" + pCustomerOrderNumber.Trim() + "' or U_InfoW2ShAt = '" + pCustomerOrderNumber.Trim() + "')";
                    }
                    // 08-02-2022 end
                }
                // 07-12-2021 begin -- remove this per Dan
                /*
               // 06-18-2021 begin
               else if (pCardCode == "TeeZed")
               {
                   sqlQuery = sqlQuery + " and DateDiff(Day, CreateDate, cast(" + pReceivedDateTime.ToString() + " as datetime)) = 0";
               }
               // 06-18-2021 end
                */
                // 07-12-2021 end
                rs.DoQuery(sqlQuery);
                // 01-21-2021 end
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
        // 07-16-2018 end

        // 09-01-2023 begin
        private bool checkExistingOrders(String pPONo, String pCardCode, String pEdiCardCode, DateTime pPODate, DateTime pReceivedDateTime, DateTime pReqShipDate, string pCustomerOrderNumber, string pShipToLocationCode, string pStoreNumber, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bDuplicate = false;
            string oPOKey = pPONo.Trim();
            if (!String.IsNullOrWhiteSpace(pShipToLocationCode))
            {
                oPOKey = oPOKey + "_" + pShipToLocationCode.Trim();
            }
            if (!String.IsNullOrWhiteSpace(pStoreNumber))
            {
                oPOKey = oPOKey + "_" + pStoreNumber.Trim();
            }
            // 02-29-2024 begin
            // switch to requested ship date
            /* DateTime oDate = (DateTime)pReceivedDateTime;
             string oRecDate = oDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
             if (!String.IsNullOrWhiteSpace(oRecDate))
             {
                 oPOKey = oPOKey + "_" + oRecDate.Trim();
             }*/
            DateTime oDate = (DateTime)pReqShipDate;
            string oReqShipDt = oDate.ToString("dd/M/yyyy", System.Globalization.CultureInfo.InvariantCulture);
            if (!String.IsNullOrWhiteSpace(oReqShipDt))
            {
                oPOKey = oPOKey + "_" + oReqShipDt.Trim();
            }
            // 02-24-2024 end
            try
            {
                DateTime cutOffDt = pReceivedDateTime.AddMonths(-9);
                string sqlQuery = "select count(DocEntry) NoOrds from ORDR  WITH(NOLOCK) where Canceled = 'N' and NumAtCard  = '" + oPOKey + "' and CardCode = '" + pCardCode.Trim() + "'";
                if (pCardCode == "HOMEDEPOT" || pCardCode.StartsWith("HDCL"))
                {
                    if (!String.IsNullOrWhiteSpace(pCustomerOrderNumber))
                    {
                        sqlQuery = sqlQuery + " and (U_InfoW2CustOrdNo = '" + pCustomerOrderNumber.Trim() + "' or U_InfoW2ShAt = '" + pCustomerOrderNumber.Trim() + "')";
                    }

                }

                rs.DoQuery(sqlQuery);
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
        // 09-01-2023 end

        // 09-09-2021 begin
        private Int16 getNoMonths(String pEdiCardCode, bool autoImport)
        {
            Int16 oNoMonths = 24;
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            String oQuery = "select IsNull(MonthsBtPOs,24) as NoMonths from InfocusEdi.dbo.WebApiDbContext  WITH(NOLOCK) where CardCode = '" +
                pEdiCardCode.Trim() + "'";
            try
            {
                rs.DoQuery(oQuery);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    string oValue = rs.Fields.Item("NoMonths").Value.ToString();
                    try
                    {
                        oNoMonths = Convert.ToInt16(oValue);
                    }
                    catch (Exception)
                    {
                        oNoMonths = 24;
                    }
                }
            }
            catch (Exception x)
            {
                String ErrorMesg = x.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error # Months between POx. Error => " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error # Months beteween POs. Error=> " + ErrorMesg);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return oNoMonths;
        }
        // 09-09-2021 end

        // 03-01-2022 begin
        private String[] getC3Shipper(String pSBOCardCode, String pCarrierCode, String pSTC, bool autoImport, string pIs3PL)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            String[] oShipperData = new String[3];

            //if (pSBOCardCode == "3PL-C0018" || (pIs3PL == "Y" && !(pSBOCardCode == "3PL-C006")))
            if (pSBOCardCode.ToUpper().StartsWith("3PL-")) // 04-22-2022
            {
                try
                {
                    // 03-10-2022 begin
                    /*
                    String oCustQry = "select top 1 t2.U_ShipperCode as ShipperCode, t2.U_Acct as Account,  t2.U_ShipType as ShipType " +
                                                "from [@C3_CSCACC] t0 left join OSHP t1 on t0.U_TrgSCAC = t1.WebSite " +
                                                "left join [@C3_DEFSHIPPER] t2 on t2.U_CustCode = t0.U_CardCode and t2.U_ShipType = t1.U_COR_Carrier " +
                                                "left join [@C3_STC] t3 on t3.Code = t2.U_STC  where U_CardCode = '" + pSBOCardCode.Trim() +
                                                "' and U_SrcSCAC = '" + pCarrierCode.Trim() + "' and t3.U_Stc = '" + pSTC.Trim() + "'";*/
                    String oCustQry = "select  t0.U_ShipperCode as ShipperCode, t0.U_Acct as Account,  t0.U_ShipType as ShipType  " +
                                              "from [@C3_DEFSHIPPER] t0  WITH(NOLOCK) left join [@C3_CSCACC] t1  WITH(NOLOCK) on t0.U_CustCode = t1.U_CardCode " +
                                             "left join OSHP t2  WITH(NOLOCK) on t2.WebSite = t1.U_TrgSCAC " + // 03-16-2022 changed from U_SrcSCAC to U_TrgSCAC
                                             "where t0.U_CustCode = '" + pSBOCardCode.Trim() + "'  and t0.U_STC = '" + pSTC.Trim() + "' and " +
                                             " t0.U_ShipType = t2.U_COR_Carrier and t1.U_SrcSCAC = '" + pCarrierCode.Trim() + "'";
                    // 03-10-2022 end
                    _logger.Debug("getC3Shipper query = " + oCustQry);
                    rs.DoQuery(oCustQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        try
                        {
                            String oShipper = rs.Fields.Item("ShipperCode").Value.ToString();
                            if (!String.IsNullOrWhiteSpace(oShipper))
                            {
                                oShipperData[0] = oShipper;
                            }
                            else
                            {
                                oShipperData[0] = "";
                            }
                        }
                        catch (Exception e1)
                        {
                            oShipperData[0] = "";
                        }
                        try
                        {
                            String oAcct = rs.Fields.Item("Account").Value.ToString();
                            if (!String.IsNullOrWhiteSpace(oAcct))
                            {
                                oShipperData[1] = oAcct;
                            }
                            else
                            {
                                oShipperData[1] = "";
                            }
                        }
                        catch (Exception e2)
                        {
                            oShipperData[1] = "";
                        }
                        try
                        {
                            String oShpType = rs.Fields.Item("ShipType").Value.ToString();
                            if (!String.IsNullOrWhiteSpace(oShpType))
                            {
                                oShipperData[2] = oShpType;
                            }
                            else
                            {
                                oShipperData[2] = "";
                            }
                        }
                        catch (Exception e3)
                        {
                            oShipperData[2] = "";
                        }
                    }
                }
                catch (Exception sh)
                {
                    String ErrorMesg = sh.Message;
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error getting C3_Shipper & Account# for " + pSBOCardCode + " Carrier " + pCarrierCode + " STC " + pSTC + " Error=> " + ErrorMesg);
                    }
                    else
                    {
                        _logger.Debug("Error getting C3_Shipper & Account# for " + pSBOCardCode + " Carrier " + pCarrierCode + " STC " + pSTC + " Error=> " + ErrorMesg);
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                }
            }
            return oShipperData;
        }

        private String getThirdPtyAcct(String pSBOCardCode, String pShipType, String pSTC, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            String oThrdPtyAcct = "";

            // if (pSBOCardCode == "3PL-C0018")
            if (pSBOCardCode.ToUpper().StartsWith("3PL-") && pSBOCardCode != "3PL-C0006") // 04-22-2022
            {
                try
                {
                    String oCustQry = "select top 1 IsNull(t0.U_CarrAcct,'') ThrdPtyAcct from  [@C3_STCSA] t0 With(NOLOCK) " +
                                               "where t0.U_CardCode = '" + pSBOCardCode.Trim() + "' and t0.U_Stc = '" + pSTC +
                                               "' and t0.U_ShipType = '" + pShipType.Trim() + "'";
                    _logger.Debug("Get 3rd Party Acct: " + oCustQry);
                    rs.DoQuery(oCustQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        oThrdPtyAcct = rs.Fields.Item("ThrdPtyAcct").Value.ToString();
                    }
                }
                catch (Exception sh)
                {
                    String ErrorMesg = sh.Message;
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error getting Third Party Acct# for " + pSBOCardCode + " STC " + pSTC + " ShipType " + pShipType + " Error=> " + ErrorMesg);
                    }
                    else
                    {
                        _logger.Debug("Error getting Third Party Acct# for " + pSBOCardCode + " STC " + pSTC + " ShipType " + pShipType + " Error=> " + ErrorMesg);
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                }
            }
            return oThrdPtyAcct;
        }
        // 03-01-2022 end
        // 03-08-2022 begin
        public String[] getThirdPtyBT(String pB1CardCode, String pShipType, String pSTC, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            String[] oThirdPtyBT = new String[6];
            try
            {
                String oCustQry = "select top 1 IsNull(t0.U_ThirdPtyName,'') BTName, IsNull(t0.U_ThirdPtyAddr,'') BtAddr, IsNull(t0.U_ThirdPtyCity,'') BTCity, " +
                                          "IsNull(t0.U_ThirdPtyState,'') BTState, IsNull(t0.U_ThirdPtyZip,'') BTZip, IsNull(t0.U_ThirdPtyCountry,'') BTCountry " +
                                           "from  [@C3_STCSA] t0 With(NOLOCK) " +
                                           "where t0.U_CardCode = '" + pB1CardCode.Trim() + "' and t0.U_Stc = '" + pSTC +
                                           "' and t0.U_ShipType = '" + pShipType.Trim() + "'";
                rs.DoQuery(oCustQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    oThirdPtyBT[0] = rs.Fields.Item("BTName").Value.ToString();
                    oThirdPtyBT[1] = rs.Fields.Item("BTAddr").Value.ToString();
                    oThirdPtyBT[2] = rs.Fields.Item("BTCity").Value.ToString();
                    oThirdPtyBT[3] = rs.Fields.Item("BTState").Value.ToString();
                    oThirdPtyBT[4] = rs.Fields.Item("BTZip").Value.ToString();
                    oThirdPtyBT[5] = rs.Fields.Item("BTCountry").Value.ToString();
                }
            }
            catch (Exception sh)
            {
                String ErrorMesg = sh.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting Third Party Bill To for " + pB1CardCode + " STC " + pSTC + " ShipType " + pShipType + " Error=> " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error getting Third Party Bil To for " + pB1CardCode + " STC " + pSTC + " ShipType " + pShipType + " Error=> " + ErrorMesg);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return oThirdPtyBT;
        }
        // 03-08-2022 end

        // 03-05-2020 begin
        private bool checkServLev(String pB1CardCode, String pEdiCardCode, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bCheckSLev = false;
            try
            {
                // 05-19-2021 begin
                //String oCustQry = "select distinct coalesce(U_EDI_CHK_SERV_LEV,'N') as ChkServLev from OCRD t1 where CardCode  = '" + pB1CardCode.Trim() + "'";
                String oCustQry =
                                  // 04-07-2022 begin
                                  /*              "select distinct coalesce(U_EDI_CHK_SERV_LEV,'N') as ChkServLev, coalesce([3PL],'N') Is3PL from OCRD t1 " +
                                                "left join InfocusEdi.dbo.WebApiDbContext t2 on t2.SBOCardCode = t1.CardCode " + */
                                  "select top 1 coalesce(U_EDI_CHK_SERV_LEV,'N') as ChkServLev, coalesce([3PL],'N') Is3PL from OCRD t1 With(NOLOCK) " +
                                   "join InfocusEdi.dbo.WebApiDbContext t2 With(NOLOCK) on t2.SBOCardCode COLLATE SQL_Latin1_General_CP850_CI_AS = t1.CardCode " +
                                  // 04-07-2022 end
                                  "where t1.CardCode  = '" + pB1CardCode.Trim() + "' and  t2.CardCode = '" + pEdiCardCode.Trim() + "'";
                // 05-19-2021 end
                rs.DoQuery(oCustQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    string oChkSLev = rs.Fields.Item("ChkServLev").Value.ToString();
                    string oIs3PL = rs.Fields.Item("Is3PL").Value.ToString(); // 05-19-2021
                    if ((oChkSLev != null && oChkSLev.ToUpper() == "Y")
                        || (oIs3PL != null && oIs3PL.ToUpper() == "Y")) // 05-19-2021
                    {
                        bCheckSLev = true;
                    }
                    else
                    {
                        bCheckSLev = false;
                    }
                }
            }
            catch (Exception x)
            {
                String ErrorMesg = x.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error getting flag for Service Level check for " + pB1CardCode + " Error=> " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error getting flag for Service Level check for " + pB1CardCode + " Error=> " + ErrorMesg);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return bCheckSLev;
        }
        // 03-05-2020 end

        // 10-11-2019 begin
        private bool checkSCAC(String pPONo, String pCardCode, string pcarrierCode, string pserviceLevel, string pIs3PL, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bValidSCAC = false;
            try
            {
                // 05-19-2021 begin
                if (pIs3PL == "Y")
                {
                    int oshpCount = 0;
                    String oQry = "select COUNT(IsNull(U_TrgSCAC,'')) RecCount from [@C3_CSCACC] With(NOLOCK) where U_CardCode = '" + pCardCode.Trim() + "' and U_SrcSCAC = '" +
                                        pcarrierCode.Trim() + "' and IsNull(U_SrcService,'') = '" + pserviceLevel.Trim() + "'";
                    if (String.IsNullOrWhiteSpace(pserviceLevel))
                    {
                        oQry = "select COUNT(IsNull(U_TrgSCAC,'')) RecCount from [@C3_CSCACC] With(NOLOCK) where U_CardCode = '" + pCardCode.Trim() + "' and U_SrcSCAC = '" +
                                      pcarrierCode.Trim() + "'";
                    }

                    rs.DoQuery(oQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        string oValue = rs.Fields.Item("RecCount").Value.ToString();
                        try
                        {
                            oshpCount = Convert.ToInt32(oValue);
                        }
                        catch
                        {
                            oshpCount = 0;
                        }
                    }
                    else
                    {
                        oshpCount = 0;
                    }
                    if (oshpCount > 0)
                    {
                        bValidSCAC = true;
                    }
                    else
                    {
                        bValidSCAC = false;
                    }
                }
                else
                { // 05-19-2021 end
                    String oCustQry = "select top 1 coalesce(U_EDI_SHIP_TYPE,'N') as SetShipType, coalesce(U_EDI_CHK_SERV_LEV,'N') as ChkServLev from OCRD t1 With(NOLOCK) where CardCode  = '" + pCardCode.Trim() + "'";
                    rs.DoQuery(oCustQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        string oValue = rs.Fields.Item("SetShipType").Value.ToString();
                        string oChkSLev = rs.Fields.Item("ChkServLev").Value.ToString();// 03-05-2020
                        string oQry = "";
                        if (oValue != null && oValue.ToUpper() == "Y")
                        {
                            int oshpCount = 0;
                            // 03-05-2020 begin
                            if (oChkSLev != null && oChkSLev.ToUpper() == "Y")
                            {
                                string oShipCode = pcarrierCode.Trim();
                                if (!(String.IsNullOrWhiteSpace(pserviceLevel)))
                                {
                                    oShipCode = oShipCode.Trim() + "_" + pserviceLevel.Trim();
                                }
                                oQry = "select COUNT(TrnspCode) RecCount from [OSHP] With(NOLOCK) where WebSite = '" + oShipCode.Trim() + "'";
                            }
                            else
                            {
                                // 03-05-2020 end
                                if (!String.IsNullOrWhiteSpace(pserviceLevel)) // && pserviceLevel.Trim().Length >= 4)
                                {
                                    oQry = "select COUNT(TrnspCode) RecCount from [OSHP] With(NOLOCK) where WebSite = '" + pserviceLevel.Trim() + "'";
                                }
                                else
                                {
                                    oQry = "select COUNT(TrnspCode) RecCount from [OSHP] With(NOLOCK) where WebSite = '" + pcarrierCode.Trim() + "' or TrnspName = '" +
                                               pcarrierCode.Trim() + "'";
                                }
                            } // 03-05-2020
                            rs.DoQuery(oQry);
                            rs.MoveFirst();
                            if (!rs.EoF)
                            {
                                oValue = rs.Fields.Item("RecCount").Value.ToString();
                                try
                                {
                                    oshpCount = Convert.ToInt32(oValue);
                                }
                                catch
                                {
                                    oshpCount = 0;
                                }
                            }
                            else
                            {
                                oshpCount = 0;
                            }
                            if (oshpCount > 0)
                            {
                                bValidSCAC = true;
                            }
                            else
                            {
                                bValidSCAC = false;
                            }
                        }
                        else
                        {
                            bValidSCAC = true;
                        }
                    }
                } // 05-19-2021
            }
            catch (Exception x)
            {
                String ErrorMesg = x.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error validating shipping type for " + pPONo + " => " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error validating shipping type for " + pPONo + " Error=> " + ErrorMesg);
                }
                bValidSCAC = false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return bValidSCAC;
        }
        // 10-11-2019 end

        // 09-24-2022 begin
        private bool checkC3Shipper(String pSBOCardCode, String pCarrierCode, String pSTC, bool autoImport, string pIs3PL)
        {
            // 2025 SOW Quote comments
            // will need to add param for useMatrix and modify if statement to check UseMatrix = 'Y'
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            int oDataCount = 0;
            bool bValid = false;

            if (pSBOCardCode.ToUpper().StartsWith("3PL-")) // 04-22-2022
            {
                try
                {
                    String oCustQry = "select  t0.U_ShipperCode as ShipperCode, t0.U_Acct as Account,  t0.U_ShipType as ShipType  " +
                                                 "from [@C3_DEFSHIPPER] t0  WITH(NOLOCK) left join [@C3_CSCACC] t1  WITH(NOLOCK) on t0.U_CustCode = t1.U_CardCode " +
                                                "left join OSHP t2  WITH(NOLOCK) on t2.WebSite = t1.U_TrgSCAC " +
                                                "where t0.U_CustCode = '" + pSBOCardCode.Trim() + "'  and t0.U_STC = '" + pSTC.Trim() + "' and " +
                                                " t0.U_ShipType = t2.U_COR_Carrier and t1.U_SrcSCAC = '" + pCarrierCode.Trim() + "'";
                    rs.DoQuery(oCustQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        try
                        {
                            String oShipper = rs.Fields.Item("ShipperCode").Value.ToString();
                            if (!String.IsNullOrWhiteSpace(oShipper))
                            {
                                oDataCount = oDataCount + 1;
                            }
                        }
                        catch (Exception e1)
                        {

                        }
                        try
                        {
                            String oAcct = rs.Fields.Item("Account").Value.ToString();
                            if (!String.IsNullOrWhiteSpace(oAcct))
                            {
                                oDataCount = oDataCount + 1;
                            }
                        }
                        catch (Exception e2)
                        {
                        }
                        try
                        {
                            String oShpType = rs.Fields.Item("ShipType").Value.ToString();
                            if (!String.IsNullOrWhiteSpace(oShpType))
                            {
                                oDataCount = oDataCount + 1;
                            }
                        }
                        catch (Exception e3)
                        {
                        }
                    }
                }
                catch (Exception sh)
                {
                    String ErrorMesg = sh.Message;
                    if (autoImport)
                    {
                        Import_Log.LogEntry("Error checking C3_Shipper & Account# for " + pSBOCardCode + " Carrier " + pCarrierCode + " STC " + pSTC + " Error=> " + ErrorMesg);
                    }
                    else
                    {
                        _logger.Debug("Error checking C3_Shipper & Account# for " + pSBOCardCode + " Carrier " + pCarrierCode + " STC " + pSTC + " Error=> " + ErrorMesg);
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                }
            }
            if (oDataCount > 0)
            {
                bValid = true;
            }
            return bValid;
        }
        // 09-24-2022 end

        // 10-22-2021 begin
        private bool checkPaymentType(int pHeaderId, String pSBOCardCode, string pPaymentType, bool autoImport)
        {
            bool bValid = true;
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            // 03-23-2026 lrussell begin
            if (String.IsNullOrWhiteSpace(pPaymentType) || String.IsNullOrEmpty(pPaymentType))
            {
                pPaymentType = string.Empty;
            }
            // 03-23-2026 lrussell end
            string oAllowPP = "Y";
            try
            {
                String oQry = "select IsNull(AllowPP, 'Y') as allowPP from [CorLog_Allow_PrePaid] With(NOLOCK) where CardCode = '" + pSBOCardCode.Trim() + "'";
                rs.DoQuery(oQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    string oValue = rs.Fields.Item("allowPP").Value.ToString();
                    try
                    {
                        oAllowPP = oValue;
                    }
                    catch (Exception xp)
                    {
                        oAllowPP = "N";
                        String ErrorMesg = xp.Message;
                        if (autoImport)
                        {
                            Import_Log.LogEntry("Errror checking Prepaid for HeaderId = " + pHeaderId + " Error=> " + ErrorMesg);
                        }
                        else
                        {
                            _logger.Debug("Error checking Prepaid for HeaderId = " + pHeaderId + " Error=> " + ErrorMesg);
                        }
                    }
                }

                // 07-07-2023 begin

                if (oAllowPP == "N" && pPaymentType.ToUpper() == "PP")
                {
                    oQry = "select Count(t0.BuyerItemCode) as 'NoItems' from [CorLog_Allow_PrePaid_Items] t0 with(nolock) join InfocusEdi850DetailRecord t1 with(nolock) on t0.BuyerItemCode = t1.BuyerItemCode " +
                           "where CardCode = '" + pSBOCardCode.Trim() + "' and HeaderId = " + pHeaderId;
                    rs.DoQuery(oQry);
                    rs.MoveFirst();
                    if (!rs.EoF)
                    {
                        int oNoItems = 0;
                        try
                        {
                            oNoItems = Convert.ToInt32(rs.Fields.Item("NoItems").Value);
                        }
                        catch (Exception i2)
                        {
                            string oErr = i2.Message;
                            oNoItems = 0;
                        }
                        if (oNoItems > 0)
                        {
                            oAllowPP = "Y";
                        }
                    }
                }
                // 07-07-2023 end
                if (oAllowPP == "N" && pPaymentType.ToUpper() == "PP")
                {
                    bValid = false;
                }
            }
            catch (Exception x)
            {
                String ErrorMesg = x.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Errror checking Prepaid for HeaderId = " + pHeaderId + " Error=> " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error checking Prepaid for HeaderId = " + pHeaderId + " Error=> " + ErrorMesg);
                }
                bValid = false;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return bValid;
        }
        // 10-22-2021 end

        // 10-24-2019 begin
        private bool checkItems(int pHeaderId, String pCardCode, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bInvalidItems = false;
            try
            {
                int NoInvalidItems = 0;
                String oCustQry = "select COUNT(LineNumber) NoInvalidItms from [Infocus_EDI_Invalid_items] t1 With(NOLOCK) where SBOCardCode  = '" + pCardCode.Trim() + "' and HeaderId = " + pHeaderId;
                rs.DoQuery(oCustQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    string oValue = rs.Fields.Item("NoInvalidItms").Value.ToString();
                    try
                    {
                        NoInvalidItems = Convert.ToInt32(oValue);
                    }
                    catch
                    {
                        NoInvalidItems = 0;
                    }
                }
                else
                {
                    NoInvalidItems = 0;
                }
                if (NoInvalidItems > 0)
                {
                    bInvalidItems = true;
                }
                else
                {
                    bInvalidItems = false;
                }

            }
            catch (Exception x)
            {
                String ErrorMesg = x.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error validating items for HeaderId = " + pHeaderId + " Error=> " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error validating items for HeaderId = " + pHeaderId + " Error=> " + ErrorMesg);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return bInvalidItems;
        }
        // 10-24-2019 end

        // 06-28-2019 begin
        private bool checkPrice(String pPONo, String pCardCode, int pHeaderId, bool autoImport)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            bool bVariance = false;
            try
            {
                String oCustQry = "select top 1 IsNull(U_EDI_PRICE_CHK,'N') as CheckPrcVar from OCRD t1 With(NOLOCK) where CardCode  = '" + pCardCode.Trim() + "'";
                rs.DoQuery(oCustQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    string oValue = rs.Fields.Item("CheckPrcVar").Value.ToString();
                    if (oValue != null && oValue.ToUpper() == "Y")
                    {
                        int PriceVarCount = 0;
                        rs.DoQuery("select COUNT(HeaderId) RecCount from [Infocus_EDI_PriceVar] With(NOLOCK) where HeaderId = " + pHeaderId);
                        rs.MoveFirst();
                        if (!rs.EoF)
                        {
                            oValue = rs.Fields.Item("RecCount").Value.ToString();
                            try
                            {
                                PriceVarCount = Convert.ToInt32(oValue);
                            }
                            catch
                            {
                                PriceVarCount = 0;
                            }
                        }
                        else
                        {
                            PriceVarCount = 0;
                        }
                        if (PriceVarCount > 0)
                        {
                            bVariance = true;
                        }
                        else
                        {
                            bVariance = false;
                        }
                    }
                    else
                    {
                        bVariance = false;
                    }
                }
            }
            catch (Exception x)
            {
                String ErrorMesg = x.Message;
                if (autoImport)
                {
                    Import_Log.LogEntry("Error validating price for " + pPONo + " Error=> " + ErrorMesg);
                }
                else
                {
                    _logger.Debug("Error validating price for " + pPONo + " Error=> " + ErrorMesg);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return bVariance;
        }

        private void set850Error(String pErrorMessage, Int32 pHeaderId, bool autoImport, bool setProcessed)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            Infocus.WebApi.Common.Bone.Import_Log._850Error = pErrorMessage;
            try
            {
                String oQry = "UPDATE InfocusEdi850HeaderRecord set ErrorMessage = '" + pErrorMessage.Trim() + "'";
                // 04-21-2022 begin
                if (setProcessed == true)
                {
                    oQry = oQry + ", Processed=1 ";
                }
                // 04-21-2022 end
                oQry = oQry + " where Processed = 0 and HeaderId = " + pHeaderId;

                rs.DoQuery(oQry);
            }
            catch (Exception x)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error setting error message for HeaderId = " + pHeaderId + " Error=> " + x.Message);
                }
                else
                {
                    _logger.Error("Error setting error message for HeaderId = " + pHeaderId + " Error=> " + x.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }
        // 06-28-2019 end

        // 07-26-2023 begin
        private void set940Error(String pErrorMessage, Int32 pHeaderId, bool autoImport, bool setProcessed)
        {
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            Infocus.WebApi.Common.Bone.Import_Log._940Error = pErrorMessage;
            try
            {
                String oQry = "UPDATE InfocusEdi940HeaderRecord set ErrorMessage = '" + pErrorMessage.Trim() + "'";
                if (setProcessed == true)
                {
                    oQry = oQry + ", Processed=1 ";
                }
                oQry = oQry + " where Processed = 0 and HeaderId = " + pHeaderId;

                rs.DoQuery(oQry);
            }
            catch (Exception x)
            {
                if (autoImport)
                {
                    Import_Log.LogEntry("Error setting error message for HeaderId = " + pHeaderId + " Error=> " + x.Message);
                }
                else
                {
                    _logger.Error("Error setting error message for HeaderId = " + pHeaderId + " Error=> " + x.Message);
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
        }
        // 07-26-2023 end

        // 05-1-2020 moved process to sales order creation
        /*
        // 10-08-2019 begin
        private void setLineTransportCode(string docEntry, string transportCode, bool autoImport)
        {
            if (!String.IsNullOrWhiteSpace(transportCode))
            {
                Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
                _logger.Debug("Setting Line Transport Code from Header for DocEntry " + docEntry);
                try
                {
                    int tCode = Convert.ToInt32(transportCode);
                    String oQry = "UPDATE RDR1 set TrnsCode = " + tCode +
                        " where DocEntry = " + docEntry;

                    rs.DoQuery(oQry);
                }
                catch (Exception x)
                {
                    _logger.Error("Error setting line transport code => " + x.Message);
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                }
            }
        }
        // 10-08-2019 end
        */
        private Int32 GetDocNum(Int32 docEntry)
        {
            Int32 docNum = 0;
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                rs.DoQuery("select top 1 DocNum from ORDR With(NOLOCK) where DocEntry = " + docEntry);
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
