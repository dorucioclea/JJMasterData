﻿using System;
using JJMasterData.Core.DataManager.Exports.Abstractions;
using JJMasterData.Core.DataManager.Exports.Configuration;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Web.Components;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Core.DataManager.Exports;

public class DataExportationWriterFactory
{
    private IServiceProvider ServiceProvider { get; }

    public event EventHandler<GridCellEventArgs> OnRenderCell;
    
    public DataExportationWriterFactory(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }
    
    private IPdfWriter GetPdfWriter()
    {
        return ServiceProvider.GetRequiredService<IPdfWriter>();
    }
    
    public bool PdfWriterExists()
    {
        return GetPdfWriter() != null;
    }

    private IExcelWriter GetExcelWriter()
    {
        return ServiceProvider.GetRequiredService<IExcelWriter>();
    }

    private ITextWriter GetTextWriter()
    {
        return ServiceProvider.GetRequiredService<ITextWriter>();
    }

    public DataExportationWriterBase GetInstance(JJDataExportation exporter)
    {
        DataExportationWriterBase writer;
        switch (exporter.ExportOptions.FileExtension)
        {
            case ExportFileExtension.CSV:
            case ExportFileExtension.TXT:
                var textWriter = GetTextWriter();
                textWriter.Delimiter = exporter.ExportOptions.Delimiter;
                textWriter.OnRenderCell += OnRenderCell;

                writer = (DataExportationWriterBase)textWriter;
                break;

            case ExportFileExtension.XLS:
                var excelWriter = GetExcelWriter();
                excelWriter.ShowRowStriped = exporter.ShowRowStriped;
                excelWriter.ShowBorder = exporter.ShowBorder;
                excelWriter.OnRenderCell += OnRenderCell;

                writer = (DataExportationWriterBase)excelWriter;

                break;
            case ExportFileExtension.PDF:
                var pdfWriter = GetPdfWriter();

                if (pdfWriter == null)
                    throw new NotImplementedException("Please implement IPdfWriter in your application services.");

                pdfWriter.ShowRowStriped = exporter.ShowRowStriped;
                pdfWriter.ShowBorder = exporter.ShowBorder;
                pdfWriter.OnRenderCell += OnRenderCell;

                // ReSharper disable once SuspiciousTypeConversion.Global;
                // PdfWriter is dynamic loaded by plugin.
                //TODO: I think this is bad, things from DataExportationWriterBase should be a parameter at IExportationWriter
                writer = pdfWriter as DataExportationWriterBase;

                break;
            default:
                throw new NotImplementedException();
        }

        ConfigureWriter(exporter, writer);

        return writer;
    }

    private static void ConfigureWriter(JJDataExportation exporter, DataExportationWriterBase writer)
    {
        writer.FormElement = exporter.FormElement;
        writer.Configuration = exporter.ExportOptions;
        writer.UserId = exporter.UserId;
        writer.ProcessOptions = exporter.ProcessOptions;
    }
}
