using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using JJMasterData.Commons.Configuration;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Logging;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Web.Html;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace JJMasterData.Core.Web.Components;

public class JJComboBox : JJBaseControl
{
    private IList<DataItemValue> _values;
    private string _selectedValue;
    private FormElementDataItem _dataItem;

    internal IEntityRepository EntityRepository { get; }
    internal ILogger<JJComboBox> Logger { get; }
    
    private IExpressionsService ExpressionsService { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }

    internal IDictionary<string,dynamic> FormValues { get; set; }

    internal PageState PageState { get; set; }

    /// <summary>
    /// If the filter is MULTVALUES_EQUALS, enable multiselect.
    /// </summary>
    public bool MultiSelect { get; set; }

    public FormElementDataItem DataItem
    {
        get => _dataItem ??= new FormElementDataItem();
        set => _dataItem = value;
    }

    public string SelectedValue
    {
        get
        {
            if (_selectedValue == null && CurrentContext.IsPost)
            {
                _selectedValue = CurrentContext.Request[Name];
            }

            return _selectedValue;
        }
        set => _selectedValue = value;
    }

    public JJComboBox(IHttpContext httpContext,
        IEntityRepository entityRepository,
        IExpressionsService expressionsService,
        IStringLocalizer<JJMasterDataResources> stringLocalizer,
        ILogger<JJComboBox> logger) : base(httpContext)
    {
        EntityRepository = entityRepository;
        Logger = logger;
        ExpressionsService = expressionsService;
        StringLocalizer = stringLocalizer;
        Enabled = true;
        MultiSelect = false;
    }

    internal override HtmlBuilder RenderHtml()
    {
        if (DataItem == null)
            throw new ArgumentException("[DataItem] properties not defined for combo", Name);

        var values = GetValues();

        if (values == null)
            throw new ArgumentException("Data source not defined for combo", Name);
        
        if (ReadOnly)
        {
            var combobox = new HtmlBuilder(HtmlTag.Div);
            combobox.AppendRange(GetReadOnlyInputs(values));
            return combobox;
        }

        return GetSelectHtml(values);
    }

    private HtmlBuilder GetSelectHtml(IEnumerable<DataItemValue> values)
    {
        var select = new HtmlBuilder(HtmlTag.Select)
            .WithCssClass(CssClass)
            .WithCssClass("form-control ")
            .WithCssClass(MultiSelect || DataItem.ShowImageLegend ? "selectpicker" : "form-select")
            .WithNameAndId(Name)
            .WithAttributeIf(MultiSelect, "multiple")
            .WithAttributeIf(MultiSelect, "title", StringLocalizer["All"])
            .WithAttributeIf(MultiSelect && PageState == PageState.Filter, "data-live-search", "true")
            .WithAttributeIf(MultiSelect, "multiselect", "multiselect")
            .WithAttributeIf(!Enabled, "disabled", "disabled")
            .WithAttribute("data-style", "form-control")
            .WithAttributes(Attributes)
            .AppendRange(GetOptions(values));

        return select;
    }

    private IEnumerable<HtmlBuilder> GetOptions(IEnumerable<DataItemValue> values)
    {
        var firstOption = new HtmlBuilder(HtmlTag.Option)
            .WithValue(string.Empty)
            .AppendTextIf(DataItem.FirstOption == FirstOptionMode.All, StringLocalizer["(All)"])
            .AppendTextIf(DataItem.FirstOption == FirstOptionMode.Choose, StringLocalizer["(Choose)"]);

        if (DataItem.FirstOption != FirstOptionMode.None)
             yield return firstOption;

        foreach (var value in values)
        {
            var label = IsManualValues() ? StringLocalizer[value.Description] : value.Description;

            var isSelected = !MultiSelect && SelectedValue != null && SelectedValue.Equals(value.Id);
            
            if (MultiSelect && SelectedValue != null)
            {
                isSelected = SelectedValue.Split(',').Contains(value.Id);
            }
            
            var option = new HtmlBuilder(HtmlTag.Option)
                .WithValue(value.Id)
                .WithAttributeIf(isSelected, "selected")
                .WithAttributeIf(DataItem.ShowImageLegend, "data-icon", value.Icon.GetCssClass())
                .AppendText(label);

            yield return option;
        }

    }

    private IEnumerable<HtmlBuilder> GetReadOnlyInputs(IEnumerable<DataItemValue> values)
    {
        var hiddenInput = new HtmlBuilder(HtmlTag.Input)
            .WithAttribute("type", "hidden")
            .WithNameAndId(Name)
            .WithValue(SelectedValue);

        yield return hiddenInput;

        var selectedText = GetSelectedText(values);

        var readonlyInput = new HtmlBuilder(HtmlTag.Input)
            .WithNameAndId("cboview_" + Name)
            .WithCssClass("form-control form-select")
            .WithCssClass(CssClass)
            .WithValue(selectedText)
            .WithAttributes(Attributes)
            .WithAttribute("readonly", "readonly");

        yield return readonlyInput;
        
    }


    private string GetSelectedText(IEnumerable<DataItemValue> list)
    {
        string selectedText = string.Empty;

        if (SelectedValue == null)
            return selectedText;

        foreach (var item in list.Where(item => SelectedValue.Equals(item.Id)))
        {
            selectedText = item.Description;

            if (IsManualValues())
                selectedText = StringLocalizer[selectedText];

            break;
        }

        return selectedText;
    }

    public IList<DataItemValue> GetValues()
    {
        try
        {
            _values ??= GetValues(null);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading data from JJComboBox");
            throw;
        }

        return _values;
    }


    /// <summary>
    /// Recovers the description from the selected value;
    /// </summary>
    public string GetDescription()
    {
        string description;
        var item = GetValue(SelectedValue);
        if (item == null)
            return null;

        var label = IsManualValues() ? StringLocalizer[item.Description] : item.Description;

        if (DataItem.ShowImageLegend)
        {
            var div = new HtmlBuilder(HtmlTag.Div);

            var icon = new JJIcon(item.Icon, item.ImageColor, item.Description)
            {
                CssClass = "fa-lg fa-fw"
            }.RenderHtml();

            div.Append(icon);

            if (DataItem.ReplaceTextOnGrid)
            {
                div.AppendText("&nbsp;");
                div.AppendText(label);
            }
            description = div.ToString();
        }
        else
        {
            description = label;
        }

        return description;
    }


    public DataItemValue GetValue(string searchId)
    {
        if (searchId == null)
            return null;

        var listValues = _values ?? GetValues(searchId);


        return listValues?.ToList().Find(x => x.Id.Equals(searchId));
    }

    private IList<DataItemValue> GetValues(string searchId)
    {
        if (DataItem == null)
            return null;

        IList<DataItemValue> values = new List<DataItemValue>();
        if (DataItem.Command != null && !string.IsNullOrEmpty(DataItem.Command.Sql))
        {
            string sql = DataItem.Command.Sql;
            if (sql.Contains("{"))
            {
                if (searchId != null)
                {
                    if (!UserValues.ContainsKey("search_id"))
                        UserValues.Add("search_id", StringManager.ClearText(searchId));
                }
                else
                {
                    if (!UserValues.ContainsKey("search_id"))
                        UserValues.Add("search_id", null);
                }
                
                sql = ExpressionsService.ParseExpression(sql, PageState, false, FormValues, UserValues);
            }


            var dt = EntityRepository.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                var item = new DataItemValue
                {
                    Id = row[0].ToString(),
                    Description = row[1].ToString()?.Trim()
                };
                if (DataItem.ShowImageLegend)
                {
                    item.Icon = (IconType)int.Parse(row[2].ToString() ?? string.Empty);
                    item.ImageColor = row[3].ToString();
                }

                values.Add(item);
            }
        }
        else
        {
            values = DataItem.Items;
        }


        return values;
    }


    private bool IsManualValues()
    {
        if (DataItem?.Items == null)
            return false;

        return DataItem.Items.Count > 0;
    }
}