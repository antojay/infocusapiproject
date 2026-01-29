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
    class Import850
    {

        public static void DoUpdate()
        {
            try
            {
                BoneSalesOrderDataLayer salesOrderDAL = new BoneSalesOrderDataLayer(InfocusEdiAutoProcess.oCompany);
                Company _company = salesOrderDAL.getCompany();
                // 01-31-2023 begin
                Int32 oDelaySec = 0;
                try
                {
                    oDelaySec = AppSettings.Instance.ImpDelay;
                }
                catch
                {
                    oDelaySec = 0;
                }
                // 01-31-2023 end
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
                // 08-0142023 end

                try
                {
                    /*string oConnectionName = "Data Source=" + InfocusEdiAutoProcess.oServerName + ";Initial catalog=" + InfocusEdiAutoProcess.oDatabaseName +
                        ";User ID=" + InfocusEdiAutoProcess.oDbUser + ";Password=" + InfocusEdiAutoProcess.oDbPassword +";MultipleActiveResultSets=true";
                     */
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
                            var records = (from recs in webApiDbContext.Edi850HeaderRecords.OrderBy(lor => lor.HeaderId)
                                           where recs.Processed == false //&& recs.CardCode == "LowesNet" 
                                           //&& recs.ErrorMessage.Trim().Length == 0
                                           select recs).ToList();
                            int ctr = 0;
                            int iLast = 0;
                            int iRow = 0;  // 05-15-2020

                            //if (records.Count > 350)
                            Import_Log.LogEntry(oMaxRows + " Max Rows to process "); // 09-18-2023
                        
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
                            Import_Log.LogEntry(iLast + " Rows to process "); // 05-15-2020
                            foreach (var record in records)
                            {
                                if (iRow < iLast)
                                { // 05-15-2020
                                    iRow = iRow + 1;
                                    string oCardCode = record.SBOCardCode;
                                    string oProcess = getAutoProcess(_company, oCardCode);
                                    if (oProcess == "Y")
                                    {
                                        Import_Log.LogEntry("Processing 850 with Record Key " + record.HeaderId + ", CardCode: " + record.CardCode + ", PO#" + record.PurchaseOrderReference);
                                        try
                                        {
                                            WebApi.Common.Bone.Import_Log._850Error = String.Empty; // 07-21-2019
                                            var documentKey = salesOrderDAL.Process850Record(record, true, "850");
                                            int iNext = ++ctr;

                                            if (documentKey.DocEntry > 0)
                                            {
                                                Import_Log.LogEntry("Successfully created sales order");
                                                DateTime now = DateTime.Now;
                                                record.Processed = true;
                                                record.ProcessedDateTime = now;
                                                record.SalesOrderKey = documentKey.DocEntry;
                                                webApiDbContext.SaveChanges();
                                                String msg = "Sucessfully processed 850 Key #" + record.HeaderId + ", CardCode: " + record.CardCode + ", PO# " + record.PurchaseOrderReference + ", Sales Order #" + documentKey.DocNum;
                                                Import_Log.LogEntry(msg);
                                                int oSalesOrderNo = documentKey.DocEntry;
                                                UpdateSOLineNo(_company, oSalesOrderNo); // 02-02-2022
                                                // InfocusEdiAutoProcess.updateEDILog(oCardCode, 850, record.HeaderId, msg);
                                            }
                                            else
                                            {
                                                //String msg = "Errors processing 850 Key #" + record.HeaderId + ", CardCode: " + record.CardCode + ", PO# " + record.PurchaseOrderReference + " Sales Order not created";
                                                String msg = "Errors processing 850 Key #" + record.HeaderId + ", PO# " + record.PurchaseOrderReference + " Sales Order not created";
                                                //InfocusEdiAutoProcess.updateEDILog(oCardCode, 850, record.HeaderId, msg);
                                                InfocusEdiAutoProcess.setErrorMessage(record.HeaderId, Infocus.WebApi.Common.Bone.Import_Log._850Error, false); // 06-29=2019
                                                record.ErrorMessage = Infocus.WebApi.Common.Bone.Import_Log._850Error; // 07-15-2021                                                
                                                Import_Log.LogEntry(msg);
                                                Import_Log.LogEntry(Infocus.WebApi.Common.Bone.Import_Log._850Error); // 07-15-2021 
                                            }
                                        }
                                        catch (Exception s0)  // 05-21-2018  begin
                                        {
                                            string oErrMesg = s0.Message;
                                            String msg = "Error processing 850 Key #" + record.HeaderId + "=> " + oErrMesg;
                                            Import_Log.LogEntry(msg);
                                            try
                                            {
                                                string oStackTrace = s0.StackTrace;
                                                Import_Log.LogEntry(oStackTrace);
                                            }
                                            catch
                                            {

                                            }
                                            msg = "Errors processing 850 Key #" + record.HeaderId;
                                            //InfocusEdiAutoProcess.updateEDILog(oCardCode, 850, record.HeaderId, msg);
                                        }
                                        try
                                        {
                                            // 01-31-2023 begin
                                            // System.Threading.Tasks.Task.Delay(4000); // pause 4 seconds between creating sales orders per Dan 01-23-2023

                                            oDelaySec = oDelaySec * 1000;
                                            System.Threading.Tasks.Task.Delay(oDelaySec); // pause between creating sales orders per Dan
                                            // 01-31-2023 end
                                        }
                                        catch
                                        {

                                        }
                                    }
                                }  // 05-15-2015
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
            Import_Log.LogEntry("Finished processing 850 transactions"); // 04-13-2012
        }

        // 02-02-2022 begin
        public static void UpdateSOLineNo(SAPbobsCOM.Company _company, int docEntry)
        {
            Import_Log.LogEntry("Check & Update null U_InfoW2LNo for SO DocEntry = " + docEntry.ToString());
            _company.StartTransaction(); // 02-16-2022
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
                Import_Log.LogEntry("Error updating null U_InfoW2LNo for 850 with SalesOrderKey = " + docEntry.ToString() + " => " + e.Message);
            }
            // 02-16-2022 begin
            finally
            {
                if (_company.InTransaction)
                {
                    _company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
            }
            // 02-16-2022 end
        }
        // 02-02-2022 end
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
                Import_Log.LogEntry("Error getting auto 850 Import flag for CardCode = " + sboCardCode + "=> " + e.Message);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
            }
            return oAutoProcess;
        }

    }
}
