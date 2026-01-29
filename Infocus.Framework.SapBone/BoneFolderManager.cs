//****************************************************************************
//
//  File:      B1FolderMgr.cs
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
using System.Threading;
using SAPbouiCOM;

namespace Infocus.Framework.SapBone
{
    /// <summary>
    /// Manages Folders and Add/Dell buttons for automaticaly created UDO Forms
    /// </summary>
    public sealed class BoneFolderManager
    {
        static private ItemEvent pVal;

        /////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Empty Constructor.
        /// </summary>
        private BoneFolderManager()
        {
        }

        /// <summary>
        /// Selects the first Folder of the form.
        /// </summary>
        private static void select()
        {
            try
            {
                // wait for the form to be loaded
                BoneConnectionContext.Application.GetLastBatchResults();

                // set the folder 
                Form form = BoneConnectionContext.Application.Forms.Item(pVal.FormUID);
                Item item = form.Items.Item("tab_0");
                Folder folder = (Folder)item.Specific;
                folder.Select();
            }

            catch
            {
                // ignore the exceptions
            }
        }

        /////////////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Selects the first folder of the form.
        /// </summary>
        /// <param name="pVal">ItemEvent.</param>
        public static void Init(ItemEvent pVal)
        {
            BoneFolderManager.pVal = pVal;
            Thread t = new Thread(new ThreadStart(select));
            t.Start();
        }

        /// <summary>
        /// Selects a PanelLevel into a form given an ItemEvent and the level to set.
        /// Called everytime a Folder is selected by the user on an automatic UDO form.
        /// </summary>
        /// <param name="pVal">ItemEvent containing information about the current form.</param>
        /// <param name="pane">PaneLevel to set.</param>
        public static void SetPane(ItemEvent pVal, int pane)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(pVal.FormUID);
            form.PaneLevel = pane;
        }

        /// <summary>
        /// Adds a new line to the matrix on the current PaneLevel.
        /// Called everytime the user clicks on the button Add of an automatic UDO form.
        /// </summary>
        /// <param name="pVal">ItemEvent containing the information about the current form.</param>
        public static void AddLine(ItemEvent pVal)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(pVal.FormUID);

            if(form.Mode != BoFormMode.fm_FIND_MODE)
            {
                form.Freeze(true);
                try
                {
                    // When only 1 matrix in UDO form => PaneLevel = 1 for mtx_0
                    Item item;
                    int nb = form.PaneLevel;
                    try
                    {
                        item = form.Items.Item("mtx_" + (form.PaneLevel));
                    }
                    catch(Exception)
                    {
                        item = form.Items.Item("mtx_" + (form.PaneLevel - 1));
                        nb = form.PaneLevel - 1;
                    }
                    Matrix mtx = (Matrix)item.Specific;

                    // clear the DB datasource
                    form.DataSources.DBDataSources.Item(nb + 1).Clear();

                    // add the line
                    int rowCount = mtx.RowCount;
                    mtx.AddRow(1, rowCount);
                    mtx.SelectRow(1 + rowCount, true, false);

                    if(form.Mode != BoFormMode.fm_ADD_MODE)
                        form.Mode = BoFormMode.fm_UPDATE_MODE;

                    mtx.FlushToDataSource();
                }
                finally
                {
                    form.Freeze(false);
                }
            }
        }

        /// <summary>
        /// Deletes the selected line from the matrix on the current PaneLevel.
        /// Called everytime the user clicks on the button Del of an automatic UDO form.
        /// </summary>
        /// <param name="pVal">ItemEvent containing the information about the current form.</param>
        public static void DelLine(ItemEvent pVal)
        {
            Form form = BoneConnectionContext.Application.Forms.Item(pVal.FormUID);

            if(form.Mode != BoFormMode.fm_FIND_MODE)
            {

                form.Freeze(true);
                try
                {
                    // When only 1 matrix in UDO form => PaneLevel = 1 for mtx_0
                    Item item;
                    int nb = form.PaneLevel;
                    try
                    {
                        item = form.Items.Item("mtx_" + (form.PaneLevel));
                    }
                    catch(Exception)
                    {
                        item = form.Items.Item("mtx_" + (form.PaneLevel - 1));
                        nb = form.PaneLevel - 1;
                    }
                    Matrix mtx = (Matrix)item.Specific;

                    int selRow = mtx.GetNextSelectedRow(0, BoOrderType.ot_SelectionOrder);
                    if(selRow == -1)
                        return;
                    mtx.DeleteRow(selRow);

                    if(form.Mode != BoFormMode.fm_ADD_MODE)
                        form.Mode = BoFormMode.fm_UPDATE_MODE;
                }
                finally
                {
                    form.Freeze(false);
                }
            }
        }
    }
}
