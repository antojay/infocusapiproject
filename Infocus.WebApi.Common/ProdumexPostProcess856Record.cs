using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Infocus.WebApi.Data.Models;
using System.Configuration;
using log4net;

namespace Infocus.WebApi.Common
{
    public sealed class ProdumexPostProcess856Record : IPostProcess856Record
    {
        private static ILog _logger = LogManager.GetLogger(typeof(ProdumexPostProcess856Record));
        /*        
                private static String ProdumexQuery =
        @"select 
            isnull(T5.PlltNum, 0) as 'NumberOfPallets',
            isnull(T3.SSCC, '') as 'PalletId',
            isnull((T3.Quantity * -1), 0) as 'NumberOfCartons'	
        from PMX_INVD T3 
            inner join 
            (
                select T0.DocEntry, COUNT(*) as 'PlltNum' 
                from PMX_INVD T0 
                where T0.DocEntry = {0} 
                    and T0.DocLineNum = {1}
                    and T0.TransType = '15'
                group by T0.DocEntry
            ) T5 on T5.DocEntry = T3.DocEntry			
        where T3.DocEntry = {0}
            and T3.TransType = '15'
            and T3.DocLineNum = {1}";
        */
        //private static String ProdumexPalletQuery =
        // 05-24-2017 begin    
        /*@"select T0.SSCC, T0.ItemCode, T0.DocLineNum, Abs(T0.Quantity) as Quantity, T1.U_InfoW2LNo
        from PMX_INVD T0
        inner join DLN1 T1 on T0.DocEntry = T1.DocEntry and T0.DocLineNum = T1.LineNum
        inner join ODLN T2 on T1.DocEntry = T2.DocEntry
        where T0.DocEntry = {0}
        and T0.TransType = '15'
        and T1.TargetType <> 16
        and T2.Canceled = 'N'";
        */
        // 01-28-2018 begin

        /* @"select coalesce(T0.SSCC, T1.U_InfoW2MPId) as 'SSCC', T0.ItemCode, T0.DocLineNum, SUM(Abs(T0.Quantity)) as Quantity, T1.U_InfoW2LNo, T1.SubCatNum, t1.Dscription
     from PMX_INVD T0
         inner join DLN1 T1 on T0.DocEntry = T1.DocEntry and T0.DocLineNum = T1.LineNum
         inner join ODLN T2 on T1.DocEntry = T2.DocEntry
     where T0.DocEntry = {0}
         and T0.TransType = '15'
         and T1.TargetType <> 16
         and T1.TreeType = 'N')
         and T2.Canceled = 'N'
     group by T0.DocLineNum, T0.SSCC, T1.U_InfoW2MPId, T0.ItemCode, T1.U_InfoW2LNo, T1.SubCatNum, t1.Dscription";
     */
        /*
        // 04-25-2019 updated ProdumexPalletQuery
        private static String ProdumexPalletQuery =
             @"With SerialInfo (DocEntry, CardCode, BOL, SerialNo, ItemCode, Warehouse, TreeType, DocLineNum, Quantity, OrderedQty, U_InfoW2LnNo, SubCatNum, Dscription, PkgTrackNo, ItemUPC, ItemStatus, ItemReason, FreightClass, NMFC) AS (
select t2.DocEntry, t2.CardCode, coalesce(t2.U_INFO_BOL,'') BOL, cast(coalesce(T0.SSCC, T1.U_InfoW2MPId) as nvarchar) as 'SerialNo', T0.ItemCode, T1.WhsCode, T1.TreeType, T0.DocLineNum, 
SUM( case when Abs(coalesce(T0.Quantity,0)) = 0 then T5.Quantity else Abs(T0.Quantity)end) as Quantity
, SUM(Abs(T1.OrderedQty)), (case when coalesce(T1.U_InfoW2LNo,0) = 0 then t5.U_InfoW2LNo else T1.U_InfoW2LNo end) as 'U_InfoW2LNo', T1.SubCatNum, t1.Dscription, coalesce(t1.U_InfoW2TrackNo,'') as 'PkgTrackNo', 
case when len(ltrim(rtrim(coalesce(T1.U_InfoW2ItemUPC,'')))) > 0 then coalesce(t1.U_InfoW2ItemUPC,'')
when len(ltrim(rtrim(coalesce(t5.U_InfoW2ItemUPC, '')))) > 0 then coalesce(t5.U_InfoW2ItemUPC,'')
when len(ltrim(rtrim(coalesce(t4.CodeBars,'')))) > 0 then coalesce(t4.CodeBars,'')
when len(ltrim(rtrim(coalesce(t7.EDIUPC,'')))) > 0 then coalesce(t7.EDIUPC,'') else coalesce(t7.UPC,'') end as ItemUPC, 
T1.U_InfoItmStatus as ItemStatus, T1.U_InfoItmRsn as ItemReason, coalesce(T1.U_InfoW2FrtClass,'') as FreightClass, coalesce(T1.U_InfoW2NMFC,'') as NMFC
from PMX_INVD T0 inner join DLN1 T1 on T0.DocEntry = T1.DocEntry and T0.DocLineNum = T1.LineNum
    inner join ODLN T2 on T1.DocEntry = T2.DocEntry
    left join OWHS T3 on T3.WhsCode = T1.WhsCode
	left join OITM T4 on t1.ItemCode = t4.ItemCode
	left join RDR1 T5 on t0.BaseEntry = t5.DocEntry and t0.BaseLine = t5.LineNum and t0.BaseType = '17'
	left join CorLog_EDI_UPC_XREF t7 on t7.BuyerItemCode = t1.ItemCode and t7.HeaderId = {1}
  where T0.DocEntry = {0} and
	 T0.TransType = '15'
    and T1.TargetType<> 16
    and T2.Canceled = 'N'
    and (T1.TreeType = 'S' or T1.TreeType = 'N') and t3.U_PMX_IMBP = 'Y' 
group by t2.DocEntry, t2.CardCode, coalesce(t2.U_INFO_BOL,''), T0.DocLineNum, coalesce(T0.SSCC, T1.U_InfoW2MPId), T1.U_InfoW2MPId, T0.ItemCode, T1.WhsCode, T1.TreeType, 
T1.U_InfoW2LNo, t5.U_InfoW2LNo, T1.SubCatNum, t1.Dscription, coalesce(t1.U_InfoW2TrackNo,''), coalesce(T1.U_InfoW2ItemUPC,''), coalesce(t5.U_InfoW2ItemUPC,''), 
 coalesce(t4.CodeBars,''), T1.U_InfoItmStatus, T1.U_InfoItmRsn, coalesce(t7.EDIUPC,''), coalesce(t7.UPC,''), coalesce(T1.U_InfoW2FrtClass,''), coalesce(T1.U_InfoW2NMFC,'')
 UNION ALL
select t3.DocEntry, t3.CardCode, coalesce(t3.U_INFO_BOL,''), cast(coalesce(T0.SSCC, T2.U_InfoW2MPId) as nvarchar) as SerialNo, t2.ItemCode,T2.WhsCode, t2.TreeType, T0.DocLineNum,
 case  when Abs(coalesce(T0.Quantity,0)) > 0 then Abs(coalesce(T0.Quantity,0))
       when Abs(coalesce(T2.Quantity,0)) = 0 then T6.Quantity else Abs(T2.Quantity)end, T2.OrderedQty, (case when coalesce(T2.U_InfoW2LNo,0) = 0 then t6.U_InfoW2LNo else T2.U_InfoW2LNo end) as 'U_InfoW2LNo', t2.SubCatNum, t2.Dscription , 
 coalesce(t2.U_InfoW2TrackNo,'') , 
 case when len(ltrim(rtrim(coalesce(t2.U_InfoW2ItemUPC,'')))) > 0 then t2.U_InfoW2ItemUPC
 when len(ltrim(rtrim(coalesce(t6.U_InfoW2ItemUPC,'')))) > 0 then t6.U_InfoW2ItemUPC
 when len(ltrim(rtrim(coalesce(t5.CodeBars,'')))) > 0 then t5.CodeBars 
 when len(ltrim(rtrim(coalesce(t7.EDIUPC, '')))) > 0 then t7.EDIUPC else coalesce(t7.UPC,'') end as ItemUPC, 
 t2.U_InfoItmStatus as ItemStatus, t2.U_InfoItmRsn as ItemReason, coalesce(t2.U_InfoW2FrtClass,'') as FreightClass, coalesce(t2.U_InfoW2NMFC,'') as NMFC
 from PMX_INVD T0 left join [Infocus_EDI_DLN_Line_Totals] t1 on t0.DocEntry = t1.DocEntry and t0.DocLineNum >= t1.LineNum 
 and (t0.DocLineNum < t1.NextLine or T0.DocLineNum <= t1.NextLine) 
 left join DLN1 t2 on t2.DocEntry = t1.DocEntry and t1.LineNum = t2.LineNum  
 inner join ODLN T3 on T2.DocEntry = T3.DocEntry   
 left join OWHS T4 on T4.WhsCode = T2.WhsCode 
 left join OITM t5 on T5.ItemCode = t2.ItemCode
 left join RDR1 t6 on t6.DocEntry = t2.BaseEntry and t6.LineNum = t2.BaseLine and t2.BaseType = '17'
 left join CorLog_EDI_UPC_XREF t7 on t7.BuyerItemCode = t2.ItemCode and t7.HeaderId = {1}
 where T0.TransType = '15' and T2.TargetType<> 16 and T3.Canceled = 'N'  
  and (T2.TreeType = 'S' or T2.TreeType = 'N') and t4.U_PMX_IMBP = 'Y' 
  and t0.DocLineNum = (select min(DocLineNum) from PMX_INVD where TransType = '15' and DocEntry = t1.DocEntry and DocLineNum >= t1.LineNum 
  and DocLineNum <= t1.NextLine) and (select COUNT(LineNum) from DLN1 left join OITM on DLN1.ItemCode = OITM.ItemCode where InvntItem = 'N' and DLN1.DocEntry = t0.DocEntry) > 0 
  and T2.DocEntry = {0} 
  UNION ALL
select t2.DocEntry, t2.CardCode,coalesce(t2.U_INFO_BOL,''), cast(coalesce(T1.U_InfoW2MPId,'') as nvarchar) as 'SerialNo', T1.ItemCode, T1.WhsCode, T1.TreeType, 
T1.LineNum, SUM(case when Abs(coalesce(T1.Quantity,0)) = 0 then T4.Quantity else Abs(T1.Quantity)end) as Quantity, SUM(Abs(T1.OrderedQty)),
(case when coalesce(T1.U_InfoW2LNo,0) = 0 then T4.U_InfoW2LNo else T1.U_InfoW2LNo end) 'U_InfoW2LNo', T1.SubCatNum, t1.Dscription, coalesce(T1.U_InfoW2TrackNo,'') 'TrackNo',
 case when len(ltrim(rtrim(coalesce(T1.U_InfoW2ItemUPC,'')))) > 0 then T1.U_InfoW2ItemUPC 
 when len(ltrim(rtrim(coalesce( T3.CodeBars,'')))) > 0 then coalesce(t3.CodeBars,'')
 when len(ltrim(rtrim(coalesce(t7.EDIUPC,'')))) > 0 then coalesce(t7.EDIUPC,'') else coalesce(t7.UPC,'') end as ItemUPC, 
 T1.U_InfoItmStatus as ItemStatus, T1.U_InfoItmRsn as ItemReason, coalesce(T1.U_InfoW2FrtClass,'') as FreightClass, coalesce(T1.U_InfoW2NMFC,'') as NMFC
from DLN1 T1 left join ODLN T2 on T1.DocEntry = T2.DocEntry
             left join OITM T3 on T1.ItemCode = T3.ItemCode
			 left join RDR1 T4 on T1.BaseEntry = T4.DocEntry and T1.BaseLine = T4.LineNum and T1.BaseType = '17'
			 left join CorLog_EDI_UPC_XREF t7 on t7.BuyerItemCode = t1.ItemCode and t7.HeaderId = {1}
             left join [Infocus_EDI_IsPMXDelivery] t8 on t8.DelNo = t2.DocEntry
where T1.DocEntry = {0} and
	 T1.TargetType<> 16 and t8.IsProdumexWhs = 'No'
    and T2.Canceled = 'N'
    and (T1.TreeType = 'S' or T1.TreeType = 'N')
    and ltrim(Rtrim(cast(coalesce(T1.U_InfoW2MPId,'') as nvarchar))) != '' and ltrim(rtrim(cast(coalesce(t1.U_PMX_SSCC,'') as nvarchar))) = ''
group by t2.DocEntry, t2.CardCode, coalesce(t2.U_INFO_BOL,''), T1.LineNum, coalesce(T1.U_InfoW2MPId,''), T1.U_InfoW2MPId, T1.ItemCode, T1.WhsCode, T1.TreeType, 
T1.U_InfoW2LNo, T4.U_InfoW2LNo, T1.SubCatNum,t1.Dscription, coalesce(T1.U_InfoW2TrackNo,''), T1.U_InfoW2ItemUPC, coalesce(t4.U_InfoW2ItemUPC,''),
coalesce(T3.CodeBars,''), coalesce(t7.EDIUPC,''), coalesce(t7.UPC,''), T1.U_InfoItmStatus, T1.U_InfoItmRsn, coalesce(T1.U_InfoW2FrtClass,''), coalesce(T1.U_InfoW2NMFC,'')
) select distinct  (case when len(ltrim(rtrim(coalesce(SerialNo,'')))) = 0 then (select MAX(U_InfoW2MPId) +1 from DLN1) else SerialNo END) SerialNo, 
(case when t0.ItemCode = SubCatNum and len(ltrim(rtrim(coalesce(t1.U_EDI_XREF,'')))) > 0 then t1.U_EDI_XREF else t0.ItemCode END) ItemCode, Warehouse, TreeType, DocLineNum, Quantity, OrderedQty, cast(coalesce(U_InfoW2LnNo,1) as int) U_InfoW2LnNo, 
(Case when len(ltrim(Rtrim(coalesce(SubCatNum,'')))) > 0 then SubCatNum else t1.Substitute END) SubCatNum, Dscription, BOL, PkgTrackNo, ItemUPC, ItemStatus, ItemReason, FreightClass, NMFC from SerialInfo t0 left join OSCN t1 on t0.CardCode = t1.CardCode and t0.ItemCode = t1.ItemCode 
";
        // 01-22-2018 end
*/

        // 08-29-2017 begin
        private static String DeliveryQuery =
            // 04-25-2019 begin
            /* @"select T1.ItemCode, T1.WhsCode, T1.LineNum, SUM(Abs(T1.Quantity)) as Quantity, T1.U_InfoW2LNo, T1.SubCatNum, 
t1.Dscription, t1.U_InfoW2MPId as 'SSCC', coalesce(T1.U_InfoW2TrackNo,'') as PkgTrackNo, coalesce(t2.U_Info_BOL,'') as BOL, U_InfoItmStatus as ItemStatus,
U_InfoItmRsn as ItemReason
from DLN1 T1 left join ODLN T2 on T1.DocEntry = T2.DocEntry
left join OWHS t3 on T1.WhsCode = t3.WhsCode
where T1.DocEntry = {0}
and T1.TargetType <> 16
and T2.Canceled = 'N' 
and (T1.TreeType = 'S' or T1.TreeType = 'N') and t3.U_PMX_IMBP = 'N' 

group by T1.LineNum, T1.ItemCode, T1.WhsCode, T1.U_InfoW2LNo, T1.SubCatNum, t1.Dscription,t1.U_InfoW2MPId, coalesce(T1.U_InfoW2TrackNo,''), 
         
coalesce(t2.U_Info_BOL,''), U_InfoItmStatus, U_InfoItmRsn";*/
// modified query to include OCRD.U_C3_ItmPrFx & correctly handle removing the prefix

                   @"select case when ltrim(rtrim(IsNull(t8.U_C3_ItmPrFx,''))) = '' then T1.ItemCode else ltrim(rtrim(Replace(t1.ItemCode, (IsNull(T8.U_C3_ItmPrfx,'')+'-'), ' '))) end ItemCode, T1.WhsCode, T1.LineNum, 
SUM(case when Abs(coalesce(T1.Quantity,0)) > 0 then Abs(T1.Quantity)
         when Abs(Coalesce(T5.Quantity,0)) > 0 then Abs(T5.Quantity) else T1.QtyToShip end) as Quantity, SUM(Abs(T1.OrderedQty)) as OrderedQty,
case when coalesce(T1.U_InfoW2LNo,0) = 0 then coalesce(t5.U_InfoW2LNo,0) else coalesce(T1.U_InfoW2LNo,0) end 'U_InfoW2LNo', T1.SubCatNum, 
t1.Dscription, t1.U_InfoW2MPId as 'SSCC', coalesce(T1.U_InfoW2TrackNo,'') as PkgTrackNo, coalesce(t2.U_Info_BOL,'') as BOL, t1.U_InfoItmStatus as ItemStatus,
t1.U_InfoItmRsn as ItemReason, 
case when len(ltrim(rtrim(coalesce(t1.U_InfoW2ItemUPC,'')))) > 0 then coalesce(t1.U_InfoW2ItemUPC,'')
when len(ltrim(rtrim(coalesce(t4.CodeBars,'')))) > 0 then coalesce(t4.CodeBars,'')
when len(ltrim(rtrim(coalesce(t7.EDIUPC,'')))) > 0 then coalesce(t7.EDIUPC,'')
else coalesce(t7.UPC,'') end as ItemUPC, coalesce(T1.U_InfoW2FrtClass,'') as FreightClass, coalesce(T1.U_InfoW2NMFC,'') as NMFC
from DLN1 T1 With(NOLOCK) left join ODLN T2 With(NOLOCK) on T1.DocEntry = T2.DocEntry
left join OWHS t3 With(NOLOCK) on T1.WhsCode = t3.WhsCode
left join OITM t4 With(NOLOCK) on t1.ItemCode = t4.ItemCode
left join RDR1 t5 With(NOLOCK) on t1.BaseEntry = t5.DocEntry and t1.BaseLine = t5.LineNum and t1.BaseType = '17'
left join CorLog_EDI_UPC_XREF t7 With(NOLOCK) on t7.BuyerItemCode = t1.ItemCode and t7.HeaderId = {1}
left join OCRD t8 on t8.CardCode = t2.CardCode
where T1.DocEntry = {0} 
and T1.TargetType <> 16 
and T2.Canceled = 'N' 
and (T1.TreeType = 'S' or T1.TreeType = 'N') and t3.U_PMX_IMBP = 'N' 
and (coalesce(T1.U_InfoW2LNo,0) > 0 or coalesce(t5.U_InfoW2LNo,0) > 0)
group by T1.LineNum, T1.ItemCode, T1.WhsCode, coalesce(T1.U_InfoW2LNo,0), coalesce(t5.U_InfoW2LNo,0), T1.SubCatNum, t1.Dscription,t1.U_InfoW2MPId, 
coalesce(T1.U_InfoW2TrackNo,''), coalesce(t2.U_Info_BOL,''), t1.U_InfoItmStatus, t1.U_InfoItmRsn, coalesce(t1.U_InfoW2ItemUPC,''), coalesce(t4.CodeBars,''),
coalesce(t7.EDIUPC,''), coalesce(t7.UPC,''), coalesce(T1.U_InfoW2FrtClass,''), coalesce(T1.U_InfoW2NMFC,''), t8.U_C3_ItmPrFx ";
        // 04-25-2019 end
        // 08-29-2017 end
// 08-17-2023 begin
        private static String Delivery940Query =
           @"select case when ltrim(rtrim(IsNull(t8.U_C3_ItmPrFx,''))) = '' then T1.ItemCode else ltrim(rtrim(Replace(t1.ItemCode, (IsNull(T8.U_C3_ItmPrfx,'')+'-'), ' '))) end ItemCode, 
T1.WhsCode, T1.LineNum,  case when T1.UomCode = 'Manual' then 'EA' else T1.UomCode end UOMCode,
SUM(case when Abs(coalesce(T1.Quantity,0)) > 0 then Abs(T1.Quantity)
         when Abs(Coalesce(T5.Quantity,0)) > 0 then Abs(T5.Quantity) else T1.QtyToShip end) as Quantity, SUM(Abs(T1.OrderedQty)) as OrderedQty,
case when coalesce(T1.U_InfoW2LNo,0) = 0 then coalesce(t5.U_InfoW2LNo,0) else coalesce(T1.U_InfoW2LNo,0) end 'U_InfoW2LNo', T1.SubCatNum, 
t1.Dscription, t1.U_InfoW2MPId as 'MPalletId', cast(t1.U_InfoW2SSCC as nvarchar(max)) 'SSCC', coalesce(T1.U_InfoW2TrackNo,'') as PkgTrackNo, coalesce(t2.U_Info_BOL,'') as BOL, t1.U_InfoItmStatus as ItemStatus,
t1.U_InfoItmRsn as ItemReason, 
case when len(ltrim(rtrim(coalesce(t1.U_InfoW2ItemUPC,'')))) > 0 then coalesce(t1.U_InfoW2ItemUPC,'')
when len(ltrim(rtrim(coalesce(t4.CodeBars,'')))) > 0 then coalesce(t4.CodeBars,'')
when len(ltrim(rtrim(coalesce(t7.EDIUPC,'')))) > 0 then coalesce(t7.EDIUPC,'')
else coalesce(t7.UPC,'') end as ItemUPC, coalesce(T1.U_InfoW2FrtClass,'') as FreightClass, coalesce(T1.U_InfoW2NMFC,'') as NMFC
from DLN1 T1 With(NOLOCK) left join ODLN T2 With(NOLOCK) on T1.DocEntry = T2.DocEntry
left join OWHS t3 With(NOLOCK) on T1.WhsCode = t3.WhsCode
left join OITM t4 With(NOLOCK) on t1.ItemCode = t4.ItemCode
left join RDR1 t5 With(NOLOCK) on t1.BaseEntry = t5.DocEntry and t1.BaseLine = t5.LineNum and t1.BaseType = '17'
left join CorLog_EDI_940UPC_XREF t7 With(NOLOCK) on t7.BuyerItemCode = t1.ItemCode and t7.HeaderId = {1}
left join OCRD t8 on t8.CardCode = t2.CardCode
where T1.DocEntry = {0} 
and T1.TargetType <> 16 
and T2.Canceled = 'N' 
and (T1.TreeType = 'S' or T1.TreeType = 'N') and t3.U_PMX_IMBP = 'N' 
and (coalesce(T1.U_InfoW2LNo,0) > 0 or coalesce(t5.U_InfoW2LNo,0) > 0)
group by T1.LineNum, T1.ItemCode, T1.WhsCode, coalesce(T1.U_InfoW2LNo,0), coalesce(t5.U_InfoW2LNo,0), T1.SubCatNum, t1.Dscription,t1.U_InfoW2MPId, cast(t1.U_InfoW2SSCC as nvarchar(max)),
coalesce(T1.U_InfoW2TrackNo,''), coalesce(t2.U_Info_BOL,''), t1.U_InfoItmStatus, t1.U_InfoItmRsn, coalesce(t1.U_InfoW2ItemUPC,''), coalesce(t4.CodeBars,''),
coalesce(t7.EDIUPC,''), coalesce(t7.UPC,''), coalesce(T1.U_InfoW2FrtClass,''), coalesce(T1.U_InfoW2NMFC,''), t8.U_C3_ItmPrFx, T1.UomCode ";
// 08-17-2023 end

        private static String ShippingMethodQuery =
@"select top 1 isnull(U_InfoW2Tm, 'M') as TransportationMethod
from OSHP With(NOLOCK)
where TrnspCode = {0}";
        // 01-21-2018 begin
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
        // 04-10-2019 begin
        private string getConnectionName(string pCardCode)
        {
            string oWebApiDbContext = "WebApiDbContext";
            string oQuery = "select WebApiConnectionName, HasNonInventory from InfocusEDI.dbo.WebApiDbContext With(NOLOCK) where CardCode = '" + pCardCode + "'";
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
        // 04-10-2019 end
        private string getPMXConnectionName(string pCardCode)
        {
            string oPMXConnection = "ProdumexConnectionString";
            string oQuery = "select ProdumexConnectionName from InfocusEDI.dbo.WebApiDbContext With(NOLOCK) where CardCode = '" + pCardCode + "'";
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
            //_logger.Debug("PMX Connection" + oPMXConnection);
            return oPMXConnection;
        }
        // 01-21-2018 end

        // 07-23-2019 begin 
        public bool CheckPMXWarehouse(Delivery delivery, String connectionString)
        {
            bool bUsesPMX = false;
            string oIsPMX = "No";
            string oQuery = "select IsProdumexWhs from [Infocus_EDI_IsPMXDelivery] With(NOLOCK) where DelNo = " + delivery.DocEntry;
            // _logger.Debug("Checking for PMX Warehouses on Delivery # " + delivery.DocNum);
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

                                //oIsPMX = (String)reader["IsProdumexWhs"];
                            }
                            catch (Exception r2)
                            {
                                oIsPMX = "Yes";
                                _logger.Error("Error checking PMX whs => " + r2.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Error("Error checking for PMX warehouses =>" + ex.Message);
                    oIsPMX = "Yes";
                }
                finally
                {
                    sqlConnection.Close();
                }
            }
            if (oIsPMX == "Yes")
            {
                //_logger.Debug("Delivery # " + delivery.DocNum + " uses PMX Warehouses");
                bUsesPMX = true;
            }
            else
            {
                //_logger.Debug("Delivery # " + delivery.DocNum + " does NOT use PMX Warehouses");
                bUsesPMX = false;
            }
            return bUsesPMX;
        }

        public void setMissingPkgId(Delivery delivery, String connectionString)
        {
            string oQuery = "update DLN1 set U_InfoW2MPId = ( RIGHT('00000000000000000' + CAST(CAST(DLN1.BaseDocNum AS NVARCHAR) + (RIGHT('00000' + CAST(BaseLine as nvarchar(4)),4)) as nvarchar(20)), 17)) " +
                            " where len(ltrim(rtrim(coalesce(U_InfoW2MPId,'')))) = 0 and DocEntry = " + delivery.DocEntry;
            //_logger.Debug("Set Missing SSCC/Package Id for Delivery # " + delivery.DocNum);
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
                    _logger.Error("Error during set missing SSCC/Package Id for Delivery # " + delivery.DocNum + " =>" + ex.Message);
                }
                finally
                {
                    sqlConnection.Close();
                }
            }

        }
        // 07-23-2019 end
        public void OnPostProcess856Record(Delivery delivery, Edi850HeaderRecord edi850HeaderRecord, Edi856HeaderRecord edi856HeaderRecord, String oIs3PL, String[] pTrackNos)
        {
            // 06-14-2024 begin
            string oSBOCardCode = delivery.CardCode;
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-14-2024 end
            // 01-21-2018 begin
            string oPmxConnectionName = getPMXConnectionName(edi850HeaderRecord.CardCode);
            if (oPmxConnectionName == null || oPmxConnectionName.Trim().Length == 0)
            {
                oPmxConnectionName = "ProdumexConnectionString";
            }
            // if (ConfigurationManager.ConnectionStrings["ProdumexConnectionString"] == null)
            if (ConfigurationManager.ConnectionStrings[oPmxConnectionName] == null)
            // 01-21-2018 end 
            {
                String msg = "No ProdumexConnectionString found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            // _logger.Error(oPmxConnectionName);
            // 01-21-2018 begin
            //String connectionString = ConfigurationManager.ConnectionStrings["ProdumexConnectionString"].ConnectionString;
            String connectionString = ConfigurationManager.ConnectionStrings[oPmxConnectionName].ConnectionString;
            //_logger.Debug(connectionString);
            // 01-21-2018 end
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                String msg = "Invalid ProdumexConnectionString found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            bool bProdumex = true;
            setMissingPkgId(delivery, connectionString); // 07-23-2019
            bProdumex = CheckPMXWarehouse(delivery, connectionString); // 07-23-2019
            // 07-31-2019 begin
            string oTrackingNo = delivery.U_Info_BOL;
            // 05-26-2021 begin
            if (oIs3PL == "Y")
            {
                oTrackingNo = delivery.U_InfoW2Notes;
            }
            if (String.IsNullOrWhiteSpace(oTrackingNo) || oTrackingNo.Trim().Length == 0)
            {
                oTrackingNo = delivery.U_Info_BOL;
                if (String.IsNullOrWhiteSpace(oTrackingNo) || oTrackingNo.Trim().Length == 0)
                {
                    oTrackingNo = delivery.TrackNo;
                }
                // 09-15-2021 begin
                if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                {
                    oTrackingNo = oTrackingNo.Replace("-", "");
                }
                // 09-15-2021 end
            }
            else
            {
                // 05-26-2021 end
                if ((!String.IsNullOrWhiteSpace(delivery.TrackNo)) && delivery.TrackNo.Trim().Length > delivery.U_Info_BOL.Trim().Length)
                {
                    oTrackingNo = delivery.TrackNo;
                    // 09-15-2021 begin
                    if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        oTrackingNo = oTrackingNo.Replace("-", "");
                    }
                    // 09-15-2021 end
                }
            }
            oTrackingNo = oTrackingNo.Replace(", ", ",");
            oTrackingNo = oTrackingNo.Replace(" ,", ",");
            String[] oTrackNos = new String[1];
            if (!String.IsNullOrWhiteSpace(oTrackingNo))
            {
                oTrackNos = oTrackingNo.Split(',');
                if (oTrackNos.Length == 0)
                {
                    oTrackNos = new String[1];
                    oTrackNos[0] = oTrackingNo.Trim();
                }
            }
            int iNextTrack = 0;
            // 07-31-2019 end
            // 05-26-2021 begin
            if (oIs3PL == "Y")
            {
                iNextTrack = 1;
            }
            // 05-26-2021 end

            if (bProdumex == true)
            { // 07-23-2019
                //_logger.Debug("Produmex Delivery");
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();

                    //String sqlToRun = String.Format(ProdumexPalletQuery, delivery.DocEntry, edi850HeaderRecord.HeaderId);
                    String sqlToRun = "execute [dbo].[Infocus_EDI_PMX_Delivery] " + edi850HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    // 03-02-2023 begin
                    if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        sqlToRun = "execute  [dbo].[Infocus_EDI_PMX_DeliveryTot]  " + edi850HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    }
                    // 03-02-2023 end
                    /*  // 02-09-2021 begin
                      if (psend856Pack == true)
                      {
                          sqlToRun = "execute [dbo].[Infocus_EDI_PMX_Pack_Delivery] " + edi850HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                      }
                      // 02-09-2020 end */
                    //_logger.Debug("Running Produmex SQL: " + sqlToRun);
                    int iNextRow = 0;

                    using (SqlCommand command = new SqlCommand(sqlToRun, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {  // 08-29-2017

                                iNextRow = iNextRow + 1;
                                String result = reader.ToString();
                                int NoFields = reader.FieldCount;
                                while (reader.Read())
                                {
                                    _logger.Debug("Processing  row " + iNextRow);

                                    string oTreeType = (String)reader["TreeType"];
                                    if (oTreeType == "N" || oTreeType == "S")
                                    {
                                        Edi856ItemDetailRecord detail = new Edi856ItemDetailRecord();
                                        edi856HeaderRecord.Details.Add(detail);
                                        // 05-23-2021 begin
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
                                            //_logger.Debug("Package Weight " + oValue);
                                            //double oPkgWgt = Convert.ToDouble(oValue);
                                            detail.GrossPkgWeight = oValue;
                                        }
                                        catch (Exception wt)
                                        {
                                            _logger.Error("Error getting shipment weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                            detail.GrossPkgWeight = "0.00";
                                        }
                                        try
                                        {
                                            oValue = reader["PackageCnt"].ToString();
                                            double oNoPkgs = Convert.ToDouble(oValue);
                                            detail.ShipmentCartons = oNoPkgs;
                                        }
                                        catch (Exception wt)
                                        {
                                            _logger.Error("Error getting number of cartons for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + wt.Message);
                                            detail.ShipmentCartons = Convert.ToDouble("1.0");
                                        }
                                        // 05-23-2021 end   

                                        // 10-15-2021 begin
                                        if (detail.ShipmentCartons < Convert.ToDouble("1.0"))
                                        {
                                            detail.ShipmentCartons = Convert.ToDouble("1.0");
                                        }
                                        // 10-15-2021 end
                                        string oAltVendNo = "";
                                        try
                                        {
                                            oAltVendNo = (String)reader["VendorItem"];
                                        }
                                        catch
                                        {
                                            oAltVendNo = "";
                                        }
                                        if (String.IsNullOrWhiteSpace(oAltVendNo))
                                        { // 02-21-2015
                                            detail.VendorItemCode = (String)reader["ItemCode"];
                                            // 02-21-2018 begin
                                        }
                                        else
                                        {
                                            detail.VendorItemCode = (String)oAltVendNo;
                                        }
                                        // 02-21-2018 end
                                       // 06-14-2024 begin
                                        if (BPList.Contains(oSBOCardCode))
                                        {
                                            detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                        }
                                        // 06-14-2024 end
                                            
                                        // 01-21-2017 begin
                                        string oSSCC = null;
                                        try
                                        {
                                            string oSerNo = (String)reader["SerialNo"];
                                            string oSerialNo = oSerNo.ToString();
                                            oSSCC = oSerialNo.ToString();
                                        }
                                        catch (Exception cc)
                                        {
                                            string oError = cc.Message;
                                            _logger.Error("Error getting SSCC for Delivery " + delivery.DocNum + " Ln# " + docLine.ToString() + " => " + oError);
                                            oSSCC = "";
                                        }


                                        //detail.SerialNumber = (String)reader["SSCC"];
                                        if (!String.IsNullOrWhiteSpace(oSSCC))
                                        {   // 05-23-2021
                                            detail.SerialNumber = oSSCC;
                                            detail.SSCC = detail.SerialNumber; // 04-01-2022
                                        } // 05-23-2021
                                        // 07-19-2021 begin
                                        if (edi850HeaderRecord.CardCode == "TeeZed" && !String.IsNullOrWhiteSpace(detail.SerialNumber) && detail.SerialNumber.StartsWith("81088103"))
                                        {
                                            try
                                            {
                                                detail.SerialNumber = detail.SerialNumber.Replace("81088103", "93127420");
                                                detail.SSCC = detail.SerialNumber; // 04-01-2022
                                            }
                                            catch (Exception sn)
                                            {
                                                _logger.Error("Error updating SSCC => " + sn.Message);
                                            }
                                        }
                                        // 07-19-2021 end

                                        // 02-22-2022 begin
                                        if (oIs3PL == "Y")
                                        {
                                            detail.SerialNumber = "";
                                            detail.SSCC = oSSCC;
                                        }
                                        // 02-22-2022 end
                                        // 01-21-2017 end
                                        if (detail.SerialNumber != null && detail.SerialNumber.Length > 0)
                                        {
                                            detail.SerialNumber = detail.SerialNumber.Trim().PadLeft(20, '0');
                                            detail.SSCC = detail.SerialNumber; // 04-01-2022
                                        }
                                        try
                                        {
                                            detail.LineNumber = Convert.ToInt32(reader["U_InfoW2LnNo"]);
                                        }
                                        catch (Exception Lno)
                                        {
                                            string oErr = Lno.Message;
                                            _logger.Error("856 Line # error for Delivery # " + delivery.DocNum + " =>" + oErr);
                                        }
                                        if (detail.LineNumber == 0)
                                        {
                                            _logger.Error("856 Line # is zero for Delivery " + delivery.DocNum);
                                            detail.LineNumber = 1;
                                        }
                                        try
                                        {
                                            detail.Quantity = Convert.ToDouble(reader["Quantity"]);
                                        }
                                        catch
                                        {
                                            detail.Quantity = Convert.ToDouble("1.0");
                                        }
                                        // 07-23-2019 begin
                                        try
                                        {
                                            detail.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]);
                                        }
                                        catch
                                        {
                                            detail.QtyOrdered = detail.Quantity;
                                        }
                                        // 07-23-2019 end
                                        // 02-22-2019 begin
                                        String oTrackNo = "";
                                        try
                                        {
                                            oTrackNo = reader["PkgTrackNo"].ToString();
                                        }
                                        catch (Exception err)
                                        {
                                            string oErr = err.Message;
                                            oTrackNo = "";
                                        }
                                        // 07-31-2019 begin
                                        if (String.IsNullOrWhiteSpace(oTrackNo) || oTrackNo.Trim().Length == 0)
                                        {
                                            if (iNextTrack < oTrackNos.Length)
                                            {
                                                oTrackNo = oTrackNos[iNextTrack];
                                                iNextTrack = iNextTrack + 1;
                                            }
                                            else
                                            {
                                                oTrackNo = oTrackNos[0];
                                            }
                                        }
                                        /*
                                        if (oTrackNo.Trim().Length == 0)
                                        {
                                            String oBOL = "";
                                            try
                                            {
                                                oBOL = reader["BOL"].ToString();

                                            }
                                            catch (Exception err)
                                            {
                                                string oErr = err.Message;
                                                oBOL = "";
                                            }
                                            if (oBOL.Contains(", "))
                                            {
                                                oBOL.Replace(", ", " ");
                                            }
                                            if (oBOL.Contains(","))
                                            {
                                                oBOL.Replace(",", " ");
                                            }
                                            string[] oTrackList = oBOL.Split(' ');
                                            int i = oTrackList.Count();
                                            if (i > 1)
                                            {
                                                oTrackNo = oTrackList[0];
                                            }
                                            else
                                            {
                                                oTrackNo = oBOL.Trim();
                                            }
                                        }
                                        else
                                        {
                                            string saveTrackNo = oTrackNo;
                                            if (oTrackNo.Contains(" , "))
                                            {
                                                oTrackNo.Replace(" , ", " ");
                                            }
                                            if (oTrackNo.Contains(" ,"))
                                            {
                                                oTrackNo.Replace(", ", " ");
                                            }
                                            if (oTrackNo.Contains(", "))
                                            {
                                                oTrackNo.Replace(", ", " ");
                                            }

                                            if (oTrackNo.Contains(","))
                                            {
                                                oTrackNo.Replace(",", " ");
                                            }
                                            string[] oTrackList = oTrackNo.Split(' ');
                                            int i = oTrackList.Count();
                                            if (i > 1)
                                            {
                                                oTrackNo = oTrackList[0];
                                            }
                                            else
                                            {
                                                oTrackNo = saveTrackNo.Trim();
                                            }
                                        }
                                        */
                                        // 07-31-2019 end
                                        /* // 10-14-2019 begin                                       
                                         try
                                         {
                                             if (!String.IsNullOrWhiteSpace(oTrackNo) && oTrackNo.Trim().Length > 0)
                                             {  // 10-14-2019 end 
                                                 oTrackNo = oTrackNo.Replace(",", "");
                                             } 
                                            // 10-14-2019 begin 
                                             if (oTrackNo.Trim().Length > 48)
                                             {
                                                 oTrackNo = oTrackNo.Substring(0, 48);
                                             }
                                             else
                                             {
                                                 if (edi856HeaderRecord.BillOfLading.Trim().Length > 48)
                                                 {
                                                     oTrackNo = edi856HeaderRecord.BillOfLading.Substring(0, 48);
                                                 }
                                                 else
                                                 {
                                                     oTrackNo = edi856HeaderRecord.BillOfLading;
                                                 }
                                             }
                                         }
                                         catch (Exception t)
                                         {
                                             _logger.Debug("Error updating track # =>" + t.Message);
                                             oTrackNo = delivery.U_Info_BOL;
                                         }
                                         // 10-14-2019 end */
                                        detail.TrackingNumber = oTrackNo;

                                        try
                                        {
                                            detail.ItemUPC = reader["ItemUPC"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemUPC = "";
                                        }
                                        // 02-22-2019 end
                                        // 04-08-2019 begin
                                        try
                                        {
                                            detail.Warehouse = reader["Warehouse"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.Warehouse = "";
                                        }
                                        try
                                        {
                                            detail.ItemStatus = reader["ItemStatus"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemStatus = "";
                                        }
                                        if (detail.ItemStatus == "IA" && delivery.CardCode == "HOMEDEPOT")
                                        {
                                            detail.ItemStatus = "AC";
                                        }
                                        else if (detail.ItemStatus == "IA" && delivery.CardCode == "LOWES")
                                        {
                                            detail.ItemStatus = "AR";
                                        }
                                        try
                                        {
                                            detail.ItemReason = reader["ItemReason"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemReason = "";
                                        }
                                        // 04-08-2019 end
                                        // 07-23-2019 begin
                                        detail.LineItemStatus = detail.ItemStatus;
                                        detail.LineItemReason = detail.ItemReason;
                                        try
                                        {
                                            detail.LineNumber = Convert.ToInt32(reader["U_InfoW2LnNo"]);
                                        }
                                        catch (Exception Lno2)
                                        {
                                            string oErr = Lno2.Message;
                                            _logger.Error("856 Line # error for Delivery # " + delivery.DocNum + " =>" + oErr);
                                        }
                                        // 07-23-201 end
                                        var detail850 = (from v in edi850HeaderRecord.Details
                                                         where v.VendorItemCode.Equals(detail.VendorItemCode, StringComparison.InvariantCultureIgnoreCase)
                                                         && edi850HeaderRecord.SalesOrderKey > 0 // 07-23-2019
                                                         select v).FirstOrDefault();
                                        if (detail850 != null)
                                        {
                                            if (!String.IsNullOrWhiteSpace(detail850.BuyerItemCode))
                                            {
                                                detail.BuyerItemCode = detail850.BuyerItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
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
                                                    detail.BuyerItemCode = (String)reader["SubCatNum"];
                                                    // 06-14-2024 begin
                                                    if (BPList.Contains(oSBOCardCode))
                                                    {
                                                        detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                    }
                                                    // 06-14-2024 end
                                                }
                                            }
                                            if (!String.IsNullOrWhiteSpace(detail850.ItemDescription))
                                            {
                                                // 10-14-2021 begin
                                                // remove quotes from item description
                                                //detail.ItemDescription = detail850.ItemDescription; 
                                                string oItmDesc = detail850.ItemDescription;
                                                oItmDesc = oItmDesc.Replace('"', ' ');
                                                detail.ItemDescription = oItmDesc;
                                                // 10-14-2021 end
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
                                                // 10-14-2021 begin
                                                // remove quotes from item description
                                                oItemDesc = oItemDesc.Replace('"', ' ');
                                                detail.ItemDescription = oItemDesc;
                                                // 10-14-2021 end
                                                detail.ItemDescription = oItemDesc;
                                            }
                                            if (detail.LineNumber == 0)
                                            {
                                                detail.LineNumber = detail850.LineNumber;
                                            }
                                            // 07-23-201 begin
                                            if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                            {
                                                detail.VendorItemCode = detail850.VendorItemCode;
                                            }
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                            // 05-21-2021 begin
                                            if (detail850.UnitPrice == null)
                                            {
                                                detail.UnitPrice = Convert.ToDecimal("0.00");
                                            }
                                            // 05-21-2021 end
                                            else if (detail.UnitPrice == Convert.ToDecimal("0.00"))
                                            {
                                                try
                                                {
                                                    detail.UnitPrice = Convert.ToDecimal(detail850.UnitPrice);
                                                }
                                                catch
                                                {

                                                }
                                            }
                                            // 07-23-201 end
                                            // ** temp
                                            /*
                                            if (edi850HeaderRecord.CardCode == "HDCL" || delivery.CardCode == "HOMEDEPOT")
                                            {
                                                string debugMsg = "Delivery #" + delivery.DocNum + " Line# " + detail.LineNumber + " Item#" + detail.VendorItemCode +
                                               " Qty: " + detail.Quantity + " Ordered: " + detail.QtyOrdered;
                                                _logger.Debug(debugMsg);
                                            }*/
                                        } // 08-10-2017 begin
                                        else
                                        {
                                            // 07-26-2023 begin
                                            Object oBuyerItem = "";
                                            try
                                            {
                                                oBuyerItem = reader["SubCatNum"];
                                            }
                                            catch
                                            {
                                                oBuyerItem = "";
                                            }
                                            //detail.BuyerItemCode = (String)reader["SubCatNum"];
                                            try
                                            {
                                                detail.BuyerItemCode = Convert.ToString(oBuyerItem);
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            catch (Exception b)
                                            {
                                                _logger.Debug("Invalid BuyerItemCode/SubCatNum: " + b.Message);
                                            }
                                            // 07-26-2023 end
                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            //detail.ItemDescription = (String) reader["Dscription"];
                                            string oItmDesc ="";
                                            try
                                            {
                                                oItmDesc = (String)reader["Dscription"];
                                                oItmDesc = oItmDesc.Replace('"', ' ');
                                            }
                                            catch
                                            {
                                                oItmDesc = "";
                                            }
                                            detail.ItemDescription = oItmDesc;
                                            // 10-14-2021 end
                                            // 01-22-2018 begin
                                            if (detail.BuyerItemCode == null || detail.BuyerItemCode.Trim().Length == 0)
                                            {
                                                detail.BuyerItemCode = detail.VendorItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            // 01-22-2018 end
                                        } // 08-10-2017 end
                                        // 07-23-2019 begin
                                        if (detail.LineNumber == 0)
                                        {
                                            _logger.Debug("856 Line # is zero for Delivery " + delivery.DocNum);
                                            detail.LineNumber = 1;
                                        }
                                        // 07-23-2019 end
                                        // 07-30-2019 begin
                                        try
                                        {
                                            detail.FreightClass = (String)reader["FreightClass"];
                                        }
                                        catch
                                        {
                                            detail.FreightClass = "";
                                        }
                                        try
                                        {
                                            detail.NMFC = (String)reader["NMFC"];
                                        }
                                        catch
                                        {
                                            detail.NMFC = "";
                                        }
                                        // 07-30-2019 end
                                        /*
                                        // 02-28-2023 begin
                                        // work around to force correct tracking number for Home Depot
                                        if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                                        {
                                            detail.SSCC = delivery.U_Info_BOL;                                            ;
                                            detail.SerialNumber = delivery.U_Info_BOL;
                                        }
                                        // 02-28-2023 end
                                         */
                                    }
                                }
                            }
                            else // 08-29-2017 begin
                            {
                                bProdumex = false;

                            }// 08-29-2017 end
                        }
                    }
                    if (!(delivery.DocDueDate == null))
                    {
                        edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                    }
                    else
                    {
                        edi856HeaderRecord.AsnShipDate = DateTime.Today;
                    }

                    edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                    // 09-15-2021 begin
                    if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        edi856HeaderRecord.BillOfLading = edi856HeaderRecord.BillOfLading.Replace("-", "");
                    }
                    // 09-15-2021 end
                    if (delivery.TrnspCode > 0)
                    {
                        String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                        using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                edi856HeaderRecord.TransportationMethod = (String)reader[0];
                            }
                        }
                    }

                    if (String.IsNullOrWhiteSpace(edi856HeaderRecord.TransportationMethod))
                    {
                        edi856HeaderRecord.TransportationMethod = "M";
                    }
                    // 01-13-2022 begin
                    if (edi850HeaderRecord.CardCode.StartsWith("TSC"))
                    {
                        edi856HeaderRecord.TransportationMethod = edi850HeaderRecord.TransportMethod;
                        if (edi856HeaderRecord.TransportationMethod == "M")
                        {
                            edi856HeaderRecord.TransportationMethod = "LT";
                        }
                        String sql = "select top 1 isnull(TrnspName, '') as TransportName from OSHP With(NOLOCK) " +
                                     "where TrnspCode = " + delivery.TrnspCode;
                        try
                        {
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    edi856HeaderRecord.TransportRouting = (String)reader[0];
                                }
                            }
                        }
                        catch (Exception tr)
                        {
                            _logger.Error("Error getting transport routing =>" + tr.Message);
                        }
                    }
                    // 01-13-2022 end
                    sqlConnection.Close();
                }
            }
            else // 07-23-2019
                // 08-29-2017 begin
                if (bProdumex == false)
                {
                    //_logger.Debug("Not Produmex");
                    // 07-12-2019 begin

                    // 07-12-2019 end
                    // 04-10-2019 begin
                    string oConnectionName = getConnectionName(edi850HeaderRecord.CardCode);
                    if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                    {
                        oConnectionName = "WebApiDbContext";
                    }
                    connectionString = ConfigurationManager.ConnectionStrings[oConnectionName].ConnectionString;
                    // connectionString = ConfigurationManager.ConnectionStrings["WebApiDbContext"].ConnectionString;
                    // 04-10-2019 end
                    if (String.IsNullOrWhiteSpace(connectionString))
                    {
                        String msg = "Invalid WebAPIConnectionString found in Web.config";
                        _logger.Error(msg);
                        throw new WebApiException(msg);
                    }
                    using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                    {
                        sqlConnection.Open();
                        // 07-12-2019 begin
                        try
                        {
                            String sql = "Update DLN1 set U_InfoW2MpId = coalesce((select U_InfoW2MpId from RDR1 where DocEntry = DLN1.BaseEntry and LineNum = DLN1.BaseLine),'') " +
                                         "where len(ltrim(rtrim(coalesce(U_InfoW2MpId,'')))) = 0 and DocEntry = " + delivery.DocEntry;
                            //_logger.Debug("Checking for missing sscc using SQL: " + sql);
                            try
                            {
                                using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                                {
                                    command.ExecuteNonQuery();
                                }
                            }
                            catch (Exception del2)
                            {
                                _logger.Error("Error updating delivery line package id numbers => " + del2.Message);
                            }
                        }
                        catch (Exception Mid)
                        {
                            _logger.Error("Error updating delivery line package id numbers => " + Mid.Message);
                        }
                        finally
                        {
                            sqlConnection.Close();

                        }
                        // 07-12-2019 end


                        String sqlToRun = String.Format(DeliveryQuery, delivery.DocEntry, edi850HeaderRecord.HeaderId);
                        //_logger.Debug("Running Delivery SQL: " + sqlToRun);
                        sqlConnection.Open();
                        using (SqlCommand command2 = new SqlCommand(sqlToRun, sqlConnection))
                        {

                            using (SqlDataReader reader2 = command2.ExecuteReader())
                            {
                                if (reader2.HasRows)
                                {
                                    while (reader2.Read())
                                    {
                                        Edi856ItemDetailRecord detail = new Edi856ItemDetailRecord();
                                        edi856HeaderRecord.Details.Add(detail);
                                        detail.VendorItemCode = (String)reader2["ItemCode"];
                                        // 06-14-2024 begin
                                        if (BPList.Contains(oSBOCardCode))
                                        {
                                            detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                        }
                                        // 06-14-2024 end
                                        // 02-22-2022 begin 
                                        if (oIs3PL == "Y")
                                        {
                                            if (edi850HeaderRecord.CardCode == "TeeZed" && !String.IsNullOrWhiteSpace(detail.SSCC) && detail.SSCC.StartsWith("81088103"))
                                            {
                                                try
                                                {
                                                    detail.SSCC = detail.SSCC.Replace("81088103", "93127420");
                                                }
                                                catch (Exception sn)
                                                {
                                                    _logger.Error("Error updating SSCC => " + sn.Message);
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // 02-22-2022 end
                                            // 07-19-2021 begin
                                            if (edi850HeaderRecord.CardCode == "TeeZed" && !String.IsNullOrWhiteSpace(detail.SerialNumber) && detail.SerialNumber.StartsWith("81088103"))
                                            {
                                                try
                                                {
                                                    detail.SerialNumber = detail.SerialNumber.Replace("81088103", "93127420");
                                                }
                                                catch (Exception sn)
                                                {
                                                    _logger.Error("Error updating SSCC => " + sn.Message);
                                                }
                                            }
                                            // 07-19-2021 end
                                        } // 02-22-2022
                                        if (detail.SerialNumber != null && oIs3PL == "N")
                                        {
                                            detail.SerialNumber = detail.SerialNumber.Trim().PadLeft(20, '0');
                                            detail.SSCC = detail.SerialNumber; // 04-01-2022
                                        }
                                        else
                                        {
                                            // 07-19-2021 begin
                                            if (oIs3PL == "Y" || edi850HeaderRecord.CardCode == "TeeZed")
                                            {
                                                String oSSCC = "";
                                                try
                                                {
                                                    oSSCC = ((String)reader2["SSCC"]).Trim();
                                                    if (!String.IsNullOrWhiteSpace(oSSCC) && oSSCC.StartsWith("81088103"))
                                                        try
                                                        {
                                                            oSSCC = oSSCC.Replace("81088103", "93127420");
                                                        }
                                                        catch (Exception sn)
                                                        {
                                                            _logger.Error("Error updating SSCC => " + sn.Message);
                                                        }
                                                    //detail.SerialNumber = oSSCC.Trim().PadLeft(20, '0');
                                                    detail.SSCC = oSSCC.Trim().PadLeft(20, '0'); // 02-22-2022
                                                }
                                                catch (Exception cc)
                                                {
                                                    string oError = cc.Message;
                                                    _logger.Error("Error getting SSCC for Delivery " + delivery.DocNum + " TeeZed (line 1055) => " + oError);
                                                    oSSCC = "";
                                                }
                                            }
                                            else
                                            {
                                                // 07-19-2021 end
                                                detail.SerialNumber = ((String)reader2["SSCC"]).Trim().PadLeft(20, '0');
                                                detail.SSCC = detail.SerialNumber; // 04-01-2022
                                            } // 07-19-2021 
                                        }
                                        detail.Quantity = Convert.ToDouble(reader2["Quantity"]);
                                        // 07-23-2019 begin
                                        try
                                        {
                                            detail.QtyOrdered = Convert.ToDouble(reader2["OrderedQty"]);
                                        }
                                        catch
                                        {
                                            //detail.QtyOrdered = detail.Quantity;
                                        }
                                        // 07-23-2019 end
                                        var detail850 = (from v in edi850HeaderRecord.Details
                                                         where v.VendorItemCode.Equals(detail.VendorItemCode, StringComparison.InvariantCultureIgnoreCase)
                                                         && edi850HeaderRecord.SalesOrderKey > 0 // 07-23-2019
                                                         select v).FirstOrDefault();
                                        if (detail850 != null)
                                        {
                                            detail.BuyerItemCode = detail850.BuyerItemCode;
                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            //detail.ItemDescription = detail850.ItemDescription; 
                                            string oItmDesc = detail850.ItemDescription;
                                            oItmDesc = oItmDesc.Replace('"', ' ');
                                            detail.ItemDescription = oItmDesc;
                                            // 10-14-2021 end
                                            // 07-23-2019 begin
                                            if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                            {
                                                detail.VendorItemCode = detail850.VendorItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            if (detail.LineNumber == 0)
                                            {
                                                detail.LineNumber = detail850.LineNumber;
                                            }
                                            // 05-21-2021 begin
                                            if (detail850.GrossPkgWeight == null)
                                            {
                                                detail.GrossPkgWeight = ("0.00");
                                            }
                                            else
                                            {
                                                try
                                                {
                                                    string oPkgWeight = detail850.GrossPkgWeight.ToString();
                                                    double oGrsPkgWgt = Convert.ToDouble(oPkgWeight);

                                                    detail.GrossPkgWeight = (detail850.GrossPkgWeight).ToString();
                                                }
                                                catch (Exception wt)
                                                {
                                                    _logger.Error("Error converting gross pkg wt => " + wt.Message);
                                                    detail.GrossPkgWeight = ("0.00");
                                                }
                                            }
                                            if (detail850.UnitPrice == null)
                                            {
                                                detail.UnitPrice = Convert.ToDecimal("0.00");
                                            }
                                            // 05-21-2021 end
                                            else if (detail.UnitPrice == Convert.ToDecimal("0.00"))
                                            {
                                                try
                                                {
                                                    detail.UnitPrice = Convert.ToDecimal(detail850.UnitPrice);
                                                }
                                                catch
                                                {

                                                }
                                            }
                                            // 07-23-2019 end
                                        }
                                        else
                                        {
                                            detail.BuyerItemCode = (String)reader2["SubCatNum"];
                                            // 01-22-2018 begin
                                            if (detail.BuyerItemCode == null || detail.BuyerItemCode.Trim().Length == 0)
                                            {
                                                detail.BuyerItemCode = detail.VendorItemCode;
                                            }
                                            // 01-22-2018 end
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                            detail.ItemDescription = (String)reader2["Dscription"];
                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            //detail.ItemDescription = (String) reader2["Dscription"];
                                            string oItmDesc = (String)reader2["Dscription"];
                                            oItmDesc = oItmDesc.Replace('"', ' ');
                                            detail.ItemDescription = oItmDesc;
                                            // 10-14-2021 end
                                        }
                                        // 02-22-2019 begin
                                        String oTrackNo = "";
                                        try
                                        {
                                            oTrackNo = reader2["PkgTrackNo"].ToString();

                                        }
                                        catch (Exception err)
                                        {
                                            string oErr = err.Message;
                                            oTrackNo = "";
                                        }
                                        // 07-31-2019 begin
                                        if (String.IsNullOrWhiteSpace(oTrackNo) || oTrackNo.Trim().Length == 0)
                                        {
                                            if (iNextTrack < oTrackNos.Length)
                                            {
                                                oTrackNo = oTrackNos[iNextTrack];
                                                iNextTrack = iNextTrack + 1;
                                            }
                                            else
                                            {
                                                oTrackNo = oTrackNos[0];
                                            }
                                        }
                                        /*
                                        if (oTrackNo.Trim().Length == 0)
                                        {
                                            String oBOL = "";
                                            try
                                            {
                                                oBOL = reader2["BOL"].ToString();

                                            }
                                            catch (Exception err)
                                            {
                                                string oErr = err.Message;
                                                oBOL = "";
                                            }
                                            if (oBOL.Contains(", "))
                                            {
                                                oBOL.Replace(", ", " ");
                                            }
                                            if (oBOL.Contains(","))
                                            {
                                                oBOL.Replace(",", " ");
                                            }
                                            string[] oTrackList = oBOL.Split(' ');
                                            int i = oTrackList.Count();
                                            if (i > 1)
                                            {
                                                oTrackNo = oTrackList[0];
                                            }
                                            else
                                            {
                                                oTrackNo = oBOL.Trim();
                                            }
                                        } */
                                        // 07-31-2019 end
                                        detail.TrackingNumber = oTrackNo;
                                        // 02-22-2019 end

                                        // 04-08-2019 begin
                                        try
                                        {
                                            detail.Warehouse = reader2["WhsCode"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.Warehouse = "";
                                        }
                                        try
                                        {
                                            detail.ItemStatus = reader2["ItemStatus"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemStatus = "";
                                        }
                                        if (detail.ItemStatus == "IA" && delivery.CardCode == "HOMEDEPOT")
                                        {
                                            detail.ItemStatus = "AC";
                                        }
                                        else if (detail.ItemStatus == "IA" && delivery.CardCode == "LOWES")
                                        {
                                            detail.ItemStatus = "AR";
                                        }
                                        try
                                        {
                                            detail.ItemReason = reader2["ItemReason"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemReason = "";
                                        }
                                        // 04-08-2019 end
                                        // 07-23-2019 begin
                                        detail.LineItemStatus = detail.ItemStatus;
                                        detail.LineItemReason = detail.ItemReason;
                                        // 07-2019 end
                                        // 07-30-2019 begin
                                        detail.FreightClass = (String)reader2["FreightClass"];
                                        detail.NMFC = (String)reader2["NMFC"];
                                        // 07-30-2019 end

                                        // ** Temp comments
                                        /*
                                        _logger.Debug("Delivery #" + delivery.DocNum + " Line# " + detail.LineNumber + " Item#" + detail.VendorItemCode +
                                            " Qty: " + detail.Quantity +
                                            " Ordered: " + detail.QtyOrdered);
                                        */
                                    }
                                }
                            }
                        }
                        if (!(delivery.DocDueDate == null))
                        {
                            edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                        }
                        else
                        {
                            edi856HeaderRecord.AsnShipDate = DateTime.Today;
                        }
                        edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                        // 09-15-2021 begin
                        if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                        {
                            edi856HeaderRecord.BillOfLading = edi856HeaderRecord.BillOfLading.Replace("-", "");
                        }
                        // 09-15-2021 end
                        if (delivery.TrnspCode > 0)
                        {
                            String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    edi856HeaderRecord.TransportationMethod = (String)reader[0];
                                }
                            }
                        }

                        if (String.IsNullOrWhiteSpace(edi856HeaderRecord.TransportationMethod))
                        {
                            edi856HeaderRecord.TransportationMethod = "M";
                        }
                        // 01-13-2022 begin
                        if (edi850HeaderRecord.CardCode.StartsWith("TSC"))
                        {
                            edi856HeaderRecord.TransportationMethod = edi850HeaderRecord.TransportMethod;
                            if (edi856HeaderRecord.TransportationMethod == "M")
                            {
                                edi856HeaderRecord.TransportationMethod = "LT";
                            }
                            String sql = "select top 1 isnull(TrnspName, '') as TransportName from OSHP With(NOLOCK) " +
                                         "where TrnspCode = " + delivery.TrnspCode;
                            try
                            {
                                using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                                {
                                    using (SqlDataReader reader = command.ExecuteReader())
                                    {
                                        reader.Read();
                                        edi856HeaderRecord.TransportRouting = (String)reader[0];
                                    }
                                }
                            }
                            catch (Exception tr)
                            {
                                _logger.Error("Error getting transport routing =>" + tr.Message);
                            }
                        }
                        // 01-13-2022 end
                        sqlConnection.Close();
                    }
                }
            // 08-29-2017 end
        }

        // 09-24-2021 begin
        public void OnPostProcess856Record(Delivery delivery, Edi850HeaderRecord edi850HeaderRecord, Edi856HeaderRecord edi856HeaderRecord, String oIs3PL)
        {
            //_logger.Debug("Starting OnPostProcess856Pack");
            // 06-14-2024 begin
            string oSBOCardCode = delivery.CardCode;
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-14-2024 end
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
            //_logger.Debug(oPmxConnectionName);
            String connectionString = ConfigurationManager.ConnectionStrings[oPmxConnectionName].ConnectionString;
            //_logger.Debug(connectionString);
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
                //_logger.Debug("Produmex Delivery");
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String sqlToRun = "execute [dbo].[Infocus_EDI_PMX_DelPack] " + edi850HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    //_logger.Debug("Running Produmex SQL: " + sqlToRun);
                    int iNextRow = 0;
                    string oLoopingStructure = "0002";

                    using (SqlCommand command = new SqlCommand(sqlToRun, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {

                                iNextRow = iNextRow + 1;
                                int NoFields = reader.FieldCount;
                                string oCurPallet = "";
                                string oCurSSCC = "";
                                Edi856PackRecord pack = new Edi856PackRecord();
                                Edi856PalletRecord pallet = new Edi856PalletRecord();
                                while (reader.Read())
                                {
                                    string oHasPallets = (String)reader["HasPallets"];
                                    string oOverboxing = (String)reader["Overboxing"];
                                    object[] recordData = new object[reader.FieldCount];
                                    reader.GetSqlValues(recordData);

                                    //_logger.Error("Processing  row " + iNextRow);
                                    if (iNextRow == 1)
                                    {
                                        if (oHasPallets.ToUpper() == "YES")
                                        {
                                            edi856HeaderRecord.Structure = "0055";
                                        }
                                        else if (oOverboxing.ToUpper() == "YES")
                                        {
                                            edi856HeaderRecord.Structure = "0001";
                                        }
                                        else
                                        {
                                            edi856HeaderRecord.Structure = "0002";
                                        }
                                        oLoopingStructure = edi856HeaderRecord.Structure;
                                    }
                                    iNextRow = iNextRow + 1;
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
                                    String oTrackNo = "";
                                    try
                                    {
                                        oTrackNo = (String)reader["PkgTrackNo"].ToString();
                                    }
                                    catch (Exception tn)
                                    {
                                        String oErr = tn.Message;
                                    }
                                    if (String.IsNullOrWhiteSpace(oTrackNo) || oTrackNo.Trim().Length == 0)
                                    {
                                        oTrackNo = delivery.TrackNo;
                                    }
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
                                            // 10-20-2021 begin
                                            try
                                            {
                                                string oQty = reader["PltPkgs"].ToString();
                                                pallet.Quantity = Convert.ToDouble(oQty);
                                            }
                                            catch (Exception pq)
                                            {
                                                String oErr = pq.Message;
                                                _logger.Error("Error getting number of packages for pallet " + oMasterSSCC + " =>" + oErr);
                                                pallet.Quantity = Convert.ToDouble("1.0");
                                            }
                                            // 10-20-2021 end
                                            try
                                            {
                                                string oPltDUOM = (String)reader["PltLenUOM"];
                                                if (!String.IsNullOrWhiteSpace(oPltDUOM))
                                                {
                                                    string oLength = (String)reader["PltLen"].ToString();
                                                    pallet.Length = Convert.ToDouble(oLength);
                                                    pallet.DimUOM = (String)reader["PltLenUOM"];
                                                    string oWidth = (String)reader["PltWidth"].ToString();
                                                    pallet.Width = Convert.ToDouble(oWidth);
                                                    string oHeight = (String)reader["PltHgt"].ToString();
                                                    pallet.Height = Convert.ToDouble(oHeight);
                                                }
                                                string oWgtUOM = (String)reader["PltWgtUOM"];
                                                if (!String.IsNullOrWhiteSpace(oWgtUOM))
                                                {
                                                    pallet.WeightUOM = oWgtUOM;
                                                    string oWeight = (String)reader["PltWgt"].ToString();
                                                    pallet.Weight = Convert.ToDouble(oWeight);
                                                    pallet.WeightQualifier = "A3";
                                                    pallet.WeightUOM = (String)reader["PltWgtUOM"];
                                                }
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
                                    string oTreeType = "";
                                    try
                                    {
                                        oTreeType = (String)reader["TreeType"].ToString();
                                    }
                                    catch (Exception tt)
                                    {
                                        String oErr = tt.Message;
                                        _logger.Error("Error getting TreeType =>" + oErr);
                                    }

                                    if (oCurSSCC != oSSCC)
                                    {
                                        pack = new Edi856PackRecord();
                                        pallet.Pack.Add(pack);
                                        oCurSSCC = oSSCC; // 10-14-2021
                                        pack.PackNo = oSSCC;
                                        pack.TrackingNo = (String)reader["PkgTrackNo"];
                                        pack.TrackNoQualifier = "CP";

                                        if (oTreeType == "N" || oTreeType == "S")
                                        {

                                            string docLine = "";
                                            try
                                            {
                                                docLine = reader["DocLine"].ToString();
                                            }
                                            catch
                                            {
                                                docLine = "";
                                            }
                                            string oValue = "";

                                            try
                                            {
                                                string oPkgWgtUOM = reader["PkgWgtUOM"].ToString();
                                                if (!String.IsNullOrWhiteSpace(oPkgWgtUOM))
                                                {
                                                    oValue = reader["PkgWeight"].ToString();
                                                    // _logger.Error("Package Weight " + oValue);
                                                    pack.Weight = Convert.ToDouble(oValue);
                                                    pack.WeightUOM = oPkgWgtUOM;
                                                }
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting shipment weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Weight = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                oValue = reader["PkgQty"].ToString();
                                                double oNoPkgs = Convert.ToDouble(oValue);
                                                if (oNoPkgs < Convert.ToDouble("1.0"))
                                                {
                                                    oNoPkgs = Convert.ToDouble("1.0");
                                                }
                                                pack.Quantity = oNoPkgs;
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting number of cartons for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + wt.Message);
                                                pack.Quantity = Convert.ToDouble("1.0");
                                            }
                                            // 11-09-2021 begin
                                            try
                                            {
                                                string oLenUOM = reader["PkgLenUOM"].ToString();
                                                if (!String.IsNullOrWhiteSpace(oLenUOM))
                                                {
                                                    pack.DimUOM = oLenUOM;
                                                }
                                            }
                                            catch (Exception pLenUom)
                                            {
                                                _logger.Error("Error getting number of length uom for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + pLenUom.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgLen"].ToString();
                                                // _logger.Error("Package Length " + oValue);
                                                pack.Length = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting package length for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Length = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oWidthUOM = reader["PkgWdtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oWidthUOM))
                                                    {
                                                        pack.DimUOM = oWidthUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception pwdt)
                                            {
                                                _logger.Error("Error getting package width UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pwdt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgWidth"].ToString();
                                                //_logger.Error("Package Width " + oValue);
                                                pack.Width = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception pLen)
                                            {
                                                _logger.Error("Error getting package width for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pLen.Message);
                                                pack.Width = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oHgtUOM = reader["PkgHgtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oHgtUOM))
                                                    {
                                                        pack.DimUOM = oHgtUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgHgt"].ToString();
                                                //_logger.Error("Package Height " + oValue);
                                                pack.Height = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                                pack.Height = Convert.ToDouble("0.00");
                                            }
                                            if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                            {
                                                pack.DimUOM = "IN";
                                            }
                                            // 11-09-2021 end
                                        }
                                        else // component
                                        {
                                            string docLine = "";
                                            try
                                            {
                                                docLine = reader["DocLine"].ToString();
                                            }
                                            catch
                                            {
                                                docLine = "";
                                            }
                                            string oValue = "";

                                            try
                                            {
                                                string oPkgWgtUOM = reader["PkgWgtUom"].ToString();

                                                if (!String.IsNullOrWhiteSpace(oPkgWgtUOM))
                                                {
                                                    oValue = reader["PkgWgt"].ToString();
                                                    //_logger.Error("Package Weight " + oValue);
                                                    pack.Weight = Convert.ToDouble(oValue);
                                                    pack.WeightUOM = oPkgWgtUOM;
                                                }
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting package weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Weight = Convert.ToDouble("0.00");
                                            }
                                            // 11-09-2021 begin
                                            try
                                            {
                                                string oLenUOM = reader["PkgLenUOM"].ToString();
                                                if (!String.IsNullOrWhiteSpace(oLenUOM))
                                                {
                                                    pack.DimUOM = oLenUOM;
                                                }
                                            }
                                            catch (Exception pLenUom)
                                            {
                                                _logger.Error("Error getting number of length uom for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + pLenUom.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgLen"].ToString();
                                                // _logger.Error("Package Length " + oValue);
                                                pack.Length = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting package length for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Length = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oWidthUOM = reader["PkgWdtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oWidthUOM))
                                                    {
                                                        pack.DimUOM = oWidthUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception pwdt)
                                            {
                                                _logger.Error("Error getting package width UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pwdt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgWidth"].ToString();
                                                // _logger.Error("Package Width " + oValue);
                                                pack.Width = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception pLen)
                                            {
                                                _logger.Error("Error getting package width for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pLen.Message);
                                                pack.Width = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oHgtUOM = reader["PkgHgtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oHgtUOM))
                                                    {
                                                        pack.DimUOM = oHgtUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgHgt"].ToString();
                                                //_logger.Error("Package Height " + oValue);
                                                pack.Height = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                                pack.Height = Convert.ToDouble("0.00");
                                            }
                                            if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                            {
                                                pack.DimUOM = "IN";
                                            }
                                            // 11-09-2021 end
                                            try
                                            {
                                                oValue = reader["PackageCnt"].ToString();
                                                double oNoPkgs = Convert.ToDouble(oValue);
                                                if (oNoPkgs < Convert.ToDouble("1.0"))
                                                {
                                                    oNoPkgs = Convert.ToDouble("1.0");
                                                }
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
                                    }
                                    catch (Exception e)
                                    {
                                        items.LineNumber = 1;
                                    }

                                    items.VendorItemCode = (String)reader["ItemCode"];
                                    // 06-14-2024 begin
                                    if (BPList.Contains(oSBOCardCode))
                                    {
                                        items.VendorItemCode = items.VendorItemCode.ToUpper();
                                    }
                                    // 06-14-2024 end
                                    try
                                    {
                                        string oBuyerItemCode = (String)reader["SubCatNum"];
                                        if (!String.IsNullOrWhiteSpace(oBuyerItemCode))
                                        {
                                            items.BuyerItemCode = oBuyerItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                items.BuyerItemCode = items.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                        }
                                    }
                                    catch (Exception bitm)
                                    {
                                        String oErr = bitm.Message;
                                        _logger.Error("Error setting buyer item code =>" + oErr);
                                    }
                                    String DocLineNum = "";
                                    try
                                    {
                                        DocLineNum = (String)reader["DocLineNum"].ToString();
                                    }
                                    catch (Exception dl)
                                    {
                                        String oErr = dl.Message;
                                        _logger.Error("Error delivery line no for Delivery " + delivery.DocNum.ToString() + "=>" + oErr);
                                        DocLineNum = "";
                                    }
                                    if (oTreeType == "N" || oTreeType == "S")
                                    {
                                        try
                                        {
                                            items.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]);
                                        }
                                        catch (Exception qty1)
                                        {
                                            String oErr = qty1.Message;
                                            _logger.Error("Error setting qty ordered =>" + oErr);
                                        }
                                        try
                                        {
                                            double oItmQty = Convert.ToDouble(reader["Quantity"]);
                                            if (oItmQty < Convert.ToDouble("1.0"))
                                            {
                                                oItmQty = Convert.ToDouble("1.0");
                                            }
                                            items.Quantity = oItmQty;
                                        }
                                        catch (Exception qty2)
                                        {
                                            String oErr = qty2.Message;
                                            _logger.Error("Error setting qty shipped =>" + oErr);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            //items.QtyOrdered = Convert.ToDouble(reader["ItmQty"]);
                                            //items.Quantity = items.QtyOrdered;
                                            items.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]); // 10-20-2021                                           
                                        }
                                        catch (Exception qty1)
                                        {
                                            String oErr = qty1.Message;
                                            _logger.Error("Error setting qty ordered =>" + oErr);
                                        }
                                        // 10-20-2021 begin
                                        try
                                        {
                                            double oItmQty = Convert.ToDouble(reader["Quantity"]);
                                            if (oItmQty < Convert.ToDouble("1.0"))
                                            {
                                                oItmQty = Convert.ToDouble("1.0");
                                            }
                                            items.Quantity = oItmQty;
                                        }
                                        catch (Exception qty2)
                                        {
                                            String oErr = qty2.Message;
                                            _logger.Error("Error setting qty shipped =>" + oErr);
                                        }
                                        // 10-20-2021 end
                                    }
                                    // 10-14-2021 begin
                                    try
                                    {
                                        items.ShipmentCartons = Convert.ToDouble(reader["PkgQty"]);
                                        if (items.ShipmentCartons < Convert.ToDouble("1.0"))
                                        {
                                            items.ShipmentCartons = Convert.ToDouble("1.0");
                                        }
                                    }
                                    catch (Exception isc)
                                    {
                                        _logger.Error("Error getting item number of item packages" + isc.Message);
                                        items.ShipmentCartons = Convert.ToDouble("1.0");
                                    }
                                    // 10-14-2021 end
                                    try
                                    {
                                        items.ItemUOM = (String)reader["ItmUOM"];
                                    }
                                    catch (Exception iu)
                                    {
                                        _logger.Error("Error getting item UOM" + iu.Message);
                                        items.ItemUOM = "EA";
                                    }
                                    // 10-14-2021 begin
                                    // remove quotes from item description
                                    //items.ItemDescription = (String) reader["Dscription"];
                                    string oItmDesc = (String)reader["Dscription"];
                                    oItmDesc = oItmDesc.Replace('"', ' ');
                                    items.ItemDescription = oItmDesc;
                                    // 10-14-2021 end
                                    if (String.IsNullOrWhiteSpace(oSSCC))
                                    {
                                        try
                                        {
                                            oSSCC = ((String)reader["SerialNo"]).Trim();
                                        }
                                        catch (Exception s2)
                                        {
                                            string oError = s2.Message;
                                            _logger.Error("Error getting SSCC for Delivery " + delivery.DocNum + " Ln# " + DocLineNum + " => " + oError);
                                            oSSCC = "";
                                        }
                                    }
                                    if (oIs3PL == "Y" || edi850HeaderRecord.CardCode == "TeeZed")
                                    {
                                        oSSCC = ((String)reader["SerialNo"]).Trim();
                                        if (!String.IsNullOrWhiteSpace(oSSCC) && oSSCC.StartsWith("81088103"))
                                            try
                                            {
                                                oSSCC = oSSCC.Replace("81088103", "93127420");
                                            }
                                            catch (Exception sn)
                                            {
                                                _logger.Error("Error updating SSCC for Delivery# " + delivery.DocNum + " Ln# " + DocLineNum + "=> " + sn.Message);
                                            }
                                        //items.SerialNumber = oSSCC.Trim().PadLeft(20, '0');
                                        items.SSCC = oSSCC.Trim().PadLeft(20, '0'); // 02-22-2022
                                    }
                                    else
                                    {
                                        // 07-19-2021 end
                                        //items.SerialNumber = ((String)reader["SSCC"]).Trim().PadLeft(20, '0');
                                        items.SerialNumber = oSSCC.Trim().PadLeft(20, '0'); // 09-27-2021
                                    }
                                    if (items.SerialNumber != null && items.SerialNumber.Length > 0)
                                    {
                                        items.SerialNumber = items.SerialNumber.Trim().PadLeft(20, '0');
                                    }

                                    if (items.LineNumber == 0)
                                    {
                                        // _logger.Debug("856 Line # is zero for Delivery " + delivery.DocNum);
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
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                items.BuyerItemCode = items.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
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
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    items.BuyerItemCode = items.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                        }
                                        if (!String.IsNullOrWhiteSpace(detail850.ItemDescription))
                                        {
                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            //items.ItemDescription = detail850.ItemDescription; 
                                            oItmDesc = detail850.ItemDescription;
                                            oItmDesc = oItmDesc.Replace('"', ' ');
                                            items.ItemDescription = oItmDesc;
                                            // 10-14-2021 end
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
                                            // 10-14-2021 begin
                                            // remove quotes from item description
                                            //items.ItemDescription = oItemDesc
                                            oItemDesc = oItemDesc.Replace('"', ' ');
                                            items.ItemDescription = oItemDesc;
                                            // 10-14-2021 end
                                        }
                                        if (items.LineNumber == 0)
                                        {
                                            items.LineNumber = detail850.LineNumber;
                                        }
                                        if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                        {
                                            items.VendorItemCode = detail850.VendorItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                items.VendorItemCode = items.VendorItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
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
                        edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                    }
                    else
                    {
                        edi856HeaderRecord.AsnShipDate = DateTime.Today;
                    }

                    edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                    if (edi850HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        edi856HeaderRecord.BillOfLading = edi856HeaderRecord.BillOfLading.Replace("-", "");
                    }
                    if (delivery.TrnspCode > 0)
                    {
                        String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                        using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                edi856HeaderRecord.TransportationMethod = (String)reader[0];
                            }
                        }
                    }

                    if (String.IsNullOrWhiteSpace(edi856HeaderRecord.TransportationMethod))
                    {
                        edi856HeaderRecord.TransportationMethod = "M";
                    }
                    // 01-13-2022 begin
                    if (edi850HeaderRecord.CardCode.StartsWith("TSC"))
                    {
                        edi856HeaderRecord.TransportationMethod = edi850HeaderRecord.TransportMethod;
                        if (edi856HeaderRecord.TransportationMethod == "M")
                        {
                            edi856HeaderRecord.TransportationMethod = "LT";
                        }
                        String sql = "select top 1 isnull(TrnspName, '') as TransportName from OSHP With(NOLOCK) " +
                                     "where TrnspCode = " + delivery.TrnspCode;
                        try
                        {
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    edi856HeaderRecord.TransportRouting = (String)reader[0];
                                }
                            }
                        }
                        catch (Exception tr)
                        {
                            _logger.Error("Error getting transport routing =>" + tr.Message);
                        }
                    }
                    // 01-13-2022 end
                    sqlConnection.Close();
                    sqlConnection.Dispose(); // 01-27-2023
                }
            }
            else if (bProdumex == false)
            {
                _logger.Error("Not Produmex  -- not implemented for non-Produmex warehouse");
            }
        }
        // 09-24-2021 end

        // 08-16-2023 begin
        public void OnPostProcess856Record(Delivery delivery, Edi940HeaderRecord Edi940HeaderRecord, Edi856HeaderRecord edi856HeaderRecord, String oIs3PL)
        {
            //_logger.Debug("Starting OnPostProcess856Pack");
            // 06-14-2024 begin
            string oSBOCardCode = delivery.CardCode;
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-14-2024 end
            string oPmxConnectionName = getPMXConnectionName(Edi940HeaderRecord.CardCode);
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
            //_logger.Debug(oPmxConnectionName);
            String connectionString = ConfigurationManager.ConnectionStrings[oPmxConnectionName].ConnectionString;
            //_logger.Debug(connectionString);
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
                //_logger.Debug("Produmex Delivery");
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String sqlToRun = "execute [dbo].[Infocus_EDI_PMX_DelPack] " + Edi940HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    //_logger.Debug("Running Produmex SQL: " + sqlToRun);
                    int iNextRow = 0;
                    string oLoopingStructure = "0002";

                    using (SqlCommand command = new SqlCommand(sqlToRun, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {

                                iNextRow = iNextRow + 1;
                                int NoFields = reader.FieldCount;
                                string oCurPallet = "";
                                string oCurSSCC = "";
                                Edi856PackRecord pack = new Edi856PackRecord();
                                Edi856PalletRecord pallet = new Edi856PalletRecord();
                                while (reader.Read())
                                {
                                    string oHasPallets = (String)reader["HasPallets"];
                                    string oOverboxing = (String)reader["Overboxing"];
                                    object[] recordData = new object[reader.FieldCount];
                                    reader.GetSqlValues(recordData);

                                    //_logger.Error("Processing  row " + iNextRow);
                                    if (iNextRow == 1)
                                    {
                                        if (oHasPallets.ToUpper() == "YES")
                                        {
                                            edi856HeaderRecord.Structure = "0055";
                                        }
                                        else if (oOverboxing.ToUpper() == "YES")
                                        {
                                            edi856HeaderRecord.Structure = "0001";
                                        }
                                        else
                                        {
                                            edi856HeaderRecord.Structure = "0002";
                                        }
                                        oLoopingStructure = edi856HeaderRecord.Structure;
                                    }
                                    iNextRow = iNextRow + 1;
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
                                    String oTrackNo = "";
                                    try
                                    {
                                        oTrackNo = (String)reader["PkgTrackNo"].ToString();
                                    }
                                    catch (Exception tn)
                                    {
                                        String oErr = tn.Message;
                                    }
                                    if (String.IsNullOrWhiteSpace(oTrackNo) || oTrackNo.Trim().Length == 0)
                                    {
                                        oTrackNo = delivery.TrackNo;
                                    }
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
                                                string oQty = reader["PltPkgs"].ToString();
                                                pallet.Quantity = Convert.ToDouble(oQty);
                                            }
                                            catch (Exception pq)
                                            {
                                                String oErr = pq.Message;
                                                _logger.Error("Error getting number of packages for pallet " + oMasterSSCC + " =>" + oErr);
                                                pallet.Quantity = Convert.ToDouble("1.0");
                                            }
                                            try
                                            {
                                                string oPltDUOM = (String)reader["PltLenUOM"];
                                                if (!String.IsNullOrWhiteSpace(oPltDUOM))
                                                {
                                                    string oLength = (String)reader["PltLen"].ToString();
                                                    pallet.Length = Convert.ToDouble(oLength);
                                                    pallet.DimUOM = (String)reader["PltLenUOM"];
                                                    string oWidth = (String)reader["PltWidth"].ToString();
                                                    pallet.Width = Convert.ToDouble(oWidth);
                                                    string oHeight = (String)reader["PltHgt"].ToString();
                                                    pallet.Height = Convert.ToDouble(oHeight);
                                                }
                                                string oWgtUOM = (String)reader["PltWgtUOM"];
                                                if (!String.IsNullOrWhiteSpace(oWgtUOM))
                                                {
                                                    pallet.WeightUOM = oWgtUOM;
                                                    string oWeight = (String)reader["PltWgt"].ToString();
                                                    pallet.Weight = Convert.ToDouble(oWeight);
                                                    pallet.WeightQualifier = "A3";
                                                    pallet.WeightUOM = (String)reader["PltWgtUOM"];
                                                }
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
                                    string oTreeType = "";
                                    try
                                    {
                                        oTreeType = (String)reader["TreeType"].ToString();
                                    }
                                    catch (Exception tt)
                                    {
                                        String oErr = tt.Message;
                                        _logger.Error("Error getting TreeType =>" + oErr);
                                    }

                                    if (oCurSSCC != oSSCC)
                                    {
                                        pack = new Edi856PackRecord();
                                        pallet.Pack.Add(pack);
                                        oCurSSCC = oSSCC; // 10-14-2021
                                        pack.PackNo = oSSCC;
                                        pack.TrackingNo = (String)reader["PkgTrackNo"];
                                        pack.TrackNoQualifier = "CP";

                                        if (oTreeType == "N" || oTreeType == "S")
                                        {

                                            string docLine = "";
                                            try
                                            {
                                                docLine = reader["DocLine"].ToString();
                                            }
                                            catch
                                            {
                                                docLine = "";
                                            }
                                            string oValue = "";

                                            try
                                            {
                                                string oPkgWgtUOM = reader["PkgWgtUOM"].ToString();
                                                if (!String.IsNullOrWhiteSpace(oPkgWgtUOM))
                                                {
                                                    oValue = reader["PkgWeight"].ToString();
                                                    // _logger.Error("Package Weight " + oValue);
                                                    pack.Weight = Convert.ToDouble(oValue);
                                                    pack.WeightUOM = oPkgWgtUOM;
                                                }
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting shipment weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Weight = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                oValue = reader["PkgQty"].ToString();
                                                double oNoPkgs = Convert.ToDouble(oValue);
                                                if (oNoPkgs < Convert.ToDouble("1.0"))
                                                {
                                                    oNoPkgs = Convert.ToDouble("1.0");
                                                }
                                                pack.Quantity = oNoPkgs;
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting number of cartons for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + wt.Message);
                                                pack.Quantity = Convert.ToDouble("1.0");
                                            }
                                            try
                                            {
                                                string oLenUOM = reader["PkgLenUOM"].ToString();
                                                if (!String.IsNullOrWhiteSpace(oLenUOM))
                                                {
                                                    pack.DimUOM = oLenUOM;
                                                }
                                            }
                                            catch (Exception pLenUom)
                                            {
                                                _logger.Error("Error getting number of length uom for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + pLenUom.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgLen"].ToString();
                                                // _logger.Error("Package Length " + oValue);
                                                pack.Length = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting package length for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Length = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oWidthUOM = reader["PkgWdtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oWidthUOM))
                                                    {
                                                        pack.DimUOM = oWidthUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception pwdt)
                                            {
                                                _logger.Error("Error getting package width UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pwdt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgWidth"].ToString();
                                                //_logger.Error("Package Width " + oValue);
                                                pack.Width = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception pLen)
                                            {
                                                _logger.Error("Error getting package width for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pLen.Message);
                                                pack.Width = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oHgtUOM = reader["PkgHgtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oHgtUOM))
                                                    {
                                                        pack.DimUOM = oHgtUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgHgt"].ToString();
                                                //_logger.Error("Package Height " + oValue);
                                                pack.Height = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                                pack.Height = Convert.ToDouble("0.00");
                                            }
                                            if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                            {
                                                pack.DimUOM = "IN";
                                            }
                                        }
                                        else // component
                                        {
                                            string docLine = "";
                                            try
                                            {
                                                docLine = reader["DocLine"].ToString();
                                            }
                                            catch
                                            {
                                                docLine = "";
                                            }
                                            string oValue = "";

                                            try
                                            {
                                                string oPkgWgtUOM = reader["PkgWgtUom"].ToString();

                                                if (!String.IsNullOrWhiteSpace(oPkgWgtUOM))
                                                {
                                                    oValue = reader["PkgWgt"].ToString();
                                                    //_logger.Error("Package Weight " + oValue);
                                                    pack.Weight = Convert.ToDouble(oValue);
                                                    pack.WeightUOM = oPkgWgtUOM;
                                                }
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting package weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Weight = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                string oLenUOM = reader["PkgLenUOM"].ToString();
                                                if (!String.IsNullOrWhiteSpace(oLenUOM))
                                                {
                                                    pack.DimUOM = oLenUOM;
                                                }
                                            }
                                            catch (Exception pLenUom)
                                            {
                                                _logger.Error("Error getting number of length uom for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + pLenUom.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgLen"].ToString();
                                                // _logger.Error("Package Length " + oValue);
                                                pack.Length = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error getting package length for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                                pack.Length = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oWidthUOM = reader["PkgWdtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oWidthUOM))
                                                    {
                                                        pack.DimUOM = oWidthUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception pwdt)
                                            {
                                                _logger.Error("Error getting package width UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pwdt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgWidth"].ToString();
                                                // _logger.Error("Package Width " + oValue);
                                                pack.Width = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception pLen)
                                            {
                                                _logger.Error("Error getting package width for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + pLen.Message);
                                                pack.Width = Convert.ToDouble("0.00");
                                            }
                                            try
                                            {
                                                if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                                {
                                                    string oHgtUOM = reader["PkgHgtUOM"].ToString();
                                                    if (!String.IsNullOrWhiteSpace(oHgtUOM))
                                                    {
                                                        pack.DimUOM = oHgtUOM;
                                                    }
                                                }
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height UOM for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                            }
                                            try
                                            {
                                                oValue = reader["PkgHgt"].ToString();
                                                //_logger.Error("Package Height " + oValue);
                                                pack.Height = Convert.ToDouble(oValue);
                                            }
                                            catch (Exception phgt)
                                            {
                                                _logger.Error("Error getting package height for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + phgt.Message);
                                                pack.Height = Convert.ToDouble("0.00");
                                            }
                                            if (String.IsNullOrWhiteSpace(pack.DimUOM))
                                            {
                                                pack.DimUOM = "IN";
                                            }
                                            try
                                            {
                                                oValue = reader["PackageCnt"].ToString();
                                                double oNoPkgs = Convert.ToDouble(oValue);
                                                if (oNoPkgs < Convert.ToDouble("1.0"))
                                                {
                                                    oNoPkgs = Convert.ToDouble("1.0");
                                                }
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
                                    }
                                    catch (Exception e)
                                    {
                                        items.LineNumber = 1;
                                    }

                                    items.VendorItemCode = (String)reader["ItemCode"];
                                    // 06-14-2024 begin
                                    if (BPList.Contains(oSBOCardCode))
                                    {
                                        items.VendorItemCode = items.VendorItemCode.ToUpper();
                                    }
                                    // 06-14-2024 end
                                    try
                                    {
                                        string oBuyerItemCode = (String)reader["SubCatNum"];
                                        if (!String.IsNullOrWhiteSpace(oBuyerItemCode))
                                        {
                                            items.BuyerItemCode = oBuyerItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                items.BuyerItemCode = items.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                        }
                                    }
                                    catch (Exception bitm)
                                    {
                                        String oErr = bitm.Message;
                                        _logger.Error("Error setting buyer item code =>" + oErr);
                                    }
                                    String DocLineNum = "";
                                    try
                                    {
                                        DocLineNum = (String)reader["DocLineNum"].ToString();
                                    }
                                    catch (Exception dl)
                                    {
                                        String oErr = dl.Message;
                                        _logger.Error("Error delivery line no for Delivery " + delivery.DocNum.ToString() + "=>" + oErr);
                                        DocLineNum = "";
                                    }
                                    if (oTreeType == "N" || oTreeType == "S")
                                    {
                                        try
                                        {
                                            items.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]);
                                        }
                                        catch (Exception qty1)
                                        {
                                            String oErr = qty1.Message;
                                            _logger.Error("Error setting qty ordered =>" + oErr);
                                        }
                                        try
                                        {
                                            double oItmQty = Convert.ToDouble(reader["Quantity"]);
                                            if (oItmQty < Convert.ToDouble("1.0"))
                                            {
                                                oItmQty = Convert.ToDouble("1.0");
                                            }
                                            items.Quantity = oItmQty;
                                        }
                                        catch (Exception qty2)
                                        {
                                            String oErr = qty2.Message;
                                            _logger.Error("Error setting qty shipped =>" + oErr);
                                        }
                                    }
                                    else
                                    {
                                        try
                                        {
                                            items.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]); // 10-20-2021                                           
                                        }
                                        catch (Exception qty1)
                                        {
                                            String oErr = qty1.Message;
                                            _logger.Error("Error setting qty ordered =>" + oErr);
                                        }
                                        try
                                        {
                                            double oItmQty = Convert.ToDouble(reader["Quantity"]);
                                            if (oItmQty < Convert.ToDouble("1.0"))
                                            {
                                                oItmQty = Convert.ToDouble("1.0");
                                            }
                                            items.Quantity = oItmQty;
                                        }
                                        catch (Exception qty2)
                                        {
                                            String oErr = qty2.Message;
                                            _logger.Error("Error setting qty shipped =>" + oErr);
                                        }
                                    }
                                    try
                                    {
                                        items.ShipmentCartons = Convert.ToDouble(reader["PkgQty"]);
                                        if (items.ShipmentCartons < Convert.ToDouble("1.0"))
                                        {
                                            items.ShipmentCartons = Convert.ToDouble("1.0");
                                        }
                                    }
                                    catch (Exception isc)
                                    {
                                        _logger.Error("Error getting item number of item packages" + isc.Message);
                                        items.ShipmentCartons = Convert.ToDouble("1.0");
                                    }
                                    try
                                    {
                                        items.ItemUOM = (String)reader["ItmUOM"];
                                    }
                                    catch (Exception iu)
                                    {
                                        _logger.Error("Error getting item UOM" + iu.Message);
                                        items.ItemUOM = "EA";
                                    }
                                    // remove quotes from item description
                                    string oItmDesc = (String)reader["Dscription"];
                                    oItmDesc = oItmDesc.Replace('"', ' ');
                                    items.ItemDescription = oItmDesc;
                                    if (String.IsNullOrWhiteSpace(oSSCC))
                                    {
                                        try
                                        {
                                            oSSCC = ((String)reader["SerialNo"]).Trim();
                                        }
                                        catch (Exception s2)
                                        {
                                            string oError = s2.Message;
                                            _logger.Error("Error getting SSCC for Delivery " + delivery.DocNum + " Ln# " + DocLineNum + " => " + oError);
                                            oSSCC = "";
                                        }
                                    }
                                    if (oIs3PL == "Y" || Edi940HeaderRecord.CardCode == "TeeZed")
                                    {
                                        oSSCC = ((String)reader["SerialNo"]).Trim();
                                        if (!String.IsNullOrWhiteSpace(oSSCC) && oSSCC.StartsWith("81088103"))
                                            try
                                            {
                                                oSSCC = oSSCC.Replace("81088103", "93127420");
                                            }
                                            catch (Exception sn)
                                            {
                                                _logger.Error("Error updating SSCC for Delivery# " + delivery.DocNum + " Ln# " + DocLineNum + "=> " + sn.Message);
                                            }
                                        items.SSCC = oSSCC.Trim().PadLeft(20, '0'); // 02-22-2022
                                    }
                                    else
                                    {
                                        items.SerialNumber = oSSCC.Trim().PadLeft(20, '0'); // 09-27-2021
                                    }
                                    if (items.SerialNumber != null && items.SerialNumber.Length > 0)
                                    {
                                        items.SerialNumber = items.SerialNumber.Trim().PadLeft(20, '0');
                                    }

                                    if (items.LineNumber == 0)
                                    {
                                        // _logger.Debug("856 Line # is zero for Delivery " + delivery.DocNum);
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

                                    var detail850 = (from v in Edi940HeaderRecord.Details
                                                     where v.VendorItemCode.Equals(items.VendorItemCode, StringComparison.InvariantCultureIgnoreCase)
                                                     && Edi940HeaderRecord.SalesOrderKey > 0
                                                     select v).FirstOrDefault();
                                    if (detail850 != null)
                                    {
                                        if (!String.IsNullOrWhiteSpace(detail850.BuyerItemCode))
                                        {
                                            items.BuyerItemCode = detail850.BuyerItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                items.BuyerItemCode = items.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
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
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    items.BuyerItemCode = items.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                        }
                                        if (!String.IsNullOrWhiteSpace(detail850.ItemDescription))
                                        {
                                            // remove quotes from item description
                                            //items.ItemDescription = detail850.ItemDescription; 
                                            oItmDesc = detail850.ItemDescription;
                                            oItmDesc = oItmDesc.Replace('"', ' ');
                                            items.ItemDescription = oItmDesc;
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
                                            // remove quotes from item description
                                            oItemDesc = oItemDesc.Replace('"', ' ');
                                            items.ItemDescription = oItemDesc;
                                        }
                                        if (items.LineNumber == 0)
                                        {
                                            items.LineNumber = detail850.LineNumber;
                                        }
                                        if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                        {
                                            items.VendorItemCode = detail850.VendorItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                items.VendorItemCode = items.VendorItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
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
                        edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                    }
                    else
                    {
                        edi856HeaderRecord.AsnShipDate = DateTime.Today;
                    }

                    edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                    if (Edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        edi856HeaderRecord.BillOfLading = edi856HeaderRecord.BillOfLading.Replace("-", "");
                    }
                    if (delivery.TrnspCode > 0)
                    {
                        String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                        using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                edi856HeaderRecord.TransportationMethod = (String)reader[0];
                            }
                        }
                    }

                    if (String.IsNullOrWhiteSpace(edi856HeaderRecord.TransportationMethod))
                    {
                        edi856HeaderRecord.TransportationMethod = "M";
                    }
                    if (Edi940HeaderRecord.CardCode.StartsWith("TSC"))
                    {
                        edi856HeaderRecord.TransportationMethod = Edi940HeaderRecord.TransportMethod;
                        if (edi856HeaderRecord.TransportationMethod == "M")
                        {
                            edi856HeaderRecord.TransportationMethod = "LT";
                        }
                        String sql = "select top 1 isnull(TrnspName, '') as TransportName from OSHP With(NOLOCK) " +
                                     "where TrnspCode = " + delivery.TrnspCode;
                        try
                        {
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    edi856HeaderRecord.TransportRouting = (String)reader[0];
                                }
                            }
                        }
                        catch (Exception tr)
                        {
                            _logger.Error("Error getting transport routing =>" + tr.Message);
                        }
                    }
                    sqlConnection.Close();
                    sqlConnection.Dispose(); // 01-27-2023
                }
            }
            else if (bProdumex == false)
            {
                _logger.Error("Not Produmex  -- not implemented for non-Produmex warehouse");
            }
        }

        public void OnPostProcess856Record(Delivery delivery, Edi940HeaderRecord edi940HeaderRecord, Edi856HeaderRecord edi856HeaderRecord, String oIs3PL, String[] pTrackNos)
        {
            //_logger.Debug("Starting OnPostProcess856Record");
            // 06-14-2024 begin
            string oSBOCardCode = delivery.CardCode;
            string[] values = new string[] { "LOWES", "HOMEDEPOT", "TSC", "WAYFAIR" };
            List<string> BPList = new List<string>(values);
            // 06-14-2024 end
            string oPmxConnectionName = getPMXConnectionName(edi940HeaderRecord.CardCode);
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
            // _logger.Error(oPmxConnectionName);
            String connectionString = ConfigurationManager.ConnectionStrings[oPmxConnectionName].ConnectionString;
            //_logger.Debug(connectionString);
            if (String.IsNullOrWhiteSpace(connectionString))
            {
                String msg = "Invalid ProdumexConnectionString found in Web.config";
                _logger.Error(msg);
                throw new WebApiException(msg);
            }
            bool bProdumex = true;
            setMissingPkgId(delivery, connectionString);
            bProdumex = CheckPMXWarehouse(delivery, connectionString);

            string oTrackingNo = delivery.U_Info_BOL;
            if (oIs3PL == "Y")
            {
                oTrackingNo = delivery.U_InfoW2Notes;
            }
            if (String.IsNullOrWhiteSpace(oTrackingNo) || oTrackingNo.Trim().Length == 0)
            {
                oTrackingNo = delivery.U_Info_BOL;
                if (String.IsNullOrWhiteSpace(oTrackingNo) || oTrackingNo.Trim().Length == 0)
                {
                    oTrackingNo = delivery.TrackNo;
                }
                if (edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                {
                    oTrackingNo = oTrackingNo.Replace("-", "");
                }
            }
            else
            {
                if ((!String.IsNullOrWhiteSpace(delivery.TrackNo)) && delivery.TrackNo.Trim().Length > delivery.U_Info_BOL.Trim().Length)
                {
                    oTrackingNo = delivery.TrackNo;
                    if (edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        oTrackingNo = oTrackingNo.Replace("-", "");
                    }
                }
            }
            oTrackingNo = oTrackingNo.Replace(", ", ",");
            oTrackingNo = oTrackingNo.Replace(" ,", ",");
            String[] oTrackNos = new String[1];
            if (!String.IsNullOrWhiteSpace(oTrackingNo))
            {
                oTrackNos = oTrackingNo.Split(',');
                if (oTrackNos.Length == 0)
                {
                    oTrackNos = new String[1];
                    oTrackNos[0] = oTrackingNo.Trim();
                }
            }
            int iNextTrack = 0;
            if (oIs3PL == "Y")
            {
                iNextTrack = 1;
            }

            if (bProdumex == true)
            {
                //_logger.Debug("Produmex Delivery");
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    String sqlToRun = "execute [dbo].[Infocus_EDI_PMX_Delivery] " + edi940HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    if (edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        sqlToRun = "execute  [dbo].[Infocus_EDI_PMX_DeliveryTot]  " + edi940HeaderRecord.HeaderId + ", " + delivery.DocEntry;
                    }
                    
                    //_logger.Debug("Running Produmex SQL: " + sqlToRun);
                    int iNextRow = 0;

                    using (SqlCommand command = new SqlCommand(sqlToRun, sqlConnection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                iNextRow = iNextRow + 1;
                                String result = reader.ToString();
                                int NoFields = reader.FieldCount;
                                while (reader.Read())
                                {
                                    //_logger.Debug("Processing  row " + iNextRow);
                                    string oTreeType = (String)reader["TreeType"];
                                    if (oTreeType == "N" || oTreeType == "S")
                                    {
                                        Edi856ItemDetailRecord detail = new Edi856ItemDetailRecord();
                                        edi856HeaderRecord.Details.Add(detail);
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
                                            //_logger.Debug("Package Weight " + oValue);
                                            detail.GrossPkgWeight = oValue;
                                        }
                                        catch (Exception wt)
                                        {
                                            _logger.Error("Error getting shipment weight for Delivery " + delivery.DocNum + " ln# " + docLine + " =>" + wt.Message);
                                            detail.GrossPkgWeight = "0.00";
                                        }
                                        try
                                        {
                                            oValue = reader["PackageCnt"].ToString();
                                            double oNoPkgs = Convert.ToDouble(oValue);
                                            detail.ShipmentCartons = oNoPkgs;
                                        }
                                        catch (Exception wt)
                                        {
                                            _logger.Error("Error getting number of cartons for Delivery " + delivery.DocNum + " ln# " + docLine + "  =>" + wt.Message);
                                            detail.ShipmentCartons = Convert.ToDouble("1.0");
                                        }
                                        if (detail.ShipmentCartons < Convert.ToDouble("1.0"))
                                        {
                                            detail.ShipmentCartons = Convert.ToDouble("1.0");
                                        }
                                        string oAltVendNo = "";
                                        try
                                        {
                                            oAltVendNo = (String)reader["VendorItem"];
                                        }
                                        catch
                                        {
                                            oAltVendNo = "";
                                        }
                                        if (String.IsNullOrWhiteSpace(oAltVendNo))
                                        {
                                            detail.VendorItemCode = (String)reader["ItemCode"];
                                        }
                                        else
                                        {
                                            detail.VendorItemCode = (String)oAltVendNo;
                                        }
                                        // 06-14-2024 begin
                                        if (BPList.Contains(oSBOCardCode))
                                        {
                                            detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                        }
                                        // 06-14-2024 end
                                        string oSSCC = null;
                                        try
                                        {
                                            string oSerNo = (String)reader["SerialNo"];
                                            string oSerialNo = oSerNo.ToString();
                                            oSSCC = oSerialNo.ToString();
                                        }
                                        catch (Exception cc)
                                        {
                                            string oError = cc.Message;
                                            _logger.Error("Error getting SSCC for Delivery " + delivery.DocNum + " Ln# " + docLine.ToString() + " => " + oError);
                                            oSSCC = "";
                                        }
                                        if (!String.IsNullOrWhiteSpace(oSSCC))
                                        {
                                            detail.SerialNumber = oSSCC;
                                            detail.SSCC = detail.SerialNumber;
                                        }
                                        if (edi940HeaderRecord.CardCode == "TeeZed" && !String.IsNullOrWhiteSpace(detail.SerialNumber) && detail.SerialNumber.StartsWith("81088103"))
                                        {
                                            try
                                            {
                                                detail.SerialNumber = detail.SerialNumber.Replace("81088103", "93127420");
                                                detail.SSCC = detail.SerialNumber;
                                            }
                                            catch (Exception sn)
                                            {
                                                _logger.Error("Error updating SSCC => " + sn.Message);
                                            }
                                        }
                                        if (oIs3PL == "Y")
                                        {
                                            detail.SerialNumber = "";
                                            detail.SSCC = oSSCC;
                                        }
                                        if (detail.SerialNumber != null && detail.SerialNumber.Length > 0)
                                        {
                                            detail.SerialNumber = detail.SerialNumber.Trim().PadLeft(20, '0');
                                            detail.SSCC = detail.SerialNumber;
                                        }
                                        try
                                        {
                                            detail.LineNumber = Convert.ToInt32(reader["U_InfoW2LnNo"]);
                                        }
                                        catch (Exception Lno)
                                        {
                                            string oErr = Lno.Message;
                                            _logger.Error("856 Line # error for Delivery # " + delivery.DocNum + " =>" + oErr);
                                        }
                                        if (detail.LineNumber == 0)
                                        {
                                            _logger.Error("856 Line # is zero for Delivery " + delivery.DocNum);
                                            detail.LineNumber = 1;
                                        }
                                        try
                                        {
                                            detail.Quantity = Convert.ToDouble(reader["Quantity"]);
                                        }
                                        catch
                                        {
                                            detail.Quantity = Convert.ToDouble("1.0");
                                        }
                                        try
                                        {
                                            detail.QtyOrdered = Convert.ToDouble(reader["OrderedQty"]);
                                        }
                                        catch
                                        {
                                            detail.QtyOrdered = detail.Quantity;
                                        }
                                        String oTrackNo = "";
                                        try
                                        {
                                            oTrackNo = reader["PkgTrackNo"].ToString();
                                        }
                                        catch (Exception err)
                                        {
                                            string oErr = err.Message;
                                            oTrackNo = "";
                                        }
                                        if (String.IsNullOrWhiteSpace(oTrackNo) || oTrackNo.Trim().Length == 0)
                                        {
                                            if (iNextTrack < oTrackNos.Length)
                                            {
                                                oTrackNo = oTrackNos[iNextTrack];
                                                iNextTrack = iNextTrack + 1;
                                            }
                                            else
                                            {
                                                oTrackNo = oTrackNos[0];
                                            }
                                        }
                                        detail.TrackingNumber = oTrackNo;

                                        try
                                        {
                                            detail.ItemUPC = reader["ItemUPC"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemUPC = "";
                                        }
                                        try
                                        {
                                            detail.Warehouse = reader["Warehouse"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.Warehouse = "";
                                        }
                                        try
                                        {
                                            detail.ItemStatus = reader["ItemStatus"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemStatus = "";
                                        }
                                        if (detail.ItemStatus == "IA" && delivery.CardCode == "HOMEDEPOT")
                                        {
                                            detail.ItemStatus = "AC";
                                        }
                                        else if (detail.ItemStatus == "IA" && delivery.CardCode == "LOWES")
                                        {
                                            detail.ItemStatus = "AR";
                                        }
                                        try
                                        {
                                            detail.ItemReason = reader["ItemReason"].ToString();
                                        }
                                        catch (Exception ec)
                                        {
                                            string oError = ec.Message;
                                            detail.ItemReason = "";
                                        }
                                        detail.LineItemStatus = detail.ItemStatus;
                                        detail.LineItemReason = detail.ItemReason;
                                        try
                                        {
                                            detail.LineNumber = Convert.ToInt32(reader["U_InfoW2LnNo"]);
                                        }
                                        catch (Exception Lno2)
                                        {
                                            string oErr = Lno2.Message;
                                            _logger.Error("856 Line # error for Delivery # " + delivery.DocNum + " =>" + oErr);
                                        }
                                        var detail850 = (from v in edi940HeaderRecord.Details
                                                         where v.VendorItemCode.Equals(detail.VendorItemCode, StringComparison.InvariantCultureIgnoreCase)
                                                         && edi940HeaderRecord.SalesOrderKey > 0
                                                         select v).FirstOrDefault();
                                        if (detail850 != null)
                                        {
                                            if (!String.IsNullOrWhiteSpace(detail850.BuyerItemCode))
                                            {
                                                detail.BuyerItemCode = detail850.BuyerItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
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
                                                    detail.BuyerItemCode = (String)reader["SubCatNum"];
                                                    // 06-14-2024 begin
                                                    if (BPList.Contains(oSBOCardCode))
                                                    {
                                                        detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                    }
                                                    // 06-14-2024 end
                                                }
                                            }
                                            if (!String.IsNullOrWhiteSpace(detail850.ItemDescription))
                                            {
                                                // remove quotes from item description
                                                string oItmDesc = detail850.ItemDescription;
                                                oItmDesc = oItmDesc.Replace('"', ' ');
                                                detail.ItemDescription = oItmDesc;
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
                                                // remove quotes from item description
                                                oItemDesc = oItemDesc.Replace('"', ' ');
                                                detail.ItemDescription = oItemDesc;
                                                detail.ItemDescription = oItemDesc;
                                            }
                                            if (detail.LineNumber == 0)
                                            {
                                                detail.LineNumber = detail850.LineNumber;
                                            }
                                            if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                            {
                                                detail.VendorItemCode = detail850.VendorItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            if (detail850.UnitPrice == null)
                                            {
                                                detail.UnitPrice = Convert.ToDecimal("0.00");
                                            }
                                            else if (detail.UnitPrice == Convert.ToDecimal("0.00"))
                                            {
                                                try
                                                {
                                                    detail.UnitPrice = Convert.ToDecimal(detail850.UnitPrice);
                                                }
                                                catch
                                                {

                                                }
                                            }
                                        }
                                        else
                                        {
                                            Object oBuyerItem = reader["SubCatNum"];
                                            try
                                            {
                                                detail.BuyerItemCode = Convert.ToString(oBuyerItem);
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                            catch (Exception b)
                                            {
                                                _logger.Debug("Invalid BuyerItemCode/SubCatNum: " + b.Message);
                                            }
                                            // remove quotes from item description
                                            string oItmDesc = (String)reader["Dscription"];
                                            oItmDesc = oItmDesc.Replace('"', ' ');
                                            detail.ItemDescription = oItmDesc;
                                            if (detail.BuyerItemCode == null || detail.BuyerItemCode.Trim().Length == 0)
                                            {
                                                detail.BuyerItemCode = detail.VendorItemCode;
                                                // 06-14-2024 begin
                                                if (BPList.Contains(oSBOCardCode))
                                                {
                                                    detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                                }
                                                // 06-14-2024 end
                                            }
                                        }
                                        if (detail.LineNumber == 0)
                                        {
                                            _logger.Debug("856 Line # is zero for Delivery " + delivery.DocNum);
                                            detail.LineNumber = 1;
                                        }
                                        detail.FreightClass = (String)reader["FreightClass"];
                                        detail.NMFC = (String)reader["NMFC"];
                                    }
                                }
                            }
                            else // 08-29-2017 begin
                            {
                                bProdumex = false;

                            }
                        }
                    }
                    if (!(delivery.DocDueDate == null))
                    {
                        edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                    }
                    else
                    {
                        edi856HeaderRecord.AsnShipDate = DateTime.Today;
                    }

                    edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                    if (edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        edi856HeaderRecord.BillOfLading = edi856HeaderRecord.BillOfLading.Replace("-", "");
                    }
                    if (delivery.TrnspCode > 0)
                    {
                        String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                        using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                edi856HeaderRecord.TransportationMethod = (String)reader[0];
                            }
                        }
                    }

                    if (String.IsNullOrWhiteSpace(edi856HeaderRecord.TransportationMethod))
                    {
                        edi856HeaderRecord.TransportationMethod = "M";
                    }
                    if (edi940HeaderRecord.CardCode.StartsWith("TSC"))
                    {
                        edi856HeaderRecord.TransportationMethod = edi940HeaderRecord.TransportMethod;
                        if (edi856HeaderRecord.TransportationMethod == "M")
                        {
                            edi856HeaderRecord.TransportationMethod = "LT";
                        }
                        String sql = "select top 1 isnull(TrnspName, '') as TransportName from OSHP With(NOLOCK) " +
                                     "where TrnspCode = " + delivery.TrnspCode;
                        try
                        {
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    edi856HeaderRecord.TransportRouting = (String)reader[0];
                                }
                            }
                        }
                        catch (Exception tr)
                        {
                            _logger.Error("Error getting transport routing =>" + tr.Message);
                        }
                    }
                    sqlConnection.Close();
                }
            }
            else if (bProdumex == false)
            {
                //_logger.Debug("Not Produmex");
                string oConnectionName = getConnectionName(edi940HeaderRecord.CardCode);
                if (oConnectionName == null || oConnectionName.Trim().Length == 0)
                {
                    oConnectionName = "WebApiDbContext";
                }
                connectionString = ConfigurationManager.ConnectionStrings[oConnectionName].ConnectionString;
                if (String.IsNullOrWhiteSpace(connectionString))
                {
                    String msg = "Invalid WebAPIConnectionString found in Web.config";
                    _logger.Error(msg);
                    throw new WebApiException(msg);
                }
                using (SqlConnection sqlConnection = new SqlConnection(connectionString))
                {
                    sqlConnection.Open();
                    try
                    {
                        String sql = "Update DLN1 set U_InfoW2MpId = coalesce((select U_InfoW2MpId from RDR1 where DocEntry = DLN1.BaseEntry and LineNum = DLN1.BaseLine),'') " +
                                     "where len(ltrim(rtrim(coalesce(U_InfoW2MpId,'')))) = 0 and DocEntry = " + delivery.DocEntry;
                        //_logger.Debug("Checking for missing sscc using SQL: " + sql);
                        try
                        {
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                        catch (Exception del2)
                        {
                            _logger.Error("Error updating delivery line package id numbers => " + del2.Message);
                        }
                    }
                    catch (Exception Mid)
                    {
                        _logger.Error("Error updating delivery line package id numbers => " + Mid.Message);
                    }
                    finally
                    {
                        sqlConnection.Close();

                    }

                    String sqlToRun = String.Format(Delivery940Query, delivery.DocEntry, edi940HeaderRecord.HeaderId);
                    //_logger.Debug("Running Delivery SQL: " + sqlToRun);
                    sqlConnection.Open();
                    using (SqlCommand command2 = new SqlCommand(sqlToRun, sqlConnection))
                    {

                        using (SqlDataReader reader2 = command2.ExecuteReader())
                        {
                            if (reader2.HasRows)
                            {
                                while (reader2.Read())
                                {
                                    Edi856ItemDetailRecord detail = new Edi856ItemDetailRecord();
                                    edi856HeaderRecord.Details.Add(detail);
                                    detail.VendorItemCode = (String)reader2["ItemCode"];
                                    // 06-14-2024 begin
                                    if (BPList.Contains(oSBOCardCode))
                                    {
                                        detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                    }
                                    // 06-14-2024 end
                                    if (oIs3PL == "Y")
                                    {
                                        if (edi940HeaderRecord.CardCode == "TeeZed" && !String.IsNullOrWhiteSpace(detail.SSCC) && detail.SSCC.StartsWith("81088103"))
                                        {
                                            try
                                            {
                                                detail.SSCC = detail.SSCC.Replace("81088103", "93127420");
                                            }
                                            catch (Exception sn)
                                            {
                                                _logger.Error("Error updating SSCC => " + sn.Message);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (edi940HeaderRecord.CardCode == "TeeZed" && !String.IsNullOrWhiteSpace(detail.SerialNumber) && detail.SerialNumber.StartsWith("81088103"))
                                        {
                                            try
                                            {
                                                detail.SerialNumber = detail.SerialNumber.Replace("81088103", "93127420");
                                            }
                                            catch (Exception sn)
                                            {
                                                _logger.Error("Error updating SSCC => " + sn.Message);
                                            }
                                        }
                                    }
                                    if (detail.SerialNumber != null && oIs3PL == "N")
                                    {
                                        detail.SerialNumber = detail.SerialNumber.Trim().PadLeft(20, '0');
                                        detail.SSCC = detail.SerialNumber; // 04-01-2022
                                    }
                                    else
                                    {
                                        if (oIs3PL == "Y" || edi940HeaderRecord.CardCode == "TeeZed")
                                        {
                                            String oSSCC = "";
                                            try
                                            {
                                                oSSCC = ((String)reader2["SSCC"]).Trim();
                                                // 12-15-2023 begin
                                                if (oSSCC.Contains('|'))
                                                {
                                                    string [] oSSCCNos = oSSCC.Split('|');
                                                    if (oSSCCNos.Length > 0)
                                                    {
                                                        oSSCC = oSSCCNos[0];
                                                    }
                                                }
                                                // 12-15-2023 end
                                                if (!String.IsNullOrWhiteSpace(oSSCC) && oSSCC.StartsWith("81088103"))
                                                    try
                                                    {
                                                        oSSCC = oSSCC.Replace("81088103", "93127420");
                                                    }
                                                    catch (Exception sn)
                                                    {
                                                        _logger.Error("Error updating SSCC => " + sn.Message);
                                                    }
                                                detail.SSCC = oSSCC.Trim().PadLeft(20, '0'); // 02-22-2022
                                            }
                                            catch (Exception cc)
                                            {
                                                string oError = cc.Message;
                                                _logger.Error("Error getting SSCC for Delivery " + delivery.DocNum + " TeeZed (line 1055) => " + oError);
                                                oSSCC = "";
                                            }
                                        }
                                        else
                                        {
                                            detail.SerialNumber = ((String)reader2["SSCC"]).Trim().PadLeft(20, '0');
                                            detail.SSCC = detail.SerialNumber; // 04-01-2022
                                        }
                                    }
                                    detail.Quantity = Convert.ToDouble(reader2["Quantity"]);
                                    try
                                    {
                                        detail.QtyOrdered = Convert.ToDouble(reader2["OrderedQty"]);
                                    }
                                    catch
                                    {
                                    }
                                    var detail850 = (from v in edi940HeaderRecord.Details
                                                     where v.VendorItemCode.Equals(detail.VendorItemCode, StringComparison.InvariantCultureIgnoreCase)
                                                     && edi940HeaderRecord.SalesOrderKey > 0
                                                     select v).FirstOrDefault();

                                    //***** need ToString get vendor/buyer item without prefix
                                    if (detail850 != null)
                                    {
                                        detail.BuyerItemCode = detail850.BuyerItemCode;
                                        // 06-14-2024 begin
                                        if (BPList.Contains(oSBOCardCode))
                                        {
                                            detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                        }
                                        // 06-14-2024 end
                                        detail.ItemUOM = detail850.UnitOfMeasure; // 08-17-2023
                                        // remove quotes from item description
                                        string oItmDesc = detail850.ItemDescription;
                                        oItmDesc = oItmDesc.Replace('"', ' ');
                                        detail.ItemDescription = oItmDesc;
                                        if (!String.IsNullOrWhiteSpace(detail850.VendorItemCode))
                                        {
                                            detail.VendorItemCode = detail850.VendorItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                detail.VendorItemCode = detail.VendorItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                        }
                                        if (detail.LineNumber == 0)
                                        {
                                            detail.LineNumber = detail850.LineNumber;
                                        }
                                        if (detail850.GrossPkgWeight == null)
                                        {
                                            detail.GrossPkgWeight = ("0.00");
                                        }
                                        else
                                        {
                                            try
                                            {
                                                string oPkgWeight = detail850.GrossPkgWeight.ToString();
                                                double oGrsPkgWgt = Convert.ToDouble(oPkgWeight);
                                                detail.GrossPkgWeight = (detail850.GrossPkgWeight).ToString();
                                            }
                                            catch (Exception wt)
                                            {
                                                _logger.Error("Error converting gross pkg wt => " + wt.Message);
                                                detail.GrossPkgWeight = ("0.00");
                                            }
                                        }
                                        if (detail850.UnitPrice == null)
                                        {
                                            detail.UnitPrice = Convert.ToDecimal("0.00");
                                        }
                                        else if (detail.UnitPrice == Convert.ToDecimal("0.00"))
                                        {
                                            try
                                            {
                                                detail.UnitPrice = Convert.ToDecimal(detail850.UnitPrice);
                                            }
                                            catch
                                            {

                                            }
                                        }
                                    }
                                    else
                                    {
                                        // 08-16-2023 begin
                                        if (oIs3PL == "Y")
                                        {
                                            detail.BuyerItemCode = detail850.BuyerItemCode;
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                        }
                                        else
                                        {
                                            String oSubCatNum = "";
                                            try
                                            {
                                                oSubCatNum = reader2["SubCatNum"].ToString();
                                            }
                                            catch
                                            {
                                                oSubCatNum = "";
                                            }
                                            /*detail.BuyerItemCode = (String)reader2["SubCatNum"];
                                            if (detail.BuyerItemCode == null || detail.BuyerItemCode.Trim().Length == 0)
                                            {
                                                detail.BuyerItemCode = detail.VendorItemCode;
                                            }*/
                                            if (String.IsNullOrWhiteSpace(oSubCatNum) || oSubCatNum.Trim().Length == 0)
                                            {
                                                detail.BuyerItemCode = detail.VendorItemCode;
                                            }
                                            else
                                            {
                                                detail.BuyerItemCode = oSubCatNum;
                                            }
                                            // 06-14-2024 begin
                                            if (BPList.Contains(oSBOCardCode))
                                            {
                                                detail.BuyerItemCode = detail.BuyerItemCode.ToUpper();
                                            }
                                            // 06-14-2024 end
                                        }
                                        // 08-16-2023 end
                                        detail.ItemDescription = (String)reader2["Dscription"];
                                        // remove quotes from item description
                                        string oItmDesc = (String)reader2["Dscription"];
                                        oItmDesc = oItmDesc.Replace('"', ' ');
                                        detail.ItemDescription = oItmDesc;
                                    }
                                    String oTrackNo = "";
                                    try
                                    {
                                        oTrackNo = reader2["PkgTrackNo"].ToString();
                                    }
                                    catch (Exception err)
                                    {
                                        string oErr = err.Message;
                                        oTrackNo = "";
                                    }
                                    if (String.IsNullOrWhiteSpace(oTrackNo) || oTrackNo.Trim().Length == 0)
                                    {
                                        if (iNextTrack < oTrackNos.Length)
                                        {
                                            oTrackNo = oTrackNos[iNextTrack];
                                            iNextTrack = iNextTrack + 1;
                                        }
                                        else
                                        {
                                            oTrackNo = oTrackNos[0];
                                        }
                                    }
                                    detail.TrackingNumber = oTrackNo;

                                    try
                                    {
                                        detail.Warehouse = reader2["WhsCode"].ToString();
                                    }
                                    catch (Exception ec)
                                    {
                                        string oError = ec.Message;
                                        detail.Warehouse = "";
                                    }
                                    try
                                    {
                                        detail.ItemStatus = reader2["ItemStatus"].ToString();
                                    }
                                    catch (Exception ec)
                                    {
                                        string oError = ec.Message;
                                        detail.ItemStatus = "";
                                    }
                                    if (detail.ItemStatus == "IA" && delivery.CardCode == "HOMEDEPOT")
                                    {
                                        detail.ItemStatus = "AC";
                                    }
                                    else if (detail.ItemStatus == "IA" && delivery.CardCode == "LOWES")
                                    {
                                        detail.ItemStatus = "AR";
                                    }
                                    try
                                    {
                                        detail.ItemReason = reader2["ItemReason"].ToString();
                                    }
                                    catch (Exception ec)
                                    {
                                        string oError = ec.Message;
                                        detail.ItemReason = "";
                                    }
                                    detail.LineItemStatus = detail.ItemStatus;
                                    detail.LineItemReason = detail.ItemReason;
                                    detail.FreightClass = (String)reader2["FreightClass"];
                                    detail.NMFC = (String)reader2["NMFC"];
                                   
                                    // ** Temp comments
                                    /*
                                    _logger.Debug("Delivery #" + delivery.DocNum + " Line# " + detail.LineNumber + " Item#" + detail.VendorItemCode +
                                        " Qty: " + detail.Quantity +
                                        " Ordered: " + detail.QtyOrdered);
                                    */
                                }
                            }
                        }
                    }
                    if (!(delivery.DocDueDate == null))
                    {
                        edi856HeaderRecord.AsnShipDate = delivery.DocDueDate;
                    }
                    else
                    {
                        edi856HeaderRecord.AsnShipDate = DateTime.Today;
                    }
                    edi856HeaderRecord.BillOfLading = delivery.U_Info_BOL;
                    if (edi940HeaderRecord.CardCode.StartsWith("HDCL"))
                    {
                        edi856HeaderRecord.BillOfLading = edi856HeaderRecord.BillOfLading.Replace("-", "");
                    }
                    if (delivery.TrnspCode > 0)
                    {
                        String sql = String.Format(ShippingMethodQuery, delivery.TrnspCode);
                        using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                        {
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                reader.Read();
                                edi856HeaderRecord.TransportationMethod = (String)reader[0];
                            }
                        }
                    }

                    if (String.IsNullOrWhiteSpace(edi856HeaderRecord.TransportationMethod))
                    {
                        edi856HeaderRecord.TransportationMethod = "M";
                    }
                    if (edi940HeaderRecord.CardCode.StartsWith("TSC"))
                    {
                        edi856HeaderRecord.TransportationMethod = edi940HeaderRecord.TransportMethod;
                        if (edi856HeaderRecord.TransportationMethod == "M")
                        {
                            edi856HeaderRecord.TransportationMethod = "LT";
                        }
                        String sql = "select top 1 isnull(TrnspName, '') as TransportName from OSHP With(NOLOCK) " +
                                     "where TrnspCode = " + delivery.TrnspCode;
                        try
                        {
                            using (SqlCommand command = new SqlCommand(sql, sqlConnection))
                            {
                                using (SqlDataReader reader = command.ExecuteReader())
                                {
                                    reader.Read();
                                    edi856HeaderRecord.TransportRouting = (String)reader[0];
                                }
                            }
                        }
                        catch (Exception tr)
                        {
                            _logger.Error("Error getting transport routing =>" + tr.Message);
                        }
                    }
                    sqlConnection.Close();
                }
            }
        }
        // 08-16-2023 end
    }
}
