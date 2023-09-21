﻿#nullable enable

// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable EventNeverSubscribedTo.Global

using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Actions;
using JJMasterData.Core.DataDictionary.Actions.Abstractions;
using JJMasterData.Core.DataDictionary.Actions.FormToolbar;
using JJMasterData.Core.DataDictionary.Actions.GridTable;
using JJMasterData.Core.DataDictionary.Actions.GridToolbar;
using JJMasterData.Core.DataDictionary.Actions.UserCreated;
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
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager.Models;
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
    private FormViewScripts? _formViewScripts;
    private ActionMap? _currentActionMap;
    private JJAuditLogView? _auditLogView;
    private JJDataImportation? _dataImportation;
    private string? _userId;
    private bool? _showTitle;
    private PageState? _pageState;
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
                var encryptedRelationValues = CurrentContext.Request.Form[$"form-view-relation-values-{Name}"];
                if (encryptedRelationValues is null)
                    return _relationValues;

                _relationValues = EncryptionService.DecryptDictionary(encryptedRelationValues);
                GridView.RelationValues = _relationValues;
            }

            return _relationValues;
        }
        set
        {
            GridView.RelationValues = value;
            _relationValues = value;
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
            _gridView.Name = Name.ToLower();
            _gridView.FormElement = FormElement;
            _gridView.UserValues = UserValues;
            _gridView.ShowTitle = true;

            _gridView.ToolBarActions.Add(new DeleteSelectedRowsAction());
            _gridView.ToolBarActions.Add(new LogAction());

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
    
    private FormViewScripts FormViewScripts => _formViewScripts ??= new(this);
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
    internal IEntityRepository EntityRepository { get; }
    internal FieldValuesService FieldValuesService { get; }
    internal ExpressionsService ExpressionsService { get; }
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
        
        var formView = await ComponentFactory.FormView.CreateAsync(RouteContext.ElementName);
        formView.FormElement.ParentName = RouteContext.ParentElementName;
        formView.UserValues = UserValues;
        formView.ShowTitle = false;
        formView.DataPanel.FieldNamePrefix = $"{formView.DataPanel.Name}_";

        return await formView.GetFormResultAsync();
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
                return await DataPanel.GetUrlRedirectResult(CurrentActionMap);
            case ComponentContext.DataPanelReload:
                return await GetReloadPanelResultAsync();
            case ComponentContext.DataExportation:
            case ComponentContext.GridViewReload:
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

        PageState = PageState.List;

        if (ComponentContext is ComponentContext.Modal)
        {
            return new JsonComponentResult(new { closeModal = true });
        }

        return await GridView.GetResultAsync();

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
        htmlBuilder.AppendScript(FormViewScripts.GetShowInsertSuccessScript());
    }

    private async Task<ComponentResult> GetCancelActionResult()
    {
        PageState = PageState.List;
        ClearTempFiles();
        return await GridView.GetResultAsync();
    }

    private async Task<ComponentResult> GetFormActionResult()
    {
        var currentAction = CurrentActionMap?.GetCurrentAction(FormElement);

        SetFormServiceEvents();

        ComponentResult? result;
        switch (currentAction)
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
            case LogAction:
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
            case CancelAction:
                result = await GetCancelActionResult();
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

    private async Task<ComponentResult> GetDefaultResult()
    {
        switch (PageState)
        {
            case PageState.Insert:
                return await GetFormResult(new FormContext((IDictionary<string, object?>)RelationValues, PageState),
                    false);
            case PageState.Update:
                var values = await GetFormValuesAsync();
                return await GetFormResult(new FormContext(values, PageState), true);
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
                html.AppendComponent(GetFormLogBottomBar(actionMap.PkFieldValues!));

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
        var formData = await GridView.GetFormStateDataAsync();
        bool isVisible = await ExpressionsService.GetBoolValueAsync(action.VisibleExpression, formData);
        if (!isVisible)
            throw new UnauthorizedAccessException(StringLocalizer["Import action not enabled"]);

        var html = new HtmlBuilder(HtmlTag.Div);

        if (ShowTitle)
            html.AppendComponent(GridView.GetTitle(UserValues));

        PageState = PageState.Import;
        
        
        DataImportation.UserValues = UserValues;
        DataImportation.BackButton.OnClientClick = "DataImportationModal.getInstance().hide()";
        DataImportation.ProcessOptions = action.ProcessOptions;
        DataImportation.EnableAuditLog = GridView.ToolBarActions.LogAction.IsVisible;

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

        var visibleRelationships = await FormElement
            .Relationships
            .ToAsyncEnumerable()
            .Where(r => r.ViewType != RelationshipViewType.None || r.IsParent)
            .WhereAwait(async r =>
                await ExpressionsService.GetBoolValueAsync(r.Panel.VisibleExpression,
                    new FormStateData(values, pageState)))
            .ToListAsync();

        var parentPanel = DataPanel;
        parentPanel.PageState = pageState;
        parentPanel.Errors = errors;
        parentPanel.Values = values;
        parentPanel.AutoReloadFormFields = autoReloadFormFields;

        if (!visibleRelationships.Any() || visibleRelationships.Count == 1)
        {
            var panelHtml = await GetHtmlFromPanel(parentPanel, true);
            panelHtml.AppendScript($"document.getElementById('form-view-page-state-{Name}').value={(int)PageState}");
            if (ComponentContext is ComponentContext.Modal)
            {
                return new ContentComponentResult(panelHtml);
            }
            
            if (ShowTitle)
                panelHtml.Prepend(GridView.GetTitle(values).GetHtmlBuilder());

            return new RenderedComponentResult(panelHtml);
        }

        var html = new HtmlBuilder(HtmlTag.Div);
        if (ShowTitle)
            html.AppendComponent(GridView.GetTitle(values));

        var layout = new FormViewRelationshipLayout(this);

        var topActions = FormElement.Options.FormToolbarActions
            .GetAllSorted()
            .Where(a => a.FormToolbarActionLocation is FormToolbarActionLocation.Top).ToList();

        html.AppendComponent(await GetFormToolbarAsync(topActions));

        var relationshipsResult = await layout.GetRelationshipsResult(visibleRelationships);

        if (relationshipsResult is RenderedComponentResult renderedComponentResult)
        {
            html.Append(renderedComponentResult.HtmlBuilder);
        }

        var bottomActions = FormElement.Options.FormToolbarActions
            .GetAllSorted()
            .Where(a => a.FormToolbarActionLocation is FormToolbarActionLocation.Bottom).ToList();

        html.AppendComponent(await GetFormToolbarAsync(bottomActions));

        return new RenderedComponentResult(html);
    }

    internal async Task<HtmlBuilder> GetHtmlFromPanel(JJDataPanel panel, bool isParent = false)
    {
        var formHtml = new HtmlBuilder(HtmlTag.Div);

        var parentPanelHtml = await panel.GetPanelHtmlBuilderAsync();

        var panelActions = panel.FormElement.Options.FormToolbarActions
            .Where(a => a.FormToolbarActionLocation == FormToolbarActionLocation.Panel || isParent).ToList();

        var toolbar = await GetFormToolbarAsync(panelActions);
        
        formHtml.Append(parentPanelHtml);
        
        formHtml.AppendComponent(toolbar);
        
        if (panel.Errors.Any())
            formHtml.AppendComponent(ComponentFactory.Html.ValidationSummary.Create(panel.Errors));
        
        return formHtml;
    }

    private JJToolbar GetFormLogBottomBar(IDictionary<string, object?> values)
    {
        var backScript = new StringBuilder();
        backScript.Append($"$('#form-view-page-state-{Name}').val('{(int)PageState.List}'); ");
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

    private async Task<JJToolbar> GetFormToolbarAsync(IList<BasicAction> actions)
    {
        var toolbar = new JJToolbar
        {
            CssClass = "mt-3"
        };

        foreach (var action in actions.Where(a => !a.IsGroup))
        {
            if (action is SaveAction saveAction)
            {
                saveAction.EnterKeyBehavior = DataPanel.FormUI.EnterKey;
            }

            var factory = ComponentFactory.Html.LinkButton;


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
                var factory = ComponentFactory.Html.LinkButton;
                var linkButton = await factory.CreateFormToolbarButtonAsync(groupedAction, this);
                btnGroup.Actions.Add(linkButton);
            }

            toolbar.Items.Add(btnGroup.GetHtmlBuilder());
        }


        if (PageState == PageState.View)
        {
            if (GridView.ToolBarActions.LogAction.IsVisible)
            {
                var values = await GetFormValuesAsync();
                toolbar.Items.Add(GetButtonViewLog(values).GetHtmlBuilder());
            }
        }

        return toolbar;
    }

    private void InsertSelectionOnRenderAction(object? sender, ActionEventArgs args)
    {
        if (sender is not JJGridView gridView)
            return;
        
        if (args.Action is InsertSelectionAction)
        {
            args.LinkButton.Tooltip = StringLocalizer["Select"];
            args.LinkButton.OnClientClick = FormViewScripts.GetInsertSelectionScript(args.FieldValues);
        }
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

    private JJLinkButton GetBackButton()
    {
        var btn = ComponentFactory.Html.LinkButton.Create();
        btn.Type = LinkButtonType.Button;
        btn.CssClass = $"{BootstrapHelper.DefaultButton} btn-small";
        btn.OnClientClick = $"JJViewHelper.doPainelAction('{Name}','CANCEL');";
        btn.IconClass = IconType.Times.GetCssClass();
        btn.Text = "Cancel";
        btn.IconClass = IconType.ArrowLeft.GetCssClass();
        btn.Text = "Back";
        return btn;
    }

    private JJLinkButton GetButtonHideLog(IDictionary<string, object?> values)
    {
        var context = new ActionContext
        {
            FormElement = FormElement,
            FormStateData = new FormStateData(values, UserValues, PageState),
            ParentComponentName = Name
        };
        string scriptAction = GridView.ActionsScripts.GetFormActionScript(
                GridView.GridActions.ViewAction, 
                context,
                ActionSource.GridTable);
        
        var btn = ComponentFactory.Html.LinkButton.Create();
        btn.Type = LinkButtonType.Button;
        btn.Text = "Hide Log";
        btn.IconClass = IconType.Film.GetCssClass();
        btn.CssClass = "btn btn-primary btn-small";
        btn.OnClientClick = $"$('#form-view-page-state-{Name}').val('{(int)PageState.List}');{scriptAction}";
        return btn;
    }

    private JJLinkButton GetButtonViewLog(IDictionary<string, object?> values)
    {
        var context = new ActionContext
        {
            FormElement = FormElement,
            FormStateData = new FormStateData(values, UserValues, PageState),
            ParentComponentName = Name
        };
        string scriptAction = GridView.ActionsScripts.GetFormActionScript(
            GridView.ToolBarActions.LogAction, 
            context,
            ActionSource.GridToolbar);
        
        var btn = ComponentFactory.Html.LinkButton.Create();
        btn.Type = LinkButtonType.Button;
        btn.Text = "View Log";
        btn.IconClass = IconType.Film.GetCssClass();
        btn.CssClass = $"{BootstrapHelper.DefaultButton} btn-small";
        btn.OnClientClick = scriptAction;
        return btn;
    }

    public async Task<FormStateData> GetFormStateDataAsync()
    {
        var values =
            await GridView.FormValuesService.GetFormValuesWithMergedValuesAsync(FormElement, PageState,
                CurrentContext.Request.Form.ContainsFormValues());

        return new FormStateData(values, UserValues, PageState);
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