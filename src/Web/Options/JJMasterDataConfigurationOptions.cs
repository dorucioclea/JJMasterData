using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Core.Options;
using JJMasterData.Web.Areas.DataDictionary.Models;

namespace JJMasterData.Web.Options;

public class JJMasterDataConfigurationOptions
{
    public JJMasterDataCommonsOptions JJMasterDataCommons { get;  } = new();
    public JJMasterDataCoreOptions JJMasterDataCore { get; } = new();
    public JJMasterDataWebOptions JJMasterDataWeb{ get; } = new();
    public ConnectionStrings ConnectionStrings { get;  } = new();
    public ConnectionProviders ConnectionProviders { get;  } = new();
}