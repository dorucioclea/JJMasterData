using System.Collections.Generic;
using System.IO;
using System.Linq;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataManager.Exportation;
using JJMasterData.Core.DataManager.Exportation.Configuration;
using JJMasterData.Core.UI.Html;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.UI.Components;

internal class DataExportationSettings
{
    private JJDataExportation DataExportation { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }

    private readonly string _colSm = BootstrapHelper.Version > 3 ? "col-sm-2" : "col-sm-4";
    private readonly string _bs4Row = BootstrapHelper.Version > 3 ? "row" : string.Empty;

    private readonly string _bsLabel = BootstrapHelper.Version > 3 ? $"{BootstrapHelper.Label}  form-label" : string.Empty;

    public DataExportationSettings(JJDataExportation dataExportation)
    {
        DataExportation = dataExportation;
        StringLocalizer = dataExportation.StringLocalizer;
    }

    internal HtmlBuilder GetHtmlBuilder()
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        string folderPath = DataExportationHelper.GetFolderPath(DataExportation);
        
        html.Append(GetFormHtmlElement(folderPath));
        html.Append(HtmlTag.Hr);
        html.Append(HtmlTag.Div, div =>
        {
            div.WithCssClass("row");
            div.Append(HtmlTag.Div, div =>
            {
                var linkFactory = DataExportation.ComponentFactory.Html.LinkButton;
                string onClientClick = DataExportation.Scripts.GetStartExportationScript();
                
                var btnOk = linkFactory.Create();
                btnOk.Text = StringLocalizer["Export"];
                btnOk.IconClass = "fa fa-check";
                btnOk.ShowAsButton = true;
                btnOk.OnClientClick = onClientClick;

                var btnCancel = linkFactory.Create();
                btnCancel.Text = "Cancel";
                btnCancel.IconClass = "fa fa-times";
                btnCancel.ShowAsButton = true;

                btnCancel.Attributes.Add(BootstrapHelper.DataDismiss, "modal");

                div.WithCssClass($"col-sm-12 {BootstrapHelper.TextRight}");
                div.AppendComponent(btnOk);
                div.AppendText("&nbsp;");
                div.AppendComponent(btnCancel);
            });
        });

        return html;
    }

    private HtmlBuilder GetFormHtmlElement(string exportationFolderPath)
    {
        var div = new HtmlBuilder(HtmlTag.Div);
        div.WithCssClass(BootstrapHelper.FormHorizontal);
        div.WithAttribute("role", "form");

        div.Append(HtmlTag.Div, div =>
        {
            div.Append(GetFileExtensionField());

            div.Append(GetOrientationField());

            div.Append(GetExportAllField());

            div.Append(GetDelimiterField());

            div.Append(GetFirstLineField());

            div.AppendComponent(GetFilesPanelHtmlElement(exportationFolderPath));

            div.Append(HtmlTag.Div, div =>
            {
                div.WithCssClass("row");
                div.Append(HtmlTag.Label, label =>
                {
                    label.WithCssClass($"small {_bsLabel} col-sm-12");
                    label.Append(HtmlTag.Br);
                    label.AppendComponent(new JJIcon()
                    {
                        IconClass = "text-info fa fa-info-circle"
                    });
                    label.AppendText(
                        $"&nbsp;{StringLocalizer["Filters performed in the previous screen will be considered in the export"]}");
                });
            });

            div.AppendComponent(GetTooManyRecordsAlert(DataExportation.Name));
        });

        return div;
    }

    private HtmlBuilder GetFileExtensionField()
    {
        return new HtmlBuilder(HtmlTag.Div)
            .WithCssClass($"{BootstrapHelper.FormGroup} {_bs4Row}")
            .Append(HtmlTag.Label, label =>
            {
                label.WithAttribute("for", $"{DataExportation.Name}{ExportOptions.FileName}");
                label.WithCssClass($"{_bsLabel} col-sm-4");
                label.AppendText(StringLocalizer["Export to"]);
            })
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass(_colSm);
                div.Append(HtmlTag.Select, select =>
                {
                    select.WithNameAndId($"{DataExportation.Name}{ExportOptions.FileName}");
                    //select.WithAttribute("onchange", $"DataExportationHelper.showExportOptions('{DataExportation.Name}',this.value);");
                    select.WithCssClass("form-control form-select");
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue($"{(int)ExportFileExtension.XLS}");
                        option.WithAttribute("selected", "selected");
                        option.AppendText("Excel");
                    });
                    if (PdfWriterExists())
                    {
                        select.Append(HtmlTag.Option, option =>
                        {
                            option.WithValue($"{(int)ExportFileExtension.PDF}");
                            option.AppendText("PDF");
                        });
                    }

                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue($"{(int)ExportFileExtension.CSV}");
                        option.AppendText("CSV");
                    });
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue($"{(int)ExportFileExtension.TXT}");
                        option.AppendText("TXT");
                    });
                });
            });
    }

    private HtmlBuilder GetOrientationField()
    {
        return new HtmlBuilder(HtmlTag.Div)
            .WithCssClass($"{BootstrapHelper.FormGroup} {_bs4Row}")
            .WithAttribute("id", $"{DataExportation.Name}-div-export-orientation")
            .WithAttribute("style", "display:none")
            .Append(HtmlTag.Label, label =>
            {
                label.WithAttribute("for", $"{DataExportation.Name}{ExportOptions.TableOrientation}");
                label.WithCssClass($"{_bsLabel} col-sm-4");
                label.AppendText(StringLocalizer["Orientation"]);
            })
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass(_colSm);
                div.Append(HtmlTag.Select, select =>
                {
                    select.WithNameAndId($"{DataExportation.Name}{ExportOptions.TableOrientation}");
                    select.WithCssClass("form-control form-select");
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue("1");
                        option.WithAttribute("selected", "selected");
                        option.AppendText("Landscape");
                    });
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue("0");
                        option.AppendText("Portrait");
                    });
                });
            });
    }

    private HtmlBuilder GetExportAllField()
    {
        return new HtmlBuilder(HtmlTag.Div)
            .WithCssClass($"{BootstrapHelper.FormGroup} {_bs4Row}")
            .WithAttribute("id", $"{DataExportation.Name}-div-export-all")
            .Append(HtmlTag.Label, label =>
            {
                label.WithAttribute("for", $"{DataExportation.Name}{ExportOptions.ExportAll}");
                label.WithCssClass($"{_bsLabel} col-sm-4");
                label.AppendText(StringLocalizer["Fields"]);
            })
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass(_colSm);
                div.Append(HtmlTag.Select, select =>
                {
                    select.WithNameAndId($"{DataExportation.Name}{ExportOptions.ExportAll}");
                    select.WithCssClass("form-control form-select");
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue("1");
                        option.WithAttribute("selected", "selected");
                        option.AppendText(StringLocalizer["All"]);
                    });
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue("2");
                        option.AppendText(StringLocalizer["Only the fields visible on the screen"]);
                    });
                });
            });
    }

    private HtmlBuilder GetDelimiterField()
    {
        return new HtmlBuilder(HtmlTag.Div)
            .WithCssClass($"{BootstrapHelper.FormGroup} {_bs4Row}")
            .WithAttribute("style", "display:none;")
            .WithAttribute("id", $"{DataExportation.Name}-div-export-delimiter")
            .Append(HtmlTag.Label, label =>
            {
                label.WithAttribute("for", $"{DataExportation.Name}{ExportOptions.ExportDelimiter}");
                label.WithCssClass($"{_bsLabel} col-sm-4");
                label.AppendText(StringLocalizer["Delimiter"]);
            })
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass(_colSm);
                div.Append(HtmlTag.Select, select =>
                {
                    select.WithNameAndId($"{DataExportation.Name}{ExportOptions.ExportDelimiter}");
                    select.WithCssClass("form-control form-select");
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue(";");
                        option.WithAttribute("selected", "selected");
                        option.AppendText(StringLocalizer["Semicolon (;)"]);
                    });
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue(",");
                        option.AppendText(StringLocalizer["Comma (,)"]);
                    });
                    select.Append(HtmlTag.Option, option =>
                    {
                        option.WithValue("|");
                        option.AppendText(StringLocalizer["Pipe (|)"]);
                    });
                });
            });
    }

    private HtmlBuilder GetFirstLineField()
    {
        return new HtmlBuilder(HtmlTag.Div)
            .WithCssClass($"{BootstrapHelper.FormGroup} {_bs4Row}")
            .WithAttribute("id", $"{DataExportation.Name}-div-export-firstline")
            .Append(HtmlTag.Label, label =>
            {
                label.WithAttribute("for", $"{DataExportation.Name}{ExportOptions.ExportTableFirstLine}");
                label.WithCssClass($"{_bsLabel} col-sm-4");
                label.AppendText(StringLocalizer["Export first line as title"]);
            })
            .Append(HtmlTag.Div, div =>
            {
                div.WithCssClass(_colSm);
                var exportFirstLineCheckbox = DataExportation.ComponentFactory.Controls.CheckBox.Create();
                exportFirstLineCheckbox.Name = $"{DataExportation.Name}{ExportOptions.ExportTableFirstLine}";
                exportFirstLineCheckbox.IsChecked = DataExportation.ExportOptions.ExportFirstLine;
                exportFirstLineCheckbox.IsSwitch = true;
                exportFirstLineCheckbox.SwitchSize = CheckBoxSwitchSize.Medium;
                
                div.Append(exportFirstLineCheckbox.GetHtmlBuilder());
            });
    }

    private JJAlert GetTooManyRecordsAlert(string name)
    {
        var alert = new JJAlert
        {
            ShowIcon = true,
            Name = $"data-exportation-warning{name}",
            ShowCloseButton = true,
            Title = "Warning!",
            Icon = IconType.ExclamationTriangle,
            Color = PanelColor.Warning,
            Messages =
            {
                StringLocalizer[
                    "You are trying to export more than 50,000 records, this can cause system overhead and slowdowns."],
                StringLocalizer[
                    "Use filters to reduce export volume, if you need to perform this operation frequently, contact your system administrator."]
            }
        };
        alert.SetAttr("style", "display:none;");
        return alert;
    }

    private  JJCollapsePanel GetFilesPanelHtmlElement(string exportationFolderPath)
    {
        var files = GetGeneratedFiles(exportationFolderPath);
        var panel = new JJCollapsePanel(DataExportation.CurrentContext.Request.Form)
        {
            Name = "exportCollapse",
            ExpandedByDefault = false,
            TitleIcon = new JJIcon(IconType.FolderOpenO),
            Title = $"{StringLocalizer["Recently generated files"]} ({files.Count})",
            HtmlBuilderContent = GetLastFilesHtmlElement(files)
        };

        return panel;
    }

    private HtmlBuilder GetLastFilesHtmlElement(List<FileInfo> files)
    {
        if (files == null || files.Count == 0)
            return new HtmlBuilder(StringLocalizer["No recently generated files."]);

        var html = new HtmlBuilder(HtmlTag.Div);
        foreach (var file in files)
        {
            if (FileIO.IsFileLocked(file))
                continue;

            var icon = JJDataExportation.GetFileIcon(file.Extension);
            string url = DataExportation.GetDownloadUrl(file.FullName);

            var div = new HtmlBuilder(HtmlTag.Div);
            div.WithCssClass("mb-1");
            div.AppendComponent(icon);
            div.AppendText("&nbsp;");
            div.Append(HtmlTag.A, a =>
            {
                a.WithAttribute("href", url);
                a.WithAttribute("title", "Download");
                a.AppendText(file.Name);
            });

            html.Append(div);
        }

        return html;
    }

    private static List<FileInfo> GetGeneratedFiles(string exportationFolderPath)
    {
        var list = new List<FileInfo>();

        var oDir = new DirectoryInfo(exportationFolderPath);

        if (oDir.Exists)
            list.AddRange(oDir.GetFiles("*", SearchOption.AllDirectories));

        return list.OrderByDescending(f => f.CreationTime).ToList();
    }

    private bool PdfWriterExists() => DataExportation.DataExportationWriterFactory.PdfWriterExists();
}