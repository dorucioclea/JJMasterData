using JJMasterData.Commons.Data.Entity.Repository.Abstractions;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataManager.Expressions;
using JJMasterData.Core.DataManager.Services;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;

namespace JJMasterData.Core.DataManager.Importation;

public class DataImportationWorkerFactory
{
    public ILoggerFactory LoggerFactory { get; }

    public IStringLocalizer<MasterDataResources> StringLocalizer { get; }

    public FieldValuesService FieldValuesService { get; }

    public IEntityRepository EntityRepository { get; }

    public ExpressionsService ExpressionsService { get; }

    public FormService FormService { get; }

    public DataImportationWorkerFactory(FormService formService, ExpressionsService expressionsService,
        IEntityRepository entityRepository, FieldValuesService fieldValuesService,
        IStringLocalizer<MasterDataResources> stringLocalizer, ILoggerFactory loggerFactory)
    {
        FormService = formService;
        ExpressionsService = expressionsService;
        EntityRepository = entityRepository;
        FieldValuesService = fieldValuesService;
        StringLocalizer = stringLocalizer;
        LoggerFactory = loggerFactory;
    }

    public DataImportationWorker Create(DataImportationContext context)
    {
        return new DataImportationWorker(context, FormService, ExpressionsService, EntityRepository, FieldValuesService,
            StringLocalizer, LoggerFactory.CreateLogger<DataImportationWorker>());
    }
}