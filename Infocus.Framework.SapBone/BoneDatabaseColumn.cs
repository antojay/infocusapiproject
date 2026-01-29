//****************************************************************************
//
//  File:      B1DbColumn.cs
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
using System.CodeDom;
using SAPbobsCOM;

namespace Infocus.Framework.SapBone
{
    /*
    enum BoFieldTypes {
      db_Alpha = 0,
      db_Memo = 1,
      db_Numeric = 2,
      db_Date = 3,
      db_Float = 4
      };
    */

    /// <summary>
    /// Manages the B1 SDK metadata object UserFieldsMD.
    /// </summary>
    /// <remarks>
    /// This class will be used by the class managing the Database (class inheriting 
    /// from B1Db base class).
    /// </remarks>
    public class BoneDatabaseColumn
    {
        /// <summary>
        /// Field name.
        /// </summary>
        public string Name;				/// fields.item[0]

        /// <summary>
        /// Size of the field.
        /// </summary>
        public int Size;				/// fields.item[1]

        /// <summary>
        /// Data type, which describes the nature of the data, of the specified field.
        /// </summary>
        public BoFieldTypes Type;				/// fields.item[2] 

        /// <summary>
        /// Data subtype, which describes the nature of the data, of the specified field type.
        /// </summary>
        public BoFldSubTypes SubType;				/// fields.item[3] 

        /// <summary>
        /// Specifies whether or not this field can have a null value.
        /// </summary>
        public bool IsNullable;			/// fields.item[4]

        /// <summary>
        /// Boolean indicating if the UserField has valid values.
        /// </summary>
        public bool HasValidValues;		/// fields.item[5]

        /// <summary>
        /// Linked UserTable name, so that the user field will be used as a foreign key in the TableName.
        /// </summary>
        public string LinkedTo;			/// fields.item[6]

        /// <summary>
        /// Description of the field.
        /// </summary>
        public string Description;		/// fields.item[7]

        /// <summary>
        /// Name of the parent table that this field refers to.
        /// </summary>
        public string Table;

        public Boolean CreateIndex;

        /// <summary>
        /// List of valid values for the specified user defined field.
        /// </summary>
        public BoneDatabaseValidValue[] ValidValues = new BoneDatabaseValidValue[0];

        /// <summary>
        /// Default value of the field.
        /// </summary>
        public int DefaultValue = -1;

        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Builds a B1DbColumn from a Fields collection.
        /// </summary>
        /// <param name="fields">Collection of Field objects.</param>
        public BoneDatabaseColumn(
          Fields fields)
        {
            this.Name = (string)fields.Item(0).Value;
            this.Size = (int)fields.Item(1).Value;
            this.Type = (BoFieldTypes)fields.Item(2).Value;
            //Not given by SBObob.GetTableFieldList()  this.SubType			= (BoFldSubTypes)fields.Item(3).Value;
            this.IsNullable = (((string)fields.Item(3).Value).Equals("0")) ? false : true;
            this.HasValidValues = ((int)fields.Item(4).Value == 0) ? false : true;
            this.LinkedTo = (string)fields.Item(5).Value;
            this.Description = (string)fields.Item(6).Value;
        }

        /// <summary>
        /// Builds a B1DbColumn from a Fields collection.
        /// </summary>
        /// <param name="field">Collection of Field objects.</param>
        public BoneDatabaseColumn(
          UserFieldsMD field)
        {
            this.Table = field.TableName;
            this.Name = field.Name;
            this.Size = field.Size;
            this.Type = field.Type;
            this.SubType = field.SubType;
            this.IsNullable = (field.Mandatory == BoYesNoEnum.tYES ? false : true);
            // Count = 0 => Value and Description empty
            if(field.ValidValues.Count == 0)
            {
                this.HasValidValues = false;
            }
            else
            {
                field.ValidValues.SetCurrentLine(0);
                this.HasValidValues = (field.ValidValues.Value == "" ? false : true);
            }
            this.LinkedTo = field.LinkedTable;
            this.Description = field.Description;
        }

        /// <summary>
        /// Empty Constructor
        /// </summary>
        public BoneDatabaseColumn()
        {
        }

        /// <summary>
        /// Builds a B1DbColumn from its main members information.
        /// </summary>
        /// <param name="table">Name of the parent table that this field refers to.</param>
        /// <param name="name">Field name.</param>
        /// <param name="description">Description of the field.</param>
        /// <param name="type">Data type, which describes the nature of the data, of the specified field.</param>
        /// <param name="subtype">Data subtype, which describes the nature of the type, of the specified field.</param>
        /// <param name="size">Size of the field.</param>
        /// <param name="validValues">List of valid values for the specified user defined field.</param>
        /// <param name="defaultValue">Default value of the field.</param>
        public BoneDatabaseColumn(
          string table,
          string name,
          string description,
          BoFieldTypes type,
          int size,
          BoneDatabaseValidValue[] validValues,
          int defaultValue)
            : this(table, name, description, type,
                BoFldSubTypes.st_None, size, validValues, defaultValue)
        {
        }

        public BoneDatabaseColumn(
          string table,
          string name,
          string description,
          BoFieldTypes type,
          BoFldSubTypes subtype,
          int size,
          BoneDatabaseValidValue[] validValues,
          int defaultValue, Boolean isNullable = true, Boolean createIndex = false)
        {
            this.Table = table;
            this.Name = name;
            this.Description = description;
            this.Type = type;
            this.SubType = subtype;
            this.Size = size;
            this.ValidValues = validValues;
            this.DefaultValue = defaultValue;
            IsNullable = isNullable;
            CreateIndex = createIndex;
        }

        /// <summary>
        /// Adds the UserFieldMD to the current company Database.
        /// </summary>
        /// <param name="company">SAPbobsCOM.Company we are connected to.</param>
        /// <returns>Return value from the SDK action UserFieldsMD.Add().</returns>
        public int Add(Company company)
        {
            int ret = 0;
            Recordset rs = company.GetBusinessObject(BoObjectTypes.BoRecordset) as Recordset;

            try
            {
                rs.DoQuery(String.Format("SELECT TOP 1 AliasID from CUFD With(NOLOCK) where tableid = '{0}' and AliasID = '{1}'", Table, Name));
                rs.MoveFirst();
                if(!rs.EoF)
                {
                    return -77;
                }
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(rs);
                rs = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            UserFieldsMD userFields = (UserFieldsMD)
              company.GetBusinessObject(BoObjectTypes.oUserFields);
            try
            {
                userFields.TableName = Table;
                userFields.Name = Name;
                userFields.Description = Description;
                userFields.Type = this.Type;
                userFields.SubType = this.SubType;
                userFields.Mandatory = IsNullable ? BoYesNoEnum.tNO : BoYesNoEnum.tYES;
                userFields.LinkedTable = LinkedTo;
                if(Size != 0)
                {
                    userFields.EditSize = Size;
                }
                //// valid values
                if(ValidValues != null && ValidValues.Length > 0)
                {

                    foreach(BoneDatabaseValidValue vval in ValidValues)
                    {
                        userFields.ValidValues.Value = vval.Val;
                        userFields.ValidValues.Description = vval.Description;
                        userFields.ValidValues.Add();
                    }
                }
                //// default value
                if(DefaultValue != -1)
                {
                    if(ValidValues != null && ValidValues.Length > 0)
                    {
                        userFields.DefaultValue = ValidValues[DefaultValue].Val;
                    }
                    else
                    {
                        userFields.DefaultValue = DefaultValue.ToString();
                    }
                }
                ret = userFields.Add();

#if	DEBUG
                if(ret != 0)
                {
                    int errcode;
                    string errmsg;
                    company.GetLastError(out errcode, out errmsg);
                    Console.Out.WriteLine(String.Format("Field {0} : {1}", Name, errmsg));
                }
#endif
            }
            finally
            {
                // clean DI object
                System.Runtime.InteropServices.Marshal.ReleaseComObject(userFields);
                userFields = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
            return ret;
        }

        /// <summary>
        /// Generates the code to add a new UserField. 
        /// <para>This code is added in your AddOn_Db class inheriting from B1Db.</para>
        /// </summary>
        /// <returns>CodeExpression containing the UserField information.</returns>
        public CodeExpression GenerateCtor()
        {
            /*
              new B1DbColumn(table,name,description,type,subtype,size,validValues,defaultValue);
            */

            int i = 0;
            CodeExpression[] validValuesArray = new CodeExpression[ValidValues.Length];
            foreach(BoneDatabaseValidValue vval in ValidValues)
            {
                validValuesArray[i++] = new CodeObjectCreateExpression(
                    "Infocus.Framework.SapBone.BoneDatabaseValidValue",
                    new CodeExpression[2] 
                    {
                        new CodePrimitiveExpression(vval.Val),
                        new CodePrimitiveExpression(vval.Description)
                    });
            }
            CodeArrayCreateExpression createValidValuesArray = new CodeArrayCreateExpression(
              "Infocus.Framework.SapBone.BoneDatabaseValidValue", validValuesArray);

            return new CodeObjectCreateExpression(
              "BoneDatabaseColumn",
              new CodeExpression[8] {
                                new CodePrimitiveExpression( Table ),
                                new CodePrimitiveExpression( Name ),
                                new CodePrimitiveExpression( Description ),
                                new CodeFieldReferenceExpression(
                                  new CodeTypeReferenceExpression("BoFieldTypes"),Type.ToString()),
                                new CodeFieldReferenceExpression(
                                  new CodeTypeReferenceExpression("BoFldSubTypes"),SubType.ToString()),
                                new CodePrimitiveExpression( Size ),
                                createValidValuesArray,
                                new CodePrimitiveExpression( DefaultValue )});
        }

        /////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Determines whether the specified B1DbColumn is equal to the current B1DbColumn.     
        /// </summary>
        /// <param name="obj">B1DbColumn to compare.</param>
        /// <returns>true if both objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if(obj is BoneDatabaseColumn)
            {
                BoneDatabaseColumn column = (BoneDatabaseColumn)obj;
                return (column.Table.Equals(Table)) && (column.Name.Equals(Name));
            }

            return base.Equals(obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type, suitable 
        /// for use in hashing algorithms and data structures like a hash table. 
        /// </summary>
        /// <returns>A hash code for the current B1DbColumn.</returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

    }
}
