﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using iText.IO.Font;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Canvas.Draw;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using JJMasterData.Commons.Configuration;
using JJMasterData.Commons.Data;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager.Exports.Abstractions;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Factories;

namespace JJMasterData.Pdf;

public class PdfWriter : BaseWriter, IPdfWriter
{
    public event EventHandler<GridCellEventArgs> OnRenderCell;
    
    public bool ShowBorder { get; set; }
    
    public bool ShowRowStriped { get; set; }

    public bool IsLandscape { get; set; }

    public IEntityRepository EntityRepository { get; } =
        JJService.Provider.GetScopedDependentService<IEntityRepository>();
    
    public IFieldFormattingService FieldFormattingService { get; } =
        JJService.Provider.GetScopedDependentService<IFieldFormattingService>();
    
    public override void GenerateDocument(Stream ms, CancellationToken token)
    {
        using var writer = new iText.Kernel.Pdf.PdfWriter(ms);

        var pdf = new PdfDocument(writer);

        var pageSize = IsLandscape ? PageSize.A4.Rotate() : PageSize.A4;
        pdf.SetDefaultPageSize(pageSize);

        var document = new Document(pdf);
        document.SetFontSize(8);
        var today = new Paragraph(DateTime.Now.ToLongDateString()).SetTextAlignment(TextAlignment.RIGHT);
        document.Add(today);

        var title = FormElement.Title;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var header = new Paragraph(title)
                .SetTextAlignment(TextAlignment.LEFT)
                .SetFontSize(16)
                .SetBold();
            document.Add(header);
        }

        var line = new SolidLine(1f);
        line.SetColor(WebColors.GetRGBColor("black"));

        var ls = new LineSeparator(line);
        document.Add(ls);

        var paragraph = new Paragraph("\n");
        document.Add(paragraph);

        var table = new Table(Fields.Count, true);
        table.UseAllAvailableWidth();
        document.Add(table);

        GenerateHeader(table);
        GenerateBody(table, token);

        table.Complete();
        document.Close();
        pdf.Close();
    }

    private void GenerateHeader(Table table)
    {
        foreach (var field in Fields)
        {
            Cell cell = new();
            cell.Add(new Paragraph(new Text(field.Label).SetBold()));
            SetHeaderCellStyle(field, ref cell);
            table.AddHeaderCell(cell);
        }
        table.Flush();
    }

    private void GenerateBody(Table table, CancellationToken token)
    {
        int tot = 0;
        if (DataSource == null)
        {
            DataSource = EntityRepository.GetDataTable(FormElement, (IDictionary)CurrentFilter, CurrentOrder, RegPerPag, 1, ref tot);
            ProcessReporter.TotalRecords = tot;
            ProcessReporter.Message = StringLocalizer["Exporting {0} records...", tot.ToString("N0")];
            Reporter(ProcessReporter);
            GenerateRows(table, token);

            int totPag = (int)Math.Ceiling((double)tot / RegPerPag);
            for (int i = 2; i <= totPag; i++)
            {
                DataSource = EntityRepository.GetDataTable(FormElement, (IDictionary)CurrentFilter, CurrentOrder, RegPerPag, i, ref tot);
                GenerateRows(table, token);
            }
        }
        else
        {
            ProcessReporter.TotalRecords = DataSource.Rows.Count;
            GenerateRows(table, token);
        }
    }

    private void GenerateRows(Table table, CancellationToken token)
    {
        foreach (DataRow row in DataSource.Rows)
        {
            var scolor = (ShowRowStriped && (ProcessReporter.TotalProcessed % 2) == 0) ? "white" : "#f2fdff";
            var wcolor = WebColors.GetRGBColor(scolor);
            table.SetBackgroundColor(wcolor);

            foreach (FormElementField field in Fields)
            {
                var cell = CreateCell(row, field);
                table.AddCell(cell);
                table.Flush();
            }

            ProcessReporter.TotalProcessed++;
            Reporter(ProcessReporter);
            token.ThrowIfCancellationRequested();
        }
    }

    [Obsolete("Must be async")]
    private Cell CreateCell(DataRow row, FormElementField field)
    {
        string value = string.Empty;
        Text image = null;

        var values = new Dictionary<string,dynamic>();
        for (int i = 0; i < row.Table.Columns.Count; i++)
        {
            values.Add(row.Table.Columns[i].ColumnName, row[i]);
        }

        if (field.DataBehavior != FieldBehavior.Virtual)
        {
            if (field.Component == FormComponent.ComboBox && field.DataItem != null)
            {
                value = GetComboBoxValue(field, values, ref image);
            }
            else
            {
                value = FieldFormattingService.FormatGridValueAsync(field, values,null).GetAwaiter().GetResult();
            }
        }

        var cell = new Cell();
        SetCellStyle(field, ref cell);

        var ev = OnRenderCell;
        if (ev != null)
        {
            var args = new GridCellEventArgs
            {
                Field = field,
                DataRow = row,
                Sender = new JJText(value)
            };

            ev.Invoke(this, args);

            value = args.HtmlResult;
            value = value.Replace("<br>", "\r\n");
            value = value.Replace("<center>", string.Empty);
            value = value.Replace("</center>", string.Empty);
        }


        Link link = null;

        if (field.Component == FormComponent.File)
        {
            string url = GetLinkFile(field, row, value);

            if (url != null)
            {
                link = new Link(value, PdfAction.CreateURI(url));
            }
            else
            {
                value = value?.Replace(",", "\r\n");
            }
        }

        var paragraph = new Paragraph();

        if (image != null)
            paragraph.Add(image);

        if (link != null)
            paragraph.Add(link);
        else
            paragraph.Add(value);

        cell.Add(paragraph);

        return cell;
    }

    private void SetHeaderCellStyle(FormElementField field, ref Cell cell)
    {
        cell.SetBorderTop(ShowBorder ? new GrooveBorder(WebColors.GetRGBColor("black"), 1f) : null);
        cell.SetBorderRight(ShowBorder ? new GrooveBorder(WebColors.GetRGBColor("black"), 1f) : null);
        cell.SetBorderLeft(ShowBorder ? new GrooveBorder(WebColors.GetRGBColor("black"), 1f) : null);

        if (!field.IsPk && field.Component != FormComponent.ComboBox &&
            field.DataType is FieldType.Float or FieldType.Int)
        {
            cell.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        }
        else
        {
            cell.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        }
    }

    private void SetCellStyle(FormElementField field, ref Cell cell)
    {
        cell.SetBorderBottom(new GrooveBorder(WebColors.GetRGBColor("black"), 1f));
        cell.SetBorderTop(ShowBorder ? new GrooveBorder(WebColors.GetRGBColor("black"), 1f) : null);
        cell.SetBorderRight(ShowBorder ? new GrooveBorder(WebColors.GetRGBColor("black"), 1f) : null);
        cell.SetBorderLeft(ShowBorder ? new GrooveBorder(WebColors.GetRGBColor("black"), 1f) : null);

        if (!field.IsPk && field.Component != FormComponent.ComboBox &&
            (field.DataType == FieldType.Float || field.DataType == FieldType.Int))
        {
            cell.SetHorizontalAlignment(HorizontalAlignment.RIGHT);
        }
        else
        {
            cell.SetHorizontalAlignment(HorizontalAlignment.LEFT);
        }
    }

    private string GetComboBoxValue(FormElementField field, IDictionary<string,dynamic> values, ref Text image)
    {
        if (values == null || !values.ContainsKey(field.Name) || values[field.Name] == null)
            return string.Empty;

        string value = string.Empty;
        string selectedValue = values[field.Name].ToString();
        var factory = JJService.Provider.GetScopedDependentService<ControlsFactory>();
        var cbo = (JJComboBox)factory.CreateControl(FormElement,FormElement.Name,field, PageState.List, values,null, selectedValue);
        var item = cbo.GetValue(selectedValue);

        if (item != null)
        {
            if (field.DataItem.ReplaceTextOnGrid)
            {
                value = " " + item.Description.Trim();
            }
            if (field.DataItem.ShowImageLegend)
            {
                image = new Text(item.Icon.GetUnicode().ToString());
                var color = ColorTranslator.FromHtml(item.ImageColor);

                var rgbColor = $"rgb({Convert.ToInt16(color.R)},{Convert.ToInt16(color.G)},{Convert.ToInt16(color.B)})";

                image.SetFont(CreateFontAwesomeIcon());
                image.SetFontColor(WebColors.GetRGBColor(rgbColor));
            }
        }
        else
        {
            value = selectedValue;
        }

        return value;
    }

    private PdfFont CreateFontAwesomeIcon()
    {
        var fontBytes = ExtractResource("JJMasterData.Pdf.Fonts.fontawesome-webfont.ttf");
        var fontProgram = FontProgramFactory.CreateFont(fontBytes, true);
        
        return PdfFontFactory.CreateFont(fontProgram);
    }
    
    private static byte[] ExtractResource(string filename)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var resFilestream = assembly.GetManifestResourceStream(filename);
        
        if (resFilestream == null)
            return null;
        byte[] ba = new byte[resFilestream.Length];
        resFilestream.Read(ba, 0, ba.Length);
        return ba;
    }
    
}
