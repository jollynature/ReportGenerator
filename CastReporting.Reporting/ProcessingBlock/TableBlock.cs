﻿/*
 *   Copyright (c) 2016 CAST
 *
 * Licensed under a custom license, Version 1.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License, accessible in the main project
 * source code: Empowerment.
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Cast.Util.Log;
using CastReporting.Reporting.Atrributes;
using CastReporting.Reporting.Helper;
using CastReporting.Reporting.ReportingModel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using OXD = DocumentFormat.OpenXml.Drawing;
using OXP = DocumentFormat.OpenXml.Presentation;
using OXW = DocumentFormat.OpenXml.Wordprocessing;

namespace CastReporting.Reporting.Builder.BlockProcessing
{
    [BlockType("TABLE")]
    public abstract class TableBlock
    {
        #region ABSTRACTS - To be implemented by Inherited children
        protected abstract TableDefinition Content(ReportData client, Dictionary<string, string> options);
        #endregion ABSTRACTS - To be implemented by Inherited children

        #region PROPERTIES
        /// <summary>
        /// Block Type Name
        /// </summary>
        public static string BlockTypeName => "TABLE";

        #endregion PROPERTIES

        #region METHODS

        public static bool IsMatching(string blockType)
        {
            return (BlockTypeName.Equals(blockType));
        }

        public TableDefinition GetContent(ReportData client, Dictionary<string, string> options)
        {
            return Content(client, options);
        }
        public static TableDefinition GetContent(string blockName, ReportData client, Dictionary<string, string> options)
        {
            TableBlock instance = BlockHelper.GetAssociatedBlockInstance<TableBlock>(blockName);
            return instance?.Content(client, options);
        }
        public static void BuildContent(ReportData client, OpenXmlPartContainer container, BlockItem block, string blockName, Dictionary<string, string> options)
        {
            TableBlock instance = BlockHelper.GetAssociatedBlockInstance<TableBlock>(blockName);
            if (null != instance)
            {
                LogHelper.Instance.LogDebugFormat("Start TableBlock generation : Type {0}", blockName);
                Stopwatch treatmentWatch = Stopwatch.StartNew();
                TableDefinition content = instance.Content(client, options);
                if (null != content)
                {
                    ApplyContent(client, container, block, content, options);
                }
                treatmentWatch.Stop();
                LogHelper.Instance.LogDebugFormat
                    ("End TableBlock generation ({0}) in {1} millisecond{2}"
                    , blockName
                    , treatmentWatch.ElapsedMilliseconds.ToString()
                    , treatmentWatch.ElapsedMilliseconds > 1 ? "s" : string.Empty
                    );
            }
        }
        public static void ApplyContent(ReportData client, OpenXmlPartContainer container, BlockItem block, TableDefinition content, Dictionary<string, string> options)
        {
            var contentblock = GetTableContentBlock(client, block);
            if (null != contentblock)
            {
                UpdateBlock(client, container, contentblock, content, options);
            }
        }

        /// <summary>
        /// Format the display of percent value into 3 digits if we can
        /// <para>Example : "3.65 %" or "10.4 %" or "243 %" or "10 052 %"</para>
        /// </summary>
        /// <param name="pValue">Numeric value to display</param>
        /// <param name="pWidthPostiveSign"></param>
        /// <returns>Displayed text</returns>
        protected static string FormatPercent(double? pValue, bool pWidthPostiveSign)
        {
            return pValue.FormatPercent(pWidthPostiveSign);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pValue"></param>
        /// <returns></returns>
        protected static string FormatPercent(double? pValue)
        {
            return pValue.FormatPercent(true);
        }

        /// <summary>
        /// Format the display of evolution value into 3 digits if we can
        /// <para>Example : "3.65 %" or "10.4 %" or "243 %" or "10 052 %"</para>
        /// </summary>
        /// <param name="pValue">Numeric value to display</param>
        /// <returns>Displayed text</returns>
        protected static string FormatEvolution(long pValue)
        {
            return pValue.FormatEvolution();
        }

        /// <summary>
        /// Format the display of evolution value into 3 digits if we can
        /// <para>Example : "3.65 %" or "10.4 %" or "243 %" or "10 052 %"</para>
        /// </summary>
        /// <param name="pValue">Numeric value to display</param>
        /// <returns>Displayed text</returns>
        protected static string FormatEvolution(decimal pValue)
        {
            return pValue.FormatEvolution();
        }
        /// <summary>
        /// Format the display of evolution value into 3 digits if we can
        /// <para>Example : "3.65" or "10.4" or "243" or "10 052"</para>
        /// </summary>
        /// <param name="pValue">Numeric value to display</param>
        /// <returns>Displayed text</returns>
        protected static string FormatEvolution(double? pValue)
        {
            return pValue.FormatEvolution();
        }

        private static void UpdateBlock(ReportData client, OpenXmlPartContainer container, OpenXmlElement block, TableDefinition content, Dictionary<string, string> options)
        {
            switch (client.ReportType)
            {
                case FormatType.Word: { UpdateWordBlock(client, container, block, content, options); } break;
                case FormatType.PowerPoint: { UpdatePowerPointBlock(client, container, block, content, options); } break;
                case FormatType.Excel: { UpdateExcelBlock(client, container, block, content, options); } break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private static OpenXmlElement GetTableContentBlock(ReportData client, BlockItem block)
        {
            switch (client.ReportType)
            {
                case FormatType.Word:
                    var tblContent = block.OxpBlock.Descendants<OXW.Table>().FirstOrDefault();
                    return tblContent ?? block.OxpBlock;
                case FormatType.PowerPoint:
                    return block.OxpBlock;
                case FormatType.Excel:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            throw new NotImplementedException();
        }

        #region Word methods
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void UpdateWordBlock(ReportData client, OpenXmlPartContainer container, OpenXmlElement block, TableDefinition content, Dictionary<string, string> options)
        {
            if (null != content && block is OXW.Table)
            {
                OXW.Table table = ((OXW.Table)block).CloneNode(true) as OXW.Table;

                OXW.TableRow headerRowTemplate = table?.Descendants<OXW.TableRow>().First().CloneNode(true) as OXW.TableRow;
                OXW.TableRow contentRowTemplate = table?.Descendants<OXW.TableRow>().Skip(1).First().CloneNode(true) as OXW.TableRow;

                #region Column number management
                OXW.TableGrid tablegrid = table?.Descendants<OXW.TableGrid>().FirstOrDefault();
                List<OXW.GridColumn> columns = tablegrid?.Descendants<OXW.GridColumn>().ToList();
                if (columns != null && content.NbColumns != columns.Count)
                {
                    if (content.NbColumns < columns.Count)
                    {
                        for (int i = columns.Count - 1, lim = content.NbColumns - 1; i > lim; i--)
                        {
                            tablegrid.RemoveChild(columns[i]);
                        }
                    }
                    else
                    {
                        for (int i = 0, lim = content.NbColumns - columns.Count; i < lim; i++)
                        {
                            tablegrid.AppendChild(new OXW.GridColumn() { Width = "1000" });
                        }
                    }
                }

                #endregion Column number management

                ModifyWordRowTextContent(headerRowTemplate, string.Empty);
                ModifyWordRowTextContent(contentRowTemplate, string.Empty);

                int idx = 0;
                int nbrow = 0;
                List<OXW.TableCell> headerCells = headerRowTemplate?.Descendants<OXW.TableCell>().Select(_ => _.CloneNode(true) as OXW.TableCell).ToList();
                List<OXW.TableCell> contentCells = contentRowTemplate?.Descendants<OXW.TableCell>().Select(_ => _.CloneNode(true) as OXW.TableCell).ToList();
                headerRowTemplate?.RemoveAllChildren<OXW.TableCell>();
                OXW.TableRow row = headerRowTemplate;
                if (headerCells != null)
                {
                    int headerCellsCount = headerCells.Count;
                    int contentCellsCount = headerCells.Count;

                    table.RemoveAllChildren<OXW.TableRow>();
                    foreach (var item in content.Data)
                    {
                        if (null != item)
                        {
                            OXW.TableCell cell;
                            if (content.HasColumnHeaders && 0 == nbrow)
                            {
                                cell = headerCells[idx % headerCellsCount].CloneNode(true) as OXW.TableCell;
                            }
                            else
                            {
                                cell = contentCells?[idx % contentCellsCount].CloneNode(true) as OXW.TableCell;
                            }
                            ModifyWordCellTextContent(cell, item);
                            // ReSharper disable once PossiblyMistakenUseOfParamsMethod
                            row?.Append(cell);
                        }

                        idx = ++idx % content.NbColumns;
                        if (0 != idx) continue;
                        if (null != row)
                        {
                            // ReSharper disable once PossiblyMistakenUseOfParamsMethod
                            table.Append(row);
                            nbrow++;
                        }
                        row = contentRowTemplate?.CloneNode(true) as OXW.TableRow;
                        row?.RemoveAllChildren<OXW.TableCell>();
                    }
                }
                var blockSdtAncestor = block.Ancestors<OXW.SdtBlock>();
                if (0 != blockSdtAncestor.ToList().Count)
                {
                    // case table is in a content control
                    var blockStd = block.Ancestors<OXW.SdtBlock>().First();
                    blockStd.Parent.ReplaceChild(table, blockStd);
                }
                else
                {
                    // case table is directly in the document
                    var blockStd = block;
                    blockStd.Parent.ReplaceChild(table, blockStd);
                }
                
            }
            else
            {
                LogHelper.Instance.LogErrorFormat("Impossible to load data in Table block with a block source of type \"{0}\"", block?.GetType().ToString() ?? "null");
            }
        }
        private static void ModifyWordRowTextContent(OpenXmlElement headerRowTemplate, string txt)
        {
            var cells = headerRowTemplate?.Descendants<OXW.TableCell>();
            if (cells == null) return;
            foreach (var cell in cells)
            {
                ModifyWordCellTextContent(cell, txt);
            }
        }
        private static void ModifyWordCellTextContent(OpenXmlElement cell, string txt)
        {
            OXW.Paragraph paragraph = cell?.Descendants<OXW.Paragraph>().FirstOrDefault();
            if (paragraph == null) return;
            paragraph = paragraph.CloneNode(true) as OXW.Paragraph;
            ModifyWordParagraphTextContent(paragraph, txt);
            cell.RemoveAllChildren<OXW.Paragraph>();
            // ReSharper disable once PossiblyMistakenUseOfParamsMethod
            if (paragraph != null) cell.Append(paragraph);
        }
        private static void ModifyWordParagraphTextContent(OpenXmlElement paragraph, string txt)
        {
            if (null == paragraph) return;
            OXW.Run run = paragraph.Descendants<OXW.Run>().FirstOrDefault();
            if (null != run)
            {
                run = run.CloneNode(true) as OXW.Run;
                paragraph.RemoveAllChildren<OXW.Run>();
            }
            else
            {
                run = new OXW.Run();
            }
            OXW.Text text = run?.Descendants<OXW.Text>().FirstOrDefault();
            text = (null == text ? new OXW.Text() : text.CloneNode(true) as OXW.Text);
            run?.RemoveAllChildren<OXW.Text>();
            if (text != null)
            {
                text.Text = txt;
                if (!string.IsNullOrEmpty(txt) && (char.IsWhiteSpace(txt[0]) || char.IsWhiteSpace(txt[txt.Length-1]))) {
                    text.Space = SpaceProcessingModeValues.Preserve;
                }
                // ReSharper disable once PossiblyMistakenUseOfParamsMethod
                run?.Append(text);
            }
            // ReSharper disable once PossiblyMistakenUseOfParamsMethod
            paragraph.Append(run);
        }

        private static void ReplaceWordRun(OpenXmlElement paragraph, OXD.Run initRun, OXD.Run finalRun)
        {
            if (null == paragraph.Descendants<OXD.Run>()) return;
            List<OXD.Run> runs = paragraph.Descendants<OXD.Run>().ToList();
            foreach (var run in runs)
            {
                if (initRun != run)
                {
                    paragraph.RemoveChild(run);
                }
            }
            paragraph.ReplaceChild(finalRun, initRun);
        }
        #endregion Word methods

        #region Powerpoint methods
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void UpdatePowerPointBlock(ReportData client, OpenXmlPartContainer container, OpenXmlElement block, TableDefinition content, Dictionary<string, string> options)
        {
            if (null != content && block is OXP.GraphicFrame)
            {
                OXD.Table initTable = block.Descendants<OXD.Table>().FirstOrDefault();
                if (null == initTable) return;
                try
                {
                    OXD.Table table = initTable.CloneNode(true) as OXD.Table;
                    OXD.TableRow headerRowTemplate = table?.Descendants<OXD.TableRow>().First().CloneNode(true) as OXD.TableRow;
                    OXD.TableRow contentRowTemplate = table?.Descendants<OXD.TableRow>().Skip(1).First().CloneNode(true) as OXD.TableRow;

                    ModifyPowerPointRowTextContent(headerRowTemplate, string.Empty);
                    ModifyPowerPointRowTextContent(contentRowTemplate, string.Empty);

                    #region Column Number Management
                    List<OXD.GridColumn> columns = table?.TableGrid.Descendants<OXD.GridColumn>().ToList();
                    if (columns != null && columns.Count < content.NbColumns)
                    {
                        int nbNewColumn = content.NbColumns - columns.Count;
                        for (int i = 0, lim = nbNewColumn; i < lim; i++)
                        {
                            AddNewGridColumn(table.TableGrid, headerRowTemplate, contentRowTemplate);
                        }
                    } else if (columns != null && columns.Count > content.NbColumns) {
                        for (int i = content.NbColumns, lim = columns.Count; i < lim; i++) {
                            RemoveLastGridColumn(table.TableGrid);
                        }
                    }
                    #endregion Column Number Management

                    int idx = 0;
                    int nbrow = 0;
                    List<OXD.TableCell> headerCells = headerRowTemplate?.Descendants<OXD.TableCell>().Select(_ => _.CloneNode(true) as OXD.TableCell).ToList();
                    List<OXD.TableCell> contentCells = contentRowTemplate?.Descendants<OXD.TableCell>().Select(_ => _.CloneNode(true) as OXD.TableCell).ToList();
                    headerRowTemplate?.RemoveAllChildren<OXD.TableCell>();
                    OXD.TableRow row = headerRowTemplate;

                    table?.RemoveAllChildren<OXD.TableRow>();
                    foreach (var item in content.Data)
                    {
                        OXD.TableCell cell;
                        if (content.HasColumnHeaders && 0 == nbrow)
                        {
                            cell = headerCells?[idx].CloneNode(true) as OXD.TableCell;
                        }
                        else
                        {
                            cell = contentCells?[idx].CloneNode(true) as OXD.TableCell;
                        }
                        ModifyPowerPointCellTextContent(cell, item);
                            
                        //row.Append(cell); => in office 2016, element <extLst> should absolutely be in the latest position in a row
                        row?.InsertBefore(cell, row.Descendants<OXD.ExtensionList>().FirstOrDefault()); 

                        idx = ++idx % content.NbColumns;
                        if (0 != idx) continue;
                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (null != row)
                        {
                            // ReSharper disable once PossiblyMistakenUseOfParamsMethod
                            table.Append(row);
                            nbrow++;
                        }
                        row = contentRowTemplate?.CloneNode(true) as OXD.TableRow;
                        row?.RemoveAllChildren<OXD.TableCell>();
                    }
                    initTable.Parent.ReplaceChild(table, initTable);
                }
                catch (Exception exception)
                {
                    LogHelper.Instance.LogErrorFormat("An unhandled exception was thrown during table block content generation : '{0}'", exception.ToString());
                    if (initTable.Descendants<OXD.TableRow>() != null && !initTable.Descendants<OXD.TableRow>().Any())
                    {
                        foreach (var row in initTable.Descendants<OXD.TableRow>().Skip(1))
                        {
                            ModifyPowerPointRowTextContent(row, string.Empty);
                        }
                    }
                }
            }
            else
            {
                LogHelper.Instance.LogErrorFormat("Impossible to load data in table block with a block source of type \"{0}\"", block?.GetType().ToString() ?? "null");
            }
        }

        private static void AddNewGridColumn(OpenXmlElement tableGrid, OpenXmlElement headerRow, OpenXmlElement contentRow)
        {
            var columns = tableGrid.Descendants<OXD.GridColumn>().ToList();
            if (columns.Count == 0 || !columns.Any()) return;
            var headerLastCell = headerRow.Descendants<OXD.TableCell>().Last();
            var contentLastCell = contentRow.Descendants<OXD.TableCell>().Last();
            double tableWidth = columns.Sum(_ => Convert.ToInt32(_.Width.Value));
            var newColWidth = Math.Floor(tableWidth / columns.Count);
            foreach (var col in columns)
            {
                col.Width = col.Width > 0 ? Convert.ToInt64(Math.Floor((tableWidth - newColWidth) / (tableWidth / col.Width))) : 0;
            }
            tableGrid.InsertAfter(new OXD.GridColumn() { Width = Convert.ToInt64(newColWidth) }, columns.Last());
            headerRow.InsertAfter((OXD.TableCell)headerLastCell.CloneNode(true), headerLastCell);
            contentRow.InsertAfter((OXD.TableCell)contentLastCell.CloneNode(true), contentLastCell);
        }

		private static void RemoveLastGridColumn(OpenXmlElement tableGrid)
		{
			var lastColumn = tableGrid.Descendants<OXD.GridColumn>().Last();
			tableGrid.RemoveChild(lastColumn);
		}

        private static void ModifyPowerPointRowTextContent(OpenXmlElement headerRowTemplate, string txt)
        {
            var cells = headerRowTemplate?.Descendants<OXD.TableCell>();
            if (cells == null) return;
            foreach (var cell in cells)
            {
                ModifyPowerPointCellTextContent(cell, txt);
            }
        }
        private static void ModifyPowerPointCellTextContent(OpenXmlElement cell, string txt)
        {
            OXD.TextBody textbody = cell?.Descendants<OXD.TextBody>().FirstOrDefault();
            if (textbody == null) return;
            OXD.TextBody final_textbody = textbody.CloneNode(true) as OXD.TextBody;
            OXD.Paragraph paragraph = final_textbody?.Descendants<OXD.Paragraph>().FirstOrDefault();
            if (null != paragraph)
            {
                OXD.Paragraph final_paragraph = paragraph.CloneNode(true) as OXD.Paragraph;
                ModifyPowerPointParagraphTextContent(final_paragraph, txt);
                final_textbody.ReplaceChild(final_paragraph, paragraph);
            }
            cell.ReplaceChild(final_textbody, textbody);
        }
        private static void ModifyPowerPointParagraphTextContent(OpenXmlElement paragraph, string txt)
        {
            OXD.Run run = paragraph?.Descendants<OXD.Run>().FirstOrDefault();
            if (run == null) return;
            OXD.Run final_run = run.CloneNode(true) as OXD.Run;
            OXD.Text text = final_run?.Descendants<OXD.Text>().FirstOrDefault();
            OXD.Text final_text = (null == text ? new OXD.Text() : text.CloneNode(true) as OXD.Text);
            if (final_text != null)
            {
                final_text.Text = txt;
                final_run?.ReplaceChild(final_text, text);
            }
            ReplaceWordRun(paragraph, run, final_run);
        }
        #endregion Powerpoint methods

        #region Excel methods
        [SuppressMessage("ReSharper", "UnusedParameter.Local")]
        private static void UpdateExcelBlock(ReportData client, OpenXmlPartContainer container, OpenXmlElement block, TableDefinition content, Dictionary<string, string> options)
        {
            // TODO : Finalize Excel alimentation
            throw new NotImplementedException();
        }
        #endregion Excel methods

        #endregion METHODS
    }
}
