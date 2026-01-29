using Infocus.Common;
using Infocus.Framework.SapBone;
using Infocus.Framework.SapBone.Forms;
using log4net;
using SAPbouiCOM;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Linq;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Infocus.Edi.Bone
{
    class EdiAddon : BoneAddOn
    {
        private static ILog _logger = LogManager.GetLogger(typeof(EdiAddon));
        public static DateTime startDate;
        public static DateTime endDate;

        public EdiAddon()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            Initialize();
            _logger.Debug("Infocus Edi Successfully Connected");
            BoneConnectionContext.Application.StatusBar.SetText("Infocus EDI Add-on Successfully Installed",
            BoMessageTime.bmt_Short, BoStatusBarMessageType.smt_Success);
        }
        public void Initialize()
        {
            RemoveMenus();
            CreateMenus();
            try
            {
                AppSettings.Load();
            }
            catch (InfocusException)
            {
                EdiGlobalEventsHandler.ShowSettingsForm();
            }
        }
        public override void OnShutDown()
        {
            RemoveMenus();
            // 04-03-2019 begin
            _logger.Debug("Infocus Edi Sutting Down");
            BoneConnectionContext.Company.Disconnect();
            System.Windows.Forms.Application.ExitThread();
            System.Windows.Forms.Application.Exit();
            // 04-03-2019 end

        }

        public override void OnCompanyChanged()
        {
            RemoveMenus();
            Initialize();
        }
        public override void OnLanguageChanged(SAPbouiCOM.BoLanguages language)
        {
            // ADD YOUR LANGUAGE CHANGE CODE HERE	...
        }
        public override void OnStatusBarErrorMessage(string txt)
        {
            // ADD YOUR CODE HERE	...
        }

        public override void OnStatusBarSuccessMessage(string txt)
        {
            // ADD YOUR CODE HERE	...
        }

        public override void OnStatusBarWarningMessage(string txt)
        {
            // ADD YOUR CODE HERE	...
        }

        public override void OnStatusBarNoTypedMessage(string txt)
        {
            // ADD YOUR CODE HERE	...
        }

        public override bool OnBeforeProgressBarCreated()
        {
            // ADD YOUR CODE HERE	...
            return true;
        }

        public override bool OnAfterProgressBarCreated()
        {
            // ADD YOUR CODE HERE	...
            return true;
        }

        public override bool OnBeforeProgressBarStopped(bool success)
        {
            // ADD YOUR CODE HERE	...
            return true;
        }

        public override bool OnAfterProgressBarStopped(bool success)
        {
            // ADD YOUR CODE HERE	...
            return true;
        }

        public override bool OnProgressBarReleased()
        {
            // ADD YOUR CODE HERE	...
            return true;
        }

        public static void Main()
        {
            int retCode = 0;
            String connStr;

            // CHANGE ADDON IDENTIFIER BEFORE RELEASING TO CUSTOMER (Solution Identifier)
             if ((Environment.GetCommandLineArgs().Length == 1))
             {
                connStr = BoneConnectionContext.ConnectionString;
             }
             else
             {
            connStr = Environment.GetCommandLineArgs().GetValue(1).ToString();
            }
            try
            {
                // INIT CONNECTIONS
                retCode = BoneConnectionContext.Initialize(connStr, String.Empty, BoneConnectionType.SSO);
                // CONNECTION FAILED
                if ((retCode != 0))
                {
                    System.Windows.Forms.MessageBox.Show("ERROR - Connection failed: " + BoneConnectionContext.Company.GetLastErrorDescription());
                    return;
                }

                EdiAddonDb addOnDb = new EdiAddonDb();
                addOnDb.Add(BoneConnectionContext.Company);

                // CREATE ADD-ON
                new EdiAddon();
                BoneConnectionContext.Application.StatusBar.SetSystemMessage("Successfully connected to EDI Add-on by Infocus Technologies"
                    , BoMessageTime.bmt_Short
                    , BoStatusBarMessageType.smt_Success);
                System.Windows.Forms.Application.Run();
            }
            catch (Exception ex)
            {
                _logger.Fatal(ex);
                System.Windows.Forms.MessageBox.Show("ERROR - Connection failed: " + ex.Message);
            }
        }
        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            String msg = String.Empty;
            Exception exception = e.ExceptionObject as Exception;
            _logger.Error(exception);
            try
            {
                ExceptionForm.ShowModal(exception);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                while (exception != null)
                {
                    msg += exception.Message;
                    if (exception.InnerException != null)
                    {
                        msg += Environment.NewLine;
                    }
                    exception = exception.InnerException;
                }
                BoneConnectionContext.Application.MessageBox(
                     msg + Environment.NewLine + Environment.NewLine + ((Exception)e.ExceptionObject).StackTrace,
                    1,
                    "Cancel",
                    "",
                    ""
                    );
            }
            // 04-03-2019 begin
            finally
            {
                BoneConnectionContext.Company.Disconnect();
                System.Windows.Forms.Application.ExitThread();
                System.Windows.Forms.Application.Exit();
            }
            // 04-03-2019 end
        }
        //private static void CreateMenus()
        //{
        //    RemoveMenus();
        //    String xml = Properties.Resources.AddMenus;
        //    try
        //    {
        //        BoneConnectionContext.Application.LoadBatchActions(ref xml);
        //    }
        //    catch(Exception ex)
        //    {
        //        _logger.Error("Error creating menus: " + ex.Message, ex);
        //        throw ex;
        //    }

        //}
        private static void CreateMenus()
        {
            MenuCreationParams creationParams = (MenuCreationParams)BoneConnectionContext.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
            SAPbouiCOM.MenuItem mi = BoneConnectionContext.Application.Menus.Item("43520"); // Modules Menu
            String path = Environment.CurrentDirectory + "/";
            creationParams.Type = BoMenuType.mt_POPUP;
            creationParams.UniqueID = SAPUIConstants.MENU_EDI;
            creationParams.String = "Infocus EDI";
            creationParams.Enabled = true;
            creationParams.Image = path + "Images\\swirl-16x16.bmp";
            creationParams.Position = 15;
            Menus menus = mi.SubMenus;
            try
            {
                menus.AddEx(creationParams);
                mi = BoneConnectionContext.Application.Menus.Item(SAPUIConstants.MENU_EDI);
                menus = mi.SubMenus;

                //Create a sub menu
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = SAPUIConstants.MENU_EDI_PROCESS_850;
                creationParams.String = "Process Incoming 850 Records (Sales Orders)";
                menus.AddEx(creationParams);
                // 08-201-2019 begin
                creationParams.UniqueID = SAPUIConstants.MENU_EDI_REJECT_850;
                creationParams.String = "Reject Unprocessed 850s";
                menus.AddEx(creationParams);
                // 08-20-2019 end
                // 04-26-2019 begin
                //  creationParams.UniqueID = SAPUIConstants.MENU_EDI_PROCESS_820;
                //  creationParams.String = "Process Incoming 820 Records (Payment)";
                //  menus.AddEx(creationParams);
                // 08-07-2023 begin
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = SAPUIConstants.MENU_EDI_PROCESS_940;
                creationParams.String = "Process Incoming 940 Records (Whs Shipping Orders)";
                menus.AddEx(creationParams);
                // 08-07-2023 end
                creationParams.UniqueID = SAPUIConstants.MENU_EDI_PROCESS_180;
                creationParams.String = "Process Incoming 180 Records (A/R Returns)";
                menus.AddEx(creationParams);
                // 04-26-2019 end
                // 02-26-2019 begin
                creationParams.UniqueID = SAPUIConstants.MENU_EDI_RESEND_856;
                creationParams.String = "Select 856 (Delivery) to be Resent";
                menus.AddEx(creationParams);
                creationParams.UniqueID = SAPUIConstants.MENU_EDI_RESEND_810;
                creationParams.String = "Select 810 (A/R Invoice) to be Resent";
                menus.AddEx(creationParams);
                // 02-26-2019 end
            }
            catch (Exception)
            {
            }
            System.Runtime.InteropServices.Marshal.ReleaseComObject(creationParams);

            //Create the Settings Menu Item
            creationParams = (MenuCreationParams)BoneConnectionContext.Application.CreateObject(BoCreatableObjectType.cot_MenuCreationParams);
            mi = BoneConnectionContext.Application.Menus.Item("43525"); // Administration->Setup
            creationParams.Type = BoMenuType.mt_POPUP;
            creationParams.UniqueID = SAPUIConstants.MENU_SETUP;
            creationParams.String = "Infocus EDI";
            creationParams.Enabled = true;
            creationParams.Position = 98;
            menus = mi.SubMenus;
            try
            {
                menus.AddEx(creationParams);
                mi = BoneConnectionContext.Application.Menus.Item(SAPUIConstants.MENU_SETUP);
                menus = mi.SubMenus;

                //Create a sub menu
                creationParams.Type = BoMenuType.mt_STRING;
                creationParams.UniqueID = SAPUIConstants.MENU_ITEM_SETUP;
                creationParams.String = "Settings";
                menus.AddEx(creationParams);
                // 07-14-2021 begin
                creationParams.UniqueID = SAPUIConstants.MENU_850_IMPORT_STATUS;
                creationParams.String = "850 Import Status";
                menus.AddEx(creationParams);
                // 07-14-2021 end
            }
            catch (Exception)
            {
            }

            System.Runtime.InteropServices.Marshal.ReleaseComObject(creationParams);
        }
        //private static void RemoveMenus()
        //{
        //    String xml = Properties.Resources.RemoveMenus;
        //    try
        //    {
        //        BoneConnectionContext.Application.LoadBatchActions(ref xml);
        //    }
        //    catch(Exception ex)
        //    {
        //        _logger.Warn("Error removing menus: " + ex.Message, ex);
        //    }
        //}
        private static void RemoveMenus()
        {
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_PROCESS_850);
            }
            catch (Exception)
            {
            }
            // 08-07-2023 begin
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_PROCESS_940);
            }
            catch (Exception)
            {
            }
            // 08-07-2023 end
            // 02-26-2019 begin
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_RESEND_856);
            }
            catch (Exception)
            {
            }
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_RESEND_810);
            }
            catch (Exception)
            {
            }
            // 02-26-2019 end
            // 08-20-2019 begin
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_REJECT_850);
            }
            catch (Exception)
            {
            }
            // 08-20-2019 end
            // 04-26-2019 begin
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_PROCESS_180);
            }
            catch (Exception)
            {

            }
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI_PROCESS_820);
            }
            catch (Exception)
            {

            }
            // 04-26-2019 end
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_EDI);
            }
            catch (Exception)
            {
            }
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_ITEM_SETUP);
            }
            catch (Exception)
            {
            }
            try
            {
                BoneConnectionContext.Application.Menus.RemoveEx(SAPUIConstants.MENU_SETUP);
            }
            catch (Exception)
            {
            }

        }

        // 08-20-2019 begin
        public static SAPbouiCOM.Matrix LoadOpen850s(Form pForm, SAPbouiCOM.Matrix pMatrix)
        {

            pMatrix.Clear();
            pMatrix.Columns.Item("V_0").DataBind.UnBind();
            pMatrix.Columns.Item("V_1").DataBind.UnBind();
            pMatrix.Columns.Item("V_2").DataBind.UnBind();
            pMatrix.Columns.Item("V_3").DataBind.UnBind();
            DataTable oDataTable = pForm.DataSources.DataTables.Item("Open850s");
            oDataTable.Clear();
            string oSQL = "select HeaderId, PurchaseOrderReference, SBOCardCode, ErrorMessage from InfocusEdi850HeaderRecord With(NOLOCK) where Processed=0 " +
                          "order by HeaderId desc ";

            oDataTable.ExecuteQuery(oSQL);
            pMatrix.FlushToDataSource();
            pMatrix.Columns.Item("V_0").DataBind.Bind("Open850s", "HeaderId");
            pMatrix.Columns.Item("V_1").DataBind.Bind("Open850s", "PurchaseOrderReference");
            pMatrix.Columns.Item("V_2").DataBind.Bind("Open850s", "SBOCardCode");
            pMatrix.Columns.Item("V_3").DataBind.Bind("Open850s", "ErrorMessage");
            pMatrix.LoadFromDataSource();
            int oRows = pMatrix.RowCount;
            return pMatrix;
        }
        // 08-20-2019 end
        public static SAPbouiCOM.Matrix LoadSent856s(Form pForm, SAPbouiCOM.Matrix pMatrix)
        {

            pMatrix.Clear();
            pMatrix.Columns.Item("V_0").DataBind.UnBind();
            pMatrix.Columns.Item("V_1").DataBind.UnBind();
            pMatrix.Columns.Item("V_2").DataBind.UnBind();
            DataTable oDataTable = pForm.DataSources.DataTables.Item("Sent856s");
            oDataTable.Clear();
            string oSQL = "select DocNum, NumAtCard, CardCode from [Infocus_EDI_Resendable_856] With(NOLOCK) " +
                          "order by DocNum desc ";

            oDataTable.ExecuteQuery(oSQL);
            pMatrix.FlushToDataSource();
            pMatrix.Columns.Item("V_0").DataBind.Bind("Sent856s", "DocNum");
            pMatrix.Columns.Item("V_1").DataBind.Bind("Sent856s", "NumAtCard");
            pMatrix.Columns.Item("V_2").DataBind.Bind("Sent856s", "CardCode");
            pMatrix.LoadFromDataSource();
            int oRows = pMatrix.RowCount;
            return pMatrix;
        }

        public static SAPbouiCOM.Matrix LoadSent810s(Form pForm, SAPbouiCOM.Matrix pMatrix)
        {

            pMatrix.Clear();
            pMatrix.Columns.Item("V_0").DataBind.UnBind();
            pMatrix.Columns.Item("V_1").DataBind.UnBind();
            pMatrix.Columns.Item("V_2").DataBind.UnBind();
            DataTable oDataTable = pForm.DataSources.DataTables.Item("Sent810s");
            oDataTable.Clear();
            string oSQL = "select DocNum, NumAtCard, CardCode from [Infocus_EDI_Resendable_810] With(NOLOCK) " +
                          "order by DocNum desc ";

            oDataTable.ExecuteQuery(oSQL);
            pMatrix.FlushToDataSource();
            pMatrix.Columns.Item("V_0").DataBind.Bind("Sent810s", "DocNum");
            pMatrix.Columns.Item("V_1").DataBind.Bind("Sent810s", "NumAtCard");
            pMatrix.Columns.Item("V_2").DataBind.Bind("Sent810s", "CardCode");
            pMatrix.LoadFromDataSource();
            int oRows = pMatrix.RowCount;
            return pMatrix;
        }

        // 08-20-2019 begin
        public static SAPbouiCOM.Matrix LoadSent810s(Form pForm, SAPbouiCOM.Matrix pMatrix, string startDate, string endDate, string cardCode)
        {

            pMatrix.Clear();
            pMatrix.Columns.Item("V_0").DataBind.UnBind();
            pMatrix.Columns.Item("V_1").DataBind.UnBind();
            pMatrix.Columns.Item("V_2").DataBind.UnBind();
            DataTable oDataTable = pForm.DataSources.DataTables.Item("Sent810s");
            oDataTable.Clear();
            string oSQL = "select t0.DocNum, t0.NumAtCard, t0.CardCode from [Infocus_EDI_Resendable_810] t0 With(NOLOCK) " +
                          " join OINV t1 With(NOLOCK) on t0.DocNum = t1.DocNum where t1.DocDate >= '" + startDate.Trim() +
                          "' and t1.DocDate <= '" + endDate.Trim() + "'";
            if (!String.IsNullOrWhiteSpace(cardCode))
            {
                oSQL = oSQL + " and t0.CardCode = '" + cardCode.Trim() + "' order by DocNum desc";
            }
            else
            {
                oSQL = oSQL + " order by DocNum desc ";
            }

            oDataTable.ExecuteQuery(oSQL);
            pMatrix.FlushToDataSource();
            pMatrix.Columns.Item("V_0").DataBind.Bind("Sent810s", "DocNum");
            pMatrix.Columns.Item("V_1").DataBind.Bind("Sent810s", "NumAtCard");
            pMatrix.Columns.Item("V_2").DataBind.Bind("Sent810s", "CardCode");
            pMatrix.LoadFromDataSource();
            int oRows = pMatrix.RowCount;
            return pMatrix;
        }
        // 08-20-2019 end

    }
}