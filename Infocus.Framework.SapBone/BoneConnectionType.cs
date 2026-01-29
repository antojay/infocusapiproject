using System;


namespace Infocus.Framework.SapBone
{
    public enum BoneConnectionType
    {
        /// <summary>
        /// Only UI API connection, no DI API connection
        /// </summary>
        OnlyUI = 0,
        /// <summary>
        /// Single Sign On connection, UI API + DI API connections
        /// </summary>
        SSO = 1,
        /// <summary>
        /// Multiple AddOns connection, UI API + shared DI API connections. New in SAP Business One SDK 2007 version.
        /// </summary>
        MultipleAddOns = 2
    }
}
