//****************************************************************************
//
//  File:      B1AddOn.cs
//
//  Copyright (c) SAP 
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
//****************************************************************************
using System;
using System.IO;
using System.Xml;
using SAPbouiCOM;
using Infocus.Framework.SapBone.Forms;

namespace Infocus.Framework.SapBone
{

    public abstract class BoneAddOn
    {
        private void ApplHandler(BoAppEventTypes EventType)
        {
            try
            {
                switch(EventType)
                {
                    case BoAppEventTypes.aet_CompanyChanged:
                        OnCompanyChanged();
                        System.Environment.Exit(0);
                        return;

                    case BoAppEventTypes.aet_LanguageChanged:
                        OnLanguageChanged(BoneConnectionContext.Application.Language);
                        return;

                    case BoAppEventTypes.aet_ServerTerminition:
                    case BoAppEventTypes.aet_ShutDown:
                        OnShutDown();
                        System.Environment.Exit(0);
                        return;
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, String.Format("EXCEPTION: ApplHandler raised\n{0}", e.InnerException.Message));
            }
        }

        public abstract void OnShutDown();
        
        public abstract void OnCompanyChanged();
        
        public abstract void OnLanguageChanged(BoLanguages language);

        private void StatusBarHandler(String text, BoStatusBarMessageType messageType)
        {
            try
            {
                switch(messageType)
                {
                    case BoStatusBarMessageType.smt_Error:
                        OnStatusBarErrorMessage(text);
                        return;
                    case BoStatusBarMessageType.smt_Success:
                        OnStatusBarSuccessMessage(text);
                        return;
                    case BoStatusBarMessageType.smt_Warning:
                        OnStatusBarWarningMessage(text);
                        return;
#if V2005 || V2005_SP01 || V2007 || V2007W1
                case BoStatusBarMessageType.smt_None:
                    OnStatusBarNoTypedMessage(text);
                    return;
#endif
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, String.Format("EXCEPTION: StatusBarHandler raised\n{0}", e.InnerException.Message));
            }
        }

        public abstract void OnStatusBarErrorMessage(String text);

        public abstract void OnStatusBarSuccessMessage(String text);

        public abstract void OnStatusBarWarningMessage(String text);

#if V2005 || V2005_SP01 || V2007 || V2007W1
    /// <summary>
    /// Abstract method.
    /// <para> The AddOnWizard will automatically override this method in your main AddOn class.</para>
    /// <para> Put in your code all you need to manage the StatusBar events of type None</para>
    /// </summary>
    /// <param name="text">Text that will be shown in the StatusBar</param>
    public abstract void OnStatusBarNoTypedMessage(String text);
#endif

        private void ProgressBarHandler(ref ProgressBarEvent pVal, out bool bubbleEvent)
        {

            bubbleEvent = true;
            try
            {
                switch(pVal.EventType)
                {
                    case BoProgressBarEventTypes.pbet_ProgressBarCreated:
                        if(pVal.BeforeAction && OnBeforeProgressBarCreated() == false ||
                          !pVal.BeforeAction && OnAfterProgressBarCreated() == false)
                            bubbleEvent = false;
                        return;

                    case BoProgressBarEventTypes.pbet_ProgressBarStopped:
                        if((pVal.BeforeAction && OnBeforeProgressBarStopped(pVal.ActionSuccess) == false) ||
                          (!pVal.BeforeAction && OnAfterProgressBarStopped(pVal.ActionSuccess) == false))
                            bubbleEvent = false;
                        return;

                    case BoProgressBarEventTypes.pbet_ProgressBarReleased:
                        if(OnProgressBarReleased() == false)
                            bubbleEvent = false;
                        return;
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: ProgressBarHandler raised\n"
                  + e.InnerException.Message);
            }
        }

        public abstract bool OnBeforeProgressBarCreated();

        public abstract bool OnAfterProgressBarCreated();

        public abstract bool OnBeforeProgressBarStopped(bool success);

        public abstract bool OnAfterProgressBarStopped(bool success);

        public abstract bool OnProgressBarReleased();

        private static void Batch(string fileName)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(fileName);
            string xmlString = xmlDoc.InnerXml;
            BoneUserInterfaceManager.Instance.LoadXml(xmlString);
            new BoneBatchInfo(BoneConnectionContext.Application);
        }

        protected static void InitializeXmlMenus()
        {
            string menuXmlFile = "addMenus.xml";
            if(!File.Exists(menuXmlFile))
            {
                menuXmlFile = menuXmlFile.Insert(0, "..\\");
            }
            if(File.Exists(menuXmlFile))
            {
                Batch(menuXmlFile);
            }
            menuXmlFile = "removeMenus.xml";
            if(!File.Exists(menuXmlFile))
            {
                menuXmlFile = menuXmlFile.Insert(0, "..\\");
            }
            if(File.Exists(menuXmlFile))
            {
                Batch(menuXmlFile);
            }
            menuXmlFile = "updateMenus.xml";
            if(!File.Exists(menuXmlFile))
            {
                menuXmlFile = menuXmlFile.Insert(0, "..\\");
            }
            if(File.Exists(menuXmlFile))
            {
                Batch(menuXmlFile);
            }
        }

        protected BoneAddOn()
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler;
            InitializeXmlMenus();

            BoneListeners listeners = new BoneListeners();

            BoneConnectionContext.Application.MenuEvent += listeners.MenuHandler;
            BoneConnectionContext.Application.ItemEvent += listeners.ItemHandler;
            BoneConnectionContext.Application.AppEvent += ApplHandler;
            BoneConnectionContext.Application.ProgressBarEvent += ProgressBarHandler;
            BoneConnectionContext.Application.StatusBarEvent += StatusBarHandler;

#if V2005 || V2005_SP01 || V2007 || V2007W1
      BoneConnectionContext.Application.RightClickEvent += listeners.RightClickHandler;
      BoneConnectionContext.Application.PrintEvent += listeners.PrintHandler;
      BoneConnectionContext.Application.ReportDataEvent += listeners.ReportDataHandler;
#endif

#if V2005_SP01 || V2007 || V2007W1
      BoneConnectionContext.Application.FormDataEvent += listeners.FormDataHandler;
#endif
            ////////////////////////////////////////////////////////////////////////////////
            // set the addon filter
            ////////////////////////////////////////////////////////////////////////////////

            EventFilters filters = listeners.BuildFilter();
            BoneConnectionContext.Application.SetFilter(filters);
        }

        private void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            if(e.ExceptionObject is Exception)
            {
                Exception ex = e.ExceptionObject as Exception;
                ShowError(ex.Message, ex.Source, ex);
            }
            else
            {
                ShowError(e.ExceptionObject.ToString(), "Global Unhandled Exception", null);
            }
        }

        public static void ShowError(String msg, String source, Exception e)
        {
            try
            {
                Form form = BoneConnectionContext.Application.Forms.GetForm(BoneFormConstants.ExceptionFormUid, 1);
                if(form != null)
                {
                    form.Select();
                    return;
                }
            }
            catch(Exception)
            {
            }
            try
            {
                ExceptionForm.ShowModal(msg, e);
            }
            catch(Exception)
            {
                String innerMessage = String.Empty;
                Exception exception = e;
                while(exception != null)
                {
                    innerMessage += exception.Message;
                    if(exception.InnerException != null)
                    {
                        innerMessage += Environment.NewLine;
                    }
                    exception = exception.InnerException;
                }
                BoneConnectionContext.Application.MessageBox(String.Format("{0}{1}{2}{3}{4}", msg, innerMessage, Environment.NewLine, Environment.NewLine, e.StackTrace),
                1,
                "Cancel",
                "",
                ""
            );
            }
        }
    }
}
