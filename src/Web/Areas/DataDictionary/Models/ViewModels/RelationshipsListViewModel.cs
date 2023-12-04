using JJMasterData.Core.DataDictionary.Models;

namespace JJMasterData.Web.Areas.DataDictionary.Models.ViewModels;

public class RelationshipsListViewModel(string elementName, string menuId) : DataDictionaryViewModel(elementName, menuId)
{
    public required FormElementRelationshipList Relationships { get; init; }
}