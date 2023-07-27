﻿using JJMasterData.Commons.Configuration;
using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Actions;
using JJMasterData.Core.DataDictionary.Actions.Abstractions;
using JJMasterData.Core.DataDictionary.Actions.FormToolbar;
using JJMasterData.Core.DataDictionary.Actions.GridTable;
using JJMasterData.Core.DataDictionary.Actions.GridToolbar;
using JJMasterData.Core.DataDictionary.Actions.UserCreated;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Extensions;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Web.Factories;
using JJMasterData.Core.Web.Html;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.UI.Components.GridView;

namespace JJMasterData.Core.Web.Components;

/// <summary>
/// Represents a CRUD.
/// </summary>
/// <example>
/// [!code-cshtml[Example](../../../example/JJMasterData.WebExample/Pages/Components/JJFormViewExample.cshtml)]
/// The GetHtml method will return something like this:
/// <img src="../media/JJFormViewExample.png"/>
/// </example>
public class JJFormView : JJAsyncBaseView
{
    #region "Events"

    public event EventHandler<FormBeforeActionEventArgs> OnBeforeInsert;
    public event EventHandler<FormBeforeActionEventArgs> OnBeforeUpdate;
    public event EventHandler<FormBeforeActionEventArgs> OnBeforeDelete;
    public event EventHandler<FormAfterActionEventArgs> OnAfterInsert;
    public event EventHandler<FormAfterActionEventArgs> OnAfterUpdate;
    public event EventHandler<FormAfterActionEventArgs> OnAfterDelete;

    #endregion

    #region "Properties"

    private JJDataPanel _dataPanel;
    private JJGridView _gridView;
    private ActionMap _currentActionMap;
    private JJAuditLogView _auditLogView;
    private JJDataImp _dataImp;
    private string _userId;

    internal JJAuditLogView AuditLogView =>
        _auditLogView ??= AuditLogViewFactory.Value.CreateAuditLogView(FormElement);

    /// <summary>
    /// Url a ser direcionada após os eventos de Update/Delete/Save
    /// </summary>
    internal string UrlRedirect { get; set; }


    /// <summary>
    /// Id do usuário Atual
    /// </summary>
    /// <remarks>
    /// Se a variavel não for atribuida diretamente,
    /// o sistema tenta recuperar em UserValues ou nas variaveis de Sessão
    /// </remarks>
    internal string UserId => _userId ??= DataHelper.GetCurrentUserId(CurrentContext, UserValues);

    /// <summary>
    /// Configurações de importação
    /// </summary>
    public JJDataImp DataImp
    {
        get
        {
            if (_dataImp != null)
                return _dataImp;

            _dataImp = _gridView.DataImp;
            _dataImp.OnAfterDelete += OnAfterDelete;
            _dataImp.OnAfterInsert += OnAfterInsert;
            _dataImp.OnAfterUpdate += OnAfterUpdate;

            return _dataImp;
        }
    }

    /// <summary>
    /// Configuração do painel com os campos do formulário
    /// </summary>
    public JJDataPanel DataPanel
    {
        get
        {
            _dataPanel ??= DataPanelFactory.Value.CreateDataPanel(FormElement);
            _dataPanel.Name = "jjpanel_" + FormElement.Name.ToLower();
            _dataPanel.UserValues = UserValues;
            _dataPanel.RenderPanelGroup = true;
            _dataPanel.IsExternalRoute = IsExternalRoute;

            _dataPanel.PageState = PageState;

            return _dataPanel;
        }
    }

    /// <summary>
    /// Values to be replaced by relationship.
    /// If the field name exists in the relationship, the value will be replaced
    /// </summary>
    /// <remarks>
    /// Key = Field name, Value=Field value
    /// </remarks>
    public IDictionary<string, dynamic> RelationValues { get; set; }

    public FormElement FormElement { get; set; }

    public JJGridView GridView
    {
        get
        {
            if (_gridView is not null)
                return _gridView;

            _gridView = GridViewFactory.Value.CreateGridView(FormElement);
            _gridView.Name = Name.ToLower();
            _gridView.FormElement = FormElement;
            _gridView.UserValues = UserValues;
            _gridView.IsExternalRoute = IsExternalRoute;
            _gridView.ShowTitle = true;

            return _gridView;
        }
    }

    /// <summary>
    /// Estado atual da pagina
    /// </summary>
    private PageState _pageState;

    public PageState PageState
    {
        get
        {
            if (CurrentContext.Request["current_pagestate_" + Name] != null)
                _pageState = (PageState)int.Parse(CurrentContext.Request["current_pagestate_" + Name]);

            return _pageState;
        }
        internal set => _pageState = value;
    }

    private ActionMap CurrentActionMap
    {
        get
        {
            if (_currentActionMap != null) return _currentActionMap;

            string encryptedActionMap = CurrentContext.Request["current_formaction_" + Name];
            if (string.IsNullOrEmpty(encryptedActionMap))
                return null;

            _currentActionMap = EncryptionService.DecryptActionMap(encryptedActionMap);
            return _currentActionMap;
        }
    }


    public DeleteSelectedRowsAction DeleteSelectedRowsAction
        => (DeleteSelectedRowsAction)GridView.ToolBarActions.Find(x => x is DeleteSelectedRowsAction);

    public InsertAction InsertAction => (InsertAction)GridView.ToolBarActions.Find(x => x is InsertAction);

    public EditAction EditAction => (EditAction)GridView.GridActions.Find(x => x is EditAction);

    public DeleteAction DeleteAction => (DeleteAction)GridView.GridActions.Find(x => x is DeleteAction);

    public ViewAction ViewAction => (ViewAction)GridView.GridActions.Find(x => x is ViewAction);

    public LogAction LogAction => (LogAction)GridView.ToolBarActions.Find(x => x is LogAction);


    public bool ShowTitle { get; set; }

    internal bool IsModal { get; set; }

    internal IHttpContext CurrentContext { get; }
    internal IEntityRepository EntityRepository { get; }
    internal Lazy<AuditLogViewFactory> AuditLogViewFactory { get; }
    internal Lazy<GridViewFactory> GridViewFactory { get; }
    internal Lazy<DataPanelFactory> DataPanelFactory { get; }
    internal FileDownloaderFactory FileDownloaderFactory { get; }
    internal FormViewFactory FormViewFactory { get; }
    internal JJMasterDataEncryptionService EncryptionService { get; }
    internal IFieldValuesService FieldValuesService { get; }
    internal IExpressionsService ExpressionsService { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }
    internal IDataDictionaryRepository DataDictionaryRepository { get; }
    internal IFormService FormService { get; }

    #endregion

    #region "Constructors"

#if NET48
    public JJFormView()
    {
        CurrentContext = JJService.Provider.GetScopedDependentService<IHttpContext>();
        EntityRepository = JJService.Provider.GetScopedDependentService<IEntityRepository>();
        AuditLogViewFactory = JJService.Provider.GetScopedDependentService<Lazy<AuditLogViewFactory>>();
        GridViewFactory = JJService.Provider.GetScopedDependentService<Lazy<GridViewFactory>>();
        DataPanelFactory = JJService.Provider.GetScopedDependentService<Lazy<DataPanelFactory>>();
        FormViewFactory = JJService.Provider.GetScopedDependentService<FormViewFactory>();
        FormService = JJService.Provider.GetScopedDependentService<IFormService>();
        EncryptionService = JJService.Provider.GetScopedDependentService<JJMasterDataEncryptionService>();
        FieldValuesService = JJService.Provider.GetScopedDependentService<IFieldValuesService>();
        ExpressionsService = JJService.Provider.GetScopedDependentService<IExpressionsService>();
        StringLocalizer = JJService.Provider.GetScopedDependentService<IStringLocalizer<JJMasterDataResources>>();
        DataDictionaryRepository = JJService.Provider.GetScopedDependentService<IDataDictionaryRepository>();
        FileDownloaderFactory = JJService.Provider.GetScopedDependentService<FileDownloaderFactory>();
    }

    public JJFormView(string elementName) : this()
    {
        var dataDictionaryRepository = JJService.Provider.GetScopedDependentService<IDataDictionaryRepository>();
        var factory = JJService.Provider.GetScopedDependentService<FormViewFactory>();
        FormElement = dataDictionaryRepository.GetMetadata(elementName);
        IsExternalRoute = false;
        factory.SetFormViewParams(this,FormElement);
    }
    
    public JJFormView(FormElement formElement) : this()
    {
        IsExternalRoute = false;
        FormElement = formElement;
    }
#endif

    internal JJFormView(
        IHttpContext currentContext,
        IEntityRepository entityRepository,
        IDataDictionaryRepository dataDictionaryRepository,
        IFormService formService,
        JJMasterDataEncryptionService encryptionService,
        IFieldValuesService fieldValuesService,
        IExpressionsService expressionsService,
        IStringLocalizer<JJMasterDataResources> stringLocalizer,
        FileDownloaderFactory fileDownloaderFactory,
        Lazy<GridViewFactory> gridViewFactory,
        Lazy<AuditLogViewFactory> auditLogViewFactory,
        Lazy<DataPanelFactory> dataPanelFactory,
        FormViewFactory formViewFactory)
    {
        CurrentContext = currentContext;
        EntityRepository = entityRepository;
        AuditLogViewFactory = auditLogViewFactory;
        GridViewFactory = gridViewFactory;
        FormViewFactory = formViewFactory;
        FileDownloaderFactory = fileDownloaderFactory;
        FormService = formService;
        EncryptionService = encryptionService;
        FieldValuesService = fieldValuesService;
        ExpressionsService = expressionsService;
        StringLocalizer = stringLocalizer;
        DataDictionaryRepository = dataDictionaryRepository;
        DataPanelFactory = dataPanelFactory;
        Name = "jjview";
    }

    internal JJFormView(
        FormElement formElement,
        IHttpContext currentContext,
        IEntityRepository entityRepository,
        IDataDictionaryRepository dataDictionaryRepository,
        IFormService formService,
        JJMasterDataEncryptionService encryptionService,
        IFieldValuesService fieldValuesService,
        IExpressionsService expressionsService,
        IStringLocalizer<JJMasterDataResources> stringLocalizer,
        FileDownloaderFactory fileDownloaderFactory,
        Lazy<GridViewFactory> gridViewFactory,
        Lazy<AuditLogViewFactory> auditLogViewFactory,
        Lazy<DataPanelFactory> dataPanelFactory,
        FormViewFactory formViewFactory) : this(currentContext, entityRepository, dataDictionaryRepository, formService,
        encryptionService, fieldValuesService, expressionsService, stringLocalizer,fileDownloaderFactory, gridViewFactory,
        auditLogViewFactory, dataPanelFactory, formViewFactory )
    {
        Name = "jjview_" + formElement.Name.ToLower();
        FormElement = formElement;
    }

    #endregion

    internal override HtmlBuilder RenderHtml()
    {
        return RenderHtmlAsync().GetAwaiter().GetResult();
    }
    
    protected override async Task<HtmlBuilder> RenderHtmlAsync()
    {
        var requestType = CurrentContext.Request.QueryString("t");
        var objName = CurrentContext.Request.QueryString("objname");
        var dataPanel = DataPanel;

        if (JJLookup.IsLookupRoute(this, CurrentContext))
            return dataPanel.RenderHtml();

        if (JJTextFile.IsFormUploadRoute(this, CurrentContext))
            return dataPanel.RenderHtml();

        if (JJFileDownloader.IsDownloadRoute(CurrentContext))
            return JJFileDownloader.ResponseRoute(CurrentContext,EncryptionService,FileDownloaderFactory);

        if (JJSearchBox.IsSearchBoxRoute(this, CurrentContext))
            return JJSearchBox.ResponseJson(DataPanel, CurrentContext);

        if ("reloadpainel".Equals(requestType))
        {
            var panelHtml = await GetReloadPanelHtmlAsync();
#pragma warning disable CS0618
            CurrentContext.Response.SendResponse(panelHtml);
#pragma warning restore CS0618
            return null;
        }

        if ("jjupload".Equals(requestType) || "ajaxdataimp".Equals(requestType))
        {
            if (!DataImp.Upload.Name.Equals(objName))
                return null;
            
            GetHtmlDataImp();
        }
        else if ("geturlaction".Equals(requestType))
        {
            dataPanel.ResponseUrlAction().GetAwaiter().GetResult();
            return null;
        }

        var htmlForm = await GetHtmlForm();

        if ("ajax".Equals(requestType) && Name.Equals(objName))
        {
            CurrentContext.Response.SendResponse(htmlForm.ToString());
            return null;
        }

        return htmlForm;
    }

    internal async Task<string> GetReloadPanelHtmlAsync()
    {
        var filter = GridView.GetSelectedRowId();
        IDictionary<string, dynamic> values = null;
        if (filter is { Count: > 0 })
            values = await EntityRepository.GetDictionaryAsync(FormElement, filter);

        string htmlPanel = GetDataPanelHtml(new(values, null, PageState), true).ToString();
        return htmlPanel;
    }

    private async Task<HtmlBuilder> GetHtmlForm()
    {
        HtmlBuilder html;

        var actionMap = CurrentActionMap;
        var currentAction = GridView.GetCurrentAction(actionMap);

        if (currentAction is EditAction || PageState is PageState.Update)
        {
            html = await GetHtmlUpdate();
        }
        else if (currentAction is InsertAction || PageState is PageState.Insert)
        {
            html = await GetHtmlInsert();
        }
        else if (currentAction is ImportAction || PageState is PageState.Import)
        {
            html = GetHtmlDataImp();
        }
        else if (currentAction is LogAction || PageState is PageState.Log)
        {
            html = GetHtmlLog();
        }
        else if (currentAction is DeleteAction)
        {
            html = await GetHtmlDelete();
        }
        else if (currentAction is DeleteSelectedRowsAction)
        {
            html = await GetHtmlDeleteSelectedRows();
        }
        else if (currentAction is ViewAction || PageState is PageState.View)
        {
            html = await GetHtmlView();
        }
        else
        {
            html = GetGridHtml();
        }

        if (html != null)
        {
            html.AppendHiddenInput($"current_pagestate_{Name.ToLower()}", ((int)PageState).ToString());
            html.AppendHiddenInput($"current_formaction_{Name.ToLower()}", "");
        }

        return html;
    }

    private HtmlBuilder GetGridHtml()
    {
        return GridView.RenderHtml();
    }


    private async Task<HtmlBuilder> GetHtmlUpdate()
    {
        string formAction = "";

        if (CurrentContext.Request["current_painelaction_" + Name] != null)
            formAction = CurrentContext.Request["current_painelaction_" + Name];

        if ("OK".Equals(formAction))
        {
            var values = await GetFormValuesAsync();
            var errors = UpdateFormValuesAsync(values).GetAwaiter().GetResult();

            if (errors.Count == 0)
            {
                if (!string.IsNullOrEmpty(UrlRedirect))
                {
                    CurrentContext.Response.Redirect(UrlRedirect);
                    return null;
                }

                PageState = PageState.List;
                return GetGridHtml();
            }

            PageState = PageState.Update;
            return GetDataPanelHtml(new(values, errors, PageState), true);
        }

        if ("CANCEL".Equals(formAction))
        {
            PageState = PageState.List;
            return GetGridHtml();
        }

        if ("REFRESH".Equals(formAction))
        {
            var values = await GetFormValuesAsync();
            return GetDataPanelHtml(new(values, null, PageState), true);
        }
        else
        {
            bool autoReloadFields;
            IDictionary<string, dynamic> values;
            if (PageState is PageState.Update)
            {
                autoReloadFields = true;
                values = await GetFormValuesAsync();
            }
            else
            {
                autoReloadFields = false;
                var acMap = CurrentActionMap;
                values = EntityRepository.GetDictionaryAsync(FormElement, acMap.PkFieldValues).GetAwaiter().GetResult();
            }

            PageState = PageState.Update;
            return GetDataPanelHtml(new(values, null, PageState), autoReloadFields);
        }
    }
    
    private async Task<HtmlBuilder> GetHtmlInsert()
    {
        var action = InsertAction;
        bool isVisible = ExpressionsService.GetBoolValue(action.VisibleExpression, action.Name, PageState.List,
            RelationValues);
        if (!isVisible)
            throw new UnauthorizedAccessException(StringLocalizer["Insert action not enabled"]);

        string formAction = "";

        if (CurrentContext.Request["current_painelaction_" + Name] != null)
            formAction = CurrentContext.Request["current_painelaction_" + Name];

        if (formAction.Equals("OK"))
        {
            var values = await GetFormValuesAsync();
            var errors = await InsertFormValuesAsync(values);

            if (errors.Count == 0)
            {
                if (!string.IsNullOrEmpty(UrlRedirect))
                {
                    CurrentContext.Response.Redirect(UrlRedirect);
                    return null;
                }

                if (action.ReopenForm)
                {
                    PageState = PageState.Insert;

                    var alert = new JJAlert
                    {
                        Name = $"pnl_insertmsg_{Name}",
                        Color = PanelColor.Success,
                        ShowIcon = true,
                        Icon = IconType.CheckCircleO
                    };
                    alert.Messages.Add(StringLocalizer["Record added successfully"]);
                    var alertHtml = alert.GetHtmlBuilder();
                    alertHtml.AppendElement(HtmlTag.Div, div =>
                    {
                        div.WithAttribute("id", $"pnl_insert_{Name}")
                            .WithAttribute("style", "display:none")
                            .AppendElement(GetDataPanelHtml(new(RelationValues, null, PageState.Insert), false));
                    });
                    alertHtml.AppendScript($"jjview.showInsertSucess('{Name}');");
                    return alertHtml;
                }

                PageState = PageState.List;
                return GetGridHtml();
            }

            PageState = PageState.Insert;
            return GetDataPanelHtml(new(values, errors, PageState), true);
        }

        if (formAction.Equals("CANCEL"))
        {
            PageState = PageState.List;
            ClearTempFiles();
            return GetGridHtml();
        }

        if (formAction.Equals("ELEMENTSEL"))
        {
            return await GetHtmlElementInsert();
        }

        if (formAction.Equals("ELEMENTLIST"))
        {
            PageState = PageState.Insert;
            return GetHtmlElementList(action);
        }

        if (PageState == PageState.Insert)
        {
            var formValues = await GetFormValuesAsync();
            return GetDataPanelHtml(new(formValues, null, PageState), true);
        }

        PageState = PageState.Insert;

        if (string.IsNullOrEmpty(action.ElementNameToSelect))
            return GetDataPanelHtml(new(RelationValues, null, PageState.Insert), false);
        return GetHtmlElementList(action);
    }

    private HtmlBuilder GetHtmlElementList(InsertAction action)
    {
        var sHtml = new HtmlBuilder(HtmlTag.Div);
        sHtml.AppendHiddenInput($"current_painelaction_{Name}", "ELEMENTLIST");
        sHtml.AppendHiddenInput($"current_selaction_{Name}", "");

        var formElement = DataDictionaryRepository.GetMetadata(action.ElementNameToSelect);
        var selectedForm = FormViewFactory.CreateFormView(formElement);
        selectedForm.UserValues = UserValues;
        selectedForm.Name = action.ElementNameToSelect;
        selectedForm.SetOptions(formElement.Options);

        var goBackScript = new StringBuilder();
        goBackScript.Append($"$('#current_pagestate_{Name}').val('{((int)PageState.List).ToString()}'); ");
        goBackScript.AppendLine("$('form:first').submit(); ");

        var goBackAction = new ScriptAction
        {
            Name = "_jjgobacktion",
            Icon = IconType.ArrowLeft,
            Text = "Back",
            ShowAsButton = true,
            OnClientClick = goBackScript.ToString(),
            IsDefaultOption = true
        };
        selectedForm.GridView.AddToolBarAction(goBackAction);

        var selAction = new ScriptAction
        {
            Name = "_jjselaction",
            Icon = IconType.CaretRight,
            ToolTip = "Select",
            IsDefaultOption = true
        };
        selectedForm.GridView.AddGridAction(selAction);

        sHtml.AppendElement(selectedForm);

        return sHtml;
    }
    
    private async Task<HtmlBuilder> GetHtmlElementInsert()
    {
        string encryptedActionMap = CurrentContext.Request.Form("current_selaction_" + Name);
        var actionMap = EncryptionService.DecryptActionMap(encryptedActionMap);
        var html = new HtmlBuilder(HtmlTag.Div);
        var formElement = await DataDictionaryRepository.GetMetadataAsync(InsertAction.ElementNameToSelect);
        var selValues = await EntityRepository.GetDictionaryAsync(formElement, actionMap.PkFieldValues);
        var values = FieldValuesService.MergeWithExpressionValues(formElement, selValues, PageState.Insert, true);
        var erros = await InsertFormValuesAsync(values, false);

        if (erros.Count > 0)
        {
            var sMsg = new StringBuilder();
            foreach (string err in erros.Values)
            {
                sMsg.Append(" - ");
                sMsg.Append(err);
                sMsg.Append("<br>");
            }

            html.AppendElement(new JJMessageBox(sMsg.ToString(), MessageIcon.Warning));
            html.AppendElement(GetHtmlElementList(InsertAction));
            PageState = PageState.Insert;
        }
        else
        {
            PageState = PageState.Update;
            html.AppendElement(GetDataPanelHtml(new(values, null, PageState), false));
        }

        return html;
    }

    private async Task<HtmlBuilder> GetHtmlView()
    {
        var acMap = CurrentActionMap;
        if (acMap == null)
        {
            PageState = PageState.List;
            return GetGridHtml();
        }

        PageState = PageState.View;
        var filter = acMap.PkFieldValues;
        var values = await EntityRepository.GetDictionaryAsync(FormElement, filter);
        return GetDataPanelHtml(new(values, null, PageState), false);
    }
    
    private async Task<HtmlBuilder> GetHtmlDelete()
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        try
        {
            var acMap = CurrentActionMap;
            var filter = acMap.PkFieldValues;

            var errors = await DeleteFormValuesAsync(filter);

            if (errors != null && errors.Count > 0)
            {
                var errorMessage = new StringBuilder();
                foreach (var err in errors)
                {
                    errorMessage.Append("- ");
                    errorMessage.Append(err.Value);
                    errorMessage.AppendLine("<br>");
                }

                html.AppendElement(new JJMessageBox(errorMessage.ToString(), MessageIcon.Warning));
            }
            else
            {
                if (GridView.EnableMultiSelect)
                    GridView.ClearSelectedGridValues();
            }
        }
        catch (Exception ex)
        {
            html.AppendElement(new JJMessageBox(ex.Message, MessageIcon.Error));
        }

        if (!string.IsNullOrEmpty(UrlRedirect))
        {
            CurrentContext.Response.Redirect(UrlRedirect);
            return null;
        }

        html.AppendElement(GetGridHtml());
        PageState = PageState.List;

        return html;
    }


    private async Task<HtmlBuilder> GetHtmlDeleteSelectedRows()
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        var errorMessage = new StringBuilder();
        int errorCount = 0;
        int successCount = 0;

        try
        {
            var rows = GridView.GetSelectedGridValues();

            foreach (var row in rows)
            {
                var errors = await DeleteFormValuesAsync(row);

                if (errors.Count > 0)
                {
                    foreach (var err in errors)
                    {
                        errorMessage.Append(" - ");
                        errorMessage.Append(err.Value);
                        errorMessage.Append("<br>");
                    }

                    errorCount++;
                }
                else
                {
                    successCount++;
                }
            }

            if (rows.Count > 0)
            {
                var message = new StringBuilder();
                var icon = MessageIcon.Info;
                if (successCount > 0)
                {
                    message.Append("<p class=\"text-success\">");
                    message.Append(StringLocalizer["{0} Record(s) deleted successfully", successCount]);
                    message.Append("</p><br>");
                }

                if (errorCount > 0)
                {
                    message.Append("<p class=\"text-danger\">");
                    message.Append(StringLocalizer["{0} Record(s) with error", successCount]);
                    message.Append(StringLocalizer["Details:"]);
                    message.Append("<br>");
                    message.Append(errorMessage);
                    icon = MessageIcon.Warning;
                }

                html.AppendElement(new JJMessageBox(message.ToString(), icon));

                GridView.ClearSelectedGridValues();
            }
        }
        catch (Exception ex)
        {
            html.AppendElement(new JJMessageBox(ex.Message, MessageIcon.Error));
        }
        finally
        {
            html.AppendElement(GetGridHtml());
            PageState = PageState.List;
        }

        return html;
    }


    private HtmlBuilder GetHtmlLog()
    {
        var actionMap = _currentActionMap;
        var script = new StringBuilder();
        script.Append($"$('#current_pagestate_{Name}').val('{(int)PageState.List}'); ");
        script.AppendLine("$('form:first').submit(); ");

        var goBackAction = new ScriptAction
        {
            Name = "goBackAction",
            Icon = IconType.Backward,
            ShowAsButton = true,
            Text = "Back",
            OnClientClick = script.ToString()
        };

        if (PageState == PageState.View)
        {
            var html = AuditLogView.GetDetailLog(actionMap.PkFieldValues);
            html.AppendElement(GetFormLogBottomBar(actionMap.PkFieldValues));
            PageState = PageState.Log;
            return html;
        }

        AuditLogView.GridView.AddToolBarAction(goBackAction);
        AuditLogView.DataPanel = DataPanel;
        PageState = PageState.Log;
        return AuditLogView.GetHtmlBuilder();
    }

    private HtmlBuilder GetHtmlDataImp()
    {
        var action = GridView.ImportAction;
        bool isVisible = ExpressionsService.GetBoolValue(action.VisibleExpression, action.Name, PageState.List,
            RelationValues);
        if (!isVisible)
            throw new UnauthorizedAccessException(StringLocalizer["Import action not enabled"]);

        var html = new HtmlBuilder(HtmlTag.Div);

        if (ShowTitle)
            html.AppendElement(GridView.GetTitle(UserValues));

        PageState = PageState.Import;
        var sScriptImport = new StringBuilder();
        sScriptImport.Append($"$('#current_pagestate_{Name}').val('{(int)PageState.List}'); ");
        sScriptImport.AppendLine("$('form:first').submit(); ");

        var dataImpView = DataImp;
        dataImpView.UserValues = UserValues;
        dataImpView.BackButton.OnClientClick = sScriptImport.ToString();
        dataImpView.ProcessOptions = action.ProcessOptions;
        dataImpView.EnableHistoryLog = LogAction.IsVisible;
        html.AppendElement(dataImpView);

        return html;
    }

    private HtmlBuilder GetDataPanelHtml(FormContext formContext, bool autoReloadFormFields)
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        var relationships = FormElement.Relationships.Where(r => r.ViewType != RelationshipViewType.None || r.IsParent)
            .ToList();

        var (values, errors, pageState) = formContext;

        var parentPanel = DataPanel;
        parentPanel.PageState = pageState;
        parentPanel.Errors = errors;
        parentPanel.Values = values;
        parentPanel.IsExternalRoute = IsExternalRoute;
        parentPanel.AutoReloadFormFields = autoReloadFormFields;

        if (ShowTitle)
            html.AppendElement(GridView.GetTitle(values));

        if (relationships.Count == 0)
        {
            return GetParentPanelHtml(parentPanel);
        }

        var layout = new FormViewRelationshipLayout(this);

        var topActions = FormElement.Options.FormToolbarActions
            .GetAllSorted()
            .Where(a => a.FormToolbarActionLocation is FormToolbarActionLocation.Top).ToList();

        html.AppendElement(GetFormToolbar(topActions, parentPanel.PageState, parentPanel.Values));

        var layoutHtml = layout.GetRelationshipsHtml(parentPanel, relationships);

        html.AppendRange(layoutHtml);

        var bottomActions = FormElement.Options.FormToolbarActions
            .GetAllSorted()
            .Where(a => a.FormToolbarActionLocation is FormToolbarActionLocation.Bottom).ToList();

        html.AppendElement(GetFormToolbar(bottomActions, parentPanel.PageState, parentPanel.Values));

        return html;
    }

    internal HtmlBuilder GetParentPanelHtml(JJDataPanel parentPanel)
    {
        var parentPanelHtml = parentPanel.GetPanelHtml();

        if (parentPanel.Errors != null)
            parentPanelHtml.AppendElement(new JJValidationSummary(parentPanel.Errors));

        var panelActions = parentPanel.FormElement.Options.FormToolbarActions
            .Where(a => a.FormToolbarActionLocation == FormToolbarActionLocation.Panel).ToList();

        var toolbar = GetFormToolbar(panelActions, parentPanel.PageState, parentPanel.Values);

        parentPanelHtml.AppendElement(toolbar);
        parentPanelHtml.AppendHiddenInput($"current_painelaction_{Name}");

        return parentPanelHtml;
    }

    private JJToolbar GetFormLogBottomBar(IDictionary<string, dynamic> values)
    {
        var backScript = new StringBuilder();
        backScript.Append($"$('#current_pagestate_{Name}').val('{(int)PageState.List}'); ");
        backScript.AppendLine("$('form:first').submit(); ");

        var btnBack = GetBackButton();
        btnBack.OnClientClick = backScript.ToString();

        var btnHideLog = GetButtonHideLog(values);

        var toolbar = new JJToolbar
        {
            CssClass = "pb-3 mt-3"
        };
        toolbar.Items.Add(btnBack.GetHtmlBuilder());
        toolbar.Items.Add(btnHideLog.GetHtmlBuilder());
        return toolbar;
    }

    private JJToolbar GetFormToolbar(IList<BasicAction> actions, PageState pageState,
        IDictionary<string, dynamic> values)
    {
        var toolbar = new JJToolbar
        {
            CssClass = "pb-3 mt-3"
        };

        var context = new ActionContext(values, pageState, ActionSource.FormToolbar, null);

        foreach (var action in actions.Where(a => !a.IsGroup))
        {
            if (action is SaveAction saveAction)
            {
                saveAction.EnterKeyBehavior = DataPanel.FormUI.EnterKey;
            }

            toolbar.Items.Add(GridView.ActionManager.GetLinkFormToolbar(action, values, pageState).GetHtmlBuilder());
        }

        if (actions.Any(a => a.IsGroup))
        {

            var btnGroup = new JJLinkButtonGroup
            {
                CaretText = "More"
            };

            foreach (var groupedAction in actions.Where(a => a.IsGroup).ToList())
            {
                btnGroup.ShowAsButton = groupedAction.ShowAsButton;
                btnGroup.Actions.Add(GridView.ActionManager.GetLinkFormToolbar(groupedAction, values, pageState));
            }

            toolbar.Items.Add(btnGroup.GetHtmlBuilder());
        }


        if (pageState == PageState.View)
        {
            if (LogAction.IsVisible)
                toolbar.Items.Add(GetButtonViewLog(values).GetHtmlBuilder());
        }

        return toolbar;
    }

    private void FormSelectedOnRenderAction(object sender, ActionEventArgs e)
    {
        if (!e.Action.Name.Equals("_jjselaction")) return;

        if (sender is not JJGridView grid) 
            return;

        var map = new ActionMap(ActionSource.GridTable, grid.FormElement, e.FieldValues, e.Action.Name);
        string encryptedActionMap = EncryptionService.EncryptActionMap(map);
        e.LinkButton.OnClientClick = $"jjview.doSelElementInsert('{Name}','{encryptedActionMap}');";
    }

    /// <summary>
    /// Insert the records in the database.
    /// </summary>
    /// <returns>The list of errors.</returns>
    public async Task<IDictionary<string, dynamic>> InsertFormValuesAsync(IDictionary<string, dynamic> values,
        bool validateFields = true)
    {
        var result = await FormService.InsertAsync(FormElement, values, new DataContext(CurrentContext, DataContextSource.Form, UserId),
            validateFields);
        UrlRedirect = result.UrlRedirect;
        return result.Errors;
    }

    /// <summary>
    /// Update the records in the database.
    /// </summary>
    /// <returns>The list of errors.</returns>
    public async Task<IDictionary<string, dynamic>> UpdateFormValuesAsync(IDictionary<string, dynamic> values)
    {
        var result = await FormService.UpdateAsync(FormElement, values, new DataContext(CurrentContext, DataContextSource.Form, UserId));
        UrlRedirect = result.UrlRedirect;
        return result.Errors;
    }

    public async Task<IDictionary<string, dynamic>> DeleteFormValuesAsync(IDictionary<string, dynamic> filter)
    {
        var values = FieldValuesService.MergeWithExpressionValues(FormElement, filter, PageState.Delete, true);
        var result = await FormService.DeleteAsync(FormElement, values, new DataContext(CurrentContext, DataContextSource.Form, UserId));
        UrlRedirect = result.UrlRedirect;
        return result.Errors;
    }


    public async Task<IDictionary<string, dynamic>> GetFormValuesAsync()
    {
        var painel = DataPanel;
        var values = await painel.GetFormValuesAsync();

        if (RelationValues == null)
            return values;

        DataHelper.CopyIntoDictionary(ref values, RelationValues, true);

        return values;
    }
    public IDictionary<string, dynamic> ValidateFields(IDictionary<string, dynamic> values, PageState pageState)
    {
        var painel = DataPanel;
        painel.Values = values;

        var errors = painel.ValidateFields(values, pageState);
        return errors;
    }

    private void ClearTempFiles()
    {
        var uploadFields = FormElement.Fields.ToList().FindAll(x => x.Component == FormComponent.File);
        foreach (var field in uploadFields)
        {
            string sessionName = $"{field.Name}_formupload_jjfiles";
            if (CurrentContext?.Session[sessionName] != null)
                CurrentContext.Session[sessionName] = null;
        }
    }

    public void SetOptions(FormElementOptions options)
    {
        FormViewFactory.SetFormOptions(this, options);
    }

    private JJLinkButton GetBackButton()
    {
        var btn = new JJLinkButton
        {
            Type = LinkButtonType.Button,
            CssClass = $"{BootstrapHelper.DefaultButton} btn-small",
            OnClientClick = $"jjview.doPainelAction('{Name}','CANCEL');",
            IconClass = IconType.Times.GetCssClass(),
            Text = "Cancel"
        };
        btn.IconClass = IconType.ArrowLeft.GetCssClass();
        btn.Text = "Back";
        return btn;
    }

    private JJLinkButton GetButtonHideLog(IDictionary<string, dynamic> values)
    {
        string scriptAction = GridView.ActionManager.GetFormActionScript(ViewAction, values, ActionSource.GridTable);
        var btn = new JJLinkButton
        {
            Type = LinkButtonType.Button,
            Text = "Hide Log",
            IconClass = IconType.Film.GetCssClass(),
            CssClass = "btn btn-primary btn-small",
            OnClientClick = $"$('#current_pagestate_{Name}').val('{(int)PageState.List}');{scriptAction}"
        };
        return btn;
    }

    private JJLinkButton GetButtonViewLog(IDictionary<string, dynamic> values)
    {
        string scriptAction = GridView.ActionManager.GetFormActionScript(LogAction, values, ActionSource.GridToolbar);
        var btn = new JJLinkButton
        {
            Type = LinkButtonType.Button,
            Text = "View Log",
            IconClass = IconType.Film.GetCssClass(),
            CssClass = BootstrapHelper.DefaultButton + " btn-small",
            OnClientClick = scriptAction
        };
        return btn;
    }

    #region "Legacy GridView inherited compatibility"
    [Obsolete("Please use GridView.GridActions")]
    public List<BasicAction> GridActions
    {
        get => GridView.GridActions;
        internal set => GridView.GridActions = value;
    }
    [Obsolete("Please use GridView.ToolBarActions")]
    public List<BasicAction> ToolBarActions
    {
        get => GridView.ToolBarActions;
        internal set => GridView.ToolBarActions = value;
    }
    [Obsolete("Please use GridView.SetCurrentFilter")]
    public void SetCurrentFilter(string userid, string userId)
    {
        GridView.SetCurrentFilter(UserId, UserId);
    }
    [Obsolete("Please use GridView.ImportAction")]
    public ImportAction ImportAction => GridView.ImportAction;

    [Obsolete("Please use GridView.FilterAction")]
    public FilterAction FilterAction => GridView.FilterAction;

    [Obsolete("Please use GridView.GetSelectedGridValues")]
    private List<IDictionary<string, dynamic>> GetSelectedGridValues() => GridView.GetSelectedGridValues();

    [Obsolete("Please use GridView.AddToolBarAction")]
    private void AddToolBarAction(UserCreatedAction userCreatedAction)
    {
        switch (userCreatedAction)
        {
            case UrlRedirectAction urlRedirectAction:
                GridView.AddToolBarAction(urlRedirectAction);
                break;
            case SqlCommandAction sqlCommandAction:
                GridView.AddToolBarAction(sqlCommandAction);
                break;
            case ScriptAction scriptAction:
                GridView.AddToolBarAction(scriptAction);
                break;
            case InternalAction internalAction:
                GridView.AddToolBarAction(internalAction);
                break;
        }
    }

    [Obsolete("Please use GridView.AddGridAction")]
    private void AddGridAction(UserCreatedAction userCreatedAction)
    {
        switch (userCreatedAction)
        {
            case UrlRedirectAction urlRedirectAction:
                GridView.AddGridAction(urlRedirectAction);
                break;
            case SqlCommandAction sqlCommandAction:
                GridView.AddGridAction(sqlCommandAction);
                break;
            case ScriptAction scriptAction:
                GridView.AddGridAction(scriptAction);
                break;
            case InternalAction internalAction:
                GridView.AddGridAction(internalAction);
                break;
        }
    }

    [Obsolete("Please use GridView.ClearSelectedGridValues")]
    private void ClearSelectedGridValues()
    {
        GridView.ClearSelectedGridValues();
    }

    [Obsolete("Please use GridView.EnableMultiSelect")]
    public bool EnableMultSelect
    {
        get => GridView.EnableMultiSelect;
        set => GridView.EnableMultiSelect = value;
    }
    #endregion

    public static implicit operator JJGridView(JJFormView formView) => formView.GridView;
    public static implicit operator JJDataPanel(JJFormView formView) => formView.DataPanel;
}