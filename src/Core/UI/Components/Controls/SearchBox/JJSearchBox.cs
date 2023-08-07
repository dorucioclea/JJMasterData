﻿using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Web.Html;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JJMasterData.Commons.Configuration;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.UI.Components.Abstractions;
using JJMasterData.Core.Web.Factories;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.Web.Components;

/// <summary>
/// Represents a searchable combobox.
/// </summary>
public class JJSearchBox : JJAsyncBaseControl
{
    private JJMasterDataEncryptionService EncryptionService { get; }
    private JJMasterDataUrlHelper UrlHelper { get; }

    #region "Events"

    /// <summary>
    /// Evento disparado para recuperar os registros com parte do texto digitado
    /// </summary>  
    public event EventHandler<SearchBoxQueryEventArgs> OnSearchQuery;

    /// <summary>
    /// Evento disparado para recuperar a descrição com base no Id
    /// </summary>
    public event EventHandler<SearchBoxItemEventArgs> OnSearchId;

    #endregion

    #region "Properties"

    private const string NumberOfItemsAttribute = "numberofitems";
    private const string ScrollbarAttribute = "scrollbar";
    private const string TriggerLengthAttribute = "triggerlength";

    private IEnumerable<DataItemValue> _values;
    private FormElementDataItem _dataItem;
    private string _selectedValue;
    private string _text;
    private string _fieldName;

    internal string FieldName
    {
        get
        {
            if (_fieldName == null)
                return Name;

            return _fieldName;
        }
        set => _fieldName = value;
    }

    internal string DictionaryName { get; set; }

    internal string Id => Name.Replace(".", "_").Replace("[", "_").Replace("]", "_");


    public new string Text
    {
        get
        {
            if (AutoReloadFormFields && _text == null && CurrentContext.IsPost)
            {
                _text = CurrentContext.Request[Name];
            }

            return _text;
        }
        set => _text = value;
    }

    /// <summary>
    /// Quantidade minima de caracteres digitado para disparar a pesquisa
    /// (Default = 1)
    /// </summary>
    public int TriggerLength
    {
        get
        {
            if (Attributes.TryGetValue(TriggerLengthAttribute, out var attribute))
                return int.Parse(attribute.ToString());
            return 0;
        }
        set => Attributes[TriggerLengthAttribute] = value;
    }

    /// <summary>
    /// Numero máximo de itens que será exibido na lista de pesquisa
    /// (Default = 10)
    /// </summary>
    public int NumberOfItems
    {
        get
        {
            if (Attributes.TryGetValue(NumberOfItemsAttribute, out var attribute))
                return int.Parse(attribute.ToString());
            return 0;
        }
        set => Attributes[NumberOfItemsAttribute] = value;
    }

    /// <summary>
    /// Exibir barra de rolagem na lista de pesquisa
    /// (Default = false)
    /// </summary>
    /// <remarks>
    /// Used to show scrollBar when there are too many match records and it's set to True.
    /// If this option is set to true, the NumberOfItems value will be 100 and the HTML render menu will be set to:
    /// <code lang="html">
    /// <ul class="typeahead dropdown-menu" style="max-height:220px;overflow:auto;"></ul>
    /// </code>
    /// </remarks>
    public bool ScrollBar
    {
        get =>
            Attributes.ContainsKey(ScrollbarAttribute) &&
            Attributes[ScrollbarAttribute].ToString().Equals("true");
        set
        {
            string v = value ? "true" : "false";
            if (Attributes.ContainsKey(ScrollbarAttribute))
                Attributes[ScrollbarAttribute] = v;
            else
                Attributes.Add(ScrollbarAttribute, v);
        }
    }

    /// <summary>
    /// Id correspondente ao texto pesquisado
    /// </summary>
    public async Task<string> GetSelectedValueAsync()
    {
        if (AutoReloadFormFields && string.IsNullOrEmpty(_selectedValue) && CurrentContext.IsPost)
        {
            _selectedValue = CurrentContext.Request[Name];
        }

        if (string.IsNullOrEmpty(_selectedValue) && !string.IsNullOrEmpty(Text))
        {
            var list = await GetValuesAsync(Text).ToListAsync();
            if (list?.Count == 1)
                _selectedValue = list.First().Id;
        }

        return _selectedValue;
    }

    /// <summary>
    /// Origem dos dados
    /// </summary>
    public FormElementDataItem DataItem
    {
        get => _dataItem ??= new FormElementDataItem();
        set => _dataItem = value;
    }

    /// <summary>
    /// Ao recarregar o painel, manter os valores digitados no formuário
    /// (Default=True)
    /// </summary>
    public bool AutoReloadFormFields { get; set; }

    public IDataItemService DataItemService { get; } 
    
    public SearchBoxContext Context { get; }

    public string SelectedValue
    {
        set => _selectedValue = value;
    }

    #endregion

    #region "Constructors"

    public JJSearchBox(
        IHttpContext httpContext,
        JJMasterDataEncryptionService encryptionService,
        IDataItemService dataItemService,
        JJMasterDataUrlHelper urlHelper) : base(httpContext)
    {
        EncryptionService = encryptionService;
        UrlHelper = urlHelper;
        DataItemService = dataItemService;
        Enabled = true;
        TriggerLength = 1;
        PlaceHolder = "Search...";
        NumberOfItems = 10;
        ScrollBar = false;
        AutoReloadFormFields = true;
        Name = "jjsearchbox1";
        Context = new(null,UserValues,PageState.List);
    }

    public JJSearchBox(
        FormStateData expOptions, 
        IHttpContext httpContext,
        JJMasterDataEncryptionService encryptionService,
        IDataItemService dataItemService,
        JJMasterDataUrlHelper urlHelper) : this(httpContext, encryptionService, dataItemService,urlHelper)
    {
        Context = new(expOptions.FormValues,expOptions.UserValues,expOptions.PageState);
        UserValues = expOptions.UserValues;
    }

    #endregion

    protected override Task<HtmlBuilder> RenderHtmlAsync()
    {
#if NET48
        if (IsSearchBoxRoute(this, JJService.Provider.GetScopedDependentService<IHttpContext>()))
        {
            ResponseJson();
            return null;
        }
#endif
        return GetSearchBoxHtml();
    }


    public static bool IsSearchBoxRoute(JJBaseView view, IHttpContext httpContext)
    {
        string requestType = httpContext.Request.QueryString("t");
        return "jjsearchbox".Equals(requestType);
    }


    public static HtmlBuilder ResponseJson(JJDataPanel view, IHttpContext httpContext)
    {
        return ResponseJson(view, view.FormElement, view.Values, httpContext, view.ControlFactory.GetFactory<SearchBoxFactory>());
    }

    internal static HtmlBuilder ResponseJson(
        JJBaseView view,
        FormElement formElement,
        IDictionary<string, dynamic> formValues, 
        IHttpContext httpContext,
        SearchBoxFactory searchBoxFactory
        )
    {
        string dictionaryName = httpContext.Request.QueryString("dictionaryName");
        string fieldName = httpContext.Request.QueryString("fieldName");
        var pageState = (PageState)int.Parse(httpContext.Request.QueryString("pageState"));
        
        if (!formElement.Name.Equals(dictionaryName))
            return null;

        var field = formElement.Fields[fieldName];
        var expOptions = new FormStateData(formValues, view.UserValues, pageState);
        
        var searchBox = searchBoxFactory.Create(formElement,field, new(expOptions, view.Name, dictionaryName));
        searchBox.ResponseJson();

        return null;
    }


    private async Task<HtmlBuilder> GetSearchBoxHtml()
    {
        if (DataItem == null)
            throw new ArgumentException("[DataItem] property not set");

        var selectedValue = await GetSelectedValueAsync();
        
        var div = new HtmlBuilder(HtmlTag.Div);
        await div.AppendAsync(HtmlTag.Input, async input =>
        {
            input.WithAttribute("id", Id + "_text");
            input.WithAttribute("name", Name + "_text");
            input.WithAttribute("jjid", Name);
            input.WithAttribute("type", "text");
            input.WithAttribute("urltypehead", GetUrl());
            input.WithAttribute("autocomplete", "off");
            input.WithAttributeIf(MaxLength > 0, "maxlength", MaxLength.ToString());
            input.WithAttributeIf(DataItem.ShowImageLegend, "showimagelegend", "true");
            input.WithAttributeIf(ReadOnly, "readonly", "readonly");
            input.WithAttributeIf(!Enabled, "disabled", "disabled");
            input.WithAttributes(Attributes);
            input.WithToolTip(ToolTip);
            input.WithCssClass("form-control jjsearchbox");
            input.WithCssClassIf(string.IsNullOrEmpty(selectedValue), "jj-icon-search");
            input.WithCssClassIf(!string.IsNullOrEmpty(selectedValue), "jj-icon-success");
            input.WithCssClass(CssClass);

            string description = Text;
            if (string.IsNullOrEmpty(description) && !string.IsNullOrEmpty(selectedValue))
                description = await GetDescriptionAsync(selectedValue);

            input.WithAttribute("value", description);

        });
        div.Append(HtmlTag.Input, input =>
        {
            input.WithAttribute("hidden", "hidden");
            input.WithAttribute("id", Id);
            input.WithAttribute("name", Name);
            input.WithValue(selectedValue);
        });

        return div;
    }

    private string GetUrl()
    {
        var url = new StringBuilder();
        if (IsExternalRoute)
        {
            string dictionaryNameEncrypted = EncryptionService.EncryptStringWithUrlEscape(DictionaryName);
            url.Append(UrlHelper.GetUrl("GetItems","Search", new
            {
                dictionaryName = dictionaryNameEncrypted, 
                fieldName = FieldName,
                fieldSearchName = Name + "_text",
                pageState = (int)Context.PageState,
                Area="MasterData"
            }));
        }
        else
        {
            url.Append("?t=jjsearchbox");
            url.Append($"&dictionaryName={DictionaryName}");
            url.Append($"&fieldName={FieldName}");
            url.Append($"&fieldSearchName={Name + "_text"}");
            url.Append($"&pageState={(int)Context.PageState}");
        }



        return url.ToString();
    }

    /// <summary>
    /// Recupera descrição com base no id
    /// </summary>
    /// <param name="idSearch">Id a ser pesquisado</param>
    /// <returns>Retorna descrição referente ao id</returns>
    public async Task<string> GetDescriptionAsync(string idSearch)
    {
        string description = null;
        if (OnSearchQuery != null)
        {
            var args = new SearchBoxItemEventArgs(idSearch);
            OnSearchId?.Invoke(this, args);
            description = args.ResultText;
        }
        else
        {
            _values ??= await DataItemService.GetValuesAsync(DataItem,null, idSearch, Context).ToListAsync();
        }

        var item = _values?.ToList().Find(x => x.Id.Equals(idSearch));

        if (item != null)
            description = item.Description;

        return description;
    }

    /// <summary>
    /// Recover values from the given text.
    /// </summary>
    public async IAsyncEnumerable<DataItemValue> GetValuesAsync(string searchText)
    {
        if (OnSearchQuery != null)
        {
            var args = new SearchBoxQueryEventArgs(searchText);
            OnSearchQuery.Invoke(this, args);
            foreach (var value in args.Values)
            {
                yield return value;
            }
        }
        else
        {
            await foreach (var value in DataItemService.GetValuesAsync(DataItem, searchText, null, Context))
            {
                yield return value;
            }
        }
    }

    public void ResponseJson()
    {
        if (!FieldName.Equals(CurrentContext.Request.QueryString("fieldName")))
            return;

        string json = JsonConvert.SerializeObject(GetSearchBoxItemsAsync());

        CurrentContext.Response.SendResponse(json, "application/json");

    }

    public async Task<List<DataItemResult>> GetSearchBoxItemsAsync()
    {
        string componentName = CurrentContext.Request.QueryString("fieldName");
        string textSearch = CurrentContext.Request.Form(componentName);

        var values = await GetValuesAsync(textSearch).ToListAsync();
        var items = DataItemService.GetItems(DataItem, values);

        return items.ToList();
    }


}