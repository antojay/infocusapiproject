//****************************************************************************
//
//  File:      B1UDOService.cs
//
//  Copyright (c) SAP 
//
// THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
//
//****************************************************************************

#if V2007W1
using System;
using System.Collections.Generic;
using System.Text;
using SAPbobsCOM;
using System.CodeDom;

namespace Infocus.Framework.SapBone
{
    /// <summary>
    /// Manages the B1 SDK DI General Service object for a chosen UDO
    /// </summary>
    /// <remarks>
    /// This class will be used In order to generate a new class in the generated add-on
    /// The generated class will use the General Service DI object for approaching to the chosen UDO tables
    /// </remarks>
    public class B1UDOService
    {
        public string sClassName;
        public string sUdoUniqueID;
        protected string[] ChildTableNames;

        protected BaseChild[] childObjs; 

        protected SAPbobsCOM.BoUDOObjType UDOType;  //Documents or Master Data

        protected SAPbobsCOM.GeneralService oUdoService;
        protected SAPbobsCOM.GeneralData oUdoHeaderGeneralData;
        protected SAPbobsCOM.GeneralDataParams oUdoHeaderKey;
                
        
        public B1UDOService()
        {
        }

        /// <summary>
        /// verifies that the FileName or the UDO are not generated twice
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (obj is B1UDOService)
            {
                B1UDOService oUDOClass = obj as B1UDOService;
                return ((oUDOClass.sClassName.Equals(sClassName)) || (oUDOClass.sUdoUniqueID.Equals(sUdoUniqueID)));                
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Initializes the object
        /// </summary>
       protected void Init()
       {
            this.oUdoService = (SAPbobsCOM.GeneralService)BoneConnectionContext.Company.GetCompanyService().GetGeneralService(sUdoUniqueID);
            // Point to the Header GeneralData of the MD UDO
            this.oUdoHeaderGeneralData = (SAPbobsCOM.GeneralData)oUdoService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralData);
            // Init ChildObjs collection
            childObjs = new BaseChild[ChildTableNames.Length];
        }

                
        //-------------------------------------------------//
        //                     Methods                     //
        //-------------------------------------------------//

        /// <summary>
        /// Adds object record to the database tables of the current UDO (Header and Lines)
        /// </summary>
        public void Add()
        {
            oUdoService.Add(oUdoHeaderGeneralData);
        }

        /// <summary>
        /// Gets an object record from the database table of the current UDO including lines
        /// </summary>
        /// <param name="KeyVal"></param>
        public void GetByKey(string sKeyVal)
        {
            this.oUdoHeaderKey = (SAPbobsCOM.GeneralDataParams)this.oUdoService.GetDataInterface(SAPbobsCOM.GeneralServiceDataInterfaces.gsGeneralDataParams);
            // UDO of Master Data type
            if (UDOType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
            {
                this.oUdoHeaderKey.SetProperty("Code", sKeyVal);
            }
            else   //UDO of Document type
            {
                this.oUdoHeaderKey.SetProperty("DocEntry", sKeyVal);
            }
            this.oUdoHeaderGeneralData = this.oUdoService.GetByParams(this.oUdoHeaderKey);

            // Obtain child info
            for (int nbChild = 0; nbChild < childObjs.Length; nbChild++)
            {
                childObjs[nbChild].oUdoLinesCollection = (SAPbobsCOM.GeneralDataCollection)oUdoHeaderGeneralData.Child(ChildTableNames[nbChild]);
                // Have the child object pointing as default to the first line
                childObjs[nbChild].SetCurrentLine(0);
            }
        }

        /// <summary>
        /// Update an existing record in the database table of the current UDO
        /// </summary>
        public void Update()
        {
            this.oUdoService.Update(this.oUdoHeaderGeneralData);
        }

        /// <summary>
        /// Deletes a record in the database table of the current UDO (including Header and Lines)
        /// </summary>
        public void Delete()
        {
            this.oUdoService.Delete(oUdoHeaderKey);
        }

        /// <summary>
        /// Relevant for document UDO type only!
        /// Cancels a record in the database table of the current UDO (including relevant lines)
        /// </summary>
        public void Cancel()
        {
            if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
            {
                this.oUdoService.Cancel(oUdoHeaderKey);
            }
            else
            {
                throw new Exception("NotRelevantMethod");
            }

        }

        /// <summary>
        /// Relevant for document UDO type only! 
        /// Closes a record in the tadabase table of the current UDO
        /// </summary>
        public void Close()
        {
            if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document )
            {
                this.oUdoService.Close(oUdoHeaderKey);
            }
            else
            {
                throw new Exception("NotRelevantMethod");
            }
        }

        /// <summary>
        /// Invokes a method implemented in the implementation.dll 
        /// </summary>
        /// <param name="oInvokeInput"></param>
        /// <param name="oGeneralData"></param>
        /// <returns></returns>
        public SAPbobsCOM.InvokeParams Invoke(SAPbobsCOM.InvokeParams oInvokeInput, SAPbobsCOM.GeneralData oGeneralData)
        {
            return this.oUdoService.InvokeMethod(oInvokeInput, oGeneralData);
        }

        /// <summary>
        /// Return the keys for all the rows in the main table for a specific UDO (Header)
        /// </summary>
        /// <returns>GeneralCollectionParams with a list of header record keys</returns>
        public SAPbobsCOM.GeneralCollectionParams GetList()
        {
            return this.oUdoService.GetList();
        }

        

        //-------------------------------------------------//
        //                   XML handling                  //
        //-------------------------------------------------//

        /// <summary>
        /// Imports data from an XML file to the UDO object
        /// </summary>
        /// <param name="sFileName">Specifies the path and file name of the XML data</param> 
        public void FromXMLFile (String sFileName)
        {
            this.oUdoHeaderGeneralData.FromXMLFile(sFileName);
            //this.oUdoService.GetDataInterfaceFromXMLFile(sFileName);
        }

        /// <summary>
        /// Imports data from an XML string to the UDO object
        /// </summary>
        /// <param name="sXMLSring">Specified the XML data</param> 
        public void FromXMLString(string sXMLSring)
        {
            this.oUdoHeaderGeneralData.FromXMLString(sXMLSring);
            //this.oUdoService.GetDataInterfaceFromXMLString(sXMLSring);
        }

        /// <summary>
        /// Retrieves the XML Schema of the data structure
        /// </summary>
        /// <returns>XML schema as a string</returns>
        public string GetXMLSchema()
        {
            return this.oUdoHeaderGeneralData.GetXMLSchema();
        }

        /// <summary>
        /// Creates an XML file that represents the 0bject data
        /// </summary>
        /// <param name="sFileName">Specifies the XML file name including path</param>
        public void ToXMLFile(string sFileName)
        {
            this.oUdoHeaderGeneralData.ToXMLFile(sFileName);
        }

        /// <summary>
        /// Creates an XML string that represents the 0bject data
        /// </summary>
        /// <returns>XML data string</returns>
        public string ToXMLString()
        {
            return this.oUdoHeaderGeneralData.ToXMLString();
        }

        //-------------------------------------------------//
        //             Header Properties                   //
        //-------------------------------------------------//

        /// <summary>
        /// Returns or sets the UDO entry key that uniquely identifies the UDO object
        /// Field Name: Code
        /// Relevant only to MasterData type UDO
        /// </summary>
        public string Code
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("Code")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
            set
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
                {
                    this.oUdoHeaderGeneralData.SetProperty("Code", value.ToString());
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Sets or returns the Name field
        /// Valid only to MasterData type UDO
        /// </summary>
        public string Name
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("Name")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
            set
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
                {
                    this.oUdoHeaderGeneralData.SetProperty("Name", value.ToString());
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Returns the DocEntry field
        /// Field Name: DocEntry
        /// </summary>
        public string DocEntry
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("DocEntry")).ToString();
            }
        }

        /// <summary>
        /// Returns the Canceled field
        /// </summary>
        public string Canceled
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("Canceled")).ToString();
            }
        }

        /// <summary>
        /// Returns the Object field
        /// </summary>
        public string Object
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("Object")).ToString();
            }
        }

        /// <summary>
        /// Returns the LogInst field
        /// </summary>
        public string LogInst
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("LogInst")).ToString();
            }
        }

        /// <summary>
        /// Returns the UserSign field
        /// </summary>
        public string UserSign
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("UserSign")).ToString();
            }
        }

        /// <summary>
        /// Returns the Transferred field
        /// </summary>
        public string Transferred
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("Transferred")).ToString();
            }
        }

        /// <summary>
        /// Returns the CreateData field
        /// </summary>
        public string CreateDate
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("CreateDate")).ToString();
            }
        }

        /// <summary>
        /// Returns the CreateTime field
        /// </summary>
        public string CreateTime
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("CreateTime")).ToString();
            }
        }

        /// <summary>
        /// Returns the UpdateDate field
        /// </summary>
        public string UpdateDate
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("UpdateDate")).ToString();
            }
        }

        /// <summary>
        /// Returns the UpdateTime field
        /// </summary>
        public string UpdateTime
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("UpdateTime")).ToString();
            }
        }

        /// <summary>
        /// Returns the DataSource field
        /// </summary>
        public string DataSource
        {
            get
            {
                return (this.oUdoHeaderGeneralData.GetProperty("DataSource")).ToString();
            }
        }

        /// <summary>
        /// Returns or sets the DocNum field
        /// Relevant only to Document UDO type
        /// </summary>
        public string DocNum
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("DocNum")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
            set
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    this.oUdoHeaderGeneralData.SetProperty("DocNum", value.ToString());
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Returns or sets the Period field
        /// Relevant only to Document type UDO
        /// </summary>
        public string Period
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("Period")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
            set
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    this.oUdoHeaderGeneralData.SetProperty("Period", value.ToString());
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Returns the Instance field
        /// Relevant only to Document Type UDO
        /// </summary>
        public string Instance
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("Instance")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Returns or sets the Series field
        /// Relevant only to Document Type UDO
        /// </summary>
        public string Series
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("Series")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
            set
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    this.oUdoHeaderGeneralData.SetProperty("Series", value.ToString());
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Returns or sets the HandWrtten field
        /// Relevant only to Document type UDO
        /// </summary>
        public string HandWrtten
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("HandWrtten")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
            set
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    this.oUdoHeaderGeneralData.SetProperty("HandWrtten", value.ToString());
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        /// <summary>
        /// Returns the Status field
        /// Relevant only to Document type UDO
        /// </summary>
        public string Status
        {
            get
            {
                if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                {
                    return (this.oUdoHeaderGeneralData.GetProperty("Status")).ToString();
                }
                else
                {
                    throw new Exception("NotRelevantProperty");
                }
            }
        }

        //-----------------------------------------------------//
        // Nested Child Class for handling the UDO Line tables //
        //-----------------------------------------------------//
        public class BaseChild
        {
            public SAPbobsCOM.GeneralDataCollection oUdoLinesCollection; 
            protected SAPbobsCOM.GeneralData oUdoLineGeneralData; 
            protected SAPbobsCOM.BoUDOObjType UDOType;

            protected BaseChild(String ChildTableName, B1UDOService udoService)
            {
                oUdoLinesCollection = (SAPbobsCOM.GeneralDataCollection)udoService.oUdoHeaderGeneralData.Child(ChildTableName);
                UDOType = udoService.UDOType;
            }

            //-------------------------------------------------//
            //                Child Methods                    //
            //-------------------------------------------------//

            /// <summary>
            /// Adds an empty line to the child object
            /// </summary>
            public void AddLine()
            {
                this.oUdoLineGeneralData = this.oUdoLinesCollection.Add();
            }

            /// <summary>
            /// Sets the relevant line at the speified index to the child object
            /// </summary>
            /// <param name="Index">The index of the relevant line</param>
            public void SetCurrentLine(int Index)
            {
                this.oUdoLineGeneralData = this.oUdoLinesCollection.Item(Index);
            }

            /// <summary>
            /// Deletes the relevant line at the specified index of the child object
            /// </summary>
            /// <param name="Index"></param>
            public void DeleteLine(int Index)
            {
                this.oUdoLinesCollection.Remove(Index);
            }

            //-------------------------------------------------//
            //                Child Properties                 //
            //-------------------------------------------------//
            /// <summary>
            /// Returns the number of records in the child table
            /// </summary>
            public int Count
            {
                get
                {
                    return (this.oUdoLinesCollection.Count);
                }
            }

            /// <summary>
            /// Returns the Code field of the Header record it is related to
            /// Relevant only to Master Data type UDO
            /// </summary>
            public string Code
            {
                get
                {
                    if (UDOType == SAPbobsCOM.BoUDOObjType.boud_MasterData)
                    {
                        return (this.oUdoLineGeneralData.GetProperty("Code")).ToString();
                    }
                    else
                    {
                        throw new Exception("NotRelevantProperty");
                    }
                }
                
            }

            /// <summary>
            /// Returns the LineId field
            /// </summary>
            public string LineId
            {
                get
                {
                    return (this.oUdoLineGeneralData.GetProperty("LineId")).ToString();
                }
            }

            /// <summary>
            /// Returns the Object field
            /// </summary>
            public string Object
            {
                get
                {
                    return (this.oUdoLineGeneralData.GetProperty("Object")).ToString();
                }
            }

            /// <summary>
            /// Returns the LogInst field
            /// </summary>
            public string LogInst
            {
                get
                {
                    return (this.oUdoLineGeneralData.GetProperty("LogInst")).ToString();
                }
            }

            /// <summary>
            /// Returns the DocEntry field
            /// Relevant only to Document type UDO
            /// </summary>
            public string DocEntry
            {
                get
                {
                    if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                    {
                        return (this.oUdoLineGeneralData.GetProperty("DocEntry")).ToString();
                    }
                    else
                    {
                        throw new Exception("NotRelevantProperty");
                    }
                }
            }

            /// <summary>
            /// Returns or sets the VisOrder field
            /// Relevant only to Document type UDO
            /// </summary>
            public string VisOrder
            {
                get
                {
                    if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                    {
                        return (this.oUdoLineGeneralData.GetProperty("VisOrder")).ToString();
                    }
                    else
                    {
                        throw new Exception("NotRelevantProperty");
                    }
                }
                set
                {
                    if (UDOType == SAPbobsCOM.BoUDOObjType.boud_Document)
                    {
                        this.oUdoLineGeneralData.SetProperty("VisOrder", value.ToString());
                    }
                    else
                    {
                        throw new Exception("NotRelevantProperty");
                    }
                }
            }
        }
    }
}

#endif