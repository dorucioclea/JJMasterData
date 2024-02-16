#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.DataDictionary.Models.Actions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Models;
using JJMasterData.Core.UI.Html;

namespace JJMasterData.Core.UI.Components;

internal class FormViewRelationshipLayout(JJFormView parentFormView, List<FormElementRelationship> relationships)
{
    public async Task<ComponentResult> GetRelationshipsResult()
    {
        var relationshipsDiv = new HtmlBuilder(HtmlTag.Div);

        if (relationships.Any(r => r.Panel.Layout is PanelLayout.Tab))
        {
            var tabNavResult = await GetTabRelationshipsResult();

            if (tabNavResult is RenderedComponentResult renderedComponentResult)
            {
                relationshipsDiv.Append((HtmlBuilder?)renderedComponentResult.HtmlBuilder);
            }
            else
            {
                return tabNavResult;
            }
        }

        foreach (var relationship in relationships.Where(r => r.Panel.Layout is not PanelLayout.Tab))
        {
            var relationshipResult = await GetRelationshipResult(relationship);

            if (relationshipResult is RenderedComponentResult renderedComponentResult)
            {
                var panel = GetNonTabRelationshipPanelHtml(relationship, renderedComponentResult.HtmlBuilder);
                relationshipsDiv.Append(panel);
            }
            else
            {
                return relationshipResult;
            }
        }

        return new RenderedComponentResult(relationshipsDiv);
    }


    private async Task<ComponentResult> GetTabRelationshipsResult()
    {
        var tabNav = new JJTabNav(parentFormView.CurrentContext.Request.Form)
        {
            Name = $"relationships-tab-nav-{parentFormView.DataPanel.Name}"
        };

        foreach (var relationship in relationships.Where(r => r.Panel.Layout is PanelLayout.Tab))
        {
            var relationshipResult = await GetRelationshipResult(relationship);

            if (relationshipResult is RenderedComponentResult renderedComponentResult)
            {
                tabNav.ListTab.Add(new NavContent
                {
                    Title = GetExpressionValue(relationship.Panel.Title),
                    Icon = relationship.Panel.Icon,
                    HtmlContent = renderedComponentResult.HtmlBuilder
                });
            }
            else
            {
                return relationshipResult;
            }
        }

        return new RenderedComponentResult(tabNav.GetHtmlBuilder());
    }

    private string GetExpressionValue(string? expression)
    {
        return parentFormView
            .ExpressionsService.GetExpressionValue(expression,
            GetFormStateData()
            )?.ToString() ?? string.Empty;
    }

    private FormStateData GetFormStateData() => new(parentFormView.DataPanel.Values, parentFormView.UserValues, parentFormView.PageState);

    private HtmlBuilder? GetNonTabRelationshipPanelHtml(FormElementRelationship relationship, HtmlBuilder? content)
    {
        switch (relationship.Panel.Layout)
        {
            case PanelLayout.Collapse:
                var collapse = new JJCollapsePanel(parentFormView.FormValues)
                {
                    Name = $"{relationship.ElementRelationship?.ChildElement ?? parentFormView.Name}-collapse-panel",
                    Title = GetExpressionValue(relationship.Panel.Title),
                    SubTitle = GetExpressionValue(relationship.Panel.SubTitle),
                    HtmlBuilderContent = content,
                    Color = relationship.Panel.Color,
                    TitleIcon = relationship.Panel.Icon.HasValue ? new JJIcon(relationship.Panel.Icon.Value) : null,
                    ExpandedByDefault = relationship.Panel.ExpandedByDefault,
                    CssClass = relationship.Panel.CssClass
                };

                return collapse.GetHtmlBuilder();
            case PanelLayout.Panel or PanelLayout.Well:
                var panel = new JJCard
                {
                    Name = $"{relationship.Id}-card",
                    Title = GetExpressionValue(relationship.Panel.Title),
                    SubTitle = GetExpressionValue(relationship.Panel.SubTitle),
                    Color = relationship.Panel.Color,
                    Layout = relationship.Panel.Layout,
                    Icon = relationship.Panel.Icon,
                    HtmlBuilderContent = content,
                    CssClass = relationship.Panel.CssClass
                };

                return panel.GetHtmlBuilder();
            case PanelLayout.NoDecoration:
                var title = GetExpressionValue(relationship.Panel.Title);
                var div = new HtmlBuilder(HtmlTag.Div);
                div.WithCssClass(relationship.Panel.CssClass);
                if (title is not null)
                {       
                    var htmlTitle = new JJTitle
                    {
                        Title = relationship.Panel.Title,
                        Size = HeadingSize.H3
                    };
                    div.AppendComponent(htmlTitle);
                }
                div.Append(content);

                return div;
            default:
                return null;
        }
    }

    private async Task<ComponentResult> GetRelationshipResult(FormElementRelationship relationship)
    {
        var parentPanel = parentFormView.DataPanel;

        var formContext = new FormContext(parentPanel.Values, parentPanel.Errors, parentPanel.PageState);
        
        if (relationship.IsParent)
            return new RenderedComponentResult(await parentFormView.GetParentPanelHtmlAtRelationship(relationship));

        var childFormView =
            await parentFormView.ComponentFactory.FormView.CreateAsync(relationship.ElementRelationship!
                .ChildElement);
        childFormView.FormElement.ParentName = parentFormView.FormElement.ParentName ?? parentFormView.FormElement.Name;

        var filter = new Dictionary<string, object?>();
        foreach (var col in relationship.ElementRelationship.Columns.Where(col =>
                     formContext.Values.ContainsKey(col.PkColumn)))
        {
            var value = formContext.Values[col.PkColumn];
            filter[col.FkColumn] = value;
        }


        switch (relationship.ViewType)
        {
            case RelationshipViewType.Update:
            case RelationshipViewType.View:
            {
                return await ConfigureOneToOneFormView(childFormView, relationship, filter);
            }
            case RelationshipViewType.List:
            {
                return await ConfigureOneToManyFormView(childFormView,relationship, filter);
            }
            default:
                return new EmptyComponentResult();
        }
    }

    private async Task<ComponentResult> ConfigureOneToManyFormView
    (   JJFormView childFormView,
        FormElementRelationship relationship,
        Dictionary<string, object?> filter)
    {
        childFormView.ShowTitle = false;
        childFormView.UserValues = new Dictionary<string, object>(parentFormView.UserValues);
        
        if(childFormView.CurrentAction is null)
            childFormView.PageState = PageState.List;
        
        childFormView.RelationValues = DataHelper.GetRelationValues(parentFormView.FormElement, filter);
        await childFormView.GridView.Filter.ApplyCurrentFilter(filter);
        childFormView.ShowTitle = false;
        childFormView.IsChildFormView = true;
        
        var isDisabled = IsRelationshipDisabled(relationship);
        
        if (parentFormView.PageState is PageState.View || parentFormView.PanelState is PageState.Update || isDisabled)
            childFormView.DisableActionsAtViewMode();
                
        var result = await childFormView.GetFormResultAsync();
                
        return result;
    }

    private async Task<ComponentResult> ConfigureOneToOneFormView(JJFormView childFormView,
        FormElementRelationship relationship, Dictionary<string, object?> filter)
    {
        Dictionary<string, object?>? childValues = null;
        if (filter.Any())
        {
            childValues =
                await parentFormView.EntityRepository.GetFieldsAsync(childFormView.FormElement, filter!);
        }
        
        childFormView.IsChildFormView = true;

        var isDisabled = IsRelationshipDisabled(relationship);
        
        if (relationship.ViewType is RelationshipViewType.View ||
            parentFormView.PageState is PageState.View || 
            parentFormView.PanelState is PageState.Update || isDisabled)
        {
            childFormView.PageState = PageState.View;
            childFormView.PanelState = PageState.View;
        }
        else
        {
            childFormView.PageState = childValues is not null ? PageState.Update : PageState.Insert;

            if (childFormView.PanelState is null)
            {
                if (childValues is not null)
                    childFormView.PanelState = relationship.EditModeOpenByDefault ? PageState.Update: PageState.View;
                else
                    childFormView.PanelState = PageState.Insert;
            }
        }
        
        childFormView.RelationValues = DataHelper.GetRelationValues(parentFormView.FormElement, filter);
        childFormView.UserValues = new Dictionary<string, object>(parentFormView.UserValues);
        childFormView.ShowTitle = false;

        if (childValues is not null)
            childFormView.DataPanel.Values = childValues;
        
        childFormView.DataPanel.RenderPanelGroup = false;
        childFormView.DataPanel.FormUI = childFormView.FormElement.Options.Form;
                
        return await childFormView.GetFormResultAsync();
    }

    private bool IsRelationshipDisabled(FormElementRelationship relationship) => parentFormView.ExpressionsService.GetBoolValue(relationship.Panel.EnableExpression, GetFormStateData());
}