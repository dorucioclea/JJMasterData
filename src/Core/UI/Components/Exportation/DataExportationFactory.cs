using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Options;
using JJMasterData.Core.UI.Components;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Components.Scripts;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace JJMasterData.Core.Web.Factories;

internal class DataExportationFactory : IFormElementComponentFactory<JJDataExp>
{
    private readonly JJMasterDataUrlHelper _urlHelper;
    private readonly JJMasterDataEncryptionService _encryptionService;
    private IEntityRepository EntityRepository { get; }
    private IDataDictionaryRepository DataDictionaryRepository { get; }
    private IExpressionsService ExpressionsService { get; }

    private IFieldValuesService FieldValuesService { get; }
    private DataExportationScripts Scripts { get; }
    private IOptions<JJMasterDataCoreOptions> Options { get; }
    private IBackgroundTask BackgroundTask { get; }
    private IHttpContext HttpContext { get; }
    private IComponentFactory<JJFileDownloader> FileDownloaderFactory { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }
    private ILoggerFactory LoggerFactory { get; }

    public DataExportationFactory(
        IEntityRepository entityRepository,
        IDataDictionaryRepository dataDictionaryRepository,
        IExpressionsService expressionsService,
        IFieldValuesService fieldValuesService,
        DataExportationScripts scripts,
        IOptions<JJMasterDataCoreOptions> options,
        IBackgroundTask backgroundTask,
        IHttpContext httpContext,
        IStringLocalizer<JJMasterDataResources> stringLocalizer,
        ILoggerFactory loggerFactory,
        IComponentFactory<JJFileDownloader> fileDownloaderFactory,
        JJMasterDataUrlHelper urlHelper,
        JJMasterDataEncryptionService encryptionService
        )
    {
        _urlHelper = urlHelper;
        _encryptionService = encryptionService;
        EntityRepository = entityRepository;
        DataDictionaryRepository = dataDictionaryRepository;
        ExpressionsService = expressionsService;
        FieldValuesService = fieldValuesService;
        Scripts = scripts;
        Options = options;
        BackgroundTask = backgroundTask;
        HttpContext = httpContext;
        StringLocalizer = stringLocalizer;
        LoggerFactory = loggerFactory;
        FileDownloaderFactory = fileDownloaderFactory;
    }

    public async Task<JJDataExp> CreateAsync(string dictionaryName)
    {
        var formElement = await DataDictionaryRepository.GetMetadataAsync(dictionaryName);
        return Create(formElement);
    }

    public JJDataExp Create(FormElement formElement)
    {
        return new JJDataExp(formElement, EntityRepository, ExpressionsService, FieldValuesService, 
            Options, BackgroundTask, StringLocalizer, FileDownloaderFactory,LoggerFactory, HttpContext, 
            _urlHelper, _encryptionService);
    }
}