﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Web;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Extensions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Tasks;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Exports;
using JJMasterData.Core.DataManager.Exports.Abstractions;
using JJMasterData.Core.DataManager.Exports.Configuration;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Options;
using JJMasterData.Core.Web.Components.Scripts;
using JJMasterData.Core.Web.Factories;
using JJMasterData.Core.Web.Html;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJMasterData.Core.Web.Components;

/// <summary>
/// Exporta dados para um arquivo
/// </summary>
///
///TODO: I think Exportation is better than Exp, exp can be experience, experiment, etc.
public class JJDataExp : JJBaseProcess
{

    #region "Events"

    /// <summary>
    /// Event fired when the cell is rendered.
    /// </summary>
    public EventHandler<GridCellEventArgs> OnRenderCell = null;

    #endregion

    #region "Properties"

    private ExportOptions _exportOptions;

    /// <summary>
    /// Recupera as configurações de exportação 
    /// </summary>
    public ExportOptions ExportOptions
    {
        get => _exportOptions ??= new ExportOptions();
        internal set => _exportOptions = value;
    }
    
    /// <summary>
    /// Exibi borda na grid 
    /// (Default = false)
    /// </summary>
    public bool ShowBorder { get; set; }

    public bool ShowRowStriped { get; set; }
    internal JJMasterDataCoreOptions MasterDataOptions { get; }
    internal DataExportationScriptHelper ScriptHelper { get; }
    private FileDownloaderFactory FileDownloaderFactory { get; }
    internal IHttpContext CurrentContext { get; }

    #endregion

    #region "Constructors"
    internal JJDataExp(
        FormElement formElement,
        IEntityRepository entityRepository,
        IExpressionsService expressionsService,
        IFieldValuesService fieldValuesService,
        DataExportationScriptHelper dataExportationScriptHelper,
        IOptions<JJMasterDataCoreOptions> masterDataOptions,
        IBackgroundTask backgroundTask, 
        IStringLocalizer<JJMasterDataResources> stringLocalizer,
        FileDownloaderFactory fileDownloaderFactory,
        ILoggerFactory loggerFactory,
        IHttpContext currentContext) : 
        base(currentContext,entityRepository, expressionsService, fieldValuesService, backgroundTask, loggerFactory.CreateLogger<JJBaseProcess>(),stringLocalizer)
    {
        ScriptHelper = dataExportationScriptHelper;
        FileDownloaderFactory = fileDownloaderFactory;
        CurrentContext = currentContext;
        MasterDataOptions = masterDataOptions.Value;
        FormElement = formElement;
    }
    #endregion

    internal override HtmlBuilder RenderHtml()
    {
        return IsRunning() ? new DataExportationLog(this).GetHtmlProcess() : new DataExportationSettings(this).GetHtmlElement();
    }

    internal static JJIcon GetFileIcon(string ext)
    {
        if (ext.EndsWith("xls"))
            return new JJIcon(IconType.FileExcelO);
        if (ext.EndsWith("pdf"))
            return new JJIcon(IconType.FilePdfO);
        return new JJIcon(IconType.FileTextO);
    }

    internal string GetDownloadUrl(string filePath)
    {
        var downloader = FileDownloaderFactory.CreateFileDownloader(filePath);
        return downloader.GetDownloadUrl(filePath);
    }

    private string GetFinishedMessageHtml(DataExportationReporter reporter)
    {
        if (!reporter.HasError)
        {
            string url = GetDownloadUrl(reporter.FilePath);
            var html = new HtmlBuilder(HtmlTag.Div);

            if (reporter.HasError)
            {
                var panel = new JJValidationSummary
                {
                    ShowCloseButton = false,
                    MessageTitle = reporter.Message
                };
                html.AppendElement(panel);
            }
            else
            {
                var file = new FileInfo(reporter.FilePath);
                var icon = GetFileIcon(file.Extension);
                icon.CssClass = "fa-3x ";

                html.AppendElement(HtmlTag.Div, div =>
                {
                    div.WithCssClass("text-center");
                    div.AppendElement(HtmlTag.Br);
                    div.AppendElement(HtmlTag.Span, span =>
                    {
                        span.WithCssClass("text-success");
                        span.AppendElement(HtmlTag.Span, span =>
                        {
                            span.WithCssClass("fa fa-check fa-lg");
                            span.WithAttribute("aria-hidden", "true");
                        });

                        span.AppendText(Translate.Key("File generated successfully!"));
                    });
                    div.AppendElement(HtmlTag.Br);

                    string elapsedTime = Format.FormatTimeSpan(reporter.StartDate, reporter.EndDate);

                    div.AppendText(StringLocalizer["Process performed on {0}", elapsedTime]);

                    div.AppendElement(HtmlTag.Br);

                    div.AppendElement(HtmlTag.I, i =>
                    {
                        i.AppendText(
                            Translate.Key("If the download does not start automatically, click on the icon below."));
                    });

                    div.AppendElement(HtmlTag.Br);
                    div.AppendElement(HtmlTag.Br);
                    div.AppendElement(HtmlTag.Br);

                    div.AppendElement(HtmlTag.A, a =>
                    {
                        a.WithAttribute("id", $"export_link_{Name}");
                        a.WithAttribute("href", url);
                        a.AppendElement(icon);
                        a.AppendElement(HtmlTag.Br);
                        a.AppendText(file.Name);
                    });
                    div.AppendElement(HtmlTag.Br);
                    div.AppendElement(HtmlTag.Br);
                });
            }

            var btnCancel = new JJLinkButton
            {
                Text = "Close",
                IconClass = "fa fa-times",
                ShowAsButton = true
            };
            btnCancel.Attributes.Add(BootstrapHelper.DataDismiss, "modal");

            html.AppendElement(HtmlTag.Hr);

            html.AppendElement(HtmlTag.Div, div =>
            {
                div.WithCssClass("row");
                div.AppendElement(HtmlTag.Div, div =>
                {
                    div.WithCssClass($"col-sm-12 {BootstrapHelper.TextRight}");
                    div.AppendElement(btnCancel);
                });
            });

            return html.ToString();
        }

        var alert = new JJAlert
        {
            Title = reporter.Message,
            Icon = IconType.Warning,
            Color = PanelColor.Danger
        };

        return alert.GetHtml();
    }

    private BaseWriter CreateWriter()
    {
        return WriterFactory.GetInstance(this);
    }

    public void StartExportation(DataTable dt)
    {
        var exporter = CreateWriter();

        exporter.DataSource = dt;
        exporter.CurrentContext = HttpContext.Current;
        exporter.AbsoluteUri = HttpContext.Current.Request.Url.AbsoluteUri;
        BackgroundTask.Run(ProcessKey, exporter);
    }

    internal void ExportFileInBackground(IDictionary<string,dynamic>filter, string order)
    {
        var exporter = CreateWriter();

        exporter.CurrentFilter = filter;
        exporter.CurrentOrder = order;
        exporter.CurrentContext = HttpContext.Current;
        exporter.AbsoluteUri = HttpContext.Current.Request.Url.AbsoluteUri;

        BackgroundTask.Run(ProcessKey, exporter);
    }

    internal DataExportationProgressDto GetCurrentProgress()
    {
        bool isRunning = BackgroundTask.IsRunning(ProcessKey);
        var reporter = BackgroundTask.GetProgress<DataExportationReporter>(ProcessKey);
        var dto = new DataExportationProgressDto();
        if (reporter != null)
        {
            dto.Message = reporter.Message;
            dto.HasError = reporter.HasError;
            dto.StartDate = reporter.StartDate.ToDateTimeString();
            dto.PercentProcess = reporter.Percentage;
            dto.IsProcessing = isRunning;

            if (!isRunning && !reporter.EndDate.Equals(DateTime.MinValue))
                dto.FinishedMessage = GetFinishedMessageHtml(reporter);
        }
        else
        {
            dto.Message = Translate.Key("Waiting...");
            dto.StartDate = DateTime.Now.ToShortDateString();
        }

        return dto;
    }
}