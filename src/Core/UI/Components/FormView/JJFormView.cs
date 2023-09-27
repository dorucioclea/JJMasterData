﻿#nullable enable

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable EventNeverSubscribedTo.Global

using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataManager;
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
using JJMasterData.Commons.Data;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.DataDictionary.Models.Actions;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.Http.Abstractions;
using JJMasterData.Core.Options;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.UI.Components.FormView;
using Microsoft.Extensions.Options;


#if NET48
using JJMasterData.Commons.Configuration;
#endif

namespace JJMasterData.Core.Web.Components;

/// <summary>
/// Represents a CRUD.
/// </summary>
/// <example>
/// [!code-cshtml[Example](../../../example/JJMasterData.WebExample/Pages/Components/JJFormViewExample.cshtml)]
/// The GetHtml method will return something like this:
/// <img src="../media/JJFormViewExample.png"/>
/// </example>
public class JJFormView : AsyncComponent
{
    #region "Events"

    public event EventHandler<FormBeforeActionEventArgs>? OnBeforeInsert;
    public event EventHandler<FormBeforeActionEventArgs>? OnBeforeUpdate;
    public event EventHandler<FormBeforeActionEventArgs>? OnBeforeDelete;
    public event EventHandler<FormAfterActionEventArgs>? OnAfterInsert;
    public event EventHandler<FormAfterActionEventArgs>? OnAfterUpdate;
    public event EventHandler<FormAfterActionEventArgs>? OnAfterDelete;


    public event AsyncEventHandler<FormBeforeActionEventArgs>? OnBeforeInsertAsync;
    public event AsyncEventHandler<FormBeforeActionEventArgs>? OnBeforeUpdateAsync;
    public event AsyncEventHandler<FormBeforeActionEventArgs>? OnBeforeDeleteAsync;
    public event AsyncEventHandler<FormAfterActionEventArgs>? OnAfterInsertAsync;
    public event AsyncEventHandler<FormAfterActionEventArgs>? OnAfterUpdateAsync;
    public event AsyncEventHandler<FormAfterActionEventArgs>? OnAfterDeleteAsync;

    #endregion

    #region "Fields"

    private JJDataPanel? _dataPanel;
    private JJGridView? _gridView;
    private FormViewScripts? _scripts;
    private ActionMap? _currentActionMap;
    private BasicAction? _currentAction;
    private JJAuditLogView? _auditLogView;
    private JJDataImportation? _dataImportation;
    private string? _userId;
    private bool? _showTitle;
    private PageState? _pageState;
    private PageState? _panelState;
    private IDictionary<string, object> _relationValues = new Dictionary<string, object>();
    private RouteContext? _routeContext;


    #endregion

    #region "Properties"

    private JJAuditLogView AuditLogView
    {
        get
        {
            if (_auditLogView != null) 
                return _auditLogView;
            
            _auditLogView = ComponentFactory.AuditLog.Create(FormElement);
            _auditLogView.FormElement.ParentName = RouteContext.ParentElementName ?? FormElement.ParentName ?? FormElement.Name;

            return _auditLogView;
        }
    }

    /// <summary>
    /// Url a ser direcionada após os eventos de Update/Delete/Save
    /// </summary>
    private string? UrlRedirect { get; set; }


    /// <summary>
    /// Id do usuário Atual
    /// </summary>
    /// <remarks>
    /// Se a variavel não for atribuida diretamente,
    /// o sistema tenta recuperar em UserValues ou nas variaveis de Sessão
    /// </remarks>
    private string? UserId => _userId ??= DataHelper.GetCurrentUserId(CurrentContext.Session, UserValues);

    /// <summary>
    /// Configurações de importação
    /// </summary>
    private JJDataImportation DataImportation
    {
        get
        {
            if (_dataImportation != null)
                return _dataImportation;

            _dataImportation = GridView.DataImportation;
            _dataImportation.OnAfterDelete += OnAfterDelete;
            _dataImportation.OnAfterInsert += OnAfterInsert;
            _dataImportation.OnAfterUpdate += OnAfterUpdate;

            return _dataImportation;
        }
    }

    /// <summary>
    /// Configuração do painel com os campos do formulário
    /// </summary>
    public JJDataPanel DataPanel
    {
        get
        {
            _dataPanel ??= ComponentFactory.DataPanel.Create(FormElement);
            _dataPanel.ParentComponentName = Name;
            _dataPanel.FormUI = FormElement.Options.Form;
            _dataPanel.UserValues = UserValues;
            _dataPanel.RenderPanelGroup = true;
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
    public IDictionary<string, object> RelationValues
    {
        get
        {
            if (!_relationValues.Any())
            {
                _relationValues = GetRelationValuesFromForm() ;
            }

            return _relationValues;
        }
        set
        {
            _relationValues = value;
            GridView.RelationValues = _relationValues;
        }
    }

    public FormElement FormElement { get; }

    public JJGridView GridView
    {
        get
        {
            if (_gridView is not null)
                return _gridView;

            _gridView = ComponentFactory.GridView.Create(FormElement);
            _gridView.Name = Name;
            _gridView.ParentComponentName = Name;
            _gridView.FormElement = FormElement;
            _gridView.UserValues = UserValues;
            _gridView.ShowTitle = true;

            _gridView.ToolBarActions.Add(new DeleteSelectedRowsAction());

            return _gridView;
        }
    }

    public PageState PageState
    {
        get
        {
            if (CurrentContext.Request.Form[$"form-view-page-state-{Name}"] != null && _pageState is null)
                _pageState = (PageState)int.Parse(CurrentContext.Request.Form[$"form-view-page-state-{Name}"]);

            return _pageState ?? PageState.List;
        }
        internal set => _pageState = value;
    }

    /// <summary>
    /// If inside a relationship, PageState of the parent DataPanel.
    /// </summary>
    internal PageState PanelState
    {
        get
        {
            if (CurrentContext.Request.Form[$"form-view-panel-state-{Name}"] != null && _panelState is null)
                _panelState = (PageState)int.Parse(CurrentContext.Request.Form[$"form-view-panel-state-{Name}"]);

            return _panelState ?? PageState.View;
        }
        set => _panelState = value;
    }
    
    private ActionMap? CurrentActionMap
    {
        get
        {
            if (_currentActionMap != null)
                return _currentActionMap;

            string encryptedActionMap = CurrentContext.Request.Form[$"form-view-action-map-{Name.ToLower()}"];
            if (string.IsNullOrEmpty(encryptedActionMap))
                return null;

            _currentActionMap = EncryptionService.DecryptActionMap(encryptedActionMap);
            return _currentActionMap;
        }
    }
    
    private BasicAction? CurrentAction
    {
        get
        {
            if (_currentAction != null)
                return _currentAction;
            
            if (CurrentActionMap is null)
                return null;

            _currentAction = CurrentActionMap.GetAction(FormElement);
            return _currentAction;
        }
    }
    

    protected RouteContext RouteContext
    {
        get
        {
            if (_routeContext != null)
                return _routeContext;

            var factory = new RouteContextFactory(CurrentContext.Request.QueryString, EncryptionService);
            _routeContext = factory.Create();
            
            return _routeContext;
        }
    }
    
    internal ComponentContext ComponentContext
    {
        get
        {
            if (RouteContext.IsCurrentFormElement(FormElement.Name))
            {
                return RouteContext.ComponentContext;
            }

            return default;
        }
    }
    
    internal FormViewScripts Scripts => _scripts ??= new(this);
    public bool ShowTitle
    {
        get
        {
            _showTitle ??= FormElement.Options.Grid.ShowTitle;
            return _showTitle.Value;
        }
        set
        {
            GridView.ShowTitle = value;
            _showTitle = value;
        }
    }
    internal IHttpContext CurrentContext { get; }
    internal IFormValues FormValues => CurrentContext.Request.Form;
    public IQueryString QueryString => CurrentContext.Request.QueryString;
    internal IEntityRepository EntityRepository { get; }
    internal FieldValuesService FieldValuesService { get; }
    internal ExpressionsService ExpressionsService { get; }
    private IEnumerable<IActionPlugin> ActionPlugins { get; }
    private IOptions<JJMasterDataCoreOptions> Options { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }
    internal IDataDictionaryRepository DataDictionaryRepository { get; }
    internal FormService FormService { get; }
    internal IEncryptionService EncryptionService { get; }
    internal IComponentFactory ComponentFactory { get; }

    #endregion

    #region "Constructors"

#if NET48
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    private JJFormView() 
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        CurrentContext = StaticServiceLocator.Provider.GetScopedDependentService<IHttpContext>();
        EntityRepository = StaticServiceLocator.Provider.GetScopedDependentService<IEntityRepository>();
        ComponentFactory = StaticServiceLocator.Provider.GetScopedDependentService<IComponentFactory>();
        FormService = StaticServiceLocator.Provider.GetScopedDependentService<FormService>();
        FieldValuesService = StaticServiceLocator.Provider.GetScopedDependentService<FieldValuesService>();
        Options = StaticServiceLocator.Provider.GetScopedDependentService<IOptions<JJMasterDataCoreOptions>>();
        ExpressionsService = StaticServiceLocator.Provider.GetScopedDependentService<ExpressionsService>();
        StringLocalizer = StaticServiceLocator.Provider.GetScopedDependentService<IStringLocalizer<JJMasterDataResources>>();
        DataDictionaryRepository = StaticServiceLocator.Provider.GetScopedDependentService<IDataDictionaryRepository>();        
        ActionPlugins = StaticServiceLocator.Provider.GetScopedDependentService<IEnumerable<IActionPlugin>>();
        FormService.EnableErrorLinks = true;
    }

    public JJFormView(string elementName) : this()
    {
        var dataDictionaryRepository = StaticServiceLocator.Provider.GetScopedDependentService<IDataDictionaryRepository>();
        var factory = StaticServiceLocator.Provider.GetScopedDependentService<FormViewFactory>();
        FormElement = dataDictionaryRepository.GetMetadataAsync(elementName).GetAwaiter().GetResult();
        factory.SetFormEventHandlerAsync(this, FormElement).GetAwaiter().GetResult();
    }

    public JJFormView(FormElement formElement) : this()
    {
        FormElement = formElement;
    }
#endif

    internal JJFormView(
        FormElement formElement,
        IHttpContext currentContext,
        IEntityRepository entityRepository,
        IDataDictionaryRepository dataDictionaryRepository,
        FormService formService,
        IEncryptionService encryptionService,
        FieldValuesService fieldValuesService,
        ExpressionsService expressionsService,
        IEnumerable<IActionPlugin> actionPlugins,
        IOptions<JJMasterDataCoreOptions> options,
        IStringLocalizer<JJMasterDataResources> stringLocalizer,
        IComponentFactory componentFactory)
    {
        FormElement = formElement;
        Name = ComponentNameGenerator.Create(FormElement.Name);
        CurrentContext = currentContext;
        EntityRepository = entityRepository;
        FormService = formService;
        EncryptionService = encryptionService;
        FieldValuesService = fieldValuesService;
        ExpressionsService = expressionsService;
        ActionPlugins = actionPlugins;
        Options = options;
        StringLocalizer = stringLocalizer;
        DataDictionaryRepository = dataDictionaryRepository;
        ComponentFactory = componentFactory;
        formService.EnableErrorLinks = true;
    }

    #endregion

    protected override async Task<ComponentResult> BuildResultAsync()
    {
        if (!RouteContext.CanRender(FormElement.Name))
            return new EmptyComponentResult();
        
        if (RouteContext.IsCurrentFormElement(FormElement.Name))
            return await GetFormResultAsync();
            
        if (RouteContext.ElementName == Options.Value.AuditLogTableName)
            return await AuditLogView.GetResultAsync();
        
        var childFormView = await ComponentFactory.FormView.CreateAsync(RouteContext.ElementName);
        childFormView.FormElement.ParentName = RouteContext.ParentElementName;
        childFormView.UserValues = UserValues;
        childFormView.RelationValues = childFormView.GetRelationValuesFromForm();
        childFormView.ShowTitle = false;
        childFormView.DataPanel.FieldNamePrefix = $"{childFormView.DataPanel.Name}_";

        if (PageState is PageState.View)
        {
            childFormView.DisableActionsAtViewMode();
        }

        return await childFormView.GetFormResultAsync();
    }

    
    internal async Task<ComponentResult> GetFormResultAsync()
    {
        
        switch (ComponentContext)
        {
                
            case ComponentContext.TextFileUploadView:
            case ComponentContext.TextFileFileUpload:
            case ComponentContext.SearchBox:
                return await DataPanel.GetResultAsync();
            case ComponentContext.UrlRedirect:
                return await DataPanel.GetUrlRedirectResult(CurrentActionMap!.GetAction<UrlRedirectAction>(FormElement));
            case ComponentContext.DataPanelReload:
                return await GetReloadPanelResultAsync();
            case ComponentContext.DataExportation:
            case ComponentContext.GridViewReload:
            case ComponentContext.GridViewFilterReload:
            case ComponentContext.GridViewFilterSearchBox:
                return await GridView.GetResultAsync();
            case ComponentContext.DownloadFile:
                return ComponentFactory.Downloader.Create().GetDirectDownloadFromUrl();
            case ComponentContext.AuditLogView:
                return await AuditLogView.GetResultAsync();
            case ComponentContext.DataImportation or ComponentContext.DataImportationFileUpload:
                return await GetImportationResult();
            case ComponentContext.InsertSelection:
                return await GetInsertSelectionResult();
            default:
                return await GetFormActionResult();
        }
    }
    
    internal async Task<ComponentResult> GetReloadPanelResultAsync()
    {
        var filter = GridView.GetSelectedRowId();
        IDictionary<string, object?>? values;
        if (filter is { Count: > 0 })
            values = await EntityRepository.GetFieldsAsync(FormElement, filter);
        else
            values = await GetFormValuesAsync();

        var fieldName = QueryString["fieldName"];
        
        var result = await GetPluginActionResult(values,fieldName);
        
        DataPanel.Values = values;
        return await DataPanel.GetResultAsync();
    }
    

    private async Task<ComponentResult> GetSaveActionResult()
    {
        var values = await GetFormValuesAsync();
        var errors = PageState is PageState.Insert
            ? await InsertFormValuesAsync(values)
            : await UpdateFormValuesAsync(values);

        if (errors.Count != 0) 
            return await GetFormResult(new FormContext(values, errors, PageState), true);
        
        if (!string.IsNullOrEmpty(UrlRedirect))
        {
            return new RedirectComponentResult(UrlRedirect!);
        }

        if (GridView.ToolBarActions.InsertAction.ReopenForm)
        {
            PageState = PageState.Insert;
            
            var formResult = await GetFormResult(new FormContext(RelationValues!, PageState.Insert), false);

            if (formResult is HtmlComponentResult htmlComponent)
            {
                AppendInsertSuccessAlert(htmlComponent.HtmlBuilder);

                return htmlComponent;
            }

            return formResult;
        }

        if (ContainsRelationships())
        {
            PanelState = PageState.View;
            return await GetFormResult(new FormContext(values, PageState), false);
        }
        
        PageState = PageState.List;

        if (ComponentContext is ComponentContext.Modal)
        {
            return new JsonComponentResult(new { closeModal = true });
        }

        return await GridView.GetResultAsync();

    }

    internal bool ContainsRelationships()
    {
        return CurrentContext.Request.Form[$"form-view-panel-state-{Name}"] != null;
    }

    private void AppendInsertSuccessAlert(HtmlBuilder htmlBuilder)
    {
        var alert = new JJAlert
        {
            Name = $"insert-alert-{Name}",
            Color = PanelColor.Success,
            Title = StringLocalizer["Success"],
            ShowIcon = true,
            Icon = IconType.CheckCircleO
        };
        alert.Messages.Add(StringLocalizer["Record added successfully"]);
        htmlBuilder.Append(HtmlTag.Div, div =>
        {
            div.WithAttribute("id", $"insert-alert-div-{Name}")
                .WithCssClass("fade-out")
                .AppendComponent(alert);
        });
        htmlBuilder.AppendScript(Scripts.GetShowInsertSuccessScript());
    }

    private async Task<ComponentResult> GetCancelActionResult()
    {
        PageState = PageState.List;
        ClearTempFiles();
        return await GridView.GetResultAsync();
    }
    
    private async Task<ComponentResult> GetBackActionResult()
    {
        PageState = PageState.List;
        return await GridView.GetResultAsync();
    }
    
    private async Task<ComponentResult> GetFormActionResult()
    {
        SetFormServiceEvents();

        ComponentResult? result;
        switch (CurrentAction)
        {
            case ViewAction:
                result = await GetViewResult();
                break;
            case EditAction:
                result = await GetUpdateResult();
                break;
            case InsertAction:
                result = await GetInsertResult();
                break;
            case AuditLogFormToolbarAction:
            case AuditLogGridToolbarAction:
                result = await GetAuditLogResult();
                break;
            case DeleteAction:
                result = await GetDeleteResult();
                break;
            case DeleteSelectedRowsAction:
                result = await GetDeleteSelectedRowsResult();
                break;
            case SaveAction:
                result = await GetSaveActionResult();
                break;
            case BackAction:
                result = await GetBackActionResult();
                break;
            case CancelAction:
                result = await GetCancelActionResult();
                break;
            case SqlCommandAction:
                result = await GetSqlCommandActionResult();
                break;
            case PluginAction:
                result = await GetPluginActionResult();
                break;
            default:
                result = await GetDefaultResult();
                break;
        }

        if (result is HtmlComponentResult htmlComponent && ComponentContext is not ComponentContext.Modal)
        {
            var html = htmlComponent.HtmlBuilder;
            
            html.WithNameAndId(Name);
            html.AppendHiddenInput($"form-view-page-state-{Name}", ((int)PageState).ToString());
            html.AppendHiddenInput($"form-view-action-map-{Name}", EncryptionService.EncryptActionMap(CurrentActionMap));
            html.AppendHiddenInput($"form-view-relation-values-{Name}",
                EncryptionService.EncryptDictionary(RelationValues));

            if (ComponentContext is ComponentContext.FormViewReload)
            {
                return new ContentComponentResult(html);
            }
        }
        
        return result;
    }

    private async Task<ComponentResult> GetSqlCommandActionResult()
    {

        JJMessageBox? messageBox = null;
        var sqlAction = CurrentActionMap!.GetAction<SqlCommandAction>(FormElement);
        try
        {
            var sqlCommand = ExpressionsService.ParseExpression(sqlAction.CommandSql, await GetFormStateDataAsync());
        
            await EntityRepository.SetCommandAsync(new DataAccessCommand(sqlCommand!));
        }
        catch (Exception ex)
        {
            var message = ExceptionManager.GetMessage(ex);
            messageBox = ComponentFactory.Html.MessageBox.Create(message, MessageIcon.Error);
        }
        
        var result = await GetDefaultResult();

        if (result is HtmlComponentResult htmlComponentResult)
        {
            htmlComponentResult.HtmlBuilder.AppendComponentIf(messageBox is not null, messageBox);
        }

        return result;
    }   

    private async Task<ComponentResult> GetPluginActionResult(string? triggeredFieldName = null)
    {
        var formValues = await GetFormValuesAsync();

        var result = await GetPluginActionResult(formValues);

        if (result.JsCallback is not null)
        {
            return new JsonComponentResult(result);
        }
        
        return await GetDefaultResult(formValues);
    }

    private async Task<PluginActionResult> GetPluginActionResult(IDictionary<string, object?> formValues, string? fieldName = null)
    {
        var pluginAction = (PluginAction)CurrentAction!;

        var actionPlugin = ActionPlugins.First(p => p.Id == pluginAction.PluginId);

        var result = await actionPlugin.ExecuteActionAsync(new PluginActionContext
        {
            Values = formValues,
            AdditionalParameters = pluginAction.AdditionalParameters ?? new Dictionary<string, object?>(),
            TriggeredFieldName = fieldName
        });
        return result;
    }

    private void SetFormServiceEvents()
    {
        FormService.OnBeforeInsert += OnBeforeInsert;
        FormService.OnBeforeDelete += OnBeforeDelete;
        FormService.OnBeforeUpdate += OnBeforeUpdate;

        FormService.OnAfterInsert += OnAfterInsert;
        FormService.OnAfterUpdate += OnAfterUpdate;
        FormService.OnAfterDelete += OnAfterDelete;

        FormService.OnBeforeInsertAsync += OnBeforeInsertAsync;
        FormService.OnBeforeDeleteAsync += OnBeforeDeleteAsync;
        FormService.OnBeforeUpdateAsync += OnBeforeUpdateAsync;

        FormService.OnAfterInsertAsync += OnAfterInsertAsync;
        FormService.OnAfterUpdateAsync += OnAfterUpdateAsync;
        FormService.OnAfterDeleteAsync += OnAfterDeleteAsync;
    }

    private async Task<ComponentResult> GetGridViewResult()
    {
        return await GridView.GetResultAsync();
    }

    private async Task<ComponentResult> GetUpdateResult()
    {
        bool autoReloadFields;
        IDictionary<string, object?>? values;
        if (PageState is PageState.Update)
        {
            autoReloadFields = true;
            values = await GetFormValuesAsync();
        }
        else
        {
            autoReloadFields = false;
            values = await EntityRepository.GetFieldsAsync(FormElement, CurrentActionMap!.PkFieldValues);
        }

        PageState = PageState.Update;
        return await GetFormResult(new FormContext(values, PageState), autoReloadFields);
    }

    private async Task<ComponentResult> GetDefaultResult(IDictionary<string,object?>? formValues = null)
    {
        switch (PageState)
        {
            case PageState.Insert:
                return await GetFormResult(new FormContext((IDictionary<string, object?>)RelationValues, PageState),
                    false);
            case PageState.Update:
                formValues ??= await GetFormValuesAsync();
                return await GetFormResult(new FormContext(formValues, PageState), true);
            default:
                return await GetGridViewResult();
        }
    }

    private async Task<ComponentResult> GetInsertResult()
    {
        var insertAction = GridView.ToolBarActions.InsertAction;
        var formData = new FormStateData(RelationValues!, UserValues, PageState.List);
        
        bool isVisible = await ExpressionsService.GetBoolValueAsync(insertAction.VisibleExpression, formData);
        if (!isVisible)
            throw new UnauthorizedAccessException(StringLocalizer["Insert action is not enabled"]);
        
        if (PageState == PageState.Insert)
        {
            var formValues = await GetFormValuesAsync();
            return await GetFormResult(new FormContext(formValues, PageState), true);
        }

        PageState = PageState.Insert;

        if (string.IsNullOrEmpty(insertAction.ElementNameToSelect))
            return await GetFormResult(new FormContext(RelationValues!, PageState.Insert), false);
        
        return await GetInsertSelectionListResult();
    }

    private async Task<ComponentResult> GetInsertSelectionListResult()
    {
        var insertAction = GridView.ToolBarActions.InsertAction;
        var html = new HtmlBuilder(HtmlTag.Div);
        html.AppendHiddenInput($"form-view-insert-selection-values-{Name}");
        var formElement = await DataDictionaryRepository.GetMetadataAsync(insertAction.ElementNameToSelect);
        formElement.ParentName = FormElement.Name;
        
        var formView = ComponentFactory.FormView.Create(formElement);
        formView.UserValues = UserValues;
        formView.GridView.OnRenderAction += InsertSelectionOnRenderAction;
        
        var backScript = new StringBuilder();
        backScript.Append($"document.getElementById('form-view-page-state-{Name}').value = '{(int)PageState.List}'; ");
        backScript.Append($"document.getElementById('form-view-action-map-{Name}').value = null; ");
        backScript.AppendLine("document.forms[0].submit(); ");
        
        formView.GridView.ToolBarActions.Add(new ScriptAction
        {
            Name = "back-action",
            Icon = IconType.ArrowLeft,
            Text = StringLocalizer["Back"],
            ShowAsButton = true,
            OnClientClick = backScript.ToString(),
            IsDefaultOption = true
        });
        
        formView.GridView.GridActions.Add(new InsertSelectionAction());
        
        var result = await formView.GetFormResultAsync();

        if (result is RenderedComponentResult renderedComponentResult)
        {
            html.Append(renderedComponentResult.HtmlBuilder);
        }
        else
        {
            return result;
        }

        return new RenderedComponentResult(html);
    }

    private async Task<ComponentResult> GetInsertSelectionResult()
    {
        var insertValues = EncryptionService.DecryptDictionary(FormValues[$"form-view-insert-selection-values-{Name}"]);
        var html = new HtmlBuilder(HtmlTag.Div);
        var formElement =
            await DataDictionaryRepository.GetMetadataAsync(GridView.ToolBarActions.InsertAction.ElementNameToSelect);
        var selectionValues = await EntityRepository.GetFieldsAsync(formElement, insertValues);
        var values =
            await FieldValuesService.MergeWithExpressionValuesAsync(formElement, selectionValues, PageState.Insert, true);

        var mappedFkValues = DataHelper.GetRelationValues(FormElement, values, true);
        
        var errors = await InsertFormValuesAsync(mappedFkValues!, false);

        if (errors.Count > 0)
        {
            html.AppendComponent(ComponentFactory.Html.MessageBox.Create(errors, MessageIcon.Warning));
            var insertSelectionResult = await GetInsertSelectionListResult();

            if (insertSelectionResult is RenderedComponentResult renderedComponentResult)
            {
                html.Append(renderedComponentResult.HtmlBuilder);
            }
            else
            {
                return insertSelectionResult;
            }

            PageState = PageState.Insert;
        }
        else
        {
            PageState = PageState.Update;
            
            var result = await GetFormResult(new FormContext(mappedFkValues!, PageState), false);

            if (result is RenderedComponentResult renderedComponentResult)
            {
                html.Append(renderedComponentResult.HtmlBuilder);
            }
            else
            {
                return result;
            }
        }

        return new RenderedComponentResult(html);
    }

    private async Task<ComponentResult> GetViewResult()
    {
        if (CurrentActionMap == null)
        {
            PageState = PageState.List;
            return await GetGridViewResult();
        }

        PageState = PageState.View;
        var filter = CurrentActionMap.PkFieldValues;
        var values = await EntityRepository.GetFieldsAsync(FormElement, filter);
        return await GetFormResult(new FormContext(values, PageState), false);
    }

    private async Task<ComponentResult> GetDeleteResult()
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        var messageFactory = ComponentFactory.Html.MessageBox;
        try
        {
            var filter = CurrentActionMap?.PkFieldValues;
            var errors = await DeleteFormValuesAsync(filter);
            if (errors.Count > 0)
            {
                html.AppendComponent(messageFactory.Create(errors, MessageIcon.Warning));
            }
            else
            {
                if (GridView.EnableMultiSelect)
                    GridView.ClearSelectedGridValues();
            }
        }
        catch (Exception ex)
        {
            html.AppendComponent(messageFactory.Create(ex.Message, MessageIcon.Error));
        }

        if (!string.IsNullOrEmpty(UrlRedirect))
        {
            return new RedirectComponentResult(UrlRedirect!);
        }

        html.Append(await GridView.GetHtmlBuilderAsync());
        PageState = PageState.List;

        return new RenderedComponentResult(html);
    }
    
    private async Task<ComponentResult> GetDeleteSelectedRowsResult()
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        var messageFactory = ComponentFactory.Html.MessageBox;
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

                html.AppendComponent(messageFactory.Create(message.ToString(), icon));

                GridView.ClearSelectedGridValues();
            }
        }
        catch (Exception ex)
        {
            html.AppendComponent(messageFactory.Create(ex.Message, MessageIcon.Error));
        }

        var gridViewResult = await GetGridViewResult();

        if (gridViewResult is RenderedComponentResult)
        {
            html.Append(new HtmlBuilder(gridViewResult));
        }
        else
        {
            return gridViewResult;
        }

        PageState = PageState.List;

        return new RenderedComponentResult(html);
    }
    
    private async Task<ComponentResult> GetAuditLogResult()
    {
        var actionMap = _currentActionMap;
        var script = new StringBuilder();
        script.Append($"document.getElementById('form-view-page-state-{Name}').value = '{(int)PageState.List}'; ");
        script.Append($"document.getElementById('form-view-action-map-{Name}').value = null; ");
        script.AppendLine("document.forms[0].submit(); ");

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
            var html = await AuditLogView.GetLogDetailsHtmlAsync(actionMap?.PkFieldValues);

            if (actionMap?.PkFieldValues != null)
                html.AppendComponent(await GetAuditLogBottomBar(actionMap.PkFieldValues!));

            PageState = PageState.AuditLog;
            return new ContentComponentResult(html);
        }

        AuditLogView.GridView.AddToolBarAction(goBackAction);
        AuditLogView.DataPanel = DataPanel;
        PageState = PageState.AuditLog;
        return await AuditLogView.GetResultAsync();
    }

    private async Task<ComponentResult> GetImportationResult()
    {
        var action = GridView.ImportAction;
        var formStateData = await GridView.GetFormStateDataAsync();
        bool isVisible = await ExpressionsService.GetBoolValueAsync(action.VisibleExpression, formStateData);
        if (!isVisible)
            throw new UnauthorizedAccessException(StringLocalizer["Import action not enabled"]);

        var html = new HtmlBuilder(HtmlTag.Div);

        if (ShowTitle)
            html.AppendComponent(GridView.GetTitle(UserValues));

        PageState = PageState.Import;
        
        DataImportation.UserValues = UserValues;
        DataImportation.BackButton.OnClientClick = "DataImportationModal.getInstance().hide()";
        DataImportation.ProcessOptions = action.ProcessOptions;
        DataImportation.EnableAuditLog = await ExpressionsService.GetBoolValueAsync(GridView.ToolBarActions.AuditLogGridToolbarAction.VisibleExpression,formStateData);

        var result = await DataImportation.GetResultAsync();

        if (result is RenderedComponentResult renderedComponentResult)
        {
            html.Append(renderedComponentResult.HtmlBuilder);
        }
        else
        {
            return result;
        }


        return new RenderedComponentResult(html);
    }
    
    private async Task<ComponentResult> GetFormResult(FormContext formContext, bool autoReloadFormFields)
    {
        var (values, errors, pageState) = formContext;

        var visibleRelationships = await GetVisibleRelationships(values, pageState);

        var parentPanel = DataPanel;
        parentPanel.PageState = pageState;
        parentPanel.Errors = errors;
        parentPanel.Values = values;
        parentPanel.AutoReloadFormFields = autoReloadFormFields;

        if (!visibleRelationships.Any() || visibleRelationships.Count == 1)
        {
            return await GetParentPanelResult(parentPanel, values);
        }

        return await GetRelationshipLayoutResult(visibleRelationships, values);
    }

    private async Task<ComponentResult> GetRelationshipLayoutResult(List<FormElementRelationship> visibleRelationships,
        IDictionary<string, object?> values)
    {
        var html = new HtmlBuilder(HtmlTag.Div);
        if (ShowTitle)
            html.AppendComponent(GridView.GetTitle(values));

        var layout = new FormViewRelationshipLayout(this);
        
        var formToolbarActions = FormElement.Options.FormToolbarActions;
        
        if (PageState is PageState.Update)
        {
            if (PanelState is PageState.View)
            {
                formToolbarActions.FormEditAction.SetVisible(true);
                formToolbarActions.RemoveAll(a => a is SaveAction or CancelAction);
            }
            else
            {
                formToolbarActions.FormEditAction.SetVisible(false);
            }
        }
        else if (PageState is PageState.View)
        {
             FormElement.Options.FormToolbarActions.AuditLogFormToolbarAction.SetVisible(await IsAuditLogEnabled());
        }
        
        FormElement.Options.FormToolbarActions.BackAction.SetVisible(true);
        
        var topActions = GetTopToolbarActions(FormElement);

        html.AppendComponent(await GetFormToolbarAsync(topActions));

        var relationshipsResult = await layout.GetRelationshipsResult(visibleRelationships);

        if (relationshipsResult is RenderedComponentResult renderedComponentResult)
        {
            html.Append(renderedComponentResult.HtmlBuilder);
        }

        var bottomActions = FormElement.Options.FormToolbarActions
            .Where(a => a.Location is FormToolbarActionLocation.Bottom).ToList();

        html.AppendComponent(await GetFormToolbarAsync(bottomActions));

        if (ComponentContext is ComponentContext.Modal)
        {
            html.AppendScript($"document.getElementById('form-view-page-state-{Name}').value={(int)PageState}");
            return new ContentComponentResult(html);
        }
        
        return new RenderedComponentResult(html);
    }

    private async Task<ComponentResult> GetParentPanelResult(JJDataPanel parentPanel, IDictionary<string, object?> values)
    {
        var panelHtml = await GetParentPanelHtml(parentPanel);
        panelHtml.AppendScript($"document.getElementById('form-view-page-state-{Name}').value={(int)PageState}");
        if (ComponentContext is ComponentContext.Modal)
            return new ContentComponentResult(panelHtml);

        if (ShowTitle)
            panelHtml.Prepend(GridView.GetTitle(values).GetHtmlBuilder());

        return new RenderedComponentResult(panelHtml);
    }

    private async Task<List<FormElementRelationship>> GetVisibleRelationships(IDictionary<string, object?> values, PageState pageState)
    {
        var visibleRelationships = await FormElement
            .Relationships
            .ToAsyncEnumerable()
            .Where(r => r.ViewType != RelationshipViewType.None || r.IsParent)
            .WhereAwait(async r =>
                await ExpressionsService.GetBoolValueAsync(r.Panel.VisibleExpression,
                    new FormStateData(values, pageState)))
            .ToListAsync();
        return visibleRelationships;
    }

    internal async Task<HtmlBuilder> GetParentPanelHtml(JJDataPanel panel)
    {
        var formHtml = new HtmlBuilder(HtmlTag.Div);

        if (PageState is PageState.View)
        {
            FormElement.Options.FormToolbarActions.AuditLogFormToolbarAction.SetVisible(await IsAuditLogEnabled());
        }
        
        var topToolbarActions = GetTopToolbarActions(FormElement);
        
        formHtml.AppendComponent(await GetFormToolbarAsync(topToolbarActions));
        
        var parentPanelHtml = await panel.GetPanelHtmlBuilderAsync();
        
        var panelAndBottomToolbarActions = GetPanelToolbarActions(FormElement);
        panelAndBottomToolbarActions.AddRange(GetBottomToolbarActions(FormElement));
        
        var toolbar = await GetFormToolbarAsync(panelAndBottomToolbarActions);
        
        formHtml.Append(parentPanelHtml);
        
        formHtml.AppendComponent(toolbar);
        
        if (panel.Errors.Any())
            formHtml.AppendComponent(ComponentFactory.Html.ValidationSummary.Create(panel.Errors));
        
        return formHtml;
    }
    
    internal async Task<HtmlBuilder> GetRelationshipParentPanelHtml(JJDataPanel panel)
    {
        var formHtml = new HtmlBuilder(HtmlTag.Div);
        
        panel.PageState = PanelState;
        
        var parentPanelHtml = await panel.GetPanelHtmlBuilderAsync();
        
        var panelToolbarActions = GetPanelToolbarActions(panel.FormElement);
        
        var toolbar = await GetFormToolbarAsync(panelToolbarActions);
        
        formHtml.Append(parentPanelHtml);
        
        formHtml.AppendComponent(toolbar);
        
        if (panel.Errors.Any())
            formHtml.AppendComponent(ComponentFactory.Html.ValidationSummary.Create(panel.Errors));
        
        formHtml.AppendHiddenInput($"form-view-panel-state-{Name}", ((int)PanelState).ToString());
        
        return formHtml;
    }

    private static List<BasicAction> GetPanelToolbarActions(FormElement formElement)
    {
        var toolbarActions = formElement.Options.FormToolbarActions
            .Where(a => a.Location == FormToolbarActionLocation.Panel);

        return toolbarActions.ToList();
    }
    
    private static List<BasicAction> GetTopToolbarActions(FormElement formElement)
    {
        var toolbarActions = formElement.Options.FormToolbarActions
            .Where(a => a.Location == FormToolbarActionLocation.Top);

        return toolbarActions.ToList();
    }
    
    private static List<BasicAction> GetBottomToolbarActions(FormElement formElement)
    {
        var toolbarActions = formElement.Options.FormToolbarActions
            .Where(a => a.Location == FormToolbarActionLocation.Bottom);

        return toolbarActions.ToList();
    }

    private async Task<JJToolbar> GetAuditLogBottomBar(IDictionary<string, object?> values)
    {
        var hideAuditLogButton = await ComponentFactory.ActionButton.CreateFormToolbarButtonAsync(FormElement.Options.FormToolbarActions.BackAction,this);

        var toolbar = new JJToolbar
        {
            CssClass = "pb-3 mt-3"
        };
        toolbar.Items.Add(hideAuditLogButton.GetHtmlBuilder());
        return toolbar;
    }

    private async Task<JJToolbar> GetFormToolbarAsync(IList<BasicAction> actions)
    {
        var toolbar = new JJToolbar
        {
            CssClass = "mb-3"
        };

        foreach (var action in actions.Where(a => !a.IsGroup))
        {
            if (action is SaveAction saveAction)
            {
                saveAction.EnterKeyBehavior = DataPanel.FormUI.EnterKey;
            }

            var factory = ComponentFactory.ActionButton;


            var linkButton = await factory.CreateFormToolbarButtonAsync(action, this);
            toolbar.Items.Add(linkButton.GetHtmlBuilder());
        }

        if (actions.Any(a => a.IsGroup))
        {
            var btnGroup = new JJLinkButtonGroup
            {
                CaretText = StringLocalizer["More"]
            };

            foreach (var groupedAction in actions.Where(a => a.IsGroup).ToList())
            {
                btnGroup.ShowAsButton = groupedAction.ShowAsButton;
                var factory = ComponentFactory.ActionButton;
                var linkButton = await factory.CreateFormToolbarButtonAsync(groupedAction, this);
                btnGroup.Actions.Add(linkButton);
            }

            toolbar.Items.Add(btnGroup.GetHtmlBuilder());
        }

        return toolbar;
    }

    private void InsertSelectionOnRenderAction(object? sender, ActionEventArgs args)
    {
        if (sender is not JJGridView)
            return;

        if (args.Action is not InsertSelectionAction) 
            return;
        
        args.LinkButton.Tooltip = StringLocalizer["Select"];
        args.LinkButton.OnClientClick = Scripts.GetInsertSelectionScript(args.FieldValues);
    }
    

    /// <summary>
    /// Insert the records in the database.
    /// </summary>
    /// <returns>The list of errors.</returns>
    public async Task<IDictionary<string, string>> InsertFormValuesAsync(
        IDictionary<string, object?> values,
        bool validateFields = true)
    {
        var dataContext = new DataContext(CurrentContext.Request, DataContextSource.Form, UserId);
        var result = await FormService.InsertAsync(FormElement, values, dataContext, validateFields);
        UrlRedirect = result.UrlRedirect;
        return result.Errors;
    }

    /// <summary>
    /// Update the records in the database.
    /// </summary>
    /// <returns>The list of errors.</returns>
    public async Task<IDictionary<string, string>> UpdateFormValuesAsync(IDictionary<string, object?> values)
    {
        var result = await FormService.UpdateAsync(FormElement, values,
            new DataContext(CurrentContext.Request, DataContextSource.Form, UserId));
        UrlRedirect = result.UrlRedirect;
        return result.Errors;
    }

    public async Task<IDictionary<string, string>> DeleteFormValuesAsync(IDictionary<string, object>? filter)
    {
        var values =
            await FieldValuesService.MergeWithExpressionValuesAsync(FormElement, filter!, PageState.Delete, true);
        var result = await FormService.DeleteAsync(FormElement, values,
            new DataContext(CurrentContext.Request, DataContextSource.Form, UserId));
        UrlRedirect = result.UrlRedirect;
        return result.Errors;
    }


    public async Task<IDictionary<string, object?>> GetFormValuesAsync()
    {
        var panel = DataPanel;
        var values = await panel.GetFormValuesAsync();

        if (!RelationValues.Any())
            return values;

        DataHelper.CopyIntoDictionary(values, RelationValues!, true);

        return values;
    }

    public async Task<IDictionary<string, string>> ValidateFieldsAsync(IDictionary<string, object> values,
        PageState pageState)
    {
        DataPanel.Values = values;
        var errors = await DataPanel.ValidateFieldsAsync(values, pageState);
        return errors;
    }

    private void ClearTempFiles()
    {
        var uploadFields = FormElement.Fields.ToList().FindAll(x => x.Component == FormComponent.File);
        foreach (var field in uploadFields)
        {
            string sessionName = $"{field.Name}-upload-view_jjfiles";
            if (CurrentContext?.Session[sessionName] != null)
                CurrentContext.Session[sessionName] = null;
        }
    }



    public async Task<FormStateData> GetFormStateDataAsync()
    {
        var values =
            await GridView.FormValuesService.GetFormValuesWithMergedValuesAsync(FormElement, PageState,
                CurrentContext.Request.Form.ContainsFormValues());
        
        if (!values.Any())
        {
            values = DataPanel.Values;
        }
        
        return new FormStateData(values, UserValues, PageState);
    }

    public IDictionary<string, object> GetRelationValuesFromForm()
    {
        var encryptedRelationValues = CurrentContext.Request.Form[$"form-view-relation-values-{Name}"];
        
        if (encryptedRelationValues is null)
            return new Dictionary<string, object>();

        return EncryptionService.DecryptDictionary(encryptedRelationValues);
    }
    
    public void SetRelationshipPageState(RelationshipViewType relationshipViewType)
    {
        var relationshipPageState = relationshipViewType == RelationshipViewType.List ? PageState.List : PageState.Update;

        if (CurrentContext.Request.Form.ContainsFormValues())
        {
            var pageState = CurrentContext.Request.Form[$"form-view-page-state-{Name}"];
            PageState = pageState != null ? (PageState)int.Parse(pageState) : relationshipPageState;
        }
        else
        {
            PageState = relationshipPageState;
        }
    }

    private async Task<bool> IsAuditLogEnabled()
    {
        var auditLogAction = FormElement.Options.GridToolbarActions.AuditLogGridToolbarAction;
        var formStateData = await GetFormStateDataAsync();
        return await ExpressionsService.GetBoolValueAsync(auditLogAction.VisibleExpression,formStateData);
    }
    
    internal void DisableActionsAtViewMode()
    {
        foreach (var action in FormElement.Options.GridTableActions)
        {
            if (action is not ViewAction)
            {
                action.SetVisible(false);
            }
        }

        foreach (var action in FormElement.Options.GridToolbarActions)
        {
            if (action is not FilterAction &&
                action is not RefreshAction &&
                action is not LegendAction &&
                action is not ConfigAction)
            {
                action.SetVisible(false);
            }
        }
    }

    #region "Legacy inherited GridView compatibility"

    [Obsolete("Please use GridView.GridActions")]
    public GridTableActionList GridActions => GridView.GridActions;

    [Obsolete("Please use GridView.ToolBarActions")]
    public GridToolbarActionList ToolBarActions => GridView.ToolBarActions;

    [Obsolete("Please use GridView.SetCurrentFilterAsync")]
    public void SetCurrentFilter(string filterKey, string filterValue)
    {
        GridView.SetCurrentFilterAsync(filterKey, filterValue).GetAwaiter().GetResult();
    }

    [Obsolete("Please use GridView.GetSelectedGridValues")]
    public List<IDictionary<string, object>> GetSelectedGridValues() => GridView.GetSelectedGridValues();

    [Obsolete("Please use GridView.AddToolBarAction")]
    public void AddToolBarAction(UserCreatedAction userCreatedAction)
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
    public void AddGridAction(UserCreatedAction userCreatedAction)
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
    public void ClearSelectedGridValues()
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