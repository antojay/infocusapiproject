//****************************************************************************
//
//  File:      BoneXmlFormMenu.cs
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
using System.Collections;
using System.Xml;
using SAPbouiCOM;

namespace Infocus.Framework.SapBone
{
  /// <summary>
  /// Base class for the events management on a menu entity who 
  /// Loads an Xml form on the menu event.
  /// </summary>
  /// <remarks>
  /// In your project you will have a class inheriting from BoneXmlFormMenu
  /// for each menu loading an xml form you want to handle one/several events.
  /// </remarks>
  public abstract class BoneXmlFormMenu : BoneMenu
  {
    static private int		counter = 0;
    /// <summary>
    /// Form unique ID tag.
    /// </summary>
    static private string	formUID;
    /// <summary>
    /// Path of the form unique ID tag inside a xml file representing a form.
    /// </summary>
    static private string	UIDPath = "Application/forms/action/form/@uid";
    private XmlDocument		xmlDoc;

    //////////////////////////////////////////////////////////////////////////////////
		
    /// <summary>
    /// Default constructor.
    /// </summary>
    protected BoneXmlFormMenu()
    {
    }

    /// <summary>
    /// Loads the xml string defining a form.
    /// </summary>
    /// <param name="xmlFile">Xml file name defining a form.</param>
    protected void LoadXml(string xmlFile)
    {
      xmlDoc = new XmlDocument();

      if (!System.IO.File.Exists(xmlFile))
      {
        xmlFile = xmlFile.Insert(0, "..\\");
      }

      if (System.IO.File.Exists(xmlFile))
      {
        xmlDoc.Load(xmlFile);
        formUID = xmlDoc.SelectSingleNode(UIDPath).Value;
      }
      else
      {
        BoneConnectionContext.Application.MessageBox("ERROR: File " + xmlFile + " not found", -1, "", "", "");
      }
    }

    /// <summary>
    /// Opens and displays the Xml form previously loaded into the B1 application.
    /// </summary>
    protected void LoadForm()
    {
      if (xmlDoc.HasChildNodes)
      {
        xmlDoc.SelectSingleNode(UIDPath).Value = formUID + counter++;
        string xmlStr = xmlDoc.DocumentElement.OuterXml;
        BoneUserInterfaceManager.Instance.LoadXml(xmlStr);
        Form oForm = BoneConnectionContext.Application.Forms.ActiveForm;
        try 
        {
          UserDataSource oUDS = oForm.DataSources.UserDataSources.Item("FolderDS");
          if (oUDS != null)
            oUDS.Value = "1";
        }
        catch (Exception) {}
      }
      else
      {
        BoneConnectionContext.Application.MessageBox("ERROR: XML File containing the form not found", -1, "", "", "");
      }
    }
  }
}
