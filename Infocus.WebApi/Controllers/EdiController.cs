using Infocus.WebApi.Common;
using Infocus.WebApi.Common.Bone;
using Infocus.WebApi.Data;
using Infocus.WebApi.Data.Models;
using log4net;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace Infocus.WebApi.Controllers
{
    public class EdiController : BaseWebApiController
    {
        private static ILog _logger = LogManager.GetLogger(typeof(EdiController));
        private static readonly String ShipmentQuery =
@"select top 1 TrnspName
from OSHP   WITH(NOLOCK)
where TrnspCode = {0}";
        private static readonly String PaymentGroupQuery =
@"select top 1 PymntGroup, ExtraDays
from OCTG  WITH(NOLOCK)
where GroupNum = {0}";

        // 05-23-2021 begin
        private static readonly String ShipTypeQuery =
@"select top 1 IsNull(U_InfoW2Tm,'') ShipType
from OSHP  WITH(NOLOCK)
where TrnspCode = {0}";
        public decimal oShipmentWgt = 0;
        public decimal oShpCartons = 0;
        // 05-23-2021 end

        // 08-02-2022 begin
        public SAPbobsCOM.Company getCompany()
        {
            try
            {
                BusinessOneRuntimeContext _instance = Infocus.WebApi.Common.Bone.BusinessOneRuntimeContext.Instance;
                SAPbobsCOM.Company _Company = _instance.GetCompany();
                // 03-17-2026 lrussell begin
                if (_Company == null)
                {
                    throw new Exception("Unable to retrieve company from instance -- EdiController line 46");
                }
                // 03-17-2026 lrussell end
                return _Company;
            }
            catch (Exception e)
            {
                return null;
            }
        }
        // 08-02-2022 end

        // 05-25-2021 begin  
        public Int32 incrementNextASN(String pSBOCardCode, Int32 pNextASN, String pConnectionName)
        {
            Int32 oNextASN = pNextASN + 1;
            try
            {
                // 08-02-2022 begin
                // remove direct update and change to use API
                //BusinessOneRuntimeContext _instance = Infocus.WebApi.Common.Bone.BusinessOneRuntimeContext.Instance;
                //SAPbobsCOM.Company _Company = _instance.GetCompany();
                SAPbobsCOM.Company _Company = getCompany();
                bool bConnected = _Company.Connected;
                if (bConnected)
                {
                    SAPbobsCOM.BusinessPartners oBP = _Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oBusinessPartners) as SAPbobsCOM.BusinessPartners;
                    try
                    {
                        bool bFound = oBP.GetByKey(pSBOCardCode.Trim());
                        if (bFound)
                        {
                            string cardCode = oBP.CardCode;
                            Int32 iNextASN = Convert.ToInt32(oNextASN);
                            oBP.UserFields.Fields.Item("U_InfoNextASN").Value = iNextASN;
                            int oRet = oBP.Update();
                            if (oRet != 0)
                            {
                                String oErrMsg = _Company.GetLastErrorDescription();
                                int oErrCode = _Company.GetLastErrorCode();
                                oErrMsg = oErrMsg + ", ErrorCode " + oErrCode.ToString();
                                _logger.Error("Error updating BP next ASN number => " + oErrMsg);
                            }
                        }
                    }
                    catch (Exception t)
                    {
                        string oErrMsg = t.Message;
                    }
                    /*string oQuery = "update OCRD set U_InfoNextASN = " + oNextASN.ToString() + " where CardCode = '" + pSBOCardCode.Trim() + "'";
                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                    {
                        sqlConnection.Open();
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception inv1)
                        {
                            _logger.Error("Error updating invoice edi line numbers => " + inv1.Message);
                        }
                        sqlConnection.Close();
                    }
               
                            }
                            catch (Exception crd1)
                            {
                                _logger.Error("Error updating BP next ASN number => " + crd1.Message);

                            } */
                    // 06-22-2022 end
                }
            }
            catch (Exception e)
            {
                oNextASN = pNextASN;
                string oMessage = e.Message;
            }
            return oNextASN;
        }
        // 05-25-20201 end

        // 08-17-2023 begin
        public Int32 getMaxTD(String pConnectionName)
        {
            Int32 oMaxTD = 0;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand("select IsNull(U_MaxTD,0) as MaxTD from dbo.[@INFO_W2_SETTINGS] where Code = 'DEFAULT'", sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string oValid = reader["MaxTD"].ToString();
                                oMaxTD = Convert.ToInt32(oValid);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
                _logger.Error("Error getting Max # of days for Last Trx: " + oMessage);
            }
            return oMaxTD;
        }
        // 08-17-2023 end

        // 03-14-2022 begin TEST
        public Int32 getEDIRecords(String pQuery, String pConnectionName)
        {
            int oCount = 0;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(pQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string oValid = (String)reader["NoRows"];
                                oCount = Convert.ToInt32(oValid);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                string oMessage = e.Message;
            }
            return oCount;
        }
        // 03-14-2022 end

        // 03-08-2019 begin
        //private string get850DetailItemUPC(string pConnectionName, int headerId, int LineNo)
        private string get850DetailItemUPC(string pConnectionName, int headerId, string LineNo) // 02-27-2022
        {
            string oItemUPC = "";
            try
            {
                string oQuery = "select coalesce(i0.ItemUPC,'') ItemUPC from dbo.InfocusEdi850DetailRecord i0 WITH(NOLOCK) where i0.HeaderId = " + headerId +
                    " and i0.LineNumber = " + LineNo;
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oItemUPC = "";
                            }
                            else
                            {
                                oItemUPC = (String)reader["ItemUPC"];
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                oItemUPC = "";
                string oMessage = e.Message;
            }
            return oItemUPC;
        }

        //private Double get850DetailRetailPrice(string pConnectionName, int headerId, int LineNo)
        private Double get850DetailRetailPrice(string pConnectionName, int headerId, string LineNo) // 02-27-2022
        {
            Double oRetailPrice = Convert.ToDouble(0);
            try
            {
                string oQuery = "select coalesce(i0.retailPrice,0) RetailPrice from dbo.InfocusEdi850DetailRecord i0  WITH(NOLOCK) where i0.HeaderId = " + headerId + " and i0.LineNumber = " + LineNo;
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oRetailPrice = Convert.ToDouble(0);
                            }
                            else
                            {
                                string retailPrice = reader["RetailPrice"].ToString();
                                oRetailPrice = Convert.ToDouble(retailPrice);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                oRetailPrice = Convert.ToDouble(0);
                string oMessage = e.Message;
            }
            return oRetailPrice;
        }
        // 03-08-2019 end

        // 05-15-2020 begin
        private int checkEDILnNo(string pConnectionName, string pTableId, int docNo, string pBaseType, string CardCode)
        {
            int oRowCount = 0;
            try
            {
                string pParentTable = "";
                if (pTableId == "DLN1")
                {
                    pParentTable = "ODLN";
                }
                else if (pTableId == "INV1")
                {
                    pParentTable = "OINV";
                }
                // 04-26-2022 undo changes from 04-07-2022
                /*string oQuery = "select COUNT(LineNum) as 'RowCount' from " + pTableId + " t0 left join " + pParentTable + " t1 on t0.DocEntry = t1.DocEntry " +
                          " left join InfocusEdi.dbo.WebAPIDbContext t2 on t1.CardCode = t2.SBOCardCode COLLATE SQL_Latin1_General_CP850_CI_AS " +
             "where t0.DocEntry = " + docNo + " and t0.BaseEntry in (select SalesOrderKey from InfocusEdi850HeaderRecord) and " +                  
                          // 04-07-2022 begin
             //" where t0.DocEntry = " + docNo + " and t0.BaseEntry in (select SalesOrderKey from InfocusEdi850HeaderRecord) and " +
                         "left join InfocusEdi850HeaderRecord t3 on t3.SBOCardCode = t1.CardCode and t3.PurchaseOrderReference = t1.NumAtCard " +
                         " and t0.BaseEntry = t3.SalesOrderKey and " +
             // 04-07-2022 end
                       
                         " t0.TreeType in ('S','N') and coalesce(t0.U_InfoW2LNo,0) = 0 and t0.BaseType = '" + pBaseType + "' and IsNull(t2.[3PL],'N') = 'N';";
                 */
                string oQuery = "select COUNT(LineNum) as 'RowCount' from dbo." + pTableId + " t0 WITH(NOLOCK) left join dbo." + pParentTable + " t1 WITH(NOLOCK) on t0.DocEntry = t1.DocEntry " +
                         " left join InfocusEdi.dbo.WebAPIDbContext t2 WITH(NOLOCK)  on t1.CardCode = t2.SBOCardCode  COLLATE SQL_Latin1_General_CP850_CI_AS " +
                        " where t0.DocEntry = " + docNo + " and t0.BaseEntry in (select SalesOrderKey from dbo.InfocusEdi850HeaderRecord) and " +
                        " t0.TreeType in ('S','N') and coalesce(t0.U_InfoW2LNo,0) = 0 and t0.BaseType = '" + pBaseType + "' and IsNull(t2.[3PL],'N') = 'N';";

                // _logger.Debug("Check EDI Ln# => " + oQuery);
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oRowCount = 0;
                            }
                            else
                            {
                                oRowCount = Convert.ToInt16(reader["RowCount"]);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                oRowCount = 0;
                string oMessage = e.Message;
            }
            return oRowCount;
        }
        // 02-21-2018 begin
        /*  public String getAltVendorItem(string pConnectionName, int pDocEntry, SOLine pSoLine)
          {
              string oAltVendorItem = "";
              string oQuery = "select coalesce(U_InfoVendorItem, coalesce(SubCatNum, ItemCode)) VendorItem from RDR1 where DocEntry = " + pDocEntry +
                              "and LineNum = " + pSoLine.LineNum;
              try
              {
                  string oConnectionString = GetConnectionString(pConnectionName);
                  using (SqlConnection sqlConnection = new SqlConnection(oConnectionString))
                  {
                      sqlConnection.Open();
                      using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                      {
                          using (SqlDataReader reader = command.ExecuteReader())
                          {
                              if (!reader.Read())
                              {
                                  oAltVendorItem = "";
                              }
                              else COUNT
         * 
                              {
                                  oAltVendorItem = (String)reader["VendorItem"];

                              }
                          }
                      }
                      sqlConnection.Close();
                  }
              }
              catch (Exception r)
              {

              }
              return oAltVendorItem;
          }
          public String getAltVendorItem(string pConnectionName, int pDocEntry, InvoiceLine pInvLine)
          {
              string oAltVendorItem = "";
              string oQuery = "select coalesce(U_InfoVendorItem, coalesce(SubCatNum, ItemCode)) VendorItem from INV1 where DocEntry = " + pDocEntry +
                              "and LineNum = " + pInvLine.LineNum;
              try
              {
                  string oConnectionString = GetConnectionString(pConnectionName);
                  using (SqlConnection sqlConnection = new SqlConnection(oConnectionString))
                  {
                      sqlConnection.Open();
                      using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                      {
                          using (SqlDataReader reader = command.ExecuteReader())
                          {
                              if (!reader.Read())
                              {
                                  oAltVendorItem = "";
                              }
                              else
                              {
                                  oAltVendorItem = (String)reader["VendorItem"];

                              }
                          }
                      }
                      sqlConnection.Close();
                  }
              }
              catch (Exception r)
              {

              }
              return oAltVendorItem;
          }
          public String getAltVendorItem(string pConnectionName, int pDocEntry, CreditMemoLine pCrLine)
          {
              string oAltVendorItem = "";
              string oQuery = "select coalesce(U_InfoVendorItem, coalesce(SubCatNum, ItemCode)) VendorItem from RIN1 where DocEntry = " + pDocEntry +
                              "and LineNum = " + pCrLine.LineNum;
              try
              {
                  string oConnectionString = GetConnectionString(pConnectionName);
                  using (SqlConnection sqlConnection = new SqlConnection(oConnectionString))
                  {
                      sqlConnection.Open();
                      using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                      {
                          using (SqlDataReader reader = command.ExecuteReader())
                          {
                              if (!reader.Read())
                              {
                                  oAltVendorItem = "";
                              }
                              else
                              {
                                  oAltVendorItem = (String)reader["VendorItem"];

                              }
                          }
                      }
                      sqlConnection.Close();
                  }
              }
              catch (Exception r)
              {

              }
              return oAltVendorItem;
          }
          // 02-21-2018 end
          */

        // 05-17-2020 begin
        private int checkZeroQty(string pConnectionName, string pTableId, string pParent, int pdocNo, string pBaseType)
        {
            int oRowCount = 0;
            try
            {
                // 04-07-2022 begin
                /*
                string oQuery = "select COUNT(LineNum) as 'RowCount' from " + pTableId +
                                " where DocEntry = " + docNo + " and BaseEntry in (select SalesOrderKey from InfocusEdi850HeaderRecord) and " +
                                "TreeType in ('S','N') and coalesce(" + pTableId + ".Quantity,0) = 0 and BaseType = '" + pBaseType + "';";
                 */
                string oQuery = "select COUNT(t0.LineNum) as 'RowCount' from dbo." + pTableId + " t0 WITH(NOLOCK)  left join dbo." + pParent + " t1 WITH(NOLOCK)  on t0.DocEntry = t1.DocEntry " +
                                         "left join dbo.InfocusEdi850HeaderRecord t2 WITH(NOLOCK) on t2.SBOCardCode = t1.CardCode and t2.PurchaseOrderReference = t1.NumAtCard " +
                                         "where t0.DocEntry = " + pdocNo + " and t0.BaseEntry = t2.SalesOrderKey and t0.TreeType in ('S','N') and IsNull(t0.Quantity,0) = 0 " +
                                         "and t0.BaseType = '" + pBaseType + "';";
                // 04-07-2022 end
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oRowCount = 0;
                            }
                            else
                            {
                                oRowCount = Convert.ToInt16(reader["RowCount"]);
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                oRowCount = 0;
                string oMessage = e.Message;
            }
            return oRowCount;
        }

        public void getDeliveryData(string connectionString, Delivery delivery)
        {
            foreach (DeliveryLine delLine in delivery.DeliveryLines)
            {
                if (delLine.Quantity == 0 && (delLine.TreeType == "S" || delLine.TreeType == "N"))
                {
                    decimal qty = getDelLineQty(connectionString, delLine);
                    if (qty > 0)
                    {
                        delLine.Quantity = qty;
                    }
                }
                // 06-07-2021 begin
                if (delLine.LineNumber850 == null)
                {
                    delLine.LineNumber850 = 0;
                }
                // 06-07-2021 end
                if (delLine.LineNumber850 == 0 && (delLine.TreeType == "S" || delLine.TreeType == "N"))
                {
                    int oLnNo = getDelLineNo(connectionString, delLine);
                    if (oLnNo > 0)
                    {
                        delLine.LineNumber850 = oLnNo;
                    }
                }
            }

        }
        public decimal getDelLineQty(string connectionString, DeliveryLine delLine)
        {
            decimal oQty = 0;
            string oQuery = "select coalesce(r1.Quantity,0) Quantity from dbo.RDR1 r1 WITH(NOLOCK) where r1.DocEntry = " + delLine.BaseEntry +
                            " and r1.LineNum = " + delLine.BaseLine;
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionString)))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                oQty = (decimal)reader[0];
                            }
                            catch (Exception n)
                            {
                                string oMessage = n.Message;
                                oQty = 0;
                            }
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oQty;
        }

        public int getDelLineNo(string connectionString, DeliveryLine delLine)
        {
            int oLnNo = 0;
            string oQuery = "select coalesce(r1.[U_InfoW2LNo],0) as 'LineNo' from dbo.RDR1 r1 WITH(NOLOCK)  where r1.DocEntry = " + delLine.BaseEntry +
                            " and r1.LineNum = " + delLine.BaseLine;
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionString)))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                oLnNo = (Int16)reader[0];
                            }
                            catch (Exception n)
                            {
                                string oMessage = n.Message;
                                oLnNo = 0;
                            }
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oLnNo;
        }

        public void getInvoiceData(string connectionString, Invoice invoice)
        {
            foreach (InvoiceLine invLine in invoice.InvoiceLines)
            {
                if (invLine.Quantity == 0 && (invLine.TreeType == "S" || invLine.TreeType == "N"))
                {
                    decimal qty = getInvLineQty(connectionString, invLine);
                    if (qty > 0)
                    {
                        invLine.Quantity = qty;
                    }
                }
                if (invLine.LineNumber850 == 0 && (invLine.TreeType == "S" || invLine.TreeType == "N"))
                {
                    int oLnNo = getInvLineNo(connectionString, invLine);
                    if (oLnNo > 0)
                    {
                        invLine.LineNumber850 = oLnNo;
                    }
                }
            }

        }
        public decimal getInvLineQty(string connectionString, InvoiceLine invLine)
        {
            decimal oQty = 0;
            string oTableId = "INV1";
            if (invLine.BaseType == 17)
            {
                oTableId = "RDR1";
            }
            string oQuery = "select coalesce(Quantity,0) Quantity from dbo." + oTableId + " WITH(NOLOCK)  where DocEntry = " + invLine.DocEntry + " and BaseEntry = " + invLine.BaseEntry +
                            " and LineNum = " + invLine.BaseLine;
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionString)))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                oQty = (decimal)reader[0];
                            }
                            catch (Exception n)
                            {
                                string oMessage = n.Message;
                                oQty = 0;
                            }
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oQty;
        }

        public int getInvLineNo(string connectionString, InvoiceLine invLine)
        {
            int oLnNo = 0;
            string oTableId = "DLN1";
            if (invLine.BaseType == 17)
            {
                oTableId = "RDR1";
            }
            string oQuery = "select coalesce(U_InfoW2LNo,0) as 'LnNo' from dbo." + oTableId + " WITH(NOLOCK)  where DocEntry = " + invLine.BaseEntry +
                            " and LineNum = " + invLine.BaseLine;
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionString)))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            try
                            {
                                oLnNo = (Int16)reader[0];
                            }
                            catch (Exception n)
                            {
                                string oMessage = n.Message;
                                oLnNo = 0;
                            }
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oLnNo;
        }
        // 05-17-2020 end
        // 02-28-2018 begin
        private int getInvNofromDelivery(String connectionString, int delNo)
        {
            int oInvNo = -1;
            string oQuery = "select top 1 t0.TrgetEntry InvNo from dbo.DLN1 t0 WITH(NOLOCK) left join dbo.ODLN t1 WITH(NOLOCK)  on t0.DocEntry = t1.DocEntry" +
                                    " where t0.TargetType = '13' and TreeType in ('S', 'N') and Canceled = 'N' and t0.DocEntry = " + delNo;
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionString)))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            oInvNo = -1;
                        }
                        else
                        {
                            try
                            {
                                oInvNo = (Int32)reader[0];
                            }
                            catch (Exception n)
                            {
                                string oMessage = n.Message;
                                oInvNo = -1;
                            }
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oInvNo;
        }

        private bool getNonInventory(string pCardCode)
        {
            bool bHasNonInventory = false;
            try
            {
                string oQuery = "select w0.HasNonInventory from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                bHasNonInventory = false;
                            }
                            else
                            {
                                bHasNonInventory = (bool)reader["HasNonInventory"];
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                bHasNonInventory = false;
                string oMessage = e.Message;
            }
            return bHasNonInventory;
        }
        // 01-11-2021 begin
        private bool getProcess855(string pCardCode)
        {
            bool bSend855 = false;
            try
            {
                string oQuery = "select IsNull(w0.Send855,'N') Send855 from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                bSend855 = false;
                            }
                            else
                            {
                                string send855 = (String)reader["Send855"];
                                if (send855 == "N")
                                {
                                    bSend855 = false;
                                }
                                else
                                {
                                    bSend855 = true;
                                }
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                bSend855 = false;
                string oMessage = e.Message;
            }
            return bSend855;
        }
        // 01-11-2021 end

        // 06-03-2022 begin
        private bool validateCardCode(string pCardCode)
        {
            bool bValid = false;
            try
            {
                string oQuery = "select IsNull(w0.SBOCardCode,'') SBOCardCode from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                bValid = false;
                            }
                            else
                            {
                                string sboCardCode = (String)reader["SBOCardCode"];
                                if (String.IsNullOrWhiteSpace(sboCardCode))
                                {
                                    bValid = false;
                                }
                                else
                                {
                                    bValid = true;
                                }
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                bValid = false;
                string oMessage = e.Message;
            }
            return bValid;
        }
        // 06-03-2022 end

        // 02-09-2021 begin
        private bool getProcess856Pack(string pCardCode)
        {
            bool bSend856 = false;
            try
            {
                string oQuery = "select IsNull(w0.Send856Pack,'N') Send856Pack from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                bSend856 = false;
                            }
                            else
                            {
                                string send856 = (String)reader["Send856Pack"];
                                if (send856 == "N")
                                {
                                    bSend856 = false;
                                }
                                else
                                {
                                    bSend856 = true;
                                }
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                bSend856 = false;
                string oMessage = e.Message;
            }
            return bSend856;
        }
        // 02-09-2021 end

        // 09-26-2019 begin
        private String getDefaultVendor(string pCardCode)
        {
            String vendorNumber = "";
            try
            {
                string oQuery = "select w0.VendorNumber from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                vendorNumber = "";
                            }
                            else
                            {
                                vendorNumber = (String)reader["VendorNumber"];
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                vendorNumber = "";
                string oMessage = e.Message;
            }
            return vendorNumber;
        }
        // 09-26-2019 end

        //private string get850DetailBuyerItem(string pConnectionName, int headerId, int LineNo)
        private string get850DetailBuyerItem(string pConnectionName, int headerId, string LineNo) // 02-27-2022
        {
            string oBuyerItemCode = "";
            try
            {
                string oQuery = "select coalesce(i0.BuyerItemCode,'') BuyerItemCode from dbo.InfocusEdi850DetailRecord i0 WITH(NOLOCK) where i0.HeaderId = " + headerId + " and i0.LineNumber = " + LineNo;
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oBuyerItemCode = "";
                            }
                            else
                            {
                                oBuyerItemCode = (String)reader["BuyerItemCode"];
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                oBuyerItemCode = "";
                string oMessage = e.Message;
            }
            return oBuyerItemCode;
        }
        // 02-28-2018 end

        // 03-07-2018 begin
        // private string get850DetailVendorItem(string pConnectionName, int headerId, int LineNo)
        private string get850DetailVendorItem(string pConnectionName, int headerId, string LineNo) // 02-27-2022
        {
            string oVendorItemCode = "";
            try
            {
                string oQuery = "select coalesce(i0.VendorItemCode,'') VendorItemCode from dbo.InfocusEdi850DetailRecord i0 WITH(NOLOCK) where i0.HeaderId = " + headerId + " and i0.LineNumber = " + LineNo;
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oVendorItemCode = "";
                            }
                            else
                            {
                                oVendorItemCode = (String)reader["VendorItemCode"];
                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception e)
            {
                oVendorItemCode = "";
                string oMessage = e.Message;
            }
            return oVendorItemCode;
        }
        // 03-07-2018 end

        // 01-17-2018 begin
        private string getConnectionName(string pCardCode)
        {
            string oWebApiDbContext = "WebApiDbContext";
            string oQuery = "select w0.WebApiConnectionName, w0.HasNonInventory from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            oWebApiDbContext = "WebApiDbContext";
                        }
                        else
                        {
                            oWebApiDbContext = (String)reader["WebApiConnectionName"];
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oWebApiDbContext;
        }

        private string getSBOCardCode(string pCardCode)
        {
            string oSBOCardCode = null;
            string oQuery = "select w0.SBOCardCode from InfocusEDI.dbo.WebApiDbContext w0 WITH(NOLOCK) where w0.CardCode = '" + pCardCode + "'";
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            oSBOCardCode = null;
                        }
                        else
                        {
                            oSBOCardCode = (String)reader["SBOCardCode"];
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oSBOCardCode;
        }
        // 01-17-2018 end
        // 01-24-2018 begin
        public double getBOMPrice(string pConnectionName, string pItemCode)
        {
            double oPrice = 0;
            string oQuery = "select coalesce(c0.Price,0) Price from dbo.[CORLOG_SALES_BOM_PRICE] c0 WITH(NOLOCK) where c0.Code = '" + pItemCode + "'";
            string oConnectionString = GetConnectionString(pConnectionName);
            using (SqlConnection sqlConnection = new SqlConnection(oConnectionString))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            oPrice = 0;
                        }
                        else
                        {
                            decimal oValue = (decimal)reader["Price"];
                            oPrice = Convert.ToDouble(oValue);
                        }
                    }
                }
                sqlConnection.Close();
            }
            return oPrice;
        }

        [HttpPost]
        public Add850RecordResponse Add850Record(Add850RecordRequest request)
        {
            _logger.Debug("Entering Add850Records for " + request.Edi850HeaderRecord.CardCode);
            WebApiDbContext dbContext = new WebApiDbContext("DefaultErrorContext"); // 06-03-2022
            Add850RecordResponse response = new Add850RecordResponse(); // 06-03-2022
            // string oErrConnectionName = "WebApiDbContext";
            string oErrConnectionName = "DefaultErrorContext"; // 08-31-2022

            if (request.Edi850HeaderRecord != null)
            {
                // 06-03-2022 begin
                bool bValidCardCode = validateCardCode(request.Edi850HeaderRecord.CardCode);
                if (bValidCardCode == false)
                {
                    response.ErrorMessage = "Request CardCode is not valid";
                    string oErrorMesg = "850 request contains invalid CardCode: " + request.Edi850HeaderRecord.CardCode; // 08-16-2022
                    _logger.Error("850 request contains invalid CardCode: " + request.Edi850HeaderRecord.CardCode + " for PO# " + request.Edi850HeaderRecord.PurchaseOrderReference);
                    response.Successful = false;
                    //UpdateEdiTrxFailures(dbContext, "850", request, response.ErrorMessage, false);
                    if (!String.IsNullOrWhiteSpace(request.Edi850HeaderRecord.CardCode) && request.Edi850HeaderRecord.CardCode.Trim().Length > 0) // 08-31-2022
                    {
                        UpdateEdiTrxFailures(oErrConnectionName, "850", oErrorMesg, request, response.ErrorMessage, false); // 08-16-2022
                    } // 08-31-2022
                    return response;
                }
                else
                {
                    // 06-03-2022 end
                    _logger.Debug("Begin Processing 850 request with PO #" + request.Edi850HeaderRecord.PurchaseOrderReference);
                }
            }
            //Debug("{@Add850RecordRequest}", request);

            //  Add850RecordResponse response = new Add850RecordResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                string oErrorMesg = response.ErrorMessage; // 08-16-2022
                response.Successful = false;
                //UpdateEdiTrxFailures(dbContext, "850", request, response.ErrorMessage, false); // 06-03-2022
                // 08-16-2022 begin
                oErrConnectionName = this.getConnectionName(request.Edi850HeaderRecord.CardCode);
                if (oErrConnectionName == null || oErrConnectionName.Trim().Length == 0)
                {
                    oErrConnectionName = "WebApiDbContext";
                }
                UpdateEdiTrxFailures(oErrConnectionName, "850", oErrorMesg, request, response.ErrorMessage, false);
                // 08-16-2022 end
                return response;
            }
            else
            {
                dbContext = null;
            }

            try
            {
                // 01-17-2018  begin
                string oConnectionName = this.getConnectionName(request.Edi850HeaderRecord.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())
                // 06-03-2022 begin
                // using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                int oRet850 = 0; // 06-03-2022
                using (dbContext = new WebApiDbContext(oConnectionName))
                // 06-03-1011 end
                // 01-17-2018 end
                {
                    request.Edi850HeaderRecord.RecordDate = DateTime.Now;
                    request.Edi850HeaderRecord.Processed = false;
                    request.Edi850HeaderRecord.Processed810 = false;
                    request.Edi850HeaderRecord.Processed856 = false;
                    request.Edi850HeaderRecord.SalesOrderKey = 0;
                    request.Edi850HeaderRecord.ReceivedDateTime = DateTime.Now;
                    request.Edi850HeaderRecord.Processed810DateTime = null;
                    request.Edi850HeaderRecord.Processed856DateTime = null;
                    request.Edi850HeaderRecord.ProcessedDateTime = null;
                    request.Edi850HeaderRecord.ErrorMessage = String.Empty;
                    // 05-30-2017 begin
                    request.Edi850HeaderRecord.Processed870 = false;
                    request.Edi850HeaderRecord.Processed870DateTime = null;
                    request.Edi850HeaderRecord.Last870Status = null;
                    // 05-30-2017 end
                    // 01-17-2018 begin
                    request.Edi850HeaderRecord.Processed855 = false;
                    request.Edi850HeaderRecord.Processed855DateTime = null;
                    request.Edi850HeaderRecord.Last855Status = null;
                    // 01-17-2018 end
                    // 02-10-2019 begin
                    request.Edi850HeaderRecord.Processed860 = false;
                    request.Edi850HeaderRecord.Processed860DateTime = null;
                    request.Edi850HeaderRecord.Last860Status = null;
                    // 02-10-2019 
                    // 03-16-2021 begin
                    request.Edi850HeaderRecord.Processed753 = false;
                    request.Edi850HeaderRecord.Processed753DateTime = null;
                    request.Edi850HeaderRecord.Last753TrxType = "00";
                    // 03-16-2021 end
                    request.Edi850HeaderRecord.HasOpen856 = false; // 08-06-2019    
                    // 03-11-2024 begin
                    request.Edi850HeaderRecord.ProcessedPreSo855 = false;
                    request.Edi850HeaderRecord.ProcessedPreSo855DateTime = null;
                    request.Edi850HeaderRecord.OrigProcessedPreSo855DateTime = null;
                    // 03-11-2024 end
                    request.Edi850HeaderRecord.NonSAPSO = "N"; // 09-25-2024
                    try
                    {
                        request.Edi850HeaderRecord.SBOCardCode = getSBOCardCode(request.Edi850HeaderRecord.CardCode); // 03-13-2018
                    }
                    catch
                    {

                    }
                    //08-30-2019 begin
                    try
                    {
                        if (String.IsNullOrWhiteSpace(request.Edi850HeaderRecord.CardCode))
                        {
                            request.Edi850HeaderRecord.Processed = true;
                            request.Edi850HeaderRecord.ErrorMessage = "Invalid Data - No Card Code sent";
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Error updating 850 without a CardCode =>" + e.Message);
                    }
                    // 08-30-2019 end
                    // 07-16-2020 begin
                    try
                    {
                        if (request.Edi850HeaderRecord.CardCode.StartsWith("Lowes2")
                            && (String.IsNullOrWhiteSpace(request.Edi850HeaderRecord.VendorNumber)
                            || String.IsNullOrEmpty(request.Edi850HeaderRecord.VendorNumber)))
                        {
                            request.Edi850HeaderRecord.VendorNumber = "98461";
                        }
                    }
                    catch (Exception l)
                    {
                        /*if (String.IsNullOrWhiteSpace(l.InnerException.Message))
                        {
                            _logger.Debug("Error checking/setting vendor number for Lowes2 => " + l.Message);
                        }
                        else
                        {
                            _logger.Debug("Error checking/setting vendor number for Lowes2 => " + l.Message + "-> " + l.InnerException.Message);
                        } */
                        _logger.Error("Error checking/setting vendor number for Lowes2 => " + l.Message);
                    }
                    // 07-16-2020 end   
                    // 06-08-2021 begin
                    if (String.IsNullOrWhiteSpace(request.Edi850HeaderRecord.TrxPurpose))
                    {
                        request.Edi850HeaderRecord.TrxPurpose = "00";
                    }
                    if (String.IsNullOrWhiteSpace(request.Edi850HeaderRecord.TransportMethod))
                    {
                        request.Edi850HeaderRecord.TransportMethod = "";
                    }
                    // 06-08-2021 end
                    dbContext.Edi850HeaderRecords.Add(request.Edi850HeaderRecord);
                    oRet850 = dbContext.SaveChanges();
                }
                // 06-03-2022 begin
                if (oRet850 == 0)
                {
                    string oErrorMesg = "Error adding 850 for " + request.Edi850HeaderRecord.CardCode + " PO# " + request.Edi850HeaderRecord.PurchaseOrderReference + "=> " + response.ErrorMessage; // 08-16-2022
                    // 08-16-2022 begin
                    //response.ErrorMessage = "Error adding 850 for " + request.Edi850HeaderRecord.CardCode + " PO# " + request.Edi850HeaderRecord.PurchaseOrderReference;
                    //UpdateEdiTrxFailures(dbContext, "850", request, response.ErrorMessage, true);
                    if (!String.IsNullOrEmpty(oConnectionName) && oConnectionName.Trim().Length > 0)
                    {
                        oErrConnectionName = oConnectionName;
                    }

                    _logger.Debug("Error Connection Name: " + oErrConnectionName); // 10-03-2022

                    UpdateEdiTrxFailures(oErrConnectionName, "850", oErrorMesg, request, response.ErrorMessage, true);
                    response.Successful = false;
                    // 08-16-2022 end
                }
                /*  else if (response.Successful == false && !String.IsNullOrWhiteSpace(response.ErrorMessage) && response.ErrorMessage.Trim().Length > 0 ) 
                  {
                      UpdateEdiTrxFailures(oErrConnectionName, "850", "Error", request, response.ErrorMessage, true);            
                  }*/
                else
                {
                    // 06-03-2022 end
                    response.Successful = true;
                    response.ErrorMessage = String.Empty;
                } // 06-03-2022
            }
            catch (Exception ex)
            {
                response.Successful = false;
                String oErrMesg = ex.Message; // 04-06-2022
                // 08-16-2022 begin
                if (ex.InnerException != null && ex.InnerException.Message.Trim().Length > 0)
                {
                    string oInner = ex.InnerException.InnerException.Message;
                    oInner = oInner.Replace("'", " ");
                    oErrMesg = oErrMesg + " InnerException: " + "InnerEx: " + oInner;
                }
                string oException = ex.ToString();
                // 08-16-2022 end
                response.ErrorMessage = "Error during Add850Record => " + oErrMesg; // 04-06-2022
                _logger.Error("Error during Add850Record => " + oErrMesg, ex);
                // 08-16-2022 begin
                //UpdateEdiTrxFailures(dbContext, "850", request, response.ErrorMessage, true);
                UpdateEdiTrxFailures(oErrConnectionName, "850", oErrMesg, request, response.ErrorMessage, true);
                // 08-16-2022 end
                //response.ErrorMessage = ex.Message;
            }

            if (request.Edi850HeaderRecord != null)
            {
                _logger.Debug("End Processing 850 request with PO #" + request.Edi850HeaderRecord.PurchaseOrderReference + ". Successful is " + response.Successful);
            }
            _logger.Debug("Leaving Add850Record for " + request.Edi850HeaderRecord.CardCode);
            return response;
        }
        // 06-03-2022 begin
        //public void UpdateEdiTrxFailures(WebApiDbContext dbContext, string pTrxType, string pErrorMesg, Add850RecordRequest pRequest, string pErrorMessage, bool pAuthorized)
        public void UpdateEdiTrxFailures(string pConnectionName, string pTrxType, string pErrorMesg, Add850RecordRequest pRequest, string pErrorMessage, bool pAuthorized) // 08-16-2022         
        {
            try
            {
                // 08-16-2022 begin
                _logger.Debug("Begin UpdateEdiTrxFailures using " + pConnectionName); // 08-24-2022
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    try
                    {
                        sqlConnection.Open();
                        int oHeaderId = 0;
                        if (pRequest.Edi850HeaderRecord.HeaderId > 0)
                        {
                            oHeaderId = pRequest.Edi850HeaderRecord.HeaderId;
                        }
                        DateTime oRecDate = DateTime.Now;
                        if (pRequest.Edi850HeaderRecord.RecordDate != null)
                        {
                            oRecDate = (DateTime)pRequest.Edi850HeaderRecord.RecordDate;
                        }
                        string oQuery = "INSERT INTO [dbo].[InfocusEdiTransactionFailures] " +
                                                "([RecordDate],[TrxType],[TrxDateTime],[CardCode],[EDIHeaderId],[SBOCardCode],[PurchaseOrderReference],[ErrorMessage]) " +
                                                " VALUES ('" + oRecDate + "','" + pTrxType + "','" + DateTime.Now + "','" + pRequest.Edi850HeaderRecord.CardCode + "'," + oHeaderId +
                                               ",'" + pRequest.Edi850HeaderRecord.SBOCardCode + "','" + pRequest.Edi850HeaderRecord.PurchaseOrderReference + "','" + pErrorMesg + "')";
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                                _logger.Debug("Added data to EdiTransactionFailures: " + pRequest.Edi850HeaderRecord.PurchaseOrderReference);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error("Error adding data to EdiTransactionFailures: " + e.Message);
                        }
                        finally
                        {
                            sqlConnection.Close();
                        }
                    }
                    catch (Exception c)
                    {
                        _logger.Error("Error opening sql connection to update InfocusEdiTransactionFailures =>" + c.Message);
                    }
                    /*  using (dbContext)
                      {
                          EdiTransactionFailures oTrxFailures = new EdiTransactionFailures();
                          oTrxFailures.CardCode = pRequest.Edi850HeaderRecord.CardCode;
                          oTrxFailures.PurchaseOrderReference = pRequest.Edi850HeaderRecord.PurchaseOrderReference;
                          oTrxFailures.TrxType = pTrxType;
                          if (pAuthorized == true)
                          {
                              oTrxFailures.SBOCardCode = pRequest.Edi850HeaderRecord.SBOCardCode;
                              oTrxFailures.EDIHeaderId = pRequest.Edi850HeaderRecord.HeaderId;
                              oTrxFailures.RecordDate = pRequest.Edi850HeaderRecord.RecordDate;
                              //oTrxFailures.ErrorMessage = pErrorMessage;
                              oTrxFailures.ErrorMessage = pErrorMesg; // 08-16-2022
                          }
                          else
                          {
                             //oTrxFailures.ErrorMessage = "Failed to Add850Record: " + pErrorMessage;
                              oTrxFailures.ErrorMessage = "Failed to Add850Record: " + pRequest.Edi850HeaderRecord.CardCode + " PO# " + pRequest.Edi850HeaderRecord.PurchaseOrderReference + " => " + pErrorMesg; // 08-16-2022
                              oTrxFailures.RecordDate = DateTime.Now;
                          }

                          oTrxFailures.RecordDate = DateTime.Now;
                          dbContext.EdiTransactionFailures.Add(oTrxFailures);
                          int oRet = dbContext.SaveChanges();
                      } */
                    // 08-16-2022 end
                }
            }
            catch (Exception f)
            {
                _logger.Error("Failed to add error to EdiTransactionFailures: " + f.Message);
            }
            _logger.Debug("Exit UpdateEdiTrxFailures"); // 08-24-2022          
        }
        // 06-03-2022 end

        // 07-21-2023 begin
        public void UpdateEdiTrxFailures(string pConnectionName, string pTrxType, string pErrorMesg, Add940RecordRequest pRequest, string pErrorMessage, bool pAuthorized) // 08-16-2022         
        {
            try
            {
                // 08-16-2022 begin
                _logger.Debug("Begin UpdateEdiTrxFailures using " + pConnectionName);
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    try
                    {
                        sqlConnection.Open();
                        int oHeaderId = 0;
                        if (pRequest.Edi940HeaderRecord.HeaderId > 0)
                        {
                            oHeaderId = pRequest.Edi940HeaderRecord.HeaderId;
                        }
                        DateTime oRecDate = DateTime.Now;
                        if (pRequest.Edi940HeaderRecord.RecordDate != null)
                        {
                            oRecDate = (DateTime)pRequest.Edi940HeaderRecord.RecordDate;
                        }
                        string oQuery = "INSERT INTO [dbo].[InfocusEdiTransactionFailures] " +
                                                "([RecordDate],[TrxType],[TrxDateTime],[CardCode],[EDIHeaderId],[SBOCardCode],[PurchaseOrderReference],[ErrorMessage]) " +
                                                " VALUES ('" + oRecDate + "','" + pTrxType + "','" + DateTime.Now + "','" + pRequest.Edi940HeaderRecord.CardCode + "'," + oHeaderId +
                                               ",'" + pRequest.Edi940HeaderRecord.SBOCardCode + "','" + pRequest.Edi940HeaderRecord.PurchaseOrderReference + "','" + pErrorMesg + "')";
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                                _logger.Debug("Added data to EdiTransactionFailures: " + pRequest.Edi940HeaderRecord.PurchaseOrderReference);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger.Error("Error adding data to EdiTransactionFailures: " + e.Message);
                        }
                        finally
                        {
                            sqlConnection.Close();
                        }
                    }
                    catch (Exception c)
                    {
                        _logger.Error("Error opening sql connection to update InfocusEdiTransactionFailures =>" + c.Message);
                    }
                }
            }
            catch (Exception f)
            {
                _logger.Error("Failed to add error to EdiTransactionFailures: " + f.Message);
            }
            _logger.Debug("Exit UpdateEdiTrxFailures");
        }
        // 07-21-2023 end

        // 02-28-2019 begin
        [HttpPost]
        public Add180RecordResponse Add180Record(Add180RecordRequest request)
        {
            _logger.Debug("Entering Add180Records ");
            if (request.Edi180HeaderRecord != null)
            {
                _logger.Debug("Begin Processing 180 request with PO #" + request.Edi180HeaderRecord.PurchaseOrderReference);
            }
            //_logger.Debug("{@Add180RecordRequest}", request);

            Add180RecordResponse response = new Add180RecordResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }

            try
            {
                string oConnectionName = this.getConnectionName(request.Edi180HeaderRecord.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    request.Edi180HeaderRecord.RecordDate = DateTime.Now;
                    request.Edi180HeaderRecord.Processed = false;
                    request.Edi180HeaderRecord.ReturnOrderKey = 0;
                    request.Edi180HeaderRecord.ReceivedDateTime = DateTime.Now;
                    request.Edi180HeaderRecord.ProcessedDateTime = null;
                    request.Edi180HeaderRecord.ErrorMessage = String.Empty;
                    try
                    {
                        request.Edi180HeaderRecord.SBOCardCode = getSBOCardCode(request.Edi180HeaderRecord.CardCode);
                    }
                    catch
                    {
                        request.Edi180HeaderRecord.SBOCardCode = request.Edi180HeaderRecord.CardCode;
                    }

                    dbContext.Edi180HeaderRecords.Add(request.Edi180HeaderRecord);
                    dbContext.SaveChanges();

                }
                response.Successful = true;
                response.ErrorMessage = String.Empty;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error("Error during Add180Record =>" + ex.Message, ex);
                response.ErrorMessage = ex.Message;
            }
            if (request.Edi180HeaderRecord != null)
            {
                _logger.Debug("End Processing 180 request with PO #" + request.Edi180HeaderRecord.PurchaseOrderReference + ". Successful is " + response.Successful);
            }
            _logger.Debug("Leaving Add180Record");
            return response;
        }
        // 02-28-2019 end

        // 04-25-2019 begin
        [HttpPost]
        public Add820RecordResponse Add820Record(Add820RecordRequest request)
        {
            if (String.IsNullOrWhiteSpace(request.Edi820HeaderRecord.CardCode))
            {
                _logger.Debug("Entering Add820Record");
            }
            else
            {
                _logger.Debug("Entering Add820Record for " + request.Edi820HeaderRecord.CardCode);
            }
            if (request.Edi820HeaderRecord != null)
            {
                _logger.Debug("Begin Processing 820 request with PO #" + request.Edi820HeaderRecord.PurchaseOrderReference);
            }
            //_logger.Debug("{@Add820RecordRequest}", request);
            Add820RecordResponse response = new Add820RecordResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }

            try
            {
                string oConnectionName = this.getConnectionName(request.Edi820HeaderRecord.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                {
                    request.Edi820HeaderRecord.RecordDate = DateTime.Now;
                    request.Edi820HeaderRecord.Processed = false;
                    request.Edi820HeaderRecord.ProcessedReturn820 = false;
                    request.Edi820HeaderRecord.ReceivedDateTime = DateTime.Now;
                    try
                    {
                        request.Edi820HeaderRecord.SBOCardCode = getSBOCardCode(request.Edi820HeaderRecord.CardCode);
                    }
                    catch
                    {

                    }

                    dbContext.Edi820HeaderRecords.Add(request.Edi820HeaderRecord);
                    dbContext.SaveChanges();

                }
                response.Successful = true;
                response.ErrorMessage = String.Empty;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error("Error during Add820Record => " + ex.Message, ex);
                response.ErrorMessage = ex.Message;
            }
            if (request.Edi820HeaderRecord != null)
            {
                _logger.Debug("End Processing 820 request with PO #" + request.Edi820HeaderRecord.PurchaseOrderReference + ". Successful is " + response.Successful);
            }
            _logger.Debug("Leaving Add820Record");
            return response;
        }

        // 04-25-2019 end

        // 02-25-2019 begin
        [HttpPost]
        public Add860RecordResponse Add860Record(Add860RecordRequest request)
        {
            _logger.Debug("Entering Add860Record");
            if (request.Edi860HeaderRecord != null)
            {
                _logger.Debug("Begin Processing 860 request with PO #" + request.Edi860HeaderRecord.PurchaseOrderReference);
            }
            //_logger.Debug("{@Add860RecordRequest}", request);

            Add860RecordResponse response = new Add860RecordResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }

            try
            {
                string oConnectionName = this.getConnectionName(request.Edi860HeaderRecord.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                {
                    request.Edi860HeaderRecord.RecordDate = DateTime.Now;
                    request.Edi860HeaderRecord.Processed = false;
                    request.Edi860HeaderRecord.SalesOrderKey = 0;
                    request.Edi860HeaderRecord.ReceivedDateTime = DateTime.Now;
                    request.Edi860HeaderRecord.ProcessedDateTime = null;
                    request.Edi860HeaderRecord.ErrorMessage = String.Empty;
                    try
                    {
                        request.Edi860HeaderRecord.SBOCardCode = getSBOCardCode(request.Edi860HeaderRecord.CardCode);
                    }
                    catch
                    {
                        request.Edi860HeaderRecord.SBOCardCode = request.Edi860HeaderRecord.CardCode;
                    }

                    dbContext.Edi860HeaderRecords.Add(request.Edi860HeaderRecord);
                    dbContext.SaveChanges();

                }
                response.Successful = true;
                response.ErrorMessage = String.Empty;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error("Error during Add860Record =>" + ex.Message, ex);
                response.ErrorMessage = ex.Message;
            }
            if (request.Edi860HeaderRecord != null)
            {
                _logger.Debug("End Processing 860 request with PO #" + request.Edi860HeaderRecord.PurchaseOrderReference + ". Successful is " + response.Successful);
            }
            _logger.Debug("Leaving Add860Record");
            return response;
        }
        // 02-25-2019 end
        // 07-21-2023 begin
        [HttpPost]
        public Add940RecordResponse Add940Record(Add940RecordRequest request)
        {
            _logger.Debug("Entering Add940Records for " + request.Edi940HeaderRecord.CardCode);
            WebApiDbContext dbContext = new WebApiDbContext("DefaultErrorContext");
            Add940RecordResponse response = new Add940RecordResponse();
            string oErrConnectionName = "DefaultErrorContext";

            if (request.Edi940HeaderRecord != null)
            {
                bool bValidCardCode = validateCardCode(request.Edi940HeaderRecord.CardCode);
                if (bValidCardCode == false)
                {
                    response.ErrorMessage = "Request CardCode is not valid";
                    string oErrorMesg = "940 request contains invalid CardCode: " + request.Edi940HeaderRecord.CardCode;
                    _logger.Error("940 request contains invalid CardCode: " + request.Edi940HeaderRecord.CardCode + " for PO# " + request.Edi940HeaderRecord.PurchaseOrderReference);
                    response.Successful = false;
                    if (!String.IsNullOrWhiteSpace(request.Edi940HeaderRecord.CardCode) && request.Edi940HeaderRecord.CardCode.Trim().Length > 0)
                    {
                        UpdateEdiTrxFailures(oErrConnectionName, "940", oErrorMesg, request, response.ErrorMessage, false);
                    }
                    return response;
                }
                else
                {
                    _logger.Debug("Begin Processing 940 request with PO #" + request.Edi940HeaderRecord.PurchaseOrderReference);
                }
            }
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                string oErrorMesg = response.ErrorMessage;
                response.Successful = false;
                oErrConnectionName = this.getConnectionName(request.Edi940HeaderRecord.CardCode);
                if (oErrConnectionName == null || oErrConnectionName.Trim().Length == 0)
                {
                    oErrConnectionName = "WebApiDbContext";
                }
                UpdateEdiTrxFailures(oErrConnectionName, "940", oErrorMesg, request, response.ErrorMessage, false);

                return response;
            }
            else
            {
                dbContext = null;
            }

            try
            {
                string oConnectionName = this.getConnectionName(request.Edi940HeaderRecord.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }

                int oRet940 = 0;
                using (dbContext = new WebApiDbContext(oConnectionName))
                {
                    request.Edi940HeaderRecord.RecordDate = DateTime.Now;
                    request.Edi940HeaderRecord.Processed = false;
                    request.Edi940HeaderRecord.Processed810 = false;
                    request.Edi940HeaderRecord.Processed856 = false;
                    request.Edi940HeaderRecord.SalesOrderKey = 0;
                    request.Edi940HeaderRecord.ReceivedDateTime = DateTime.Now;
                    request.Edi940HeaderRecord.Processed810DateTime = null;
                    request.Edi940HeaderRecord.Processed856DateTime = null;
                    request.Edi940HeaderRecord.ProcessedDateTime = null;
                    request.Edi940HeaderRecord.ErrorMessage = String.Empty;
                    request.Edi940HeaderRecord.HasOpen856 = false;
                    try
                    {
                        request.Edi940HeaderRecord.SBOCardCode = getSBOCardCode(request.Edi940HeaderRecord.CardCode);
                    }
                    catch
                    {

                    }

                    try
                    {
                        if (String.IsNullOrWhiteSpace(request.Edi940HeaderRecord.CardCode))
                        {
                            request.Edi940HeaderRecord.Processed = true;
                            request.Edi940HeaderRecord.ErrorMessage = "Invalid Data - No Card Code sent";
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.Error("Error updating 940 without a CardCode =>" + e.Message);
                    }

                    if (String.IsNullOrWhiteSpace(request.Edi940HeaderRecord.TrxPurpose))
                    {
                        request.Edi940HeaderRecord.TrxPurpose = "00";
                    }
                    if (String.IsNullOrWhiteSpace(request.Edi940HeaderRecord.TransportMethod))
                    {
                        request.Edi940HeaderRecord.TransportMethod = "";
                    }
                    dbContext.Edi940HeaderRecords.Add(request.Edi940HeaderRecord);
                    oRet940 = dbContext.SaveChanges();
                }

                if (oRet940 == 0)
                {
                    string oErrorMesg = "Error adding 940 for " + request.Edi940HeaderRecord.CardCode + " PO# " + request.Edi940HeaderRecord.PurchaseOrderReference + "=> " + response.ErrorMessage;
                    if (!String.IsNullOrEmpty(oConnectionName) && oConnectionName.Trim().Length > 0)
                    {
                        oErrConnectionName = oConnectionName;
                    }

                    _logger.Debug("Error Connection Name: " + oErrConnectionName);

                    UpdateEdiTrxFailures(oErrConnectionName, "940", oErrorMesg, request, response.ErrorMessage, true);
                    response.Successful = false;
                }
                else
                {
                    response.Successful = true;
                    response.ErrorMessage = String.Empty;
                }
            }
            catch (Exception ex)
            {
                response.Successful = false;
                String oErrMesg = ex.Message;

                if (ex.InnerException != null && ex.InnerException.Message.Trim().Length > 0)
                {
                    string oInner = ex.InnerException.InnerException.Message;
                    oInner = oInner.Replace("'", " ");
                    oErrMesg = oErrMesg + " InnerException: " + "InnerEx: " + oInner;
                }
                string oException = ex.ToString();

                response.ErrorMessage = "Error during Add940Record => " + oErrMesg;
                _logger.Error("Error during Add940Record => " + oErrMesg, ex);

                UpdateEdiTrxFailures(oErrConnectionName, "940", oErrMesg, request, response.ErrorMessage, true);

            }

            if (request.Edi940HeaderRecord != null)
            {
                _logger.Debug("End Processing 940 request with PO #" + request.Edi940HeaderRecord.PurchaseOrderReference + ". Successful is " + response.Successful);
            }
            _logger.Debug("Leaving Add940Record for " + request.Edi940HeaderRecord.CardCode);
            return response;
        }
        // 07-21-2023 end

        [HttpPost]
        public Get856RecordsResponse Get856Records(Get856RecordsRequest request)
        {
            if (request == null)
            {
                throw new Exception("Invalid 856 request");
            }
            string oSBOCardCode = getSBOCardCode(request.CardCode); // 01-17-2018
            string oLastTrxDateTime = request.LastRecTrxDT;
            string oLastTrxCutoffDT = request.LastTrxCutoff; // 04-08-2022
            string oSourceTrx = request.SourceTrx; // 08-08-2023
            string oPartnerId = request.PartnerId;   // 12-20-2023
            // 08-16-2023 begin
            if (String.IsNullOrWhiteSpace(oSourceTrx))
            {
                oSourceTrx = "850";
            }
            // 08-16-2023 end

            bool bSendPack = getProcess856Pack(request.CardCode); // 08-31-2021
            bool bSend855 = getProcess855(request.CardCode); // 02-27-2022   

            // 02-15-2022 begin

            DateTime oLast856Date = DateTime.Now;
            if (!String.IsNullOrWhiteSpace(oLastTrxDateTime))
            {
                try
                {
                    oLast856Date = Convert.ToDateTime(oLastTrxDateTime);
                }
                catch (Exception ld)
                {
                    _logger.Error("Error processing last date/time => " + oLastTrxDateTime + "; " + ld.Message);
                    oLast856Date = DateTime.Now;
                }
            }
            // 02-15-2022

            // 08-17-2023 begin
            string oConnectionName = this.getConnectionName(request.CardCode);
            if (oConnectionName == null || oConnectionName.Trim().Length == 0)
            {
                oConnectionName = "WebApiDbContext";
            }
            Int32 NoDays = 0;
            Int32 oMaxDays = getMaxTD(oConnectionName);
            try
            {
                NoDays = (DateTime.Now - oLast856Date).Days;
            }
            catch (Exception nd)
            {
                String oErrMsg = nd.Message;
                _logger.Error("Error getting #Days between today & Last856Date: " + oErrMsg);
                NoDays = 0;
            }
            if (NoDays == 0 && oLast856Date.Date != DateTime.Now.Date)
            {
                _logger.Error("Error getting #Days between today & Last856Date, Last856Date will be set to today");
                oLast856Date = DateTime.Now;
            }
            else if (NoDays > oMaxDays)
            {
                oLast856Date = DateTime.Now.AddDays(-1 * oMaxDays);
                _logger.Error("# Days for Last Trx exceeds max, set LastTrxDt to " + oLast856Date.ToShortDateString());
            }
            // 08-17-2023 end

            // 08-08-2023 begin
            bool b940 = false;
            if (!String.IsNullOrWhiteSpace(oSourceTrx) && oSourceTrx == "940")
            {
                b940 = true;
            }
            // 08-08-2023 end

            // 04-08-2022 begin
            DateTime oCutoffDt = DateTime.Now;
            if (!String.IsNullOrWhiteSpace(oLastTrxCutoffDT) && !String.IsNullOrWhiteSpace(oLastTrxDateTime))
            {
                try
                {
                    oCutoffDt = Convert.ToDateTime(oLastTrxCutoffDT);
                }
                catch (Exception cd)
                {
                    _logger.Error("Error processing cutoff date/time => " + oLastTrxCutoffDT + "; " + cd.Message);
                    oCutoffDt = DateTime.Now;
                }
            } // 04-24-2022 begin
            else
            {
                oCutoffDt = DateTime.Now;
            } // 04-24-2022 end
            // 04-08-2022 end
            List<Edi850WithDelivery> listToProcess = new List<Edi850WithDelivery>();
            // 08-08-2023 begin
            List<Edi940WithDelivery> list940ToProcess = new List<Edi940WithDelivery>();
            _logger.Debug("Entering Get856Records for " + oSBOCardCode + " : " + request.CardCode);
            _logger.Debug("Processing the following request object:");
            Get856RecordsResponse response = new Get856RecordsResponse();
            IPreProcess856Record iPreProcess856Record = null;
            IPostProcess856Record iPostProcess856Record = null;
            //IPreProcess856PRecord iPreProcess856PRecord = null; // 09-23-2021
            // IPostProcess856PRecord iPostProcess856PRecord = null; // 09-02-2021

            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get856Records";
                response.Successful = false;
                return response;
            }
            // 12-20-2023 begin
            if (b940 &&
                (String.IsNullOrWhiteSpace(oPartnerId) || oPartnerId.Trim().Length == 0))
            {
                _logger.Error("Missing PartnerId for 940");
                response.ErrorMessage = "PartnerId is required for 940 Get856Records";
                response.Successful = false;
                return response;
            }
            // 12-20-2023 end
            try
            {
                // 01-17-2018  begin
                //iPreProcess856Record = Get856PreProcess(request.CardCode);
                //iPostProcess856Record = Get856PostProcess(request.CardCode);
                // 01-17-2018  end
                List<Edi850HeaderRecord> listOf850Records = null;
                List<Edi940HeaderRecord> listOf940Records = null; // 08-08-2023
                // 01-17-2018  begin
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {  // 01-17-2018 end
                    /*
                     // 09-02-2021 begin
                    if (bSendPack == true)
                    {
                        iPreProcess856PRecord = Get856PPreProcess(request.CardCode);
                        iPostProcess856PRecord = Get856PPostProcess(request.CardCode);
                    }
                    else
                    {
                        // 09-02-2021 end
                        */
                    // 02-15-2022 begin
                    //iPreProcess856Record = Get856PreProcess(request.CardCode);
                    //iPreProcess856Record = Get856PreProcess(request.CardCode, oLast856Date); // 02-15-2022 end
                    iPreProcess856Record = Get856PreProcess(request.CardCode, oLast856Date, oCutoffDt); // 04-08-2022 

                    iPostProcess856Record = Get856PostProcess(request.CardCode);
                    //}
                    // 01-17-2018 begin
                    // oSBOCardCode = request.CardCode;
                }
                else
                {
                    if (request.CardCode.StartsWith("TeeZ"))
                    {
                        //_logger.Debug("Processing 3PL");
                        // 02-15-2022 begin
                        // iPreProcess856Record = Get856PreProcess(request.CardCode);
                        //iPreProcess856Record = Get856PreProcess(request.CardCode, oLast856Date); // 02-14-2022 end
                        iPreProcess856Record = Get856PreProcess(request.CardCode, oLast856Date, oCutoffDt); // 04-08-2022 end
                        iPostProcess856Record = Get856PostProcess(request.CardCode);
                    }
                    else
                    {
                        /*
                         // 09-02-2021 begin
                        if (bSendPack == true)
                        {
                            iPreProcess856PRecord = Get856PPreProcess(oSBOCardCode);
                            iPostProcess856PRecord = Get856PPostProcess(oSBOCardCode);
                        }
                        else
                        {
                            // 09-02-2021 end
                            */
                        //iPreProcess856Record = Get856PreProcess(oSBOCardCode, oLast856Date);
                        iPreProcess856Record = Get856PreProcess(oSBOCardCode, oLast856Date, oCutoffDt); // 04-08-2022
                        iPostProcess856Record = Get856PostProcess(oSBOCardCode);
                        //}
                    }
                }
                // 01-17-2018 end
                // 08-17-2023 begin
                // moved to earlier in processing
                /*
                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                 */
                // 07-17-2023 end
                // _logger.Debug("ConnectionName: " + oConnectionName);

                // 04-30-2019 begin
                //_logger.Debug("Updating EDI line for Deliveries with missing EDI Line number");

                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    try
                    {
                        sqlConnection.Open();

                        // 05-15-2020 begin
                        string oQuery = "";
                        /*
                        string oQuery = "execute [dbo].[Infocus_EDI_Set_Delivery_EDILn] ";
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception del2)
                        {
                            _logger.Debug("Error updating delivery line numbers => " + del2.Message);
                        }
                        */


                        _logger.Debug("Updating EDI trx for canceled orders");
                        // 08-10-2023 begin
                        //oQuery = "execute [dbo].[Infocus_EDI_Set_Canceled_Orders] "; 
                        if (b940 == true)
                        {
                            oQuery = "execute [dbo].[Infocus_EDI_Set_Canceled_Orders] 940";
                        }
                        else
                        {
                            oQuery = "execute [dbo].[Infocus_EDI_Set_Canceled_Orders] 850";
                        }
                        // 08-10-2023 end
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception del2)
                        {
                            // 08-09-2023 begin
                            if (b940 == true)
                            {
                                _logger.Error("Error updating InfocusEdi940HeaderRecord  => " + del2.Message);
                            }
                            else
                            {
                                // 08-09-2023 end
                                _logger.Error("Error updating InfocusEdi850HeaderRecord  => " + del2.Message);
                            } // 08-09-2023
                        }
                        // 12-28-2022 begin

                        _logger.Debug("Updating InfocusEdi850HeaderRecord column HasOpen856");

                        //oQuery = "execute [dbo].[Infocus_EDI_Check_Open_856] ";
                        // 08-09-2023 begin
                        if (b940 == true)
                        {
                            oQuery = "execute [dbo].[Infocus_EDI_Check_Open_940_856] '" + request.CardCode.Trim() + "'";
                        }
                        else
                        {
                            // 08-09-2023 end
                            oQuery = "execute [dbo].[Infocus_EDI_Check_Open_856v2] '" + request.CardCode.Trim() + "'"; // 08-02-2022
                        } // 08-09-2023
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception del2)
                        {
                            _logger.Error("Error updating HasOpen856  => " + del2.Message);
                        }

                    }
                    catch (Exception del)
                    {
                        _logger.Error("Error updating delivery => " + del.Message);
                    }
                    finally
                    {
                        sqlConnection.Close();
                    }
                }
                // 04-30-2019 end

                // 05-17-2020 end

                //    using (WebApiDbContext dbContext = new WebApiDbContext())
                _logger.Debug("Checking for 856s to be processed");
                string oLog = _logger.ToString();
                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end   
                {
                    if (b940 == true)
                    {
                        _logger.Debug(" 940 856s");
                        listOf940Records = dbContext.Edi940HeaderRecords.Include("Details")//.Include("Deliveries")
                                            .Where(x => (x.Processed856 == false ||
                                                  (x.Orig856ProcessedDateTime > oLast856Date
                                                   && x.Orig856ProcessedDateTime < oCutoffDt))
                                                   && (x.HasOpen856 == true
                                                   || x.Processed856 == false
                                                   || (x.Processed856 == true && x.Orig856ProcessedDateTime > oLast856Date && x.Orig856ProcessedDateTime < oCutoffDt))
                                                   && x.Processed == true
                                                   && x.TrxPurpose != "01"
                                                   && x.CardCode == request.CardCode
                                                   && x.CustCode3PL == request.PartnerId // 12-20-2023
                                                   && (x.IgnoreTrxRequest != "Y")
                                                   && x.SalesOrderKey > 0).ToList();
                        _logger.Debug("There are " + listOf940Records.Count + " 856 records to evaluate");
                        foreach (var record in listOf940Records)
                        {
                            string oPONo = record.PurchaseOrderReference;
                            int oHeaderId = record.HeaderId;
                            //_logger.Debug("Get Delivery from view for HeaderId " + record.HeaderId);
                            try
                            {
                                Delivery[] deliveriesFound = getDeliveries(dbContext, record);
                                if (deliveriesFound != null && deliveriesFound.Count() > 0)
                                {
                                    for (int d = 0; d < deliveriesFound.Length; d++)
                                    {
                                        Delivery delivery = deliveriesFound[d];
                                        if (delivery != null
                                            && delivery.Canceled == "N")
                                        {
                                            // _logger.Debug("Found 856 delivery line from delivery with key " + delivery.DocNum);
                                            if (((String.IsNullOrWhiteSpace(delivery.U_Info_BOL) || delivery.U_Info_BOL.Trim().Length == 0)
                                                && !(record.CardCode.StartsWith("TeeZed") && record.CarrierCode == "LBLS"))
                                                || (record.CardCode.StartsWith("Lowes2")) &&
                                                (delivery.U_COR_ActShipDt == null || String.IsNullOrWhiteSpace(delivery.U_COR_ActShipDt.Value.ToString().Trim())))
                                            {
                                                _logger.Error("Invalid BOL or Actual Ship Date for Delivery# " + delivery.DocNum);
                                            }
                                            else if (String.IsNullOrWhiteSpace(delivery.U_InfoW2Cc) || delivery.U_InfoW2Cc.ToUpper().StartsWith("UNSP"))
                                            {
                                                _logger.Error("Invalid Carrier Delivery# " + delivery.DocNum);

                                            }
                                            else
                                            {
                                                int oZeroLinNo = checkEDILnNo(oConnectionName, "DLN1", delivery.DocEntry, "17", delivery.CardCode);

                                                if (oZeroLinNo > 0)
                                                {
                                                    _logger.Error("Found lines for Delivery# " + delivery.DocNum + " with 0 EDI Line#");
                                                }
                                                int oZeroQty = checkZeroQty(oConnectionName, "DLN1", "ODLN", delivery.DocEntry, "17");
                                                if (oZeroQty > 0)
                                                {
                                                    _logger.Error("Found lines for Delivery# " + delivery.DocNum + " with zero quantity");
                                                }
                                                if (oZeroLinNo > 0 || oZeroQty > 0)
                                                {
                                                    getDeliveryData(oConnectionName, delivery);
                                                }
                                                oZeroQty = 0;
                                                oZeroLinNo = 0;
                                                foreach (DeliveryLine delLine in delivery.DeliveryLines)
                                                {
                                                    if (delLine.TreeType == "S" || delLine.TreeType == "N")
                                                    {
                                                        if (delLine.Quantity <= 0)
                                                        {
                                                            oZeroQty = oZeroQty + 1;
                                                        }
                                                        if (delLine.LineNumber850 <= 0)
                                                        {
                                                            oZeroLinNo = oZeroLinNo + 1;
                                                        }
                                                    }
                                                }
                                                if (oZeroLinNo > 0 || oZeroQty > 0)
                                                {
                                                    _logger.Error("Invalid qty or line# from sales order for Delivery# " + delivery.DocNum);
                                                }
                                                else
                                                {
                                                    list940ToProcess.Add(new Edi940WithDelivery(record, delivery));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            string oErr = "856 delivery line not found for 940 record with key " + record.HeaderId;
                                            // _logger.Error("856 delivery line not found for 940 record with key " + record.HeaderId);
                                        }
                                        try
                                        {
                                            System.Runtime.InteropServices.Marshal.ReleaseComObject(delivery);
                                        }
                                        catch
                                        {
                                        }
                                    }
                                }
                            }
                            catch (Exception gdln)
                            {
                                _logger.Error("856 error getting delivery for record with key " + record.HeaderId + " => " + gdln.Message);
                            }
                        }
                    }
                    else
                    {
                        listOf850Records = dbContext.Edi850HeaderRecords.Include("Details").Include("Deliveries")
                            .Where(x => (x.Processed856 == false ||
                                (x.Orig856ProcessedDateTime > oLast856Date // 02-25-2022
                                && x.Orig856ProcessedDateTime < oCutoffDt)) // 04-08-2022
                                && (x.HasOpen856 == true // 08-06-2019
                                                         // || x.Processed856DateTime > oLast856Date) // 02-12-2022
                                || x.Processed856 == false // 03-31-2022
                                                           //    || (x.Processed856 == true && x.Orig856ProcessedDateTime > oLast856Date)) // 02-25-2022
                                || (x.Processed856 == true && x.Orig856ProcessedDateTime > oLast856Date && x.Orig856ProcessedDateTime < oCutoffDt))// 04-08-2022
                                  && x.Processed == true
                                  && x.TrxPurpose != "01" //05-31-2017
                                  && x.CardCode == request.CardCode
                                  && (x.IgnoreTrxRequest != "Y") // 08-06-2022
                                                                 //&& x.CardCode == oSBOCardCode // 01-18-2017
                                                                 // && x.HeaderId >= 13537
                                  && (bSend855 == false || x.Processed855 == true) //08-31-2022
                                  && x.Deliveries.Count > 0
                                  && x.SalesOrderKey > 0).ToList();
                        _logger.Debug("There are " + listOf850Records.Count + " 856 records to evaluate");
                        foreach (var record in listOf850Records)
                        {
                            if (record.Deliveries.Count() > 0)
                            {
                                // 08-06-2019 begin 
                                string oPONo = record.PurchaseOrderReference;
                                int oHeaderId = record.HeaderId;
                                // 06-11-2020 begin
                                bool sent855 = false;
                                // 08-26-2022 begin
                                if (request.CardCode.StartsWith("LowesNet") || request.CardCode.StartsWith("TSCCL")
                                    || request.CardCode.StartsWith("Wayfair")
                                    || bSend855 == true) // 02-27-2022
                                {
                                    if (record.Processed855 == true)
                                    {
                                        sent855 = true;
                                    }
                                }
                                else
                                {
                                    sent855 = true;
                                }
                                if (sent855 == true)
                                {
                                    // 06-11-2020 end
                                    //_logger.Debug("Get Delivery from view for HeaderId " + record.HeaderId);
                                    try
                                    {
                                        //Delivery delivery = GetDeliveryFromXref(dbContext, record, oConnectionName, oLast856Date);
                                        // 12-28-2022 begin
                                        if (record.Deliveries != null && record.Deliveries.Count > 0)
                                        {
                                            Delivery[] deliveriesFound = record.Deliveries.ToArray();
                                            for (int d = 0; d < deliveriesFound.Length; d++)
                                            {  // 12-28-2022 end
                                                Delivery delivery = deliveriesFound[d];
                                                //Delivery delivery = GetDeliveryFromXref(dbContext, record, oConnectionName, oLast856Date, oCutoffDt); // 04-08-2022

                                                if (delivery != null
                                                    && delivery.Canceled == "N") // 12-28-2022
                                                {

                                                    // _logger.Debug("Found 856 delivery line from delivery with key " + delivery.DocNum);
                                                    /* if ((record.CardCode == "WAYFAIR" || record.CardCode.StartsWith("WAYFAIR")) && String.IsNullOrWhiteSpace(delivery.U_Info_Pro))
                                                     {
                                                         _logger.Debug("Wayfair delivery # " + delivery.DocNum.ToString() + " is missing the Pro#");
                                                     }
                                                     else
                                                     {*/
                                                    // 10-15-2019 begin
                                                    if (((String.IsNullOrWhiteSpace(delivery.U_Info_BOL) || delivery.U_Info_BOL.Trim().Length == 0)
                                                        && !(record.CardCode.StartsWith("TeeZed") && record.CarrierCode == "LBLS")) // 07-19-2021
                                                                                                                                    // 02-02-2023 begin
                                                        || (record.CardCode.StartsWith("Lowes2")) &&
                                                        (delivery.U_COR_ActShipDt == null || String.IsNullOrWhiteSpace(delivery.U_COR_ActShipDt.Value.ToString().Trim())))
                                                    // 02-02-2023 end
                                                    {
                                                        // 1-27-2023 remove write error
                                                        String oErrMessage = "Invalid BOL or Actual Ship Date";
                                                        _logger.Error("Invalid BOL or Actual Ship Date for Delivery# " + delivery.DocNum);
                                                    }
                                                    else if (String.IsNullOrWhiteSpace(delivery.U_InfoW2Cc) || delivery.U_InfoW2Cc.ToUpper().StartsWith("UNSP"))
                                                    {
                                                        _logger.Error("Invalid Carrier Delivery# " + delivery.DocNum);

                                                    }
                                                    // 05-15-2020 begin
                                                    else
                                                    {
                                                        int oZeroLinNo = checkEDILnNo(oConnectionName, "DLN1", delivery.DocEntry, "17", delivery.CardCode);

                                                        if (oZeroLinNo > 0)
                                                        {
                                                            _logger.Error("Found lines for Delivery# " + delivery.DocNum + " with 0 EDI Line#");
                                                        }

                                                        // 05-17-2020 begin 
                                                        int oZeroQty = checkZeroQty(oConnectionName, "DLN1", "ODLN", delivery.DocEntry, "17");
                                                        if (oZeroQty > 0)
                                                        {
                                                            _logger.Error("Found lines for Delivery# " + delivery.DocNum + " with zero quantity");
                                                        }
                                                        if (oZeroLinNo > 0 || oZeroQty > 0)
                                                        {
                                                            getDeliveryData(oConnectionName, delivery);
                                                        }
                                                        //oZeroLinNo = checkEDILnNo(oConnectionName, "DLN1", delivery.DocEntry, "17");
                                                        //oZeroQty = checkZeroQty(oConnectionName, "DLN1", delivery.DocEntry, "17");
                                                        oZeroQty = 0;
                                                        oZeroLinNo = 0;
                                                        foreach (DeliveryLine delLine in delivery.DeliveryLines)
                                                        {
                                                            if (delLine.TreeType == "S" || delLine.TreeType == "N")
                                                            {
                                                                if (delLine.Quantity <= 0)
                                                                {
                                                                    oZeroQty = oZeroQty + 1;
                                                                }
                                                                if (delLine.LineNumber850 <= 0)
                                                                {
                                                                    oZeroLinNo = oZeroLinNo + 1;
                                                                }
                                                            }
                                                        }
                                                        if (oZeroLinNo > 0 || oZeroQty > 0)
                                                        {
                                                            _logger.Error("Invalid qty or line# from sales order for Delivery# " + delivery.DocNum);
                                                        }
                                                        // 05-17-2020 end
                                                        // 05-15-2020 end
                                                        else
                                                        {
                                                            // 10-15-2019 end
                                                            listToProcess.Add(new Edi850WithDelivery(record, delivery));
                                                        } // 10-15-2019
                                                        //}
                                                    } // 05-15-2020 
                                                }
                                                else
                                                {
                                                    string oErr = "856 delivery line not found for 850 record with key " + record.HeaderId; // 04-08-2022
                                                    // _logger.Error("856 delivery line not found for 850 record with key " + record.HeaderId);
                                                }
                                                try
                                                {
                                                    System.Runtime.InteropServices.Marshal.ReleaseComObject(delivery); // 12-28-2022
                                                }
                                                catch
                                                {
                                                }
                                            } // 12-28-2022
                                        } // 12-28-2022
                                        // 06-11-2020 begin 
                                    }
                                    catch (Exception gdln)
                                    {
                                        _logger.Error("856 error getting delivery for record with key " + record.HeaderId + " => " + gdln.Message);
                                    }
                                }
                                else
                                {
                                    _logger.Error("856 skipped 855 not sent for 850 record with key " + record.HeaderId);
                                }
                                // 06-11-2020 end
                                /*
                                    // 08-06-2019 end
                                    _logger.Debug("Processing 856 with 850 Key " + record.HeaderId);
                                    delivery = FindMatchingDelivery(dbContext, record);

                                    if (delivery != null)
                                    {
                                        _logger.Debug("Found 856 delivery line from delivery with key " + delivery.DocNum);
                                        listToProcess.Add(new Edi850WithDelivery(record, delivery));
                                    }
                                    else
                                    {
                                        _logger.Debug("856 delivery line not found for 850 record with key " + record.HeaderId);
                                    }
                                   */

                                // 08-06-2019
                            } // 12-28-2022
                        }
                    } // 08-09-2023
                }

                // 08-09-2023 begin
                if (b940 == true && list940ToProcess.Count == 0)
                {
                    response.Successful = true;
                    _logger.Debug("No matching deliveries found to process.  0 856s sent. ");
                    response.ErrorMessage = "No matching 856s found";
                    _logger.Debug("Leaving Get856Records for " + oSBOCardCode);
                    return response;
                }
                else
                    // 08-09-2023 end
                    //if (listToProcess.Count == 0)
                    if (listToProcess.Count == 0 && b940 == false) // 08-15-2023
                    {
                        response.Successful = true;
                        _logger.Debug("No matching deliveries found to process.  0 856s sent. ");
                        response.ErrorMessage = "No matching 856s found"; // 02-12-2022
                        _logger.Debug("Leaving Get856Records for " + oSBOCardCode);
                        return response;
                    }
                    else
                    {
                        // 08-09-2023 begin
                        if (b940 == true && list940ToProcess.Count > 0)
                        {
                            _logger.Debug("Found " + list940ToProcess.Count + " Deliveries to be sent");
                        }
                        else if (b940 == false && listToProcess.Count > 0)
                        {
                            // 08-09-2023 end
                            _logger.Debug("Found " + listToProcess.Count + " Deliveries to be sent");
                        } // 08-09-2023
                    }
                bool send856Pack = getProcess856Pack(request.CardCode); // 02-09-2021
                // 08-15-2023 begin
                if (b940 == true)
                {
                    foreach (var selectedRecord in list940ToProcess)
                    {
                        try
                        {
                            if (iPreProcess856Record != null)
                            {
                                if
    (!iPreProcess856Record.OnPreProcess856Record(selectedRecord.Delivery, selectedRecord.Edi940HeaderRecord, oLast856Date, oCutoffDt))
                                {
                                    continue;
                                }
                            }
                            if ((!(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2Cc) &&
                                !(selectedRecord.Delivery.U_InfoW2Cc.Trim().ToUpper() == "UNSP") &&
                                !(selectedRecord.Delivery.U_InfoW2Cc.Trim().ToUpper() == "UNSP") &&
                                !(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL))))
                                || (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("TeeZed") && selectedRecord.Edi940HeaderRecord.CarrierCode == "LBLS"))
                            {
                                Edi856HeaderRecord record = new Edi856HeaderRecord();
                                response.Edi856Records.Add(record);
                                Int32 oNextASN = 0;
                                try
                                {
                                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                    {
                                        sqlConnection.Open();
                                        // get Ship From Address
                                        using (SqlCommand command = new SqlCommand("select  t0.[DeliveryNo], t0.[IntDelNo], t0.[WhsCode], t0.[ShipFromName], " +
                                              "t0.[ShipFromAddress1], t0.[ShipFromAddress2], t0.[ShipFromCity], t0.[ShipFromState], t0.[ShipFromZip], t0.[ShipFromCountry], t0.[ShipFromPhone], t0.[ShipFromFax] " +
                                            " from dbo.[Infocus_856_Delivery_Whs] t0 WITH(NOLOCK) where t0.IntDelNo = " + selectedRecord.Delivery.DocEntry, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (!reader.Read())
                                                {
                                                    _logger.Error("Could not set ship from address for Delivery # " + selectedRecord.Delivery.DocNum);
                                                    record.ShipFromName = "Corsan";
                                                }
                                                else
                                                {
                                                    _logger.Error("Setting ship from address for Delivery # " + selectedRecord.Delivery.DocNum);

                                                    record.ShipFromName = reader["ShipFromName"].ToString();
                                                    record.ShipFromAddress1 = reader["ShipFromAddress1"].ToString();
                                                    record.ShipFromAddress2 = reader["ShipFromAddress2"].ToString();
                                                    record.ShipFromCity = reader["ShipFromCity"].ToString();
                                                    record.ShipFromState = reader["ShipFromState"].ToString();
                                                    record.ShipFromZip = reader["ShipFromZip"].ToString();
                                                    record.ShipFromCountry = reader["ShipFromCountry"].ToString();
                                                }
                                            }
                                        }
                                        if (selectedRecord.Delivery.CardCode == "3PL-C0006" || selectedRecord.Edi940HeaderRecord.CardCode.ToUpper().StartsWith("TEEZ"))
                                        {
                                            string oSqlQry = "select IsNull(c1.[U_InfoNextASN], 0) NextAsn from dbo.OCRD c1 WITH(NOLOCK) where c1.CardCode = '" + selectedRecord.Edi940HeaderRecord.SBOCardCode.Trim() + "'";
                                            using (SqlCommand command = new SqlCommand(oSqlQry, sqlConnection))
                                            {
                                                try
                                                {
                                                    using (SqlDataReader reader = command.ExecuteReader())
                                                    {
                                                        if (!reader.Read())
                                                        {
                                                            _logger.Error("Could not get NextASN for Delivery # " + selectedRecord.Delivery.DocNum);
                                                        }
                                                        else
                                                        {
                                                            oNextASN = Convert.ToInt32(reader["NextAsn"].ToString());
                                                        }
                                                    }
                                                }
                                                catch (Exception na)
                                                {
                                                    oNextASN = selectedRecord.Delivery.DocNum;
                                                    string oErr = na.Message;
                                                    _logger.Error("Could not get next ASN # for CardCode " + selectedRecord.Edi940HeaderRecord.SBOCardCode + "=> " + oErr);
                                                }
                                            }
                                        }
                                        if (oNextASN <= 0)
                                        {
                                            oNextASN = selectedRecord.Delivery.DocNum;
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                                catch (Exception sf)
                                {
                                    string oErr = sf.Message;
                                    _logger.Error("Could not set ship from address for Delivery # " + selectedRecord.Delivery.DocNum);
                                    record.ShipFromName = "Corsan";
                                }
                                record.CardCode = selectedRecord.Edi940HeaderRecord.CardCode;
                                string oIs3PL = "N";
                                string oQry1 = "select coalesce(w0.[3PL],'N') Is3PL from InfocusEDI.dbo.WebApiDbContext w0  WITH(NOLOCK) where w0.CardCode = '"
                                               + selectedRecord.Edi940HeaderRecord.CardCode.Trim() +
                                               "' and w0.SBOCardCode = '" + selectedRecord.Edi940HeaderRecord.SBOCardCode.Trim() + "'";
                                try
                                {
                                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                    {
                                        sqlConnection.Open();

                                        using (SqlCommand command = new SqlCommand(oQry1, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (!reader.Read())
                                                {
                                                    oIs3PL = "N";
                                                }
                                                else
                                                {
                                                    oIs3PL = reader["Is3PL"].ToString();
                                                }
                                            }
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                                catch (Exception pl3)
                                {
                                    oIs3PL = "N";
                                    String oErr = pl3.Message;
                                }
                                if (String.IsNullOrWhiteSpace(oIs3PL))
                                {
                                    oIs3PL = "N";
                                }
                                _logger.Debug("Is 3PL? " + oIs3PL);
                                if (oIs3PL == "Y")
                                {
                                    string o3PLCarrier = selectedRecord.Delivery.U_InfoW2Cc.ToString().Trim();
                                    try
                                    {
                                        o3PLCarrier = o3PLCarrier.Replace("3PL_", " ");
                                        o3PLCarrier = o3PLCarrier.Replace("3PL-", " ");
                                        o3PLCarrier = o3PLCarrier.Trim();
                                    }
                                    catch
                                    {

                                    }
                                    record.CarrierCode = o3PLCarrier;
                                }
                                else
                                {
                                    record.CarrierCode = selectedRecord.Delivery.U_InfoW2Cc;
                                }
                                if (oSBOCardCode.StartsWith("IndoCount") || selectedRecord.Delivery.CardCode == "3PL-C0018"
                                   || selectedRecord.Delivery.CardCode == "3PL-C0024") // 09-08-2023
                                {
                                    String oCarrier = get3PLCarrier(oConnectionName, selectedRecord.Edi940HeaderRecord.SBOCardCode, selectedRecord.Delivery.U_InfoW2Cc);
                                    if (!String.IsNullOrWhiteSpace(oCarrier))
                                    {
                                        record.CarrierCode = oCarrier;
                                    }
                                }
                                _logger.Debug("CarrierCode: " + record.CarrierCode);
                                record.BillOfLading = selectedRecord.Delivery.U_Info_BOL; // 09-08-2023
                                record.DeliveryPhoneNumber = selectedRecord.Edi940HeaderRecord.ShipToPhoneNo;
                                record.Department = selectedRecord.Edi940HeaderRecord.DepartmentNumber;
                                record.PaymentMethod = selectedRecord.Edi940HeaderRecord.PaymentMethod;
                                record.PartnerId = selectedRecord.Edi940HeaderRecord.CustCode3PL; // 09-15-2023
                                if (selectedRecord.Edi940HeaderRecord.PurchaseOrderDate == null ||
                                   selectedRecord.Edi940HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                                {
                                    record.PurchaseOrderDate = selectedRecord.Edi940HeaderRecord.RecordDate;
                                }
                                else
                                {
                                    record.PurchaseOrderDate = selectedRecord.Edi940HeaderRecord.PurchaseOrderDate;
                                }
                                if (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("TeeZed"))
                                {
                                    string oPONo = selectedRecord.Edi940HeaderRecord.PurchaseOrderReference;
                                    if (oPONo.Contains("-"))
                                    {
                                        string[] oPoParsed = oPONo.Split('=');
                                        if (oPoParsed.Length > 0)
                                        {
                                            oPONo = oPoParsed[0];
                                        }
                                    }
                                    record.PurchaseOrderReference = oPONo;
                                }
                                else
                                {
                                    record.PurchaseOrderReference = selectedRecord.Edi940HeaderRecord.PurchaseOrderReference;
                                }
                                _logger.Debug("PurchaseOrderReference: " + record.PurchaseOrderReference);

                                if (!(selectedRecord.Edi940HeaderRecord.RequestedShipDate == null))
                                {
                                    record.RequestedShipDate = selectedRecord.Edi940HeaderRecord.RequestedShipDate;
                                }
                                record.ShipToAddress1 = selectedRecord.Edi940HeaderRecord.ShipToAddress1;
                                record.ShipToAddress2 = selectedRecord.Edi940HeaderRecord.ShipToAddress2;
                                record.ShipToAttention = selectedRecord.Edi940HeaderRecord.ShipToAttention;
                                record.ShipToCity = selectedRecord.Edi940HeaderRecord.ShipToCity;
                                record.ShipToCountry = selectedRecord.Edi940HeaderRecord.ShipToCountry;
                                record.ShipToLocationCode = selectedRecord.Edi940HeaderRecord.ShipToLocationCode;
                                record.ShipToStoreLocation = selectedRecord.Edi940HeaderRecord.StoreNumber;
                                record.ShipToName = selectedRecord.Edi940HeaderRecord.ShipToName;
                                record.ShipToState = selectedRecord.Edi940HeaderRecord.ShipToState;
                                record.ShipToZip = selectedRecord.Edi940HeaderRecord.ShipToZip;
                                record.VendorNumber = selectedRecord.Edi940HeaderRecord.VendorNumber;

                                _logger.Debug("Get Sender and Receiver for " + selectedRecord.Edi940HeaderRecord.CustCode3PL);
                                //12-12-2023 begin
                                /*
                                record.SenderQual = "01";
                                record.SenderId = selectedRecord.Edi940HeaderRecord.CardCode;
                                record.ReceiverId = selectedRecord.Edi940HeaderRecord.CustCode3PL;
                                record.ReceiverQual = "02";/
                                 */
                                string cardCode = selectedRecord.Edi940HeaderRecord.SBOCardCode;
                                if (record.CardCode == "IndoCntRetT")
                                {
                                    record.SenderId = "7039064436T";
                                }
                                else
                                {
                                    record.SenderId = "7039064436";
                                }

                                record.SenderQual = "12";
                                /*
                                if (selectedRecord.Edi940HeaderRecord.CustCode3PL == "C0002")
                                 {
                                                                    record.ReceiverQual = "08";
                                                                    record.ReceiverId = "6112390050";
                                                                }
                                                                else if (selectedRecord.Edi940HeaderRecord.CustCode3PL == "C0003")
                                                                {
                                                                    record.ReceiverQual = "08";
                                                                    record.ReceiverId = "6123830000";
                                                                }
                                                                else if (selectedRecord.Edi940HeaderRecord.CustCode3PL == "C0004")
                                                                {
                                                                    record.ReceiverQual = "12";
                                                                    record.ReceiverId = "8137472355";
                                                                }
                                                                */
                                string oQuery = "select U_Qual as'Qual', U_Id as 'ReceiverId' from [@C3_EDIRETAILIDS] where U_CardCode = '" + cardCode.Trim() +
                                  "' and U_RCode = '" + selectedRecord.Edi940HeaderRecord.CustCode3PL.Trim() + "'";
                                _logger.Debug("ReceiverId query: " + oQuery);
                                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                {
                                    sqlConnection.Open();

                                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                                    {
                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            if (reader.Read())
                                            {
                                                try
                                                {
                                                    String oValue = (String)reader["Qual"].ToString();
                                                    try
                                                    {
                                                        record.ReceiverQual = oValue;
                                                    }
                                                    catch (Exception qual)
                                                    {
                                                        String oErr = qual.Message;
                                                        _logger.Error("Error getting the receiver qualifier for Delivery " + selectedRecord.Delivery.DocNum + " =>" + oErr);
                                                    }
                                                    oValue = (String)reader["ReceiverId"].ToString();
                                                    try
                                                    {
                                                        record.ReceiverId = oValue;
                                                    }
                                                    catch (Exception rid)
                                                    {
                                                        String oErr = rid.Message;
                                                        _logger.Error("Error getting the receiver id for Delivery " + selectedRecord.Delivery.DocNum + " =>" + oErr);
                                                    }
                                                }
                                                catch (Exception e)
                                                {
                                                    string oErrMsg = e.Message;
                                                    _logger.Debug("Error getting ReceiverId: " + e.Message);
                                                }
                                            }
                                        }
                                    }
                                }
                                // 12-12-2023 end

                                _logger.Debug("Set ShipTo Address");

                                if (selectedRecord.Delivery.U_InfoW2SWgt == null)
                                {
                                    selectedRecord.Delivery.U_InfoW2SWgt = 0;
                                }
                                if (!(selectedRecord.Delivery.U_InfoW2SWgt == null))
                                {
                                    record.ShipmentWeight = selectedRecord.Delivery.U_InfoW2SWgt;
                                }
                                if (!(selectedRecord.Delivery.U_InfoW2Cartons == null))
                                {
                                    record.ShipmentCartons = selectedRecord.Delivery.U_InfoW2Cartons;
                                }
                                if (!String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2ServiceLev))
                                {
                                    record.ServiceLevel = selectedRecord.Delivery.U_InfoW2ServiceLev;
                                }
                                if (oIs3PL == "Y" && oNextASN != selectedRecord.Delivery.DocNum
                                    && selectedRecord.Delivery.CardCode == "3PL-C0006") // 04-26-2022
                                {
                                    record.ShipmentNumber = oNextASN.ToString();
                                    incrementNextASN(selectedRecord.Delivery.CardCode, oNextASN, oConnectionName);
                                }
                                else
                                {
                                    record.ShipmentNumber = selectedRecord.Delivery.DocEntry.ToString(); // 10-31-2019
                                }
                                if (selectedRecord.Delivery.U_InfoW2CnNo < 0)
                                {
                                    record.ConfirmationNo = 0;
                                }
                                else
                                {
                                    record.ConfirmationNo = selectedRecord.Delivery.U_InfoW2CnNo;
                                }
                                /*record.OrderBuyCode = selectedRecord.Delivery.U_InfoW2BCode;
                                 record.OrderBuyName = selectedRecord.Delivery.U_InfoW2BName;
                                 record.OrderBuyAddr1 = selectedRecord.Delivery.U_InfoW2BAd1;
                                 record.OrderBuyAddr2 = selectedRecord.Delivery.U_InfoW2BAd2;
                                 record.OrderBuyCity = selectedRecord.Delivery.U_InfoW2BCity;
                                 record.OrderBuyState = selectedRecord.Delivery.U_InfoW2BState;
                                 record.OrderBuyZip = selectedRecord.Delivery.U_InfoW2BZip;
                                 record.OrderBuyCountryCd = selectedRecord.Delivery.U_InfoW2BCntry;
                                 * */
                                if (!(selectedRecord.Delivery.U_InfoW2Job == null))
                                {
                                    record.JobNumber = selectedRecord.Delivery.U_InfoW2Job;
                                }
                                _logger.Debug("Setting Pro#");
                                if (String.IsNullOrEmpty(selectedRecord.Delivery.U_Info_Pro))
                                {
                                    try
                                    {
                                        if ((selectedRecord.Edi940HeaderRecord.CardCode.ToUpper().Trim() == "WAYFAIR" ||
                                            selectedRecord.Edi940HeaderRecord.CardCode.ToUpper().StartsWith("WAYFAIR"))
                                            && String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_Pro))
                                        {
                                            if (selectedRecord.Delivery.U_Info_BOL.Trim().Length > 48)
                                            {
                                                char[] cList = new char[2];
                                                cList[0] = ',';
                                                cList[1] = ' ';
                                                int oLastPos = selectedRecord.Delivery.U_Info_BOL.LastIndexOfAny(cList, 0);
                                                string pBOL = "";
                                                if (selectedRecord.Delivery.U_Info_BOL.Trim().Length < 48)
                                                {
                                                    if (oLastPos > 0)
                                                    {
                                                        pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, oLastPos);
                                                    }
                                                    else
                                                    {
                                                        pBOL = selectedRecord.Delivery.U_Info_BOL.Trim();
                                                    }
                                                }
                                                else
                                                {
                                                    pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                                    string lastChar = pBOL.Substring(47);
                                                    oLastPos = pBOL.LastIndexOfAny(cList, 0);
                                                    if (oLastPos > 0 && lastChar != " ")
                                                    {
                                                        record.ProNumber = pBOL.Substring(0, oLastPos - 1);
                                                    }
                                                    else
                                                    {
                                                        record.ProNumber = pBOL;
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                record.ProNumber = selectedRecord.Delivery.U_Info_BOL;
                                            }

                                        }
                                        else
                                        {
                                            record.ProNumber = String.Empty;
                                        }
                                        _logger.Debug("Exit Set Pro#");
                                    }
                                    catch
                                    {

                                    }
                                }
                                else
                                {
                                    if (!(String.IsNullOrWhiteSpace(record.BillOfLading)))
                                    {
                                        record.ProNumber = record.BillOfLading;
                                    }
                                    else
                                    {
                                        record.ProNumber = "";
                                    }
                                }
                                if (oIs3PL == "Y")
                                {
                                    _logger.Debug("Setting Master BOL");
                                    if (!String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_C3_MstrBOL))
                                    {
                                        record.MasterBOL = selectedRecord.Delivery.U_C3_MstrBOL;
                                    }
                                    else
                                    {
                                        record.MasterBOL = "";
                                    }
                                    _logger.Debug("Master BOL:" + record.MasterBOL);
                                }

                                record.AsnShipDate = selectedRecord.Delivery.DocDueDate;
                                record.TransportationMethod = String.Empty;
                                _logger.Debug("Set Transport Method");
                                try
                                {
                                    if (!String.IsNullOrWhiteSpace(selectedRecord.Edi940HeaderRecord.TransportMethod))
                                    {
                                        record.TransportationMethod = selectedRecord.Edi940HeaderRecord.TransportMethod;
                                    }
                                }
                                catch
                                {
                                }
                                record.TransportRouting = String.Empty;

                                if (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("TSC"))
                                {
                                    if (record.TransportationMethod == "M")
                                    {
                                        record.TransportationMethod = "LT";
                                    }
                                    String sql = "select top 1 isnull(s0.TrnspName, '') as TransportName from dbo.OSHP s0 WITH(NOLOCK) " +
                                                 "where s0.TrnspCode = " + selectedRecord.Delivery.TrnspCode;
                                    try
                                    {
                                        using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                        {
                                            sqlConnection.Open();

                                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                                            {
                                                using (SqlDataReader reader = command.ExecuteReader())
                                                {
                                                    reader.Read();
                                                    record.TransportRouting = (String)reader[0];
                                                }
                                            }
                                            sqlConnection.Close();
                                        }
                                    }
                                    catch (Exception tr)
                                    {
                                        _logger.Error("Error getting transport routing =>" + tr.Message);
                                    }
                                }
                                if (oIs3PL == "Y")
                                {
                                    try
                                    {
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_C3_FreightCst.ToString())) // 06-07-2021
                                        {
                                            string oFreightCst = selectedRecord.Delivery.U_C3_FreightCst.ToString();
                                            record.FreightCost = Convert.ToDecimal(oFreightCst);
                                        }
                                        else
                                        {
                                            record.FreightCost = 0;
                                        }
                                    }
                                    catch
                                    {
                                        record.FreightCost = Convert.ToDecimal("0.00");
                                    }
                                }
                                else
                                {
                                    record.FreightCost = Convert.ToDecimal("0.00");
                                }

                                char[] charList = new char[2];
                                charList[0] = ',';
                                charList[1] = ' ';
                                _logger.Debug("Parse BOL");
                                if ((selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("HDCL")
                                    || selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("WAYFAIR")
                                    || selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("HAYCL"))
                                    && !String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL)
                                    && selectedRecord.Delivery.U_Info_BOL.Trim().Length > 30)
                                {
                                    try
                                    {
                                        string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 30);
                                        int oLastPos = pBOL.LastIndexOfAny(charList);
                                        string lastChar = pBOL.Substring(29);
                                        if (oLastPos > 0 && lastChar != " ")
                                        {
                                            record.BillOfLading = pBOL.Substring(0, oLastPos - 1);
                                        }
                                        else
                                        {
                                            record.BillOfLading = pBOL;
                                        }
                                        if (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            record.BillOfLading = record.BillOfLading.Replace("-", "");
                                        }
                                        selectedRecord.Delivery.U_Info_BOL = record.BillOfLading; // testing
                                    }
                                    catch
                                    {

                                    }
                                }
                                else if ((selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("LowesNet")
                                || selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("TSCCL"))
                                     && !String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL)
                                     && selectedRecord.Delivery.U_Info_BOL.Trim().Length > 48)
                                {
                                    string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                    int oLastPos = pBOL.LastIndexOfAny(charList);
                                    string lastChar = pBOL.Substring(47);
                                    if (oLastPos > 0 && lastChar != " ")
                                    {
                                        record.BillOfLading = pBOL.Substring(0, oLastPos - 1);
                                    }
                                    else
                                    {
                                        record.BillOfLading = pBOL;
                                    }
                                    if (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                                    {
                                        record.BillOfLading = record.BillOfLading.Replace("-", "");
                                    }

                                    selectedRecord.Delivery.U_Info_BOL = record.BillOfLading; // testing
                                }
                                else
                                {
                                    if (selectedRecord.Delivery.U_Info_BOL.Trim().Length > 48)
                                    {
                                        string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                        int oLastPos = pBOL.LastIndexOfAny(charList);
                                        string lastChar = pBOL.Substring(47);
                                        if (oLastPos > 0 && lastChar != " ")
                                        {
                                            record.BillOfLading = pBOL.Substring(0, oLastPos - 1);
                                        }
                                        else
                                        {
                                            record.BillOfLading = pBOL;
                                        }
                                        if (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            record.BillOfLading = record.BillOfLading.Replace("-", "");
                                        }
                                    }
                                    else
                                    {
                                        record.BillOfLading = selectedRecord.Delivery.U_Info_BOL;
                                        if (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            record.BillOfLading = record.BillOfLading.Replace("-", "");
                                        }
                                    }
                                    selectedRecord.Delivery.U_Info_BOL = record.BillOfLading; // testing
                                }
                                _logger.Debug("BOL #" + record.BillOfLading);

                                record.DeliveryNumber = selectedRecord.Delivery.DocNum;
                                if (send856Pack == true)
                                {
                                    record.Structure = "0001";
                                }
                                else
                                {
                                    record.Structure = "0055";
                                }
                                try
                                {
                                    string oSQry = "select p0.NoPKgs, p0.ShipWgt from dbo.[Infocus_Delivery_PkgTotals]  p0 WITH(NOLOCK) where p0.DocEntry =  " + selectedRecord.Delivery.DocEntry;
                                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                    {
                                        sqlConnection.Open();

                                        using (SqlCommand command = new SqlCommand(oSQry, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (reader.Read())
                                                {
                                                    try
                                                    {
                                                        String oValue = (String)reader["NoPkgs"].ToString();
                                                        try
                                                        {
                                                            decimal oNoPkgs = Convert.ToDecimal(oValue);
                                                            record.ShipmentCartons = oNoPkgs;
                                                        }
                                                        catch (Exception pkg)
                                                        {
                                                            String oErr = pkg.Message;
                                                            _logger.Error("Error getting number of packages for Delivery " + selectedRecord.Delivery.DocNum + " =>" + oErr);
                                                        }
                                                        oValue = (String)reader["ShipWgt"].ToString();
                                                        try
                                                        {
                                                            decimal oShipWgt = Convert.ToDecimal(oValue);
                                                            record.ShipmentWeight = oShipWgt;
                                                        }
                                                        catch (Exception pkg)
                                                        {
                                                            String oErr = pkg.Message;
                                                            _logger.Error("Error getting number of shipment weight for Delivery " + selectedRecord.Delivery.DocNum + " =>" + oErr);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string ErrMesg = e.Message;
                                                        _logger.Error("Error executing [Infocus_Delivery_PkgTotals] =>" + ErrMesg);
                                                    }
                                                }
                                            }
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                                catch (Exception stot)
                                {
                                    _logger.Error(stot.Message);
                                }
                                _logger.Debug("Process Tracking numbers");
                                String[] oTrackNos = new String[1];
                                if (oIs3PL == "Y")
                                {
                                    try
                                    {
                                        string oW2Notes = "";
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2Notes))
                                        {
                                            oW2Notes = selectedRecord.Delivery.U_InfoW2Notes.ToString();
                                            if (!String.IsNullOrWhiteSpace(oW2Notes))
                                            {
                                                oTrackNos = oW2Notes.Split(',');
                                                if (oTrackNos.Length == 0)
                                                {
                                                    oTrackNos = new String[1];
                                                    oTrackNos[0] = oW2Notes.Trim();
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception TrkNoErr)
                                    {
                                        string ErrMesg = TrkNoErr.Message;
                                        _logger.Error("Error executing getting tracking numbers for U_InfoW2Notes => " + ErrMesg);
                                        oTrackNos = new String[1];
                                        oTrackNos[0] = selectedRecord.Delivery.TrackNo;
                                    }
                                }

                                if (oIs3PL == "Y" && selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("TeeZed"))
                                {
                                    oTrackNos[0] = "";
                                }

                                _logger.Debug("Process lines for " + selectedRecord.Edi940HeaderRecord.SBOCardCode); // 05-21-2021

                                if (iPostProcess856Record != null || (bSendPack == true && iPostProcess856Record != null)) // 09-02-2021 added check for SendPack
                                {
                                    if (bSendPack == true)
                                    {
                                        iPostProcess856Record.OnPostProcess856Record(selectedRecord.Delivery, selectedRecord.Edi940HeaderRecord, record, oIs3PL);
                                    }
                                    else
                                    {
                                        iPostProcess856Record.OnPostProcess856Record(selectedRecord.Delivery, selectedRecord.Edi940HeaderRecord, record, oIs3PL, oTrackNos);
                                    }
                                }
                                //DateTime oProcessDT = DateTime.Now;
                                if (selectedRecord.Edi940HeaderRecord.Orig856ProcessedDateTime.ToString().Trim().Length == 0)
                                {
                                    selectedRecord.Edi940HeaderRecord.Orig856ProcessedDateTime = DateTime.Now;
                                }
                                record.TrxDateTime = Convert.ToDateTime(selectedRecord.Edi940HeaderRecord.Orig856ProcessedDateTime); // 03-10-2022

                                selectedRecord.Edi940HeaderRecord.Processed856 = true;
                                selectedRecord.Edi940HeaderRecord.HasOpen856 = false;
                                selectedRecord.Delivery.U_InfoW2856 = "Y";
                                selectedRecord.Edi940HeaderRecord.Processed856DateTime = DateTime.Now;
                                if (selectedRecord.Edi940HeaderRecord.Orig856ProcessedDateTime == null
                                    || selectedRecord.Edi940HeaderRecord.Orig856ProcessedDateTime.ToString().Trim().Length == 0)
                                {
                                    selectedRecord.Edi940HeaderRecord.Orig856ProcessedDateTime = selectedRecord.Edi940HeaderRecord.Processed856DateTime;
                                    // 12-19-2023 begin
                                    int oDelNo = selectedRecord.Delivery.DocEntry;
                                    SAPbobsCOM.Company _Company = getCompany();
                                    bool bConnected = _Company.Connected;
                                    if (bConnected)
                                    {
                                        try
                                        {
                                            SAPbobsCOM.Documents oDel = _Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes) as SAPbobsCOM.Documents;
                                            bool bFound = oDel.GetByKey(oDelNo);
                                            if (bFound)
                                            {
                                                if (!String.IsNullOrWhiteSpace(oDel.UserFields.Fields.Item("U_Info850HdrId").Value.ToString())
                                                    && oDel.UserFields.Fields.Item("U_Info850HdrId").Value.ToString().Length > 0)
                                                {
                                                    oDel.UserFields.Fields.Item("U_Info850HdrId").Value = "Y";
                                                    try
                                                    {
                                                        int oRet = oDel.Update();
                                                        if (oRet != 0)
                                                        {
                                                            String oErrMsg = _Company.GetLastErrorDescription();
                                                            int oErrCode = _Company.GetLastErrorCode();
                                                            oErrMsg = oErrMsg + ", ErrorCode " + oErrCode.ToString();
                                                            _logger.Error("Error setting 856 Sent flag for Delivery #" + selectedRecord.Delivery.DocNum.ToString() + " => " + oErrMsg);
                                                        }
                                                    }
                                                    catch (Exception d)
                                                    {
                                                        _logger.Error("Error updating delivery 'Sent 856': " + d.Message);
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception d0)
                                        {
                                            //_logger.Error("Error updating delivery Sent 856 using HeaderId: " + d0.Message);
                                        }
                                    }
                                    // 12-19-2023 end
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            _logger.Error(ex.Message);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                        }

                    }

                }
                else
                {
                    // 08-15-2023 end
                    // To do -Need to filter records, but not sure how to right now jbb
                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            if (iPreProcess856Record != null)
                            {
                                // 02-15-2022 begin
                                //if (!iPreProcess856Record.OnPreProcess856Record(selectedRecord.Delivery, selectedRecord.Edi850HeaderRecord))
                                //if (!iPreProcess856Record.OnPreProcess856Record(selectedRecord.Delivery, selectedRecord.Edi850HeaderRecord, oLast856Date)) // 02-15-2022 end
                                if (!iPreProcess856Record.OnPreProcess856Record(selectedRecord.Delivery, selectedRecord.Edi850HeaderRecord, oLast856Date, oCutoffDt)) // 04-08-2022                               
                                {
                                    continue;
                                }
                            }
                            // _logger.Debug("Processing 856 for 850 Key " + selectedRecord.Edi850HeaderRecord.HeaderId + ", CardCode " + selectedRecord.Edi850HeaderRecord.CardCode + " PO# " + selectedRecord.Edi850HeaderRecord.PurchaseOrderReference); // 10-07-2019
                            // 04-19-2018 begin
                            if ((!(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2Cc) &&
                                !(selectedRecord.Delivery.U_InfoW2Cc.Trim().ToUpper() == "UNSP") && // 06-30-2023
                                !(selectedRecord.Delivery.U_InfoW2Cc.Trim().ToUpper() == "UNSP") && // 06-30-2023
                                !(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL))))
                                || (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TeeZed") && selectedRecord.Edi850HeaderRecord.CarrierCode == "LBLS")) // 07-19-2021
                            { // 04-19-2018 end
                                //bool bSendPack = getProcess856Pack(request.CardCode); // 02-11-2021

                                Edi856HeaderRecord record = new Edi856HeaderRecord();
                                /*
                                   if (bSendPack == true)
                                   {
                                       record = null;
                                       record = new Edi856PHeaderRecord(); // 09-24-2021
                                       response.Edi856PRecords.Add(record);
                                   }
                                   else
                                   {*/
                                response.Edi856Records.Add(record);
                                // }
                                Int32 oNextASN = 0;
                                // 03-04-2019 begin

                                try
                                {
                                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                    {
                                        sqlConnection.Open();

                                        // get Ship From Address
                                        using (SqlCommand command = new SqlCommand("select  t0.[DeliveryNo], t0.[IntDelNo], t0.[WhsCode], t0.[ShipFromName], " +
                                              "t0.[ShipFromAddress1], t0.[ShipFromAddress2], t0.[ShipFromCity], t0.[ShipFromState], t0.[ShipFromZip], t0.[ShipFromCountry], t0.[ShipFromPhone], t0.[ShipFromFax] " +
                                            " from dbo.[Infocus_856_Delivery_Whs] t0 WITH(NOLOCK) where t0.IntDelNo = " + selectedRecord.Delivery.DocEntry, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (!reader.Read())
                                                {
                                                    _logger.Error("Could not set ship from address for Delivery # " + selectedRecord.Delivery.DocNum);
                                                    record.ShipFromName = "Corsan";
                                                    //record.ShipFromCode = "9999";
                                                }
                                                else
                                                {
                                                    //record.ShipFromCode = "9999";
                                                    record.ShipFromName = reader["ShipFromName"].ToString();
                                                    record.ShipFromAddress1 = reader["ShipFromAddress1"].ToString();
                                                    record.ShipFromAddress2 = reader["ShipFromAddress2"].ToString();
                                                    record.ShipFromCity = reader["ShipFromCity"].ToString();
                                                    record.ShipFromState = reader["ShipFromState"].ToString();
                                                    record.ShipFromZip = reader["ShipFromZip"].ToString();
                                                    record.ShipFromCountry = reader["ShipFromCountry"].ToString();

                                                }
                                            }
                                        }
                                        // 04-26-2022 begin
                                        if (selectedRecord.Delivery.CardCode == "3PL-C0006" || selectedRecord.Edi850HeaderRecord.CardCode.ToUpper().StartsWith("TEEZ"))
                                        { // 04-26-2022 end
                                            // 05-25-2021 begin
                                            string oSqlQry = "select IsNull(c1.[U_InfoNextASN], 0) NextAsn from dbo.OCRD c1 WITH(NOLOCK) where c1.CardCode = '" + selectedRecord.Edi850HeaderRecord.SBOCardCode.Trim() + "'";
                                            using (SqlCommand command = new SqlCommand(oSqlQry, sqlConnection))
                                            {
                                                try
                                                {
                                                    using (SqlDataReader reader = command.ExecuteReader())
                                                    {
                                                        if (!reader.Read())
                                                        {
                                                            _logger.Error("Could not get NextASN for Delivery # " + selectedRecord.Delivery.DocNum);
                                                        }
                                                        else
                                                        {
                                                            oNextASN = Convert.ToInt32(reader["NextAsn"].ToString());
                                                        }
                                                    }
                                                }
                                                catch (Exception na)
                                                {
                                                    oNextASN = selectedRecord.Delivery.DocNum;
                                                    string oErr = na.Message;
                                                    _logger.Error("Could not get next ASN # for CardCode " + selectedRecord.Edi850HeaderRecord.SBOCardCode + "=> " + oErr);
                                                }
                                            }
                                        } // 04-26-2022
                                        if (oNextASN <= 0)
                                        {
                                            oNextASN = selectedRecord.Delivery.DocNum;
                                        }
                                        // 05-25-2021 end
                                        sqlConnection.Close();
                                    }
                                }
                                catch (Exception sf)
                                {
                                    string oErr = sf.Message;
                                    _logger.Error("Could not set ship from address for Delivery # " + selectedRecord.Delivery.DocNum);
                                    record.ShipFromName = "Corsan";
                                    //record.ShipFromCode = "9999";
                                }
                                // 03-04-2019 end
                                record.BuyerName = selectedRecord.Edi850HeaderRecord.BuyerName;
                                record.CardCode = selectedRecord.Edi850HeaderRecord.CardCode;

                                // 05-23-2021 begin
                                string oIs3PL = "N";
                                string oQry1 = "select coalesce(w0.[3PL],'N') Is3PL from InfocusEDI.dbo.WebApiDbContext w0  WITH(NOLOCK) where w0.CardCode = '" + selectedRecord.Edi850HeaderRecord.CardCode.Trim() +
                                               "' and w0.SBOCardCode = '" + selectedRecord.Edi850HeaderRecord.SBOCardCode.Trim() + "'";
                                try
                                {
                                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                    {
                                        sqlConnection.Open();

                                        using (SqlCommand command = new SqlCommand(oQry1, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (!reader.Read())
                                                {
                                                    oIs3PL = "N";
                                                }
                                                else
                                                {
                                                    oIs3PL = reader["Is3PL"].ToString();
                                                }
                                            }
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                                catch (Exception pl3)
                                {
                                    oIs3PL = "N";
                                    String oErr = pl3.Message;
                                }
                                if (String.IsNullOrWhiteSpace(oIs3PL))
                                {
                                    oIs3PL = "N";
                                }

                                if (oIs3PL == "Y")
                                {
                                    string o3PLCarrier = selectedRecord.Delivery.U_InfoW2Cc.ToString().Trim();
                                    try
                                    {
                                        o3PLCarrier = o3PLCarrier.Replace("3PL_", " ");
                                        o3PLCarrier = o3PLCarrier.Replace("3PL-", " ");
                                        o3PLCarrier = o3PLCarrier.Trim();
                                    }
                                    catch
                                    {

                                    }
                                    record.CarrierCode = o3PLCarrier;
                                }
                                //  05-23-2021 end
                                else
                                {
                                    record.CarrierCode = selectedRecord.Delivery.U_InfoW2Cc;
                                }
                                // 03-02-2022 begin
                                if (oSBOCardCode.StartsWith("IndoCount") || selectedRecord.Delivery.CardCode == "3PL-C0018")
                                {
                                    String oCarrier = get3PLCarrier(oConnectionName, selectedRecord.Edi850HeaderRecord.SBOCardCode, selectedRecord.Delivery.U_InfoW2Cc);
                                    if (!String.IsNullOrWhiteSpace(oCarrier))
                                    {
                                        record.CarrierCode = oCarrier;
                                    }
                                }
                                // 03-02-2022 end
                                record.ConditionDescription = selectedRecord.Edi850HeaderRecord.ConditionDescription;
                                record.DeliveryPhoneNumber = selectedRecord.Edi850HeaderRecord.DeliveryPhoneNumber;
                                record.Department = selectedRecord.Edi850HeaderRecord.Department;
                                record.PaymentMethod = selectedRecord.Edi850HeaderRecord.PaymentMethod;
                                record.PromotionChargeCode = selectedRecord.Edi850HeaderRecord.PromotionChargeCode;
                                // 08-28-2019 begin
                                if (selectedRecord.Edi850HeaderRecord.PurchaseOrderDate == null ||
                                    selectedRecord.Edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                                {
                                    record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.RecordDate;
                                }
                                else
                                { // 08-28-2019 end
                                    record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.PurchaseOrderDate;
                                } // 08-28-2019
                                // 07-19-2021 begin
                                if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TeeZed"))
                                {
                                    string oPONo = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                                    if (oPONo.Contains("-"))
                                    {
                                        string[] oPoParsed = oPONo.Split('=');
                                        if (oPoParsed.Length > 0)
                                        {
                                            oPONo = oPoParsed[0];
                                        }
                                    }
                                    record.PurchaseOrderReference = oPONo;
                                }
                                else
                                { // 07-19-2021 end
                                    record.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                                } // 07-19-2021
                                record.ReplenishmentNumber = selectedRecord.Edi850HeaderRecord.ReplenishmentNumber;
                                if (!(selectedRecord.Edi850HeaderRecord.RequestedDeliveryDate == null))
                                {
                                    record.RequestedDeliveryDate = selectedRecord.Edi850HeaderRecord.RequestedDeliveryDate;
                                }
                                if (!(selectedRecord.Edi850HeaderRecord.RequestedShipDate == null))
                                {
                                    record.RequestedShipDate = selectedRecord.Edi850HeaderRecord.RequestedShipDate;
                                }
                                record.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                                record.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                                record.ShipToAttention = selectedRecord.Edi850HeaderRecord.ShipToAttention;
                                record.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                                record.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                                record.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                                record.ShipToStoreLocation = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation; // 11-02-2016
                                record.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                                record.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                                record.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                                record.TruckLoadNumber = selectedRecord.Edi850HeaderRecord.TruckLoadNumber;
                                record.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                                // 07-19-2017 begin
                                if (selectedRecord.Delivery.U_InfoW2SWgt == null)
                                {
                                    selectedRecord.Delivery.U_InfoW2SWgt = 0;
                                }
                                record.ShipmentWeight = selectedRecord.Delivery.U_InfoW2SWgt;
                                // 02-12-2019 begin
                                record.ShipmentCartons = selectedRecord.Delivery.U_InfoW2Cartons;
                                record.ServiceLevel = selectedRecord.Delivery.U_InfoW2ServiceLev;
                                // 02-12-2019 end
                                // 05-25-2021 begin
                                if (oIs3PL == "Y" && oNextASN != selectedRecord.Delivery.DocNum
                                    && selectedRecord.Delivery.CardCode == "3PL-C0006") // 04-26-2022
                                {
                                    record.ShipmentNumber = oNextASN.ToString();
                                    incrementNextASN(selectedRecord.Delivery.CardCode, oNextASN, oConnectionName);
                                }
                                else
                                {
                                    // 05-25-2021 end
                                    record.ShipmentNumber = selectedRecord.Delivery.DocEntry.ToString(); // 10-31-2019
                                } // 05-25-2021
                                // 01-21-2018 begin
                                if (selectedRecord.Delivery.U_InfoW2CnNo < 0)
                                {
                                    record.ConfirmationNo = 0;
                                }
                                else
                                {  // 01-21-2018 end
                                    record.ConfirmationNo = selectedRecord.Delivery.U_InfoW2CnNo;
                                }
                                record.OrderBuyCode = selectedRecord.Delivery.U_InfoW2BCode;
                                record.OrderBuyName = selectedRecord.Delivery.U_InfoW2BName;
                                record.OrderBuyAddr1 = selectedRecord.Delivery.U_InfoW2BAd1;
                                record.OrderBuyAddr2 = selectedRecord.Delivery.U_InfoW2BAd2;
                                record.OrderBuyCity = selectedRecord.Delivery.U_InfoW2BCity;
                                record.OrderBuyState = selectedRecord.Delivery.U_InfoW2BState;
                                record.OrderBuyZip = selectedRecord.Delivery.U_InfoW2BZip;
                                record.OrderBuyCountryCd = selectedRecord.Delivery.U_InfoW2BCntry;
                                record.JobNumber = selectedRecord.Delivery.U_InfoW2Job;
                                // 07-19-2017 end

                                // 07-31-2019 begin 
                                if (String.IsNullOrEmpty(selectedRecord.Delivery.U_Info_Pro))
                                {
                                    //09-10-2019 begin
                                    if ((selectedRecord.Edi850HeaderRecord.CardCode.ToUpper().Trim() == "WAYFAIR" ||
                                        selectedRecord.Edi850HeaderRecord.CardCode.ToUpper().StartsWith("WAYFAIR"))
                                        && String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_Pro))
                                    {
                                        // 10-05-2019 begin
                                        if (selectedRecord.Delivery.U_Info_BOL.Trim().Length > 48)
                                        {
                                            char[] cList = new char[2];
                                            cList[0] = ',';
                                            cList[1] = ' ';
                                            int oLastPos = selectedRecord.Delivery.U_Info_BOL.LastIndexOfAny(cList, 0);
                                            // 11-05-201 begin
                                            string pBOL = "";
                                            if (selectedRecord.Delivery.U_Info_BOL.Trim().Length < 48)
                                            {
                                                if (oLastPos > 0)
                                                {
                                                    pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, oLastPos);
                                                }
                                                else
                                                {
                                                    pBOL = selectedRecord.Delivery.U_Info_BOL.Trim();
                                                }
                                            }
                                            else
                                            {
                                                //record.ProNumber = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                                // string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                                pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                                // 11-05-2019 end
                                                string lastChar = pBOL.Substring(47);
                                                oLastPos = pBOL.LastIndexOfAny(cList, 0);
                                                if (oLastPos > 0 && lastChar != " ")
                                                {
                                                    record.ProNumber = pBOL.Substring(0, oLastPos - 1);
                                                }
                                                else
                                                {
                                                    record.ProNumber = pBOL;
                                                }
                                            }
                                        }
                                        else
                                        { // 10-05-2019 end
                                            record.ProNumber = selectedRecord.Delivery.U_Info_BOL;
                                        } // 10-05-2019
                                    }
                                    else
                                    {
                                        // 09-10-2019 end
                                        record.ProNumber = String.Empty;
                                    } // 09-10-2019
                                }
                                else
                                {
                                    record.ProNumber = record.BillOfLading;
                                }
                                // 07-31-2019 end
                                // 05-25-2021 begin
                                if (oIs3PL == "Y")
                                {
                                    if (String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_C3_MstrBOL))
                                    {
                                        record.MasterBOL = "";
                                    }
                                    else
                                    {
                                        record.MasterBOL = selectedRecord.Delivery.U_C3_MstrBOL;
                                    }
                                    // 07-19-2021 begin
                                    if (selectedRecord.Edi850HeaderRecord.CarrierCode.StartsWith("TeeZed") && selectedRecord.Edi850HeaderRecord.CarrierCode == "LBLS")
                                    {
                                        record.BillOfLading = "LABELS SENT";
                                    }
                                    // 07-19-2021 end
                                }
                                // 05-25-2021 end
                                // 11-06-2019 begin
                                if (String.IsNullOrWhiteSpace(record.ProNumber) &&
                                    (selectedRecord.Edi850HeaderRecord.CardCode.ToUpper().Trim() == "WAYFAIR" ||
                                        selectedRecord.Edi850HeaderRecord.CardCode.ToUpper().StartsWith("WAYFAIR"))
                                    )
                                {
                                    record.ProNumber = record.BillOfLading;
                                }
                                // 11-06-2019 end
                                record.AsnShipDate = selectedRecord.Delivery.DocDueDate;
                                //record.BillOfLading = String.Empty;
                                record.TransportationMethod = String.Empty;
                                // 03-03-2019 begin
                                try
                                {
                                    if (!String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.TransportMethod))
                                    {
                                        record.TransportationMethod = selectedRecord.Edi850HeaderRecord.TransportMethod;
                                    }
                                }
                                catch
                                {
                                }
                                // 03-03-2019 end
                                // 07-19-2019 begin
                                record.TransportRouting = String.Empty;
                                try
                                {
                                    if (!String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.TransportRouting))
                                    {
                                        record.TransportRouting = selectedRecord.Edi850HeaderRecord.TransportRouting;
                                    }
                                }
                                catch
                                {

                                }
                                // 07-19-2019 end
                                // 01-18-2022 begin
                                if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSC"))
                                {
                                    if (record.TransportationMethod == "M")
                                    {
                                        record.TransportationMethod = "LT";
                                    }
                                    String sql = "select top 1 isnull(s0.TrnspName, '') as TransportName from dbo.OSHP s0 WITH(NOLOCK) " +
                                                 "where s0.TrnspCode = " + selectedRecord.Delivery.TrnspCode;
                                    try
                                    {
                                        using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                        {
                                            sqlConnection.Open();

                                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                                            {
                                                using (SqlDataReader reader = command.ExecuteReader())
                                                {
                                                    reader.Read();
                                                    record.TransportRouting = (String)reader[0];
                                                }
                                            }
                                            sqlConnection.Close();
                                        }
                                    }
                                    catch (Exception tr)
                                    {
                                        _logger.Error("Error getting transport routing =>" + tr.Message);
                                    }
                                }
                                // 01-18-2022 end
                                // 05-25-2021 begin
                                if (oIs3PL == "Y")
                                {
                                    try
                                    {
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_C3_FreightCst.ToString())) // 06-07-2021
                                        {
                                            string oFreightCst = selectedRecord.Delivery.U_C3_FreightCst.ToString();
                                            record.FreightCost = Convert.ToDecimal(oFreightCst);
                                        } // 06-07-2021  begin
                                        else
                                        {
                                            record.FreightCost = 0;
                                        } // 06-07-2021 end
                                    }
                                    catch
                                    {
                                        record.FreightCost = Convert.ToDecimal("0.00");
                                    }

                                }
                                else
                                {
                                    record.FreightCost = Convert.ToDecimal("0.00");
                                }
                                // 05-25-2021 end

                                // 08-21-2019 begin
                                char[] charList = new char[2];
                                charList[0] = ',';
                                charList[1] = ' ';

                                if ((selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL")
                                    || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("WAYFAIR")
                                    || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HAYCL"))
                                    && !String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL)
                                    && selectedRecord.Delivery.U_Info_BOL.Trim().Length > 30)
                                {
                                    string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 30);
                                    int oLastPos = pBOL.LastIndexOfAny(charList);
                                    string lastChar = pBOL.Substring(29);
                                    if (oLastPos > 0 && lastChar != " ")
                                    {
                                        record.BillOfLading = pBOL.Substring(0, oLastPos - 1);
                                    }
                                    else
                                    {
                                        record.BillOfLading = pBOL;
                                    }
                                    // 09-15-2021 begin
                                    if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                    {
                                        record.BillOfLading = record.BillOfLading.Replace("-", "");
                                    }
                                    // 09-15-2021 end
                                    selectedRecord.Delivery.U_Info_BOL = record.BillOfLading; // testing
                                }
                                else if ((selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("LowesNet")
                                || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSCCL"))
                                     && !String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL)
                                     && selectedRecord.Delivery.U_Info_BOL.Trim().Length > 48)
                                {
                                    //record.BillOfLading = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                    string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                    int oLastPos = pBOL.LastIndexOfAny(charList);
                                    string lastChar = pBOL.Substring(47);
                                    if (oLastPos > 0 && lastChar != " ")
                                    {
                                        record.BillOfLading = pBOL.Substring(0, oLastPos - 1);
                                    }
                                    else
                                    {
                                        record.BillOfLading = pBOL;
                                    }
                                    // 09-15-2021 begin
                                    if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                    {
                                        record.BillOfLading = record.BillOfLading.Replace("-", "");
                                    }
                                    // 09-15-2021 end

                                    selectedRecord.Delivery.U_Info_BOL = record.BillOfLading; // testing
                                }
                                else
                                {
                                    if (selectedRecord.Delivery.U_Info_BOL.Trim().Length > 48)
                                    {
                                        /// record.BillOfLading = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                        string pBOL = selectedRecord.Delivery.U_Info_BOL.Substring(0, 48);
                                        int oLastPos = pBOL.LastIndexOfAny(charList);
                                        string lastChar = pBOL.Substring(47);
                                        if (oLastPos > 0 && lastChar != " ")
                                        {
                                            record.BillOfLading = pBOL.Substring(0, oLastPos - 1);
                                        }
                                        else
                                        {
                                            record.BillOfLading = pBOL;
                                        }
                                        // 09-15-2021 begin
                                        if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            record.BillOfLading = record.BillOfLading.Replace("-", "");
                                        }
                                        // 09-15-2021 end

                                    }
                                    else
                                    {
                                        // 08-21-2019 end
                                        record.BillOfLading = selectedRecord.Delivery.U_Info_BOL; // 04-19-2018
                                        // 09-15-2021 begin
                                        if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            record.BillOfLading = record.BillOfLading.Replace("-", "");
                                        }
                                        // 09-15-2021 end
                                    }
                                    selectedRecord.Delivery.U_Info_BOL = record.BillOfLading; // testing
                                } // 08-21-2019
                                //foreach(Edi850DetailRecord detail850Record in selectedRecord.Edi850HeaderRecord.Details)
                                //{
                                //    Edi856PalletDetailRecord detail856Record = new Edi856PalletDetailRecord();
                                //    record.Details.Add(detail856Record);

                                //    detail856Record.BuyerItemCode = detail850Record.BuyerItemCode;
                                //    detail856Record.ItemDescription = detail850Record.ItemDescription;
                                //    detail856Record.LineNumber = detail850Record.LineNumber;
                                //    detail856Record.Quantity = detail850Record.Quantity;
                                //    detail856Record.UnitOfMeasure = detail850Record.UnitOfMeasure;
                                //    detail856Record.UnitPrice = detail850Record.UnitPrice;
                                //    detail856Record.VendorItemCode = detail850Record.VendorItemCode;

                                //    var foundDeliveryLine = (from v in selectedRecord.Delivery.DeliveryLines
                                //                             where v.LineNumber850 == detail850Record.LineNumber
                                //                             select v).FirstOrDefault();
                                //    if(foundDeliveryLine != null)
                                //    {
                                //        detail856Record.QuantityShipped = Convert.ToDouble(foundDeliveryLine.Quantity);
                                //    }
                                //    else
                                //    {
                                //        detail856Record.QuantityShipped = 0;
                                //    }

                                //    detail856Record.NumberOfBags = 0;
                                //    detail856Record.NumberOfCartons = 0;
                                //    detail856Record.NumberOfPallets = 0;
                                //    detail856Record.PalletSerialNumber = String.Empty;
                                //}
                                // 02-09-2021 begin
                                record.DeliveryNumber = selectedRecord.Delivery.DocNum; // 02-09-2020
                                if (send856Pack == true)
                                {
                                    record.Structure = "0001";
                                }
                                else
                                {
                                    record.Structure = "0055";
                                }
                                // 02-09-2021 end
                                // 05-23-2021 begin
                                /*  if (oIs3PL == "Y")
                                  {
                                      try
                                      {
                                          if (selectedRecord.Delivery.TrnspCode > 0)
                                          {
                                              String shipmentSql = String.Format(ShipTypeQuery, selectedRecord.Delivery.TrnspCode);
                                              //_logger.Debug("Executing SQL: " + shipmentSql);
                                              using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                              {
                                                  sqlConnection.Open();
                                                  using (SqlCommand command = new SqlCommand(shipmentSql, sqlConnection))
                                                  {
                                                      using (SqlDataReader reader = command.ExecuteReader())
                                                      {
                                                          if (!reader.Read())
                                                          {
                                                              throw new WebApiException("Could not locate OSHP with TrnspCode " + selectedRecord.Delivery.TrnspCode);
                                                          }
                                                          String oShipType = (String)reader[0];
                                                          if (!(oShipType == "M"))
                                                          {
                                                              record.BillOfLading = "";
                                                              record.ProNumber = "";
                                                          }
                                                      }
                                                  }
                                              }
                                          }
                                      }
                                      catch (Exception styp)
                                      {

                                      }
                                  } */
                                //05-26-2021???
                                try
                                {
                                    string oSQry = "select p0.NoPKgs, p0.ShipWgt from dbo.[Infocus_Delivery_PkgTotals]  p0 WITH(NOLOCK) where p0.DocEntry =  " + selectedRecord.Delivery.DocEntry;
                                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                                    {
                                        sqlConnection.Open();

                                        using (SqlCommand command = new SqlCommand(oSQry, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (reader.Read())
                                                {
                                                    try
                                                    {
                                                        String oValue = (String)reader["NoPkgs"].ToString();
                                                        try
                                                        {
                                                            decimal oNoPkgs = Convert.ToDecimal(oValue);
                                                            record.ShipmentCartons = oNoPkgs;
                                                        }
                                                        catch (Exception pkg)
                                                        {
                                                            String oErr = pkg.Message;
                                                            _logger.Error("Error getting number of packages for Delivery " + selectedRecord.Delivery.DocNum + " =>" + oErr);
                                                        }
                                                        oValue = (String)reader["ShipWgt"].ToString();
                                                        try
                                                        {
                                                            decimal oShipWgt = Convert.ToDecimal(oValue);
                                                            record.ShipmentWeight = oShipWgt;
                                                        }
                                                        catch (Exception pkg)
                                                        {
                                                            String oErr = pkg.Message;
                                                            _logger.Error("Error getting number of shipment weight for Delivery " + selectedRecord.Delivery.DocNum + " =>" + oErr);
                                                        }
                                                    }
                                                    catch (Exception e)
                                                    {
                                                        string ErrMesg = e.Message;
                                                        _logger.Error("Error executing [Infocus_Delivery_PkgTotals] =>" + ErrMesg);
                                                    }
                                                }
                                            }
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                                catch (Exception stot)
                                {
                                    _logger.Error(stot.Message);
                                }
                                // 05-21-2021 end
                                // 05-26-2021 begin
                                String[] oTrackNos = new String[1];
                                if (oIs3PL == "Y")
                                {
                                    try
                                    {
                                        string oW2Notes = "";
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2Notes))
                                        {
                                            oW2Notes = selectedRecord.Delivery.U_InfoW2Notes.ToString();
                                            if (!String.IsNullOrWhiteSpace(oW2Notes))
                                            {
                                                oTrackNos = oW2Notes.Split(',');
                                                if (oTrackNos.Length == 0)
                                                {
                                                    oTrackNos = new String[1];
                                                    oTrackNos[0] = oW2Notes.Trim();
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception TrkNoErr)
                                    {
                                        string ErrMesg = TrkNoErr.Message;
                                        _logger.Error("Error executing getting tracking numbers for U_InfoW2Notes => " + ErrMesg);
                                        oTrackNos = new String[1];
                                        oTrackNos[0] = selectedRecord.Delivery.TrackNo;
                                    }
                                }
                                // 05-26-2021 end

                                // 06-03-2021 begin
                                if (oIs3PL == "Y" && selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TeeZed"))
                                {
                                    oTrackNos[0] = "";
                                }
                                // 06-03-2021 end

                                _logger.Debug("Process lines for " + selectedRecord.Edi850HeaderRecord.SBOCardCode); // 05-21-2021

                                if (iPostProcess856Record != null || (bSendPack == true && iPostProcess856Record != null)) // 09-02-2021 added check for SendPack
                                {
                                    // 01-27-2023 remove log message
                                    /*
                                       _logger.Debug("Processing lines for Del# " + selectedRecord.Delivery.DocNum + " PO# " + selectedRecord.Delivery.NumAtCard); // 08-31-2022
                                    */
                                    // 08-31-2021 begin
                                    if (bSendPack == true)
                                    {
                                        iPostProcess856Record.OnPostProcess856Record(selectedRecord.Delivery, selectedRecord.Edi850HeaderRecord, record, oIs3PL);
                                    }
                                    else
                                    {
                                        // 08-31-2021 end
                                        iPostProcess856Record.OnPostProcess856Record(selectedRecord.Delivery, selectedRecord.Edi850HeaderRecord, record, oIs3PL, oTrackNos);
                                    } // 08-31-2021
                                }
                                // 02-25-2022 begin
                                /*
                                // 02-22-2022 begin
                                DateTime oTrxUpdt = DateTime.Now;
                                record.TrxDateTime = oTrxUpdt.ToString("MM-dd-yyyy HH:mm:ss.mmm");
                                selectedRecord.Edi850HeaderRecord.Processed856DateTime = oTrxUpdt;
                                // 02-22-2022 end
                                 */
                                //DateTime oProcessDT = DateTime.Now;
                                if (selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime.ToString().Trim().Length == 0)
                                {
                                    selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime = DateTime.Now;
                                }
                                //oProcessDT = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime);
                                //record.TrxDateTime = oProcessDT.ToString("MM-dd-yyyy HH:MM:ss.mmm");
                                record.TrxDateTime = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime); // 03-10-2022
                                // 02-25-2022 end

                                selectedRecord.Edi850HeaderRecord.Processed856 = true; // 05-20-2020
                                selectedRecord.Edi850HeaderRecord.HasOpen856 = false; // 07-09-2020
                                selectedRecord.Delivery.U_InfoW2856 = "Y"; // 07-09-2020
                                // 01-24-2023 begin
                                selectedRecord.Edi850HeaderRecord.Processed856DateTime = DateTime.Now;
                                if (selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime == null
                                    || selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime.ToString().Trim().Length == 0)
                                {
                                    selectedRecord.Edi850HeaderRecord.Orig856ProcessedDateTime = selectedRecord.Edi850HeaderRecord.Processed856DateTime;
                                }
                                // 01-24-2023 end
                                // 12-19-2023 begin
                                int oDelNo = selectedRecord.Delivery.DocEntry;
                                SAPbobsCOM.Company _Company = getCompany();
                                bool bConnected = _Company.Connected;
                                if (bConnected)
                                {
                                    try
                                    {
                                        SAPbobsCOM.Documents oDel = _Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes) as SAPbobsCOM.Documents;
                                        bool bFound = oDel.GetByKey(oDelNo);
                                        if (bFound)
                                        {
                                            String oSent850 = oDel.UserFields.Fields.Item("U_Info850HdrId").Value.ToString();
                                            if (!String.IsNullOrWhiteSpace(oSent850)
                                                && oSent850.Trim().Length > 0)
                                            {
                                                oDel.UserFields.Fields.Item("U_Info850HdrId").Value = "Y";
                                                try
                                                {
                                                    int oRet = oDel.Update();
                                                    if (oRet != 0)
                                                    {
                                                        String oErrMsg = _Company.GetLastErrorDescription();
                                                        int oErrCode = _Company.GetLastErrorCode();
                                                        oErrMsg = oErrMsg + ", ErrorCode " + oErrCode.ToString();
                                                        _logger.Error("Error setting 856 Sent flag for Delivery #" + selectedRecord.Delivery.DocNum.ToString() + " => " + oErrMsg);
                                                    }
                                                }
                                                catch (Exception d)
                                                {
                                                    _logger.Error("Error updating delivery 'Sent 856': " + d.Message);
                                                }
                                            }
                                        }
                                    }
                                    catch (Exception d0)
                                    {
                                        //_logger.Error("Error updating delivery Sent 856 using HeaderId for Delivery " + oDelNo + ": " + d0.Message);
                                    }
                                }
                                // 12-19-2023 end
                            } // 04-19-2018 
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            _logger.Error(ex.Message);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                        }

                    }
                } // 08-15-2023
                DateTime now = DateTime.Now;
                DateTime oTrxDT = now; // 02-22-2022
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())
                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    // 09-08-2023 begin
                    if (b940 == true)
                    {
                        foreach (var selectedRecord in list940ToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi940HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi940HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            try
                            {
                                if (selectedRecord.IsError)
                                {
                                    recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                    recordToUpdate.Processed856 = false;
                                    recordToUpdate.HasOpen856 = true;
                                    selectedRecord.Delivery.U_InfoW2856 = "N";
                                    _logger.Error("940 Record Key " + selectedRecord.Edi940HeaderRecord.HeaderId + " has error -- set 856 to unprocessed");
                                }
                                else if (!(recordToUpdate == null) &&
                                    (!(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2Cc) &&
                                    !(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL))))
                                    || (selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("TeeZed") && selectedRecord.Edi940HeaderRecord.CarrierCode == "LBLS"))
                                {
                                    recordToUpdate.Processed856 = true;
                                    selectedRecord.Delivery.U_InfoW2856 = "Y";
                                    recordToUpdate.Processed856DateTime = now;
                                    if (recordToUpdate.Orig856ProcessedDateTime == null || recordToUpdate.Orig856ProcessedDateTime.ToString().Trim().Length == 0)
                                    {
                                        recordToUpdate.Orig856ProcessedDateTime = now;
                                    }
                                    recordToUpdate.HasOpen856 = false;
                                    if (selectedRecord.Edi940HeaderRecord.CardCode == "LowesNet" ||
                                       selectedRecord.Edi940HeaderRecord.CardCode.StartsWith("LOWES") ||
                                       selectedRecord.Edi940HeaderRecord.CardCode == "LOWES")
                                    {
                                        recordToUpdate.ErrorMessage = String.Empty;
                                    }
                                }
                                else
                                {
                                    if (!(recordToUpdate == null))
                                    {
                                        recordToUpdate.Processed856 = false;
                                        recordToUpdate.ErrorMessage = String.Empty;
                                    }
                                    selectedRecord.Delivery.U_InfoW2856 = "N";
                                }
                                // force delivery flag to Y if Processed856 is true
                                if (recordToUpdate.Processed856 == true)
                                {
                                    selectedRecord.Delivery.U_InfoW2856 = "Y";
                                }
                                int dbResult1 = dbContext.SaveChanges();
                                _logger.Debug("Result of 856 SaveChanges:" + dbResult1.ToString()); // 12-18-2023
                            }
                            catch (Exception er)
                            {
                                _logger.Error("Error updating 856 Processed ", er);
                            }
                        }
                    }
                    else
                    {
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  // && (v.Processed856 == false
                                                  // || (v.Processed856DateTime == null || v.Processed856DateTime < oTrxDT)) // 01-27-2023
                                                  select v).FirstOrDefault();
                            try
                            {
                                // 03-15-2022 begin
                                /*
                                if (recordToUpdate == null)
                                {
                                    throw new WebApiException("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                                }*/
                                // 04-07-2022 begin
                                /*
                                if (recordToUpdate == null)
                                {
                                    _logger.Error("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                                }
                                else
                                {
                                 */
                                // 04-07-2022 end
                                // 03-15-2022 end
                                if ((selectedRecord.IsError)
                                    // 03-23-2026 lrussell begin
                                    && !(selectedRecord.ErrorMessage.Contains("Object reference not set to an instance of an object"))
                                    && selectedRecord.Delivery.DocEntry > 0 && !String.IsNullOrWhiteSpace(selectedRecord.Delivery.Canceled)
                                    && !(selectedRecord.Delivery.Canceled == "Y")
                                    )
                                // 03-23-2026 lrussell end
                                {
                                    recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                    recordToUpdate.Processed856 = false;
                                    recordToUpdate.HasOpen856 = true; // 08-06-2019
                                    selectedRecord.Delivery.U_InfoW2856 = "N"; // 10-28-2020
                                    _logger.Error("850 Record Key " + selectedRecord.Edi850HeaderRecord.HeaderId + " has error -- set 856 to unprocessed"); // 07-09-2020
                                }
                                else if (!(recordToUpdate == null) && // 04-08-2022
                                    (!(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_InfoW2Cc) && // 04-19-2018
                                    !(String.IsNullOrWhiteSpace(selectedRecord.Delivery.U_Info_BOL)))) // 04-19-2018
                                    || (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TeeZed") && selectedRecord.Edi850HeaderRecord.CarrierCode == "LBLS")) // 07-19-2021
                                {
                                    //_logger.Debug("line 2437 in updated 856 processed");
                                    recordToUpdate.Processed856 = true;
                                    //  _logger.Debug("updated Processed856");
                                    // 02-25-2022 begin
                                    /*
                                    // 02-22-2022 begin
                                    try
                                    {
                                        oTrxDT = Convert.ToDateTime(recordToUpdate.Processed856DateTime.ToString());
                                    }
                                    catch (Exception d)
                                    {
                                        oTrxDT = now;
                                    }
                                    //recordToUpdate.Processed856DateTime = now;
                                    recordToUpdate.Processed856DateTime = oTrxDT;
                                    // 02-22-2022 end
                                     */
                                    recordToUpdate.Processed856DateTime = now;
                                    //_logger.Debug("Updated Processed856DateTime ");
                                    //_logger.Debug("line 2455 in updated 856 processed");
                                    if (recordToUpdate.Orig856ProcessedDateTime == null || recordToUpdate.Orig856ProcessedDateTime.ToString().Trim().Length == 0)
                                    {
                                        recordToUpdate.Orig856ProcessedDateTime = now;
                                        // _logger.Debug("Updated Orig856ProcessedDateTime ");

                                    }
                                    // 02-25-2022 end
                                    // _logger.Debug("line 2461 in updated 856 processed");
                                    recordToUpdate.Processed870 = false; // 05-31-2017
                                    recordToUpdate.Processed860 = false; // 02-10-2019
                                    recordToUpdate.HasOpen856 = false; // 08-06-2019
                                    //05-01-2018 begin
                                    if (selectedRecord.Edi850HeaderRecord.CardCode == "LowesNet" ||
                                        selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("LOWES") || // 08-30-2019
                                        selectedRecord.Edi850HeaderRecord.CardCode == "LOWES")
                                    {
                                        recordToUpdate.ErrorMessage = String.Empty;
                                    }
                                    selectedRecord.Delivery.U_InfoW2856 = "Y"; // 10-28-2020
                                }// 07-09-2020 
                                else
                                {
                                    //05-01-2018 end
                                    //_logger.Debug("line 2477 in updated 856 processed");
                                    if (!(recordToUpdate == null)) // 04-24-2022
                                    {
                                        recordToUpdate.Processed856 = false; // 01-17-2018
                                        recordToUpdate.ErrorMessage = String.Empty;
                                    } // 04-24-2022
                                    selectedRecord.Delivery.U_InfoW2856 = "N"; // 10-28-2020
                                } //05-01-2018
                                //selectedRecord.Delivery.U_InfoW2856 = "Y"; // 08-06-2019
                                // 12-18-2023 begin
                                // force delivery flag to Y if Processed856 is true
                                if (recordToUpdate.Processed856 == true)
                                {
                                    selectedRecord.Delivery.U_InfoW2856 = "Y";
                                }
                                // 12-18-2023 en
                                int dbResult1 = dbContext.SaveChanges(); // 05-20-2020                     

                                //_logger.Debug("Save Changes for 856 Delivery# " + selectedRecord.Delivery.DocNum.ToString() + " returned status = " + dbResult1.ToString()); // 10-28-2020
                                //} // 07-09-2020 begin
                                //  } // 03-15-2022

                            }
                            catch (Exception er)
                            {
                                _logger.Error("Error updating 856 Processed ", er);
                            }
                        }
                    } // 09-08-2023
                    int dbResult = dbContext.SaveChanges();
                    _logger.Debug("Save 856 Changes returned status = " + dbResult.ToString());
                }
                response.Successful = true;
                // 05-31-2017 begin
                // set sales order 'order status' = "CC" 
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                // remove debugging info
                /*  if (response.Edi856Records.Count > 0)
                  {
                      _logger.Debug("Updating 856 status to processed");
                  } */
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    _logger.Debug("ConnectionName = " + oConnectionName); // 08-20-2022
                    sqlConnection.Open();
                    // 01-17-2018
                    //    using (WebApiDbContext dbContext = new WebApiDbContext())
                    // 09-12-2023 begin
                    if (b940 == true)
                    {
                        using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                        {
                            int iNext = 0;
                            SAPbobsCOM.Company _Company = getCompany();
                            SAPbobsCOM.Documents oDelivery = _Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes) as SAPbobsCOM.Documents;
                            foreach (var selectedRecord in list940ToProcess)
                            {
                                var recordToUpdate = (from v in dbContext.Edi940HeaderRecords
                                                      where v.HeaderId == selectedRecord.Edi940HeaderRecord.HeaderId
                                                      select v).FirstOrDefault();
                                iNext = iNext + 1;
                                if (recordToUpdate != null)
                                {

                                    if (_Company.Connected)
                                    {
                                        try
                                        {
                                            bool bFound = oDelivery.GetByKey(selectedRecord.Delivery.DocEntry);
                                            if (bFound)
                                            {
                                                //if (oDelivery.UserFields.Fields.Item("U_InfoW2856").Value != "Y")
                                                //{
                                                oDelivery.UserFields.Fields.Item("U_InfoW2856").Value = "Y";
                                                int oRet = oDelivery.Update();
                                                if (oRet != 0)
                                                {
                                                    String oErrMsg = _Company.GetLastErrorDescription();
                                                    int oErrCode = _Company.GetLastErrorCode();
                                                    oErrMsg = oErrMsg + ", ErrorCode " + oErrCode.ToString();
                                                    _logger.Error("Error setting 856 Sent flag for Delivery #" + selectedRecord.Delivery.DocNum.ToString() + " => " + oErrMsg);
                                                }
                                                //}
                                            }
                                        }
                                        catch (Exception d)
                                        {
                                            String oErr = d.Message;
                                            _logger.Error("Error setting 856 Sent flag for Delivery #" + selectedRecord.Delivery.DocNum.ToString() + " =>" + oErr);
                                        }

                                    }
                                }
                            }
                            int dbResult = dbContext.SaveChanges();
                        }
                    }
                    else
                    {
                        // 09-12-2023 end
                        using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                        // 01-17-2018 end
                        {
                            // 01-27-2023 begin
                            int iNext = 0;
                            SAPbobsCOM.Company _Company = getCompany();
                            SAPbobsCOM.Documents oDelivery = _Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes) as SAPbobsCOM.Documents;
                            // 01-27-2023 end

                            foreach (var selectedRecord in listToProcess)
                            {
                                var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                                      where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                      select v).FirstOrDefault();
                                iNext = iNext + 1;
                                //_logger.Debug("Updating record #" + iNext);
                                if (recordToUpdate != null)
                                {
                                    // 01-27-2023 begin
                                    // remove set CC
                                    /*
                                     // 05-01-2018 begin
                                     bool bSetCC = true;
                                     if (selectedRecord.Edi850HeaderRecord.CardCode == "LowesNet" ||
                                        selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("LOWES") || // 08-30-2019
                                        selectedRecord.Edi850HeaderRecord.CardCode == "LOWES")
                                     {
                                         bSetCC = false;
                                     }
                                     if (bSetCC)
                                     {
                                         // 05-01-2018 end
                                         // 05-15-2020 begin
                                         /* if (selectedRecord.IsError == false)
                                          {
                                              string oQry = "";
                                              if (selectedRecord.Edi850HeaderRecord.SBOCardCode == null || selectedRecord.Edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                                              {  // 01-17-2018 
                                                  // 08-07-2019 begin
                                                  /* using (SqlCommand command = new SqlCommand("UPDATE ORDR set U_InfoOrdStatus = 'CC' where Canceled = 'N' and NumAtCard = '" + recordToUpdate.PurchaseOrderReference + "' and CardCode = '" + recordToUpdate.CardCode + "' and DocEntry = " + recordToUpdate.SalesOrderKey, sqlConnection))
                                                   {
                                                       command.ExecuteNonQuery();
                                                   }*/
                                    /* 05-15-2020
                                            oQry = "execute Infocus_EDI_Update_OrdStatus '" + recordToUpdate.PurchaseOrderReference + "', '" + recordToUpdate.CardCode + "', " + recordToUpdate.SalesOrderKey;
                                            // 08-07-2019 end
                                            // 01-17-2018 begin
                                        }
                                        else
                                        {
                                            // 08-07-2019 begin
                                            /*using (SqlCommand command = new SqlCommand("UPDATE ORDR set U_InfoOrdStatus = 'CC' where Canceled = 'N' and NumAtCard = '" + recordToUpdate.PurchaseOrderReference + "' and CardCode = '" + recordToUpdate.SBOCardCode + "' and DocEntry = " + recordToUpdate.SalesOrderKey, sqlConnection))
                                            {
                                                command.ExecuteNonQuery();
                                            }*/
                                    /* 05-15-2020
                                            oQry = "execute Infocus_EDI_Update_OrdStatus '" + recordToUpdate.PurchaseOrderReference + "', '" + recordToUpdate.SBOCardCode + "', " + recordToUpdate.SalesOrderKey;
                                            // 08-07-2019 end

                                        }
                                        // 01-17-2018 end
                                        // 08-07-2019 begin
                                        if (!String.IsNullOrWhiteSpace(oQry))
                                        {
                                            using (SqlCommand command = new SqlCommand(oQry, sqlConnection))
                                            {
                                                command.ExecuteNonQuery();
                                            }
                                        }
                                        // 08-07-2019 end
                                    }
                            */
                                    //} // 05-01-2018
                                    // 01-27-2023 end
                                    // 08-02-2022 begin
                                    /*
                                    // 08-11-2019 begin
                                    // set U_InfoW2856 = 'Y'                               
                                    try
                                    {
                                        string oQry = "";

                                        --stored procedure call --

                                        oQry = "execute [Infocus_EDI_Update_ODLN_856] " + selectedRecord.Delivery.DocEntry;
                                        if (!String.IsNullOrWhiteSpace(oQry))
                                        {
                                            using (SqlCommand command = new SqlCommand(oQry, sqlConnection))
                                            {
                                                command.ExecuteNonQuery();
                                            }
                                        }

                                    }
                                    catch (Exception r2)
                                    {
                                        _logger.Error("Error setting flag for Delivery # " + selectedRecord.Delivery.DocNum + " =>" + r2.Message);
                                    }
                                    // 08-11-2019 end
                                     */
                                    // 01-25-2023 move company 
                                    //SAPbobsCOM.Company _Company = getCompany();
                                    if (_Company.Connected)
                                    {
                                        // 1-25-2023 move oDelivery definition
                                        //SAPbobsCOM.Documents oDelivery = _Company.GetBusinessObject(SAPbobsCOM.BoObjectTypes.oDeliveryNotes) as SAPbobsCOM.Documents;
                                        try
                                        {
                                            bool bFound = oDelivery.GetByKey(selectedRecord.Delivery.DocEntry);
                                            if (bFound)
                                            {
                                                if (oDelivery.UserFields.Fields.Item("U_InfoW2856").Value != "Y")  // 01-27-2023
                                                {
                                                    oDelivery.UserFields.Fields.Item("U_InfoW2856").Value = "Y";
                                                    int oRet = oDelivery.Update();
                                                    if (oRet != 0)
                                                    {
                                                        String oErrMsg = _Company.GetLastErrorDescription();
                                                        int oErrCode = _Company.GetLastErrorCode();
                                                        oErrMsg = oErrMsg + ", ErrorCode " + oErrCode.ToString();
                                                        _logger.Error("Error setting 856 Sent flag for Delivery #" + selectedRecord.Delivery.DocNum.ToString() + " => " + oErrMsg);
                                                    }
                                                } // 01-27-2023
                                            }
                                        }
                                        catch (Exception d)
                                        {
                                            String oErr = d.Message;
                                            _logger.Error("Error setting 856 Sent flag for Delivery #" + selectedRecord.Delivery.DocNum.ToString() + " =>" + oErr);
                                        }

                                    }
                                    // 08-02-2022 end
                                }
                            }
                            int dbResult = dbContext.SaveChanges();
                        }
                    } // 09-12-2023

                    sqlConnection.Close();
                }
                // 05-31-2017 end
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                _logger.Error(ex.Message);
                if (ex.InnerException != null && !String.IsNullOrWhiteSpace(ex.InnerException.Message))
                {
                    _logger.Error("Inner Exception follows");
                    _logger.Error(ex.InnerException);
                    _logger.Error(ex.InnerException.Message); // 07-01-2019
                }
                response.ErrorMessage = ex.Message;
            }
            // 08-24-2022 begin
            if (response.Edi856Records.Count <= 0)
            {
                _logger.Debug("No 856s found to return");
            }
            else
            {
                // 08-24-202 end
                _logger.Debug("Returning the 856 response object which contains " + response.Edi856Records.Count + " ASNs");
            } // 08-24-2022
            //_logger.Debug("{@Get856RecordsResponse}", response);
            _logger.Debug("Leaving Get856Records for " + oSBOCardCode);
            //string[] oOutput = new String[response.Edi856Records.Count];
            return response;
        }

        // 02-15-2022 begin
        //private IPreProcess856Record Get856PreProcess(String cardCode, DateTime trxDate)
        //private IPreProcess856Record Get856PreProcess(String cardCode, DateTime trxDate) // 02-15-2022 end
        private IPreProcess856Record Get856PreProcess(String cardCode, DateTime trxDate, DateTime cutoffDT) // 04-08-2022 end
        {
            String preProcess856Record = ConfigurationManager.AppSettings["PreProcess856Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess856Record))
            {
                preProcess856Record = ConfigurationManager.AppSettings["PreProcess856Record"];
            }
            // 02-15-2022 begin
            if (trxDate == null)
            {
                trxDate = DateTime.Now;
            }
            // 02-15-2022 end
            // 04-08-2022 begin
            if (cutoffDT == null)
            {
                cutoffDT = DateTime.Now;
            }
            // 04-08-2022 end
            if (!String.IsNullOrWhiteSpace(preProcess856Record))
            {
                try
                {
                    IPreProcess856Record iPreProcess856Record = (IPreProcess856Record)Activator.CreateInstance(Type.GetType(preProcess856Record, true));
                    return iPreProcess856Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess856Record", ex);
                    _logger.Error("Could not instantiate IPreProcess856Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess856Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private IPostProcess856Record Get856PostProcess(String cardCode)
        {
            String postProcess856Record = ConfigurationManager.AppSettings["PostProcess856Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess856Record))
            {
                postProcess856Record = ConfigurationManager.AppSettings["PostProcess856Record"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess856Record))
            {
                try
                {
                    IPostProcess856Record iPostProcess856Record = (IPostProcess856Record)Activator.CreateInstance(Type.GetType(postProcess856Record, true));
                    return iPostProcess856Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess856Record", ex);
                    _logger.Error("Could not instantiate IPostProcess856Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess856Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        /*
        // 09-02-2021 begin
        private IPostProcess856PRecord Get856PPostProcess(String cardCode)
        {
            String postProcess856PRecord = ConfigurationManager.AppSettings["PostProcess856PRecord-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess856PRecord))
            {
                postProcess856PRecord = ConfigurationManager.AppSettings["PostProcess856PRecord"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess856PRecord))
            {
                try
                {
                    System.Type oType = Type.GetType(postProcess856PRecord, true);
                    IPostProcess856PRecord iPostProcess856PRecord = (IPostProcess856PRecord)Activator.CreateInstance(Type.GetType(postProcess856PRecord, true));
                    return iPostProcess856PRecord;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess856PRecord", ex);
                    _logger.Debug("Could not instantiate IPostProcess856PRecord =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess856PRecord. Reason: " + ex.Message);
                }
            }
            return null;
        }

        private IPreProcess856PRecord Get856PPreProcess(String cardCode)
        {
            String preProcess856PRecord = ConfigurationManager.AppSettings["PreProcess856PRecord-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess856PRecord))
            {
                preProcess856PRecord = ConfigurationManager.AppSettings["PreProcess856Record"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess856PRecord))
            {
                try
                {
                    System.Type oType = Type.GetType(preProcess856PRecord, true);
                    
                    IPreProcess856PRecord iPreProcess856PRecord = (IPreProcess856PRecord)Activator.CreateInstance(Type.GetType(preProcess856PRecord, true));
                    return iPreProcess856PRecord;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess856PRecord", ex);
                    _logger.Debug("Could not instantiate IPreProcess856PRecord =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcessP856Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        // 09-02-2021 end
         * */
        // 08-06-2019 begin
        private Delivery GetDeliveryFromXref(WebApiDbContext context, Edi850HeaderRecord record, String connectionName, DateTime pLastTrxDT, DateTime pCutOffDT)
        {
            try
            {
                string oDelNo = null;
                string oQuery = "select top 1 IsNull(t0.DocEntry,0) DocEntry from dbo.[Infocus_EDI_856_ODLN] t0 WITH (NOLOCK) where (IsNull(t0.Sent856,'N') = 'N' or " +
                 " (IsNull(t0.Sent856,'N') = 'Y' and t0.Orig856ProcessedDateTime > '" + pLastTrxDT.ToString("MM-dd-yyyy HH:MM:ss.mmm") + //"' )) " + // 02-15-2022
               "' and t0.Orig856ProcessedDateTime < '" + pCutOffDT.ToString("MM-dd-yyyy HH:MM:ss.mmm") + "' )) " + // 04-08-2022
                 " and t0.HeaderId = " + record.HeaderId + " and t0.EDICardCode = '" + record.CardCode.Trim() + "'";
                try
                {
                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionName)))
                    {
                        sqlConnection.Open();
                        using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    try
                                    {
                                        oDelNo = (String)reader["DocEntry"].ToString();
                                    }
                                    catch (Exception e)
                                    {
                                        string ErrMesg = e.Message;
                                        oDelNo = "";
                                    }
                                }
                            }
                        }
                        sqlConnection.Close();
                    }
                }
                catch (Exception del)
                {
                    _logger.Error("Error getting delvery docentry from dbo.[Infocus_EDI_856_ODLN] for HeaderId = " + record.HeaderId + " => " + del.Message);
                }
                if (String.IsNullOrWhiteSpace(oDelNo))
                {
                    return null;
                }
                else
                {
                    int DelNo = -1;
                    try
                    {
                        DelNo = Convert.ToInt32(oDelNo);
                    }
                    catch
                    {
                        DelNo = -1;
                    }
                    if (DelNo == -1)
                    {
                        return null;
                    }
                    else
                    {
                        try
                        {
                            DeliveryLine deliverylines = (from v in context.DeliveryLines.Include("Delivery").Include("Delivery.DeliveryLines")
                                                          where v.DocEntry == DelNo
                                                          && v.Delivery.U_Info_BOL != null
                                                          && ((v.TreeType == "N") || (v.TreeType == "S"))
                                                          select v).FirstOrDefault();
                            if (deliverylines != null)
                            {
                                return deliverylines.Delivery;
                            }
                        }
                        catch (Exception r)
                        {
                            // 01-15-2022 begin
                            String oInnerEx = "";
                            try
                            {
                                oInnerEx = r.InnerException.Message;
                            }
                            catch
                            {
                                oInnerEx = "";
                            }
                            if (oInnerEx.Trim().Length > 0)
                            {
                                _logger.Error("Error getting delivery line (EdiController line 2580) =>" + r.Message + " Inner Exception: " + oInnerEx);
                            }
                            else
                            {
                                // 01-15-2022 end
                                _logger.Error("Error getting delivery line (EdiController line 2580) =>" + r.Message);
                            } // 01-15-2022
                            return null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                _logger.Error("Could not find Delivery", e);
                _logger.Error("Could not find Delivery =>" + oErrMesg);
                return null;
            }
            return null;
        }
        // 08-06-2019 end

        // 08-04-2022 begin
        private Invoice GetInvoiceFromXref(WebApiDbContext context, String connectionName, Edi850HeaderRecord record)
        {
            try
            {
                string oInvNo = null;
                string oQuery = "select top 1 IsNull(t0.DocEntry,0) DocEntry from dbo.[Infocus_EDI_810_OINV]  t0 WITH (NOLOCK) " +
                                        //"where HeaderId = " + record.HeaderId + " and EDICardCode = '" + record.CardCode.Trim() + "'";
                                        " where t0.EDICardCode = '" + record.CardCode.Trim() + "' and ( t0.HeaderId =  " + record.HeaderId +  // 10-03-2022
                                        " or t0.PurchaseOrderReference = '" + record.PurchaseOrderReference.Trim() + "')"; // 10-03-2022
                try
                {
                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionName)))
                    {
                        sqlConnection.Open();
                        using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    try
                                    {
                                        oInvNo = (String)reader["DocEntry"].ToString();
                                    }
                                    catch (Exception e)
                                    {
                                        string ErrMesg = e.Message;
                                        oInvNo = "";
                                    }
                                }
                            }
                        }
                        sqlConnection.Close();
                    }
                }
                catch (Exception inv)
                {
                    _logger.Error("Error getting delvery docentry from dbo.[Infocus_EDI_810_OINV] for HeaderId = " + record.HeaderId + " => " + inv.Message);
                }
                if (String.IsNullOrWhiteSpace(oInvNo))
                {
                    return null;
                }
                else
                {
                    int InvNo = -1;
                    try
                    {
                        InvNo = Convert.ToInt32(oInvNo);
                    }
                    catch
                    {
                        InvNo = -1;
                    }
                    if (InvNo == -1)
                    {
                        return null;
                    }
                    else
                    {
                        try
                        {
                            InvoiceLine invoiceLines = (from v in context.InvoiceLines.Include("Invoice").Include("Invoice.InvoiceLines")
                                                        where v.DocEntry == InvNo
                                                        && ((v.TreeType == "N") || (v.TreeType == "S"))
                                                        select v).FirstOrDefault();
                            if (invoiceLines != null)
                            {
                                return invoiceLines.Invoice;
                            }
                        }
                        catch (Exception r)
                        {
                            String oInnerEx = "";
                            try
                            {
                                oInnerEx = r.InnerException.Message;
                            }
                            catch
                            {
                                oInnerEx = "";
                            }
                            if (oInnerEx.Trim().Length > 0)
                            {
                                _logger.Error("Error getting invoice line =>" + r.Message + " Inner Exception: " + oInnerEx);
                            }
                            else
                            {
                                _logger.Error("Error getting invoice line =>" + r.Message);
                            }
                            return null;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                _logger.Error("Could not find Invoice", e);
                _logger.Error("Could not find Invoice =>" + oErrMesg);
                return null;
            }
            return null;
        }
        // 08-04-2022 end
        private Delivery FindMatchingDelivery(WebApiDbContext context, Edi850HeaderRecord record)
        {
            try
            {
                DeliveryLine deliveryLine = (from v in context.DeliveryLines.Include("Delivery").Include("Delivery.DeliveryLines")
                                             where v.BaseType == 17
                                                 && v.BaseEntry == record.SalesOrderKey
                                                 && v.Delivery.Canceled == "N"
                                                 && (v.TargetType == null || v.TargetType != 16)
                                                 && v.Delivery.U_Info_BOL != null
                                                 && ((v.TreeType == "N") || (v.TreeType == "S")) // 11-07-2018
                                                 && (v.Delivery.U_Info850HdrId == record.HeaderId || v.Delivery.CreateDate >= record.ProcessedDateTime) // 01-27-2023
                                             //&& v.LineNumber850 > 0 // 04-30-2019 
                                             select v).FirstOrDefault();
                if (deliveryLine != null)
                {
                    return deliveryLine.Delivery;
                }
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                _logger.Error("Could not find Delivery", e);
                _logger.Error("Could not find Delivery =>" + oErrMesg);
                return null;
            }
            return null;
        }

        // 08-11-2023 begin
        private Delivery[] getDeliveries(WebApiDbContext context, Edi940HeaderRecord record)
        {
            try
            {
                List<Delivery> list = FindDeliveries(context, record);
                Delivery[] deliveries = list.ToArray();
                return deliveries;
            }
            catch (Exception d)
            {
                _logger.Error("Error in getDeliveries: ", d);

                return null;
            }
        }
        private List<Delivery> FindDeliveries(WebApiDbContext context, Edi940HeaderRecord record)  // 940 only
        {
            try
            {
                List<Delivery> deliveries = (from v in context.Deliveries.Include("DeliveryLines")
                                             where v.Canceled == "N"
                                                 && v.U_Info_BOL != null
                                                 && (v.NumAtCard == record.PurchaseOrderReference
                                                 || (v.CardCode == "3PL-C0024" && v.NumAtCard.StartsWith(record.PurchaseOrderReference))) // 09-08-2023 
                                                 && v.CardCode == record.SBOCardCode
                                                 && v.U_Info850HdrId == record.HeaderId
                                             select v).ToList();
                return deliveries;
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                _logger.Error("Error in FindDeliveries: " + e + ", " + oErrMesg);
                return null;
            }
        }
        // 08-11-2023 end

        // 11-07-2018 begin
        /*
        private Delivery FindMatchingDeliverySQL(WebApiDbContext context, Edi850HeaderRecord record, String connectionName)
        {
            try
            {
                string oDelNo = "";
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(connectionName)))
               {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand("select top 1  t0.DocEntry from DLN1 t0 left join ODLN t1 on t0.DocEntry = t1.DocEntry where BaseType = '17' and BaseEntry = " + record.SalesOrderKey + 
                        " and t1.NumAtCard = " + record.PurchaseOrderReference + " and t0.TreeType in ('S','N') ", sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                try
                                {
                                    oDelNo = (String)reader[0];
                                }
                                catch
                                {
                                    oDelNo = "";
                                }
                            }
                        }
                    }
                    sqlConnection.Close();
                }
                DeliveryLine deliveryLine = (from v in context.DeliveryLines.Include("Delivery").Include("Delivery.DeliveryLines")
                                             where v.BaseType == 17
                                                 && v.BaseEntry == record.SalesOrderKey
                                                 && v.Delivery.Canceled == "N"
                                                 && (v.TargetType == null || v.TargetType != 16)
                                                 && v.Delivery.U_Info_BOL != null
                                                 && (v.TreeType == "S" || v.TreeType == "N")
                                             select v).FirstOrDefault();
                if (deliveryLine != null)
                {
                    return deliveryLine.Delivery;
                }
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                return null;
            }
            return null;
        }
        // 11-07-2018 end
         * */
        // 03-02-2022 begin
        public String get3PLCarrier(String pConnectionName, String pB1CardCode, String pCarrierCode)
        {
            String oCarrier = "";
            string oQuery = "select top 1 IsNull(c0.[U_SrcSCAC],'') as SrcSCAC from dbo.[@C3_CSCACC] c0  WITH(NOLOCK) where c0.[U_TrgSCAC] = '" + pCarrierCode + "' and c0.[U_CardCode] = '" + pB1CardCode + "'";
            try
            {
                string oConnectionString = GetConnectionString(pConnectionName);
                using (SqlConnection sqlConnection = new SqlConnection(oConnectionString))
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                oCarrier = "";
                            }
                            else
                            {
                                oCarrier = (String)reader["SrcSCAC"];

                            }
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception r)
            {

            }
            return oCarrier;
        }
        // 03-02-2022 end

        // 05-30-2017 begin
        [HttpPost]
        public Get870RecordsResponse Get870Records(Get870RecordsRequest request)
        {
            string oSBOCardCode = getSBOCardCode(request.CardCode); // 01-17-2018
            IPreProcess870Record iPreProcess870Record = null;
            IPostProcess870Record iPostProcess870Record = null;
            List<Edi850WithSalesOrder> listToProcess = new List<Edi850WithSalesOrder>();
            _logger.Debug("Entering Get870Records for " + request.CardCode + " : " + request.CardCode);
            //_logger.Debug("Processing the following request object:");
            Get870RecordsResponse response = new Get870RecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get870Records";
                response.Successful = false;
                return response;
            }
            try
            {
                // 01-17-2018 begin
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {  // 01-17-2018 end
                    iPreProcess870Record = Get870PreProcess(request.CardCode);
                    iPostProcess870Record = Get870PostProcess(request.CardCode);
                    // 01-17-2018 begin
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess870Record = Get870PreProcess(oSBOCardCode);
                    iPostProcess870Record = Get870PostProcess(oSBOCardCode);
                }
                // 01-17-2018 end
                List<Edi850HeaderRecord> listOf850Records = null;
                // 01-17-2018  begin
                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    listOf850Records = dbContext.Edi850HeaderRecords.Include("Details")
                                .Where(x => x.Processed == true
                                    && x.CardCode == request.CardCode
                                    //&& x.CardCode == oSBOCardCode // 01-17-2018
                                    && x.TrxPurpose != "01"
                                    //&& x.Processed870 == false
                                    && ((x.OrderType == "OS" || x.OrderType == "DR") && x.CardCode == "C001000") // 
                                                                                                                 //order types & cardcode for Lowe's
                                    && x.SalesOrderKey > 0).ToList();
                    _logger.Debug("There are " + listOf850Records.Count + " 870 records to evaluate");
                    foreach (var record in listOf850Records)
                    {
                        //_logger.Debug("Processing 870 with 850 Key " + record.HeaderId + ", CardCode: " + record.CardCode);
                        SOrder salesorder = FindMatchingSOrder870(dbContext, record, "870");
                        // 07-03-2017 begin
                        int NoLines = 0;
                        if (salesorder != null && (salesorder.U_InfoOrdStatus == record.Last870Status && record.Processed870 == true))
                        {
                            // 01-17-2018  begin
                            if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                            {
                                oConnectionName = "WebApiDbContext";
                            }
                            //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                            // 01-19-2017 end
                            {
                                sqlConnection.Open();

                                using (SqlCommand command = new SqlCommand("select COUNT(LineNum) from dbo.[CORTRI_870Lines_ToProcess] t0  WITH(NOLOCK) where t0.SalesOrderKey = " + salesorder.DocEntry, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        if (!reader.Read())
                                        {
                                            NoLines = 0;
                                        }
                                        else
                                        {
                                            NoLines = (Int32)reader[0];
                                        }
                                        sqlConnection.Close();
                                    }
                                }
                            }
                        }
                        //if (salesorder != null && (salesorder.U_InfoOrdStatus != record.Last870Status || record.Processed870 == false || salesorder.SOLines.S))
                        if (salesorder != null && (salesorder.U_InfoOrdStatus != record.Last870Status || record.Processed870 == false || NoLines > 0))
                        // 07-03-2017 end
                        {
                            /// _logger.Debug("Found 870 sales order with key " + salesorder.DocNum);
                            listToProcess.Add(new Edi850WithSalesOrder(record, salesorder));
                        }
                        else
                        {
                            _logger.Error("870 sales order line not found for 850 record with key " + record.HeaderId);
                        }
                    }
                }

                if (listToProcess.Count == 0)
                {
                    response.Successful = true;
                    return response;
                }
                //To do -Need to filter records, but not sure how to right now
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    sqlConnection.Open();

                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            if (iPreProcess870Record != null)
                            {
                                if (!iPreProcess870Record.OnPreProcess870Record(selectedRecord.SOrder, selectedRecord.Edi850HeaderRecord))
                                {
                                    continue;
                                }
                            }
                            Edi870HeaderRecord record = new Edi870HeaderRecord();
                            response.Edi870Records.Add(record);

                            record.BuyerName = selectedRecord.Edi850HeaderRecord.BuyerName;
                            if (selectedRecord.Edi850HeaderRecord.SBOCardCode == null || selectedRecord.Edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                            {  // 01-17-2018
                                record.CardCode = selectedRecord.Edi850HeaderRecord.CardCode;
                                // 01-17-2018 begin
                            }
                            else
                            {
                                record.CardCode = selectedRecord.Edi850HeaderRecord.SBOCardCode;
                            } //01-17-2018 end
                            record.CarrierCode = selectedRecord.SOrder.U_InfoW2Cc;
                            record.ConditionDescription = selectedRecord.Edi850HeaderRecord.ConditionDescription;
                            record.DeliveryPhoneNumber = selectedRecord.Edi850HeaderRecord.DeliveryPhoneNumber;
                            record.Department = selectedRecord.Edi850HeaderRecord.Department;
                            record.PaymentMethod = selectedRecord.Edi850HeaderRecord.PaymentMethod;
                            record.PromotionChargeCode = selectedRecord.Edi850HeaderRecord.PromotionChargeCode;
                            // 08-28-2019 begin
                            if (selectedRecord.Edi850HeaderRecord.PurchaseOrderDate == null ||
                                selectedRecord.Edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                            {
                                record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.RecordDate;
                            }
                            else
                            { // 08-28-2019 end
                                record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.PurchaseOrderDate;
                            } // 08-28-2019
                            record.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                            record.ReplenishmentNumber = selectedRecord.Edi850HeaderRecord.ReplenishmentNumber;
                            record.RequestedDeliveryDate = selectedRecord.Edi850HeaderRecord.RequestedDeliveryDate;
                            record.RequestedShipDate = selectedRecord.Edi850HeaderRecord.RequestedShipDate;
                            record.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                            record.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                            record.ShipToAttention = selectedRecord.Edi850HeaderRecord.ShipToAttention;
                            record.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                            record.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                            record.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                            record.ShipToStoreLocation = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation;
                            record.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                            record.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                            record.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                            record.TruckLoadNumber = selectedRecord.Edi850HeaderRecord.TruckLoadNumber;
                            record.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                            record.Last870Status = selectedRecord.SOrder.U_InfoOrdStatus;
                            record.ReasonCode870 = selectedRecord.SOrder.U_InfoReasonCd;
                            record.ConfirmationNo = selectedRecord.SOrder.DocNum;

                            record.AsnShipDate = selectedRecord.SOrder.DocDueDate;
                            record.BillOfLading = selectedRecord.SOrder.U_Info_BOL;
                            // 09-15-2021 begin
                            if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                            {
                                record.BillOfLading = record.BillOfLading.Replace("-", "");
                            }
                            // 09-15-2021 end
                            record.TransportationMethod = String.Empty;
                            // 03-04-2019 begin
                            try
                            {
                                if (!String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.TransportMethod))
                                {
                                    record.TransportationMethod = selectedRecord.Edi850HeaderRecord.TransportMethod;
                                }
                            }
                            catch
                            {

                            }
                            // 03-04-2019 end
                            // 07-03-2017 begin
                            if (selectedRecord.SOrder.U_InfoOrdStatus == "EX" && selectedRecord.SOrder.U_InfoW2EDelDate != null)
                            {
                                record.ExpectedDeliveryDate = selectedRecord.SOrder.U_InfoW2EDelDate;
                            }
                            if (selectedRecord.SOrder.U_InfoOrdStatus == "ED" && selectedRecord.SOrder.U_InfoW2EShDate != null)
                            {
                                record.ExpectedShipDate = selectedRecord.SOrder.U_InfoW2EShDate;
                            }
                            // 07-03-2017 end
                            foreach (SOLine soLine in selectedRecord.SOrder.SOLines)
                            {
                                // string oAltVendorItem = getAltVendorItem(oConnectionName, selectedRecord.SOrder.DocEntry, soLine);


                                if (soLine.TreeType == "N" || soLine.TreeType == "S") // 01-20-2018
                                {
                                    Edi870DetailRecord detail870Record = new Edi870DetailRecord();
                                    record.Details.Add(detail870Record);
                                    //detail870Record.BuyerItemCode = soLine.ItemCode;
                                    //if (String.IsNullOrWhiteSpace(oAltVendorItem))
                                    //{
                                    detail870Record.VendorItemCode = soLine.ItemCode;
                                    /*}
                                    else
                                    {
                                        detail870Record.VendorItemCode = oAltVendorItem;
                                    }*/
                                    // 10-14-2021 begin
                                    // remove quotes from item description
                                    //detail870Record.ItemDescription = soLine.Dscription;
                                    string oItmDesc = soLine.Dscription;
                                    oItmDesc = oItmDesc.Replace('"', ' ');
                                    // 10-22-2021 begin
                                    oItmDesc = oItmDesc.Replace("  ", " ");
                                    oItmDesc = oItmDesc.Trim();
                                    if (oItmDesc.Length > 80)
                                    {
                                        oItmDesc = oItmDesc.Substring(0, 80);
                                    }
                                    // 10-22-2021 end
                                    detail870Record.ItemDescription = oItmDesc;
                                    // 10-14-2021 end
                                    // 
                                    detail870Record.LineNumber = (Int32)soLine.LineNumber850;
                                    detail870Record.Quantity = Convert.ToDouble(soLine.Quantity);
                                    detail870Record.QuantityShipped = Convert.ToDouble(soLine.DelivrdQty);
                                    detail870Record.UnitPrice = Convert.ToDouble(soLine.Price);
                                    detail870Record.UnitOfMeasure = soLine.UomCode;
                                    if (String.IsNullOrWhiteSpace(detail870Record.UnitOfMeasure)
                                        || detail870Record.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        detail870Record.UnitOfMeasure = "EA";
                                    }
                                    if (String.IsNullOrWhiteSpace(soLine.LineItemStatus))
                                    {
                                        detail870Record.Item870Status = selectedRecord.SOrder.U_InfoOrdStatus;
                                    }
                                    else
                                    {
                                        detail870Record.Item870Status = soLine.LineItemStatus;
                                        // 07-03-2017 begin
                                        //}
                                        if (!(String.IsNullOrWhiteSpace(soLine.LineItemStatus)) && (soLine.LineItemStatus != selectedRecord.SOrder.U_InfoOrdStatus
                                            && (soLine.LineItemStatus == "EX" || soLine.LineItemStatus == "ED")))
                                        {
                                            if (!(String.IsNullOrWhiteSpace(soLine.LineItemStatus)) && soLine.LineItemStatus == "EX" && soLine.ExpectedLnDeliveryDate != null)
                                            {
                                                detail870Record.ExpectedLnDeliveryDate = soLine.ExpectedLnDeliveryDate;
                                            }
                                            if (!(String.IsNullOrWhiteSpace(soLine.LineItemStatus)) && soLine.LineItemStatus == "ED" && soLine.ExpectedLnShipDate != null)
                                            {
                                                detail870Record.ExpectedLnShipDate = soLine.ExpectedLnShipDate;
                                            }
                                        }
                                    }
                                    // 07-03-2017 end

                                    if (!String.IsNullOrWhiteSpace(soLine.LineItemRsnCd))
                                    {
                                        detail870Record.ItemReasonCode870 = soLine.LineItemRsnCd;
                                    }
                                    using (SqlCommand command = new SqlCommand("select top 1 s0.Substitute from dbo.OSCN s0 WITH(NOLOCK) where s0.ItemCode = '" + soLine.ItemCode + "' and s0.CardCode = '" + selectedRecord.SOrder.CardCode + "'", sqlConnection))
                                    {
                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            if (!reader.Read())
                                            {
                                                throw new WebApiException("Could not locate Vendor Item Code for Item Code " + soLine.ItemCode);
                                            }
                                            detail870Record.BuyerItemCode = (String)reader[0];
                                        }
                                    }
                                }
                            }
                            if (iPostProcess870Record != null)
                            {
                                iPostProcess870Record.OnPostProcess870Record(selectedRecord.SOrder, selectedRecord.Edi850HeaderRecord, record);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                            _logger.Error(ex.Message);
                        }
                    }
                    sqlConnection.Close();
                }

                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    sqlConnection.Open();

                    DateTime now = DateTime.Now;
                    // 01-17-2018  begin
                    if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                    {
                        oConnectionName = "WebApiDbContext";
                    }
                    //    using (WebApiDbContext dbContext = new WebApiDbContext())

                    using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                    // 01-17-2018 end
                    {
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate == null)
                            {
                                throw new WebApiException("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                            }
                            if (selectedRecord.IsError)
                            {
                                recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                recordToUpdate.Processed870 = false;
                            }
                            else
                            {
                                recordToUpdate.Processed870 = true;
                                recordToUpdate.Processed870DateTime = now;
                                recordToUpdate.ErrorMessage = String.Empty;
                            }
                            string oOrdStatus = null;
                            DateTime oCurrentDateTime = DateTime.Now; // 07-03-2017
                            DateTime oExpDelDate = oCurrentDateTime;  // 07-03-2017
                            DateTime oExpShDate = oCurrentDateTime;// 07-03-2017
                            using (SqlCommand command = new SqlCommand("select top 1 t1.[U_InfoOrdStatus], t1.[U_InfoW2EDelDate], t1.[U_InfoW2EShDate] from dbo.ORDR t1  WITH(NOLOCK)  where t1.[NumAtCard] = '" + recordToUpdate.PurchaseOrderReference + "' and t1.[CardCode] = '" + selectedRecord.SOrder.CardCode + "' and t1.[DocEntry] = " + selectedRecord.SOrder.DocEntry, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (reader.Read())
                                    {
                                        try // 07-03-2017
                                        {
                                            oOrdStatus = (String)reader[0];
                                            // 07-03-2017 begin
                                        }
                                        catch
                                        {
                                            oOrdStatus = "";
                                        }
                                        try
                                        {
                                            oExpDelDate = (DateTime)reader[1];
                                        }
                                        catch
                                        {
                                            oExpDelDate = oCurrentDateTime;
                                        }
                                        try
                                        {
                                            oExpShDate = (DateTime)reader[2];
                                        }
                                        catch
                                        {
                                            oExpShDate = oCurrentDateTime;
                                        }
                                        // 07-03-2017 end
                                    }
                                }
                            }
                            if (oOrdStatus != null)
                            {
                                recordToUpdate.Last870Status = oOrdStatus;
                            }
                            // 07-03-2017 begin
                            if (oExpDelDate != null && oCurrentDateTime != oExpDelDate)
                            {
                                recordToUpdate.ExpectedDeliveryDate = oExpDelDate;
                            }
                            if (oExpShDate != null && oExpDelDate != oCurrentDateTime)
                            {
                                recordToUpdate.ExpectedShipDate = oExpShDate;
                            }
                            // 07-03-2017 end
                        }
                        // 07-03-2017 begin
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850DetailRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate != null && !selectedRecord.IsError)
                            {
                                string oLnStatus = null;
                                string oLnReason = null;
                                DateTime oCurrentDateTime = DateTime.Now;
                                DateTime oExpDelDate = oCurrentDateTime;
                                DateTime oExpShDate = oCurrentDateTime;
                                using (SqlCommand command = new SqlCommand("select top 1  r1.[U_InfoItmStatus], r1.[U_InfoItmRsn], r1.[U_InfoW2ELnDelDate], r1.[U_InfoW2ELnShpDate] from dbo.RDR1 r1 WITH(NOLOCK) where r1.DocEntry = " + selectedRecord.SOrder.DocEntry + " and coalesce(r1.[U_InfoW2LNo],0) = " + recordToUpdate.LineNumber, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            try
                                            {
                                                oLnStatus = (String)reader[0];
                                            }
                                            catch
                                            {
                                                oLnStatus = "";
                                            }
                                            try
                                            {
                                                oLnReason = (String)reader[1];
                                            }
                                            catch
                                            {
                                                oLnReason = "";
                                            }
                                            try
                                            {
                                                oExpDelDate = (DateTime)reader[2];
                                            }
                                            catch
                                            {
                                                oExpDelDate = oCurrentDateTime;
                                            }
                                            try
                                            {
                                                oExpShDate = (DateTime)reader[3];
                                            }
                                            catch
                                            {
                                                oExpShDate = oCurrentDateTime;
                                            }
                                        }
                                    }
                                }
                                if (oExpDelDate != null && oExpDelDate != oCurrentDateTime)
                                {
                                    recordToUpdate.ExpectedLnDeliveryDate = oExpDelDate;
                                }
                                if (oExpShDate != null && oExpShDate != oCurrentDateTime)
                                {
                                    recordToUpdate.ExpectedLnShipDate = oExpShDate;
                                }
                            }
                        }
                        // 07-03-2017 end
                        dbContext.SaveChanges();
                    }
                    sqlConnection.Close();
                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                response.ErrorMessage = ex.Message;
                _logger.Error(ex.Message);
            }
            _logger.Debug("Returning the 870 response object which contains " + response.Edi870Records.Count + " 870s");
            //_logger.Debug("{@Get870RecordsResponse}", response);
            _logger.Debug("Leaving Get870Records for " + request.CardCode);
            return response;
        }

        private IPreProcess870Record Get870PreProcess(String cardCode)
        {
            String preProcess870Record = ConfigurationManager.AppSettings["PreProcess870Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess870Record))
            {
                preProcess870Record = ConfigurationManager.AppSettings["PreProcess870Record"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess870Record))
            {
                try
                {
                    IPreProcess870Record iPreProcess870Record = (IPreProcess870Record)Activator.CreateInstance(Type.GetType(preProcess870Record, true));
                    return iPreProcess870Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess870Record", ex);
                    _logger.Error("Could not instantiate IPreProcess870Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess870Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private IPostProcess870Record Get870PostProcess(String cardCode)
        {
            String postProcess870Record = ConfigurationManager.AppSettings["PostProcess870Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess870Record))
            {
                postProcess870Record = ConfigurationManager.AppSettings["PostProcess870Record"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess870Record))
            {
                try
                {
                    IPostProcess870Record iPostProcess870Record = (IPostProcess870Record)Activator.CreateInstance(Type.GetType(postProcess870Record, true));
                    return iPostProcess870Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess870Record", ex);
                    _logger.Error("Could not instantiate IPostProcess870Record +>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess870Record. Reason: " + ex.Message);
                }
            }
            return null;
        }

        // 03-09-2021 begin

        [HttpPost]
        public Get753RecordsResponse Get753Records(Get753RecordsRequest request)
        {
            string oSBOCardCode = getSBOCardCode(request.CardCode);
            IPreProcess753Record iPreProcess753Record = null;
            IPostProcess753Record iPostProcess753Record = null;
            List<Edi850WithSalesOrder> listToProcess = new List<Edi850WithSalesOrder>();
            _logger.Debug("Entering Get753Records for " + request.CardCode + " : " + request.CardCode);
            // _logger.Debug("Processing the following request object:");
            Get753RecordsResponse response = new Get753RecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get753Records";
                response.Successful = false;
                return response;
            }
            try
            {
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {
                    iPreProcess753Record = Get753PreProcess(request.CardCode);
                    iPostProcess753Record = Get753PostProcess(request.CardCode);
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess753Record = Get753PreProcess(oSBOCardCode);
                    iPostProcess753Record = Get753PostProcess(oSBOCardCode);
                }

                List<Edi850HeaderRecord> listOf850Records = null;

                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                _logger.Debug("ConnectionName => " + oConnectionName);
                // set 753 flag from Sales Order
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    sqlConnection.Open();
                    // update 753 flag if status has changed
                    // 03-26-2026 lrussell begin
                    // add query to get count of records to update before trying to update Processed753 flag
                    int numHdrsFound = 0;
                    string oQuery1 = "SELECT COUNT(HeaderId) FROM InfocusEdi850HeaderRecord WITH(NOLOCK) where Processed753=0 and HeaderId in " +
                                    "(select HeaderId from dbo.[Info_EDI_753_Status_Changed] t0 where t0.CardCode = '" +
                                    request.CardCode.Trim() + "')";
                    try
                    {
                        SqlCommand command1 = new SqlCommand(oQuery1, sqlConnection);
                        using (System.Data.SqlClient.SqlCommand command = new System.Data.SqlClient.SqlCommand(oQuery1, sqlConnection))
                        {
                            using (System.Data.SqlClient.SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    numHdrsFound = 0;
                                }
                                string svalue  = (String)reader[0];
                                try
                                {
                                    numHdrsFound = Convert.ToInt32(svalue);

                                }
                                catch
                                {
                                    numHdrsFound = 0;
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                    if (numHdrsFound == 0)
                    {
                        _logger.Debug(numHdrsFound.ToString() + " 753 records to update with new status");
                        sqlConnection.Close();
                    }
                    else
                    {
                        _logger.Debug("There are " + numHdrsFound + " 753 records to update with new status");
                        // 03-23-2026 lrussell end
                        string oQuery = "UPDATE InfocusEdi850HeaderRecord set Processed753=0 where HeaderId in " +
                                        "(select HeaderId from dbo.[Info_EDI_753_Status_Changed] t0 where t0.CardCode = '" +
                                        request.CardCode.Trim() + "')";
                        try
                        {
                            SqlCommand command = new SqlCommand(oQuery, sqlConnection);
                            SqlTransaction sqlTrx = command.Transaction;

                            /*using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }*/
                            command.ExecuteNonQuery();
                            sqlTrx.Commit();
                            sqlTrx.Dispose();
                        }
                        catch (Exception del2)
                        {
                            _logger.Error("Error updating  753 status => " + del2.Message);
                        }
                        finally
                        {
                            sqlConnection.Close();
                        }
                    } // 03-23-2026 lrussell
                }
                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                {
                    listOf850Records = dbContext.Edi850HeaderRecords.Include("Details")
                                .Where(x => x.Processed == true
                                    && x.CardCode == request.CardCode
                                    && x.TrxPurpose != "01"
                                    && x.Processed753 == false
                                    && x.Processed856 == false
                                    && (x.PaymentMethod == "CC" || x.PaymentMethod == "CP")
                                    && x.SalesOrderKey > 0).ToList();
                    _logger.Debug("There are " + listOf850Records.Count + " 753 records to evaluate");
                    DateTime oCurrDate = DateTime.Now.AddHours(Convert.ToDouble(-1));
                    foreach (var record in listOf850Records)
                    {
                        // _logger.Debug("Processing 753 with 850 Key " + record.HeaderId + ", CardCode: " + record.CardCode);
                        SOrder salesorder = FindMatchingSOrder753(dbContext, record, "753");
                        bool send753 = false;
                        if (salesorder != null)
                        {
                            if (String.IsNullOrWhiteSpace(salesorder.U_InfoW2753))
                            {
                                salesorder.U_InfoW2753 = "N";
                            }
                            // 02-02-2023 begin
                            if (record.Processed753 == false)
                            {
                                salesorder.U_InfoW2753 = "N";
                            }
                            // 02-02-2023 end
                            DateTime send753On = salesorder.DocDueDate.AddDays(Convert.ToDouble(-3));
                            // 06-03-2022 begin
                            // if current hour > 6am then subtract 4 days rather than 3
                            int oCurrHour = DateTime.Now.Hour;
                            if (oCurrHour > 6)
                            {
                                send753On = salesorder.DocDueDate.AddDays(Convert.ToDouble(-4));
                            }
                            // 06-03-2022 end
                            // 03-26-2026 lrussell begin
                            // remove check of U_ZSPS_SchedShpDt for send753On date
                            /*
                            // 01-27-2023 begin
                            // if send753On < the current date use udf U_ZSPS_SchedShpDt
                            if (salesorder.U_ZSPS_SchedShpDt != null)
                            {
                                DateTime oSchedShpDt = (DateTime)salesorder.U_ZSPS_SchedShpDt;
                                if (oCurrHour > 6)
                                {
                                    send753On = oSchedShpDt.AddDays(Convert.ToDouble(-4));
                                }
                                else
                                {
                                    send753On = oSchedShpDt.AddDays(Convert.ToDouble(-3));
                                }
                            }
                            // 01-27-2023 end
                            */
                            // 03-26-2026 lrussell end  
                            // 03-26-2026 lrussell begin
                            // add check for send753On date on Saturday or Sunday and adjust to next Monday if so
                            if (send753On.DayOfWeek == DayOfWeek.Saturday)
                            {
                                send753On = send753On.AddDays(Convert.ToDouble(2));
                            }
                            else if (send753On.DayOfWeek == DayOfWeek.Sunday)
                            {
                                send753On = send753On.AddDays(Convert.ToDouble(1));
                            }
                            // 03-26-2026 lrussell end
                            // 05-24-2022 begin
                            // if (oCurrDate >= send753On && oCurrDate < send753On.AddDays(Convert.ToDouble(1))
                            if (oCurrDate >= send753On // 05-24-2022 end
                                && (String.IsNullOrWhiteSpace(salesorder.Canceled) || salesorder.Canceled == "N") // 06-03-2022
                                  // 01-27-2023 begin
                                  // remove requirement that sales order still be open
                                  // && salesorder.DocStatus == "O") // 06-18-2021
                                )
                            // 01-27-2023 end
                            {
                                send753 = true;
                            }
                            else
                            {
                                // 01-27-2023 begin
                                if (salesorder.Canceled == "Y")
                                {
                                    _logger.Error("Sales Order " + salesorder.DocNum + " has been canceled -- 753 not sent");
                                    record.ErrorMessage = "Sales Order " + salesorder.DocNum + " has been canceled -- 753 not sent";
                                }
                                else
                                {
                                    // 01-27-2023 end
                                    _logger.Error("Sales Order " + salesorder.DocNum + " is past the 3 day window for 753");
                                    record.ErrorMessage = "Sales Order " + salesorder.DocNum + " is past the 3 day window for 753";
                                } // 01-27-2023 
                                dbContext.SaveChanges();
                            }
                        }
                        // 03-26-2026 lrusssell added check for salesorder.DocNum
                        if (salesorder != null && (record.Processed753 == false || salesorder.U_InfoW2753 != "Y") && send753 == true && salesorder.DocNum > 0)
                        {
                            // _logger.Debug("Found sales order with key " + salesorder.DocNum);
                            listToProcess.Add(new Edi850WithSalesOrder(record, salesorder));
                        }
                        else
                        {
                            // 03-26-2026 lrussell begin -- add sales order number to error messages for easier troubleshooting
                            //_logger.Error("Sales order lines not found for 850 record with key " + record.HeaderId);
                            if (salesorder == null)
                            {
                                _logger.Error("No matching sales order found for 850 record with key " + record.HeaderId);
                            }
                            else if (salesorder.DocNum <= 0)
                            {
                                _logger.Error("Sales order number not found for 850 HeaderId " + record.HeaderId);
                            }
                            else
                            {
                                //_logger.Error("No lines found for Sales Order " + salesorder.DocNum.ToString(0 + " with 850 HeaderId " + record.HeaderId));
                                _logger.Error("Error finding Sales Order " + salesorder.DocNum.ToString(0 + " with 850 HeaderId " + record.HeaderId));
                            }
                            // 03-26-2026 lrussell end
                        }
                    }
                }

                if (listToProcess.Count == 0)
                {
                    response.ErrorMessage = "No Matching 753s found"; //  02-13-2023
                    return response;
                }

                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    sqlConnection.Open();

                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            if (iPreProcess753Record != null)
                            {
                                if (!iPreProcess753Record.OnPreProcess753Record(selectedRecord.SOrder, selectedRecord.Edi850HeaderRecord))
                                {
                                    continue;
                                }
                            }
                            Edi753HeaderRecord record = new Edi753HeaderRecord();
                            response.Edi753Records.Add(record);
                            if (selectedRecord.Edi850HeaderRecord.SBOCardCode == null || selectedRecord.Edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                            {
                                record.CardCode = selectedRecord.Edi850HeaderRecord.CardCode;
                            }
                            else
                            {
                                record.CardCode = selectedRecord.Edi850HeaderRecord.SBOCardCode;
                            }
                            record.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                            record.TransactionCode = "00";
                            DateTime oCurrDate = DateTime.Now.AddHours(Convert.ToDouble(-1));
                            if (selectedRecord.Edi850HeaderRecord.Processed753DateTime != null && selectedRecord.Edi850HeaderRecord.Processed753DateTime < oCurrDate)
                            {
                                record.TransactionCode = "01";
                            }
                            DateTime oCurrentDate = DateTime.Now;
                            record.TransactionDate = oCurrentDate.ToLocalTime().ToString("yyyyMMdd");
                            record.TransactionTime = oCurrentDate.ToLocalTime().ToString("HHMM");
                            record.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                            if (String.IsNullOrWhiteSpace(record.VendorNumber))
                            {
                                record.VendorNumber = getDefaultVendor(selectedRecord.Edi850HeaderRecord.CardCode);
                            }
                            record.ShipperContact = "";
                            record.ShipperPhone = "";
                            record.ShipFromName = "";
                            record.ShipFromAddress1 = "";
                            record.ShipFromAddress2 = "";
                            record.ShipFromCity = "";
                            record.ShipFromState = "";
                            record.ShipFromZip = "";
                            record.ShipFromCountry = "US";

                            try
                            {
                                // update 753 flag if status does not equal order status
                                string oQuery = "SELECT [Send753On],[WhsCode],[WhsName],[CompanyName]," +
                                                "[ShipFromContact],[ShipFromAddr1],[ShipFromAddr2],[ShipFromCity],[ShipFromState],[ShipFromZip]," +
                                                "[ShipFromPhone], [ShipFromEmail] FROM [dbo].[Infocus_EDI_753]  WITH(NOLOCK) where CardCode = '" + selectedRecord.Edi850HeaderRecord.SBOCardCode + "' " +
                                                " and IntSoNo = " + selectedRecord.SOrder.DocEntry;
                                try
                                {
                                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                                    {
                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            if (reader.Read())
                                            {
                                                String oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[2];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipFromName = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[4];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipperContact = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[5];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipFromAddress1 = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[6];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipFromAddress2 = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[7];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipFromCity = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[8];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipFromState = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[9];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipFromZip = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[10];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipperPhone = oValue.Trim();
                                                }
                                                oValue = "";
                                                try
                                                {
                                                    oValue = (String)reader[11];
                                                }
                                                catch
                                                {
                                                    oValue = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    record.ShipperEmail = oValue.Trim();
                                                }
                                            }
                                        }
                                    }
                                }
                                catch (Exception del2)
                                {
                                    _logger.Error("Error getting ship from data for Sales Order " + selectedRecord.SOrder.DocNum + " => " + del2.Message);
                                }

                                // add detail for order
                                int oNextLine = 1;
                                int oNoPallets = 1;
                                decimal oSWeight = 0;
                                decimal oCubicFt = 0;
                                string isStackable = "N";
                                // 02-13-2023 begin
                                /*oQuery = "SELECT WhsName,CompanyName,ShipFromContact,ShipFromAddr2,ShipFromAddr2,ShipFromCity,ShipFromState,ShipFromZip," +
                                          "ShipFromPhone,Stackable,PalletH,PalletL,PalletW,ShipmtWeight,TotalPallets,CubicFt FROM [dbo].[Infocus_EDI_753]  WITH(NOLOCK) " +
                                          "where CardCode = '" + selectedRecord.Edi850HeaderRecord.SBOCardCode + "' and IntSoNo = " + selectedRecord.SOrder.DocEntry;
                                */
                                oQuery = "SELECT WhsName,CompanyName,ShipFromContact,ShipFromAddr2,ShipFromAddr2,ShipFromCity," +
                                              "ShipFromState,ShipFromZip, ShipFromPhone, " + //--Stackable,PalletH,PalletL,PalletW, " +
                                              "SUM(ShipmtWeight) as 'ShipmtWeight',Sum(TotalPallets) as TotalPallets,SUM(CubicFt) as CubicFt,  " +
                                              "ShipToName, ShipToAddress1, ShipToAddress2, ShipToCity, ShipToState, ShipToZip, " +
                                              "ShipToCountry, ShipToStoreLocation, ShipToLocationCode, PoNo " +
                                              "FROM [dbo].[Infocus_EDI_753]  " +
                                              "where CardCode = '" + selectedRecord.Edi850HeaderRecord.SBOCardCode + "' and IntSoNo = " + selectedRecord.SOrder.DocEntry +
                                              "group by WhsName,CompanyName,ShipFromContact,ShipFromAddr2,ShipFromAddr2,ShipFromCity, " +
                                             "ShipFromState,ShipFromZip, ShipFromPhone, ShipToName, ShipToAddress1, ShipToAddress2, ShipToCity, ShipToState, " +
                                             "ShipToZip,ShipToCountry, ShipToStoreLocation, ShipToLocationCode, PoNo";
                                // 02-13-2023 end
                                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                                        {
                                            if (reader.HasRows)
                                            {

                                                //int oRecordNo = 0;

                                                while (reader.Read())
                                                {
                                                    Edi753DetailRecord detailRecord = new Edi753DetailRecord();
                                                    record.Details.Add(detailRecord);
                                                    detailRecord.LineNumber = oNextLine;
                                                    // 02-27-2023 begin
                                                    String oPoNo = "";
                                                    try
                                                    {
                                                        oPoNo = (String)reader["PoNo"];
                                                    }
                                                    catch
                                                    {
                                                        oPoNo = record.PurchaseOrderReference;
                                                    }
                                                    if (String.IsNullOrWhiteSpace(oPoNo))
                                                    {
                                                        oPoNo = record.PurchaseOrderReference;
                                                    }
                                                    detailRecord.PurchaseOrderReference = oPoNo;
                                                    // 02-27-2023 end
                                                    // 02-24-2023 begin
                                                    //detailRecord.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                                                    // detailRecord.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                                                    //detailRecord.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                                                    //detailRecord.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                                                    //detailRecord.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                                                    //detailRecord.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                                                    //detailRecord.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                                                    // 02-17-2023 begin
                                                    /* if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.ShipToLocationCode))
                                                     {
                                                         detailRecord.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation;
                                                     }
                                                     else
                                                     {
                                                     // 02-17-2023 end
                                                         detailRecord.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                                                     } */
                                                    // detailRecord.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                                                    String oShipToName = "";
                                                    try
                                                    {
                                                        oShipToName = (String)reader["ShipToName"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                                                    }
                                                    detailRecord.ShipToName = oShipToName;
                                                    String oShipToAddress1 = "";
                                                    try
                                                    {
                                                        oShipToAddress1 = (String)reader["ShipToAddress1"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                                                    }
                                                    detailRecord.ShipToAddress1 = oShipToAddress1;
                                                    String oShipToAddress2 = "";
                                                    try
                                                    {
                                                        oShipToAddress2 = (String)reader["ShipToAddress2"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                                                    }
                                                    detailRecord.ShipToAddress2 = oShipToAddress2;
                                                    String oShipToCity = "";
                                                    try
                                                    {
                                                        oShipToCity = (String)reader["ShipToCity"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                                                    }
                                                    detailRecord.ShipToCity = oShipToCity;
                                                    String oShipToState = "";
                                                    try
                                                    {
                                                        oShipToState = (String)reader["ShipToState"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                                                    }
                                                    detailRecord.ShipToState = oShipToState;
                                                    String oShipToZip = "";
                                                    try
                                                    {
                                                        oShipToZip = (String)reader["ShipToZip"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                                                    }
                                                    detailRecord.ShipToZip = oShipToZip;
                                                    String oShipToCountry = "";
                                                    try
                                                    {
                                                        oShipToCountry = (String)reader["ShipToCountry"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                                                    }
                                                    detailRecord.ShipToCountry = oShipToCountry;
                                                    String oShipToLocation = "";
                                                    try
                                                    {
                                                        oShipToLocation = (String)reader["ShipToLocationCode"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToLocation = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                                                    }
                                                    String oShipToStore = "";
                                                    try
                                                    {
                                                        oShipToStore = (String)reader["ShipToStoreLocation"];
                                                    }
                                                    catch
                                                    {
                                                        oShipToStore = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation;
                                                    }
                                                    if (String.IsNullOrWhiteSpace(oShipToLocation))
                                                    {
                                                        detailRecord.ShipToLocationCode = oShipToStore;
                                                    }
                                                    else
                                                    {
                                                        detailRecord.ShipToLocationCode = oShipToLocation;
                                                    }
                                                    // 02-23-2023 end
                                                    // 03-26-2026 lrussell begin
                                                    // remove use of U_ZSPS_SchedShpDt when setting so line ready to ship date
                                                    detailRecord.ReadyToShipDate = selectedRecord.SOrder.DocDueDate.ToLocalTime().ToString("yyyyMMdd");
                                                    /*
                                                    // 02-02-2023 begin
                                                    if (selectedRecord.SOrder.U_ZSPS_SchedShpDt != null)
                                                    {
                                                        DateTime oSchedShip = (DateTime)selectedRecord.SOrder.U_ZSPS_SchedShpDt;
                                                        detailRecord.ReadyToShipDate = oSchedShip.ToLocalTime().ToString("yyyyMMdd");
                                                    }
                                                    else
                                                    {
                                                        // 02-02-2023 end
                                                        detailRecord.ReadyToShipDate = selectedRecord.SOrder.DocDueDate.ToLocalTime().ToString("yyyyMMdd");
                                                    } // 02-02-2023 
                                                    */
                                                    // 03-26-2026 lrussell end

                                                    detailRecord.PickUpTime = "0800";

                                                    try
                                                    {
                                                        isStackable = (String)reader["Stackable"];
                                                    }
                                                    catch
                                                    {
                                                        isStackable = "";
                                                    }
                                                    detailRecord.Stackable = isStackable;
                                                    try
                                                    {
                                                        oNoPallets = (Int32)reader["TotalPallets"];
                                                    }
                                                    catch
                                                    {
                                                        oNoPallets = 1;
                                                    }
                                                    detailRecord.PackageCode = "PLT";
                                                    detailRecord.PackageCount = Convert.ToInt32(oNoPallets);
                                                    try
                                                    {
                                                        oSWeight = (decimal)reader["ShipmtWeight"];
                                                    }
                                                    catch
                                                    {
                                                        oSWeight = 0;
                                                    }
                                                    detailRecord.WeightUOMCode = "L";
                                                    detailRecord.ShipmentWeight = Convert.ToDouble(oSWeight);
                                                    try
                                                    {
                                                        oCubicFt = (decimal)reader["CubicFt"];
                                                    }
                                                    catch
                                                    {
                                                        oCubicFt = 0;
                                                    }
                                                    detailRecord.VolumeQual = "E";
                                                    detailRecord.ShipmentVolume = Convert.ToDouble(oCubicFt);
                                                    oNextLine = oNextLine + 1;
                                                }
                                            }
                                        }
                                    }
                                    // sqlConnection.Close();
                                }

                            }
                            catch (Exception e)
                            {

                            }



                            if (iPostProcess753Record != null)
                            {
                                iPostProcess753Record.OnPostProcess753Record(selectedRecord.SOrder, selectedRecord.Edi850HeaderRecord, record);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                            _logger.Error(ex.Message);
                        }
                    }
                    sqlConnection.Close();
                }

                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    sqlConnection.Open();

                    DateTime now = DateTime.Now;
                    if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                    {
                        oConnectionName = "WebApiDbContext";
                    }

                    using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                    {
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate == null)
                            {
                                throw new WebApiException("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                            }
                            if (selectedRecord.IsError)
                            {
                                recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                recordToUpdate.Processed753 = false;
                            }
                            else
                            {
                                recordToUpdate.Processed753 = true;
                                try
                                {
                                    int oYear = 0;
                                    string oUpdDate = ((DateTime)recordToUpdate.Processed753DateTime).ToLocalTime().ToString("yyyyMMdd");
                                    try
                                    {
                                        string sYear = oUpdDate.Substring(0, 4);
                                        oYear = Convert.ToInt32(sYear);
                                    }
                                    catch
                                    {
                                        oYear = 0;
                                    }
                                    if (String.IsNullOrWhiteSpace(oUpdDate) || oYear < 2020)
                                    {
                                        recordToUpdate.Last753TrxType = "00";
                                    }
                                    else
                                    {
                                        recordToUpdate.Last753TrxType = "01";
                                    }
                                }
                                catch
                                {
                                    recordToUpdate.Last753TrxType = "00";
                                }
                                recordToUpdate.Processed753DateTime = now;
                                recordToUpdate.ErrorMessage = String.Empty;
                                dbContext.SaveChanges();
                            }
                        }
                        SqlTransaction sqlTrx = sqlConnection.BeginTransaction();
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850DetailRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            SOrder salesorder = FindMatchingSOrder753(dbContext, selectedRecord.Edi850HeaderRecord, "753");
                            if (salesorder != null)
                            {

                                if (recordToUpdate != null && !selectedRecord.IsError)
                                {
                                    selectedRecord.SOrder.U_InfoW2753 = "Y";
                                    salesorder.U_InfoW2753 = "Y";
                                }
                                else
                                {
                                    selectedRecord.SOrder.U_InfoW2753 = "N";
                                    salesorder.U_InfoW2753 = "N";
                                }

                            }
                        }
                        sqlTrx.Commit();
                        sqlTrx.Dispose();
                        dbContext.SaveChanges();
                    }
                    sqlConnection.Close();
                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                response.ErrorMessage = ex.Message;
                _logger.Error(ex.Message);
            }
            _logger.Debug("Returning the 753 response object which contains " + response.Edi753Records.Count + " 753s");
            _logger.Debug("Leaving Get753Records for " + request.CardCode);
            return response;
        }

        private IPreProcess753Record Get753PreProcess(String cardCode)
        {
            String preProcess753Record = ConfigurationManager.AppSettings["PreProcess753Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess753Record))
            {
                preProcess753Record = ConfigurationManager.AppSettings["PreProcess753Record"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess753Record))
            {
                try
                {
                    IPreProcess753Record iPreProcess753Record = (IPreProcess753Record)Activator.CreateInstance(Type.GetType(preProcess753Record, true));
                    return iPreProcess753Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess753Record", ex);
                    _logger.Error("Could not instantiate IPreProcess753Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess753Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private IPostProcess753Record Get753PostProcess(String cardCode)
        {
            String postProcess753Record = ConfigurationManager.AppSettings["PostProcess753Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess753Record))
            {
                postProcess753Record = ConfigurationManager.AppSettings["PostProcess753Record"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess753Record))
            {
                try
                {
                    IPostProcess753Record iPostProcess753Record = (IPostProcess753Record)Activator.CreateInstance(Type.GetType(postProcess753Record, true));
                    return iPostProcess753Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess753Record", ex);
                    //_logger.Debug("Could not instantiate IPostProcess753Record +>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess753Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        // 03-09-2021 end

        private SOrder FindMatchingSOrder870(WebApiDbContext context, Edi850HeaderRecord record, String EDITrx)
        {
            try
            {
                // _logger.Debug("FindMatchingSOrder: " + record.SalesOrderKey);
                SOLine salesorderline = (from v in context.SOLines.Include("SOrder").Include("SOrder.SOLines")
                                         where v.DocEntry == record.SalesOrderKey
                                             //&& !(v.SOrder.U_InfoOrdStatus == record.Last870Status)
                                             && (v.TreeType == "N" || v.TreeType == "S") // 01-18-2018
                                             && (v.SOrder.Canceled == "N" ||
                                              (EDITrx.ToUpper().Trim() == "870" && record.Processed870 == false)) // 07-12-2021 send 855 if sales order is canceled & 855 has not been sent
                                         select v).FirstOrDefault();
                if (salesorderline != null)
                {
                    return salesorderline.SOrder;
                }
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(ex.InnerException.Message))
                    {
                        _logger.Error("Error Finding Matching Sales Order => " + ex.Message);
                    }
                    else
                    {
                        _logger.Error("Error Finding Matching Sales Order => " + ex.Message + "-> " + ex.InnerException.Message);
                    }
                }
                catch (Exception r)
                {

                }
                return null;
            }
        }
        // 05-30-2017 end

        // 05-18-2022 begin
        private SOrder FindMatchingSOrder753(WebApiDbContext context, Edi850HeaderRecord record, String EDITrx)
        {
            try
            {
                SOLine salesorderline = (from v in context.SOLines.Include("SOrder").Include("SOrder.SOLines")
                                         where v.DocEntry == record.SalesOrderKey
                                              && (v.TreeType == "N" || v.TreeType == "S")
                                             && (v.SOrder.Canceled == "N" ||
                                              (EDITrx.ToUpper().Trim() == "753" && record.Processed753 == false))
                                         select v).FirstOrDefault();
                if (salesorderline != null)
                {
                    return salesorderline.SOrder;
                }
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(ex.InnerException.Message))
                    {
                        _logger.Error("Error Finding Matching Sales Order => " + ex.Message);
                    }
                    else
                    {
                        _logger.Error("Error Finding Matching Sales Order => " + ex.Message + "-> " + ex.InnerException.Message);
                    }
                }
                catch (Exception r)
                {

                }
                return null;
            }
        }
        // 05-18-2022 end

        // 02-02-2021 begin
        private SOrder FindMatchingSOrder855(WebApiDbContext context, Edi850HeaderRecord record, DateTime pLastRecTrxDT, bool bSend855)
        {
            try
            {
                //_logger.Debug("Find Matching SOrder for 855: " + record.SalesOrderKey);
                SOLine salesorderline = (from v in context.SOLines.Include("SOrder").Include("SOrder.SOLines")
                                         where v.DocEntry == record.SalesOrderKey
                                             && (v.TreeType.Trim() == "N" || v.TreeType.Trim() == "S") // 01-18-2018
                                             && (record.Processed855 == false // 07-12-2021 send 855 if sales order is canceled & 855 has not been sent
                                                                              // || record.Processed855DateTime > pLastRecTrxDT) // 02-22-2022
                                              || record.Orig855ProcessedDateTime > pLastRecTrxDT // 02-25-2022
                                              || bSend855 == false) // 02-27-2022
                                         select v).FirstOrDefault();
                if (salesorderline != null)
                {
                    return salesorderline.SOrder;
                }
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    if (String.IsNullOrWhiteSpace(ex.InnerException.Message))
                    {
                        _logger.Error("Error Finding Matching Sales Order => " + ex.Message);
                    }
                    else
                    {
                        _logger.Error("Error Finding Matching Sales Order => " + ex.Message + "-> " + ex.InnerException.Message);
                    }
                }
                catch (Exception r)
                {

                }
                return null;
            }
        }
        // 02-02-2021 end
        // 01-17-2018 begin
        [HttpPost]
        public Get855RecordsResponse Get855Records(Get855RecordsRequest request)
        {
            string oSBOCardCode = getSBOCardCode(request.CardCode);
            IPreProcess855Record iPreProcess855Record = null;
            IPostProcess855Record iPostProcess855Record = null;
            List<Edi850WithSalesOrder> listToProcess = new List<Edi850WithSalesOrder>();
            // 02-22-2022 begin
            string logMessage = "Entering Get855Records for " + request.CardCode + " : " + request.CardCode;
            // _logger.Debug("Entering Get855Records for " + request.CardCode);
            _logger.Debug(logMessage); // 02-12-2006
            // 06-14-2024 begin
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-14-2024 end
            DateTime oLast855Date = DateTime.Now;
            string oLastTrxDateTime = request.LastRecTrxDT;
            if (!String.IsNullOrWhiteSpace(oLastTrxDateTime))
            {
                try
                {
                    oLast855Date = Convert.ToDateTime(oLastTrxDateTime);
                }
                catch (Exception ld)
                {
                    _logger.Error("Error processing last date/time => " + oLastTrxDateTime + "; " + ld.Message);
                    oLast855Date = DateTime.Now;
                }
            }
            if (!String.IsNullOrWhiteSpace(request.LastRecTrxDT))
            {
                logMessage = logMessage.Trim() + " LastRecTrxDT = " + request.LastRecTrxDT.ToString();
                _logger.Debug(logMessage);
            }
            // 08-17-2023 begin
            string oConnectionName = this.getConnectionName(request.CardCode);
            if (oConnectionName == null || oConnectionName.Trim().Length == 0)
            {
                oConnectionName = "WebApiDbContext";
            }
            Int32 NoDays = 0;
            Int32 oMaxDays = getMaxTD(oConnectionName);
            try
            {
                NoDays = (DateTime.Now - oLast855Date).Days;
            }
            catch (Exception nd)
            {
                String oErrMsg = nd.Message;
                _logger.Error("Error getting #Days between today & Last856Date: " + oErrMsg);
                NoDays = 0;
            }
            if (NoDays == 0 && oLast855Date.Date != DateTime.Now.Date)
            {
                _logger.Error("Error getting #Days between today & Last856Date, Last856Date will be set to today");
                oLast855Date = DateTime.Now;
            }
            else if (NoDays > oMaxDays)
            {
                oLast855Date = DateTime.Now.AddDays(-1 * oMaxDays);
                _logger.Error("# Days for Last Trx exceeds max, set LastTrxDt to " + oLast855Date.ToShortDateString());
            }
            // 08-17-2023 end


            // 02-22-2022 end
            //_logger.Debug("Processing the following request object:");
            Get855RecordsResponse response = new Get855RecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get855Records";
                response.Successful = false;
                return response;
            }
            try
            {
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {
                    iPreProcess855Record = Get855PreProcess(request.CardCode);
                    iPostProcess855Record = Get855PostProcess(request.CardCode);
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess855Record = Get855PreProcess(oSBOCardCode);
                    iPostProcess855Record = Get855PostProcess(oSBOCardCode);
                }

                List<Edi850HeaderRecord> listOf850Records = null;
                // 08-17-2023 begin
                // set connectionName earlier in code
                /*
                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                 */
                // 08-17-2023 end
                // 06-23-2020 begin
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    sqlConnection.Open();
                    // update 855 flag if Last855 status does not equal order status
                    string oQuery = "UPDATE InfocusEdi850HeaderRecord set Processed855=0 where HeaderId in " +
                                    "(select HeaderId from dbo.[Info_EDI_855_Status_Changed] t0 where t0.CardCode = '" +
                                    request.CardCode.Trim() + "')";
                    try
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception del2)
                    {
                        _logger.Error("Error updating 855 status => " + del2.Message);
                    }
                    // 08-02-2022 begin
                    // remove direct update
                    /*
                    // 02-12-2022 begin
                    oQuery = "UPDATE RDR1 set U_InfoW2LNo=0 where IsNull(U_InfoW2LNo,999) = 999 and DocEntry in  " +
                                    "(select SalesOrderKey from [InfocusEdi850HeaderRecord] t0 where t0.CardCode = '" +
                                    request.CardCode.Trim() + "' and Processed855=0)";
                    try
                    {
                        using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                    catch (Exception del2)
                    {
                        _logger.Error("Error updating RDR1 U_InfoW2LNo => " + del2.Message);
                    }
                    // 02-12-2022 end
                    */
                    // 08-02-2022 end
                    sqlConnection.Close();
                }

                // 06-23-2020 end
                // 03-11-2024 begin
                string oIs3PL = "N";
                string oSendPreSo855 = "N";
                string oQry1 = "select coalesce(w0.[3PL],'N') Is3PL, coalesce(w0.[SendPreSO855],'N') SendPreSo855 from InfocusEDI.dbo.WebApiDbContext w0  WITH(NOLOCK) " +
                               " where w0.CardCode = '" + request.CardCode.Trim() + "'";
                try
                {
                    using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                    {
                        sqlConnection.Open();

                        using (SqlCommand command = new SqlCommand(oQry1, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                if (!reader.Read())
                                {
                                    oSendPreSo855 = "N";
                                    oIs3PL = "N";
                                }
                                else
                                {
                                    try
                                    {
                                        oIs3PL = reader["Is3PL"].ToString();
                                    }
                                    catch
                                    {
                                        oIs3PL = "N";
                                    }
                                    try
                                    {
                                        oSendPreSo855 = reader["SendPreSo855"].ToString();
                                    }
                                    catch
                                    {
                                        oSendPreSo855 = "N";
                                    }
                                }
                            }
                        }
                        sqlConnection.Close();
                    }
                }
                catch (Exception pl3)
                {
                    oIs3PL = "N";
                    String oErr = pl3.Message;
                }
                if (String.IsNullOrWhiteSpace(oIs3PL))
                {
                    oIs3PL = "N";
                }
                if (oIs3PL == "N")
                {
                    oSendPreSo855 = "N";
                }
                // 03-11-2024 end
                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                {
                    bool process855 = getProcess855(request.CardCode); // 1-11-2021
                    /*listOf850Records = dbContext.Edi850HeaderRecords.Include("Details")
                                .Where(x => (
                                    (x.Processed == true
                                    || (x.Processed == false && process855 == true && oSendPreSo855 == "Y" )) // 03-12-2024
                                    && x.CardCode == request.CardCode
                                    && x.TrxPurpose != "01"
                                    && (x.Processed855 == false // 04-12-2018
                                    //|| x.Processed855DateTime > oLast855Date) // 02-22-2022
                                    || x.Orig855ProcessedDateTime > oLast855Date
                                    || (oSendPreSo855 == "Y" && x.SalesOrderKey <= 0 && x.Processed855 == false && x.ProcessedPreSo855 == false) // 03-11-2024
                                    )
                                    //&& x.SalesOrderKey > 0 // 4-12-2018
                                    && (x.CardCode == "LowesNet" // specify cardcode for Lowe's Net
                                    || x.CardCode.StartsWith("LowesNet") // 08-30-2019
                                    || x.CardCode == "Lowesnetest" // 07-13-2018
                                    || x.CardCode.StartsWith("TSCCL")  // 02-17-2019
                                    || x.CardCode.StartsWith("WAYFAIR") // 07-23-2019
                                    || process855 == true) // 1-11-2021 
                                    && (x.IgnoreTrxRequest != "Y") // 08-06-2022
                                    && (x.SalesOrderKey > 0
                                    || (oIs3PL == "Y" && oSendPreSo855 == "Y" && x.ProcessedPreSo855 == false  && x.Processed855 == false 
                                        && x.SalesOrderKey <= 0 && !(x.MessageText.Contains("Cancel")) && !(x.MessageText.Contains("Duplicate")))) // 03-11-2024
                                    )).ToList();
                     * */
                    listOf850Records = dbContext.Edi850HeaderRecords.Include("Details")
                               .Where(x => //x.Processed == true &&
                                    x.CardCode == request.CardCode
                                   && x.TrxPurpose != "01"
                                   && (x.Processed855 == false // 04-12-2018
                                                               //|| x.Processed855DateTime > oLast855Date) // 02-22-2022
                                   || x.Orig855ProcessedDateTime > oLast855Date) // 02-25-2022 )
                                                                                 //&& x.SalesOrderKey > 0 // 4-12-2018
                                   && (x.CardCode == "LowesNet" // specify cardcode for Lowe's Net
                                   || x.CardCode.StartsWith("LowesNet") // 08-30-2019
                                   || x.CardCode == "Lowesnetest" // 07-13-2018
                                   || x.CardCode.StartsWith("TSCCL")  // 02-17-2019
                                   || x.CardCode.StartsWith("WAYFAIR") // 07-23-2019
                                   || process855 == true) // 1-11-2021 
                                   && (x.IgnoreTrxRequest != "Y") // 08-06-2022
                                                                  // 03-11-2024 begin
                             && ((oSendPreSo855 == "Y" && oIs3PL == "Y" && ((x.SalesOrderKey <= 0 && x.Processed == false)
                             || (x.Processed == true && x.SalesOrderKey > 0 && x.NonSAPSO == "Y")) // 09-25-2024
                              || (x.SalesOrderKey > 0 && x.Processed == true)))).ToList();
                    // && x.SalesOrderKey > 0)).ToList();
                    // 03-11-2024 end                   

                    _logger.Debug("There are " + listOf850Records.Count + " 855 records to evaluate");

                    foreach (var record in listOf850Records)
                    {
                        //_logger.Debug("Processing 855 with 850 Key " + record.HeaderId + ", CardCode: " + record.CardCode);
                        //SOrder salesOrder = FindMatchingSOrder(dbContext, record, "855");
                        //SOrder salesOrder = FindMatchingSOrder855(dbContext, record); // 02-02-2021

                        // 03-11-2024 begin
                        // 09-25-2024 lrussell add logic to check both sales order key & ProcessedPre855
                        if (oSendPreSo855 == "Y" && oIs3PL == "Y"
                             && record.ProcessedPreSo855 == false) // 09-25-2024
                        {
                            if ((record.SalesOrderKey <= 0 && record.Processed == false)
                            || (record.NonSAPSO == "Y" && record.Processed == true && record.SalesOrderKey > 0))
                            {
                                SOrder oPreSalesOrder = new SOrder();
                                oPreSalesOrder.DocEntry = 0;
                                oPreSalesOrder.U_InfoW2Notes = "PreSo855";
                                listToProcess.Add(new Edi850WithSalesOrder(record, oPreSalesOrder));
                            }
                        }
                        else
                        {
                            // 03-11-2024 end                        
                            SOrder salesOrder = FindMatchingSOrder855(dbContext, record, oLast855Date, process855); // 02-02-2022
                            if (salesOrder != null &&
                                (String.IsNullOrWhiteSpace(record.NonSAPSO) || record.NonSAPSO == "N")) // 09-25-2024
                            {
                                //_logger.Debug("Found 855 line from sales order with key " + salesOrder.DocNum);
                                listToProcess.Add(new Edi850WithSalesOrder(record, salesOrder));
                            }
                            else
                            {
                                _logger.Error("855 sales order line not found for 850 record with key " + record.HeaderId);
                            }
                        } // 03-11-2024
                    }
                }

                if (listToProcess.Count == 0)
                {
                    response.Successful = true;
                    response.ErrorMessage = "No matching 855s found"; // 03-29-2022
                    _logger.Debug("Leaving Get855Records for " + oSBOCardCode); // 01-25-2023
                    return response;
                }
                //To do -Need to filter records, but not sure how to right now
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    sqlConnection.Open();

                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            if (iPreProcess855Record != null)
                            {
                                if (!iPreProcess855Record.OnPreProcess855Record(selectedRecord.SOrder, selectedRecord.Edi850HeaderRecord))
                                {
                                    continue;
                                }
                            }
                            // 03-11-2024 begin
                            if (selectedRecord.SOrder.U_InfoW2Notes == "PreSo855" && oIs3PL == "Y" && oSendPreSo855 == "Y")
                            {
                                Edi855HeaderRecord record = new Edi855HeaderRecord();
                                response.Edi855Records.Add(record);
                                record.CardCode = selectedRecord.Edi850HeaderRecord.CardCode;
                                if (selectedRecord.Edi850HeaderRecord.PurchaseOrderDate == null ||
                                    selectedRecord.Edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                                {
                                    record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.RecordDate;
                                }
                                else
                                {
                                    record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.PurchaseOrderDate;
                                }
                                record.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                                record.SalesOrder = "0";
                                record.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                                record.ConfirmationNo = 0;
                                if (selectedRecord.Edi850HeaderRecord.RequestedShipDate.HasValue && selectedRecord.Edi850HeaderRecord.RequestedShipDate.Value > DateTime.MinValue)
                                {
                                    record.AsnShipDate = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.RequestedShipDate);
                                }
                                else if (selectedRecord.Edi850HeaderRecord.ExpectedDeliveryDate.HasValue && selectedRecord.Edi850HeaderRecord.ExpectedDeliveryDate.Value > DateTime.MinValue)
                                {
                                    record.AsnShipDate = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.ExpectedDeliveryDate);
                                }
                                else
                                {
                                    record.AsnShipDate = DateTime.Now.AddDays(14);
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyName))
                                {
                                    record.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                                }
                                else
                                {
                                    record.ShipToName = selectedRecord.Edi850HeaderRecord.OrderBuyName;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyAddr1))
                                {
                                    record.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                                }
                                else
                                {
                                    record.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.OrderBuyAddr1;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyAddr2))
                                {
                                    record.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                                }
                                else
                                {
                                    record.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.OrderBuyAddr2;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyCity))
                                {
                                    record.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                                }
                                else
                                {
                                    record.ShipToCity = selectedRecord.Edi850HeaderRecord.OrderBuyCity;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyState))
                                {
                                    record.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                                }
                                else
                                {
                                    record.ShipToState = selectedRecord.Edi850HeaderRecord.OrderBuyState;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyZip))
                                {
                                    record.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                                }
                                else
                                {
                                    record.ShipToZip = selectedRecord.Edi850HeaderRecord.OrderBuyZip;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyCountryCd))
                                {
                                    record.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                                }
                                else
                                {
                                    record.ShipToCountry = selectedRecord.Edi850HeaderRecord.OrderBuyCountryCd;
                                }
                                record.PaymentMethod = selectedRecord.Edi850HeaderRecord.PaymentMethod;
                                record.ShipToStoreLocation = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation;
                                record.ShipToAttention = selectedRecord.Edi850HeaderRecord.ShipToAttention;
                                record.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                                foreach (Edi850DetailRecord detailRec in selectedRecord.Edi850HeaderRecord.Details)
                                {
                                    Edi855DetailRecord detail855Record = new Edi855DetailRecord();
                                    record.Details.Add(detail855Record);
                                    detail855Record.OrderNumber = "0";
                                    detail855Record.VendorItemCode = detailRec.VendorItemCode;
                                    // 06-14-2024 begin
                                    if (BPList.Contains(oSBOCardCode))
                                    {
                                        detail855Record.VendorItemCode = detail855Record.VendorItemCode.ToUpper();
                                    }
                                    // 06-14-2024 end
                                    detail855Record.BuyerItemCode = detailRec.BuyerItemCode;
                                    // 06-14-2024 begin
                                    if (BPList.Contains(oSBOCardCode))
                                    {
                                        detail855Record.BuyerItemCode = detail855Record.BuyerItemCode.ToUpper();
                                    }
                                    // 06-14-2024 end
                                    detail855Record.ItemUPC = detailRec.ItemUPC;
                                    try
                                    {
                                        detail855Record.UnitPrice = Convert.ToDecimal(detailRec.UnitPrice);
                                    }
                                    catch
                                    {
                                        detail855Record.UnitPrice = Convert.ToDecimal("0");
                                    }
                                    // remove quotes from item description
                                    string oItmDesc = "";
                                    try
                                    {
                                        oItmDesc = detailRec.ItemDescription;
                                    }
                                    catch
                                    {
                                        oItmDesc = "";
                                    }
                                    if (!String.IsNullOrWhiteSpace(oItmDesc))
                                    {
                                        oItmDesc = oItmDesc.Replace('"', ' ');
                                        oItmDesc = oItmDesc.Replace("  ", " ");
                                        oItmDesc = oItmDesc.Trim();
                                        if (oItmDesc.Length > 80)
                                        {
                                            oItmDesc = oItmDesc.Substring(0, 80);
                                        }
                                    }
                                    else
                                    {
                                        oItmDesc = "";
                                    }
                                    detail855Record.ItemDescription = oItmDesc;
                                    detail855Record.Quantity = Convert.ToDouble(detailRec.Quantity);
                                    detail855Record.UnitOfMeasure = detailRec.UnitOfMeasure;
                                    if (String.IsNullOrWhiteSpace(detail855Record.UnitOfMeasure))
                                    {
                                        detail855Record.UnitOfMeasure = "EA";
                                    }
                                    if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("WAYFAIR"))
                                    {
                                        detail855Record.ExpectedLnDeliveryDate = detailRec.ExpectedLnDeliveryDate;
                                        if (detail855Record.ExpectedLnDeliveryDate < DateTime.Today)
                                        {
                                            detail855Record.ExpectedLnDeliveryDate = DateTime.Today.AddDays(3);
                                        }
                                    }
                                }
                                if (selectedRecord.Edi850HeaderRecord.ToString().Trim().Length == 0)
                                {
                                    selectedRecord.Edi850HeaderRecord.ProcessedPreSo855DateTime = DateTime.Now;
                                    // 03-26-2025 begin
                                    if (selectedRecord.Edi850HeaderRecord.CardCode.ToUpper().StartsWith("INDOC"))
                                    {
                                        selectedRecord.Edi850HeaderRecord.Processed855DateTime = selectedRecord.Edi850HeaderRecord.ProcessedPreSo855DateTime;
                                    }
                                    // 03-26-2025 end
                                    // 02-12-2026 lrussell restore missing code & correct closing bracket
                                    // begin
                                }
                            }
                            else
                            {
                                // 02-12-2026 lrussell end
                                // 03-11-2024 end
                                if ((selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSCCL") && selectedRecord.SOrder.U_InfoOrdStatus == "RD")
                                    || !(selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSCCL")))
                                {  // 02-21-2019

                                    if ((selectedRecord.SOrder.U_InfoOrdStatus != selectedRecord.Edi850HeaderRecord.Last855Status
                                      //     || selectedRecord.Edi850HeaderRecord.Processed855DateTime > oLast855Date) // 02-22-2022
                                      || selectedRecord.Edi850HeaderRecord.Orig855ProcessedDateTime > oLast855Date) // 02-25-2022
                                        && selectedRecord.SOrder.DocNum > 0) // 04-12-2018
                                    {  // 03-19-2018
                                       // 02-12-2026 lrussell un-comment creation of new 855 header rec
                                        Edi855HeaderRecord record = new Edi855HeaderRecord();
                                        record = new Edi855HeaderRecord();
                                        response.Edi855Records.Add(record);

                                        //record.BuyerName = selectedRecord.Edi850HeaderRecord.BuyerName;
                                        record.CardCode = selectedRecord.Edi850HeaderRecord.CardCode;

                                        // 08-28-2019 begin
                                        if (selectedRecord.Edi850HeaderRecord.PurchaseOrderDate == null ||
                                            selectedRecord.Edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                                        {
                                            record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.RecordDate;
                                        }
                                        else
                                        { // 08-28-2019 end
                                            record.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.PurchaseOrderDate;
                                        } // 08-28-2019
                                        record.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                                        record.SalesOrder = selectedRecord.SOrder.DocNum.ToString(); // 02-09-2022
                                        record.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                                        record.ConfirmationNo = selectedRecord.SOrder.DocNum;
                                        record.AsnShipDate = selectedRecord.SOrder.DocDueDate;
                                        record.TotalDue = Convert.ToDouble(selectedRecord.SOrder.DocTotal); // 07-19-2019
                                        // 07-03-2019 begin
                                        //String oShipToAddress = selectedRecord.SOrder.Address2;
                                        //String[] oAddress = oShipToAddress.Split('\r');
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyName))
                                        {
                                            record.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                                        }
                                        else
                                        {
                                            record.ShipToName = selectedRecord.Edi850HeaderRecord.OrderBuyName;
                                        }
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyAddr1))
                                        {
                                            record.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                                        }
                                        else
                                        {
                                            record.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.OrderBuyAddr1;
                                        }
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyAddr2))
                                        {
                                            record.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                                        }
                                        else
                                        {
                                            record.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.OrderBuyAddr2;
                                        }
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyCity))
                                        {
                                            record.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                                        }
                                        else
                                        {
                                            record.ShipToCity = selectedRecord.Edi850HeaderRecord.OrderBuyCity;
                                        }
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyState))
                                        {
                                            record.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                                        }
                                        else
                                        {
                                            record.ShipToState = selectedRecord.Edi850HeaderRecord.OrderBuyState;
                                        }
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyZip))
                                        {
                                            record.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                                        }
                                        else
                                        {
                                            record.ShipToZip = selectedRecord.Edi850HeaderRecord.OrderBuyZip;
                                        }
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.OrderBuyCountryCd))
                                        {
                                            record.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                                        }
                                        else
                                        {
                                            record.ShipToCountry = selectedRecord.Edi850HeaderRecord.OrderBuyCountryCd;
                                        }
                                        record.PaymentMethod = selectedRecord.Edi850HeaderRecord.PaymentMethod;
                                        record.ShipToStoreLocation = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation;
                                        record.ShipToAttention = selectedRecord.Edi850HeaderRecord.ShipToAttention;
                                        record.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;

                                        // 07-03-2019 end
                                        foreach (SOLine soLine in selectedRecord.SOrder.SOLines)
                                        {
                                            //   string oAltVendorItem = getAltVendorItem(oConnectionName, selectedRecord.SOrder.DocEntry, soLine);

                                            if (soLine.TreeType == "N") //|| soLine.TreeType == "S") removed 02-15-2018
                                            {
                                                Edi855DetailRecord detail855Record = new Edi855DetailRecord();
                                                record.Details.Add(detail855Record);
                                                detail855Record.OrderNumber = Convert.ToString(selectedRecord.SOrder.DocNum); // 07-03-2019
                                                //if (String.IsNullOrWhiteSpace(oAltVendorItem))
                                                //{
                                                detail855Record.VendorItemCode = soLine.ItemCode;
                                                /* }
                                                 else
                                                 {
                                                     detail855Record.VendorItemCode = oAltVendorItem;
                                                 }*/

                                                detail855Record.LineNumber = (Int32)soLine.LineNumber850;
                                                if (String.IsNullOrWhiteSpace(soLine.SubCatNum) || soLine.SubCatNum.Trim().Length == 0)
                                                {
                                                    detail855Record.BuyerItemCode = soLine.ItemCode;
                                                }
                                                else
                                                {
                                                    detail855Record.BuyerItemCode = soLine.SubCatNum;
                                                }
                                                // 02-14-2019 begin
                                                if (!(String.IsNullOrWhiteSpace(soLine.U_InfoW2ItemUPC)))
                                                {
                                                    detail855Record.ItemUPC = soLine.U_InfoW2ItemUPC;
                                                }
                                                detail855Record.UnitPrice = soLine.Price;
                                                // 02-14-2019 end
                                                // 10-14-2021 begin
                                                // remove quotes from item description
                                                //detail855Record.ItemDescription = soLine.Dscription;
                                                string oItmDesc = "";
                                                try
                                                {
                                                    oItmDesc = soLine.Dscription;
                                                }
                                                catch
                                                {
                                                    oItmDesc = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oItmDesc))
                                                {
                                                    oItmDesc = oItmDesc.Replace('"', ' ');
                                                    // 10-22-2021 begin
                                                    oItmDesc = oItmDesc.Replace("  ", " ");
                                                    oItmDesc = oItmDesc.Trim();
                                                    if (oItmDesc.Length > 80)
                                                    {
                                                        oItmDesc = oItmDesc.Substring(0, 80);
                                                    }
                                                    // 10-22-2021 end
                                                }
                                                else
                                                {
                                                    oItmDesc = "";
                                                }
                                                detail855Record.ItemDescription = oItmDesc;
                                                // 10-14-2021 end

                                                detail855Record.Quantity = Convert.ToDouble(soLine.Quantity);
                                                detail855Record.UnitOfMeasure = soLine.UomCode;
                                                if (String.IsNullOrWhiteSpace(detail855Record.UnitOfMeasure)
                                                    || detail855Record.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    detail855Record.UnitOfMeasure = "EA";
                                                }
                                                if (String.IsNullOrWhiteSpace(soLine.LineItemStatus) || soLine.LineItemStatus.Trim().Length == 0)
                                                {
                                                    // 03-19-2018 begin
                                                    if (!String.IsNullOrWhiteSpace(selectedRecord.SOrder.U_InfoOrdStatus))
                                                    {
                                                        detail855Record.Item855Status = selectedRecord.SOrder.U_InfoOrdStatus;
                                                    }
                                                    else
                                                    {
                                                        detail855Record.Item855Status = "IA";
                                                    }
                                                    // 03-19-2018 end
                                                }
                                                else
                                                {
                                                    detail855Record.Item855Status = soLine.LineItemStatus;
                                                    if (!(String.IsNullOrWhiteSpace(soLine.LineItemStatus))
                                                        && (soLine.LineItemStatus == "IB"))
                                                    {
                                                        if (soLine.ExpectedLnDeliveryDate != null)
                                                        {
                                                            detail855Record.ExpectedLnDeliveryDate = soLine.ExpectedLnDeliveryDate;
                                                        }
                                                        else if (!String.IsNullOrWhiteSpace(soLine.LineItemRsnCd))
                                                        {
                                                            detail855Record.ItemReasonCode855 = soLine.LineItemRsnCd;
                                                        }
                                                        else
                                                        {
                                                            detail855Record.ItemReasonCode855 = "Out of Stock";
                                                        }
                                                    }
                                                    // 03-19-2018 begin
                                                    else if (String.IsNullOrWhiteSpace(soLine.LineItemStatus))
                                                    {
                                                        detail855Record.Item855Status = selectedRecord.SOrder.U_InfoOrdStatus;
                                                    }
                                                    // 03-19-2018 end
                                                }
                                                // 07-24-2019  begin
                                                if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("WAYFAIR"))
                                                {
                                                    detail855Record.ExpectedLnDeliveryDate = soLine.ExpectedLnDeliveryDate;
                                                    if (detail855Record.ExpectedLnDeliveryDate < DateTime.Today)
                                                    {
                                                        detail855Record.ExpectedLnDeliveryDate = DateTime.Today.AddDays(3);
                                                    }
                                                }
                                                // 07-24-2019 end
                                            } // 02-15-2018 begin
                                            else if (soLine.TreeType == "S")
                                            {
                                                Edi855DetailRecord detail855Record = new Edi855DetailRecord();
                                                record.Details.Add(detail855Record);
                                                // if (String.IsNullOrWhiteSpace(oAltVendorItem))
                                                //{
                                                detail855Record.VendorItemCode = soLine.ItemCode;
                                                /* }
                                                 else
                                                 {
                                                     detail855Record.VendorItemCode = oAltVendorItem;
                                                 }*/
                                                // 06-14-2024 begin// 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail855Record.VendorItemCode = detail855Record.VendorItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                                detail855Record.LineNumber = (Int32)soLine.LineNumber850;
                                                if (String.IsNullOrWhiteSpace(soLine.SubCatNum) || soLine.SubCatNum.Trim().Length == 0)
                                                {
                                                    detail855Record.BuyerItemCode = soLine.ItemCode;
                                                }
                                                else
                                                {
                                                    detail855Record.BuyerItemCode = soLine.SubCatNum;
                                                }
                                                // 06-14-2024 begin// 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail855Record.BuyerItemCode = detail855Record.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                                // 02-14-2019 begin
                                                if (!(String.IsNullOrWhiteSpace(soLine.U_InfoW2ItemUPC)))
                                                {
                                                    detail855Record.ItemUPC = soLine.U_InfoW2ItemUPC;
                                                }
                                                detail855Record.UnitPrice = soLine.Price;
                                                // 02-14-2019 end

                                                // 10-14-2021 begin
                                                // remove quotes from item description
                                                // detail855Record.ItemDescription = soLine.Dscription;
                                                string oItmDesc = "";
                                                try
                                                {
                                                    oItmDesc = soLine.Dscription;
                                                }
                                                catch
                                                {
                                                    oItmDesc = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oItmDesc))
                                                {
                                                    oItmDesc = oItmDesc.Replace('"', ' ');
                                                    // 10-22-2021 begin
                                                    oItmDesc = oItmDesc.Replace("  ", " ");
                                                    oItmDesc = oItmDesc.Trim();
                                                    if (oItmDesc.Length > 80)
                                                    {
                                                        oItmDesc = oItmDesc.Substring(0, 80);
                                                    }
                                                    // 10-22-2021 end
                                                }
                                                else
                                                {
                                                    oItmDesc = "";
                                                }
                                                detail855Record.ItemDescription = oItmDesc;
                                                // 10-14-2021 end
                                                detail855Record.Quantity = Convert.ToDouble(soLine.Quantity);
                                                detail855Record.UnitOfMeasure = soLine.UomCode;
                                                if (String.IsNullOrWhiteSpace(detail855Record.UnitOfMeasure)
                                                    || detail855Record.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                                {
                                                    detail855Record.UnitOfMeasure = "EA";
                                                }
                                                // 03-11-2024 begin
                                                if (selectedRecord.Edi850HeaderRecord.Processed == false
                                                    && oSendPreSo855 == "Y"
                                                    && selectedRecord.Edi850HeaderRecord.SalesOrderKey <= 0 && oIs3PL == "Y")
                                                {
                                                    detail855Record.Item855Status = "NA";
                                                }
                                                else
                                                    // 03-11-2024 end
                                                    if (String.IsNullOrWhiteSpace(soLine.LineItemStatus) || soLine.LineItemStatus.Trim().Length == 0)
                                                    {
                                                        // 03-19-2018 begin
                                                        if (!String.IsNullOrWhiteSpace(selectedRecord.SOrder.U_InfoOrdStatus))
                                                        {
                                                            detail855Record.Item855Status = selectedRecord.SOrder.U_InfoOrdStatus;
                                                        }
                                                        else
                                                        {
                                                            detail855Record.Item855Status = "IA";
                                                        }
                                                        // 03-19-2018 end
                                                    }
                                                    else
                                                    {
                                                        detail855Record.Item855Status = soLine.LineItemStatus;
                                                        if (!(String.IsNullOrWhiteSpace(soLine.LineItemStatus))
                                                            && (soLine.LineItemStatus == "IB"))
                                                        {
                                                            if (soLine.ExpectedLnDeliveryDate != null)
                                                            {
                                                                detail855Record.ExpectedLnDeliveryDate = soLine.ExpectedLnDeliveryDate;
                                                            }
                                                            else if (!String.IsNullOrWhiteSpace(soLine.LineItemRsnCd))
                                                            {
                                                                detail855Record.ItemReasonCode855 = soLine.LineItemRsnCd;
                                                            }
                                                            else
                                                            {
                                                                detail855Record.ItemReasonCode855 = "Out of Stock";
                                                            }
                                                        }
                                                    }
                                                // 02-15-2018 begin
                                                if (String.IsNullOrWhiteSpace(selectedRecord.SOrder.U_InfoOrdStatus))
                                                {
                                                    selectedRecord.SOrder.U_InfoOrdStatus = detail855Record.Item855Status;
                                                }
                                                else if (selectedRecord.SOrder.U_InfoOrdStatus == "IA" && detail855Record.Item855Status == "IB")
                                                {
                                                    selectedRecord.SOrder.U_InfoOrdStatus = detail855Record.Item855Status;
                                                }
                                                // 02-15-2018 end
                                                // 07-24-2019  begin
                                                if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("WAYFAIR"))
                                                {
                                                    detail855Record.ExpectedLnDeliveryDate = soLine.ExpectedLnDeliveryDate;
                                                    if (detail855Record.ExpectedLnDeliveryDate < DateTime.Today)
                                                    {
                                                        detail855Record.ExpectedLnDeliveryDate = DateTime.Today.AddDays(3);
                                                    }
                                                }
                                            }
                                            // 02-15-2018 end

                                            // 07-24-2019 end

                                        }
                                        // 02-25-2022 begin
                                        /*
                                        // 02-22-2022 begin
                                        DateTime oProcessDT = DateTime.Now;
                                        record.TrxDateTime = oProcessDT.ToString("MM-dd-yyyy HH:mm:ss.mmm");
                                        selectedRecord.Edi850HeaderRecord.Processed855DateTime = oProcessDT;
                                        // 02-22-2022 end
                                        */
                                        // DateTime oProcessDT = DateTime.Now;
                                        if (selectedRecord.Edi850HeaderRecord.Orig855ProcessedDateTime.ToString().Trim().Length == 0)
                                        {
                                            selectedRecord.Edi850HeaderRecord.Orig855ProcessedDateTime = DateTime.Now;
                                        }
                                        //oProcessDT = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.Orig855ProcessedDateTime);
                                        //record.TrxDateTime = oProcessDT.ToString("MM-dd-yyyy HH:MM:ss.mmm");
                                        // 03-11-2024 begin
                                        if (selectedRecord.Edi850HeaderRecord.Processed == false
                                                    && oSendPreSo855 == "Y"
                                                    && selectedRecord.Edi850HeaderRecord.SalesOrderKey <= 0 && oIs3PL == "Y")
                                        {
                                            record.TrxDateTime = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.ReceivedDateTime);
                                        }
                                        else
                                        {
                                            // 03-11-2024 end
                                            record.TrxDateTime = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.Orig855ProcessedDateTime); // 03-10-2022
                                        } // 03-11-2024
                                        // 02-25-2022 end
                                        if (iPostProcess855Record != null)
                                        {
                                            iPostProcess855Record.OnPostProcess855Record(selectedRecord.SOrder, selectedRecord.Edi850HeaderRecord, record);
                                        }
                                    } // 03-19-2018
                                } // 02-21-2019 end
                            } // 03-11-2024
                        }
                        // 02-12-2026 lrussell remove extra brackets
                        //     }
                        //}
                        // 02-12-2026 lrussell end
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                            _logger.Error(ex.Message);
                        }
                    }
                    sqlConnection.Close();
                }

                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    sqlConnection.Open();

                    DateTime now = DateTime.Now;
                    // DateTime svNow = now; // 02-22-2022
                    if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                    {
                        oConnectionName = "WebApiDbContext";
                    }

                    using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                    {
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate == null)
                            {
                                throw new WebApiException("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                            }
                            if (selectedRecord.IsError)
                            {
                                recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                recordToUpdate.Processed855 = false;
                            }
                            else
                            {
                                //recordToUpdate.Processed855 = true; // 03-11-2024
                                // 01-25-2023 begin
                                /*
                                // 02-22-2022 begin
                                try
                                {
                                    now = Convert.ToDateTime(selectedRecord.Edi850HeaderRecord.Processed855DateTime.ToString());
                                }
                                catch (Exception d)
                                {
                                    now = svNow;
                                }
                                // 02-22-2022 end
                                */
                                // 01-25-2022 end
                                // 03-03-2022 begin
                                DateTime oProcessDT = DateTime.Now;
                                // 03-11-2024 begin
                                if ((recordToUpdate.SalesOrderKey == 0 || recordToUpdate.NonSAPSO == "Y") && oIs3PL == "Y" && oSendPreSo855 == "Y")
                                {
                                    if (recordToUpdate.OrigProcessedPreSo855DateTime.ToString().Trim().Length == 0)
                                    {
                                        recordToUpdate.OrigProcessedPreSo855DateTime = DateTime.Now;
                                    }
                                    recordToUpdate.ProcessedPreSo855DateTime = now;
                                    recordToUpdate.ProcessedPreSo855 = true;
                                    // 03-25-2025 begin
                                    if (recordToUpdate.CardCode.ToUpper().StartsWith("INDOC"))
                                    {
                                        recordToUpdate.Processed855 = true;
                                        recordToUpdate.Processed855DateTime = recordToUpdate.ProcessedPreSo855DateTime;
                                        if (recordToUpdate.Orig855ProcessedDateTime.ToString().Trim().Length == 0)
                                        {
                                            recordToUpdate.Orig855ProcessedDateTime = recordToUpdate.OrigProcessedPreSo855DateTime;
                                        }

                                    }
                                    // 03-25-2025 end
                                }
                                else
                                {
                                    recordToUpdate.Processed855 = true;
                                    // 03-11-2024 end
                                    if (recordToUpdate.Orig855ProcessedDateTime.ToString().Trim().Length == 0)
                                    {
                                        recordToUpdate.Orig855ProcessedDateTime = DateTime.Now;
                                    }
                                    // 03-03-2022 end                               
                                    recordToUpdate.Processed855DateTime = now;
                                } // 03-11-2024

                                recordToUpdate.ErrorMessage = String.Empty;
                            }
                            // 03-11-2024 begin
                            if (recordToUpdate.SalesOrderKey == 0 && oIs3PL == "Y" && oSendPreSo855 == "Y"
                                && ((recordToUpdate.ErrorMessage.ToUpper().Contains("REJECTED")) // 09-25-2024
                                || (recordToUpdate.ErrorMessage.ToUpper().Contains("INVALID"))) // 09-25-2024
                            )
                            {
                                string oMessage = "don't need to update pre So 855";
                            }
                            else
                            {
                                // 03-11-2024 end
                                string oOrdStatus = selectedRecord.SOrder.U_InfoOrdStatus;
                                DateTime oCurrentDateTime = DateTime.Now;
                                DateTime oExpDelDate = oCurrentDateTime;
                                DateTime oExpShDate = oCurrentDateTime;

                                if (oOrdStatus != null)
                                {
                                    recordToUpdate.Last855Status = oOrdStatus;
                                }
                                if (oExpDelDate != null && oCurrentDateTime != oExpDelDate)
                                {
                                    recordToUpdate.ExpectedDeliveryDate = oExpDelDate;
                                }
                                if (oExpShDate != null && oExpDelDate != oCurrentDateTime)
                                {
                                    recordToUpdate.ExpectedShipDate = oExpShDate;
                                }
                            } // 03-11-2024 end
                        }
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi850DetailRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate != null && !selectedRecord.IsError)
                            {
                                // 03-11-2024 begin
                                if (recordToUpdate.Edi850HeaderRecord.SalesOrderKey > 0 && oSendPreSo855 == "N")
                                {
                                    // 03-11-2024 end
                                    string oLnStatus = null;
                                    string oLnReason = null;
                                    DateTime oCurrentDateTime = DateTime.Now;
                                    DateTime oExpDelDate = oCurrentDateTime;
                                    DateTime oExpShDate = oCurrentDateTime;
                                    using (SqlCommand command = new SqlCommand("select top 1 r1.[U_InfoItmStatus], r1.[U_InfoItmRsn], r1.[U_InfoW2ELnDelDate], r1.[U_InfoW2ELnShpDate] from dbo.RDR1 r1 WITH(NOLOCK) where r1.DocEntry = " + selectedRecord.SOrder.DocEntry + " and coalesce(r1.[U_InfoW2LNo],0) = " + recordToUpdate.LineNumber, sqlConnection))
                                    {
                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            if (reader.Read())
                                            {
                                                try
                                                {
                                                    oLnStatus = (String)reader[0];
                                                }
                                                catch
                                                {
                                                    oLnStatus = "";
                                                }
                                                try
                                                {
                                                    oLnReason = (String)reader[1];
                                                }
                                                catch
                                                {
                                                    oLnReason = "";
                                                }
                                                try
                                                {
                                                    oExpDelDate = (DateTime)reader[2];
                                                }
                                                catch
                                                {
                                                    oExpDelDate = oCurrentDateTime;
                                                }
                                                try
                                                {
                                                    oExpShDate = (DateTime)reader[3];
                                                }
                                                catch
                                                {
                                                    oExpShDate = oCurrentDateTime;
                                                }
                                            }
                                        }
                                    }
                                    if (oExpDelDate != null && oExpDelDate != oCurrentDateTime)
                                    {
                                        recordToUpdate.ExpectedLnDeliveryDate = oExpDelDate;
                                    }
                                    if (oExpShDate != null && oExpShDate != oCurrentDateTime)
                                    {
                                        recordToUpdate.ExpectedLnShipDate = oExpShDate;
                                    }
                                } // 03-11-2024

                            }
                        }

                        dbContext.SaveChanges();
                    }
                    sqlConnection.Close();
                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                response.ErrorMessage = ex.Message;
                _logger.Error(ex.Message);
            }
            _logger.Debug("Returning the 855 response object which contains " + response.Edi855Records.Count + " 855s");
            //_logger.Debug("{@Get855RecordsResponse}", response);
            _logger.Debug("Leaving Get855Records for " + request.CardCode);
            return response;
        }

        private IPreProcess855Record Get855PreProcess(String cardCode)
        {
            String preProcess855Record = ConfigurationManager.AppSettings["PreProcess855Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess855Record))
            {
                preProcess855Record = ConfigurationManager.AppSettings["PreProcess855Record"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess855Record))
            {
                try
                {
                    IPreProcess855Record iPreProcess855Record = (IPreProcess855Record)Activator.CreateInstance(Type.GetType(preProcess855Record, true));
                    return iPreProcess855Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess855Record", ex);
                    _logger.Error("Could not instantiate IPreProcess855Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess855Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private IPostProcess855Record Get855PostProcess(String cardCode)
        {
            String postProcess855Record = ConfigurationManager.AppSettings["PostProcess855Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess855Record))
            {
                postProcess855Record = ConfigurationManager.AppSettings["PostProcess855Record"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess855Record))
            {
                try
                {
                    IPostProcess855Record iPostProcess855Record = (IPostProcess855Record)Activator.CreateInstance(Type.GetType(postProcess855Record, true));
                    return iPostProcess855Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess855Record", ex);
                    _logger.Debug("Error not instantiate IPostProcess855Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess855Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        // 01-17-2018 end

        // 01-19-2018 begin
        [HttpPost]
        public Get846RecordsResponse Get846Records(Get846RecordsRequest request)
        {
            int count846Rec = 0; // 10-15-2019

            string oSBOCardCode = getSBOCardCode(request.CardCode);
            // 06-13-2024 begin
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-13-2024 end

            IPreProcess846Record iPreProcess846Record = null;
            IPostProcess846Record iPostProcess846Record = null;
            List<Edi850WithSalesOrder> listToProcess = new List<Edi850WithSalesOrder>();
            _logger.Debug("Entering Get846Records for " + request.CardCode + " : " + request.CardCode);
            //_logger.Debug("Processing the following request object:");
            Get846RecordsResponse response = new Get846RecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get846Records";
                response.Successful = false;
                return response;
            }
            try
            {
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {
                    iPreProcess846Record = Get846PreProcess(request.CardCode);
                    iPostProcess846Record = Get846PostProcess(request.CardCode);
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess846Record = Get846PreProcess(oSBOCardCode);
                    iPostProcess846Record = Get846PostProcess(oSBOCardCode);
                }

                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }

                //To do -Need to filter records, but not sure how to right now
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }

                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    try
                    {
                        sqlConnection.Open();
                        /*string oItmQuery = "select (select top 1 VendorNumber from InfocusEdi850HeaderRecord where CardCode = '" +
                                         request.CardCode.Trim() + "') as VendorId, " +
                                           "Substitute as BuyerItemCode, t0.ItemCode as VendorItemCode, t1.OnHand, t1.OnOrder, t1.IsCommited, " +
                                           "t1.AvgPrice, t1.itemName " +
                                            "from OSCN t0 left join OITM t1 on t0.ItemCode = t1.ItemCode " +
                                            "where t0.CardCode = '" + oSBOCardCode.Trim() + "'";
                                            */
                        // 03-34-2022 begin
                        //string oItmQuery = "select coalesce((select top 1 VendorNumber from InfocusEdi850HeaderRecord where CardCode = '" +
                        //               request.CardCode.Trim() + "'),'') as VendorId," +
                        string oItmQuery = "select coalesce(t1.DefaultVendorNo,'') as VendorId, " + // 03-23-2022 end
                                        "BuyerItemCode, VendorItemCode, coalesce(AltVendItem,'') AltVendItem, PurchaserItem, OnHand, OnOrder, Available, " +
                                        " Commited, Price, ItemName, CardName, BarCode, QtyQualifier, NewItem, Discontinued, PoQty, NextAvailDt, UOM " +
                                        "from Infocus_EDI_846 t0  WITH(NOLOCK) " +
                                        "left join InfocusEDI.dbo.WebApiDbContext t1  WITH(NOLOCK) on t0.CardCode = t1.SBOCardCode collate SQL_Latin1_General_CP1_CI_AS  " + // 03-23-2022
                                        "and Upper(IsNUll(t1.IdType,'LIVE')) != 'TEST' " + // 03-23-2022
                                        " where t0.CardCode = '" + oSBOCardCode.Trim() + "'";
                        using (SqlCommand command = new SqlCommand(oItmQuery, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                                {
                                    if (reader.HasRows)
                                    {
                                        int oRecordNo = 0;
                                        Edi846HeaderRecord record = new Edi846HeaderRecord();
                                        while (reader.Read())
                                        {
                                            if (oRecordNo == 0)
                                            {
                                                response.Edi846Records.Add(record);
                                                record.CardCode = request.CardCode;
                                                record.InventoryDate = DateTime.Today;
                                                if (!String.IsNullOrWhiteSpace((String)reader["VendorId"]))
                                                {
                                                    record.VendorNumber = (String)reader["VendorId"];
                                                }
                                                DateTime oToday = DateTime.Now;
                                                string oKey = oToday.ToString("yyyyMMddHHmm");
                                                record.ReportId = oKey;

                                                oRecordNo = oRecordNo + 1;
                                            }
                                            Edi846DetailRecord detail846Record = new Edi846DetailRecord();
                                            record.Details.Add(detail846Record);
                                            string altVendorItem = "";
                                            try
                                            {
                                                altVendorItem = (String)reader["AltVendItem"];
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    altVendorItem = altVendorItem.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            catch
                                            {
                                                altVendorItem = "";
                                            } // 02-21-2018 end
                                            // 09-24-2019 begin
                                            string purchaserItem = "";
                                            try
                                            {
                                                purchaserItem = (String)reader["PurchaserItem"];
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    purchaserItem = purchaserItem.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            catch
                                            {
                                                purchaserItem = "";
                                            }
                                            if (request.CardCode.StartsWith("HDCL") && purchaserItem.Trim().Length > 0)
                                            {
                                                detail846Record.BuyerItemCode = purchaserItem.Trim();
                                            }
                                            else
                                            {
                                                // 09-24-2019 end
                                                detail846Record.BuyerItemCode = (String)reader["BuyerItemCode"];
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846Record.BuyerItemCode = detail846Record.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            } // 09-24-2019 
                                            if (String.IsNullOrWhiteSpace(altVendorItem))
                                            {  // 02-21-2018
                                                detail846Record.VendorItemCode = (String)reader["VendorItemCode"];
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846Record.VendorItemCode = detail846Record.VendorItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            else
                                            {  // 02-21-2018 begin
                                                detail846Record.VendorItemCode = altVendorItem;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846Record.VendorItemCode = detail846Record.VendorItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            if (String.IsNullOrWhiteSpace(detail846Record.BuyerItemCode))
                                            {
                                                detail846Record.BuyerItemCode = detail846Record.VendorItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846Record.BuyerItemCode = detail846Record.VendorItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            // 10-10-2020 mod no longer needed 
                                            /*// 10-19-2020 begin
                                            if (detail846Record.VendorItemCode == "200105" &&
                                                detail846Record.BuyerItemCode == "1291827" &&
                                                oSBOCardCode.Trim() == "LOWES")
                                            {
                                                detail846Record.VendorItemCode = "200105P";
                                            }
                                            // 10-19-2020 end*/
                                            // 10-15-2019 begin
                                            try
                                            {
                                                string itmUOM = (String)reader["UOM"];
                                                if (String.IsNullOrWhiteSpace(itmUOM))
                                                {
                                                    detail846Record.UOM = "EA";
                                                }
                                                else
                                                {
                                                    detail846Record.UOM = itmUOM;
                                                }
                                            }
                                            catch
                                            {
                                                detail846Record.UOM = "EA";
                                            }
                                            // 10-15-2019 end
                                            try
                                            {
                                                detail846Record.ItemDiscontinued = (String)reader["Discontinued"];
                                                if (String.IsNullOrWhiteSpace(detail846Record.ItemDiscontinued))
                                                {
                                                    detail846Record.ItemDiscontinued = "N";
                                                }
                                            }
                                            catch
                                            {
                                                detail846Record.ItemDiscontinued = "N";
                                            }
                                            // 07-18-2019 end
                                            try
                                            {
                                                detail846Record.Quantity1Qualifier = (String)reader["QtyQualifier"];
                                            }
                                            catch
                                            {
                                                detail846Record.Quantity1Qualifier = "33";
                                            }
                                            decimal oOnHand = 0;
                                            try
                                            {
                                                oOnHand = (decimal)reader["OnHand"];
                                            }
                                            catch (Exception e3)
                                            {
                                                oOnHand = 0;
                                                string oErrMesg = e3.Message;
                                            }
                                            decimal oCommitted = 0;
                                            try
                                            {
                                                oCommitted = (decimal)reader["Commited"];
                                            }
                                            catch (Exception e4)
                                            {
                                                oCommitted = 0;
                                                string oErrMesg = e4.Message;
                                                _logger.Debug("Error getting OnHand qty =>" + e4.Message); // 09-30-2022                                     
                                            }
                                            decimal oOrdered = 0;
                                            try
                                            {
                                                // 10-03-2022 begin
                                                string oValue = reader["OnOrder"].ToString();
                                                if (String.IsNullOrWhiteSpace(oValue))
                                                {
                                                    oValue = "0.00";
                                                }
                                                //oOrdered = (decimal)reader["OnOrder"];
                                                oOrdered = Convert.ToDecimal(oValue);
                                                // 10-03-2022 end
                                            }
                                            catch (Exception e2)
                                            {
                                                oOrdered = 0;
                                                string oErrMesg = e2.Message;
                                                _logger.Debug("Error getting  ordered qty =>" + e2.Message); // 09-30-2022                                     
                                            }
                                            decimal oAvail = oOnHand + oOrdered;
                                            oAvail = oAvail - oCommitted;
                                            if (oAvail < 0 || (oOnHand + oOrdered) < oCommitted)
                                            {
                                                oAvail = 0;
                                            }
                                            // 09-30-2022 begin
                                            decimal oAvailable = 0;
                                            try
                                            {
                                                // 10-03-2022 begin
                                                //oAvailable = (decimal)reader["Available"];
                                                string oValue = reader["Available"].ToString();
                                                // _logger.Debug("Available: " + oValue);
                                                oAvailable = Convert.ToDecimal(oValue);
                                                // 10-03-2022 end
                                            }
                                            catch (Exception av)
                                            {
                                                oAvailable = Convert.ToDecimal("0.00");
                                                string oErrMesg = av.Message;
                                                _logger.Debug("Error getting available qty =>" + av.Message);
                                            }
                                            // 10-03-2022 begin
                                            if (oAvailable < 0 || (oOnHand + oOrdered) < oCommitted)
                                            {
                                                oAvailable = 0;
                                            }
                                            // 10-03-2022 end
                                            if (oAvailable > 0)
                                            {
                                                detail846Record.Quantity1 = Convert.ToDouble(oAvailable);
                                            }
                                            else
                                            {
                                                // 09-30-2022 end
                                                detail846Record.Quantity1 = Convert.ToDouble(oAvail);
                                            } // 09-30-2022
                                            // _logger.Debug("Buyeritemcode: " + detail846Record.BuyerItemCode + " Available qty =" + detail846Record.Quantity1.ToString()); // 09-30-2022

                                            //detail846Record.Description = "item quantity available";
                                            detail846Record.Description = Convert.ToDouble(oAvail).ToString();
                                            decimal oUnitPrice = 0;
                                            try
                                            {
                                                string sprice = Convert.ToString(reader["Price"].ToString());
                                                oUnitPrice = Convert.ToDecimal(sprice);
                                            }
                                            catch (Exception p)
                                            {
                                                oUnitPrice = 0;
                                                _logger.Debug("Error getting item price =>" + p.Message);
                                            }
                                            detail846Record.UnitPrice = Convert.ToDouble(oUnitPrice);
                                            String oItemName = "";
                                            try
                                            {
                                                oItemName = (string)reader["ItemName"];
                                            }
                                            catch
                                            {
                                                oItemName = "";
                                            }

                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            // detail846Record.ItemDescription = oItemName;
                                            oItemName = oItemName.Replace('"', ' ');
                                            detail846Record.ItemDescription = oItemName;
                                            // 10-14-2021 end
                                            // 01-29-2019 begin
                                            try
                                            {
                                                detail846Record.ItemUPC = (String)reader["BarCode"]; // added for Tractor Supply
                                            }
                                            catch (Exception bc)
                                            {
                                                detail846Record.ItemUPC = "";
                                                _logger.Error("Invalid UPC for Buyer Item " + detail846Record.BuyerItemCode + ": " + bc.Message);

                                            }
                                            // 01-29-2019 end
                                            // 09-24-2019 begin
                                            string oNextAvailDt = "";
                                            try
                                            {
                                                oNextAvailDt = Convert.ToString((DateTime)reader["NextAvailDt"]);
                                            }
                                            catch (Exception d)
                                            {
                                                string oErrMsg = d.Message;
                                                oNextAvailDt = "";
                                            }
                                            decimal oPOQty = 0;
                                            try
                                            {
                                                oPOQty = (decimal)reader["PoQty"];
                                            }
                                            catch
                                            {
                                                oPOQty = 0;
                                            }
                                            if (oPOQty > 0) // 10-15-2019
                                            {
                                                if (!String.IsNullOrWhiteSpace(oNextAvailDt) || oNextAvailDt.Trim().Length > 0)
                                                {
                                                    try
                                                    {
                                                        if (oPOQty > 0)
                                                        {
                                                            detail846Record.DateAvailable = Convert.ToDateTime(oNextAvailDt);
                                                        }
                                                        detail846Record.QtyOnOrder = Convert.ToDouble(oPOQty);
                                                    }
                                                    catch (Exception av)
                                                    {
                                                        _logger.Error("Error getting next available date for 846 BuyerItemCode " + detail846Record.BuyerItemCode + " =>" + av.Message);
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        detail846Record.QtyOnOrder = Convert.ToDouble(oPOQty);
                                                        DateTime NextDate = DateTime.Now;
                                                        NextDate = NextDate.AddDays(120);
                                                        detail846Record.DateAvailable = NextDate;
                                                    }
                                                    catch (Exception p)
                                                    {
                                                        _logger.Error("Error converting on order qty for 846 BuyerItemCode " + detail846Record.BuyerItemCode + "=> " + p.Message);
                                                    }
                                                }
                                                // 09-24-2019 end
                                            } // 10-15-2019
                                            // 03-28-2022 begin
                                            else if (request.CardCode.ToUpper().StartsWith("HDCL"))
                                            {
                                                try
                                                {
                                                    detail846Record.QtyOnOrder = Convert.ToDouble("0");
                                                    DateTime oNextDate = DateTime.Now;
                                                    detail846Record.DateAvailable = oNextDate;
                                                }
                                                catch (Exception p)
                                                {
                                                    _logger.Error("Error converting on order qty for 846 BuyerItemCode " + detail846Record.BuyerItemCode + "=> " + p.Message);
                                                }
                                            }
                                            // 03-28-2022 end
                                            // 10-15-2019 begin
                                            if (String.IsNullOrWhiteSpace(detail846Record.UOM))
                                            {
                                                detail846Record.UOM = "EA";
                                            }
                                            // 10-15-2019 end
                                            count846Rec = count846Rec + 1; // 10-15-2019
                                        }

                                        if (iPostProcess846Record != null)
                                        {
                                            iPostProcess846Record.OnPostProcess846Record(record);
                                        }
                                    }
                                    dbContext.SaveChanges();
                                }
                            }
                        }
                        sqlConnection.Close();

                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                        _logger.Error(ex.Message);
                    }
                    sqlConnection.Close();

                }
                response.Successful = true;
                // 03-24-2022 begin
                if (count846Rec == 0)
                {
                    response.ErrorMessage = "No inventory found";
                }
                // 03-24-2022 end
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                _logger.Error(ex.Message);
                response.ErrorMessage = ex.Message;
            }

            _logger.Debug("Returning the 846 response object which contains " + count846Rec + " 846s");
            //_logger.Debug("{@Get846RecordsResponse}", response);
            // 03-29-2022 begin
            if (count846Rec == 0)
            {
                response.ErrorMessage = "No inventory found";
            }
            // 03-29-2022 end
            _logger.Debug("Leaving Get846Records for " + request.CardCode);
            return response;
        }

        private IPreProcess846Record Get846PreProcess(String cardCode)
        {
            String preProcess846Record = ConfigurationManager.AppSettings["PreProcess846Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess846Record))
            {
                preProcess846Record = ConfigurationManager.AppSettings["PreProcess846Record"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess846Record))
            {
                try
                {
                    IPreProcess846Record iPreProcess846Record = (IPreProcess846Record)Activator.CreateInstance(Type.GetType(preProcess846Record, true));
                    return iPreProcess846Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess846Record", ex);
                    _logger.Error("Could not instantiate IPreProcess846Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess846Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private IPostProcess846Record Get846PostProcess(String cardCode)
        {
            String postProcess846Record = ConfigurationManager.AppSettings["PostProcess846Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess846Record))
            {
                postProcess846Record = ConfigurationManager.AppSettings["PostProcess846Record"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess846Record))
            {
                try
                {
                    IPostProcess846Record iPostProcess846Record = (IPostProcess846Record)Activator.CreateInstance(Type.GetType(postProcess846Record, true));
                    return iPostProcess846Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess846Record", ex);
                    _logger.Error("Could not instantiate IPostProcess846Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess846Record. Reason: " + ex.Message);
                }
            }
            return null;
        }
        // 01-19-2018 end

        // 07-30-2021 begin
        [HttpPost]
        public Get846WRecordsResponse Get846WRecords(Get846WRecordsRequest request)
        {
            int count846WRec = 0;
            // 06-13-2024 begin
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-13-2024 end

            string oSBOCardCode = getSBOCardCode(request.CardCode);
            IPreProcess846WRecord iPreProcess846WRecord = null;
            IPostProcess846WRecord iPostProcess846WRecord = null;
            List<Edi850WithSalesOrder> listToProcess = new List<Edi850WithSalesOrder>();
            _logger.Debug("Entering Get846WRecords for " + request.CardCode + " : " + request.CardCode);
            // _logger.Debug("Processing the following request object:");
            Get846WRecordsResponse response = new Get846WRecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get846WRecords";
                response.Successful = false;
                return response;
            }
            try
            {
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {
                    iPreProcess846WRecord = Get846WPreProcess(request.CardCode);
                    iPostProcess846WRecord = Get846WPostProcess(request.CardCode);
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess846WRecord = Get846WPreProcess(oSBOCardCode);
                    iPostProcess846WRecord = Get846WPostProcess(oSBOCardCode);
                }

                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }

                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }

                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    try
                    {
                        sqlConnection.Open();
                        string oItmQuery = //"select coalesce((select top 1 VendorNumber from InfocusEdi850HeaderRecord where CardCode = '" +
                                           //request.CardCode.Trim() + "'),'') as VendorId," +
                                           // 01-14-2024 begin
                        /*                "select IsNull(t0.DefaultVendor,'') as VendorId, " + // 04-07-2022
                                        "t0.BuyerItemCode, t0.VendorItemCode, coalesce(t0.AltVendItem,'') AltVendItem, t0.PurchaserItem, t0.OnHand, t0.OnOrder, t0.Available," +
                                        " t0.Commited, t0.Price, t0.ItemName, t0.CardName, t0.BarCode, t0.QtyQualifier, t0.NewItem, t0.Discontinued, t0.PoQty, t0.NextAvailDt, t0.UOM " +
                                        "from Infocus_EDI_846 t0 WITH(NOLOCK) " +
                                        " where t0.CardCode = '" + oSBOCardCode.Trim() + "'";
                       */
                        "execute [Get_Infocus_EDI_846W] '" + oSBOCardCode.Trim() + "'";
                        // 01-14-2024 end
                        using (SqlCommand command = new SqlCommand(oItmQuery, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                                {
                                    if (reader.HasRows)
                                    {
                                        int oRecordNo = 0;
                                        Edi846WHeaderRecord record = new Edi846WHeaderRecord();
                                        while (reader.Read())
                                        {
                                            if (oRecordNo == 0)
                                            {
                                                response.Edi846WRecords.Add(record);
                                                record.CardCode = request.CardCode;
                                                record.InventoryDate = DateTime.Today;
                                                if (!String.IsNullOrWhiteSpace((String)reader["VendorId"]))
                                                {
                                                    record.VendorNumber = (String)reader["VendorId"];
                                                }
                                                DateTime oToday = DateTime.Now;
                                                string oKey = oToday.ToString("yyyyMMddHHmm");
                                                record.ReportId = oKey;

                                                oRecordNo = oRecordNo + 1;
                                            }
                                            Edi846WDetailRecord detail846WRecord = new Edi846WDetailRecord();
                                            record.Details.Add(detail846WRecord);

                                            string altVendorItem = "";
                                            try
                                            {
                                                altVendorItem = (String)reader["AltVendItem"];
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    altVendorItem = altVendorItem.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }
                                            catch
                                            {
                                                altVendorItem = "";
                                            } // 02-21-2018 end
                                            // 09-24-2019 begin
                                            string purchaserItem = "";
                                            try
                                            {
                                                purchaserItem = (String)reader["PurchaserItem"];
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    purchaserItem = purchaserItem.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }
                                            catch
                                            {
                                                purchaserItem = "";
                                            }
                                            string oBuyerItemCode = "";
                                            try
                                            {
                                                oBuyerItemCode = (String)reader["BuyerItemCode"];
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    oBuyerItemCode = oBuyerItemCode.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }
                                            catch
                                            {
                                                oBuyerItemCode = "";
                                            }
                                            if (request.CardCode.StartsWith("HDCL") && purchaserItem.Trim().Length > 0)
                                            {
                                                detail846WRecord.BuyerItemCode = purchaserItem.Trim();
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846WRecord.BuyerItemCode = detail846WRecord.BuyerItemCode.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }
                                            else
                                            {
                                                // 09-24-2019 end
                                                detail846WRecord.BuyerItemCode = (String)reader["BuyerItemCode"];
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846WRecord.BuyerItemCode = detail846WRecord.BuyerItemCode.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            } // 09-24-2019 
                                            if (String.IsNullOrWhiteSpace(altVendorItem))
                                            {  // 02-21-2018
                                                detail846WRecord.VendorItemCode = (String)reader["VendorItemCode"];
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846WRecord.VendorItemCode = detail846WRecord.VendorItemCode.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }
                                            else
                                            {  // 02-21-2018 begin
                                                detail846WRecord.VendorItemCode = altVendorItem;
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846WRecord.VendorItemCode = detail846WRecord.VendorItemCode.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }
                                            if (String.IsNullOrWhiteSpace(detail846WRecord.BuyerItemCode))
                                            {
                                                detail846WRecord.BuyerItemCode = detail846WRecord.VendorItemCode;
                                                // 06-13-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail846WRecord.BuyerItemCode = detail846WRecord.BuyerItemCode.ToUpper();
                                                }
                                                // 06-13-2024 end
                                            }

                                            try
                                            {
                                                string itmUOM = (String)reader["UOM"];
                                                if (String.IsNullOrWhiteSpace(itmUOM))
                                                {
                                                    detail846WRecord.UOM = "EA";
                                                }
                                                else
                                                {
                                                    detail846WRecord.UOM = itmUOM;
                                                }
                                            }
                                            catch
                                            {
                                                detail846WRecord.UOM = "EA";
                                            }
                                            // 10-15-2019 end
                                            try
                                            {
                                                detail846WRecord.ItemDiscontinued = (String)reader["Discontinued"];
                                                if (String.IsNullOrWhiteSpace(detail846WRecord.ItemDiscontinued))
                                                {
                                                    detail846WRecord.ItemDiscontinued = "N";
                                                }
                                            }
                                            catch
                                            {
                                                detail846WRecord.ItemDiscontinued = "N";
                                            }
                                            // 07-18-2019 end
                                            try
                                            {
                                                detail846WRecord.Quantity1Qualifier = (String)reader["QtyQualifier"];
                                            }
                                            catch
                                            {
                                                detail846WRecord.Quantity1Qualifier = "33";
                                            }
                                            decimal oOnHand = 0;
                                            try
                                            {
                                                oOnHand = (decimal)reader["OnHand"];
                                            }
                                            catch (Exception e3)
                                            {
                                                oOnHand = 0;
                                                string oErrMesg = e3.Message;
                                            }
                                            decimal oCommitted = 0;
                                            try
                                            {
                                                oCommitted = (decimal)reader["Commited"];
                                            }
                                            catch (Exception e4)
                                            {
                                                oCommitted = 0;
                                                string oErrMesg = e4.Message;
                                            }
                                            decimal oOrdered = 0;
                                            try
                                            {
                                                string oValue = Convert.ToString(reader["OnOrder"].ToString());
                                                oOrdered = Convert.ToDecimal(oValue);
                                                //oOrdered = (decimal)reader["OnOrder"];
                                            }
                                            catch (Exception e2)
                                            {
                                                oOrdered = 0;
                                                string oErrMesg = e2.Message;
                                            }
                                            decimal oAvail = oOnHand + oOrdered;
                                            oAvail = oAvail - oCommitted;
                                            if (oAvail < 0 || (oOnHand + oOrdered) < oCommitted)
                                            {
                                                oAvail = 0;
                                            }
                                            detail846WRecord.Quantity1 = Convert.ToDouble(oAvail);
                                            //detail846WRecord.Description = "item quantity available";
                                            detail846WRecord.Description = Convert.ToDouble(oAvail).ToString();
                                            decimal oUnitPrice = 0;
                                            try
                                            {
                                                string sprice = Convert.ToString(reader["Price"].ToString());
                                                oUnitPrice = Convert.ToDecimal(sprice);
                                            }
                                            catch (Exception p)
                                            {
                                                oUnitPrice = 0;
                                                _logger.Error("Error getting item price =>" + p.Message);
                                            }
                                            detail846WRecord.UnitPrice = Convert.ToDouble(oUnitPrice);
                                            String oItemName = "";
                                            try
                                            {
                                                oItemName = (string)reader["ItemName"];
                                            }
                                            catch
                                            {
                                                oItemName = "";
                                            }

                                            // 10-14-2021 begin
                                            //detail846WRecord.ItemDescription = oItemName;
                                            // remove quotes from item description
                                            oItemName = oItemName.Replace('"', ' ');
                                            detail846WRecord.ItemDescription = oItemName;
                                            // 10-14-2021 end
                                            // 01-29-2019 begin
                                            try
                                            {
                                                detail846WRecord.ItemUPC = (String)reader["BarCode"]; // added for Tractor Supply
                                            }
                                            catch (Exception bc)
                                            {
                                                detail846WRecord.ItemUPC = "";
                                                _logger.Error("Invalid UPC for Buyer Item " + detail846WRecord.BuyerItemCode + ": " + bc.Message);
                                            }
                                            // 01-29-2019 end
                                            // 09-24-2019 begin
                                            string oNextAvailDt = "";
                                            try
                                            {
                                                oNextAvailDt = Convert.ToString((DateTime)reader["NextAvailDt"]);
                                            }
                                            catch (Exception d)
                                            {
                                                string oErrMsg = d.Message;
                                                oNextAvailDt = "";
                                            }
                                            decimal oPOQty = 0;
                                            try
                                            {
                                                oPOQty = (decimal)reader["PoQty"];
                                            }
                                            catch
                                            {
                                                oPOQty = 0;
                                            }
                                            if (oPOQty > 0) // 10-15-2019
                                            {
                                                if (!String.IsNullOrWhiteSpace(oNextAvailDt) || oNextAvailDt.Trim().Length > 0)
                                                {
                                                    try
                                                    {
                                                        if (oPOQty > 0)
                                                        {
                                                            detail846WRecord.DateAvailable = Convert.ToDateTime(oNextAvailDt);
                                                        }
                                                        detail846WRecord.QtyOnOrder = Convert.ToDouble(oPOQty);
                                                    }
                                                    catch (Exception av)
                                                    {
                                                        _logger.Error("Error getting next available date for 846W BuyerItemCode " + detail846WRecord.BuyerItemCode);
                                                    }
                                                }
                                                else
                                                {
                                                    try
                                                    {
                                                        detail846WRecord.QtyOnOrder = Convert.ToDouble(oPOQty);
                                                        DateTime NextDate = DateTime.Now;
                                                        NextDate = NextDate.AddDays(120);
                                                        detail846WRecord.DateAvailable = NextDate;
                                                    }
                                                    catch (Exception p)
                                                    {
                                                        _logger.Error("Error converting on order qty for 846W BuyerItemCode " + detail846WRecord.BuyerItemCode + "=> " + p.Message);
                                                    }
                                                }
                                                // 09-24-2019 end
                                            } // 10-15-2019
                                            // 10-15-2019 begin
                                            if (String.IsNullOrWhiteSpace(detail846WRecord.UOM))
                                            {
                                                detail846WRecord.UOM = "EA";
                                            }
                                            // 10-15-2019 end
                                            // 02-01-2024 begin
                                            /*
                                            // 08-03-2021 begin
                                            SqlConnection sqlConn = new SqlConnection(GetConnectionString(oConnectionName));
                                            sqlConn.Open();
                                            // 2022-10-10 begin
                                            //
                                            //string oWhsQry = "select t0.WhsCode, IsNull(t0.OnHand,0) OnHand, IsNull(t0.OnOrder,0) OnOrder, IsNull(t0.Commited,0) Commited" +
                                            //                  " from dbo.[Infocus_EDI_846Whs]  t0 WITH(NOLOCK) where t0.CardCode = '" + oSBOCardCode.Trim() + "'" +
                                            //                  " and t0.BuyerItemCode = '" + oBuyerItemCode + "'";
                                            //
                                            String oWhsQry = "select * from [Infocus_EDI_846W_Detail] where CardCode = '" + oSBOCardCode.Trim() + "' " +
                                                             "and BuyerItemCode = '" + oBuyerItemCode.Trim() + "'";
                                            // 2023-10-10 end
                                            using (SqlCommand whsCmd = new SqlCommand(oWhsQry, sqlConn))
                                            {
                                                try
                                                {
                                                    using (SqlDataReader readerw2 = whsCmd.ExecuteReader())
                                                    {
                                                        using (WebApiDbContext dbContext2 = new WebApiDbContext(oConnectionName))
                                                        {
                                                            if (readerw2.HasRows)
                                                            {

                                                                int oRecNo = 0;
                                                                while (readerw2.Read())
                                                                {
                                                                    string oWhsCode = "";
                                                                    try
                                                                    {
                                                                        oWhsCode = (string)readerw2["WhsCode"];
                                                                    }
                                                                    catch (Exception w2)
                                                                    {
                                                                        oWhsCode = "Charlott";
                                                                        string oErrMesg = w2.Message;
                                                                    }
                                                                    // 10-10-2023 begin
                                                                    /*
                                                                    decimal oOH = 0;
                                                                    try
                                                                    {
                                                                        oOH = (decimal)readerw2["OnHand"];
                                                                    }
                                                                    catch (Exception w3)
                                                                    {
                                                                        oOH = 0;
                                                                        string oErrMesg = w3.Message;
                                                                    }
                                                                    decimal oCmtd = 0;
                                                                    try
                                                                    {
                                                                        oCmtd = (decimal)readerw2["Commited"];
                                                                    }
                                                                    catch (Exception w4)
                                                                    {
                                                                        oCmtd = 0;
                                                                        string oErrMesg = w4.Message;
                                                                    }
                                                                    decimal oOnOrd = 0;
                                                                    try
                                                                    {
                                                                        oOnOrd = (decimal)readerw2["OnOrder"];
                                                                    }
                                                                    catch (Exception w2)
                                                                    {
                                                                        oOnOrd = 0;
                                                                        string oErrMesg = w2.Message;
                                                                    }
                                                                    decimal oWhsAvail = oOH + oOnOrd;
                                                                    oWhsAvail = oWhsAvail - oCmtd;
                                                                     if (oWhsAvail < 0 || (oOH + oOnOrd) < oCmtd)
                                                                    {
                                                                        oWhsAvail = 0;
                                                                    }*/
                                            /* 02-01-2024 begin #2
                                                                    decimal oWhsAvail = 0;
                                                                    try
                                                                    {
                                                                        oWhsAvail = (decimal)readerw2["AvailQty"];
                                                                    }
                                                                    catch (Exception w2)
                                                                    {
                                                                        oWhsAvail = 0;
                                                                        string oErrMesg = w2.Message;
                                                                    }
                                                                    if (oWhsAvail < 0)
                                                                    {
                                                                        oWhsAvail = 0;
                                                                    }
                                                                    // 2023-10-10 end
                                                                    Edi846WWhsRecord warehouseRec = new Edi846WWhsRecord();
                                                                    detail846WRecord.Warehouse.Add(warehouseRec);
                                                                    warehouseRec.Warehouse = oWhsCode;
                                                                    warehouseRec.WhsQualifier = "WS";
                                                                    double oWhsQty = Convert.ToDouble(oWhsAvail);
                                                                    if (oWhsQty > detail846WRecord.Quantity1)
                                                                    {
                                                                        warehouseRec.WhsQuantity = detail846WRecord.Quantity1;
                                                                    }
                                                                    else
                                                                    {
                                                                        warehouseRec.WhsQuantity = Convert.ToDouble(oWhsAvail);
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                                catch (Exception rw)
                                                {
                                                    string oError = rw.Message;
                                                }
                                                finally
                                                {
                                                    string oMesgW2 = "Test";
                                                }
                                            }
                                            sqlConn.Close();
                                            sqlConn.Dispose();
                                            // 08-03-2021 end
                                            */
                                            decimal oTotWhsAvail = 0; // 02-14-2024
                                            decimal oWhsAvail = 0;
                                            try
                                            {
                                                string oQtyStr = Convert.ToString(reader["ChltQty"].ToString());
                                                oWhsAvail = Convert.ToDecimal(oQtyStr);
                                            }
                                            catch (Exception c)
                                            {
                                                string oErrMsg = c.Message;
                                                oWhsAvail = 0;
                                            }
                                            if (oWhsAvail < 0)
                                            {
                                                oWhsAvail = 0;
                                            }
                                            oTotWhsAvail = oWhsAvail; // 02-14-2024 
                                            Edi846WWhsRecord warehouseRec = new Edi846WWhsRecord();
                                            detail846WRecord.WhsDetail.Add(warehouseRec);
                                            //warehouseRec.Warehouse = "Charlott";
                                            // 02-20-2024 correct warehouse id
                                            warehouseRec.Warehouse = "001"; // 02-14-2024 set whs id to match CHUB
                                            warehouseRec.WhsQualifier = "WS";
                                            // 05-29-2024 begin
                                            if (oWhsAvail != oAvail)
                                            {
                                                oWhsAvail = oAvail;
                                            }
                                            // 05-29-2024 end
                                            double oWhsQty = Convert.ToDouble(oWhsAvail);
                                            // 02-14-2024 begin
                                            /* if (oWhsQty > detail846WRecord.Quantity1)
                                             {
                                                 warehouseRec.WhsQuantity = detail846WRecord.Quantity1;
                                             }
                                             else
                                             {
                                                 warehouseRec.WhsQuantity = Convert.ToDouble(oWhsAvail);
                                             } */
                                            warehouseRec.WhsQuantity = Convert.ToDouble(oWhsAvail);
                                            // 02-14-2024 end
                                            // 09-24-2025 begin
                                            if (oPOQty > 0)
                                            {
                                                warehouseRec.WhsNextAvailQty = Convert.ToDouble(oPOQty); // 10-21-2025
                                                try
                                                {
                                                    DateTime whsDateTime = Convert.ToDateTime(oNextAvailDt);
                                                    if (whsDateTime <= DateTime.Now)
                                                    {
                                                        whsDateTime = DateTime.Now;
                                                        whsDateTime = whsDateTime.AddDays(1);
                                                    }
                                                    warehouseRec.WhsNextAvailDt = whsDateTime;
                                                }
                                                catch (Exception av)
                                                {
                                                    _logger.Error("Error getting whs next available date for 846 BuyerItemCode " + detail846WRecord.BuyerItemCode + " =>" + av.Message);
                                                }
                                            }
                                            // 09-24-2025 end
                                            //count846WRec = count846WRec + 1; // 10-15-2019
                                            // 02-01-2024 end
                                            // 05-01-2024 begin 
                                            // remove Indian Trail from 846W
                                            /* warehouseRec = new Edi846WWhsRecord();
                                            detail846WRecord.WhsDetail.Add(warehouseRec);
                                            
                                            //warehouseRec.Warehouse = "IndianTr"; // 02-14-2024 set whs id to match CHUB
                                            // 02-20-2024 correct warehouse id
                                            warehouseRec.Warehouse = "003";
                                            warehouseRec.WhsQualifier = "WS";
                                            oWhsAvail = 0;
                                            try
                                            {
                                                string oQtyStr = Convert.ToString(reader["IndianTrQty"].ToString());
                                                oWhsAvail = Convert.ToDecimal(oQtyStr); ;
                                            }
                                            catch (Exception i)
                                            {
                                                string oErrMsg = i.Message;
                                                oWhsAvail = 0;
                                            }
                                            if (oWhsAvail < 0)
                                            {
                                                oWhsAvail = 0;
                                            }
                                             */
                                            // 05-01-2024 end
                                            // 02-14-2024 begin
                                            /*
                                            if (oWhsQty > detail846WRecord.Quantity1)
                                            {
                                                warehouseRec.WhsQuantity = detail846WRecord.Quantity1;
                                            }
                                            else
                                            {
                                                warehouseRec.WhsQuantity = Convert.ToDouble(oWhsAvail);
                                            }*/
                                            // 05-01-2024 begin 
                                            // remove Indian Trail from 846W
                                            /*
                                            warehouseRec.WhsQuantity = Convert.ToDouble(oWhsAvail);
                                            oTotWhsAvail = oTotWhsAvail + oWhsAvail;
                                            double oTotAvail = Convert.ToDouble(oTotWhsAvail);
                                            detail846WRecord.Quantity1 = oTotAvail;
                                            // 02-14-2024 end
                                            //count846WRec = count846WRec + 1;
                                            // 02-01-2024 end Indian Trail
                                            */
                                            // 05-01-2023 end
                                            //if (iPostProcess846WRecord != null)
                                            // {
                                            //iPostProcess846WRecord.OnPostProcess846WRecord(record);
                                            // }
                                            count846WRec = count846WRec + 1;
                                        }
                                    }
                                    dbContext.SaveChanges();
                                }
                            }
                        }
                        sqlConnection.Close();
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex);
                        _logger.Error(ex.Message);
                    }
                    sqlConnection.Close();

                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                _logger.Error(ex.Message);
                response.ErrorMessage = ex.Message;
            }
            _logger.Debug("Returning the 846W response object which contains " + count846WRec + " 846Ws");
            //_logger.Debug("{@Get846WRecordsResponse}", response);
            // 03-29-2022 begin
            if (count846WRec == 0)
            {
                response.ErrorMessage = "No inventory found";
            }
            // 03-29-2022 end
            _logger.Debug("Leaving Get846WRecords for " + request.CardCode);
            return response;
        }

        private IPreProcess846WRecord Get846WPreProcess(String cardCode)
        {
            String preProcess846WRecord = ConfigurationManager.AppSettings["PreProcess846WRecord-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess846WRecord))
            {
                preProcess846WRecord = ConfigurationManager.AppSettings["PreProcess846WRecord"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess846WRecord))
            {
                try
                {
                    IPreProcess846WRecord iPreProcess846WRecord = (IPreProcess846WRecord)Activator.CreateInstance(Type.GetType(preProcess846WRecord, true));
                    return iPreProcess846WRecord;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess846WRecord", ex);
                    _logger.Error("Could not instantiate IPreProcess846WRecord =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess846WRecord. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private IPostProcess846WRecord Get846WPostProcess(String cardCode)
        {
            String postProcess846WRecord = ConfigurationManager.AppSettings["PostProcess846WRecord-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess846WRecord))
            {
                postProcess846WRecord = ConfigurationManager.AppSettings["PostProcess846WRecord"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess846WRecord))
            {
                try
                {
                    IPostProcess846WRecord iPostProcess846WRecord = (IPostProcess846WRecord)Activator.CreateInstance(Type.GetType(postProcess846WRecord, true));
                    return iPostProcess846WRecord;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess846WRecord", ex);
                    _logger.Error("Could not instantiate IPostProcess846WRecord =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess846WRecord. Reason: " + ex.Message);
                }
            }
            return null;
        }
        // 07-30-2021 end

        [HttpPost]
        public Get810RecordsResponse Get810Records(Get810RecordsRequest request)
        {
            string oSBOCardCode = getSBOCardCode(request.CardCode); // 01-17-2018
            bool bHasNonInventory = false;
            IPreProcess810Record iPreProcess810Record = null;
            IPostProcess810Record iPostProcess810Record = null;
            List<Edi850WithInvoice> listToProcess = new List<Edi850WithInvoice>();
            _logger.Debug("Entering Get810Records for " + request.CardCode + " : " + request.CardCode);
            // 06-21-2024 begin
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-21-2024 end
            Get810RecordsResponse response = new Get810RecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get810Records";
                response.Successful = false;
                return response;
            }
            try
            {
                // 01-17-2018 begin
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {  // 01-17-2018 end
                    iPreProcess810Record = Get810PreProcess(request.CardCode);
                    iPostProcess810Record = Get810PostProcess(request.CardCode);
                    // 01-17-2018 begin
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess810Record = Get810PreProcess(oSBOCardCode);
                    iPostProcess810Record = Get810PostProcess(oSBOCardCode);
                }
                // 01-17-2018 end
                List<Edi850HeaderRecord> listOf850Records = null;
                // 01-17-2018  begin
                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                // 04-30-2019 begin
                _logger.Debug("Setting EDI Invoice Defaults");
                if (request.CardCode == "ACE" || request.CardCode == "ACET" || request.CardCode == "C001000" || request.CardCode == "C001000T")
                {
                    _logger.Debug("Skip setting EDI Invoice Defaults for " + request.CardCode);
                }
                // 08-02-2022 begin 
                /*
            else
            {
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    try
                    {
                        /* updating invoice */
                // sqlConnection.Open();
                // 05-15-2020 begin
                // remove update of EDI Ln# on INV1
                /*
                //string oQuery = "execute [dbo].[Infocus_EDI_Invoice] ";
                string oQuery = "UPDATE INV1 SET BaseEntry = COALESCE(BaseEntry,0), BaseLine = COALESCE(BaseLine,0), " +
                                 "ShipDate = COALESCE(ShipDate,DocDate), U_InfoItmStatus = coalesce(U_InfoItmStatus, 'IA'), U_InfoW2LNo = coalesce(U_InfoW2LNo,1) " +
                                 " where  coalesce(U_InfoW2LNo,0) = 0 and DocEntry in (select DocEntry from Infocus_Open_810); " +
                                 "UPDATE OINV set U_InfoOrdStatus = coalesce(U_InfoOrdStatus, coalesce((select top 1 U_InfoOrdStatus from ORDR where NumAtCard = OINV.NumAtCard and Canceled = 'N'),'IA')), " +
                                 "U_InfoTrxPurpose = coalesce(U_InfoTrxPurpose,0) where DocEntry in (select DocEntry from Infocus_Open_810);  " +
                                 "UPDATE OINV set U_InfoW2CnNo = (select Top 1 SalesOrderKey from InfocusEdi850HeaderRecord where NumAtCard = PurchaseOrderReference) " +
                                 "where DocStatus = 'O' and NumAtCard in (select PurchaseOrderReference from InfocusEdi850HeaderRecord where Processed810 = 0) and (U_InfoW2CnNo is null or U_InfoW2CnNo = 0); ";
                try
                {
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception del2)
                {
                    _logger.Debug("Error updating invoice defaults => " + del2.Message);
                }
                 */
                // 05-15-2020 end
                /*
                            sqlConnection.Close();
                        }
                        catch (Exception del)
                        {
                            _logger.Error("Error updating delivery line numbers => " + del.Message);
                        }
                    }
                }
                */
                // 08-02-2022 end
                // 04-30-2019 end
                // 05-17-2020 begin
                /*
                // 03-03-2020 begin
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    try
                    {
                        sqlConnection.Open();
                        string oQuery = "update INV1 set U_InfoW2LNo = (select DLN1.U_InfoW2LNo from DLN1 where DLN1.DocEntry = INV1.BaseEntry and " +
                                        "DLN1.LineNum = INV1.BaseLine and INV1.BaseTYpe = '15') " +
                                        "where INV1.U_InfoW2LNo <= 0 and Inv1.ItemCode not in ('Warehouse') and INV1.TreeType in ('S','N') and " +
                                        "INV1.DocEntry in (select OINV.DocEntry from OINV where NumAtCard in " +
                                        "(select purchaseOrderReference from InfocusEdi850HeaderRecord where Processed856=1 and Processed810=0))";
                        try
                        {
                            using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception inv1)
                        {
                            _logger.Debug("Error updating invoice edi line numbers => " + inv1.Message);
                        }

                        sqlConnection.Close();
                    }
                    catch (Exception inv0)
                    {
                        _logger.Debug("Error updating invoice edi line numbers => " + inv0.Message);
                    }
                }
                // 03-03-2020 end
                */
                // 05-17-2020 end
                //    using (WebApiDbContext dbContext = new WebApiDbContext())

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    bHasNonInventory = getNonInventory(oConnectionName);
                    listOf850Records = dbContext.Edi850HeaderRecords.Include("Details")
                        .Where(x => x.Processed810 == false
                            && x.Processed == true
                            && x.Processed856 == true
                            && x.Processed856DateTime.ToString().Trim().Length > 0 // 08-22-2019 
                            && x.TrxPurpose != "01" //05-31-2017
                            && x.CardCode == request.CardCode
                            && (x.IgnoreTrxRequest != "Y") // 08-06-2022             
                                                           //&& x.CardCode == oSBOCardCode // 01-17-2018
                                                           // && (x.HeaderId >= 28926 || request.CardCode == "ACET" || request.CardCode == "ACE" || request.CardCode == "C001000" || request.CardCode == "C001000T")
                            && x.SalesOrderKey > 0).ToList();
                    _logger.Debug("There are " + listOf850Records.Count + " 810 records to process");
                    foreach (var record in listOf850Records)
                    {
                        //_logger.Debug("Processing 810 with 850 Key " + record.HeaderId + ", CardCode: " + record.CardCode);
                        try
                        {
                            Invoice invoice = FindMatchingInvoice(dbContext, record);
                            // 02-14-2023 begin
                            DateTime oDateTime = (DateTime)record.ProcessedDateTime;
                            String oProcDate = oDateTime.ToShortDateString();
                            DateTime oProcessedDate = Convert.ToDateTime(oProcDate);
                            // 02-14-2023 end
                            if (invoice != null
                                 //&& invoice.CreateDate >= record.ProcessedDateTime) // 02-02-2023
                                 && invoice.CreateDate >= oProcessedDate) // 02-14-2023 
                            {
                                //_logger.Debug("Found 810 invoice from delivery with DocNum " + invoice.DocNum);
                                // 07-22-2021 begin
                                bool bMissingTrackNo = false;
                                if (record.CardCode.StartsWith("HDCL"))
                                {
                                    if ((((invoice.TrackNo == null) || String.IsNullOrWhiteSpace(invoice.TrackNo) || invoice.TrackNo.Trim().Length == 0)) &&
                                          (invoice.U_Info_BOL == null || String.IsNullOrWhiteSpace(invoice.U_Info_BOL) || invoice.U_Info_BOL.Trim().Length == 0))
                                    {
                                        string HDError = "810 Error: Home Depot Invoice# " + invoice.DocNum + " does not contain a tracking number";
                                        _logger.Error(HDError);
                                        bMissingTrackNo = true;
                                        record.ErrorMessage = HDError;
                                        dbContext.SaveChanges();
                                    }
                                }
                                // 07-22-2021 end
                                // 05-17-2020 begin 
                                int oZeroLinNo = checkEDILnNo(oConnectionName, "INV1", invoice.DocEntry, "15", invoice.CardCode);
                                if (oZeroLinNo > 0)
                                {
                                    _logger.Error("Found lines for Invoice# " + invoice.DocNum + " with 0 EDI Line#");
                                }
                                // 05-17-2020 begin 
                                int oZeroQty = checkZeroQty(oConnectionName, "INV1", "OINV", invoice.DocEntry, "15");
                                if (oZeroQty > 0)
                                {
                                    _logger.Error("Found lines for Invoice# " + invoice.DocNum + " with zero quantity");
                                }
                                if (oZeroLinNo > 0 || oZeroQty > 0)
                                {
                                    getInvoiceData(oConnectionName, invoice);
                                }
                                oZeroQty = 0;
                                oZeroLinNo = 0;
                                foreach (InvoiceLine invLine in invoice.InvoiceLines)
                                {
                                    if (invLine.TreeType == "S" || invLine.TreeType == "N")
                                    {
                                        if (invLine.Quantity <= 0)
                                        {
                                            oZeroQty = oZeroQty + 1;
                                        }
                                        if (invLine.LineNumber850 <= 0)
                                        {
                                            oZeroLinNo = oZeroLinNo + 1;
                                        }
                                    }
                                }
                                if (oZeroLinNo > 0 || oZeroQty > 0)
                                {
                                    _logger.Error("Unable to set invalid data from delivery/sales order for Invoice# " + invoice.DocNum);
                                }
                                // 05-17-2020 end
                                // 05-15-2020 end

                                else if (bMissingTrackNo == false)  // 07-28-2021
                                {
                                    listToProcess.Add(new Edi850WithInvoice(record, invoice));
                                } // 07-28-2021 end
                            } // 04-14-2019 begin
                            else if (invoice == null) // 09-23-2019 added if *invoice == null
                            {
                                //String connectionString = GetConnectionString(oConnectionName);
                                // 08-04-2022 begin
                                invoice = GetInvoiceFromXref(dbContext, oConnectionName, record);
                                if (invoice == null)
                                { // 08-04-2022 end
                                    invoice = FindMatchingInvoiceByDel(dbContext, oConnectionName, record);
                                } // 08-04-2022
                                if (invoice != null
                                //&& invoice.CreateDate >= record.ProcessedDateTime) // 02-02-2023
                                && invoice.CreateDate >= oProcessedDate) // 02-14-2023 
                                {
                                    //_logger.Debug("Found 810 invoice from delivery with DocNum " + invoice.DocNum);
                                    // 07-28-2021begin
                                    bool bMissingTrackNo = false;
                                    if (record.CardCode.StartsWith("HDCL"))
                                    {
                                        if ((((invoice.TrackNo == null) || String.IsNullOrWhiteSpace(invoice.TrackNo) || invoice.TrackNo.Trim().Length == 0)) &&
                                              (invoice.U_Info_BOL == null || String.IsNullOrWhiteSpace(invoice.U_Info_BOL) || invoice.U_Info_BOL.Trim().Length == 0))
                                        {
                                            string HDError = "810 Error: Home Depot Invoice# " + invoice.DocNum + " does not contain a tracking number";
                                            _logger.Error(HDError);
                                            bMissingTrackNo = true;
                                            record.ErrorMessage = HDError;
                                            dbContext.SaveChanges();
                                        }
                                    }
                                    if (bMissingTrackNo == false)
                                    { // 07-28-2021 end                           
                                        listToProcess.Add(new Edi850WithInvoice(record, invoice));
                                    } // 07-28-2021
                                }
                                // 02-28-2018 begin
                                else if (bHasNonInventory)
                                {
                                    invoice = FindMatchingInvoiceByDel(dbContext, oConnectionName, record);
                                    if (invoice != null
                                    // && invoice.CreateDate >= record.ProcessedDateTime) // 02-02-2023
                                    && invoice.CreateDate >= oProcessedDate) // 02-14-2023 
                                    {
                                        // 07-28-2021begin
                                        bool bMissingTrackNo = false;
                                        if (record.CardCode.StartsWith("HDCL"))
                                        {
                                            if ((((invoice.TrackNo == null) || String.IsNullOrWhiteSpace(invoice.TrackNo) || invoice.TrackNo.Trim().Length == 0)) &&
                                                  (invoice.U_Info_BOL == null || String.IsNullOrWhiteSpace(invoice.U_Info_BOL) || invoice.U_Info_BOL.Trim().Length == 0))
                                            {
                                                string HDError = "810 Error: Home Depot Invoice# " + invoice.DocNum + " does not contain a tracking number";
                                                _logger.Error(HDError);
                                                bMissingTrackNo = true;
                                                record.ErrorMessage = HDError;
                                                dbContext.SaveChanges();
                                            }
                                        }
                                        if (bMissingTrackNo == false)
                                        { // 07-28-2021 end   
                                            listToProcess.Add(new Edi850WithInvoice(record, invoice));
                                        } // 07-28-2021
                                        // 02-28-2018 end
                                    }
                                    else
                                    {
                                        // 09-23-2019 begin
                                        invoice = FindMatchingInvoiceBySalesOrd(dbContext, oConnectionName, record);
                                        if (invoice != null
                                        //&& invoice.CreateDate >= record.ProcessedDateTime) // 02-02-2023              
                                        && invoice.CreateDate >= oProcessedDate) // 02-14-2023 
                                        {
                                            // 07-28-2021begin
                                            bool bMissingTrackNo = false;
                                            if (record.CardCode.StartsWith("HDCL"))
                                            {
                                                if ((((invoice.TrackNo == null) || String.IsNullOrWhiteSpace(invoice.TrackNo) || invoice.TrackNo.Trim().Length == 0)) &&
                                                      (invoice.U_Info_BOL == null || String.IsNullOrWhiteSpace(invoice.U_Info_BOL) || invoice.U_Info_BOL.Trim().Length == 0))
                                                {
                                                    string HDError = "810 Error: Home Depot Invoice# " + invoice.DocNum + " does not contain a tracking number";
                                                    _logger.Error(HDError);
                                                    bMissingTrackNo = true;
                                                    record.ErrorMessage = HDError;
                                                    dbContext.SaveChanges();
                                                }
                                            }
                                            if (bMissingTrackNo == false)
                                            { // 07-28-2021 end   
                                                listToProcess.Add(new Edi850WithInvoice(record, invoice));
                                            } // 07-28-2021
                                        }
                                        /*  else
                                          {
                                              // 09-23-2019 end
                                              _logger.Debug("810 invoice not found for 850 record with key " + record.HeaderId);
                                          } // 09-23-2019
                                         */
                                    }
                                }
                                // 09-23-2019 begin
                                else if (invoice == null)
                                {
                                    invoice = FindMatchingInvoiceBySalesOrd(dbContext, oConnectionName, record);
                                    if (invoice != null
                                    //&& invoice.CreateDate >= record.ProcessedDateTime) // 02-02-2023
                                    && invoice.CreateDate >= oProcessedDate) // 02-14-2023 
                                    {
                                        // 07-28-2021begin
                                        bool bMissingTrackNo = false;
                                        if (record.CardCode.StartsWith("HDCL"))
                                        {
                                            if ((((invoice.TrackNo == null) || String.IsNullOrWhiteSpace(invoice.TrackNo) || invoice.TrackNo.Trim().Length == 0)) &&
                                                  (invoice.U_Info_BOL == null || String.IsNullOrWhiteSpace(invoice.U_Info_BOL) || invoice.U_Info_BOL.Trim().Length == 0))
                                            {
                                                string HDError = "810 Error: Home Depot Invoice# " + invoice.DocNum + " does not contain a tracking number";
                                                _logger.Error(HDError);
                                                bMissingTrackNo = true;
                                                record.ErrorMessage = HDError;
                                                dbContext.SaveChanges();
                                            }
                                        }
                                        if (bMissingTrackNo == false)
                                        { // 07-28-2021 end   
                                            listToProcess.Add(new Edi850WithInvoice(record, invoice));
                                        } // 07-28-2021
                                        // 02-28-2018 end
                                    }
                                    /*    else
                                         {
                                             _logger.Debug("810 invoice not found for 850 record with key " + record.HeaderId);
                                         } 
                                     */
                                }
                                // 09-23-2019 end
                                // 04-07-2022 begin
                                /*
                            // 04-14-2019 begin
                            else if (invoice == null)
                            {
                                _logger.Debug("810 invoice not found for 850 record with key " + record.HeaderId);
                            }
                                 */
                                // 04-07-2022 end
                            }
                            // 04-14-2019 end

                        }
                        catch (Exception c0)
                        {
                            String oErrMesg = c0.Message;
                            _logger.Error("Error Processing 810 " + oErrMesg);
                        }
                    }
                }
                if (listToProcess.Count == 0)
                {
                    response.Successful = true;
                    response.ErrorMessage = "No matching 810s found"; // 03-29-2022
                    _logger.Debug("Leaving Get810Records for " + oSBOCardCode); // 01-25-2023
                    return response;
                }
                else
                {
                    _logger.Debug(listToProcess.Count + " 810s to be processed");
                }
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    sqlConnection.Open();
                    //To do -Need to filter records, but not sure how to right now
                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            if (iPreProcess810Record != null)
                            {
                                if (!iPreProcess810Record.OnPreProcess810Record(selectedRecord.Invoice, selectedRecord.Edi850HeaderRecord))
                                {
                                    continue;
                                }
                            }
                            // 12-02-2019 begin
                            bool bValid = false;
                            try
                            {
                                string oQry = "select COUNT(i1.LineNum) as ZeroCount from dbo.INV1 i1 WITH(NOLOCK) where i1.DocEntry = " + selectedRecord.Invoice.DocEntry +
                                    " and coalesce(i1.[U_InfoW2LNo],0) = 0 and i1.TreeType in ('S','N') " +
                                   "group by i1.DocEntry, i1.DocDate";
                                using (SqlCommand command = new SqlCommand(oQry, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {

                                        if (reader.Read())
                                        {
                                            string oValue = reader["ZeroCount"].ToString();
                                            int NoFound = 0;
                                            try
                                            {
                                                NoFound = Convert.ToInt32(oValue);
                                            }
                                            catch
                                            {
                                                NoFound = 0;
                                            }
                                            if (NoFound <= 0)
                                            {
                                                bValid = true;
                                            }
                                        }
                                        else
                                        {
                                            bValid = true;
                                        }
                                    }
                                }
                            }
                            catch (Exception b)
                            {
                                _logger.Error("Error checking for zero lines numbers" + b.Message);
                            }
                            if (bValid)
                            {
                                // 12-02-2019 end
                                Edi810HeaderRecord edi810HeaderRecord = new Edi810HeaderRecord();
                                response.Edi810Records.Add(edi810HeaderRecord);

                                edi810HeaderRecord.BuyerName = selectedRecord.Edi850HeaderRecord.BuyerName;
                                edi810HeaderRecord.CardCode = request.CardCode;


                                edi810HeaderRecord.CarrierCode = selectedRecord.Invoice.U_InfoW2Cc;
                                edi810HeaderRecord.ConditionDescription = selectedRecord.Edi850HeaderRecord.ConditionDescription;
                                edi810HeaderRecord.DeliveryPhoneNumber = selectedRecord.Edi850HeaderRecord.DeliveryPhoneNumber;
                                edi810HeaderRecord.Department = selectedRecord.Edi850HeaderRecord.Department;

                                edi810HeaderRecord.PaymentMethod = selectedRecord.Edi850HeaderRecord.PaymentMethod;
                                edi810HeaderRecord.PromotionChargeCode = selectedRecord.Edi850HeaderRecord.PromotionChargeCode;
                                // 08-28-2019 begin
                                if (selectedRecord.Edi850HeaderRecord.PurchaseOrderDate == null ||
                                    selectedRecord.Edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                                {
                                    edi810HeaderRecord.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.RecordDate;
                                }
                                else
                                { // 08-28-2019 end
                                    edi810HeaderRecord.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.PurchaseOrderDate;
                                } // 08-28-2019
                                edi810HeaderRecord.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                                edi810HeaderRecord.ReplenishmentNumber = selectedRecord.Edi850HeaderRecord.ReplenishmentNumber;
                                edi810HeaderRecord.RequestedDeliveryDate = selectedRecord.Edi850HeaderRecord.RequestedDeliveryDate;
                                edi810HeaderRecord.RequestedShipDate = selectedRecord.Edi850HeaderRecord.RequestedShipDate;

                                edi810HeaderRecord.ShipDate = selectedRecord.Invoice.DocDate; // 08-30-2019
                                // 03-06-2019 begin
                                if (selectedRecord.Invoice.CardCode == "TSC" || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSC"))
                                {
                                    edi810HeaderRecord.ShipToLocationCode = "9999";
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToAddress1))
                                {
                                    edi810HeaderRecord.BillToAddress1 = "";
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToAddress1 = selectedRecord.Edi850HeaderRecord.BillToAddress1;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToAddress2))
                                {
                                    edi810HeaderRecord.BillToAddress2 = "";
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToAddress2 = selectedRecord.Edi850HeaderRecord.BillToAddress2;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToCity))
                                {
                                    edi810HeaderRecord.BillToCity = "";
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToCity = selectedRecord.Edi850HeaderRecord.BillToCity;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToState))
                                {
                                    edi810HeaderRecord.BillToState = "";
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToState = selectedRecord.Edi850HeaderRecord.BillToState;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToZip))
                                {
                                    edi810HeaderRecord.BillToZip = "";
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToZip = selectedRecord.Edi850HeaderRecord.BillToZip;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToCountry))
                                {
                                    edi810HeaderRecord.BillToCountry = "";
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToCountry = selectedRecord.Edi850HeaderRecord.BillToCountry;
                                }
                                if (String.IsNullOrWhiteSpace(selectedRecord.Edi850HeaderRecord.BillToName))
                                {
                                    edi810HeaderRecord.BillToName = selectedRecord.Invoice.CardName;
                                }
                                else
                                {
                                    edi810HeaderRecord.BillToName = selectedRecord.Edi850HeaderRecord.BillToName;
                                }
                                // 03-06-2019 end
                                edi810HeaderRecord.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                                edi810HeaderRecord.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                                edi810HeaderRecord.ShipToAttention = selectedRecord.Edi850HeaderRecord.ShipToAttention;
                                edi810HeaderRecord.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                                edi810HeaderRecord.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                                edi810HeaderRecord.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                                edi810HeaderRecord.ShipToStoreLocation = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation; // 11-02-2016
                                edi810HeaderRecord.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                                edi810HeaderRecord.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                                edi810HeaderRecord.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                                edi810HeaderRecord.TruckLoadNumber = selectedRecord.Edi850HeaderRecord.TruckLoadNumber;
                                edi810HeaderRecord.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                                edi810HeaderRecord.InvoiceNumber = selectedRecord.Invoice.DocNum;
                                // 07-19-2017 begin
                                edi810HeaderRecord.ShipmentWeight = selectedRecord.Invoice.U_InfoW2SWgt;

                                // 01-21-2018 begin
                                if (selectedRecord.Invoice.U_InfoW2CnNo < 0)
                                {
                                    edi810HeaderRecord.ConfirmationNo = 0;
                                }
                                else
                                {  // 01-21-2018 end
                                    edi810HeaderRecord.ConfirmationNo = selectedRecord.Invoice.U_InfoW2CnNo;
                                }
                                edi810HeaderRecord.OrderBuyCode = selectedRecord.Invoice.U_InfoW2BCode;
                                edi810HeaderRecord.OrderBuyName = selectedRecord.Invoice.U_InfoW2BName;
                                edi810HeaderRecord.OrderBuyAddr1 = selectedRecord.Invoice.U_InfoW2BAd1;
                                edi810HeaderRecord.OrderBuyAddr2 = selectedRecord.Invoice.U_InfoW2BAd2;
                                edi810HeaderRecord.OrderBuyCity = selectedRecord.Invoice.U_InfoW2BCity;
                                edi810HeaderRecord.OrderBuyState = selectedRecord.Invoice.U_InfoW2BState;
                                edi810HeaderRecord.OrderBuyZip = selectedRecord.Invoice.U_InfoW2BZip;
                                edi810HeaderRecord.OrderBuyCountryCd = selectedRecord.Invoice.U_InfoW2BCntry;
                                edi810HeaderRecord.JobNumber = selectedRecord.Invoice.U_InfoW2Job;
                                // 07-19-2017 end
                                edi810HeaderRecord.InvoiceDueDt = selectedRecord.Invoice.DocDueDate; //05-25-2018
                                // 08-15-2017 begin
                                if (selectedRecord.Invoice.CardCode == "ACE" || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("ACE"))
                                {
                                    edi810HeaderRecord.OrderType = "DI";
                                    DateTime oDocDate = selectedRecord.Invoice.DocDate;
                                    try
                                    {
                                        double oDays = Convert.ToDouble(selectedRecord.Invoice.U_InfoW2TDiscDays);
                                        oDocDate.AddDays(oDays);
                                    }
                                    catch (Exception)
                                    {
                                        oDocDate = selectedRecord.Invoice.DocDueDate;
                                    }
                                    edi810HeaderRecord.DiscountDueDt = oDocDate;
                                    oDocDate = selectedRecord.Invoice.DocDate;
                                    try
                                    {
                                        if (String.IsNullOrWhiteSpace(selectedRecord.Invoice.U_InfoW2TDays.ToString()))
                                        {
                                            oDocDate = Convert.ToDateTime(edi810HeaderRecord.DiscountDueDt);
                                        }
                                        else
                                        {
                                            double oDays = Convert.ToDouble(selectedRecord.Invoice.U_InfoW2TDays);
                                            oDocDate.AddDays(oDays);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        oDocDate = selectedRecord.Invoice.DocDueDate;
                                    }
                                    edi810HeaderRecord.InvoiceDueDt = oDocDate;
                                    try
                                    {
                                        decimal oDocTotal = selectedRecord.Invoice.DocTotal;
                                        decimal oPct = (decimal)selectedRecord.Invoice.U_InfoW2TDisc;
                                        oPct = oPct / 100;
                                        edi810HeaderRecord.TermsDiscountAmt = oDocTotal * oPct;
                                    }
                                    catch
                                    {
                                        edi810HeaderRecord.TermsDiscountAmt = 0;
                                    }
                                }
                                // 08-15-2017 end

                                // 07-31-2019 begin
                                string oTrackingNo = selectedRecord.Invoice.U_Info_BOL;
                                // 04-22-2021 begin set U_Info_BOL from TrackNo if null
                                if ((String.IsNullOrWhiteSpace(selectedRecord.Invoice.U_Info_BOL) || selectedRecord.Invoice.U_Info_BOL.Trim().Length == 0)
                                     && (!String.IsNullOrWhiteSpace(selectedRecord.Invoice.TrackNo) && selectedRecord.Invoice.TrackNo.Trim().Length > 0))
                                {
                                    selectedRecord.Invoice.U_Info_BOL = selectedRecord.Invoice.TrackNo.Trim();
                                }
                                ;
                                // 04-22-2021 end
                                if ((!String.IsNullOrWhiteSpace(selectedRecord.Invoice.TrackNo) && selectedRecord.Invoice.TrackNo.Trim().Length > 0)
                                    && selectedRecord.Invoice.TrackNo.Trim().Length > selectedRecord.Invoice.U_Info_BOL.Length)
                                {
                                    oTrackingNo = selectedRecord.Invoice.TrackNo;
                                }
                                // 07-29-2021 begin
                                else if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL") &&
                                    (String.IsNullOrWhiteSpace(selectedRecord.Invoice.TrackNo) || selectedRecord.Invoice.TrackNo.Trim().Length == 0)
                                    && !(String.IsNullOrWhiteSpace(selectedRecord.Invoice.U_Info_BOL)) && selectedRecord.Invoice.U_Info_BOL.Trim().Length > 0)
                                {
                                    oTrackingNo = selectedRecord.Invoice.U_Info_BOL;
                                    selectedRecord.Invoice.TrackNo = selectedRecord.Invoice.U_Info_BOL;
                                }
                                // 07-29-2021 end
                                // 05-22-2020 begin
                                if (String.IsNullOrWhiteSpace(oTrackingNo) || oTrackingNo.Trim().Length == 0)
                                {
                                    oTrackingNo = "";
                                }
                                else
                                {
                                    // 05-22-2020 end
                                    oTrackingNo = oTrackingNo.Replace(", ", ",");
                                    oTrackingNo = oTrackingNo.Replace(" ,", ",");
                                    oTrackingNo = oTrackingNo.Replace(",", ";"); // 07-29-2021
                                    // 09-15-2021 begin
                                    if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                    {
                                        oTrackingNo = oTrackingNo.Replace("-", "");
                                    }
                                    // 09-15-2021 end
                                } // 05-22-2020
                                /*
                                //04-22-2021 begin
                                if (String.IsNullOrWhiteSpace(oTrackingNo))
                                {

                                }
                                // 04-22-2021 end
                                 */
                                String[] oTrackNos = new String[1];
                                if (!String.IsNullOrWhiteSpace(oTrackingNo))
                                {
                                    //oTrackNos = oTrackingNo.Split(',');
                                    oTrackNos = oTrackingNo.Split(';'); // 07-20-2021
                                    if (oTrackNos.Length == 0)
                                    {
                                        oTrackNos = new String[1];
                                        oTrackNos[0] = oTrackingNo.Trim();
                                    }
                                }

                                int iNextTrack = 0;
                                // 07-31-2019 end
                                // 03-08-2019 begin
                                if (selectedRecord.Invoice.CardCode.StartsWith("TSC"))
                                {
                                    DateTime oDocDate = selectedRecord.Invoice.DocDueDate;
                                    // 03-04/2021 begin
                                    /*
                                    edi810HeaderRecord.TermsType = "08";
                                    edi810HeaderRecord.InvoiceDueDt = selectedRecord.Invoice.DocDueDate;
                                    edi810HeaderRecord.DiscountDueDt = oDocDate.AddDays(30);
                                    decimal TermsDiscountAmt = Decimal.Multiply(selectedRecord.Invoice.DocTotal, Convert.ToDecimal(.03));
                                    TermsDiscountAmt = Decimal.Round(TermsDiscountAmt, 2);
                                    edi810HeaderRecord.TermsDiscountAmt = TermsDiscountAmt;
                                    edi810HeaderRecord.TermsDescription = "3% 30 Net 31";
                                    edi810HeaderRecord.TermsDiscountDays = 30;
                                    edi810HeaderRecord.NetDaysDue = 31;
                                     * */
                                    edi810HeaderRecord.InvoiceDueDt = selectedRecord.Invoice.DocDueDate;
                                    edi810HeaderRecord.TermsType = selectedRecord.Edi850HeaderRecord.TermsType;
                                    edi810HeaderRecord.TermsDescription = selectedRecord.Edi850HeaderRecord.TermsDescription;
                                    if (!(selectedRecord.Edi850HeaderRecord.TermsNetDays == null))
                                    {
                                        edi810HeaderRecord.NetDaysDue = (int)selectedRecord.Edi850HeaderRecord.TermsNetDays;
                                    }
                                    // 03-04/2021 end
                                    edi810HeaderRecord.ShipDate = selectedRecord.Invoice.DocDate;
                                    // 03-04/2021 begin
                                    // remove Allowance from 810
                                    /*
                                    //edi810HeaderRecord.AllowanceCode = "A530";
                                    edi810HeaderRecord.AllowanceCode = "C000"; // 03-02-2021
                                    decimal docAllowances = Decimal.Multiply(selectedRecord.Invoice.DocTotal, Convert.ToDecimal(.03));
                                    docAllowances = Decimal.Round(docAllowances, 2);
                                    //edi810HeaderRecord.AllowanceAmt = "3";
                                    edi810HeaderRecord.AllowanceAmt = docAllowances.ToString(); // 02-22-2021  changed per TSC EDI
                                    edi810HeaderRecord.AllowanceHCode = "02";
                                    //edi810HeaderRecord.AllowanceDescription = "RTM Allowance";
                                    edi810HeaderRecord.AllowanceDescription = "Defective Allowance"; // 03-02-2021
                                    edi810HeaderRecord.AllowanceType = "A"; // 03-02-2021
                                    decimal docTotalAftDisc = selectedRecord.Invoice.DocTotal - docAllowances;
                                    edi810HeaderRecord.TotalAfterAllowances = Convert.ToDouble(docTotalAftDisc);
                                     */
                                    edi810HeaderRecord.TotalDue = Convert.ToDouble(selectedRecord.Invoice.DocTotal);
                                    edi810HeaderRecord.TotalAfterAllowances = edi810HeaderRecord.TotalDue;
                                    // 03-04-2021 end
                                }
                                else
                                {
                                    edi810HeaderRecord.TermsType = String.Empty;
                                }

                                edi810HeaderRecord.TrackingNumber = selectedRecord.Invoice.TrackNo;
                                // 03-08-2019 end

                                // 07-29-2019 begin
                                if (selectedRecord.Invoice.CardCode == ("HomeDepot") || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                {
                                    edi810HeaderRecord.ShipDate = selectedRecord.Invoice.DocDate;
                                    // 07-29-2021 begin
                                    if ((String.IsNullOrWhiteSpace(selectedRecord.Invoice.TrackNo) || selectedRecord.Invoice.TrackNo.Trim().Length == 0)
                                    && !(String.IsNullOrWhiteSpace(selectedRecord.Invoice.U_Info_BOL)) && selectedRecord.Invoice.U_Info_BOL.Trim().Length > 0)
                                    {
                                        edi810HeaderRecord.TrackingNumber = selectedRecord.Invoice.U_Info_BOL;
                                        // 09-15-2021 begin
                                        if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            edi810HeaderRecord.TrackingNumber = edi810HeaderRecord.TrackingNumber.Replace("-", "");
                                        }
                                        // 09-15-2021 end
                                    }
                                    // 07-29-2021 end
                                }
                                if (selectedRecord.Invoice.CardCode.ToUpper() == "WAYFAIR" || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("WAYFAIR"))
                                {
                                    edi810HeaderRecord.CustomerOrderNumber = selectedRecord.Edi850HeaderRecord.CustomerOrderNumber;


                                    // get Supplier Name, Phone, & Fax
                                    try
                                    {
                                        using (SqlConnection sqlConn = new SqlConnection(GetConnectionString(oConnectionName)))
                                        {
                                            sqlConn.Open();
                                            // 08-04-2022 begin
                                            string oWhsQry = "select t0.[InvoiceNo], t0.[IntInvNo], t0.[WhsCode], t0.[ShipFromName], t0.[ShipFromAddress1], t0.[ShipFromAddress2], " +
                                                                      " t0.[ShipFromCity], t0.[ShipFromState], t0.[ShipFromZip], t0.[ShipFromCountry], t0.[ShipFromPhone], t0.[ShipFromFax], " +
                                                                      "(select top 1 ShipDate from dbo.INV1 i0 WITH(NOLOCK) where i0.DocEntry = t0.IntInvNo) 'ShipDate' " +
                                                                      "from [Infocus_810_Whs] t0  WITH(NOLOCK) where t0.IntInvNo = " + selectedRecord.Invoice.DocEntry;
                                            //using (SqlCommand command = new SqlCommand("select t0.*, (select top 1 ShipDate from dbo.INV1  WITH(NOLOCK) where DocEntry = t0.IntInvNo) 'ShipDate' from [Infocus_810_Whs] t0  WITH(NOLOCK) where IntInvNo = " + selectedRecord.Invoice.DocEntry, sqlConnection))
                                            using (SqlCommand command = new SqlCommand(oWhsQry, sqlConnection))
                                            // 08-04-2022 end
                                            {
                                                using (SqlDataReader reader = command.ExecuteReader())
                                                {
                                                    if (!reader.Read())
                                                    {
                                                        _logger.Error("Could not set Supplier Name, Phone #, and Fax# for Invoice # " + selectedRecord.Invoice.DocNum);
                                                        edi810HeaderRecord.SupplierName = "Corsan";
                                                        //record.ShipFromCode = "9999";
                                                    }
                                                    else
                                                    {
                                                        //record.ShipFromCode = "9999";
                                                        try
                                                        {
                                                            edi810HeaderRecord.SupplierName = reader["ShipFromName"].ToString();
                                                        }
                                                        catch
                                                        {

                                                        }
                                                        try
                                                        {
                                                            edi810HeaderRecord.SupplierPhone = reader["ShipFromPhone"].ToString();
                                                        }
                                                        catch
                                                        {

                                                        }
                                                        try
                                                        {
                                                            edi810HeaderRecord.SupplierFax = reader["ShipFromFax"].ToString();
                                                        }
                                                        catch
                                                        {

                                                        }
                                                        try
                                                        {
                                                            string oValue = reader["ShipDate"].ToString();
                                                            edi810HeaderRecord.ShipDate = Convert.ToDateTime(oValue);
                                                        }
                                                        catch (Exception dt)
                                                        {
                                                            _logger.Error("Unable to set Ship Date => " + dt.Message);
                                                            edi810HeaderRecord.ShipDate = selectedRecord.Invoice.DocDate; // 1-15-2021
                                                        }

                                                    }
                                                }
                                            }
                                            sqlConn.Close();
                                        }
                                    }
                                    catch (Exception sf)
                                    {
                                        string oErr = sf.Message;
                                        _logger.Debug("Could not set Supplier Name, Phone #, and Fax # for Invoice# " + selectedRecord.Invoice.DocNum);
                                        edi810HeaderRecord.SupplierName = "Corsan";
                                    }
                                }
                                // 07-29-2019 end

                                //foreach(Edi850DetailRecord detail850Record in selectedRecord.Edi850HeaderRecord.Details)
                                //{
                                //    Edi810DetailRecord detail810Record = new Edi810DetailRecord();
                                //    edi810HeaderRecord.Details.Add(detail810Record);

                                //    detail810Record.BuyerItemCode = detail850Record.BuyerItemCode;
                                //    detail810Record.ItemDescription = detail850Record.ItemDescription;
                                //    detail810Record.LineNumber = detail850Record.LineNumber;
                                //    detail810Record.Quantity = detail850Record.Quantity;
                                //    detail810Record.UnitOfMeasure = detail850Record.UnitOfMeasure;
                                //    detail810Record.VendorItemCode = detail850Record.VendorItemCode;

                                //    var foundInvoiceLine = (from v in selectedRecord.Invoice.InvoiceLines
                                //                             where v.LineNumber850 == detail850Record.LineNumber
                                //                             select v).FirstOrDefault();
                                //    if(foundInvoiceLine != null)
                                //    {
                                //        detail810Record.QuantityShipped = Convert.ToDouble(foundInvoiceLine.Quantity);
                                //        detail810Record.UnitPrice = Convert.ToDouble(foundInvoiceLine.Price);
                                //    }
                                //    else
                                //    {
                                //        detail810Record.QuantityShipped = 0;
                                //        detail810Record.UnitPrice = detail850Record.UnitPrice;
                                //    }
                                //}
                                foreach (InvoiceLine invoiceLine in selectedRecord.Invoice.InvoiceLines)
                                {
                                    //string oAltVendorItem = getAltVendorItem(oConnectionName, selectedRecord.Invoice.DocEntry, invoiceLine);
                                    int headerId = selectedRecord.Edi850HeaderRecord.HeaderId;
                                    // 02-27-2022 begin
                                    //int ediLineNo = invoiceLine.LineNumber850;
                                    int ediLineNo = 0;
                                    if (invoiceLine.LineNumber850.ToString().Trim().Length > 0)
                                    {
                                        ediLineNo = Convert.ToInt32(invoiceLine.LineNumber850.ToString());
                                    }
                                    // 02-27-2022 end
                                    if (invoiceLine.TreeType == "N" || invoiceLine.TreeType == "S") // 01-2018
                                    {
                                        decimal oInvLineTotal = invoiceLine.LineTotal;
                                        decimal oInvTotal = selectedRecord.Invoice.DocTotal; // 03-26-2018
                                        string oIsInventoryItm = "Y";
                                        if (invoiceLine.TreeType == "N" || invoiceLine.TreeType == "S")
                                        {
                                            try
                                            {
                                                string oLineQuery = "select  t0.Linenum, t0.ItemCode, t0.LineTotal, t1.InvntItem, " +
                                                                    "coalesce(t2.LineTotal,0) CalcTotal " +
                                                                    "from dbo.INV1 t0  WITH(NOLOCK) left join dbo.OITM t1  WITH(NOLOCK) on t0.ItemCode = t1.ItemCode " +
                                                                    "left join dbo.[Infocus_EDI_INV_Line_Totals] t2  WITH(NOLOCK) on t2.DocEntry = t0.DocEntry and t2.LineNum = t0.LineNum " +
                                                                    " where t0.DocEntry = " + invoiceLine.DocEntry + " and t0.LineNum = " + invoiceLine.LineNum;

                                                using (SqlCommand command = new SqlCommand(oLineQuery, sqlConnection))
                                                {
                                                    using (SqlDataReader reader = command.ExecuteReader())
                                                    {
                                                        if (!reader.Read())
                                                        {
                                                            oInvLineTotal = invoiceLine.LineTotal;
                                                            oIsInventoryItm = "Y";
                                                        }
                                                        else
                                                        {
                                                            oIsInventoryItm = Convert.ToString(reader[3]);
                                                            oInvLineTotal = Convert.ToDecimal(reader[4]);
                                                        }
                                                    }
                                                }
                                            }
                                            catch (Exception iL)
                                            {
                                                string oErrMesg = iL.Message;
                                                oInvLineTotal = invoiceLine.LineTotal;
                                                oIsInventoryItm = "Y";
                                            }
                                        }
                                        if (oIsInventoryItm == "Y" || invoiceLine.TreeType == "S")
                                        {
                                            Edi810DetailRecord detail810Record = new Edi810DetailRecord();
                                            edi810HeaderRecord.Details.Add(detail810Record);
                                            //detail810Record.BuyerItemCode = invoiceLine.ItemCode;
                                            //if (String.IsNullOrWhiteSpace(oAltVendorItem))
                                            //{
                                            detail810Record.VendorItemCode = invoiceLine.ItemCode; // 03-14-2017
                                            // 06-21-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                detail810Record.VendorItemCode = detail810Record.VendorItemCode.ToUpper();
                                            }
                                            // 06-21-2024 end
                                            /*}
                                            else
                                            {
                                                detail810Record.VendorItemCode = oAltVendorItem;
                                            }*/

                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            // detail810Record.ItemDescription = invoiceLine.Dscription;
                                            //ring oItmDesc = invoiceLine.Dscription;
                                            string oItmDesc = "";
                                            try
                                            {
                                                oItmDesc = invoiceLine.Dscription;
                                            }
                                            catch
                                            {
                                                oItmDesc = "";
                                            }
                                            if (!String.IsNullOrWhiteSpace(oItmDesc))
                                            {
                                                oItmDesc = oItmDesc.Replace('"', ' ');
                                                // 10-22-2021 begin
                                                oItmDesc = oItmDesc.Replace("  ", " ");
                                                oItmDesc = oItmDesc.Trim();
                                                if (oItmDesc.Length > 80)
                                                {
                                                    oItmDesc = oItmDesc.Substring(0, 80);
                                                }
                                                // 10-22-2021 end
                                            }
                                            else
                                            {
                                                oItmDesc = "";
                                            }
                                            detail810Record.ItemDescription = oItmDesc;
                                            // 10-14-2021 end
                                            // 02-27-2022 begin
                                            //detail810Record.LineNumber = invoiceLine.LineNumber850;
                                            try
                                            {
                                                detail810Record.LineNumber = Convert.ToInt32(invoiceLine.LineNumber850.ToString());
                                            }
                                            catch (Exception il)
                                            {
                                                _logger.Error("Error getting 850 Line Number for Inv DocEntry " + selectedRecord.Invoice.DocEntry.ToString() + " =>" + il.Message);
                                            }
                                            // 02-27-2022 end

                                            detail810Record.Quantity = Convert.ToDouble(invoiceLine.Quantity);
                                            detail810Record.QuantityShipped = Convert.ToDouble(invoiceLine.Quantity);
                                            // 02-28-2018 begin
                                            if (oInvLineTotal * invoiceLine.Quantity > invoiceLine.LineTotal || oInvLineTotal * invoiceLine.Quantity == oInvTotal)
                                            {
                                                detail810Record.UnitPrice = Convert.ToDouble(oInvLineTotal);
                                            }
                                            else
                                            {
                                                // 02-28-2018 end
                                                detail810Record.UnitPrice = Convert.ToDouble(invoiceLine.Price);
                                            } // 02-28-2018
                                            // 01-24-2018 begin
                                            if (detail810Record.UnitPrice == 0 && invoiceLine.TreeType == "S")
                                            {
                                                detail810Record.UnitPrice = getBOMPrice(oConnectionName, invoiceLine.ItemCode);
                                            }
                                            // 01-24-2018 end
                                            detail810Record.UnitOfMeasure = invoiceLine.UomCode;
                                            if (String.IsNullOrWhiteSpace(detail810Record.UnitOfMeasure)
                                                || detail810Record.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                            {
                                                detail810Record.UnitOfMeasure = "EA";
                                            }
                                            // 07-31-2019 begin

                                            // 01-22-2018 begin
                                            try
                                            {
                                                // 01-22-2018 end
                                                // 03-07-2018 begin
                                                string oVendorItemCode = get850DetailVendorItem(oConnectionName, selectedRecord.Edi850HeaderRecord.HeaderId, invoiceLine.LineNumber850.ToString());
                                                if (!String.IsNullOrWhiteSpace(oVendorItemCode))
                                                {
                                                    detail810Record.VendorItemCode = oVendorItemCode;
                                                    // 06-21-2024 begin
                                                    if (BPList.Contains(oSBOCardCode))
                                                    {
                                                        detail810Record.VendorItemCode = detail810Record.VendorItemCode.ToUpper();
                                                    }
                                                    // 06-21-2024 end
                                                }
                                                // 03-07-2018 end
                                                // 03-08-2019 begin
                                                string oItemUPC = get850DetailItemUPC(oConnectionName, selectedRecord.Edi850HeaderRecord.HeaderId, invoiceLine.LineNumber850.ToString());
                                                if (!String.IsNullOrWhiteSpace(oItemUPC))
                                                {
                                                    detail810Record.ItemUPC = oItemUPC;
                                                }
                                                double retailPrice = get850DetailRetailPrice(oConnectionName, selectedRecord.Edi850HeaderRecord.HeaderId, invoiceLine.LineNumber850.ToString());
                                                try
                                                {
                                                    detail810Record.RetailPrice = Convert.ToDouble(retailPrice);
                                                }
                                                catch (Exception dp)
                                                {
                                                    _logger.Error("Error getting retail price for Inv# " + selectedRecord.Invoice.DocNum + " Ln# " + invoiceLine.LineNum + " => " + dp.Message);
                                                }
                                                if (String.IsNullOrWhiteSpace(invoiceLine.U_InfoW2PackNote))
                                                {
                                                    detail810Record.PackingNotes = "";
                                                }
                                                else
                                                {
                                                    detail810Record.PackingNotes = invoiceLine.U_InfoW2PackNote;
                                                }
                                                // 03-08-2019 end
                                                // 01-22-2018 begin

                                                string oBuyerItemCode = get850DetailBuyerItem(oConnectionName, selectedRecord.Edi850HeaderRecord.HeaderId, invoiceLine.LineNumber850.ToString());
                                                if (String.IsNullOrWhiteSpace(oBuyerItemCode))
                                                {
                                                    if (detail810Record.BuyerItemCode == null || detail810Record.BuyerItemCode.Trim().Length == 0)
                                                    {
                                                        // 01-22-2018 end
                                                        if (invoiceLine.SubCatNum == null || invoiceLine.SubCatNum.Trim().Length == 0)
                                                        {
                                                            using (SqlCommand command = new SqlCommand("select top 1 s0.Substitute from dbo.OSCN s0  WITH(NOLOCK) where s0.ItemCode = '" + invoiceLine.ItemCode + "' and s0.CardCode = '" + selectedRecord.Invoice.CardCode + "'", sqlConnection))
                                                            {
                                                                using (SqlDataReader reader = command.ExecuteReader())
                                                                {
                                                                    if (!reader.Read())
                                                                    {
                                                                        throw new WebApiException("Could not locate Vendor Item Code for Item Code " + invoiceLine.ItemCode);
                                                                    }
                                                                    //detail810Record.VendorItemCode = (String)reader[0];
                                                                    detail810Record.BuyerItemCode = (String)reader[0];  // 03-14-2017
                                                                    if (BPList.Contains(oSBOCardCode))
                                                                    {
                                                                        detail810Record.BuyerItemCode = detail810Record.BuyerItemCode.ToUpper();
                                                                    }
                                                                    // 06-21-2024 end
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            detail810Record.BuyerItemCode = invoiceLine.SubCatNum;
                                                            // 06-21-2024 begin
                                                            if (BPList.Contains(oSBOCardCode))
                                                            {
                                                                detail810Record.BuyerItemCode = detail810Record.BuyerItemCode.ToUpper();
                                                            }
                                                            // 06-21-2024 end
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    detail810Record.BuyerItemCode = oBuyerItemCode;
                                                    // 06-21-2024 begin
                                                    if (BPList.Contains(oSBOCardCode))
                                                    {
                                                        detail810Record.BuyerItemCode = detail810Record.BuyerItemCode.ToUpper();
                                                    }
                                                    // 06-21-2024 end
                                                }
                                                if (String.IsNullOrWhiteSpace(detail810Record.BuyerItemCode))
                                                {
                                                    detail810Record.BuyerItemCode = detail810Record.VendorItemCode;
                                                    // 06-21-2024 begin
                                                    if (BPList.Contains(oSBOCardCode))
                                                    {
                                                        detail810Record.BuyerItemCode = detail810Record.BuyerItemCode.ToUpper();
                                                    }
                                                    // 06-21-2024 end
                                                }
                                                // 01-22-2018 begin
                                            }
                                            catch (Exception bc)
                                            {
                                                string oErrMsg = bc.Message;
                                            }
                                            // 07-31-2019 begin
                                            detail810Record.TrackingNumber = invoiceLine.TrackingNo;
                                            if (String.IsNullOrWhiteSpace(detail810Record.TrackingNumber))
                                            {
                                                if (iNextTrack < oTrackNos.Length)
                                                {
                                                    detail810Record.TrackingNumber = oTrackNos[iNextTrack];
                                                    iNextTrack = iNextTrack + 1;
                                                }
                                                else
                                                {
                                                    detail810Record.TrackingNumber = oTrackNos[0];
                                                }
                                            }
                                            // 07-31-2019 end
                                        } // 02-28-2018
                                    }
                                }
                                /* Must figure these out */
                                edi810HeaderRecord.FinalShipToLocation = String.Empty;
                                edi810HeaderRecord.BillOfLading = selectedRecord.Invoice.U_Info_BOL;
                                // 09-15-2021 begin
                                if (selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                {
                                    edi810HeaderRecord.BillOfLading = edi810HeaderRecord.BillOfLading.Replace("-", "");
                                }
                                // 09-15-2021 end
                                String groupNumSql = String.Format(PaymentGroupQuery, selectedRecord.Invoice.GroupNum);
                                //_logger.Debug("Executing SQL: " + groupNumSql);
                                using (SqlCommand command = new SqlCommand(groupNumSql, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        try
                                        {
                                            if (!reader.Read())
                                            {
                                                throw new WebApiException("Could not locate Payment Group with GroupNum " + selectedRecord.Invoice.GroupNum);
                                            }
                                            edi810HeaderRecord.TermsDescription = (String)reader["PymntGroup"];
                                            edi810HeaderRecord.NetDaysDue = (Int16)reader["ExtraDays"];
                                        }
                                        catch (Exception e)
                                        {
                                            _logger.Debug(" Error getting payment group =>" + e.Message);
                                        }
                                    }
                                }
                                //edi810HeaderRecord.TermsType = String.Empty;
                                // 1-25-2021 begin
                                if (selectedRecord.Invoice.CardCode == "TSC" || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSC"))
                                {
                                    edi810HeaderRecord.TermsDescription = selectedRecord.Edi850HeaderRecord.TermsDescription;
                                    edi810HeaderRecord.TermsType = selectedRecord.Edi850HeaderRecord.TermsType;
                                    int iTerms = 0;
                                    try
                                    {
                                        iTerms = Convert.ToInt32(selectedRecord.Edi850HeaderRecord.TermsNetDays);
                                    }
                                    catch
                                    {
                                        iTerms = 60;
                                    }
                                    if (iTerms <= 0)
                                    {
                                        iTerms = 60;
                                    }
                                    edi810HeaderRecord.NetDaysDue = iTerms;
                                    edi810HeaderRecord.InvoiceDueDt = selectedRecord.Invoice.DocDate.AddDays(iTerms);
                                    if (selectedRecord.Edi850HeaderRecord.TermsType == "08")
                                    {
                                        int iDiscDays = 0;
                                        if (!(selectedRecord.Edi850HeaderRecord.TermsDiscountDays == null))
                                        {
                                            try
                                            {
                                                iDiscDays = Convert.ToInt32(selectedRecord.Edi850HeaderRecord.TermsDiscountDays);
                                            }
                                            catch
                                            {
                                                iDiscDays = 0;
                                            }
                                        }
                                        if (iDiscDays > 0)
                                        {
                                            edi810HeaderRecord.TermsDiscountDays = iDiscDays;
                                        }
                                        decimal discAmt = 0;
                                        try
                                        {
                                            if (!(selectedRecord.Edi850HeaderRecord.TermsDiscount == null))
                                            {
                                                edi810HeaderRecord.TermsDiscountAmt = Convert.ToDecimal("0.00");
                                            }
                                            else
                                            {
                                                edi810HeaderRecord.TermsDiscountAmt = Convert.ToDecimal(selectedRecord.Edi850HeaderRecord.TermsDiscount);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            edi810HeaderRecord.TermsDiscountAmt = discAmt;
                                        }
                                    }
                                    else
                                    {
                                        edi810HeaderRecord.TermsDiscountDays = 0;
                                        edi810HeaderRecord.TermsDiscountAmt = Convert.ToDecimal("0");

                                    }
                                }
                                // 01-25-2021 end
                                edi810HeaderRecord.TotalDue = Convert.ToDouble(selectedRecord.Invoice.DocTotal);
                                edi810HeaderRecord.ShipmentPaymentMethod = edi810HeaderRecord.PaymentMethod;
                                edi810HeaderRecord.CarrierName = String.Empty;

                                edi810HeaderRecord.FreightCharge = Convert.ToDouble(selectedRecord.Invoice.TotalExpns);
                                try
                                {  // 02-03-2021
                                    if (selectedRecord.Invoice.TrnspCode > 0)
                                    {
                                        String shipmentSql = String.Format(ShipmentQuery, selectedRecord.Invoice.TrnspCode);
                                        //_logger.Debug("Executing SQL: " + shipmentSql);
                                        using (SqlCommand command = new SqlCommand(shipmentSql, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (!reader.Read())
                                                {
                                                    throw new WebApiException("Could not locate OSHP with TrnspCode " + selectedRecord.Invoice.TrnspCode);
                                                }
                                                edi810HeaderRecord.CarrierName = (String)reader[0];
                                            }
                                        }
                                    }
                                    // 02-03-2021 begin
                                }
                                catch
                                {
                                    if (!String.IsNullOrWhiteSpace(selectedRecord.Invoice.U_InfoW2Cc) && selectedRecord.Invoice.U_InfoW2Cc.Trim().Length > 0)
                                    {
                                        String shipmentSql = "select t0.TrnspName from dbo.OSHP t0 WITH(NOLOCK) where t0.WebSite = '" + selectedRecord.Invoice.U_InfoW2Cc.Trim() + "'";
                                        //_logger.Debug("Executing SQL: " + shipmentSql);
                                        using (SqlCommand command = new SqlCommand(shipmentSql, sqlConnection))
                                        {
                                            using (SqlDataReader reader = command.ExecuteReader())
                                            {
                                                if (!reader.Read())
                                                {
                                                    throw new WebApiException("Could not locate OSHP with Website " + selectedRecord.Invoice.U_InfoW2Cc);
                                                }
                                                edi810HeaderRecord.CarrierName = (String)reader[0];
                                            }
                                        }
                                    }
                                }
                                if (selectedRecord.Invoice.CardCode == "TSC" && (String.IsNullOrWhiteSpace(edi810HeaderRecord.CarrierName) || selectedRecord.Invoice.U_InfoW2Cc.Trim().Length == 0))
                                {
                                    String shipmentSql = "select t0.TrnspName from dbo.OSHP t0 WITH(NOLOCK) where t0.WebSite = '" + selectedRecord.Invoice.U_InfoW2Cc.Trim() + "'";
                                    //_logger.Debug("Executing SQL: " + shipmentSql);
                                    using (SqlCommand command = new SqlCommand(shipmentSql, sqlConnection))
                                    {
                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            if (!reader.Read())
                                            {
                                                throw new WebApiException("Could not locate OSHP with Website " + selectedRecord.Invoice.U_InfoW2Cc);
                                            }
                                            edi810HeaderRecord.CarrierName = (String)reader[0];
                                        }
                                    }
                                }
                                if (selectedRecord.Invoice.CardCode == "TSC" && (String.IsNullOrWhiteSpace(edi810HeaderRecord.CarrierName) || selectedRecord.Invoice.U_InfoW2Cc.Trim().Length == 0))
                                {
                                    edi810HeaderRecord.CarrierName = edi810HeaderRecord.CarrierCode;
                                }
                                // 02-03-2021 end
                                if (iPostProcess810Record != null)
                                {
                                    iPostProcess810Record.OnPostProcess810Record(selectedRecord.Invoice, selectedRecord.Edi850HeaderRecord, edi810HeaderRecord);
                                }
                                /* End Figure */
                                // 12-02-2019 begin
                            }
                            else
                            {
                                string oMessage = "Invoice # " + selectedRecord.Invoice.DocNum + " was skipped because it has lines with zero edi line number";
                                _logger.Error(oMessage);
                                selectedRecord.IsError = true;
                                selectedRecord.ErrorMessage = oMessage;
                            }
                            // 12-02-2019 end
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            _logger.Error(ex.Message);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                        }
                    }
                    sqlConnection.Close();
                }
                DateTime now = DateTime.Now;
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate == null)
                            {
                                throw new WebApiException("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                            }
                            if (selectedRecord.IsError)
                            {
                                recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                recordToUpdate.Processed810 = false;
                            }
                            else
                            {
                                recordToUpdate.Processed810 = true;
                                recordToUpdate.Processed810DateTime = now;
                                recordToUpdate.ErrorMessage = String.Empty;
                            }
                            dbContext.SaveChanges(); // 03-30-2019

                        }
                        catch (Exception err)
                        {
                            _logger.Error("Error updating 810 status =>" + err.Message);
                        }
                    }
                    dbContext.SaveChanges();
                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                _logger.Debug(ex.Message);
                response.ErrorMessage = ex.Message;
            }
            _logger.Debug("Returning the 810 response object which contains " + response.Edi810Records.Count + " invoices");
            //_logger.Debug("{@Get810RecordsResponse}", response);
            // 03-29-2022 begin
            if (response.Edi810Records.Count == 0)
            {
                response.ErrorMessage = "No matching 810s found";
            }
            // 03-29-2022 end
            _logger.Debug("Leaving Get810Records for " + request.CardCode);
            return response;
        }

        private IPreProcess810Record Get810PreProcess(String cardCode)
        {
            String preProcess810Record = ConfigurationManager.AppSettings["PreProcess810Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess810Record))
            {
                preProcess810Record = ConfigurationManager.AppSettings["PreProcess810Record"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess810Record))
            {
                try
                {
                    IPreProcess810Record iPreProcess810Record = (IPreProcess810Record)Activator.CreateInstance(Type.GetType(preProcess810Record, true));
                    return iPreProcess810Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess810Record", ex);
                    _logger.Error("Could not instantiate IPreProcess810Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess810Record. Reason: " + ex.Message);
                }
            }
            return null;
        }

        private IPostProcess810Record Get810PostProcess(String cardCode)
        {
            String postProcess810Record = ConfigurationManager.AppSettings["PostProcess810Record-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess810Record))
            {
                postProcess810Record = ConfigurationManager.AppSettings["PostProcess810Record"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess810Record))
            {
                try
                {
                    IPostProcess810Record iPostProcess810Record = (IPostProcess810Record)Activator.CreateInstance(Type.GetType(postProcess810Record, true));
                    return iPostProcess810Record;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess810Record", ex);
                    _logger.Error("Could not instantiate IPostProcess810Record =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess810Record. Reason: " + ex.Message);
                }
            }
            return null;
        }

        // 07-23-2019 begin
        public GetRet180RecordsResponse GetRet180Records(GetRet180RecordsRequest request)
        {
            string oSBOCardCode = getSBOCardCode(request.CardCode);

            List<Edi180WithReturn> listToProcess = new List<Edi180WithReturn>();
            _logger.Debug("Entering GetRet810Records for " + request.CardCode + " : " + request.CardCode);
            //_logger.Debug("Processing the following request object:");
            GetRet180RecordsResponse response = new GetRet180RecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for GetRet810Records";
                response.Successful = false;
                return response;
            }

            try
            {
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {
                    oSBOCardCode = request.CardCode;
                }

                List<Edi180HeaderRecord> listOf180Records = null;
                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                {
                    listOf180Records = dbContext.Edi180HeaderRecords.Include("Details")
                                .Where(x => x.Processed == true
                                    && x.CardCode == request.CardCode
                                    && x.TrxPurpose != "01"
                                    && x.ProcessedReturn180 == false
                                    && x.ReturnOrderKey > 0).ToList();
                    _logger.Debug("There are " + listOf180Records.Count + " 180 records to evaluate");
                    foreach (var record in listOf180Records)
                    {
                        //_logger.Debug("Processing 180 with Key " + record.HeaderId);
                        SReturn returnDoc = FindMatchingReturn(dbContext, record);

                        if (returnDoc != null)
                        {
                            //_logger.Debug("Found 180 line -- Return # " + returnDoc.DocNum);
                            listToProcess.Add(new Edi180WithReturn(record, returnDoc));
                        }
                        else
                        {
                            _logger.Error("180 sales return not found for 180 record with HeaderId " + record.HeaderId);
                        }
                    }
                }

                if (listToProcess.Count == 0)
                {
                    response.Successful = true;
                    return response;
                }
                //To do -Need to filter records, but not sure how to right now
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    sqlConnection.Open();

                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {

                            Edi180RHeaderRecord record = new Edi180RHeaderRecord();
                            response.EdiRet180Records.Add(record);
                            record.CardCode = selectedRecord.Edi180HeaderRecord.CardCode;
                            record.HeaderId = selectedRecord.Edi180HeaderRecord.HeaderId;
                            record.ReturnOrderKey = selectedRecord.SOReturn.DocNum;
                            record.RecordDate = selectedRecord.Edi180HeaderRecord.RecordDate;
                            record.ReceivedDateTime = selectedRecord.Edi180HeaderRecord.ReceivedDateTime;
                            record.ProcessedDateTime = selectedRecord.Edi180HeaderRecord.ProcessedDateTime;
                            record.Processed = selectedRecord.Edi180HeaderRecord.Processed;
                            record.OrderType = selectedRecord.Edi180HeaderRecord.OrderType;
                            record.ChargeCode = selectedRecord.Edi180HeaderRecord.ChargeCode;
                            record.ChargeId = selectedRecord.Edi180HeaderRecord.ChargeId;
                            //record.PurchaseOrderDate = selectedRecord.Edi180HeaderRecord.PurchaseOrderDate;
                            record.PurchaseOrderReference = selectedRecord.Edi180HeaderRecord.PurchaseOrderReference;
                            record.VendorNumber = selectedRecord.Edi180HeaderRecord.VendorNumber;
                            record.ReturnTotal = Convert.ToDouble(selectedRecord.SOReturn.DocTotal);
                            record.SBOCardCode = selectedRecord.SOReturn.CardCode;
                            ICollection<SRLine> returnLines = selectedRecord.SOReturn.SRLines;
                            record.ReferenceId = selectedRecord.Edi180HeaderRecord.ReferenceId;
                            record.TrxPurpose = selectedRecord.Edi180HeaderRecord.TrxPurpose;
                            record.TrxStatus = selectedRecord.Edi180HeaderRecord.TrxStatus;
                            record.RequestDate = selectedRecord.Edi180HeaderRecord.ReceivedDateTime;
                            foreach (SRLine returnLine in selectedRecord.SOReturn.SRLines)
                            {
                                if (returnLine.TreeType == "N")
                                {
                                    Edi180RDetailRecord detail180RetRecord = new Edi180RDetailRecord();
                                    record.Details.Add(detail180RetRecord);
                                    detail180RetRecord.VendorItemCode = returnLine.ItemCode;

                                    detail180RetRecord.LineNumber = returnLine.LineNumber850;
                                    detail180RetRecord.ItemReference = returnLine.LineNum;
                                    if (String.IsNullOrWhiteSpace(returnLine.SubCatNum) || returnLine.SubCatNum.Trim().Length == 0)
                                    {
                                        string oBuyerItem = "";
                                        try
                                        {
                                            string oQry = "select top 1 s0.ItemCode from dbo.OSCN s0 WITH(NOLOCK) where s0.[U_EDI_XREF] = '" + returnLine.ItemCode + "' and s0.CardCode = '" + selectedRecord.SOReturn.CardCode + "'";
                                            using (SqlCommand command = new SqlCommand(oQry, sqlConnection))
                                            {
                                                using (SqlDataReader reader = command.ExecuteReader())
                                                {
                                                    if (!reader.Read())
                                                    {
                                                        _logger.Error("Could not locate Buyer Item Code for Item Code " + returnLine.ItemCode);
                                                    }
                                                    else
                                                    {
                                                        oBuyerItem = (String)reader[0];
                                                    }
                                                }
                                            }
                                        }
                                        catch (Exception b)
                                        {
                                            _logger.Error("Error getting Buyer Item =>" + b.Message);
                                        }
                                        if (String.IsNullOrWhiteSpace(oBuyerItem))
                                        {
                                            detail180RetRecord.BuyerItemCode = returnLine.ItemCode;
                                        }
                                        else
                                        {
                                            detail180RetRecord.BuyerItemCode = oBuyerItem;
                                        }
                                    }
                                    else
                                    {
                                        detail180RetRecord.BuyerItemCode = returnLine.SubCatNum;
                                    }
                                    if (!(String.IsNullOrWhiteSpace(returnLine.U_InfoW2ItemUPC)))
                                    {
                                        detail180RetRecord.ItemUPC = returnLine.U_InfoW2ItemUPC;
                                    }
                                    detail180RetRecord.UnitPrice = Convert.ToDouble(returnLine.Price);
                                    // 10-14-2021 begin
                                    // remove quotes from item description
                                    // detail180RetRecord.ItemDescription = returnLine.Dscription;
                                    //string oItmDesc = returnLine.Dscription;

                                    string oItmDesc = "";
                                    try
                                    {
                                        oItmDesc = returnLine.Dscription;
                                    }
                                    catch
                                    {
                                        oItmDesc = "";
                                    }
                                    if (!String.IsNullOrWhiteSpace(oItmDesc))
                                    {
                                        oItmDesc = oItmDesc.Replace('"', ' ');
                                        // 10-22-2021 begin
                                        oItmDesc = oItmDesc.Replace("  ", " ");
                                        oItmDesc = oItmDesc.Trim();
                                        if (oItmDesc.Length > 80)
                                        {
                                            oItmDesc = oItmDesc.Substring(0, 80);
                                        }
                                        // 10-22-2021 end
                                    }
                                    else
                                    {
                                        oItmDesc = "";
                                    }
                                    detail180RetRecord.ItemDescription = oItmDesc;
                                    // 10-14-2021 end
                                    detail180RetRecord.Quantity = Convert.ToDouble(returnLine.Quantity);
                                    detail180RetRecord.UnitOfMeasure = returnLine.UomCode;
                                    if (String.IsNullOrWhiteSpace(detail180RetRecord.UnitOfMeasure)
                                        || detail180RetRecord.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        detail180RetRecord.UnitOfMeasure = "EA";
                                    }
                                    if (String.IsNullOrWhiteSpace(returnLine.LineItemStatus) || returnLine.LineItemStatus.Trim().Length == 0)
                                    {
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.SOReturn.U_InfoOrdStatus))
                                        {
                                            detail180RetRecord.ReturnCode = selectedRecord.SOReturn.U_InfoOrdStatus;
                                        }
                                        else
                                        {
                                            detail180RetRecord.ReturnCode = "CR";
                                        }
                                    }
                                    else
                                    {
                                        detail180RetRecord.ReturnCode = returnLine.LineItemStatus;
                                    }
                                    if (String.IsNullOrWhiteSpace(returnLine.LineItemRsnCd) || returnLine.LineItemRsnCd.Trim().Length == 0)
                                    {
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.SOReturn.U_InfoReasonCd))
                                        {
                                            detail180RetRecord.ReturnReasonCode = selectedRecord.SOReturn.U_InfoReasonCd;
                                        }
                                        else
                                        {
                                            detail180RetRecord.ReturnReasonCode = "CO";
                                        }
                                    }
                                    else
                                    {
                                        detail180RetRecord.ReturnReasonCode = returnLine.LineItemRsnCd;
                                    }
                                    detail180RetRecord.PurchaseOrderReference = selectedRecord.Edi180HeaderRecord.PurchaseOrderReference;
                                    detail180RetRecord.ReturnTotal = Convert.ToDouble(returnLine.LineTotal);


                                }
                                else if (returnLine.TreeType == "S")
                                {
                                    Edi180RDetailRecord detail180RetRecord = new Edi180RDetailRecord();
                                    record.Details.Add(detail180RetRecord);

                                    detail180RetRecord.VendorItemCode = returnLine.ItemCode;
                                    detail180RetRecord.LineNumber = returnLine.LineNumber850;
                                    detail180RetRecord.ItemReference = returnLine.LineNum;

                                    if (String.IsNullOrWhiteSpace(returnLine.SubCatNum) || returnLine.SubCatNum.Trim().Length == 0)
                                    {

                                        string oBuyerItem = "";
                                        try
                                        {
                                            string oQry = "select top 1 s0.ItemCode from dbo.OSCN s0  WITH(NOLOCK) where s0.[U_EDI_XREF] = '" + returnLine.ItemCode + "' and s0.CardCode = '" + selectedRecord.SOReturn.CardCode + "'";
                                            using (SqlCommand command = new SqlCommand(oQry, sqlConnection))
                                            {
                                                using (SqlDataReader reader = command.ExecuteReader())
                                                {
                                                    if (!reader.Read())
                                                    {
                                                        _logger.Error("Could not locate Buyer Item Code for Item Code " + returnLine.ItemCode);
                                                    }
                                                    oBuyerItem = (String)reader[0];
                                                }
                                            }
                                        }
                                        catch (Exception b)
                                        {
                                            _logger.Error("Error getting Buyer Item =>" + b.Message);
                                        }
                                        if (String.IsNullOrWhiteSpace(oBuyerItem))
                                        {
                                            detail180RetRecord.BuyerItemCode = returnLine.ItemCode;
                                        }
                                        else
                                        {
                                            detail180RetRecord.BuyerItemCode = oBuyerItem;
                                        }
                                    }
                                    else
                                    {
                                        detail180RetRecord.BuyerItemCode = returnLine.SubCatNum;
                                    }
                                    if (!(String.IsNullOrWhiteSpace(returnLine.U_InfoW2ItemUPC)))
                                    {
                                        detail180RetRecord.ItemUPC = returnLine.U_InfoW2ItemUPC;
                                    }
                                    detail180RetRecord.UnitPrice = Convert.ToDouble(returnLine.Price);
                                    // 10-14-2021 begin
                                    // remove quotes from item description
                                    // detail180RetRecord.ItemDescription = returnLineDscription;
                                    string oItmDesc = returnLine.Dscription;
                                    oItmDesc = oItmDesc.Replace('"', ' ');
                                    // 10-22-2021 begin
                                    oItmDesc = oItmDesc.Replace("  ", " ");
                                    oItmDesc = oItmDesc.Trim();
                                    if (oItmDesc.Length > 80)
                                    {
                                        oItmDesc = oItmDesc.Substring(0, 80);
                                    }
                                    // 10-22-2021 end
                                    detail180RetRecord.ItemDescription = oItmDesc;
                                    // 10-14-2021 end
                                    detail180RetRecord.Quantity = Convert.ToDouble(returnLine.Quantity);
                                    detail180RetRecord.UnitOfMeasure = returnLine.UomCode;
                                    if (String.IsNullOrWhiteSpace(detail180RetRecord.UnitOfMeasure)
                                        || detail180RetRecord.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        detail180RetRecord.UnitOfMeasure = "EA";
                                    }

                                    if (String.IsNullOrWhiteSpace(returnLine.LineItemStatus) || returnLine.LineItemStatus.Trim().Length == 0)
                                    {
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.SOReturn.U_InfoOrdStatus))
                                        {
                                            detail180RetRecord.ReturnCode = selectedRecord.SOReturn.U_InfoOrdStatus;
                                        }
                                        else
                                        {
                                            detail180RetRecord.ReturnCode = "CR";
                                        }
                                    }
                                    else
                                    {
                                        detail180RetRecord.ReturnCode = returnLine.LineItemStatus;
                                    }
                                    if (String.IsNullOrWhiteSpace(returnLine.LineItemRsnCd) || returnLine.LineItemRsnCd.Trim().Length == 0)
                                    {
                                        if (!String.IsNullOrWhiteSpace(selectedRecord.SOReturn.U_InfoReasonCd))
                                        {
                                            detail180RetRecord.ReturnReasonCode = selectedRecord.SOReturn.U_InfoReasonCd;
                                        }
                                        else
                                        {
                                            detail180RetRecord.ReturnReasonCode = "CO";
                                        }
                                    }
                                    else
                                    {
                                        detail180RetRecord.ReturnReasonCode = returnLine.LineItemRsnCd;
                                    }
                                    detail180RetRecord.PurchaseOrderReference = selectedRecord.Edi180HeaderRecord.PurchaseOrderReference;
                                    detail180RetRecord.ReturnTotal = Convert.ToDouble(returnLine.LineTotal);

                                }

                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                            _logger.Error(ex.Message);
                        }
                    }
                    sqlConnection.Close();
                }

                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                {
                    sqlConnection.Open();

                    DateTime now = DateTime.Now;
                    if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                    {
                        oConnectionName = "WebApiDbContext";
                    }

                    using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                    {
                        foreach (var selectedRecord in listToProcess)
                        {
                            var recordToUpdate = (from v in dbContext.Edi180HeaderRecords
                                                  where v.HeaderId == selectedRecord.Edi180HeaderRecord.HeaderId
                                                  select v).FirstOrDefault();
                            if (recordToUpdate == null)
                            {
                                throw new WebApiException("No 180 record found with HeaderId " + selectedRecord.Edi180HeaderRecord.HeaderId);
                            }
                            if (selectedRecord.IsError)
                            {
                                recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                                recordToUpdate.ProcessedReturn180 = false;
                            }
                            else
                            {
                                recordToUpdate.ProcessedReturn180 = true;
                                recordToUpdate.Processed180DateTime = now;
                                recordToUpdate.ErrorMessage = String.Empty;
                            }
                            DateTime oCurrentDateTime = DateTime.Now;
                            DateTime oExpDelDate = oCurrentDateTime;
                            DateTime oExpShDate = oCurrentDateTime;
                        }

                        dbContext.SaveChanges();
                    }
                    sqlConnection.Close();
                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                response.ErrorMessage = ex.Message;
                _logger.Error(ex.Message);
            }
            _logger.Debug("Returning the 180 response object which contains " + response.EdiRet180Records.Count + " 180s");
            _logger.Debug("Leaving Get180Records for " + request.CardCode);
            return response;
        }

        private SReturn FindMatchingReturn(WebApiDbContext context, Edi180HeaderRecord record)
        {
            try
            {
                SRLine returnLine = (from v in context.SRLines.Include("SOReturn").Include("SOReturn.SRLines")
                                     where v.BaseType == 15
                                         && v.DocEntry == record.ReturnOrderKey
                                         && v.SOReturn.Canceled == "N"
                                         && (v.TargetType == null || v.TargetType != 16)
                                         && ((v.TreeType == "N") || (v.TreeType == "S"))
                                     select v).FirstOrDefault();
                if (returnLine != null)
                {
                    return returnLine.SOReturn;
                }
            }
            catch (Exception e)
            {
                string oErrMesg = e.Message;
                _logger.Error("Could not find Return", e);
                _logger.Error("Could not find Return =>" + oErrMesg);
                return null;
            }
            return null;
        }
        // 07-23-2019 end
        private Invoice FindMatchingInvoice(WebApiDbContext context, Edi850HeaderRecord record)
        {
            // _logger.Debug("Look up delivery for PO# " + record.PurchaseOrderReference);
            var delivery = FindMatchingDelivery(context, record);
            if (delivery == null)
            {
                return null;
            }
            try
            {
                //_logger.Debug("Look up invoice for " + record.PurchaseOrderReference + ", delivery# " + delivery.DocNum);
                InvoiceLine invoiceLine = (from v in context.InvoiceLines.Include("Invoice").Include("Invoice.InvoiceLines")
                                           where v.BaseType == 15
                                               && v.BaseEntry == delivery.DocEntry
                                               && v.Invoice.Canceled == "N"
                                               && (v.TreeType == "S" || v.TreeType == "N")
                                               && (v.Invoice.DocDate >= record.ProcessedDateTime || v.Invoice.U_Info850HdrId == record.HeaderId) // 01-27-2023
                                           select v).FirstOrDefault();

                if (invoiceLine != null)
                {
                    return invoiceLine.Invoice;
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Could not find Invoice for Delivery " + delivery.DocNum + " =>" + ex.Message, ex);
                return null;
            }
            return null;
        }

        // 02-28-2018 begin
        private Invoice FindMatchingInvoiceByDel(WebApiDbContext context, String connectionName, Edi850HeaderRecord record)
        {
            var delivery = FindMatchingDelivery(context, record);
            if (delivery == null)
            {
                return null;
            }
            try
            {
                int invNo = getInvNofromDelivery(connectionName, delivery.DocEntry);
                if (invNo > 0)
                {
                    InvoiceLine invoiceLine = (from v in context.InvoiceLines.Include("Invoice").Include("Invoice.InvoiceLines")
                                               where v.BaseType == 15
                                                   && v.DocEntry == invNo
                                                   && v.Invoice.Canceled == "N"
                                                   && (v.TreeType == "S" || v.TreeType == "N")
                                               select v).FirstOrDefault();

                    if (invoiceLine != null)
                    {
                        return invoiceLine.Invoice;
                    }
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ei)
            {
                String oErrMessage = ei.Message;
                _logger.Error("Could not find Invoice for Delivery " + delivery.DocNum + " =>" + ei.Message, ei);
                return null;
            }
            return null;
        }
        // 02-28-2018 end
        // 09-23-2019  begin
        private Invoice FindMatchingInvoiceBySalesOrd(WebApiDbContext context, String connectionName, Edi850HeaderRecord record)
        {
            try
            {
                InvoiceLine invoiceLine = (from v in context.InvoiceLines.Include("Invoice").Include("Invoice.InvoiceLines")
                                           where v.BaseType == 17
                                               && v.BaseEntry == record.SalesOrderKey
                                               && v.Invoice.Canceled == "N"
                                               && (v.TreeType == "S" || v.TreeType == "N")
                                           select v).FirstOrDefault();

                if (invoiceLine != null)
                {
                    return invoiceLine.Invoice;
                }
            }
            catch (Exception ei)
            {
                String oErrMessage = ei.Message;
                _logger.Error("Could not find Invoice for SalesOrderKey " + record.SalesOrderKey + " =>" + ei.Message, ei);
                return null;
            }
            return null;
        }
        // 09-23-2019 end
        // 08-19-2017 begin
        [HttpPost]
        public Get810CRecordsResponse Get810CRecords(Get810CRecordsRequest request)
        {
            string oSBOCardCode = getSBOCardCode(request.CardCode); // 01-17-2018
            IPreProcess810CRecord iPreProcess810CRecord = null;
            IPostProcess810CRecord iPostProcess810CRecord = null;
            List<Edi850WithCreditMemo> listToProcess = new List<Edi850WithCreditMemo>();
            _logger.Debug("Entering Get810CRecords for " + request.CardCode + " : " + request.CardCode);
            Get810CRecordsResponse response = new Get810CRecordsResponse();
            if (!this.Authorize(request))
            {
                response.ErrorMessage = "Authentication information is invalid";
                response.Successful = false;
                return response;
            }
            if (String.IsNullOrWhiteSpace(request.CardCode))
            {
                response.ErrorMessage = "Card Code is required for Get810CRecords";
                response.Successful = false;
                return response;
            }
            try
            {
                // 01-17-2018 begin
                if (oSBOCardCode == null || oSBOCardCode.Trim().Length == 0)
                {  // 01-17-2018 end
                    iPreProcess810CRecord = Get810CPreProcess(request.CardCode);
                    iPostProcess810CRecord = Get810CPostProcess(request.CardCode);
                    // 01-17-2018 begin
                    oSBOCardCode = request.CardCode;
                }
                else
                {
                    iPreProcess810CRecord = Get810CPreProcess(oSBOCardCode);
                    iPostProcess810CRecord = Get810CPostProcess(oSBOCardCode);
                }
                // 01-17-2018 end
                List<Edi850HeaderRecord> listOf850Records = null;
                // 01-17-2018  begin
                string oConnectionName = this.getConnectionName(request.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    listOf850Records = dbContext.Edi850HeaderRecords.Include("Details")
                        .Where(x => x.Processed810C == false
                            && x.Processed == true
                            && x.Processed810 == true
                            && x.TrxPurpose != "01"
                            && x.CardCode == request.CardCode
                            //&& x.CardCode == oSBOCardCode // 01-17-2018
                            && x.SalesOrderKey > 0).ToList();
                    _logger.Debug("There are " + listOf850Records.Count + " 810C records to process");
                    foreach (var record in listOf850Records)
                    {
                        //_logger.Debug("Processing 810C with 850 Key " + record.HeaderId + ", CardCode: " + record.CardCode);
                        try
                        {
                            CreditMemo creditMemo = FindMatchingCreditMemo(dbContext, record);
                            if (creditMemo != null)
                            {
                                // _logger.Debug("Found 810C Credit Memo from Invoice # " + creditMemo.DocNum);
                                listToProcess.Add(new Edi850WithCreditMemo(record, creditMemo));
                            }
                            else
                            {
                                _logger.Debug("810C creditMemo not found for 850 record with key " + record.HeaderId);
                            }
                        }
                        catch (Exception c0)
                        {
                            String oErrMesg = c0.Message;
                        }
                    }
                }
                if (listToProcess.Count == 0)
                {
                    response.Successful = true;
                    return response;
                }
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(oConnectionName)))
                // 01-19-2017 end
                {
                    sqlConnection.Open();
                    //To do -Need to filter records, but not sure how to right now
                    foreach (var selectedRecord in listToProcess)
                    {
                        try
                        {
                            if (iPreProcess810CRecord != null)
                            {
                                if (!iPreProcess810CRecord.OnPreProcess810CRecord(selectedRecord.CreditMemo, selectedRecord.Edi850HeaderRecord))
                                {
                                    continue;
                                }
                            }
                            Edi810CHeaderRecord edi810CHeaderRecord = new Edi810CHeaderRecord();
                            response.Edi810CRecords.Add(edi810CHeaderRecord);

                            edi810CHeaderRecord.BuyerName = selectedRecord.Edi850HeaderRecord.BuyerName;
                            if (selectedRecord.Edi850HeaderRecord.SBOCardCode == null || selectedRecord.Edi850HeaderRecord.SBOCardCode.Trim().Length == 0)
                            {  // 01-17-2018
                                edi810CHeaderRecord.CardCode = selectedRecord.Edi850HeaderRecord.CardCode;
                                // 01-17-2018 begin
                            }
                            else
                            {
                                edi810CHeaderRecord.CardCode = request.CardCode;
                            } // 01-17-2018 end
                            edi810CHeaderRecord.CarrierCode = selectedRecord.CreditMemo.U_InfoW2Cc;
                            edi810CHeaderRecord.ConditionDescription = selectedRecord.Edi850HeaderRecord.ConditionDescription;
                            edi810CHeaderRecord.DeliveryPhoneNumber = selectedRecord.Edi850HeaderRecord.DeliveryPhoneNumber;
                            edi810CHeaderRecord.Department = selectedRecord.Edi850HeaderRecord.Department;

                            edi810CHeaderRecord.PaymentMethod = selectedRecord.Edi850HeaderRecord.PaymentMethod;
                            edi810CHeaderRecord.PromotionChargeCode = selectedRecord.Edi850HeaderRecord.PromotionChargeCode;
                            // 08-28-2019 begin
                            if (selectedRecord.Edi850HeaderRecord.PurchaseOrderDate == null ||
                                selectedRecord.Edi850HeaderRecord.PurchaseOrderDate.ToString().Trim().Length == 0)
                            {
                                edi810CHeaderRecord.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.RecordDate;
                            }
                            else
                            { // 08-28-2019 end
                                edi810CHeaderRecord.PurchaseOrderDate = selectedRecord.Edi850HeaderRecord.PurchaseOrderDate;
                            } // 08-28-2019
                            edi810CHeaderRecord.PurchaseOrderReference = selectedRecord.Edi850HeaderRecord.PurchaseOrderReference;
                            edi810CHeaderRecord.ReplenishmentNumber = selectedRecord.Edi850HeaderRecord.ReplenishmentNumber;
                            edi810CHeaderRecord.RequestedDeliveryDate = selectedRecord.Edi850HeaderRecord.RequestedDeliveryDate;
                            edi810CHeaderRecord.RequestedShipDate = selectedRecord.Edi850HeaderRecord.RequestedShipDate;
                            edi810CHeaderRecord.ShipToAddress1 = selectedRecord.Edi850HeaderRecord.ShipToAddress1;
                            edi810CHeaderRecord.ShipToAddress2 = selectedRecord.Edi850HeaderRecord.ShipToAddress2;
                            edi810CHeaderRecord.ShipToAttention = selectedRecord.Edi850HeaderRecord.ShipToAttention;
                            edi810CHeaderRecord.ShipToCity = selectedRecord.Edi850HeaderRecord.ShipToCity;
                            edi810CHeaderRecord.ShipToCountry = selectedRecord.Edi850HeaderRecord.ShipToCountry;
                            edi810CHeaderRecord.ShipToLocationCode = selectedRecord.Edi850HeaderRecord.ShipToLocationCode;
                            edi810CHeaderRecord.ShipToStoreLocation = selectedRecord.Edi850HeaderRecord.ShipToStoreLocation; // 11-02-2016
                            edi810CHeaderRecord.ShipToName = selectedRecord.Edi850HeaderRecord.ShipToName;
                            edi810CHeaderRecord.ShipToState = selectedRecord.Edi850HeaderRecord.ShipToState;
                            edi810CHeaderRecord.ShipToZip = selectedRecord.Edi850HeaderRecord.ShipToZip;
                            edi810CHeaderRecord.TruckLoadNumber = selectedRecord.Edi850HeaderRecord.TruckLoadNumber;
                            edi810CHeaderRecord.VendorNumber = selectedRecord.Edi850HeaderRecord.VendorNumber;
                            edi810CHeaderRecord.CreditMemoNumber = selectedRecord.CreditMemo.DocNum;
                            // 05-15-2019 begin
                            Int32 sourceInvoice = GetSourceInvoice(oConnectionName, selectedRecord.CreditMemo.DocEntry);
                            if (sourceInvoice > 0)
                            {
                                edi810CHeaderRecord.InvoiceNumber = sourceInvoice;
                            }
                            // 05-15-2019 end
                            // 07-19-2017 begin
                            edi810CHeaderRecord.ShipmentWeight = selectedRecord.CreditMemo.U_InfoW2SWgt;
                            // 01-21-2018 begin
                            if (selectedRecord.CreditMemo.U_InfoW2CnNo < 0)
                            {
                                edi810CHeaderRecord.ConfirmationNo = 0;
                            }
                            else
                            {  // 01-21-2018 end
                                edi810CHeaderRecord.ConfirmationNo = selectedRecord.CreditMemo.U_InfoW2CnNo;
                            }
                            edi810CHeaderRecord.OrderBuyCode = selectedRecord.CreditMemo.U_InfoW2BCode;
                            edi810CHeaderRecord.OrderBuyName = selectedRecord.CreditMemo.U_InfoW2BName;
                            edi810CHeaderRecord.OrderBuyAddr1 = selectedRecord.CreditMemo.U_InfoW2BAd1;
                            edi810CHeaderRecord.OrderBuyAddr2 = selectedRecord.CreditMemo.U_InfoW2BAd2;
                            edi810CHeaderRecord.OrderBuyCity = selectedRecord.CreditMemo.U_InfoW2BCity;
                            edi810CHeaderRecord.OrderBuyState = selectedRecord.CreditMemo.U_InfoW2BState;
                            edi810CHeaderRecord.OrderBuyZip = selectedRecord.CreditMemo.U_InfoW2BZip;
                            edi810CHeaderRecord.OrderBuyCountryCd = selectedRecord.CreditMemo.U_InfoW2BCntry;
                            edi810CHeaderRecord.JobNumber = selectedRecord.CreditMemo.U_InfoW2Job;
                            // 07-19-2017 end
                            // 08-15-2017 begin
                            if (selectedRecord.CreditMemo.CardCode == "ACE" || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("ACE"))
                            {
                                edi810CHeaderRecord.OrderType = "CR";
                                DateTime oDocDate = selectedRecord.CreditMemo.DocDate;
                                try
                                {
                                    double oDays = Convert.ToDouble(selectedRecord.CreditMemo.U_InfoW2TDiscDays);
                                    oDocDate.AddDays(oDays);
                                }
                                catch (Exception)
                                {
                                    oDocDate = selectedRecord.CreditMemo.DocDueDate;
                                }
                                edi810CHeaderRecord.DiscountDueDt = oDocDate;
                                oDocDate = selectedRecord.CreditMemo.DocDate;
                                try
                                {
                                    if (String.IsNullOrWhiteSpace(selectedRecord.CreditMemo.U_InfoW2TDays.ToString()))
                                    {
                                        oDocDate = Convert.ToDateTime(edi810CHeaderRecord.DiscountDueDt);
                                    }
                                    else
                                    {
                                        double oDays = Convert.ToDouble(selectedRecord.CreditMemo.U_InfoW2TDays);
                                        oDocDate.AddDays(oDays);
                                    }
                                }
                                catch (Exception)
                                {
                                    oDocDate = selectedRecord.CreditMemo.DocDueDate;
                                }
                                edi810CHeaderRecord.CreditMemoDueDt = oDocDate;
                                try
                                {
                                    edi810CHeaderRecord.TermsDiscountAmt = selectedRecord.CreditMemo.DocTotal * (selectedRecord.CreditMemo.U_InfoW2TDisc / 100);
                                }
                                catch
                                {
                                    edi810CHeaderRecord.TermsDiscountAmt = 0;
                                }
                            }
                            // 08-15-2017 end

                            //foreach(Edi850DetailRecord detail850Record in selectedRecord.Edi850HeaderRecord.Details)
                            //{
                            //    Edi810CDetailRecord detail810CRecord = new Edi810CDetailRecord();
                            //    edi810CHeaderRecord.Details.Add(detail810CRecord);

                            //    detail810CRecord.BuyerItemCode = detail850Record.BuyerItemCode;
                            //    detail810CRecord.ItemDescription = detail850Record.ItemDescription;
                            //    detail810CRecord.LineNumber = detail850Record.LineNumber;
                            //    detail810CRecord.Quantity = detail850Record.Quantity;
                            //    detail810CRecord.UnitOfMeasure = detail850Record.UnitOfMeasure;
                            //    detail810CRecord.VendorItemCode = detail850Record.VendorItemCode;

                            //    var foundCreditMemoLine = (from v in selectedRecord.CreditMemo.CreditMemoLines
                            //                             where v.LineNumber850 == detail850Record.LineNumber
                            //                             select v).FirstOrDefault();
                            //    if(foundCreditMemoLine != null)
                            //    {
                            //        detail810CRecord.QuantityShipped = Convert.ToDouble(foundCreditMemoLine.Quantity);
                            //        detail810CRecord.UnitPrice = Convert.ToDouble(foundCreditMemoLine.Price);
                            //    }
                            //    else
                            //    {
                            //        detail810CRecord.QuantityShipped = 0;
                            //        detail810CRecord.UnitPrice = detail850Record.UnitPrice;
                            //    }
                            //}
                            foreach (CreditMemoLine creditMemoLine in selectedRecord.CreditMemo.CreditMemoLines)
                            {
                                //string oAltVendorItem = getAltVendorItem(oConnectionName, selectedRecord.CreditMemo.DocEntry, creditMemoLine);

                                if (creditMemoLine.TreeType == "N" || creditMemoLine.TreeType == "S") // 01-20-2018
                                {
                                    Edi810CDetailRecord detail810CRecord = new Edi810CDetailRecord();
                                    edi810CHeaderRecord.Details.Add(detail810CRecord);
                                    //detail810CRecord.BuyerItemCode = creditMemoLine.ItemCode;
                                    //if (String.IsNullOrWhiteSpace(oAltVendorItem))
                                    //{
                                    detail810CRecord.VendorItemCode = creditMemoLine.ItemCode; // 03-14-2017
                                    /*}
                                    else
                                    {
                                        detail810CRecord.VendorItemCode = oAltVendorItem;
                                    }*/

                                    // 10-14-2021 begin
                                    // remove quotes from item description
                                    // detail810CRecord.ItemDescription = creditMemoLine.Dscription;
                                    //string oItmDesc = creditMemoLine.Dscription;
                                    string oItmDesc = "";
                                    try
                                    {
                                        oItmDesc = creditMemoLine.Dscription;
                                    }
                                    catch
                                    {
                                        oItmDesc = "";
                                    }
                                    if (!String.IsNullOrWhiteSpace(oItmDesc))
                                    {
                                        oItmDesc = oItmDesc.Replace('"', ' ');
                                        // 10-22-2021 begin
                                        oItmDesc = oItmDesc.Replace("  ", " ");
                                        oItmDesc = oItmDesc.Trim();
                                        if (oItmDesc.Length > 80)
                                        {
                                            oItmDesc = oItmDesc.Substring(0, 80);
                                        }
                                        // 10-22-2021 end
                                    }
                                    else
                                    {
                                        oItmDesc = "";
                                    }
                                    detail810CRecord.ItemDescription = oItmDesc;
                                    // 10-14-2021 end
                                    detail810CRecord.LineNumber = creditMemoLine.LineNumber850;
                                    detail810CRecord.Quantity = Convert.ToDouble(creditMemoLine.Quantity);
                                    detail810CRecord.QuantityShipped = Convert.ToDouble(creditMemoLine.Quantity);
                                    detail810CRecord.UnitPrice = Convert.ToDouble(creditMemoLine.Price);
                                    detail810CRecord.UnitOfMeasure = creditMemoLine.UomCode;
                                    if (String.IsNullOrWhiteSpace(detail810CRecord.UnitOfMeasure)
                                        || detail810CRecord.UnitOfMeasure.Equals("Manual", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        detail810CRecord.UnitOfMeasure = "EA";
                                    }
                                    using (SqlCommand command = new SqlCommand("select top 1 s0.Substitute from dbo.OSCN s0 WITH(NOLOCK) where s0.ItemCode = '" + creditMemoLine.ItemCode + "' and s0.CardCode = '" + selectedRecord.CreditMemo.CardCode + "'", sqlConnection))
                                    {
                                        using (SqlDataReader reader = command.ExecuteReader())
                                        {
                                            if (!reader.Read())
                                            {
                                                throw new WebApiException("Could not locate Vendor Item Code for Item Code " + creditMemoLine.ItemCode);
                                            }
                                            //detail810CRecord.VendorItemCode = (String)reader[0];
                                            detail810CRecord.BuyerItemCode = (String)reader[0];  // 03-14-2017
                                        }
                                    }
                                }
                            }
                            /* Must figure these out */
                            edi810CHeaderRecord.FinalShipToLocation = String.Empty;
                            edi810CHeaderRecord.BillOfLading = selectedRecord.CreditMemo.U_Info_BOL;
                            String groupNumSql = String.Format(PaymentGroupQuery, selectedRecord.CreditMemo.GroupNum);
                            //_logger.Debug("Executing SQL: " + groupNumSql);
                            using (SqlCommand command = new SqlCommand(groupNumSql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    if (!reader.Read())
                                    {
                                        throw new WebApiException("Could not locate Payment Group with GroupNum " + selectedRecord.CreditMemo.GroupNum);
                                    }
                                    edi810CHeaderRecord.TermsDescription = (String)reader["PymntGroup"];
                                    edi810CHeaderRecord.NetDaysDue = (Int16)reader["ExtraDays"];
                                }
                            }
                            if (selectedRecord.CreditMemo.CardCode == "TSC" || selectedRecord.Edi850HeaderRecord.CardCode.StartsWith("TSC"))
                            {
                                edi810CHeaderRecord.TermsType = "08";

                            }
                            else
                            {
                                edi810CHeaderRecord.TermsType = String.Empty;
                            }

                            edi810CHeaderRecord.TotalDue = Convert.ToDouble(selectedRecord.CreditMemo.DocTotal);
                            edi810CHeaderRecord.ShipmentPaymentMethod = edi810CHeaderRecord.PaymentMethod;
                            edi810CHeaderRecord.CarrierName = String.Empty;

                            edi810CHeaderRecord.FreightCharge = Convert.ToDouble(selectedRecord.CreditMemo.TotalExpns);
                            if (selectedRecord.CreditMemo.TrnspCode > 0)
                            {
                                String shipmentSql = String.Format(ShipmentQuery, selectedRecord.CreditMemo.TrnspCode);
                                //_logger.Debug("Executing SQL: " + shipmentSql);
                                using (SqlCommand command = new SqlCommand(shipmentSql, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        if (!reader.Read())
                                        {
                                            throw new WebApiException("Could not locate OSHP with TrnspCode " + selectedRecord.CreditMemo.TrnspCode);
                                        }
                                        edi810CHeaderRecord.CarrierName = (String)reader[0];
                                    }
                                }
                            }
                            if (iPostProcess810CRecord != null)
                            {
                                iPostProcess810CRecord.OnPostProcess810CRecord(selectedRecord.CreditMemo, selectedRecord.Edi850HeaderRecord, edi810CHeaderRecord);
                            }
                            /* End Figure */
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            _logger.Error(ex.Message);
                            selectedRecord.IsError = true;
                            selectedRecord.ErrorMessage = ex.Message;
                        }
                    }
                    sqlConnection.Close();
                }
                DateTime now = DateTime.Now;
                // 01-17-2018  begin
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                //    using (WebApiDbContext dbContext = new WebApiDbContext())

                using (WebApiDbContext dbContext = new WebApiDbContext(oConnectionName))
                // 01-17-2018 end
                {
                    foreach (var selectedRecord in listToProcess)
                    {
                        var recordToUpdate = (from v in dbContext.Edi850HeaderRecords
                                              where v.HeaderId == selectedRecord.Edi850HeaderRecord.HeaderId
                                              select v).FirstOrDefault();
                        if (recordToUpdate == null)
                        {
                            throw new WebApiException("No 850 record found with key " + selectedRecord.Edi850HeaderRecord.HeaderId);
                        }
                        if (selectedRecord.IsError)
                        {
                            recordToUpdate.ErrorMessage = selectedRecord.ErrorMessage;
                            recordToUpdate.Processed810C = false;
                        }
                        else
                        {
                            recordToUpdate.Processed810C = true;
                            recordToUpdate.Processed810CDateTime = now;
                            recordToUpdate.ErrorMessage = String.Empty;
                        }
                    }
                    dbContext.SaveChanges();
                }
                response.Successful = true;
            }
            catch (Exception ex)
            {
                response.Successful = false;
                _logger.Error(ex);
                _logger.Error(ex.Message);
                response.ErrorMessage = ex.Message;
            }
            _logger.Debug("Returning the 810C response object which contains " + response.Edi810CRecords.Count + " Credit Memos");
            //_logger.Debug("{@Get810CRecordsResponse}", response);
            _logger.Debug("Leaving Get810CRecords for " + request.CardCode);
            return response;
        }

        private IPreProcess810CRecord Get810CPreProcess(String cardCode)
        {
            String preProcess810CRecord = ConfigurationManager.AppSettings["PreProcess810CRecord-" + cardCode];
            if (String.IsNullOrWhiteSpace(preProcess810CRecord))
            {
                preProcess810CRecord = ConfigurationManager.AppSettings["PreProcess810CRecord"];
            }
            if (!String.IsNullOrWhiteSpace(preProcess810CRecord))
            {
                try
                {
                    IPreProcess810CRecord iPreProcess810CRecord = (IPreProcess810CRecord)Activator.CreateInstance(Type.GetType(preProcess810CRecord, true));
                    return iPreProcess810CRecord;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPreProcess810CRecord", ex);
                    _logger.Error("Could not instantiate IPreProcess810CRecord =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPreProcess810CRecord. Reason: " + ex.Message);
                }
            }
            return null;
        }

        private IPostProcess810CRecord Get810CPostProcess(String cardCode)
        {
            String postProcess810CRecord = ConfigurationManager.AppSettings["PostProcess810CRecord-" + cardCode];
            if (String.IsNullOrWhiteSpace(postProcess810CRecord))
            {
                postProcess810CRecord = ConfigurationManager.AppSettings["PostProcess810CRecord"];
            }
            if (!String.IsNullOrWhiteSpace(postProcess810CRecord))
            {
                try
                {
                    IPostProcess810CRecord iPostProcess810CRecord = (IPostProcess810CRecord)Activator.CreateInstance(Type.GetType(postProcess810CRecord, true));
                    return iPostProcess810CRecord;
                }
                catch (Exception ex)
                {
                    _logger.Error("Could not instantiate IPostProcess810CRecord", ex);
                    _logger.Error("Could not instantiate IPostProcess810CRecord =>" + ex.Message);
                    throw new WebApiException("Could not instantiate IPostProcess810CRecord. Reason: " + ex.Message);
                }
            }
            return null;
        }
        private CreditMemo FindMatchingCreditMemo(WebApiDbContext context, Edi850HeaderRecord record)
        {
            try
            {
                var invoice = FindMatchingInvoice(context, record);
                if (invoice == null)
                {
                    return null;
                }
                CreditMemoLine creditMemoLine = (from v in context.CreditMemoLines.Include("CreditMemo").Include("CreditMemo.CreditMemoLines")
                                                 where v.BaseType == 13
                                                     && v.BaseEntry == invoice.DocEntry
                                                     && v.CreditMemo.Canceled == "N"
                                                 select v).FirstOrDefault();
                if (creditMemoLine != null)
                {
                    return creditMemoLine.CreditMemo;
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.Error("Could not find Credit Memo : " + ex.Message, ex);
                return null;
            }
        }
        // 08-19-2017 end

        public String GetConnectionString()
        {
            if (ConfigurationManager.ConnectionStrings["WebApiDbContext"] == null)
            {
                String msg = "No WebApiDbContext connection string found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }

            String connectionString = ConfigurationManager.ConnectionStrings["WebApiDbContext"].ConnectionString;
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                String msg = "Invalid WebApiDbContext connection string found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            return connectionString;
        }

        // 05-15-2019 begin
        public Int32 GetSourceInvoice(String pConnectionName, Int32 pCreditMemo)
        {
            Int32 InvNo = -1;
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString(pConnectionName)))
                {
                    sqlConnection.Open();
                    String CrSQL = "select top 1 r1.BaseEntry from dbo.RIN1 r1 WITH(NOLOCK) where r1.BaseType = '13' and r1.DocEntry = " + pCreditMemo.ToString();
                    // _logger.Debug("Executing SQL: " + CrSQL);
                    using (SqlCommand command = new SqlCommand(CrSQL, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                _logger.Error("Could not locate base invoice for Credit Memo (DocEntry =  " + pCreditMemo.ToString() + ")");
                                InvNo = -1;
                            }
                            InvNo = (Int32)reader["BaseEntry"];
                        }
                    }
                    sqlConnection.Close();
                }
            }
            catch (Exception ex)
            {
                String msg = "Error getting base invoice no for Credit with DocEntry = " + pCreditMemo.ToString() + " =>" + ex.Message; ;
                _logger.Error(msg);
            }
            return InvNo;
        }
        // 05-15-2019 end
        public String GetConnectionString(String pConnectionName)
        {
            if (ConfigurationManager.ConnectionStrings[pConnectionName] == null)
            {
                String msg = $"No connection string found for the connection Name '{pConnectionName}' in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }

            String connectionString = ConfigurationManager.ConnectionStrings[pConnectionName].ConnectionString;
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                String msg = "Invalid WebApiDbContext connection string found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            return connectionString;
        }
        [HttpGet]
        public Add850RecordRequest GetExampleAdd850RecordRequest()
        {
            _logger.Debug("Entering GetExampleAdd850RecordRequest");

            Add850RecordRequest add850RecordRequest = new Add850RecordRequest();
            add850RecordRequest.Edi850HeaderRecord.AllowanceCode = "A240"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.AllowanceRate = Convert.ToDouble(".02"); // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.BillingId = "PSM";
            add850RecordRequest.Edi850HeaderRecord.BillingText = "MC 2222-3333-4444-5555"; // 07-08-2019
            add850RecordRequest.Edi850HeaderRecord.BillToAddress1 = "PO BOX 134";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToAddress2 = "ATTN: Billing Dept";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToCity = "WILKESBORO";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToCode = "701"; // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToContact = "Robert White"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.BillToCountry = "US";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToEmail = "rwhite@gmail.com"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.BillToFaxNo = "(919) 877-3298"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.BillToName = "LOWE'S";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToPhoneNo = "(919) 977-5492"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.BillToState = "NC";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BillToZip = "28697";  // 03-06-2019
            add850RecordRequest.Edi850HeaderRecord.BOLNotes = "Bill of Lading Notes"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.BrandName = "Brand Name"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.BrandName = "Brand Name"; // 07-03-2019           
            add850RecordRequest.Edi850HeaderRecord.BuyerName = "BRIAN SIGMON";
            add850RecordRequest.Edi850HeaderRecord.CardCode = "A2000";
            add850RecordRequest.Edi850HeaderRecord.CarrierCode = "FDEG";
            add850RecordRequest.Edi850HeaderRecord.ConditionDescription = "TEST CONDITION DESCRIPTION";
            add850RecordRequest.Edi850HeaderRecord.ConsumerPO = "CS093321"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.ConsumerPODate = DateTime.Today.AddDays(-2); ; // 05-12-2021   
            add850RecordRequest.Edi850HeaderRecord.CustomerRef = "89101112"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.ContractNumber = "CL20190125";  // 03-25-2019
            add850RecordRequest.Edi850HeaderRecord.CrossDockId = "Cross Dock Id"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.CrossDockName = "Cross Dock Short Name"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.CustCode3PL = "CR5591"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.CustomerOrderNumber = "MAR20190301-1123";  // 03-25-2019
            add850RecordRequest.Edi850HeaderRecord.DeliveryAgentId = "Delivery Agent Id"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.DeliveryAgentName = "Delivery Agent Short Name"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.DeliveryContact = "Customer Service"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.DeliveryEmail = "customerservice@lowes.com"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.DeliveryFaxNo = "(631) 200-2199"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.DeliveryPhoneNumber = "PH# (631) 208-2190";
            add850RecordRequest.Edi850HeaderRecord.Department = "7";
            add850RecordRequest.Edi850HeaderRecord.DiscountDescr = "10% "; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.ErrorMessage = "No Errors";
            add850RecordRequest.Edi850HeaderRecord.FileId = "http://test.com/img.jpg"; // 01-29-2021
            add850RecordRequest.Edi850HeaderRecord.FreightBillType = "Third Party"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.GiftMessage = "Happy Birthday!"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.HandlingCode = "15"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.HandlingDescription = "Leave at side door"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.HeaderId = 0;
            add850RecordRequest.Edi850HeaderRecord.InternalControlNo = "12345678";  // 03-25-2019
            add850RecordRequest.Edi850HeaderRecord.ItemTaxType = "Sales Tax 7.75%"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.JobNumber = "PRJ129986"; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.MessageText = " For alternative ship methods please refer to the routing guide"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.OrderBuyAddr1 = "2003 W US HIGHWAY 421"; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyAddr2 = String.Empty; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyCity = "WILKESBORO"; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyCode = "SOS Order"; // 07-17-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyCountryCd = "US"; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyName = "Tom Jones"; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyState = "NC"; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderBuyZip = "28697"; //06-27-2017
            add850RecordRequest.Edi850HeaderRecord.OrderMessage = "Thank you for your order"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.OrderType = "OS"; // special order  05-17-2017
            add850RecordRequest.Edi850HeaderRecord.OrderType3PL = ""; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.PackingNotes = "Header Level Packing Slip Notes"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.PackingStoreCode = "DC_378233"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.PackSlipTemplate = "GS1-128"; // 01-29-2021
            add850RecordRequest.Edi850HeaderRecord.PaymentDescription = "(Carrier 3rd party billing)"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.PaymentMethod = "PP";
            add850RecordRequest.Edi850HeaderRecord.PoolPointId = "Pool Point Id"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.PoolPointName = "Pool Point Short Name"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.POPurposeCode = ""; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.POType3PL = "SA1"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.PriorityShippingFee = 32.75; // 03-25-2019
            add850RecordRequest.Edi850HeaderRecord.ProcessingCode = "LFK"; // 01-29-2021
            add850RecordRequest.Edi850HeaderRecord.PromotionChargeCode = String.Empty;
            add850RecordRequest.Edi850HeaderRecord.PurchaseOrderDate = DateTime.Today;
            add850RecordRequest.Edi850HeaderRecord.PurchaseOrderReference = "33805815";
            add850RecordRequest.Edi850HeaderRecord.ReceiptId = "CSR20210501S1112"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.ReceivedDateTime = DateTime.Now;
            add850RecordRequest.Edi850HeaderRecord.RecordDate = DateTime.Now;
            add850RecordRequest.Edi850HeaderRecord.ReferenceId = "Ref No. A887D3309"; // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.ReplenishmentNumber = String.Empty;
            add850RecordRequest.Edi850HeaderRecord.RequestedDeliveryDate = DateTime.Today.AddDays(7);
            add850RecordRequest.Edi850HeaderRecord.RequestedShipDate = DateTime.Today.AddDays(5);
            add850RecordRequest.Edi850HeaderRecord.SBOCardCode = "SAPB1CustNo";
            add850RecordRequest.Edi850HeaderRecord.ServiceLevel = "3D"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.ShipAfterDate = DateTime.Today.AddDays(3); // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.ShipCutOffDate = DateTime.Today.AddDays(30); // 07-03-2019
            add850RecordRequest.Edi850HeaderRecord.ShipFromName = "3PL Products LLC"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.ShipFromStore = ""; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.ShipmentWeight = 75;  // 07-17-2019
            add850RecordRequest.Edi850HeaderRecord.ShipmentZone = "7";  // 07-17-2019
            add850RecordRequest.Edi850HeaderRecord.ShippingAccount = "W555X32"; // 07-08-2019
            add850RecordRequest.Edi850HeaderRecord.Shipper3PL = "3PL"; // 02-12-2022
            add850RecordRequest.Edi850HeaderRecord.ShipToAddress1 = "2003 W US HIGHWAY 421";
            add850RecordRequest.Edi850HeaderRecord.ShipToAddress2 = "Bldg 21";
            add850RecordRequest.Edi850HeaderRecord.ShipToAttention = "Warehouse Manager"; // 07-17-2019
            add850RecordRequest.Edi850HeaderRecord.ShipToCity = "WILKESBORO";
            add850RecordRequest.Edi850HeaderRecord.ShipToCountry = "US";
            add850RecordRequest.Edi850HeaderRecord.ShipToLocationCode = "701";
            add850RecordRequest.Edi850HeaderRecord.ShipToName = "Acme Products";
            add850RecordRequest.Edi850HeaderRecord.ShipToState = "NC";
            add850RecordRequest.Edi850HeaderRecord.ShipToStoreLocation = "960"; // 11-02-2016
            add850RecordRequest.Edi850HeaderRecord.ShipToZip = "28697";
            add850RecordRequest.Edi850HeaderRecord.SupplierFax = "(704) 76509989"; // 07-02-2019
            add850RecordRequest.Edi850HeaderRecord.SupplierFax = "(704) 765-9989"; // 07-08-2019
            add850RecordRequest.Edi850HeaderRecord.SupplierName = "Corsan Logisitics";
            add850RecordRequest.Edi850HeaderRecord.SupplierPhone = "(704) 765-9979"; // 07-08-2019
            add850RecordRequest.Edi850HeaderRecord.TaxComponent = ""; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.TermsBasisCode = "03"; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsDayofMonth = 15; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsDescription = "2%10 Net 60"; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsDiscount = 2; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsDiscountDays = 10; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsNetDays = 60; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsNetDueDate = DateTime.Today.AddDays(60); // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.TermsType = "05"; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyBTName = "Third Party Seller";  // 08-10-2021
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyBTAddr = "5000 N. Ocean Blvd";  // 08-10-2021
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyBTCity = "Briny Breezes";  // 08-10-2021
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyBTState = "Florida";  // 08-10-2021
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyBTZip = "33435";  // 08-10-2021
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyBTCountry = "US"; // 08-10-2021
            add850RecordRequest.Edi850HeaderRecord.ThirdPtyAcct = "Y442R12"; // 02-12-2022
            add850RecordRequest.Edi850HeaderRecord.TotalExpectedCost = 575.32;  //07-08-2019
            add850RecordRequest.Edi850HeaderRecord.TransportMethod = "LT"; // 02-03-2019
            add850RecordRequest.Edi850HeaderRecord.TransportRouting = String.Empty;  // 07-17-2019
            add850RecordRequest.Edi850HeaderRecord.TruckLoadNumber = String.Empty;
            add850RecordRequest.Edi850HeaderRecord.TrxPurpose = "00"; // original 05-17-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined01 = "PLBR"; // department
            add850RecordRequest.Edi850HeaderRecord.UserDefined02 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined03 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined04 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined05 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined06 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined07 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined08 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined09 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.UserDefined10 = String.Empty; // 08-30-2017
            add850RecordRequest.Edi850HeaderRecord.VendorMessage = "Please use Tee-Zed on all documents/labels"; // 05-12-2021
            add850RecordRequest.Edi850HeaderRecord.VendorNote1 = "Vendor Note  #1"; // 10-27-2019 
            add850RecordRequest.Edi850HeaderRecord.VendorNote2 = "Vendor Note #2"; // 10-27-2019
            add850RecordRequest.Edi850HeaderRecord.VendorNumber = "800588";
            add850RecordRequest.Edi850HeaderRecord.WebSite = "www.tradingpartner.com";  // 07-17-2019
            add850RecordRequest.Edi850HeaderRecord.ShipmentCd = "GS-128"; // 09-16-2021
            add850RecordRequest.Edi850HeaderRecord.BusinessRuleCd = "LFK"; // 09-16-2021

            add850RecordRequest.SecurityInformation.Password = "password1";
            add850RecordRequest.SecurityInformation.UserName = "user1";

            add850RecordRequest.Edi850HeaderRecord.Details = new List<Edi850DetailRecord>();
            Edi850DetailRecord detail1 = new Edi850DetailRecord();
            add850RecordRequest.Edi850HeaderRecord.Details.Add(detail1);
            detail1.BuyerItemCode = "757138";
            detail1.CustItemCode = "KR145-8"; // 05-12-2021
            detail1.VendorItemCode = "EB-STATE3-02";
            detail1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detail1.VendorItemDescription = "WIFI ENABLED THERMOSTAT (ECOBEE 3)"; // 07-03-2019
            detail1.LineNumber = 1;
            detail1.Quantity = 4;
            detail1.GrossPkgWeight = Convert.ToDouble("2.5"); // 07-03-2019
            detail1.SalesEventName = "Summer Clearance"; // 07-03-2019
            detail1.SalesEventText = "Sale Ends 08/32/2019"; // 07-03-2019
            detail1.Comments = "Custom Comments (may repeat for each block of 264 characters)"; // 07-03-2019
            // detail1.Comments = "Custom Comments #2"; // 10-31-2019
            detail1.AllowanceType = "N"; // 07-03-2019
            detail1.AllowanceId = "I530"; // 07-03-2019
            detail1.PackingNotes = "Packing Notes";
            detail1.RetailPrice = 32.23;
            detail1.UnitPrice = 32.23;
            detail1.UnitOfMeasure = "EA";
            detail1.ItemUPC = String.Empty;
            detail1.PurchaserItemCode = "PR1123-530";
            detail1.GrossPkgWeight = Convert.ToDouble("5.32");
            // 03-25-2019 begin 
            detail1.PurchaserItemCode = "PR-757138";
            detail1.Routing = " UNSP_CG";
            detail1.ServiceLevel = "ET";
            detail1.AssignedId = "1";
            detail1.AssignedId2 = "2";
            detail1.TrackingNumber = "4567894578945";
            detail1.TrackingNoText = "Tracking Number Text";
            detail1.DeliveryConfirmation = "265732";
            detail1.DeliveryText = "Delivery Text";
            // 03-25-2019 end 
            detail1.UnitOfMeasure = "EA";
            detail1.UnitPrice = 195.69;
            detail1.RetailPrice = 225.49; // 02-03-2019
            detail1.ItemUPC = "040232034558"; // 02-03-2019
            detail1.PackingNotes = "Detail Level Packing List Notes"; // 02-03-2019


            Edi850DetailRecord detail2 = new Edi850DetailRecord();
            add850RecordRequest.Edi850HeaderRecord.Details.Add(detail2);
            detail2.BuyerItemCode = "786132";
            detail2.CustItemCode = "KR132-12"; // 05-12-2021
            detail2.VendorItemCode = "NWS01-US";
            detail2.ItemDescription = "NETATMO WEATHER STATION";
            detail2.LineNumber = 2;
            detail2.Quantity = 54;
            detail2.UnitOfMeasure = "EA";
            detail2.UnitPrice = 107.61;
            detail2.PackingNotes = String.Empty;
            detail2.RetailPrice = 32.23;
            detail2.ItemUPC = "001143786132";
            detail2.PurchaserItemCode = "";
            detail2.GrossPkgWeight = Convert.ToDouble("107.61");

            // 03-25-2019 begin
            detail2.PurchaserItemCode = "PR-786132";
            detail2.Routing = "UNSP_CG";
            detail2.ServiceLevel = "ET";
            detail2.AssignedId = "2";
            detail2.AssignedId2 = "2";
            detail2.TrackingNumber = "4567894578945";
            detail2.TrackingNoText = "Tracking Number Text";
            detail2.DeliveryConfirmation = "265732";
            detail2.DeliveryText = "Delivery Text";
            // 03-25-2019 end 
            _logger.Debug("Returning the following object:");

            return add850RecordRequest;
        }
        // 03-16-2021 begin
        public Get753RecordsResponse GetExampleGet753RecordsResponse()
        {
            _logger.Debug("Entering GetExampleGet753RecordsResponse");

            Get753RecordsResponse add753RecordResponse = new Get753RecordsResponse();
            Edi753HeaderRecord edi753HeaderRecord = new Edi753HeaderRecord();
            add753RecordResponse.Edi753Records.Add(edi753HeaderRecord);
            edi753HeaderRecord.CardCode = "Lowes2";
            edi753HeaderRecord.PurchaseOrderReference = "33805815";
            edi753HeaderRecord.TransactionCode = "00";
            DateTime oCurrentDate = DateTime.Now;
            edi753HeaderRecord.TransactionDate = oCurrentDate.ToLocalTime().ToString("yyyyMMdd");
            edi753HeaderRecord.TransactionTime = oCurrentDate.ToLocalTime().ToString("HHMM");
            edi753HeaderRecord.ShipperContact = "Jim Winston";
            edi753HeaderRecord.ShipperPhone = "(912) 332-5573";
            edi753HeaderRecord.VendorNumber = "331924";
            edi753HeaderRecord.ShipFromName = "Corsan Logistics";
            edi753HeaderRecord.ShipFromAddress1 = "13201 Reese Blvd";
            edi753HeaderRecord.ShipFromAddress2 = "#100";
            edi753HeaderRecord.ShipFromCity = "Huntersville";
            edi753HeaderRecord.ShipFromState = "NC";
            edi753HeaderRecord.ShipFromZip = "28078";
            edi753HeaderRecord.ShipFromCountry = "US";

            Edi753DetailRecord detailRecord1 = new Edi753DetailRecord();
            edi753HeaderRecord.Details.Add(detailRecord1);
            detailRecord1.LineNumber = 1;
            detailRecord1.ShipToName = "Lowes";
            detailRecord1.ShipToAddress1 = "2003 W US HIGHWAY 421";
            detailRecord1.ShipToAddress2 = "Suite 101";
            detailRecord1.ShipToCity = "WILKESBORO";
            detailRecord1.ShipToState = "NC";
            detailRecord1.ShipToZip = "28697";
            detailRecord1.ShipToCountry = "US";
            detailRecord1.ShipToLocationCode = "701";
            detailRecord1.Stackable = "Y";
            detailRecord1.PackageCode = "PLT";
            detailRecord1.PackageCount = Convert.ToInt32(4);
            detailRecord1.PurchaseOrderReference = "33805815";
            detailRecord1.ReadyToShipDate = oCurrentDate.AddDays(Convert.ToDouble(5)).ToLocalTime().ToString("yyyyMMdd");
            detailRecord1.PickUpTime = "0700";
            detailRecord1.VolumeQual = "E";
            detailRecord1.ShipmentVolume = Convert.ToDouble("32");
            detailRecord1.WeightUOMCode = "L";
            detailRecord1.ShipmentWeight = Convert.ToDouble("125.50");

            _logger.Debug("Returning the following object:");

            return add753RecordResponse;
        }
        // 03-16-2021 end

        // 04-25-2019 begin
        public Add820RecordRequest GetExampleAdd820RecordRequest()
        {
            _logger.Debug("Entering GetExampleAdd820RecordRequest");
            try
            {
                Add820RecordRequest add820RecordRequest = new Add820RecordRequest();
                add820RecordRequest.SecurityInformation.UserName = "user1";
                add820RecordRequest.SecurityInformation.Password = "password1";
                add820RecordRequest.Edi820HeaderRecord.CardCode = "HDCL";
                add820RecordRequest.Edi820HeaderRecord.Payee = "Corsan Logistics, LLC";
                add820RecordRequest.Edi820HeaderRecord.Payer = "Home Depot";
                add820RecordRequest.Edi820HeaderRecord.PaymentMethod = "FWT";
                add820RecordRequest.Edi820HeaderRecord.ReferenceIdCd = "EF";
                add820RecordRequest.Edi820HeaderRecord.ReferenceId = "ACF555E34";
                add820RecordRequest.Edi820HeaderRecord.TrxHandlingCd = "C";
                add820RecordRequest.Edi820HeaderRecord.CreditDebit = "";
                add820RecordRequest.Edi820HeaderRecord.PaymentAmount = 1275.00;
                add820RecordRequest.Edi820HeaderRecord.PaymentDate = Convert.ToDateTime("2019-04-21");
                add820RecordRequest.Edi820HeaderRecord.PaymentAccount = "1004576";
                add820RecordRequest.Edi820HeaderRecord.VendorNumber = "32597";
                add820RecordRequest.Edi820HeaderRecord.Details = new List<Edi820DetailRecord>();

                Edi820DetailRecord detail1 = new Edi820DetailRecord();
                add820RecordRequest.Edi820HeaderRecord.Details.Add(detail1);
                detail1.EntityIdCd = "PE";
                detail1.EntityName = "Corsan Logistics";
                detail1.EntityAssignedId = "1";
                detail1.EntityIdCd = "PO";
                detail1.EntityType = "2";
                detail1.InvoiceNo = "1120";
                detail1.InvoiceDate = Convert.ToDateTime("2019-01-31");
                detail1.InvoiceAmount = 675.32;
                detail1.PurchaseOrderReference = "AD339F126";
                detail1.ReturnAuthNo = "";
                detail1.AdjustmentAmt = 75.32;
                detail1.AdjustmentReasonCd = "ZZ";
                detail1.AdjustmentRefId = "1120-HD43";
                detail1.AmountPaid = 600.00;
                detail1.DiscountAmount = 0;
                Edi820DetailRecord detail2 = new Edi820DetailRecord();
                add820RecordRequest.Edi820HeaderRecord.Details.Add(detail2);
                detail2.EntityIdCd = "PE";
                detail2.EntityName = "Corsan Logistics";
                detail2.EntityAssignedId = "2";
                detail2.EntityIdCd = "PO";
                detail2.EntityType = "2";
                detail2.InvoiceNo = "1122";
                detail2.InvoiceDate = Convert.ToDateTime("2019-02-15");
                detail2.PurchaseOrderReference = "AD339F130";
                detail2.InvoiceAmount = 675.00;
                detail2.AmountPaid = 675.00;
                _logger.Debug("Returning the following object:");

                return add820RecordRequest;
            }
            catch (Exception e)
            {
                String oErrMsg = e.Message;
                return null;
            }
        }
        // 04-03-2019 begin
        [HttpGet]
        public Get810RecordsResponse GetExampleGet810RecordsResponse()
        {
            _logger.Debug("Entering GetExampleGet810RecordsResponse");

            Get810RecordsResponse add810RecordResponse = new Get810RecordsResponse();
            Edi810HeaderRecord edi810HeaderRecord = new Edi810HeaderRecord();
            add810RecordResponse.Edi810Records.Add(edi810HeaderRecord);
            //edi810HeaderRecord.ContractNumber = "CL20190125";
            edi810HeaderRecord.CardCode = "HDCL";
            edi810HeaderRecord.PurchaseOrderReference = "33805815";
            edi810HeaderRecord.TrxPurpose = "00";
            edi810HeaderRecord.OrderType = "OS";
            edi810HeaderRecord.PurchaseOrderDate = DateTime.Today.AddDays(-5);
            edi810HeaderRecord.Department = "7";
            edi810HeaderRecord.VendorNumber = "800588";
            edi810HeaderRecord.ReplenishmentNumber = "";
            edi810HeaderRecord.BuyerName = "BRIAN SIGMON";
            edi810HeaderRecord.DeliveryPhoneNumber = "PH# (770) 433-8211";
            edi810HeaderRecord.TruckLoadNumber = "";
            edi810HeaderRecord.CarrierCode = "FDEG";
            edi810HeaderRecord.ConditionDescription = "TEST CONDITION DESCRIPTION";
            edi810HeaderRecord.ShipToName = "Home Depot";
            edi810HeaderRecord.ShipToAttention = String.Empty;
            edi810HeaderRecord.ShipToLocationCode = "701";
            edi810HeaderRecord.ShipToStoreLocation = "2920";
            edi810HeaderRecord.ShipToAddress1 = "2455 Paces Ferry Rd NW";
            edi810HeaderRecord.ShipToAddress2 = "";
            edi810HeaderRecord.ShipToCity = "Atlanta";
            edi810HeaderRecord.ShipToState = "GA";
            edi810HeaderRecord.ShipToZip = "30339";
            edi810HeaderRecord.ShipToCountry = "US";
            edi810HeaderRecord.CustomerOrderNumber = "MAR20190301-1123"; // 07-08-2019
            //edi810HeaderRecord.InternalControlNo = "12345678";
            //edi810HeaderRecord.PriorityShippingFee = 32.75;
            edi810HeaderRecord.BillToName = "Home Depot";
            edi810HeaderRecord.BillToAddress1 = "PO BOX 134";
            edi810HeaderRecord.BillToAddress2 = "";
            edi810HeaderRecord.BillToCity = "Atlanta";
            edi810HeaderRecord.BillToState = "GA";
            edi810HeaderRecord.BillToZip = "30339";
            edi810HeaderRecord.BillToCountry = "US";
            edi810HeaderRecord.BillToCode = "791";
            edi810HeaderRecord.RequestedDeliveryDate = DateTime.Today.AddDays(3);
            edi810HeaderRecord.RequestedShipDate = DateTime.Today.AddDays(2);
            edi810HeaderRecord.PaymentMethod = "PP";
            edi810HeaderRecord.PromotionChargeCode = "";
            edi810HeaderRecord.OrderBuyCode = "Drop Ship Order";
            edi810HeaderRecord.OrderBuyName = "Tom Jones";
            edi810HeaderRecord.OrderBuyAddr1 = "2003 W US HIGHWAY 421";
            edi810HeaderRecord.OrderBuyAddr2 = "";
            edi810HeaderRecord.OrderBuyCity = "WILKESBORO";
            edi810HeaderRecord.OrderBuyState = "NC";
            edi810HeaderRecord.OrderBuyZip = "28697";
            edi810HeaderRecord.OrderBuyCountryCd = "US";
            edi810HeaderRecord.JobNumber = String.Empty;
            edi810HeaderRecord.ShipmentWeight = Convert.ToDecimal("100.50");
            edi810HeaderRecord.TermsType = String.Empty;
            //edi810HeaderRecord.TermsType = "03";
            edi810HeaderRecord.TermsDescription = "2%10 Net 60";
            edi810HeaderRecord.TermsDiscountAmt = 2;
            edi810HeaderRecord.TermsDiscountDays = 10;
            edi810HeaderRecord.NetDaysDue = 60;
            /*edi810HeaderRecord.TermsNetDueDate = "2019-05-24T00:00:00-04:00";
            edi810HeaderRecord.TermsDayofMonth = String.Empty; ;
            edi810HeaderRecord.BOLNotes = "Bill of Lading Notes";
            edi810HeaderRecord.PackingNotes = "Header Level Packing Slip Notes";
            edi810HeaderRecord.TransportMethod = "LT";
            edi810HeaderRecord.ServiceLevel = "3D";
            edi810HeaderRecord.HandlingCode = "15";
            edi810HeaderRecord.DeliveryContact = "Customer Service";
            edi810HeaderRecord.DeliveryEmail = "customerservice@lowes.com";
             */
            // 06-01-2019 begin
            edi810HeaderRecord.AllowanceType = "A";
            edi810HeaderRecord.AllowanceHCode = "03";
            edi810HeaderRecord.AllowanceCode = "D500";
            edi810HeaderRecord.AllowanceDescription = "Handling // Drop Ship Fee";
            edi810HeaderRecord.AllowanceAmt = "3";
            // 06-01-2019 end
            // 07-08-2019 begom
            edi810HeaderRecord.SupplierName = "Corsan Logistics";
            edi810HeaderRecord.SupplierPhone = "(704) 765-9979";
            edi810HeaderRecord.SupplierFax = "	(704) 765-9989";
            edi810HeaderRecord.ShipDate = DateTime.Today;
            edi810HeaderRecord.TotalDue = 168.09;
            edi810HeaderRecord.TotalAfterAllowances = 165.09;
            // 07-08-2019 end

            edi810HeaderRecord.TrackingNumber = String.Empty; // 07-17-2019
            edi810HeaderRecord.InvoiceDueDt = DateTime.Today; // 07-17-2019
            edi810HeaderRecord.InvoiceNumber = 3209; // 07-17-2019
            Edi810DetailRecord detailRecord1 = new Edi810DetailRecord();
            edi810HeaderRecord.Details.Add(detailRecord1);
            detailRecord1.BuyerItemCode = "757138";
            detailRecord1.VendorItemCode = "EB-STATE3-02";
            detailRecord1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detailRecord1.VendorItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT"; // 07-19-2019
            detailRecord1.ItemUPC = "1234567890";
            detailRecord1.LineNumber = 1;
            detailRecord1.UnitOfMeasure = "EA";
            detailRecord1.UnitPrice = Convert.ToDouble("42.27");
            detailRecord1.RetailPrice = Convert.ToDouble("50.00");
            detailRecord1.Quantity = 4;
            detailRecord1.QuantityShipped = 4;
            detailRecord1.TrackingNumber = "1Z6W9Y400340147676";
            detailRecord1.PackingNotes = String.Empty;
            detailRecord1.PurchaserItemCode = "STATE3-02-A3";
            detailRecord1.Routing = String.Empty;
            detailRecord1.ServiceLevel = String.Empty;
            detailRecord1.TrackingNoText = "Tracking number text";
            _logger.Debug("Returning the following object:");

            return add810RecordResponse;
        }
        // 04-03-2019 end
        [HttpGet]
        public Get856RecordsResponse GetExampleGet856RecordsResponse()
        {
            _logger.Debug("Entering GetExampleGet856RecordsResponse");

            Get856RecordsResponse add856RecordResponse = new Get856RecordsResponse();
            Edi856HeaderRecord edi856HeaderRecord = new Edi856HeaderRecord();
            add856RecordResponse.Edi856Records.Add(edi856HeaderRecord);

            edi856HeaderRecord.TrxDateTime = DateTime.Now; // 03-10-2022
            edi856HeaderRecord.AsnShipDate = DateTime.Today;
            edi856HeaderRecord.BillOfLading = "BOL123";
            edi856HeaderRecord.BuyerName = "BRIAN SIGMON";
            edi856HeaderRecord.CardCode = "C001000";
            edi856HeaderRecord.CarrierCode = "FDEG";
            edi856HeaderRecord.ConditionDescription = "TEST CONDITION DESCRIPTION";
            edi856HeaderRecord.DeliveryNumber = 12345678; // 02-09-2020
            edi856HeaderRecord.DeliveryPhoneNumber = "PH# (631) 208-2190";
            edi856HeaderRecord.Department = "7";
            edi856HeaderRecord.FreightCost = Convert.ToDecimal("75.32");
            edi856HeaderRecord.OrderBuyAddr1 = "2003 W US HIGHWAY 421"; // 6-27-2017
            edi856HeaderRecord.OrderBuyAddr2 = String.Empty; // 6-27-2017
            edi856HeaderRecord.OrderBuyCity = "WILKESBORO"; // 6-27-2017
            edi856HeaderRecord.OrderBuyCode = "SOS Order"; //07-17-2017
            edi856HeaderRecord.OrderBuyCountryCd = "US"; // 6-27-2017
            edi856HeaderRecord.OrderBuyName = "Tom Jones"; // 6-27-2017
            edi856HeaderRecord.OrderBuyState = "NC"; // 6-27-2017
            edi856HeaderRecord.OrderBuyZip = "28697"; // 6-27-2017
            edi856HeaderRecord.OrderType = "OS"; // 05-31-2017
            edi856HeaderRecord.PaymentMethod = "PP";
            edi856HeaderRecord.PromotionChargeCode = String.Empty;
            edi856HeaderRecord.ProNumber = "CN1112A23"; // 02-12-2019
            edi856HeaderRecord.PurchaseOrderDate = DateTime.Today;
            edi856HeaderRecord.PurchaseOrderReference = "33805815";
            edi856HeaderRecord.ReplenishmentNumber = String.Empty;
            edi856HeaderRecord.RequestedDeliveryDate = DateTime.Today.AddDays(5);
            edi856HeaderRecord.RequestedShipDate = DateTime.Today.AddDays(3);
            edi856HeaderRecord.ServiceLevel = "CG"; // 02-12-2019
            edi856HeaderRecord.ShipFromAddress1 = "13201 Reese Blvd"; // 03-04-2019
            edi856HeaderRecord.ShipFromAddress2 = String.Empty; // 03-04-2019
            edi856HeaderRecord.ShipFromCity = "HUNTERSVILLE"; // 03-04-2019
            edi856HeaderRecord.ShipFromCode = "9999"; // 03-04-2019
            edi856HeaderRecord.ShipFromCountry = "US"; // 03-04-2019
            edi856HeaderRecord.ShipFromName = "Corsan Logistics Charlotte"; // 03-04-2019
            edi856HeaderRecord.ShipFromState = "NC"; // 03-04-2019
            edi856HeaderRecord.ShipFromZip = "28078";// 03-04-2019
            edi856HeaderRecord.ShipmentNumber = "64680";  // 10-31-2019
            edi856HeaderRecord.ShipmentCartons = 5; // 02-12-2019
            edi856HeaderRecord.ShipmentVolume = 32;  // 07-08-2019
            edi856HeaderRecord.ShipmentWeight = 125; // 02-12-2019
            edi856HeaderRecord.ShipmentZone = "R";  // 07-08-2019
            edi856HeaderRecord.ShipMethod = "LT"; // 02-12-2019
            edi856HeaderRecord.ShipToAddress1 = "2003 W US HIGHWAY 421";
            edi856HeaderRecord.ShipToAddress2 = String.Empty;
            edi856HeaderRecord.ShipToCity = "WILKESBORO";
            edi856HeaderRecord.ShipToCountry = "US";
            edi856HeaderRecord.ShipToLocationCode = "701";
            edi856HeaderRecord.ShipToName = "LOWE'S OF WILKESBORO, NC";
            edi856HeaderRecord.ShipToState = "NC";
            edi856HeaderRecord.ShipToStoreLocation = "960"; // 11-02-2016
            edi856HeaderRecord.ShipToZip = "28697";
            edi856HeaderRecord.Structure = "0002"; // 02-09-2021
            edi856HeaderRecord.TransportationMethod = "M";
            edi856HeaderRecord.TransportRouting = String.Empty; // 07-18-2019
            edi856HeaderRecord.TruckLoadNumber = String.Empty;
            edi856HeaderRecord.TrxPurpose = "00"; // 05-31-2017
            edi856HeaderRecord.UserDefined01 = "PLBR";
            edi856HeaderRecord.VendorNumber = "53777";

            Edi856ItemDetailRecord detailRecord1 = new Edi856ItemDetailRecord();
            edi856HeaderRecord.Details.Add(detailRecord1);
            detailRecord1.BuyerItemCode = "757138";
            detailRecord1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detailRecord1.ItemUPC = "1234567890"; //02-12-2019
            detailRecord1.LineNumber = 1;
            detailRecord1.QtyOrdered = 4; // 07-08-2019
            detailRecord1.Quantity = 4;
            detailRecord1.SerialNumber = "00000000000000000001";
            detailRecord1.SSCC = "12345678000000000001"; // 02-22-2022
            detailRecord1.ShipmentCartons = 1; // 07-17-2019
            detailRecord1.TrackingNumber = "1Z6W9Y400340147676"; // 02-22-2019
            detailRecord1.VendorItemCode = "EB-STATE3-02";
            detailRecord1.FreightClass = "70";  // 07-08-2019
            detailRecord1.NMFC = "86752";  // 07-08-2019

            Edi856ItemDetailRecord detailRecord2 = new Edi856ItemDetailRecord();
            edi856HeaderRecord.Details.Add(detailRecord2);
            detailRecord2.BuyerItemCode = "757139";
            detailRecord2.ItemDescription = "ECOBEE 2 WIFI ENABLED THERMOSTAT";
            detailRecord2.ItemUPC = "1234567891"; // 02-12-2019
            detailRecord2.LineNumber = 1;
            detailRecord2.QtyOrdered = 8; // 07-08-2019         
            detailRecord2.Quantity = 4;
            detailRecord2.SerialNumber = "00000000000000000002";
            detailRecord2.SSCC = "12345678000000000100"; // 02-22-2022
            detailRecord2.ShipmentCartons = 1; // 07-17-2019
            detailRecord2.TrackingNumber = "1Z6W9Y400342176846";  // 02-22-2019 
            detailRecord2.VendorItemCode = "EB-STATE3-02";
            detailRecord2.FreightClass = "70";  // 07-08-2019
            detailRecord2.NMFC = "86752";  // 07-08-2019


            return add856RecordResponse;
        }
        // 02-09-2021 begin
        public Get856RecordsResponse GetExampleGet856PackResponse()
        {
            _logger.Debug("Entering GetExampleGet856PackResponse");

            Get856RecordsResponse add856RecordResponse = new Get856RecordsResponse();
            Edi856HeaderRecord edi856HeaderRecord = new Edi856HeaderRecord();
            add856RecordResponse.Edi856Records.Add(edi856HeaderRecord);

            edi856HeaderRecord.AsnShipDate = DateTime.Today;
            edi856HeaderRecord.BillOfLading = "BOL123";
            edi856HeaderRecord.BuyerName = "BRIAN SIGMON";
            edi856HeaderRecord.CardCode = "C001000";
            edi856HeaderRecord.CarrierCode = "FDEG";
            edi856HeaderRecord.ConditionDescription = "TEST CONDITION DESCRIPTION";
            edi856HeaderRecord.DeliveryNumber = 12345678; // 02-09-2020
            edi856HeaderRecord.DeliveryPhoneNumber = "PH# (631) 208-2190";
            edi856HeaderRecord.Department = "7";
            edi856HeaderRecord.OrderBuyAddr1 = "2003 W US HIGHWAY 421"; // 6-27-2017
            edi856HeaderRecord.OrderBuyAddr2 = String.Empty; // 6-27-2017
            edi856HeaderRecord.OrderBuyCity = "WILKESBORO"; // 6-27-2017
            edi856HeaderRecord.OrderBuyCode = "SOS Order"; //07-17-2017
            edi856HeaderRecord.OrderBuyCountryCd = "US"; // 6-27-2017
            edi856HeaderRecord.OrderBuyName = "Tom Jones"; // 6-27-2017
            edi856HeaderRecord.OrderBuyState = "NC"; // 6-27-2017
            edi856HeaderRecord.OrderBuyZip = "28697"; // 6-27-2017
            edi856HeaderRecord.OrderType = "OS"; // 05-31-2017
            edi856HeaderRecord.PaymentMethod = "PP";
            edi856HeaderRecord.PromotionChargeCode = String.Empty;
            edi856HeaderRecord.ProNumber = "CN1112A23"; // 02-12-2019
            edi856HeaderRecord.PurchaseOrderDate = DateTime.Today;
            edi856HeaderRecord.PurchaseOrderReference = "33805815";
            edi856HeaderRecord.ReplenishmentNumber = String.Empty;
            edi856HeaderRecord.RequestedDeliveryDate = DateTime.Today.AddDays(5);
            edi856HeaderRecord.RequestedShipDate = DateTime.Today.AddDays(3);
            edi856HeaderRecord.ServiceLevel = "CG"; // 02-12-2019
            edi856HeaderRecord.ShipFromAddress1 = "13201 Reese Blvd"; // 03-04-2019
            edi856HeaderRecord.ShipFromAddress2 = String.Empty; // 03-04-2019
            edi856HeaderRecord.ShipFromCity = "HUNTERSVILLE"; // 03-04-2019
            edi856HeaderRecord.ShipFromCode = "9999"; // 03-04-2019
            edi856HeaderRecord.ShipFromCountry = "US"; // 03-04-2019
            edi856HeaderRecord.ShipFromName = "Corsan Logistics Charlotte"; // 03-04-2019
            edi856HeaderRecord.ShipFromState = "NC"; // 03-04-2019
            edi856HeaderRecord.ShipFromZip = "28078";// 03-04-2019
            edi856HeaderRecord.ShipmentNumber = "64680";  // 10-31-2019
            edi856HeaderRecord.ShipmentCartons = 5; // 02-12-2019
            edi856HeaderRecord.ShipmentVolume = 32;  // 07-08-2019
            edi856HeaderRecord.ShipmentWeight = 125; // 02-12-2019
            edi856HeaderRecord.ShipmentZone = "R";  // 07-08-2019
            edi856HeaderRecord.ShipMethod = "LT"; // 02-12-2019
            edi856HeaderRecord.ShipToAddress1 = "2003 W US HIGHWAY 421";
            edi856HeaderRecord.ShipToAddress2 = String.Empty;
            edi856HeaderRecord.ShipToCity = "WILKESBORO";
            edi856HeaderRecord.ShipToCountry = "US";
            edi856HeaderRecord.ShipToLocationCode = "701";
            edi856HeaderRecord.ShipToName = "LOWE'S OF WILKESBORO, NC";
            edi856HeaderRecord.ShipToState = "NC";
            edi856HeaderRecord.ShipToStoreLocation = "960"; // 11-02-2016
            edi856HeaderRecord.ShipToZip = "28697";
            edi856HeaderRecord.Structure = "0002"; // 02-09-2021
            edi856HeaderRecord.TransportationMethod = "M";
            edi856HeaderRecord.TransportRouting = String.Empty; // 07-18-2019
            edi856HeaderRecord.TruckLoadNumber = String.Empty;
            edi856HeaderRecord.TrxPurpose = "00"; // 05-31-2017
            edi856HeaderRecord.UserDefined01 = "PLBR";
            edi856HeaderRecord.VendorNumber = "53777";

            Edi856PalletRecord palletRecord = new Edi856PalletRecord();
            edi856HeaderRecord.Details.Add(palletRecord);
            palletRecord.PalletNo = "145786";
            palletRecord.PalletQualifier = "GM";
            palletRecord.TrackingNo = "1Z6W9Y400340147676";
            palletRecord.TrackNoQualifier = "CP";
            palletRecord.Quantity = Convert.ToDouble("12");
            palletRecord.Weight = Convert.ToDouble("50.34");
            palletRecord.WeightQualifier = "A3";
            palletRecord.WeightUOM = "LB";
            palletRecord.Length = Convert.ToDouble("12");
            palletRecord.Width = Convert.ToDouble("12");
            palletRecord.Height = Convert.ToDouble("12");
            palletRecord.DimUOM = "IN";

            Edi856PackRecord packRecord = new Edi856PackRecord();
            palletRecord.Pack.Add(packRecord);
            packRecord.PackNo = "00000000000000000001";
            packRecord.TrackNoQualifier = "CP";
            packRecord.TrackingNo = "1Z6W9Y400340147676";
            packRecord.Quantity = Convert.ToDouble("12");
            packRecord.Weight = Convert.ToDouble("50.34");
            packRecord.WeightQualifier = "A3";
            packRecord.WeightUOM = "LB";
            packRecord.Length = Convert.ToDouble("12");
            packRecord.Width = Convert.ToDouble("12");
            packRecord.Height = Convert.ToDouble("12");
            packRecord.DimUOM = "IN";

            Edi856ItemDetailRecord detailRecord1 = new Edi856ItemDetailRecord();
            packRecord.Items.Add(detailRecord1);

            detailRecord1.BuyerItemCode = "757138";
            detailRecord1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detailRecord1.ItemUPC = "1234567890"; //02-12-2019
            detailRecord1.LineNumber = 1;
            detailRecord1.QtyOrdered = 4; // 07-08-2019
            detailRecord1.Quantity = 4;
            detailRecord1.SerialNumber = "00000000000000000022";
            detailRecord1.ShipmentCartons = 1; // 07-17-2019
            detailRecord1.TrackingNumber = "1Z6W9Y400340147676"; // 02-22-2019
            detailRecord1.VendorItemCode = "EB-STATE3-02";
            detailRecord1.FreightClass = "70";  // 07-08-2019
            detailRecord1.NMFC = "86752";  // 07-08-2019

            Edi856ItemDetailRecord detailRecord2 = new Edi856ItemDetailRecord();
            packRecord.Items.Add(detailRecord2);
            detailRecord2.BuyerItemCode = "757139";
            detailRecord2.ItemDescription = "ECOBEE 2 WIFI ENABLED THERMOSTAT";
            detailRecord2.ItemUPC = "1234567891"; // 02-12-2019
            detailRecord2.LineNumber = 2;
            detailRecord2.QtyOrdered = 8; // 07-08-2019         
            detailRecord2.Quantity = 4;
            detailRecord2.SerialNumber = "000000000000000000023";
            detailRecord2.ShipmentCartons = 1; // 07-17-2019
            detailRecord2.TrackingNumber = "1Z6W9Y400342176846";  // 02-22-2019 
            detailRecord2.VendorItemCode = "EB-STATE3-02";
            detailRecord2.FreightClass = "70";  // 07-08-2019
            detailRecord2.NMFC = "86752";  // 07-08-2019

            return add856RecordResponse;
        }
        // 02-09-2021 end
        // 02-17-2019 begin
        [HttpGet]
        public Get855RecordsResponse GetExampleGet855RecordsResponse()
        {
            _logger.Debug("Entering GetExampleGet855RecordsResponse");

            Get855RecordsResponse add855RecordResponse = new Get855RecordsResponse();
            Edi855HeaderRecord edi855HeaderRecord = new Edi855HeaderRecord();
            add855RecordResponse.Edi855Records.Add(edi855HeaderRecord);

            edi855HeaderRecord.CardCode = "C001000";
            edi855HeaderRecord.TrxDateTime = DateTime.Now; // 03-10-2022
            edi855HeaderRecord.PaymentMethod = "PP";
            edi855HeaderRecord.PromotionChargeCode = String.Empty;
            edi855HeaderRecord.PurchaseOrderDate = DateTime.Today;
            edi855HeaderRecord.PurchaseOrderReference = "33805815";
            edi855HeaderRecord.SalesOrder = "200113"; // 02-12-2022
            edi855HeaderRecord.RequestedDeliveryDate = DateTime.Today.AddDays(5);
            edi855HeaderRecord.RequestedShipDate = DateTime.Today.AddDays(3);
            edi855HeaderRecord.ShipToAddress1 = "2003 W US HIGHWAY 421";
            edi855HeaderRecord.ShipToAddress2 = String.Empty;
            edi855HeaderRecord.ShipToCity = "Moorseville";
            edi855HeaderRecord.ShipToCountry = "US";
            edi855HeaderRecord.ShipToLocationCode = "701";
            edi855HeaderRecord.ShipToStoreLocation = "960"; // 11-02-2016
            edi855HeaderRecord.ShipToName = "Starlight Retail";
            edi855HeaderRecord.ShipToState = "NC";
            edi855HeaderRecord.ShipToZip = "28697";
            Edi855DetailRecord detailRecord1 = new Edi855DetailRecord();
            edi855HeaderRecord.Details.Add(detailRecord1);
            detailRecord1.LineNumber = 10;
            detailRecord1.BuyerItemCode = "757138";
            detailRecord1.VendorItemCode = "EB-STATE3-02";
            detailRecord1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detailRecord1.ItemUPC = "1234567890"; //02-12-2019
            detailRecord1.Quantity = 4;
            detailRecord1.ExpectedLnDeliveryDate = Convert.ToDateTime("2022-FEB-25");
            detailRecord1.OrderNumber = "1110";
            detailRecord1.UnitOfMeasure = "BX";
            detailRecord1.Item855Status = "IA";
            detailRecord1.ItemReasonCode855 = "";


            Edi855DetailRecord detailRecord2 = new Edi855DetailRecord();
            edi855HeaderRecord.Details.Add(detailRecord2);
            detailRecord2.LineNumber = 20;
            detailRecord2.BuyerItemCode = "757139";
            detailRecord2.VendorItemCode = "EB-STATE3-02";
            detailRecord2.ItemDescription = "ECOBEE 2 WIFI ENABLED THERMOSTAT";
            detailRecord2.ItemUPC = "1234567891"; // 02-12-2019
            detailRecord2.Quantity = 1;
            detailRecord2.ExpectedLnDeliveryDate = Convert.ToDateTime("2022-JUN-30");
            detailRecord2.OrderNumber = "1110";
            detailRecord2.UnitOfMeasure = "BX";
            detailRecord2.Item855Status = "IB";
            detailRecord2.ItemReasonCode855 = "Item On Backorder";


            return add855RecordResponse;
        }
        // 02-17-2019 end

        // 02-26-2019 begin
        [HttpGet]
        public Add860RecordRequest GetExampleAdd860RecordRequest()
        {
            Add860RecordRequest add860RecordRequest = new Add860RecordRequest();
            add860RecordRequest.SecurityInformation.UserName = "user1";
            add860RecordRequest.SecurityInformation.Password = "password1";
            add860RecordRequest.Edi860HeaderRecord.CardCode = "TSCCL";
            add860RecordRequest.Edi860HeaderRecord.VendorNumber = "800588";
            add860RecordRequest.Edi860HeaderRecord.TrxPurpose = "04"; // Change Order
            add860RecordRequest.Edi860HeaderRecord.OrderType = "SA"; // standalone order
            add860RecordRequest.Edi860HeaderRecord.PurchaseOrderReference = "9003034468";
            add860RecordRequest.Edi860HeaderRecord.PurchaseOrderDate = Convert.ToDateTime("02/15/2019");
            add860RecordRequest.Edi860HeaderRecord.ChangeRequestDate = DateTime.Today;
            add860RecordRequest.Edi860HeaderRecord.ChangeDateCode = "038";
            add860RecordRequest.Edi860HeaderRecord.ChangeShipDate = Convert.ToDateTime("03-01-2019");
            add860RecordRequest.Edi860HeaderRecord.UserDefined02 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined02 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined03 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined04 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined05 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined06 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined07 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined08 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined09 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined10 = String.Empty;
            //add860RecordRequest.Edi860HeaderRecord.Details = new List<Edi860DetailRecord>();

            Edi860DetailRecord detail1 = new Edi860DetailRecord();
            add860RecordRequest.Edi860HeaderRecord.Details.Add(detail1);
            detail1.ChangeTypeCode = "DI"; // delete item
            detail1.BuyerItemCode = "1347095";
            detail1.VendorItemCode = "EZ-105";
            detail1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detail1.LineNumber = 1;
            detail1.Quantity = 4;
            detail1.UnitOfMeasure = "EA";
            detail1.UnitPrice = 195.69;
            detail1.RetailPrice = 225.49;
            detail1.ItemUPC = "040232034558";

            Edi860DetailRecord detail2 = new Edi860DetailRecord();
            add860RecordRequest.Edi860HeaderRecord.Details.Add(detail2);
            detail2.ChangeTypeCode = "AI";
            detail2.BuyerItemCode = "1347095";
            detail2.VendorItemCode = "EZ-105";
            detail2.ItemDescription = "NETATMO WEATHER STATION";
            detail2.LineNumber = 2;
            detail2.Quantity = 4;
            detail2.UnitOfMeasure = "EA";
            detail2.UnitPrice = 107.61;
            detail2.RetailPrice = 209.32;
            detail2.ItemUPC = "040232034558";

            /* Add860RecordRequest add860RecordRequest2 = new Add860RecordRequest();
             add860RecordRequest2.SecurityInformation.UserName = "user1";
             add860RecordRequest2.SecurityInformation.Password = "password1";
             add860RecordRequest2.Edi860HeaderRecord.CardCode = "TSCCL";
             add860RecordRequest2.Edi860HeaderRecord.TrxPurpose = "04"; // Cancel Order
             add860RecordRequest2.Edi860HeaderRecord.OrderType = "SA"; // standalone order
             add860RecordRequest2.Edi860HeaderRecord.PurchaseOrderReference = "9003034468";
             add860RecordRequest2.Edi860HeaderRecord.PurchaseOrderDate = Convert.ToDateTime("02/15/2019");
             add860RecordRequest2.Edi860HeaderRecord.ChangeRequestDate = DateTime.Today;
             add860RecordRequest2.Edi860HeaderRecord.ChangeDateCode = "038";
             add860RecordRequest2.Edi860HeaderRecord.ChangeShipDate = Convert.ToDateTime("03-01-2019");
             add860RecordRequest2.Edi860HeaderRecord.UserDefined02 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined02 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined03 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined04 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined05 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined06 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined07 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined08 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined09 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.UserDefined10 = String.Empty;
             add860RecordRequest2.Edi860HeaderRecord.Details = new List<Edi860DetailRecord>();

             Edi860DetailRecord detail3 = new Edi860DetailRecord();
             add860RecordRequest2.Edi860HeaderRecord.Details.Add(detail2);
             detail3.ChangeTypeCode = "RZ";
             detail3.BuyerItemCode = "1347095";
             detail3.VendorItemCode = "EZ-105";
             detail3.ItemDescription = "NETATMO WEATHER STATION";
             detail3.LineNumber = 2;
             detail3.Quantity = 4;
             detail3.UnitOfMeasure = "EA";
             detail3.UnitPrice = 107.61;
             _logger.Debug("Returning the following object:"); */

            return add860RecordRequest;
        }
        [HttpGet]
        public Add860RecordRequest GetExampleAdd860RecordRequest2()
        {
            _logger.Debug("Entering GetExampleAdd860RecordRequest2");
            Add860RecordRequest add860RecordRequest = new Add860RecordRequest();
            add860RecordRequest.SecurityInformation.UserName = "user1";
            add860RecordRequest.SecurityInformation.Password = "password1";
            add860RecordRequest.Edi860HeaderRecord.CardCode = "TSCCL";
            add860RecordRequest.Edi860HeaderRecord.VendorNumber = "800588";
            add860RecordRequest.Edi860HeaderRecord.TrxPurpose = "01"; // Cancel Order
            add860RecordRequest.Edi860HeaderRecord.OrderType = "SA"; // standalone order
            add860RecordRequest.Edi860HeaderRecord.PurchaseOrderReference = "9003034468";
            add860RecordRequest.Edi860HeaderRecord.PurchaseOrderDate = Convert.ToDateTime("02/15/2019");
            add860RecordRequest.Edi860HeaderRecord.ChangeRequestDate = DateTime.Today;
            add860RecordRequest.Edi860HeaderRecord.ChangeDateCode = "";
            add860RecordRequest.Edi860HeaderRecord.UserDefined02 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined02 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined03 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined04 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined05 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined06 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined07 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined08 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined09 = String.Empty;
            add860RecordRequest.Edi860HeaderRecord.UserDefined10 = String.Empty;

            _logger.Debug("Returning the following object:");

            return add860RecordRequest;
        }
        [HttpGet]
        public Add180RecordRequest GetExampleAdd180RecordRequest()
        {
            _logger.Debug("Entering GetExampleAdd180RecordRequest");

            Add180RecordRequest add180RecordRequest = new Add180RecordRequest();
            add180RecordRequest.SecurityInformation.UserName = "user1";
            add180RecordRequest.SecurityInformation.Password = "password1";
            add180RecordRequest.Edi180HeaderRecord.CardCode = "TSCCL";
            add180RecordRequest.Edi180HeaderRecord.OrderType = "OS"; // special order
            add180RecordRequest.Edi180HeaderRecord.TrxPurpose = "00"; // original
            add180RecordRequest.Edi180HeaderRecord.RequestDate = DateTime.Today;
            add180RecordRequest.Edi180HeaderRecord.ReferenceId = "A11kf23";
            //add180RecordRequest.Edi180HeaderRecord.PurchaseOrderDate = Convert.ToDateTime("02-14-2019");
            add180RecordRequest.Edi180HeaderRecord.PurchaseOrderReference = "33805815";
            add180RecordRequest.Edi180HeaderRecord.ChargeId = "C";
            add180RecordRequest.Edi180HeaderRecord.ChargeCode = "G470";
            add180RecordRequest.Edi180HeaderRecord.ChargeAmount = 7.32;
            add180RecordRequest.Edi180HeaderRecord.ChargeReference = "TSC1135";
            add180RecordRequest.Edi180HeaderRecord.VendorNumber = "800588";

            add180RecordRequest.Edi180HeaderRecord.UserDefined01 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined02 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined02 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined03 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined04 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined05 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined06 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined07 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined08 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined09 = String.Empty;
            add180RecordRequest.Edi180HeaderRecord.UserDefined10 = String.Empty;

            add180RecordRequest.Edi180HeaderRecord.Details = new List<Edi180DetailRecord>();
            Edi180DetailRecord detail1 = new Edi180DetailRecord();
            add180RecordRequest.Edi180HeaderRecord.Details.Add(detail1);
            detail1.BuyerItemCode = "1347095";
            detail1.VendorItemCode = "EZ-105";
            detail1.ItemDescription = "testing";
            detail1.LineNumber = 1;
            detail1.Quantity = 4;
            detail1.UnitOfMeasure = "EA";
            detail1.UnitPrice = 195.69;
            detail1.RetailPrice = 225.99;
            detail1.ItemUPC = "040232034558";
            detail1.ItemReference = "ITSC-223";
            detail1.ReturnCode = "DR";
            detail1.ReturnReasonCode = "DT";
            detail1.ReturnTotal = 188.37;
            detail1.PurchaseOrderReference = "33805815";
            detail1.IdAssigned = "10";

            _logger.Debug("Returning the following object:");

            return add180RecordRequest;
        }
        // 02-26-2019 end

        [HttpGet]
        public Add850RecordResponse GetExampleAdd850RecordResponse()
        {
            _logger.Debug("Entering GetExampleAdd850RecordResponse");

            Add850RecordResponse add850RecordResponse = new Add850RecordResponse();
            add850RecordResponse.ErrorMessage = String.Empty;
            add850RecordResponse.Successful = true;
            _logger.Debug("Returning the following object:");
            return add850RecordResponse;
        }

        // 07-21-2023 begin
        public Add940RecordRequest GetExampleAdd940RecordRequest()
        {
            _logger.Debug("Entering GetExampleAdd940RecordRequest");

            Add940RecordRequest add940RecordRequest = new Add940RecordRequest();
            add940RecordRequest.Edi940HeaderRecord.BillToAddress1 = "PO BOX 134";
            add940RecordRequest.Edi940HeaderRecord.BillToAddress2 = "ATTN: Billing Dept";
            add940RecordRequest.Edi940HeaderRecord.BillToAttention = "Mary Ells";
            add940RecordRequest.Edi940HeaderRecord.BillToCity = "WILKESBORO";
            add940RecordRequest.Edi940HeaderRecord.BillToContact = "Robert White";
            add940RecordRequest.Edi940HeaderRecord.BillToCountry = "US";
            add940RecordRequest.Edi940HeaderRecord.BillToEmail = "rwhite@gmail.com";
            add940RecordRequest.Edi940HeaderRecord.BillToFaxNo = "(219) 877-3298";
            add940RecordRequest.Edi940HeaderRecord.BillToIdCdQualifier = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.BillToIdentificationCd = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.BillToLocationCode = "B554";
            add940RecordRequest.Edi940HeaderRecord.BillToName = "Acme Ltd";
            add940RecordRequest.Edi940HeaderRecord.BillToPhoneNo = "(929) 977-5492";
            add940RecordRequest.Edi940HeaderRecord.BillToState = "NC";
            add940RecordRequest.Edi940HeaderRecord.BillToZip = "28697";
            add940RecordRequest.Edi940HeaderRecord.CancelDate = DateTime.Today.AddDays(30);
            add940RecordRequest.Edi940HeaderRecord.CardCode = "A2000";
            add940RecordRequest.Edi940HeaderRecord.CarrierCode = "FDEG";
            add940RecordRequest.Edi940HeaderRecord.DepartmentNumber = "7";
            add940RecordRequest.Edi940HeaderRecord.DepositorOrderNo = "CSR20210501S1112";
            add940RecordRequest.Edi940HeaderRecord.DistributionCenter = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ErrorMessage = "No Errors";
            add940RecordRequest.Edi940HeaderRecord.HeaderId = 0;
            add940RecordRequest.Edi940HeaderRecord.InternalControlNo = "12345678";
            add940RecordRequest.Edi940HeaderRecord.MerchTypeCd = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.OrderNumber = "CSR110976";
            add940RecordRequest.Edi940HeaderRecord.Notes = "Instructions";
            add940RecordRequest.Edi940HeaderRecord.PaymentMethod = "PP";
            add940RecordRequest.Edi940HeaderRecord.PurchaseOrderDate = DateTime.Today;
            add940RecordRequest.Edi940HeaderRecord.PurchaseOrderReference = "33805815";
            add940RecordRequest.Edi940HeaderRecord.ReceivedDateTime = DateTime.Now;
            add940RecordRequest.Edi940HeaderRecord.RecordDate = DateTime.Now;
            add940RecordRequest.Edi940HeaderRecord.RequestedShipDate = DateTime.Today.AddDays(5);
            add940RecordRequest.Edi940HeaderRecord.Routing = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.SBOCardCode = "SAPB1CustNo";
            add940RecordRequest.Edi940HeaderRecord.ShipFromAddress1 = "110 N. White Oak";
            add940RecordRequest.Edi940HeaderRecord.ShipFromAddress2 = "Apt 4B";
            add940RecordRequest.Edi940HeaderRecord.ShipFromAttention = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipFromCity = "Whittington";
            add940RecordRequest.Edi940HeaderRecord.ShipFromCountry = "US";
            add940RecordRequest.Edi940HeaderRecord.ShipFromIdCdQualifier = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipFromIdentificationCd = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipFromLocationCode = "12008";
            add940RecordRequest.Edi940HeaderRecord.ShipFromName = "3PL Products LLC";
            add940RecordRequest.Edi940HeaderRecord.ShipFromState = "IL";
            add940RecordRequest.Edi940HeaderRecord.ShipFromZip = "22340-1232";
            add940RecordRequest.Edi940HeaderRecord.ShipId = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipToAddress1 = "110 N. White Oak";
            add940RecordRequest.Edi940HeaderRecord.ShipToAddress2 = "Apt 4B";
            add940RecordRequest.Edi940HeaderRecord.ShipToAttention = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipToCity = "Whittington";
            add940RecordRequest.Edi940HeaderRecord.ShipToContact = "Bldg Manager";
            add940RecordRequest.Edi940HeaderRecord.ShipToCountry = "US";
            add940RecordRequest.Edi940HeaderRecord.ShipToIdCdQualifier = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipToIdentificationCd = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipToLocationCode = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.ShipToName = "Fran Davie";
            add940RecordRequest.Edi940HeaderRecord.ShipToPhoneNo = "(631) 208-2190";
            add940RecordRequest.Edi940HeaderRecord.ShipToState = "IL";
            add940RecordRequest.Edi940HeaderRecord.ShipToZip = "22340-1232";
            add940RecordRequest.Edi940HeaderRecord.StoreNumber = "0094";
            add940RecordRequest.Edi940HeaderRecord.CustCode3PL = "Y442R12";
            add940RecordRequest.Edi940HeaderRecord.TransportMethod = "LT";
            add940RecordRequest.Edi940HeaderRecord.TrxPurpose = "00"; // original 
            add940RecordRequest.Edi940HeaderRecord.TrxSource = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined01 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined02 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined03 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined04 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined05 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined06 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined07 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined08 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined09 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.UserDefined10 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.VendorNumber = "800588";
            add940RecordRequest.Edi940HeaderRecord.WhsAddress1 = "11034 Dimiter Drive";
            add940RecordRequest.Edi940HeaderRecord.WhsAddress2 = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.WhsAttention = "Ron Tetter";
            add940RecordRequest.Edi940HeaderRecord.WhsCity = "Huttersville";
            add940RecordRequest.Edi940HeaderRecord.WhsCountry = "US";
            add940RecordRequest.Edi940HeaderRecord.WhsIdCdQualifier = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.WhsIdentificationCd = String.Empty;
            add940RecordRequest.Edi940HeaderRecord.WhsLocationCode = "W3554";
            add940RecordRequest.Edi940HeaderRecord.WhsName = "Main Warehouse";
            add940RecordRequest.Edi940HeaderRecord.WhsState = "NC";
            add940RecordRequest.Edi940HeaderRecord.WhsZip = "28699";

            add940RecordRequest.SecurityInformation.Password = "password1";
            add940RecordRequest.SecurityInformation.UserName = "user1";

            add940RecordRequest.Edi940HeaderRecord.Details = new List<Edi940DetailRecord>();
            Edi940DetailRecord detail1 = new Edi940DetailRecord();
            add940RecordRequest.Edi940HeaderRecord.Details.Add(detail1);
            detail1.BuyerItemCode = "757138";
            detail1.Comments = "Custom Comments (may repeat for each block of 264 characters)";
            detail1.GrossPkgWeight = Convert.ToDouble("2.5");
            detail1.InnerPackSize = String.Empty;
            detail1.ItemDescription = "ECOBEE 3 WIFI ENABLED THERMOSTAT";
            detail1.ItemUPC = String.Empty;
            detail1.LineNumber = 1;
            detail1.OldItemCode = "KR145-8";
            detail1.PackingNotes = "Packing Notes";
            detail1.PackSize = String.Empty;
            detail1.Quantity = 4;
            detail1.RetailPrice = 32.23;
            detail1.UnitOfMeasure = "EA";
            detail1.UnitPrice = 32.23;
            detail1.VendorItemCode = "EB-STATE3-02";
            detail1.VendorItemDescription = "WIFI ENABLED THERMOSTAT (ECOBEE 3)";

            Edi940DetailRecord detail2 = new Edi940DetailRecord();
            add940RecordRequest.Edi940HeaderRecord.Details.Add(detail2);
            detail2.BuyerItemCode = "786132";
            detail2.Comments = String.Empty;
            detail2.GrossPkgWeight = Convert.ToDouble("107.61");
            detail2.InnerPackSize = String.Empty;
            detail2.ItemDescription = "NETATMO WEATHER STATION";
            detail2.ItemUPC = "001143786132";
            detail2.LineNumber = 2;
            detail2.OldItemCode = "KR132-12";
            detail2.PackingNotes = String.Empty;
            detail2.PackSize = String.Empty;
            detail2.Quantity = 54;
            detail2.RetailPrice = 32.23;
            detail2.UnitOfMeasure = "EA";
            detail2.UnitPrice = 107.61;
            detail2.VendorItemCode = "NWS01-US";
            detail2.VendorItemDescription = String.Empty;



            _logger.Debug("Returning the following object:");

            return add940RecordRequest;
        }

        // 07-23-2023 end
    }


    public class Edi850WithDelivery
    {
        public Edi850WithDelivery()
        {

        }
        public Edi850WithDelivery(Edi850HeaderRecord record, Delivery delivery)
        {
            Edi850HeaderRecord = record;
            Delivery = delivery;
        }
        public Edi850HeaderRecord Edi850HeaderRecord
        {
            get;
            set;
        }
        public Delivery Delivery
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }

    // 08-09-2023 begin
    public class Edi940WithDelivery
    {
        public Edi940WithDelivery()
        {

        }
        public Edi940WithDelivery(Edi940HeaderRecord record, Delivery delivery)
        {
            Edi940HeaderRecord = record;
            Delivery = delivery;
        }
        public Edi940HeaderRecord Edi940HeaderRecord
        {
            get;
            set;
        }
        public Delivery Delivery
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }
    // 08-09-2023 end

    public class Edi850WithInvoice
    {
        public Edi850WithInvoice()
        {

        }
        public Edi850WithInvoice(Edi850HeaderRecord record, Invoice invoice)
        {
            Edi850HeaderRecord = record;
            Invoice = invoice;
        }
        public Edi850HeaderRecord Edi850HeaderRecord
        {
            get;
            set;
        }
        public Invoice Invoice
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }

    // 08-20-2017 begin
    public class Edi850WithCreditMemo
    {
        public Edi850WithCreditMemo()
        {

        }
        public Edi850WithCreditMemo(Edi850HeaderRecord record, CreditMemo invoice)
        {
            Edi850HeaderRecord = record;
            CreditMemo = invoice;
        }
        public Edi850HeaderRecord Edi850HeaderRecord
        {
            get;
            set;
        }
        public CreditMemo CreditMemo
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }
    // 08-20-2017 end

    // 05-31-2017 begin
    public class Edi850WithSalesOrder
    {
        public Edi850WithSalesOrder()
        {

        }
        public Edi850WithSalesOrder(Edi850HeaderRecord record, SOrder salesorder)
        {
            Edi850HeaderRecord = record;
            SOrder = salesorder;
        }
        public Edi850HeaderRecord Edi850HeaderRecord
        {
            get;
            set;
        }
        public SOrder SOrder
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }
    // 05-31-2017 end

    /*
    // 08-10-2023 begin
    public class Edi940WithDelivery
    {
        public Edi940WithDelivery()
        {

        }
        public Edi940WithDelivery(Edi940HeaderRecord record, Delivery delivery)
        {
            Edi940HeaderRecord = record;
            Delivery = delivery;
        }
        public Edi940HeaderRecord Edi940HeaderRecord
        {
            get;
            set;
        }

        public Delivery Delivery
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
    }
    // 08-10-2023 end
    */

    // 07-23-2019 begin
    public class Edi180WithReturn
    {
        public Edi180WithReturn()
        {

        }
        public Edi180WithReturn(Edi180HeaderRecord record, SReturn soReturn)
        {
            Edi180HeaderRecord = record;
            SOReturn = soReturn;
        }
        public Edi180HeaderRecord Edi180HeaderRecord
        {
            get;
            set;
        }
        public SReturn SOReturn
        {
            get;
            set;
        }
        public bool IsError
        {
            get;
            set;
        }
        public String ErrorMessage
        {
            get;
            set;
        }
        // 07-23-2019 end


    }
}