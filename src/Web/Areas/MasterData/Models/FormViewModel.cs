using JJMasterData.Core.Web.Components;

namespace JJMasterData.Web.Areas.MasterData.Models;

public record FormViewModel(string DictionaryName, Action<JJFormView> Configure, bool IsBlazor);