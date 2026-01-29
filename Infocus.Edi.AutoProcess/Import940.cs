using SAPbobsCOM;
using System.Data.SqlClient;
using System;
using System.Linq;
using Infocus.Common;
using Infocus.WebApi.Data;
using Infocus.WebApi.Common.Bone;
using log4net;

namespace Infocus.Edi.AutoProcess
{
    class Import940
    {

        public static void DoUpdate()
        {
            try
            {
                BoneSalesOrderDataLayer salesOrderDAL = new BoneSalesOrderDataLayer(InfocusEdiAutoProcess.oCompany);
                Company _company = salesOrderDAL.getCompany();

                Int32 oDelaySec = 0;
                try
                {
                    oDelaySec = AppSettings.Instance.ImpDelay;
                }
                catch
                {
                    oDelaySec = 0;
                }
                // 08-08-2023 begin
                Int32 oMaxRows = 0;
                try
                {
                    oMaxRows = AppSettings.Instance.MaxRows;
                }
                catch
                {
                    oMaxRows = 0;
                }
                // 08-08-2023 end
                // 08-14-2023 begin
                Int32 oMaxTD = 0;
                try
                {
                    oMaxTD = AppSettings.Instance.MaxTD;
                }
                catch
                {
                    oMaxTD = 0;
                }
                // 08-14-2023 end
                try
                {
                    String oConnectionName = InfocusEdiAutoProcess.oSqlConnection.ConnectionString;
                    if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                    {
                        Import_Log.LogEntry("Unable to connect to " + InfocusEdiAutoProcess.oCompany.CompanyDB);
                    }
                    else
                    {
                        DateTime oStartDate = DateTime.Now;
                        oStartDate = oStartDate.AddDays(-5);

                        using (WebApiDbContext webApiDbContext = new WebApiDbContext(InfocusEdiAutoProcess.oSqlConnection, false))
                        {
                            var records = (from recs in webApiDbContext.Edi940HeaderRecords.OrderBy(lor => lor.HeaderId)
                                           where recs.Processed == false
                                           select recs).ToList();
                            int ctr = 0;
                            int iLast = 0;
                            int iRow = 0;  

                            //if (records.Count > 350)
                            if (records.Count > oMaxRows && oMaxRows > 0)
                            {
                                //iLast = 350;
                                iLast = oMaxRows; // 08-08-2023
                            }
                            else
                            {
                                iLast = records.Count;
                            }
                            iLast = records.Count;
                            Import_Log.LogEntry(iLast + " Rows to process ");
                            foreach (var record in records)
                            {
                                if (iRow < iLast)
                                {
                                    iRow = iRow + 1;
                                    string oCardCode = record.SBOCardCode;
                                    string oProcess = getAutoProcess(_company, oCardCode);
                                    if (oProcess == "Y")
                                    {
                                        Import_Log.LogEntry("Processing 940 with Record Key " + record.HeaderId + ", CardCode: " + record.CardCode + ", PO#" + record.PurchaseOrderReference);
                                        try
                                        {
                                            WebApi.Common.Bone.Import_Log._940Error = String.Empty;
                                            var documentKey = salesOrderDAL.Process940Record(record, true, "940");
                                            int iNext = ++ctr;

                                            if (!(documentKey == null) && documentKey.DocEntry > 0)
                                            {
                                                Import_Log.LogEntry("Successfully created sales order");
                                                DateTime now = DateTime.Now;
                                                record.Processed = true;
                                                record.ProcessedDateTime = now;
                                                record.SalesOrderKey = documentKey.DocEntry;
                                                webApiDbContext.SaveChanges();
                                                String msg = "Sucessfully processed 940 Key #" + record.HeaderId + ", CardCode: " + record.CardCode + ", PO# " + record.PurchaseOrderReference + ", Sales Order #" + documentKey.DocNum;
                                                Import_Log.LogEntry(msg);
                                                int oSalesOrderNo = documentKey.DocEntry;
                                                UpdateSOLineNo(_company, oSalesOrderNo); // 02-02-2022
                                                // InfocusEdiAutoProcess.updateEDILog(oCardCode, 940, record.HeaderId, msg);
                                            }
                                            else
                                            {
                                                //String msg = "Errors processing 940 Key #" + record.HeaderId + ", CardCode: " + record.CardCode + ", PO# " + record.PurchaseOrderReference + " Sales Order not created";
                                                String msg = "Errors processing 940 Key #" + record.HeaderId + ", PO# " + record.PurchaseOrderReference + " Sales Order not created";
                                                //InfocusEdiAutoProcess.updateEDILog(oCardCode, 940, record.HeaderId, msg);
                                                InfocusEdiAutoProcess.setErrorMessage(record.HeaderId, Infocus.WebApi.Common.Bone.Import_Log._940Error, false); // 06-29=2019
                                                record.ErrorMessage = Infocus.WebApi.Common.Bone.Import_Log._940Error; // 07-15-2021                                                
                                                Import_Log.LogEntry(msg);
                                                Import_Log.LogEntry(Infocus.WebApi.Common.Bone.Import_Log._940Error); // 07-15-2021 
                                            }
                                        }
                                        catch (Exception s0)
                                        {
                                            string oErrMesg = s0.Message;
                                            String msg = "Error processing 940 Key #" + record.HeaderId + "=> " + oErrMesg;
                                            Import_Log.LogEntry(msg);
                                            try
                                            {
                                                string oStackTrace = s0.StackTrace;
                                                Import_Log.LogEntry(oStackTrace);
                                            }
                                            catch
                                            {

                                            }
                                            msg = "Errors processing 940 Key #" + record.HeaderId;
                                            //InfocusEdiAutoProcess.updateEDILog(oCardCode, 940, record.HeaderId, msg);
                                        }
                                        try
                                        {
                                            oDelaySec = oDelaySec * 1000;
                                            System.Threading.Tasks.Task.Delay(oDelaySec); // pause between creating sales orders per Dan

                                        }
                                        catch
                                        {

                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Import_Log.LogEntry(ex.Message);
                    // ExceptionForm.ShowModal(ex);
                    String oErrMsg = ex.Message;
                    System.Windows.Forms.MessageBox.Show(oErrMsg);
                }
                finally
                {

                }
            }

            catch
            {
                Import_Log.LogEntry("Error during transaction update");
            }
            Import_Log.LogEntry("Finished processing 940 transactions");
        }


        public static void UpdateSOLineNo(SAPbobsCOM.Company _company, int docEntry)
        {
            Import_Log.LogEntry("Check & Update null U_InfoW2LNo for SO DocEntry = " + docEntry.ToString());
            _company.StartTransaction();
            try
            {
                Documents salesOrder = _company.GetBusinessObject(BoObjectTypes.oOrders) as Documents;
                salesOrder.GetByKey(docEntry);
                Document_Lines soLines = salesOrder.Lines;
                int noLines = soLines.Count;
                for (int i = salesOrder.Lines.Count - 1; i >= 0; i--)
                {
                    salesOrder.Lines.SetCurrentLine(i);
                    string lineNo = salesOrder.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value.ToString();
                    if (String.IsNullOrWhiteSpace(lineNo))
                    {
                        salesOrder.Lines.UserFields.Fields.Item("U_InfoW2LNo").Value = 0;
                        salesOrder.Update();
                    }
                }
            }
            catch (Exception e)
            {
                Import_Log.LogEntry("Error updating null U_InfoW2LNo for 940 with SalesOrderKey = " + docEntry.ToString() + " => " + e.Message);
            }
            // 02-16-2022 begin
            finally
            {
                if (_company.InTransaction)
                {
                    _company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
            }

        }

        private static String getAutoProcess(SAPbobsCOM.Company _company, String sboCardCode)
        {
            String oAutoProcess = "N";
            Recordset rs = _company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;
            try
            {
                string oQry = "select coalesce(U_Info850Auto,'N') as AutoProcess from OCRD With(NOLOCK) where CardCode  = '" + sboCardCode + "'";
                rs.DoQuery(oQry);
                rs.MoveFirst();
                if (!rs.EoF)
                {
                    oAutoProcess = (String)rs.Fields.Item(0).Value;
                }
            }
            catch (Exception e)
            {
                oAutoProcess = "N";
                Import_Log.LogEntry("Error getting auto Import flag for CardCode = " + sboCardCode + "=> " + e.Message);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return oAutoProcess;
        }

    }
}
