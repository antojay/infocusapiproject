//****************************************************************************
//
//  File:      BoneListeners.cs
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
using System.Reflection;
using System.Collections;
using SAPbouiCOM;

namespace Infocus.Framework.SapBone
{
    /// <summary>
    /// Represents a collection of listeners.
    /// </summary>
    internal class BoneListeners
    {
        /// <summary>
        /// Hashtable containing all the listeners of the AddOn.
        /// </summary>
        private Hashtable listenersTable = new Hashtable();

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor.
        /// Registers the listeners for each event asked by the user.  
        /// </summary>
        public BoneListeners()
        {
            Type attrType = Type.GetType("Infocus.Framework.SapBone.BoneListenerAttribute");
            foreach(Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if(asm.FullName.StartsWith("mscorlib"))
                {
                    continue;
                }
                else if(asm.FullName.StartsWith("Interop"))
                {
                    continue;
                }
                else if(asm.FullName.StartsWith("Microsoft"))
                {
                    continue;
                }
                else if(asm.FullName.StartsWith("System"))
                {
                    continue;
                }
                foreach(Type type in asm.GetTypes())
                {
                    //// ignore type different than Action
                    if(type.IsSubclassOf(Type.GetType("Infocus.Framework.SapBone.BoneAction")) == false)
                    {
                        continue;
                    }

                    //// ignore all the base abstract types
                    if(type.IsAbstract == true)
                    {
                        continue;
                    }
                    BoneAction action;

                    try
                    {
                        //// create the B1Action object
                        ConstructorInfo ctor = type.GetConstructor(new Type[0] { });
                        action = (BoneAction)ctor.Invoke(new BoneAddOn[] { });
                    }

                    catch(Exception e)
                    {
                        new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: " + type.Name + ".CTOR raised\n"
                          + e.InnerException.Message);
                        continue;
                    }

                    //// look into all the methods of the action ...
                    foreach(MethodInfo method in type.GetMethods())
                    {
                        //// ... and put the BoneListeners in the B1AddOn event table
                        foreach(Attribute attr in method.GetCustomAttributes(attrType, true))
                        {
                            BoneListenerAttribute listenerAttr = (BoneListenerAttribute)attr;
                            // generic events
                            if(type.IsSubclassOf(Type.GetType("Infocus.Framework.SapBone.BoneEvent")) == true)
                            {
                                // register listeners for all generic event keys
                                string[] keys = listenerAttr.GetEventActionKeys(action.GetKey(listenerAttr.GetBefore()));
                                foreach(string key in keys)
                                {
                                    registerListener(action, method, listenerAttr, key);
                                }
                            }
                            else // events specific to Items, Menus or Forms
                            {
                                string key = action.GetKey(listenerAttr.GetBefore());
                                registerListener(action, method, listenerAttr, key);
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Registers a listener. 
        /// Links together an action, the name of a method and the key of the element.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="method"></param>
        /// <param name="listener"></param>
        /// <param name="key"></param>
        private void registerListener(
          BoneAction action,
          MethodInfo method,
          BoneListenerAttribute listener,
          string key)
        {
            BoEventTypes eventType = listener.GetEventType();
            Hashtable table = (Hashtable)listenersTable[eventType];
            if(table == null)
            {
                table = new Hashtable();
                listenersTable[eventType] = table;
            }

            //////////////////////////////////////////////////////////////////////////
            // before entering the listener in the listener table check:
            // - whether the listener has the correct signature: parameter and return
            // - whether the listener will be ever called
            // - whether the same event is handled twice
            //////////////////////////////////////////////////////////////////////////

            if(EventTables.getMethodName(eventType, listener.GetBefore()) == null)
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\nis not supported and will not be registered");
                return;
            }

            if(method.ReturnType.ToString().Equals(
              EventTables.GetMethodReturn(eventType, listener.GetBefore())) == false)
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\nhas wrong return type and will not be registered");
                return;
            }

            if(method.GetParameters().Length != 1)
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\nhas a wrong number of parameters and will not be registered");
                return;
            }

            ParameterInfo paramInfo = (ParameterInfo)method.GetParameters().GetValue(0);
            if((eventType == BoEventTypes.et_MENU_CLICK)
              && (!paramInfo.ParameterType.ToString().Equals("SAPbouiCOM.MenuEvent")))
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\ndoes not take MenuEvent as parameter and will not be registered");
                return;
            }

#if	V2005 || V2005_SP01 || V2007 || V2007W1
            else if((eventType == BoEventTypes.et_PRINT)
              && (!paramInfo.ParameterType.ToString().Equals("SAPbouiCOM.PrintEventInfo")))
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\ndoes not take PrintEventInfo as parameter and will not be registered");
                return;
            }
            else if((eventType == BoEventTypes.et_PRINT_DATA)
                && (!paramInfo.ParameterType.ToString().Equals("SAPbouiCOM.ReportDataInfo")))
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                + method.DeclaringType.Name + "." + method.Name
                + "\ndoes not take ReportDataInfo as parameter and will not be registered");
                return;
            }
            else if((eventType == BoEventTypes.et_RIGHT_CLICK)
              && (!paramInfo.ParameterType.ToString().Equals("SAPbouiCOM.ContextMenuInfo")))
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                + method.DeclaringType.Name + "." + method.Name
                + "\ndoes not take ContextMenuInfo as parameter and will not be registered");
                return;
            }
#endif
#if V2005_SP01 || V2007 || V2007W1
            else if((eventType == BoEventTypes.et_FORM_DATA_ADD
              || eventType == BoEventTypes.et_FORM_DATA_DELETE
              || eventType == BoEventTypes.et_FORM_DATA_LOAD
              || eventType == BoEventTypes.et_FORM_DATA_UPDATE)
              && (!paramInfo.ParameterType.ToString().Equals("SAPbouiCOM.BusinessObjectInfo")))
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\ndoes not take BusinessObjectInfo as parameter and will not be registered");
                return;
            }
#endif

            else if((eventType != BoEventTypes.et_MENU_CLICK)

#if	V2005 || V2005_SP01 || V2007 || V2007W1
 && (eventType != BoEventTypes.et_PRINT)
        && (eventType != BoEventTypes.et_PRINT_DATA)
        && (eventType != BoEventTypes.et_RIGHT_CLICK)
#endif
#if V2005_SP01 || V2007 || V2007W1
 && (eventType != BoEventTypes.et_FORM_DATA_ADD)
        && (eventType != BoEventTypes.et_FORM_DATA_DELETE)
        && (eventType != BoEventTypes.et_FORM_DATA_LOAD)
        && (eventType != BoEventTypes.et_FORM_DATA_UPDATE)
#endif

 && (!paramInfo.ParameterType.ToString().Equals("SAPbouiCOM.ItemEvent")))
            {
                new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                  + method.DeclaringType.Name + "." + method.Name
                  + "\ndoes not take ItemEvent as parameter and will not be registered");
                return;
            }

            if(table[key] != null)
            {
                //new BoneInfo(BoneConnectionContext.Application, "ERROR: listener "
                //  + method.DeclaringType.Name + "." + method.Name
                //  + "\ndefines an already defined listener and will not be registered");
                return;
            }

            //////////////////////////////////////////////////////////////////////////

            table.Add(key, new BoneListener(action, method));
        }

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a filter with only the events the client wants to receive.
        /// </summary>
        /// <returns>EventFilters collection</returns>
        public EventFilters BuildFilter()
        {

            if(listenersTable.Count == 0)
                return null;

            //// loop on event tables
            EventFilters eventFilters = new EventFilters();
            foreach(BoEventTypes eventKey in listenersTable.Keys)
            {
                EventFilter eventFilter = (EventFilter)eventFilters.Add(eventKey);

                //// no forms in menu click
                if(eventKey.Equals(BoEventTypes.et_MENU_CLICK) == true)
                    continue;

                Hashtable formDefined = new Hashtable();
                Hashtable actions = (Hashtable)listenersTable[eventKey];

                // indicates if the action key is generic
                bool genericFlag = false;

                // check if any of the action keys apply to all forms (i.e., generic, indicated by *)
                // generic action keys are of the form "*.*.True"
                foreach(string actionKey in actions.Keys)
                {
                    if(actionKey.Substring(0, actionKey.IndexOf('.')).Equals("*"))
                    {
                        genericFlag = true;
                        break;
                    }
                }
                // if any of the action keys is generic (applies to all forms), then
                // the filters should not be set for the event type
                if(genericFlag == true)
                    continue;

                //// loop on actions in current event table
                foreach(string actionKey in actions.Keys)
                {
                    //// formType is the first part of the key
                    string form = actionKey.Substring(0, actionKey.IndexOf('.'));

                    if(formDefined[form] == null)
                    {
                        eventFilter.AddEx(form);
                        formDefined[form] = true;
                    }
                }
            }

            return eventFilters;
        }

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles all MenuEvents 
        /// and calls all the listeners registered to the event et_MENU_CLICK.
        /// </summary>
        /// <param name="pVal">MenuEvent</param>
        /// <param name="bubbleEvent">Specifies whether or not the application will continue processing this event</param>
        public void MenuHandler(
          ref MenuEvent pVal,
          out bool bubbleEvent)
        {
            bubbleEvent = true;
            try
            {
                // get the event table
                Hashtable table = (Hashtable)listenersTable[BoEventTypes.et_MENU_CLICK];
                if(table == null)
                    return;

                // Array to hold the 2 types of listeners for menu click event. This array may have
                // null values if a listener does not exist in the listener table for the given event key
                BoneListener[] listeners = new BoneListener[2];

                // get the listener for specific Menu Item
                listeners[0] = (BoneListener)table[EventTables.GetEventKey(pVal)];
                // get the generic listener for Menu Click Event
                listeners[1] = (BoneListener)table[EventTables.GetGenericEventKey(pVal)];

                foreach(BoneListener listener in listeners)
                {
                    if(listener != null) // check if listener exists
                    {
                        // handle the event
                        if(listener.Action.Action(listener.Method, pVal) == false)
                            bubbleEvent = false;
                    }
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: MenuHandler raised\n"
                  + e.InnerException.Message);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles all ItemEvents 
        /// and calls all the listeners registered to the received EventType.
        /// </summary>
        /// <param name="formUID">Unique ID of the form that received this event</param>
        /// <param name="pVal">ItemEvent information</param>
        /// <param name="bubbleEvent">Specifies whether or not the application will continue processing this event</param>
        public void ItemHandler(
          string formUID,
          ref ItemEvent pVal,
          out bool bubbleEvent)
        {
            bubbleEvent = true;
            try
            {
                // get the event table
                Hashtable table = (Hashtable)listenersTable[pVal.EventType];
                if(table == null)
                    return;

                // Array to hold the 3 types of listeners for the given event type. This array may have
                // null values if a listener does not exist in the listener table for the given event key
                BoneListener[] listeners = new BoneListener[3];

                // get the listener for specific Item and specific Form type
                listeners[0] = (BoneListener)table[EventTables.GetEventKey(pVal)];
                // get the generic listener for the Item Event on specified list of Form types
                listeners[1] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, true)];
                // get the generic listener for the Item Event on any/all Form types
                listeners[2] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, false)];

                foreach(BoneListener listener in listeners)
                {
                    if(listener != null) // check if listener exists
                    {
                        // handle the event
                        if(listener.Action.Action(listener.Method, pVal) == false)
                            bubbleEvent = false;
                    }
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: ItemHandler raised\n"
                  + e.InnerException.Message);
            }
        }
        /////////////////////////////////////////////////////////////////////////////////

#if V2005 || V2005_SP01 || V2007 || V2007W1

        /// <summary>
        /// Handles RightClickEvents 
        /// and calls all the listeners registered to the received EventType.
        /// </summary>
        /// <param name="ctxMenuInfo">Context menu info</param>
        /// <param name="bubbleEvent">Specifies whether or not the application will continue processing this event</param>
        public void RightClickHandler(
          ref ContextMenuInfo ctxMenuInfo,
          out bool bubbleEvent)
        {
            bubbleEvent = true;
            try
            {
                // get the event table
                Hashtable table = (Hashtable)listenersTable[BoEventTypes.et_RIGHT_CLICK];
                if(table == null)
                    return;

                // Array to hold the 3 types of listeners for the right click event. This array may have
                // null values if a listener does not exist in the listener table for the given event key
                BoneListener[] listeners = new BoneListener[3];

                // get the listener for specific Item and specific Form type
                listeners[0] = (BoneListener)table[EventTables.GetEventKey(ctxMenuInfo)];
                // get the generic listener for the Right Click Event on specified list of Form types
                listeners[1] = (BoneListener)table[EventTables.GetGenericEventKey(ctxMenuInfo, true)];
                // get the generic listener for the Right Click Event on any/all Form types
                listeners[2] = (BoneListener)table[EventTables.GetGenericEventKey(ctxMenuInfo, false)];

                foreach(BoneListener listener in listeners)
                {
                    if(listener != null) // check if listener exists
                    {
                        // handle the event
                        if(listener.Action.Action(listener.Method, ctxMenuInfo) == false)
                            bubbleEvent = false;
                    }
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: RightClickHandler raised\n"
                  + e.InnerException.Message);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the PrintEventInfo event 
        /// and calls all the listeners registered to the received EventType.
        /// </summary>
        /// <param name="pVal">PrintEventInfo information</param>
        /// <param name="bubbleEvent">Specifies whether or not the application will continue processing this event</param>
        public void PrintHandler(
          ref SAPbouiCOM.PrintEventInfo pVal,
          out bool bubbleEvent)
        {
            bubbleEvent = true;
            try
            {
                // get the event table
                Hashtable table = (Hashtable)listenersTable[BoEventTypes.et_PRINT];
                if(table == null)
                    return;

                // Array to hold the 3 types of listeners for the print event. This array may have
                // null values if a listener does not exist in the listener table for the given event key
                BoneListener[] listeners = new BoneListener[3];

                // get the listener for specific Item and specific Form type
                listeners[0] = (BoneListener)table[EventTables.GetEventKey(pVal)];
                // get the generic listener for the Print Event on specified list of Form types
                listeners[1] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, true)];
                // get the generic listener for the Print Event on any/all Form types
                listeners[2] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, false)];

                foreach(BoneListener listener in listeners)
                {
                    if(listener != null) // check if listener exists
                    {
                        // handle the event
                        if(listener.Action.Action(listener.Method, pVal) == false)
                            bubbleEvent = false;
                    }
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: PrintHandler raised\n"
                  + e.InnerException.Message);
            }
        }

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Handles the ReportDataInfo event 
        /// and calls all the listeners registered to the received EventType.
        /// </summary>
        /// <param name="pVal">ReportDataInfo information</param>
        /// <param name="bubbleEvent">Specifies whether or not the application will continue processing this event</param>
        public void ReportDataHandler(
          ref SAPbouiCOM.ReportDataInfo pVal,
          out bool bubbleEvent)
        {

            bubbleEvent = true;
            try
            {
                // get the event table
                Hashtable table = (Hashtable)listenersTable[BoEventTypes.et_PRINT_DATA];
                if(table == null)
                    return;

                // Array to hold the 3 types of listeners for the report data event. This array may have
                // null values if a listener does not exist in the listener table for the given event key
                BoneListener[] listeners = new BoneListener[3];

                // get the listener for specific Item and specific Form type
                listeners[0] = (BoneListener)table[EventTables.GetEventKey(pVal)];
                // get the generic listener for the Report Data Event on specified list of Form types
                listeners[1] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, true)];
                // get the generic listener for the Report Data Event on any/all Form types
                listeners[2] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, false)];

                foreach(BoneListener listener in listeners)
                {
                    if(listener != null) // check if listener exists
                    {
                        // handle the event
                        if(listener.Action.Action(listener.Method, pVal) == false)
                            bubbleEvent = false;
                    }
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: ReportDataHandler raised\n"
                  + e.InnerException.Message);
            }
        }
#endif

        /////////////////////////////////////////////////////////////////////////////////

#if V2005_SP01 || V2007 || V2007W1

        /// <summary>
        /// Handles the FormData event 
        /// and calls all the listeners registered to the received EventType.
        /// </summary>
        /// <param name="pVal">BusinessObjectInfo information</param>
        /// <param name="bubbleEvent">Specifies whether or not the application will continue processing this event</param>
        public void FormDataHandler(
          ref SAPbouiCOM.BusinessObjectInfo pVal,
          out bool bubbleEvent)
        {
            bubbleEvent = true;
            try
            {
                // get the event table
                Hashtable table = (Hashtable)listenersTable[pVal.EventType];
                if(table == null)
                    return;

                // Array to hold the 3 types of listeners for the form data event. This array may have
                // null values if a listener does not exist in the listener table for the given event key
                BoneListener[] listeners = new BoneListener[3];

                // get the listener for specific Item and specific Form type
                listeners[0] = (BoneListener)table[EventTables.GetEventKey(pVal)];
                // get the generic listener for the Form Data Event on specified list of Form types
                listeners[1] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, true)];
                // get the generic listener for the Form Data Event on any/all Form types
                listeners[2] = (BoneListener)table[EventTables.GetGenericEventKey(pVal, false)];

                foreach(BoneListener listener in listeners)
                {
                    if(listener != null) // check if listener exists
                    {
                        // handle the event
                        if(listener.Action.Action(listener.Method, pVal) == false)
                            bubbleEvent = false;
                    }
                }
            }
            catch(Exception e)
            {
                new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: FormDataHandler raised\n"
                  + e.InnerException.Message);
            }
        }

#endif

    }
}
