using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Core.DataDictionary.Models;

namespace JJMasterData.Web.Areas.DataDictionary.Models.ViewModels;

public class RelationshipsLayoutDetailsViewModel : DataDictionaryViewModel
{
    public required int Id { get; set; }
    public required bool IsParent { get; set; }
    public required RelationshipViewType ViewType { get; set; }
    public required FormElementPanel Panel { get; set; } = new();
    
    // ReSharper disable once UnusedMember.Global
    // Reason: Used for model binding.
    public RelationshipsLayoutDetailsViewModel()
    {
    }
    public RelationshipsLayoutDetailsViewModel(string elementName, string menuId) : base(elementName, menuId)
    {
    }
}