//****************************************************************************
//
//  File:      BoneListenerAttribute.cs
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
using SAPbouiCOM;

namespace Infocus.Framework.SapBone
{
  /// <summary>
  /// Defines a BoneListener by an EventType and the before boolean flag.
  /// </summary>
  [ AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false) ]
  public sealed class BoneListenerAttribute : System.Attribute 
  {
    /// <summary>
    /// Valid BoEventTypes value specifying the event type.
    /// </summary>
    private BoEventTypes	eventType;
    /// <summary>
    /// Boolean value specifying whether or not the application sent 
    /// the event before notification. 
    /// </summary>
    private bool			before;
    /// <summary>
    /// A string array containing a list of form types indicating that the addon is 
    /// interested in listening to the given event only on these form types.
    /// </summary>
    private string[] formTypes;

    /////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Builds a BoneListenerAttribute given an event type, the flag before and a list of form types.
    /// </summary>
    /// <param name="eventType">Valid BoEventTypes value specifying the event type</param>
    /// <param name="before">Boolean value specifying whether 
    /// or not the application sent the event before notification.</param>
    /// <param name="formTypes">A string array containing a list of form types indicating that the 
    /// addon is interested in listening to the given event only on these form types.</param>
    public BoneListenerAttribute(BoEventTypes eventType, bool before, string[] formTypes)
    {
      this.eventType = eventType;
      this.before = before;
      this.formTypes = formTypes;
    }

    /// <summary>
    /// Builds a BoneListenerAttribute given an event type and the flag before.
    /// </summary>
    /// <param name="eventType">Valid BoEventTypes value specifying the event type</param>
    /// <param name="before">Boolean value specifying whether 
    /// or not the application sent the event before notification.</param>
    public BoneListenerAttribute( BoEventTypes eventType, bool before)
    {
      this.eventType = eventType;
      this.before = before;
    }

    /// <summary>
    /// Builds a BoneListenerAttribute given an event type. 
    /// The before flag is set to false by default.
    /// </summary>
    /// <param name="eventType">Valid BoEventTypes value specifying the event type</param>
    public BoneListenerAttribute( BoEventTypes eventType)
    {
      this.eventType = eventType;
      this.before = false;
    }

    /// <summary>
    /// Returns the event type of this BoneListenerAttribute.
    /// </summary>
    /// <returns>Valid BoEventTypes value specifying the event type.</returns>
    public BoEventTypes GetEventType()
    {
      return eventType;
    }

    /// <summary>
    /// Returns a boolean value specifying whether this BoneListenerAttribute
    /// is associated to the before or the after notification.
    /// </summary>
    /// <returns>Boolean value specifying whether this BoneListenerAttribute
    /// is associated to the before or the after notification.</returns>
    public bool GetBefore()
    {
      return before;
    }

    /// <summary>
    /// Returns the list of form types of this BoneListenerAttribute.
    /// </summary>
    /// <returns>A string array containing a list of form types indicating that the
    /// addon is interested in listening to the given event only on these form types.</returns>
    public string[] GetFormTypes()
    {
      return formTypes;
    }

    /// <summary>
    /// Returns the list of action keys for the generic event handling of this BoneListenerAttribute.
    /// </summary>
    /// <returns>A string array of action keys for generic event handling. 
    /// These keys are not specific to any item and are applicable either to all form 
    /// types or a specific form (from the list of specified form types).</returns>
    public string[] GetEventActionKeys(string beforeFlag)
    {
      string[] keys;
      if (this.GetEventType().Equals(BoEventTypes.et_MENU_CLICK))
      {
        keys = new string[1];
        keys[0] = "*." + beforeFlag;
      }
      else
      {
        string[] forms = this.GetFormTypes();
        if (forms == null)
        {
          // for generic case when there are no forms specified
          keys = new string[1];
          keys[0] = "*.*." + beforeFlag;
        }
        else
        {
          // when a list of form types are specified
          int i = 0;
          keys = new string[forms.Length];
          foreach (string formStr in forms)
          {
            keys[i++] = formStr + ".*." + beforeFlag;
          }
        }
      }
      return keys;
    }
  }
}