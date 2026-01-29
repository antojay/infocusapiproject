//****************************************************************************
//
//  File:      EventTables.cs
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
using SAPbouiCOM;

namespace Infocus.Framework.SapBone
{
    /// <summary>
    /// Manages all information about Events.
    /// Keeps a list of all the possible events by type of object and 
    /// maps the information between an event and the function who manages it. 
    /// </summary>
    public class EventTables
    {
        private Hashtable beforeMethodNames = new Hashtable();
        private Hashtable afterMethodNames = new Hashtable();
        private Hashtable itemEventTypes = new Hashtable();
        private Hashtable beforeEnabled = new Hashtable();
        private Hashtable returnTypes = new Hashtable();
        private Hashtable itemTypes = new Hashtable();
        private BoEventTypes[] formEventTypes;
        private BoEventTypes[] genericEventTypes;
        private static EventTables et;

        ////////////////////////////////////////////////////////////////////////

        private EventTables()
        {
            ////////////////////////////////////////////////////////////////////////
            // beforeEnabled
            ////////////////////////////////////////////////////////////////////////

            // Matrix Events
            beforeEnabled[BoEventTypes.et_MATRIX_LOAD] = true;
            beforeEnabled[BoEventTypes.et_DATASOURCE_LOAD] = true;
            beforeEnabled[BoEventTypes.et_MATRIX_COLLAPSE_PRESSED] = true;

            // Menu Events
            beforeEnabled[BoEventTypes.et_MENU_CLICK] = true;

            // General Events
            beforeEnabled[BoEventTypes.et_CLICK] = true;
            beforeEnabled[BoEventTypes.et_DOUBLE_CLICK] = true;
            beforeEnabled[BoEventTypes.et_KEY_DOWN] = true;
            beforeEnabled[BoEventTypes.et_GOT_FOCUS] = false;
            beforeEnabled[BoEventTypes.et_LOST_FOCUS] = false;

            // Item Events
#if V2005_SP01 || V2007 || V2007W1
            beforeEnabled[BoEventTypes.et_VALIDATE] = true;
#else
      beforeEnabled[ BoEventTypes.et_VALIDATE			] = false;
#endif
            beforeEnabled[BoEventTypes.et_ITEM_PRESSED] = true;
            beforeEnabled[BoEventTypes.et_COMBO_SELECT] = true;
            beforeEnabled[BoEventTypes.et_MATRIX_LINK_PRESSED] = true;

            // Form Events
#if V2005_SP01 || V2007 || V2007W1
            beforeEnabled[BoEventTypes.et_FORM_LOAD] = true;
            beforeEnabled[BoEventTypes.et_FORM_CLOSE] = true;
#else
      beforeEnabled[ BoEventTypes.et_FORM_LOAD		] = false;
      beforeEnabled[ BoEventTypes.et_FORM_CLOSE		] = false;
#endif
            beforeEnabled[BoEventTypes.et_FORM_UNLOAD] = true;
            beforeEnabled[BoEventTypes.et_FORM_ACTIVATE] = false;
            beforeEnabled[BoEventTypes.et_FORM_DEACTIVATE] = false;
            beforeEnabled[BoEventTypes.et_FORM_RESIZE] = false;
            beforeEnabled[BoEventTypes.et_FORM_KEY_DOWN] = true;
            beforeEnabled[BoEventTypes.et_FORM_MENU_HILIGHT] = false;

            // Form Data Events
#if V2005_SP01 || V2007 || V2007W1
            beforeEnabled[BoEventTypes.et_FORM_DATA_ADD] = true;
            beforeEnabled[BoEventTypes.et_FORM_DATA_DELETE] = true;
            beforeEnabled[BoEventTypes.et_FORM_DATA_LOAD] = true;
            beforeEnabled[BoEventTypes.et_FORM_DATA_UPDATE] = true;
#endif

            // Other Events
#if V2005 || V2005_SP01 || V2007 || V2007W1
            beforeEnabled[BoEventTypes.et_CHOOSE_FROM_LIST] = true;
            beforeEnabled[BoEventTypes.et_RIGHT_CLICK] = true;

            // Print Events => Form Events
            beforeEnabled[BoEventTypes.et_PRINT] = true;
            beforeEnabled[BoEventTypes.et_PRINT_DATA] = true;
#endif


            ////////////////////////////////////////////////////////////////////////
            // returnTypes = BubbleEvent
            ////////////////////////////////////////////////////////////////////////

            returnTypes[BoEventTypes.et_MATRIX_LOAD] = false;
            returnTypes[BoEventTypes.et_DATASOURCE_LOAD] = false;
            returnTypes[BoEventTypes.et_MATRIX_COLLAPSE_PRESSED] = false;

            returnTypes[BoEventTypes.et_CLICK] = false;
            returnTypes[BoEventTypes.et_DOUBLE_CLICK] = false;
            returnTypes[BoEventTypes.et_KEY_DOWN] = false;
            returnTypes[BoEventTypes.et_GOT_FOCUS] = false;
            returnTypes[BoEventTypes.et_LOST_FOCUS] = false;

            returnTypes[BoEventTypes.et_ITEM_PRESSED] = false;
            returnTypes[BoEventTypes.et_COMBO_SELECT] = false;
            returnTypes[BoEventTypes.et_MATRIX_LINK_PRESSED] = false;

            returnTypes[BoEventTypes.et_FORM_UNLOAD] = false;
            returnTypes[BoEventTypes.et_FORM_ACTIVATE] = false;
            returnTypes[BoEventTypes.et_FORM_DEACTIVATE] = false;
            returnTypes[BoEventTypes.et_FORM_RESIZE] = false;
            returnTypes[BoEventTypes.et_FORM_KEY_DOWN] = false;
            returnTypes[BoEventTypes.et_FORM_MENU_HILIGHT] = false;

#if V2005_SP01 || V2007 || V2007W1
            returnTypes[BoEventTypes.et_VALIDATE] = false;
            returnTypes[BoEventTypes.et_FORM_LOAD] = false;
            returnTypes[BoEventTypes.et_FORM_CLOSE] = false;

            returnTypes[BoEventTypes.et_FORM_DATA_ADD] = false;
            returnTypes[BoEventTypes.et_FORM_DATA_DELETE] = false;
            returnTypes[BoEventTypes.et_FORM_DATA_LOAD] = false;
            returnTypes[BoEventTypes.et_FORM_DATA_UPDATE] = false;
#else
      returnTypes[ BoEventTypes.et_VALIDATE			] = true;
      returnTypes[ BoEventTypes.et_FORM_LOAD			] = true;
      returnTypes[ BoEventTypes.et_FORM_CLOSE			] = true;
#endif

#if V2005 || V2005_SP01 || V2007 || V2007W1
            returnTypes[BoEventTypes.et_CHOOSE_FROM_LIST] = false;
            returnTypes[BoEventTypes.et_RIGHT_CLICK] = false;

            returnTypes[BoEventTypes.et_PRINT] = false;
            returnTypes[BoEventTypes.et_PRINT_DATA] = false;
#endif
            returnTypes[BoEventTypes.et_MENU_CLICK] = false;

            ////////////////////////////////////////////////////////////////////////
            // Generic Event Types
            ////////////////////////////////////////////////////////////////////////

            genericEventTypes =
              new BoEventTypes[] { BoEventTypes.et_FORM_LOAD,	
                             BoEventTypes.et_FORM_UNLOAD,
                             BoEventTypes.et_FORM_ACTIVATE,
                             BoEventTypes.et_FORM_DEACTIVATE,	
                             BoEventTypes.et_FORM_CLOSE,	
                             BoEventTypes.et_FORM_RESIZE,
                             BoEventTypes.et_FORM_KEY_DOWN,
                             BoEventTypes.et_FORM_MENU_HILIGHT
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_PRINT,
                             BoEventTypes.et_PRINT_DATA,
                             BoEventTypes.et_RIGHT_CLICK 
#endif
#if V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_FORM_DATA_ADD
                             , BoEventTypes.et_FORM_DATA_DELETE
                             , BoEventTypes.et_FORM_DATA_LOAD
                             , BoEventTypes.et_FORM_DATA_UPDATE
#endif
                              , BoEventTypes.et_CLICK, 
                              BoEventTypes.et_DOUBLE_CLICK,
                              BoEventTypes.et_ITEM_PRESSED,
                              BoEventTypes.et_CHOOSE_FROM_LIST,

                              BoEventTypes.et_KEY_DOWN,

                              BoEventTypes.et_GOT_FOCUS,
                              BoEventTypes.et_LOST_FOCUS, 
                              BoEventTypes.et_COMBO_SELECT ,

                              BoEventTypes.et_KEY_DOWN,
                              BoEventTypes.et_VALIDATE,

                              BoEventTypes.et_MATRIX_LINK_PRESSED,
                              BoEventTypes.et_MATRIX_LOAD,
                              BoEventTypes.et_DATASOURCE_LOAD,
                              BoEventTypes.et_MATRIX_COLLAPSE_PRESSED
                           };

            ////////////////////////////////////////////////////////////////////////
            // formEventTypes
            ////////////////////////////////////////////////////////////////////////

            formEventTypes =
              new BoEventTypes[] {	BoEventTypes.et_FORM_LOAD,	
                             BoEventTypes.et_FORM_UNLOAD,
                             BoEventTypes.et_FORM_ACTIVATE,
                             BoEventTypes.et_FORM_DEACTIVATE,	
                             BoEventTypes.et_FORM_CLOSE,	
                             BoEventTypes.et_FORM_RESIZE,
                             BoEventTypes.et_FORM_KEY_DOWN,
                             BoEventTypes.et_FORM_MENU_HILIGHT
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_PRINT,
                             BoEventTypes.et_PRINT_DATA,
                             BoEventTypes.et_RIGHT_CLICK 
#endif
#if V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_FORM_DATA_ADD
                             , BoEventTypes.et_FORM_DATA_DELETE
                             , BoEventTypes.et_FORM_DATA_LOAD
                             , BoEventTypes.et_FORM_DATA_UPDATE
#endif
                           };
            ////////////////////////////////////////////////////////////////////////
            // itemEventTable
            ////////////////////////////////////////////////////////////////////////

            itemEventTypes.Add(BoFormItemTypes.it_BUTTON,
              new BoEventTypes[] {	BoEventTypes.et_CLICK, 
                             BoEventTypes.et_DOUBLE_CLICK,
                             BoEventTypes.et_ITEM_PRESSED
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_CHOOSE_FROM_LIST 
#endif
                           });

            itemEventTypes.Add(BoFormItemTypes.it_CHECK_BOX,
              new BoEventTypes[] {	BoEventTypes.et_CLICK, 
                             BoEventTypes.et_DOUBLE_CLICK,
                             BoEventTypes.et_ITEM_PRESSED,
                             BoEventTypes.et_KEY_DOWN});

            itemEventTypes.Add(BoFormItemTypes.it_COMBO_BOX,
              new BoEventTypes[] {	BoEventTypes.et_CLICK, 
                             BoEventTypes.et_DOUBLE_CLICK, 
                             BoEventTypes.et_GOT_FOCUS,
                             BoEventTypes.et_LOST_FOCUS, 
                             BoEventTypes.et_COMBO_SELECT 
#if	V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_RIGHT_CLICK 
#endif
                           });

            itemEventTypes.Add(BoFormItemTypes.it_EDIT,
              new BoEventTypes[] {	BoEventTypes.et_CLICK, 
                             BoEventTypes.et_DOUBLE_CLICK, 
                             BoEventTypes.et_KEY_DOWN,
                             BoEventTypes.et_GOT_FOCUS,
                             BoEventTypes.et_LOST_FOCUS, 
                             BoEventTypes.et_VALIDATE
#if	V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_CHOOSE_FROM_LIST 
                             , BoEventTypes.et_RIGHT_CLICK 
#endif
                           });

            itemEventTypes.Add(BoFormItemTypes.it_FOLDER,
              new BoEventTypes[] {	BoEventTypes.et_ITEM_PRESSED,
                             BoEventTypes.et_CLICK });

            itemEventTypes.Add(BoFormItemTypes.it_MATRIX,        // Column
              new BoEventTypes[] {	BoEventTypes.et_CLICK,        // Column
                             BoEventTypes.et_GOT_FOCUS,    // Column
                             BoEventTypes.et_LOST_FOCUS,   // Column
                             BoEventTypes.et_VALIDATE,     // Column
                             BoEventTypes.et_ITEM_PRESSED, // Column
                             BoEventTypes.et_COMBO_SELECT, // Column
                             BoEventTypes.et_MATRIX_LINK_PRESSED,
                             BoEventTypes.et_MATRIX_LOAD,
                             BoEventTypes.et_DATASOURCE_LOAD,
                             BoEventTypes.et_MATRIX_COLLAPSE_PRESSED
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_CHOOSE_FROM_LIST, // Column & Row
                             BoEventTypes.et_RIGHT_CLICK       // Column
#endif
                           });

#if V2005 || V2005_SP01 || V2007 || V2007W1
            itemEventTypes.Add(BoFormItemTypes.it_GRID,        // Column
              new BoEventTypes[] {	BoEventTypes.et_CLICK,        // Column
                             BoEventTypes.et_GOT_FOCUS,    // Column
                             BoEventTypes.et_LOST_FOCUS,   // Column
                             BoEventTypes.et_VALIDATE,     // Column
                             BoEventTypes.et_ITEM_PRESSED, // Column
                             BoEventTypes.et_COMBO_SELECT, // Column
                             BoEventTypes.et_CHOOSE_FROM_LIST, // Column & Row
                             BoEventTypes.et_RIGHT_CLICK       // Column
                           });
#endif

            itemEventTypes.Add(BoFormItemTypes.it_OPTION_BUTTON,
              new BoEventTypes[] {	BoEventTypes.et_CLICK,
                             BoEventTypes.et_DOUBLE_CLICK,
                             BoEventTypes.et_ITEM_PRESSED});

            itemEventTypes.Add(BoFormItemTypes.it_STATIC,
              new BoEventTypes[] {	BoEventTypes.et_CLICK,
                             BoEventTypes.et_DOUBLE_CLICK,
                             BoEventTypes.et_ITEM_PRESSED
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_RIGHT_CLICK
#endif
                           });

            itemEventTypes.Add(BoFormItemTypes.it_EXTEDIT,
              new BoEventTypes[] {	BoEventTypes.et_CLICK,
                             BoEventTypes.et_DOUBLE_CLICK,
                             BoEventTypes.et_GOT_FOCUS,
                             BoEventTypes.et_VALIDATE
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_RIGHT_CLICK
#endif
                           });

            itemEventTypes.Add(BoFormItemTypes.it_PANE_COMBO_BOX,
              new BoEventTypes[] {  BoEventTypes.et_CLICK, 
                             BoEventTypes.et_DOUBLE_CLICK, 
                             BoEventTypes.et_GOT_FOCUS,
                             BoEventTypes.et_LOST_FOCUS });

            itemEventTypes.Add(BoFormItemTypes.it_PICTURE,
              new BoEventTypes[] {	BoEventTypes.et_CLICK
#if V2005 || V2005_SP01 || V2007 || V2007W1
                             , BoEventTypes.et_RIGHT_CLICK
#endif
                           });

            ////////////////////////////////////////////////////////////////////////
            // itemTypes
            ////////////////////////////////////////////////////////////////////////

            NodeType itemType = new NodeType();
            itemType.typeName = "Button";
            itemType.varName = "button";
            itemType.type = "Button";
            itemTypes.Add(BoFormItemTypes.it_BUTTON, itemType);

            itemType = new NodeType();
            itemType.typeName = "CheckBox";
            itemType.varName = "checkbox";
            itemType.type = "CheckBox";
            itemTypes.Add(BoFormItemTypes.it_CHECK_BOX, itemType);

            itemType = new NodeType();
            itemType.typeName = "ComboBox";
            itemType.varName = "combobox";
            itemType.type = "ComboBox";
            itemTypes.Add(BoFormItemTypes.it_COMBO_BOX, itemType);

            itemType = new NodeType();
            itemType.typeName = "StaticText";
            itemType.varName = "statictext";
            itemType.type = "StaticText";
            itemTypes.Add(BoFormItemTypes.it_STATIC, itemType);

            itemType = new NodeType();
            itemType.typeName = "EditText";
            itemType.varName = "edittext";
            itemType.type = "EditText";
            itemTypes.Add(BoFormItemTypes.it_EDIT, itemType);

            itemType = new NodeType();
            itemType.typeName = "Folder";
            itemType.varName = "folder";
            itemType.type = "Folder";
            itemTypes.Add(BoFormItemTypes.it_FOLDER, itemType);

            itemType = new NodeType();
            itemType.typeName = "Matrix";
            itemType.varName = "matrix";
            itemType.type = "Matrix";
            itemTypes.Add(BoFormItemTypes.it_MATRIX, itemType);

#if V2005 || V2005_SP01 || V2007 || V2007W1
            itemType = new NodeType();
            itemType.typeName = "Grid";
            itemType.varName = "grid";
            itemType.type = "Grid";
            itemTypes.Add(BoFormItemTypes.it_GRID, itemType);
#endif

            itemType = new NodeType();
            itemType.typeName = "OptionButton";
            itemType.varName = "optbutton";
            itemType.type = "OptionBtn";
            itemTypes.Add(BoFormItemTypes.it_OPTION_BUTTON, itemType);

            itemType = new NodeType();
            itemType.typeName = "Picture";
            itemType.varName = "picture";
            itemType.type = "PictureBox";
            itemTypes.Add(BoFormItemTypes.it_PICTURE, itemType);

            itemType = new NodeType();
            itemType.typeName = "PaneComboBox";
            itemType.varName = "panecombobox";
            itemType.type = "PaneComboBox";
            itemTypes.Add(BoFormItemTypes.it_PANE_COMBO_BOX, itemType);

            ////////////////////////////////////////////////////////////////////////
            // beforeMethodNames
            ////////////////////////////////////////////////////////////////////////

            beforeMethodNames[BoEventTypes.et_MATRIX_LOAD] = "OnBeforeMatrixLoad";
            beforeMethodNames[BoEventTypes.et_DATASOURCE_LOAD] = "OnBeforeDatasourceLoad";
            beforeMethodNames[BoEventTypes.et_MATRIX_COLLAPSE_PRESSED] = "OnBeforeMatrixCollapsePressed";

            beforeMethodNames[BoEventTypes.et_CLICK] = "OnBeforeClick";
            beforeMethodNames[BoEventTypes.et_DOUBLE_CLICK] = "OnBeforeDoubleClick";
            beforeMethodNames[BoEventTypes.et_KEY_DOWN] = "OnBeforeKeyDown";

            beforeMethodNames[BoEventTypes.et_ITEM_PRESSED] = "OnBeforeItemPressed";
            beforeMethodNames[BoEventTypes.et_COMBO_SELECT] = "OnBeforeComboSelect";
            beforeMethodNames[BoEventTypes.et_MATRIX_LINK_PRESSED] = "OnBeforeMatrixLinkPressed";

            beforeMethodNames[BoEventTypes.et_FORM_UNLOAD] = "OnBeforeFormUnload";
            beforeMethodNames[BoEventTypes.et_FORM_ACTIVATE] = "OnBeforeFormActivate";
            beforeMethodNames[BoEventTypes.et_FORM_KEY_DOWN] = "OnBeforeFormKeyDown";

#if V2005_SP01 || V2007 || V2007W1
            beforeMethodNames[BoEventTypes.et_FORM_LOAD] = "OnBeforeFormLoad";
            beforeMethodNames[BoEventTypes.et_FORM_CLOSE] = "OnBeforeFormClose";
            beforeMethodNames[BoEventTypes.et_VALIDATE] = "OnBeforeValidate";

            beforeMethodNames[BoEventTypes.et_FORM_DATA_ADD] = "OnBeforeFormDataAdd";
            beforeMethodNames[BoEventTypes.et_FORM_DATA_DELETE] = "OnBeforeFormDataDelete";
            beforeMethodNames[BoEventTypes.et_FORM_DATA_LOAD] = "OnBeforeFormDataLoad";
            beforeMethodNames[BoEventTypes.et_FORM_DATA_UPDATE] = "OnBeforeFormDataUpdate";
#endif
            beforeMethodNames[BoEventTypes.et_MENU_CLICK] = "OnBeforeMenuClick";

#if V2005 || V2005_SP01 || V2007 || V2007W1
            beforeMethodNames[BoEventTypes.et_PRINT] = "OnBeforePrint";
            beforeMethodNames[BoEventTypes.et_PRINT_DATA] = "OnBeforePrintData";

            beforeMethodNames[BoEventTypes.et_CHOOSE_FROM_LIST] = "OnBeforeChooseFromList"; ;
            beforeMethodNames[BoEventTypes.et_RIGHT_CLICK] = "OnBeforeRightClick"; ;
#endif

            ////////////////////////////////////////////////////////////////////////
            // afterMethodNames
            ////////////////////////////////////////////////////////////////////////

            afterMethodNames[BoEventTypes.et_MENU_CLICK] = "OnAfterMenuClick";

            afterMethodNames[BoEventTypes.et_CLICK] = "OnAfterClick";
            afterMethodNames[BoEventTypes.et_DOUBLE_CLICK] = "OnAfterDoubleClick";
            afterMethodNames[BoEventTypes.et_KEY_DOWN] = "OnAfterKeyDown";
            afterMethodNames[BoEventTypes.et_GOT_FOCUS] = "OnGotFocus";
            afterMethodNames[BoEventTypes.et_LOST_FOCUS] = "OnLostFocus";

            afterMethodNames[BoEventTypes.et_MATRIX_LOAD] = "OnAfterMatrixLoad";
            afterMethodNames[BoEventTypes.et_DATASOURCE_LOAD] = "OnAfterDatasourceLoad";
            afterMethodNames[BoEventTypes.et_MATRIX_COLLAPSE_PRESSED] = "OnAfterMatrixCollapsePressed";

            afterMethodNames[BoEventTypes.et_ITEM_PRESSED] = "OnAfterItemPressed";
            afterMethodNames[BoEventTypes.et_COMBO_SELECT] = "OnAfterComboSelect";
            afterMethodNames[BoEventTypes.et_MATRIX_LINK_PRESSED] = "OnAfterMatrixLinkPressed";

            afterMethodNames[BoEventTypes.et_FORM_UNLOAD] = "OnAfterFormUnload";
            afterMethodNames[BoEventTypes.et_FORM_ACTIVATE] = "OnAfterFormActivate";
            afterMethodNames[BoEventTypes.et_FORM_DEACTIVATE] = "OnFormDeactivate";
            afterMethodNames[BoEventTypes.et_FORM_RESIZE] = "OnFormResize";
            afterMethodNames[BoEventTypes.et_FORM_KEY_DOWN] = "OnAfterFormKeyDown";
            afterMethodNames[BoEventTypes.et_FORM_MENU_HILIGHT] = "OnFormMenuHilight";

#if V2005_SP01 || V2007 || V2007W1
            afterMethodNames[BoEventTypes.et_VALIDATE] = "OnAfterValidate";
            afterMethodNames[BoEventTypes.et_FORM_LOAD] = "OnAfterFormLoad";
            afterMethodNames[BoEventTypes.et_FORM_CLOSE] = "OnAfterFormClose";

            afterMethodNames[BoEventTypes.et_FORM_DATA_ADD] = "OnAfterFormDataAdd";
            afterMethodNames[BoEventTypes.et_FORM_DATA_DELETE] = "OnAfterFormDataDelete";
            afterMethodNames[BoEventTypes.et_FORM_DATA_LOAD] = "OnAfterFormDataLoad";
            afterMethodNames[BoEventTypes.et_FORM_DATA_UPDATE] = "OnAfterFormDataUpdate";
#else
      afterMethodNames[ BoEventTypes.et_VALIDATE ]		= "OnValidate";
      afterMethodNames[ BoEventTypes.et_FORM_LOAD ]		= "OnFormLoad";
      afterMethodNames[ BoEventTypes.et_FORM_CLOSE ]		= "OnFormClose";
#endif

#if V2005 || V2005_SP01 || V2007 || V2007W1
            afterMethodNames[BoEventTypes.et_PRINT] = "OnAfterPrint";
            afterMethodNames[BoEventTypes.et_PRINT_DATA] = "OnAfterPrintData";

            afterMethodNames[BoEventTypes.et_CHOOSE_FROM_LIST] = "OnAfterChooseFromList";
            afterMethodNames[BoEventTypes.et_RIGHT_CLICK] = "OnAfterRightClick";
#endif

        }

        /////////////////////////////////////////////////////////////////////////////////

        public static string getMenuClassName(string UID)
        {
            return "Menu__" + UID;
        }

        public static string getFormClassName(string formType)
        {
            return "Form__" + formType;
        }

        public static string getItemClassName(string itemType, string formType, string UID)
        {
            return itemType + "__" + formType + "__" + UID;
        }

        public static string getEventsHandlerClassName()
        {
            return "EventsHandler";
        }

        public static BoEventTypes[] getGenericEvents()
        {
            if(et == null)
                et = new EventTables();
            return et.genericEventTypes;
        }

        public static BoEventTypes[] getFormEvents()
        {
            if(et == null)
                et = new EventTables();
            return et.formEventTypes;
        }

        public static BoEventTypes[] getItemEvents(BoFormItemTypes type)
        {
            if(et == null)
                et = new EventTables();
            return (BoEventTypes[])et.itemEventTypes[type];
        }

        public static BoEventTypes[] getItemEvents(string itemTypeName)
        {
            if(et == null)
                et = new EventTables();

            foreach(BoFormItemTypes key in et.itemTypes.Keys)
            {
                NodeType itemType = (NodeType)et.itemTypes[key];
                if(itemType.typeName.Equals(itemTypeName) == true)
                    return getItemEvents(key);
            }

            return null;
        }

        public static string getMethodName(BoEventTypes type, bool before)
        {
            if(et == null)
                et = new EventTables();
            return (before == true) ?
              (string)et.beforeMethodNames[type] :
              (string)et.afterMethodNames[type];
        }

        public static string GetMethodReturn(BoEventTypes type, bool before)
        {
            if(et == null)
                et = new EventTables();
            return (before == true) || ((bool)et.returnTypes[type]) ?
              "System.Boolean" :
              "System.Void";
        }

        public static NodeType GetItemType(BoFormItemTypes itemType)
        {
            if(et == null)
                et = new EventTables();
            return (NodeType)et.itemTypes[itemType];
        }

        public static NodeType GetItemType(string itemTypeName)
        {
            if(et == null)
                et = new EventTables();

            foreach(BoFormItemTypes key in et.itemTypes.Keys)
            {
                NodeType itemType = (NodeType)et.itemTypes[key];
                if(itemType.typeName.Equals(itemTypeName) == true)
                    return itemType;
            }

            return null;
        }

        public static string GetEventKey(MenuEvent pVal)
        {
            return pVal.MenuUID + "." + pVal.BeforeAction;
        }

        public static string GetGenericEventKey(MenuEvent pVal)
        {
            return "*." + pVal.BeforeAction.ToString();
        }

        public static string GetActionKey(string UID, bool before)
        {
            return UID + "." + before;
        }

        public static string GetEventKey(ItemEvent pVal)
        {
            return pVal.FormTypeEx + "." + pVal.ItemUID + "." + pVal.BeforeAction;
        }

        public static string GetGenericEventKey(ItemEvent pVal, bool formExists)
        {
            if(formExists)
            {
                return pVal.FormTypeEx + ".*." + pVal.BeforeAction;
            }
            else
            {
                return "*.*." + pVal.BeforeAction.ToString();
            }
        }

        public static string GetActionKey(string formType, string itemUID, bool before)
        {
            return formType + "." + itemUID + "." + before;
        }

#if V2005 || V2005_SP01 || V2007 || V2007W1
        public static string GetEventKey(ContextMenuInfo pVal)
        {

            return BoneConnectionContext.Application.Forms.Item(pVal.FormUID).TypeEx + "."
            + pVal.ItemUID + "." + pVal.BeforeAction.ToString();
        }

        public static string GetGenericEventKey(ContextMenuInfo pVal, bool formExists)
        {
            if(formExists)
            {
                return BoneConnectionContext.Application.Forms.Item(pVal.FormUID).TypeEx + ".*."
                + pVal.BeforeAction.ToString();
            }
            else
            {
                return "*.*." + pVal.BeforeAction.ToString();
            }
        }

        public static string GetEventKey(PrintEventInfo pVal)
        {

            return BoneConnectionContext.Application.Forms.Item(pVal.FormUID).TypeEx + ".."
            + pVal.BeforeAction.ToString();
        }

        public static string GetGenericEventKey(PrintEventInfo pVal, bool formExists)
        {
            if(formExists)
            {
                return BoneConnectionContext.Application.Forms.Item(pVal.FormUID).TypeEx + ".*."
                  + pVal.BeforeAction.ToString();
            }
            else
            {
                return "*.*." + pVal.BeforeAction.ToString();
            }
        }

        public static string GetEventKey(ReportDataInfo pVal)
        {
            return BoneConnectionContext.Application.Forms.Item(pVal.FormUID).TypeEx + ".."
              + pVal.BeforeAction.ToString();
        }

        public static string GetGenericEventKey(ReportDataInfo pVal, bool formExists)
        {
            if(formExists)
            {
                return BoneConnectionContext.Application.Forms.Item(pVal.FormUID).TypeEx + ".*."
                  + pVal.BeforeAction.ToString();
            }
            else
            {
                return "*.*." + pVal.BeforeAction.ToString();
            }
        }
#endif

#if V2005_SP01 || V2007 || V2007W1
        public static string GetEventKey(BusinessObjectInfo pVal)
        {
            return pVal.FormTypeEx + ".." + pVal.BeforeAction.ToString();
        }

        public static string GetGenericEventKey(BusinessObjectInfo pVal, bool formExists)
        {
            if(formExists)
            {
                return pVal.FormTypeEx + ".*." + pVal.BeforeAction.ToString();
            }
            else
            {
                return "*.*." + pVal.BeforeAction.ToString();
            }
        }
#endif

        public static string GetActionKey(bool before)
        {
            return before.ToString();
        }

        public static bool IsBeforeEnabled(BoEventTypes type)
        {
            if(et == null)
                et = new EventTables();
            return (bool)et.beforeEnabled[type];
        }

        public static bool IsSuccessAccessValid(BoEventTypes type, bool before)
        {
            string methodName = getMethodName(type, before);
            return methodName.StartsWith("OnAfter");
        }

        public static bool IsFormAccessValid(BoEventTypes type, bool before)
        {
            string methodName = getMethodName(type, before);
            return (methodName.Equals("OnAfterFormUnload") == false);
        }

        public static bool IsValidEvent(BoFormItemTypes itemType, BoEventTypes eventType)
        {
            if(et == null)
                et = new EventTables();
            BoEventTypes[] eventTypes = (BoEventTypes[])et.itemEventTypes[itemType];
            if(eventTypes == null)
                return false;

            foreach(BoEventTypes eType in eventTypes)
                if(eType == eventType)
                    return true;
            return false;
        }
    }
}
