using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Models.Actions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Models;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.UI.Events.Args;
using JJMasterData.Core.UI.Html;

namespace JJMasterData.Core.UI.Components;

internal class GridTableBody(JJGridView gridView)
{
    private string Name => $"{GridView.Name}-table";
    private JJGridView GridView { get; } = gridView;

    public event AsyncEventHandler<ActionEventArgs> OnRenderActionAsync;
    public event AsyncEventHandler<GridCellEventArgs> OnRenderCellAsync;
    public event AsyncEventHandler<GridSelectedCellEventArgs> OnRenderSelectedCellAsync;

    public async Task<HtmlBuilder> GetHtmlBuilderAsync()
    {
        var tbody = new HtmlBuilder(HtmlTag.Tbody);

        tbody.WithAttribute("id", Name);
        await tbody.AppendRangeAsync(GetRowsList());

        return tbody;
    }

    private async IAsyncEnumerable<HtmlBuilder> GetRowsList()
    {
        var rows = GridView.DataSource;

        for (var i = 0; i < rows?.Count; i++)
        {
            yield return await GetRowHtml(rows[i], i);
        }
    }

    internal async Task<HtmlBuilder> GetRowHtml(IDictionary<string,object> row, int index)
    {
        var tr = new HtmlBuilder(HtmlTag.Tr);
        var basicActions = GridView.FormElement.Options.GridTableActions.OrderBy(x => x.Order).ToList();
        var defaultAction = basicActions.Find(x => x.IsVisible && x.IsDefaultOption);

        tr.WithAttribute("id", $"row{index}");
        var enableGridAction = !GridView.EnableEditMode && (defaultAction != null || GridView.EnableMultiSelect);
        tr.WithCssClassIf(enableGridAction, "tr-hover-action");

        await tr.AppendRangeAsync(GetTdHtmlList(row, index));

        return tr;
    }

    internal async IAsyncEnumerable<HtmlBuilder> GetTdHtmlList(IDictionary<string,object> row, int index)
    {
        var values = await GetValues(row);
        var formStateData = new FormStateData(values, GridView.UserValues, PageState.List);
        var basicActions = GridView.FormElement.Options.GridTableActions.OrderBy(x => x.Order).ToList();
        var defaultAction = basicActions.Find(x => x.IsVisible && x.IsDefaultOption);
        var onClickScript = await GetOnClickScript(formStateData, defaultAction);

        if (GridView.EnableMultiSelect)
        {
            var checkBox = await GetMultiSelectCheckbox(row, index, values);
            var td = new HtmlBuilder(HtmlTag.Td);
            td.WithCssClass("jj-checkbox");
            
            await td.AppendControlAsync(checkBox);

            if (!GridView.EnableEditMode && onClickScript == string.Empty)
            {
                onClickScript =
                    $"$('#{checkBox.Name}').not(':disabled').prop('checked',!$('#{checkBox.Name}').is(':checked')).change()";
            }

            yield return td;
        }

        await foreach (var visibleFieldHtml in GetVisibleFieldsHtmlList(row, index, values, onClickScript))
        {
            yield return visibleFieldHtml;
        }

        await foreach (var actionHtml in GetActionsHtmlListAsync(formStateData))
        {
            yield return actionHtml;
        }
    }
    
    private async IAsyncEnumerable<HtmlBuilder> GetVisibleFieldsHtmlList(IDictionary<string,object> row, int index, IDictionary<string, object> values, string onClickScript)
    {
        await foreach (var field in GridView.GetVisibleFieldsAsync())
        {
            string value = string.Empty;

            var td = new HtmlBuilder(HtmlTag.Td);
            string style = GetTdStyle(field);
            td.WithAttributeIf(string.IsNullOrEmpty(style), "style", style!);
            td.WithAttributeIfNotEmpty( "style", style);
            td.WithAttribute("onclick", onClickScript);

            if (GridView.EnableEditMode && field.DataBehavior != FieldBehavior.ViewOnly)
            {
                td.Append(await GetEditModeFieldHtml(field, row, index, values, value));
            }
            else
            {
                values.TryGetValue(field.Name, out var objValue);
                value = objValue?.ToString() ?? string.Empty;
                var formStateData = new FormStateData(values, GridView.UserValues, PageState.List);
                HtmlBuilder cell;
                if (field.DataItem is not null && field.DataItem.ShowIcon)
                {
                    var dataItemValue = await GridView.DataItemService.GetValuesAsync(field.DataItem, formStateData,null,value).FirstOrDefaultAsync();
                    cell = new HtmlBuilder(HtmlTag.Div);
                    cell.AppendComponent(new JJIcon(dataItemValue.Icon,dataItemValue.IconColor ?? string.Empty));
                    cell.AppendIf(dataItemValue.Description is not null && field.DataItem.ReplaceTextOnGrid, HtmlTag.Span, span =>
                    {
                        span.AppendText(field.DataItem.ReplaceTextOnGrid ? dataItemValue.Description! : dataItemValue.Id);
                        span.WithCssClass($"{BootstrapHelper.MarginLeft}-1");
                    });
                }
                else if (field.DataFile is not null)
                {
                    var textFile =  GridView.ComponentFactory.Controls.Create<JJTextFile>(GridView.FormElement, field, new(formStateData,Name,value));
                    cell = textFile.GetButtonGroupHtml();
                }
                else
                {
                    value = await GridView.FieldsService.FormatGridValueAsync(GridView.FormElement,field, values, GridView.UserValues);
                    cell = new HtmlBuilder(value.Trim());
                }
                if (OnRenderCellAsync != null)
                {
                    var args = new GridCellEventArgs
                    {
                        Field = field,
                        DataRow = row,
                        HtmlResult = cell,
                        Sender = new JJText(value)
                    };
                    
                    await OnRenderCellAsync(this, args);
                    
                    td.Append(args.HtmlResult);
                }
                else
                {
                    td.Append(cell);
                }
            }

            yield return td;
        }
    }

    private async Task<HtmlBuilder> GetEditModeFieldHtml(FormElementField field, IDictionary<string,object> row, int index, IDictionary<string, object> values,
        string value)
    {
        string name = GridView.GetFieldName(field.Name, values);
        bool hasError = GridView.Errors.ContainsKey(name);

        var div = new HtmlBuilder(HtmlTag.Div);

        div.WithCssClassIf(hasError, BootstrapHelper.HasError);
        if (field.Component
                is FormComponent.ComboBox
                or FormComponent.CheckBox
                or FormComponent.Search
                or FormComponent.Number
            && values.TryGetValue(field.Name, out var value1))
        {
            value = value1.ToString();
        }

        var control = GridView.ComponentFactory.Controls.Create(GridView.FormElement, field, new(values, GridView.UserValues, PageState.List), value);
        control.Name = name;
        control.Attributes.Add("nRowId", index.ToString());
        control.CssClass = field.Name;
        
        if (OnRenderCellAsync != null)
        {
            var args = new GridCellEventArgs { Field = field, DataRow = row, Sender = control };

            await OnRenderCellAsync(GridView, args);

            if (args.HtmlResult is not null)
            {
                div.Append(args.HtmlResult);
            }
            else
            {
                await div.AppendControlAsync(control);
            }
        }
        else
        {
            await div.AppendControlAsync(control);
        }
          

        return div;
    }

    public async IAsyncEnumerable<HtmlBuilder> GetActionsHtmlListAsync(FormStateData formStateData)
    {
        var basicActions = GridView.GridActions.OrderBy(x => x.Order).ToList();
        var actionsWithoutGroup = basicActions.FindAll(x => x.IsVisible && !x.IsGroup);
        var groupedActions = basicActions.FindAll(x => x.IsVisible && x.IsGroup);
        await foreach (var action in GetActionsWithoutGroupHtmlAsync(actionsWithoutGroup, formStateData))
        {
            yield return action;
        }

        if (groupedActions.Count > 0)
        {
            yield return await GetActionsGroupHtmlAsync(groupedActions, formStateData);
        }
    }


    private async Task<HtmlBuilder> GetActionsGroupHtmlAsync(IEnumerable<BasicAction> actions, FormStateData formStateData)
    {
        var td = new HtmlBuilder(HtmlTag.Td);
        td.WithCssClass("table-action");

        var btnGroup = new JJLinkButtonGroup();
        
        var factory = GridView.ComponentFactory.ActionButton;
        
        foreach (var groupedAction in actions.Where(a => a.IsGroup).ToList())
        {
            btnGroup.ShowAsButton = groupedAction.ShowAsButton;
            var linkButton = factory.CreateGridTableButton(groupedAction, GridView, formStateData);

            if ( OnRenderActionAsync != null)
            {
                var args = new ActionEventArgs(groupedAction, linkButton, formStateData.Values);
                await OnRenderActionAsync(GridView, args);
            }
            
            btnGroup.Actions.Add(linkButton);
        }

        td.AppendComponent(btnGroup);
        return td;
    }


    private async IAsyncEnumerable<HtmlBuilder> GetActionsWithoutGroupHtmlAsync(IEnumerable<BasicAction> actionsWithoutGroup, FormStateData formStateData)
    {
        var factory = GridView.ComponentFactory.ActionButton;
        foreach (var action in actionsWithoutGroup)
        {
            var td = new HtmlBuilder(HtmlTag.Td);
            td.WithCssClass("table-action");
            var link =  factory.CreateGridTableButton(action, GridView, formStateData);
            if (OnRenderActionAsync is not null)
            {
                var args = new ActionEventArgs(action, link, formStateData.Values);

        
                await OnRenderActionAsync(GridView, args);
                
                
                if (args.HtmlResult != null)
                {
                    td.AppendText(args.HtmlResult);
                    link = null;
                }
            }

            if (link != null)
                td.AppendComponent(link);

            yield return td;
        }
    }

    private static string GetTdStyle(FormElementField field)
    {
        switch (field.Component)
        {
            case FormComponent.ComboBox:
                if (field.DataItem is { ShowIcon: true, ReplaceTextOnGrid: false })
                {
                    return "text-align:center;";
                }

                break;
            case FormComponent.CheckBox:
                return "text-align:center";
            default:
                if (field.DataType is FieldType.Float or FieldType.Int)
                {
                    if (!field.IsPk)
                        return "text-align:right";
                }
                break;
        }

        return string.Empty;
    }

    private async Task<JJCheckBox> GetMultiSelectCheckbox(IDictionary<string,object> row, int index, IDictionary<string, object> values)
    {
        string pkValues = DataHelper.ParsePkValues(GridView.FormElement, values, ';');
        var td = new HtmlBuilder(HtmlTag.Td);
        td.WithCssClass("jj-checkbox");

        var checkBox = new JJCheckBox(GridView.CurrentContext.Request.Form, GridView.StringLocalizer)
        {
            Name = $"jjchk_{index}",
            Value = GridView.EncryptionService.EncryptStringWithUrlEscape(pkValues),
            Text = string.Empty,
            Attributes =
            {
                ["onchange"] = $"GridViewSelectionHelper.selectItem('{GridView.Name}', this);"
            }
        };

        var selectedGridValues = GridView.GetSelectedGridValues();
        
        checkBox.IsChecked = selectedGridValues.Any(x => x.Any(kvp => kvp.Value.Equals(pkValues)));

        if (OnRenderSelectedCellAsync is not null)
        {
            var args = new GridSelectedCellEventArgs
            {
                DataRow = row,
                CheckBox = checkBox
            };
            
            await OnRenderSelectedCellAsync(GridView, args);
            
            if (args.CheckBox != null)
                return checkBox;
        }

        return checkBox;
    }

    private async Task<string> GetOnClickScript(FormStateData formStateData, BasicAction defaultAction)
    {
        if (GridView.EnableEditMode || defaultAction == null)
            return string.Empty;

        var factory = GridView.ComponentFactory.ActionButton;
        
        var actionButton = factory.CreateGridTableButton(defaultAction, GridView, formStateData);

        if (OnRenderActionAsync != null)
        {
            var args = new ActionEventArgs(defaultAction, actionButton, formStateData.Values);
            await OnRenderActionAsync(GridView, args);

            if (args.HtmlResult != null)
                actionButton = null;
        }

        if (actionButton is { Visible: true })
        {
            if (!string.IsNullOrEmpty(actionButton.OnClientClick))
                return actionButton.OnClientClick;

            return !string.IsNullOrEmpty(actionButton.UrlAction)
                ? $"window.location.href = '{actionButton.UrlAction}'"
                : string.Empty;
        }

        return string.Empty;
    }

    private async Task<IDictionary<string, object>> GetValues(IDictionary<string,object> row)
    {
        if (!GridView.EnableEditMode)
            return row;

        var prefixName = GridView.GetFieldName(string.Empty, row);
        return await GridView.FormValuesService.GetFormValuesWithMergedValuesAsync(GridView.FormElement, PageState.List,row, GridView.AutoReloadFormFields, prefixName);
    }
}