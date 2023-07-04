using System;
using System.Threading.Tasks;
using JJMasterData.Commons.Configuration;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.DI;
using JJMasterData.Commons.Tasks;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.FormEvents.Abstractions;
using JJMasterData.Core.FormEvents.Args;
using JJMasterData.Core.Web.Components;

namespace JJMasterData.Core.Web.Factories;

public class DataImportationFactory
{
    private IDataDictionaryRepository DataDictionaryRepository { get; }
    private IEntityRepository EntityRepository { get; }
    private IExpressionsService ExpressionsService { get; }
    private IFormFieldsService FormFieldsService { get; }
    private IBackgroundTask BackgroundTask { get; }
    
    private IFormService FormService { get; }

    private IFieldVisibilityService FieldVisibilityService { get; }
    private IFormEventResolver FormEventResolver { get; }

    public DataImportationFactory(
        IDataDictionaryRepository dataDictionaryRepository,
        IEntityRepository entityRepository, 
        IExpressionsService expressionsService, 
        IFormFieldsService formFieldsService, 
        IBackgroundTask backgroundTask,
        IFormService formService,
        IFieldVisibilityService fieldVisibilityService,
        IFormEventResolver formEventResolver
        )
    {
        DataDictionaryRepository = dataDictionaryRepository;
        EntityRepository = entityRepository;
        ExpressionsService = expressionsService;
        FormFieldsService = formFieldsService;
        BackgroundTask = backgroundTask;
        FormService = formService;
        FieldVisibilityService = fieldVisibilityService;
        FormEventResolver = formEventResolver;
    }

    public async Task<JJDataImp> CreateDataImpAsync(string elementName)
    {
        if (string.IsNullOrEmpty(elementName))
            throw new ArgumentNullException(nameof(elementName));
        
        var formElement = await DataDictionaryRepository.GetMetadataAsync(elementName);
        
        var dataContext = new DataContext(DataContextSource.Upload, DataHelper.GetCurrentUserId(null));
        
        var formEvent = FormEventResolver.GetFormEvent(elementName);
        formEvent?.OnFormElementLoad(dataContext, new FormElementLoadEventArgs(formElement));

        var dataImp = CreateDataImp(formElement);
        
        if (formEvent != null) 
            dataImp.OnBeforeImport += formEvent.OnBeforeImport;

        return dataImp;
    }
    
    public JJDataImp CreateDataImp(FormElement formElement)
    {
        return new JJDataImp(formElement, EntityRepository,ExpressionsService, FormFieldsService,FormService, FieldVisibilityService, BackgroundTask);
    }


}