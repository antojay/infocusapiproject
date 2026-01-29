//****************************************************************************
//
//  File:      BoneListener.cs
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

namespace Infocus.Framework.SapBone
{
  /// <summary>
  /// Represents a listener to manage the Events.
  /// </summary>
  public class BoneListener
  {
    /// <summary>
    /// Action to represent.
    /// </summary>
    public BoneAction	Action;
    /// <summary>
    /// Name of the method representing the listener.
    /// </summary>
    public MethodInfo	Method;

    /////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Buils a new BoneListener.
    /// </summary>
    /// <param name="action">Action to represent</param>
    /// <param name="method">Name of the method associated</param>
    public BoneListener(BoneAction action, MethodInfo method)
    {
      this.Action = action;
      this.Method = method;
    }
  }
}
