using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Infocus.WebApi.Data.Models;
using System.Configuration;
using log4net;
// copied from ProdumexPostProcess856Record on 08-31-2021
namespace Infocus.WebApi.Common
{
    public sealed class ProdumexPostProcess856Pack : IPostProcess856Record
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ProdumexPostProcess856Pack));


        private static String ShippingMethodQuery =
@"select top 1 isnull(U_InfoW2Tm, 'M') as TransportationMethod
from OSHP
where TrnspCode = {0}";
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
        private string getConnectionName(string pCardCode)
        {
            string oWebApiDbContext = "WebApiDbContext";
            string oQuery = "select WebApiConnectionName, HasNonInventory from InfocusEDI.dbo.WebApiDbContext where CardCode = '" + pCardCode + "'";
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
        public String GetConnectionString(String pConnectionName)
        {
            if (ConfigurationManager.ConnectionStrings[pConnectionName] == null)
            {
                String msg = "No WebApiDbContext connection string found in Web.config";
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

        private string getPMXConnectionName(string pCardCode)
        {
            string oPMXConnection = "ProdumexConnectionString";
            string oQuery = "select ProdumexConnectionName from InfocusEDI.dbo.WebApiDbContext where CardCode = '" + pCardCode + "'";
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
            {
                sqlConnection.Open();
                using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                {
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            oPMXConnection = "ProdumexConnectionString";
                        }
                        else
                        {
                            oPMXConnection = (String)reader["ProdumexConnectionName"];
                        }
                    }
                }
                sqlConnection.Close();
            }
            _logger.Debug("PMX Connection" + oPMXConnection);
            return oPMXConnection;
        }
        public bool CheckPMXWarehouse(Delivery delivery, String connectionString)
        {
            bool bUsesPMX = false;
            string oIsPMX = "No";
            string oQuery = "select IsProdumexWhs from [Infocus_EDI_IsPMXDelivery] where DelNo = " + delivery.DocEntry;
            _logger.Debug("Checking for PMX Warehouses on Delivery # " + delivery.DocNum);
            using (SqlConnection sqlConnection = new SqlConnection(connectionString))
            {

                try
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            try
                            {
                                if (!reader.Read())
                                {
                                    oIsPMX = "Yes";
                                }
                                else
                                {
                                    oIsPMX = (String)reader["IsProdumexWhs"];
                                }
                            }
                            catch (Exception r2)
                            {
                                oIsPMX = "Yes";
                                _logger.Debug("Error checking PMX whs => " + r2.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug("Error checking for PMX warehouses =>" + ex.Message);
                    oIsPMX = "Yes";
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            if (oIsPMX == "Yes")
            {
                _logger.Debug("Delivery # " + delivery.DocNum + " uses PMX Warehouses");
                bUsesPMX = true;
            }
            else
            {
                _logger.Debug("Delivery # " + delivery.DocNum + " does NOT use PMX Warehouses");
                bUsesPMX = false;
            }
            return bUsesPMX;
        }

        public void setMissingPkgId(Delivery delivery, String connectionString)
        {
            string oQuery = "update DLN1 set U_InfoW2MPId = ( RIGHT('00000000000000000' + CAST(CAST(DLN1.BaseDocNum AS NVARCHAR) + (RIGHT('00000' + CAST(BaseLine as nvarchar(4)),4)) as nvarchar(20)), 17)) " +
                            " where len(ltrim(rtrim(coalesce(U_InfoW2MPId,'')))) = 0 and DocEntry = " + delivery.DocEntry;
            _logger.Debug("Set Missing SSCC/Package Id for Delivery # " + delivery.DocNum);
            using (SqlConnection sqlConnection = new SqlConnection(GetConnectionString()))
            {

                try
                {
                    sqlConnection.Open();
                    using (SqlCommand command = new SqlCommand(oQuery, sqlConnection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug("Error during set missing SSCC/Package Id for Delivery # " + delivery.DocNum + " =>" + ex.Message);

                }
                finally
                {
                    sqlConnection.Close();
                }
            }

        }
        public void OnPostProcess856Pack(Delivery delivery, Edi850HeaderRecord edi850HeaderRecord, Edi856HeaderRecord edi856HeaderRecord, String oIs3PL)
        {
            _logger.Debug("Starting OnPostProcess856Pack");
            string oPmxConnectionName = getPMXConnectionName(edi850HeaderRecord.CardCode);
            if (oPmxConnectionName == null || oPmxConnectionName.Trim().Length == 0)
            {
                oPmxConnectionName = "ProdumexConnectionString";
            }
            if (ConfigurationManager.ConnectionStrings[oPmxConnectionName] == null)
            {
                String msg = "No ProdumexConnectionString found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            _logger.Debug(oPmxConnectionName);
            String connectionString = ConfigurationManager.ConnectionStrings[oPmxConnectionName].ConnectionString;
            _logger.Debug(connectionString);
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                String msg = "Invalid ProdumexConnectionString found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            bool bProdumex = true;
            bProdumex = CheckPMXWarehouse(delivery, connectionString);
            if (bProdumex == false)
            {
                setMissingPkgId(delivery, connectionString);
            }
            if (bProdumex == true)
            {
                _logger.Debug("Produmex Delivery");
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String sqlToRun = "execute [dbo].[Infocus_EDI_PMX_DelPack] " + edi850HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    _logger.Debug("Running Produmex SQL: " + sqlToRun);
                    int iNextRow = 0;
                    string oLoopingStructure = "0002";

                    using (SqlCommand command = new SqlCommand(sqlToRun, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                iNextRow = iNextRow + 1;
                                String result = reader.ToString();
                                int NoFields = reader.FieldCount;
                                string oCurPallet = "";
                                string oCurSSCC = "";
                                string oCurItem = "";
                                Edi856PackRecord pack = new Edi856PackRecord();
                                Edi856PalletRecord pallet = new Edi856PalletRecord();
                                while (reader.Read())
                                {
                                    string oHasPallets = (String)reader["HasPallets"];
                                    string oOverboxing = (String)reader["Overboxing"];

                                    
                                    _logger.Error("Processing  row " + iNextRow);
                                    if (iNextRow == 1)
                                    {
                                        if (oHasPallets.ToUpper() == "YES")
                                        {
                                            edi850HeaderRecord.PackSlipTemplate = "0055";
                                        }
                                        else if (oOverboxing.ToUpper() == "YES")
                                        {
                                            edi850HeaderRecord.PackSlipTemplate = "0001";
                                        }
                                        else
                                        {
                                            edi850HeaderRecord.PackSlipTemplate = "0002";
                                        }
                                    }
                                    iNextRow = iNextRow + 1;
                                    oLoopingStructure = edi850HeaderRecord.PackSlipTemplate;
                                    String oSSCC = (String)reader["SerialNo"];
                                    String oMasterSSCC = "";
                                    try
                                    {
                                        oMasterSSCC = (String)reader["MasterSSCC"];
                                        if (String.IsNullOrWhiteSpace(oMasterSSCC))
                                        {
                                            oMasterSSCC = oSSCC;
                                        }
                                    }
                                    catch (Exception ms)
                                    {
                                        oMasterSSCC = oSSCC;
                                    }
                                    String oTrackNo = (String)reader["PkgTrackNo"];
                                    if (!(oMasterSSCC == oCurPallet))
                                    {
                                        pallet = new Edi856PalletRecord();
                                        edi856HeaderRecord.Details.Add(pallet);
                                        pallet.PalletNo = oMasterSSCC;
                                        oCurPallet = pallet.PalletNo;

                                        if (oLoopingStructure == "0055")
                                        {
                                            string oPallet = (String)reader["MasterSSCC"];
                                            pallet.PalletNo = oPallet;
                                            pallet.PalletQualifier = "GM";
                                            pallet.TrackingNo = oTrackNo;
                                            pallet.TrackNoQualifier = "CP";
                                            try
                                            {
                                                string oLength = (String)reader["PltLen"];
                                                pallet.Length = Convert.ToDouble(oLength);
                                                pallet.DimUOM = (String)reader["PltLenUOM"];
                                                string oWidth = (String)reader["PltWidth"];
                                                pallet.Width = Convert.ToDouble(oWidth);
                                                string oHeight = (String)reader["PltHgt"];
                                                pallet.Height = Convert.ToDouble(oHeight);
                                                string oWeight = (String)reader["PltWgt"];
                                                pallet.Weight = Convert.ToDouble(oWeight);
                                                pallet.WeightQualifier = "A3";
                                                pallet.WeightUOM = (String)reader["PltWgtUOM"];
                                            }
                                            catch (Exception w)
                                            {
                                                String oErr = w.Message;
                                                _logger.Error("Error setting pallet dimensions=>" + oErr);
                                            }
                                        }
                                        else
                                        {
                                            pallet.PalletNo = oSSCC;
                                        }
                                    }
                                    if (oCurSSCC != oSSCC)
                                    {
                                        pack = new Edi856PackRecord();
                                        pallet.Pack.Add(pack);
                               
                                        pack.PackNo = oSSCC;
                                        pack.TrackingNo = (String)reader["PkgTrackNo"];
                                        pack.TrackNoQualifier = "CP";
                                        /* skipping dimensions for pack
                                        string oLen = (String)reader["PkgLen"];
                                        pack.Length = Convert.ToDouble(oLen);
                                         */
                                        string oTreeType = (String)reader["TreeType"];
                                        if (oTreeType == "N" || oTreeType == "S")
                                        {

                                            string docLine = "";
                                            try
                                            {
                                                docLine = reader["DocLineNum"].ToString();
                                            }
                                            catch
                                            {
                                                docLine = "";
                                            }
                                            string oValue = "";
                                            try
                                            {
                                                oValue = reader["PkgWeight"].ToString();
                                                _logger.Error("Package Weight " + oValue);
                                                pack.Weight = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting shipment weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Weight = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                oValue = reader["PackageCnt"].ToString();
                                                double oNoPkgs = Convert.ToDouble(oValue);
                                                pack.Quantity = oNoPkgs;
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting number of cartons for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + wt.Message);
                                                pack.Quantity = Convert.ToDouble("1.0");
                                            }
                                        }
                                    }
                                        Edi856ItemDetailRecord items = new Edi856ItemDetailRecord();
                                        pack.Items.Add(items);
                                        try
                                        {
                                            items.LineNumber = Convert.ToInt32(reader["EDILineNum"]);
                                        } catch (Exception e)
                                        {
                                            items.LineNumber = 0;
                                        }

                                        items.VendorItemCode = (String)reader["ItemCode"];
                                        try
                                        {
                                            string oBuyerItemCode = (String)reader["SubCatNum"];
                                            if (!String.IsNullOrWhiteSpace(oBuyerItemCode))
                                            {
                                                items.BuyerItemCode = oBuyerItemCode;
                                            }
                                        }
                                        catch (Exception bitm)
                                        {
                                            String oErr = bitm.Message;
                                            _logger.Equals("Error setting buyer item code =>" + oErr);
                                        }
                                        try
                                        {
                                            items.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]);
                                        }
                                        catch (Exception qty1)
                                        {
                                            String oErr = qty1.Message;
                                            _logger.Equals("Error setting qty ordered =>" + oErr);
                                        }
                                        try
                                        {
                                            items.Quantity = Convert.ToDouble(reader["Quantity"]);
                                        }
                                        catch (Exception qty2)
                                        {
                                            String oErr = qty2.Message;
                                            _logger.Equals("Error setting qty shipped =>" + oErr);
                                        }
                                        items.ItemDescription = (String)reader["Dscription"];
                                    
                                        if (items.SerialNumber != null && items.SerialNumber.Length > 0)
                                        {
                                            items.SerialNumber = items.SerialNumber.Trim().PadLeft(20, '0');
                                        }

                                        if (items.LineNumber == 0)
                                        {
                                            _logger.Debug("856 Line # is zero for Delivery " + delivery.DocNum);
                                            items.LineNumber = 1;
                                        }


                                        try
                                        {
                                            items.ItemUPC = reader["ItemUPC"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            items.ItemUPC = "";
                                        }
                                        try
                                        {
                                            items.Warehouse = reader["Warehouse"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            items.Warehouse = "";
                                        }
                                        try
                                        {
                                            items.ItemStatus = reader["ItemStatus"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            items.ItemStatus = "";
                                        }
                                        if (items.ItemStatus == "IA" && delivery.CardCode == "HOMEDEPOT")
                                        {
                                            items.ItemStatus = "AC";
                                        }
                                        else if (items.ItemStatus == "IA" && delivery.CardCode == "LOWES")
                                        {
                                            items.ItemStatus = "AR";
                                        }
                                        try
                                        {
                                            items.ItemReason = reader["ItemReason"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            items.ItemReason = "";
                                        }
                                        items.LineItemStatus = items.ItemStatus;
                                        items.LineItemReason = items.ItemReason;

                                        var detail850 = (from v in edi850HeaderRecord.Details
                                                         where v.VendorItemCode.Equals(items.VendorItemCode, StringComparison.InvariantCultureIgnoreCase)
                                                         && edi850HeaderRecord.SalesOrderKey > 0
                                                         select v).FirstOrDefault();
                                        if (detail850 != null)
                                        {
                                            if (!String.IsNullOrWhiteSpace(detail850.BuyerItemCode))
                                            {
                                                items.BuyerItemCode = detail850.BuyerItemCode;
                                            }
                                            else
                                            {
                                                String oSubCatNum = "";
                                                try
                                                {
                                                    oSubCatNum = (String)reader["SubCatNum"];
                                                }
                                                catch
                                                {
                                                    oSubCatNum = "";
                                                }
                                                if (!String.IsNullOrWhiteSpace(oSubCatNum))
                                                {
                                                    items.BuyerItemCode = (String)reader["SubCatNum"];
                                                }
                                            }
                                            if (!String.IsNullOrWhiteSpace(detail850.ItemDescription))
                                            {
                                                items.ItemDescription = detail850.ItemDescription;
                                            }
                                            else
                                            {
                                                String oItemDesc = "";
                                                try
                                                {
                                                    oItemDesc = (String)reader["Dscription"];
                                                }
                                                catch
                                                {
                                                    oItemDesc = "";
                                                }
                                                items.ItemDescription = oItemDesc;
                                            }
                                            if (items.LineNumber == 0)
                                            {
                                                items.LineNumber = detail850.LineNumber;
                                            }
                                            if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                            {
                                                items.VendorItemCode = detail850.VendorItemCode;
                                            }
                                            if (detail850.UnitPrice == null)
                                            {
                                                items.UnitPrice = Convert.ToDecimal("0.00");
                                            }
                                            else if (items.UnitPrice == Convert.ToDecimal("0.00"))
                                            {
                                                try
                                                {
                                                    items.UnitPrice = Convert.ToDecimal(detail850.UnitPrice);
                                                }
                                                catch
                                                {

                                                }
                                            }
                                        }


                                        items.FreightClass = (String)reader["FreightClass"];
                                        items.NMFC = (String)reader["NMFC"];                             

                                } // while loop
                            }
                            else
                            {
                                bProdumex = false;
                            }
                        }
                    }
                    if (!(delivery.DocDueDate == null))
                    {
                        Edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                    }
                    else
                    {
                        Edi856HeaderRecord.AsnShipDate = DateTime.Today;
                    }

                    Edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                    if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        Edi856HeaderRecord.BillOfLading = Edi856HeaderRecord.BillOfLading.Replace("-", "");
                    }
                    if (delivery.TrnspCode > 0)
                    {
                        String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                        using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                Edi856HeaderRecord.TransportationMethod = (String)reader[0];
                            }
                        }
                    }

                    if (String.IsNullOrWhiteSpace(Edi856HeaderRecord.TransportationMethod))
                    {
                        Edi856HeaderRecord.TransportationMethod = "M";
                    }
                    
                    sqlConnection.Close();
                }
            }
            else if (bProdumex == false)
            {
                _logger.Error("Not Produmex  -- not implemented for non-Produmex warehouse");
            }
        }
    }
}
