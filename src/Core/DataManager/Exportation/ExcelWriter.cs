﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager.Exports.Abstractions;
using JJMasterData.Core.DataManager.Expressions.Abstractions;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Options;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web;
using JJMasterData.Core.Web.Components;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJMasterData.Core.DataManager.Exports;

public class ExcelWriter : DataExportationWriterBase, IExcelWriter
{
    public event EventHandler<GridCellEventArgs> OnRenderCell;
    public event AsyncEventHandler<GridCellEventArgs> OnRenderCellAsync;

    public bool ShowBorder { get; set; }

    /// <summary>
    /// Exibir colunas zebradas 
    /// (Default = true)
    /// </summary>
    public bool ShowRowStriped { get; set; }

    private IEntityRepository EntityRepository { get; }
    private IFieldFormattingService FieldFormattingService { get; }

    public ExcelWriter(IExpressionsService expressionsService, IStringLocalizer<JJMasterDataResources> stringLocalizer,
        IOptions<JJMasterDataCoreOptions> options, IControlFactory<JJTextFile> textFileFactory,
        ILoggerFactory loggerFactory, IEntityRepository entityRepository,
        IFieldFormattingService fieldFormattingService) : base(expressionsService, stringLocalizer, options,
        textFileFactory, loggerFactory.CreateLogger<DataExportationWriterBase>())
    {
        EntityRepository = entityRepository;
        FieldFormattingService = fieldFormattingService;
    }

    public override async Task GenerateDocument(Stream stream, CancellationToken token)
    {
        using var sw = new StreamWriter(stream, Encoding.UTF8);
        await sw.WriteLineAsync("<html  ");
        await sw.WriteLineAsync("	xmlns:o=\"urn:schemas-microsoft-com:office:office\"  ");
        await sw.WriteLineAsync("	xmlns:x=\"urn:schemas-microsoft-com:office:excel\"  ");
        await sw.WriteLineAsync("	xmlns:v=\"urn:schemas-microsoft-com:vml\"> ");
        await sw.WriteLineAsync("<head> ");
        await sw.WriteLineAsync("<meta http-equiv=\"content-type\" content=\"text/html; charset=UTF-8\"> ");
        await sw.WriteLineAsync("</head> ");
        await sw.WriteLineAsync("<body>");
        await sw.WriteLineAsync("\t<table>");

        if (Configuration.ExportFirstLine)
        {
            await GenerateHeader(sw);
        }

        await sw.FlushAsync();

        await GenerateBody(sw, token);

        await sw.WriteLineAsync("\t</table>");
        await sw.WriteLineAsync("</body>");
        await sw.WriteLineAsync("</html>");

        await sw.FlushAsync();
        sw.Close();
    }

    private async Task GenerateBody(StreamWriter sw, CancellationToken token)
    {
        if (DataSource == null)
        {
            var entityParameters = new EntityParameters
            {
                Filters = CurrentFilter,
                RecordsPerPage = RecordsPerPage,
                OrderBy = CurrentOrder,
                CurrentPage = 1,
            };
            var result = await EntityRepository.GetDictionaryListAsync(FormElement, entityParameters);
            DataSource = result.Data;
            ProcessReporter.TotalOfRecords = result.TotalOfRecords;
            ProcessReporter.Message = StringLocalizer["Exporting {0} records...", TotalOfRecords.ToString("N0")];
            Reporter(ProcessReporter);
            await GenerateRows(sw, token);

            int totPag = (int)Math.Ceiling((double)DataSource.Count / RecordsPerPage);
            for (int i = 2; i <= totPag; i++)
            {
                entityParameters = new EntityParameters
                {
                    Filters = CurrentFilter,
                    CurrentPage = i,
                    RecordsPerPage = RecordsPerPage,
                    OrderBy = CurrentOrder
                };
                result = await EntityRepository.GetDictionaryListAsync(FormElement, entityParameters);
                DataSource = result.Data;
                TotalOfRecords = result.TotalOfRecords;
                await GenerateRows(sw, token);
            }
        }
        else
        {
            ProcessReporter.TotalOfRecords = TotalOfRecords;
            await GenerateRows(sw, token);
        }
    }

    private async Task GenerateRows(StreamWriter sw, CancellationToken token)
    {
        foreach (var row in DataSource)
        {
            await sw.WriteAsync("\t\t\t<tr>");
            foreach (var field in await GetVisibleFieldsAsync())
            {
                string value = await CreateCell(row, field);

                string tdStyle;
                if (field.DataType is FieldType.Float or FieldType.Int)
                {
                    tdStyle = " style=\"text-align:right;\" ";
                }
                else
                {
                    tdStyle = " style=\"mso-number-format:'@';\" ";
                }

                await sw.WriteAsync($"\t\t\t\t<td{tdStyle}>");
                await sw.WriteAsync(value);
                await sw.WriteLineAsync("</td>");
            }

            await sw.WriteLineAsync("\t\t\t</tr>");
            await sw.FlushAsync();
            ProcessReporter.TotalProcessed++;

            Reporter(ProcessReporter);
            token.ThrowIfCancellationRequested();
        }
    }

    private async Task<string> CreateCell(Dictionary<string, object> row, FormElementField field)
    {
        string value = string.Empty;
        if (field.DataBehavior != FieldBehavior.Virtual)
        {
            if (row.Keys.Contains(field.Name))
            {
                value = FieldFormattingService.FormatValue(field, row[field.Name]);
            }
        }

        if (field.Component == FormComponent.File)
        {
            string link = GetLinkFile(field, row, value);
            if (link != null)
                value = $"<a href=\"{link}\">{value}</a>";
            else
            {
                if (value != null)
                    value = value.Replace(",", "<br style=\"mso-data-placement:same-cell;\"/>");
            }
        }
        
        if (OnRenderCell != null || OnRenderCellAsync != null)
        {
            var args = new GridCellEventArgs
            {
                Field = field,
                DataRow = row,
                Sender = new JJText(value)
            };
            OnRenderCell?.Invoke(this, args);

            if (OnRenderCellAsync != null)
            {
                await OnRenderCellAsync(this, args);
            }
            
            value = args.HtmlResult;
        }

        return value;
    }

    private async Task GenerateHeader(StreamWriter sw)
    {
        await sw.WriteLineAsync("\t\t\t<tr>");
        foreach (var field in await GetVisibleFieldsAsync())
        {
            string thStyle = "";
            if (field.DataType is FieldType.Float or FieldType.Int)
            {
                thStyle = " style=\"text-align:right;\" ";
            }

            await sw.WriteAsync($"\t\t\t\t<td{thStyle}>");
            await sw.WriteAsync(StringLocalizer[field.LabelOrName]);
            await sw.WriteLineAsync("</td>");
        }

        await sw.WriteLineAsync("\t\t\t</tr>");
    }
}