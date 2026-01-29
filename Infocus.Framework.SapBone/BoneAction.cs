using System;
using System.Reflection;

namespace Infocus.Framework.SapBone
{
  /// <summary>
  /// Base class for the events management.
  /// <para>Each action represents an entity inside B1 (Item, Form, Menu for example) 
  /// and declares the listeneres this entity wants to handle. </para>
  /// </summary>
  public abstract class BoneAction
  {
    /// <summary>
    /// System.Type of the entity represented by this action.
    /// </summary>
    private Type type;
		
    /////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Builds an action given its type.
    /// </summary>
    protected BoneAction()
    {
      this.type = GetType();
    }

    /// <summary>
    /// Invokes the appropriate method handling each event received from B1.
    /// </summary>
    /// <param name="method">Name of the method to invoke.</param>
    /// <param name="pVal">Information of the event received.</param>
    /// <returns>Boolean value setting the BubbleEvent value.</returns>
    public bool Action(MethodInfo method, object pVal)
    {
      try	
      {
        if	(method.ReturnType.ToString().Equals("System.Boolean" ))
          return (bool)method.Invoke(this, new object[1] { pVal });
        else
          method.Invoke(this, new object[1] { pVal });
        return true;
      }

      catch (Exception e)
      {
        new BoneInfo(BoneConnectionContext.Application, "EXCEPTION: " + type.Name + "." + method.Name + " raised\n"
          + e.InnerException.Message);
        return true;
      }
    }

    /// <summary>
    /// Returns the key identifying an action.
    /// </summary>
    /// <param name="before">Boolean value specifying whether the action
    /// wants to handle the before or the after notification.</param>
    /// <returns>String identifying the action key.</returns>
    public abstract string GetKey(bool before);
  }
}