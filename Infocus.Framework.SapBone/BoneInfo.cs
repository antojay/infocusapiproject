//****************************************************************************
//
//  File:      B1Info.cs
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
using System.Xml;
using System.Threading;
using SAPbouiCOM;

namespace Infocus.Framework.SapBone
{
    /// <summary>
    /// Displays to the current user a message window inside the SAP Business One application.
    /// </summary>
    public class BoneInfo
    {
        /// <summary>
        /// Message to show to the user.
        /// </summary>
        private string msg;
        /// <summary>
        /// Current Application we are connected to.
        /// </summary>
        private Application theAppl;

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Shows a MessageBox in the Business One application.
        /// </summary>
        private void displayMsg()
        {
            theAppl.MessageBox(msg, -1, "", "", "");
        }

        /// <summary>
        /// Displays to the user a MessageBox inside the SAP Business One application.
        /// </summary>
        /// <param name="Application">SAPbouiCOM.Application we are connected to.</param>
        /// <param name="msg">Message to show to the user.</param>
        public BoneInfo(Application theAppl, string msg)
        {
            this.msg = msg;
            this.theAppl = theAppl;
            //
            // JBB 2/22/2016 - Commented Out the following code
            //
            //Thread t = new Thread(new ThreadStart(this.displayMsg));
            //t.Start();
            //
            // Added the following Code:
            displayMsg();
            //
            // JBB End 2/22/2016
        }
    }

    /// <summary>
    /// Searchs for the results of the last batch action asked and
    /// shows them to the user on a MessageBox in the SAP Business One application.
    /// </summary>
    public class BoneBatchInfo
    {
        /// <summary>
        /// Searchs for the results of the last batch action asked and
        /// shows them to the user on a MessageBox in the SAP Business One application.
        /// </summary>
        /// <param name="Application">SAPbouiCOM.Application we are connected to.</param>
        public BoneBatchInfo(Application theAppl)
        {
            string result = theAppl.GetLastBatchResults();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(result);

            /*
            <result>
              <errors>
                <error code=ERRORCODE descr=ERRORDESCR>
                </error>
              </errors>
            </result>
            */

            string errorPath = "result/errors/error";
            foreach(XmlNode errorNode in doc.SelectNodes(errorPath))
            {
                XmlElement errorElem = (XmlElement)errorNode;
                XmlAttribute errAttr = errorElem.Attributes["code"];
                if(errAttr != null)
                {
                    XmlAttribute descAttr = errorElem.Attributes["descr"];
                    new BoneInfo(theAppl, "ERROR " + errAttr.Value + " : " +
                      ((descAttr != null) ? descAttr.Value : ""));
                }
            }
        }
    }
}
