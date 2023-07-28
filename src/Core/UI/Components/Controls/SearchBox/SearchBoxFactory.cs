using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.DataDictionary.Repository.Abstractions;
using JJMasterData.Core.DataManager;
using JJMasterData.Core.DataManager.Services.Abstractions;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.Web.Factories;

internal class SearchBoxFactory : IControlFactory<JJSearchBox>
{
    private IEntityRepository EntityRepository { get; }
    private IDataItemService DataItemService { get; }
    private IDataDictionaryRepository DataDictionaryRepository { get; }
    private IFormValuesService FormValuesService { get; }
    private IHttpContext HttpContext { get; }
    private JJMasterDataEncryptionService EncryptionService { get; }
    private JJMasterDataUrlHelper UrlHelper { get; }

    public SearchBoxFactory(
        IEntityRepository entityRepository,
        IDataItemService dataItemService,
        IDataDictionaryRepository dataDictionaryRepository, 
        IFormValuesService formValuesService,
        IHttpContext httpContext, JJMasterDataEncryptionService encryptionService, JJMasterDataUrlHelper urlHelper)
    {
        EntityRepository = entityRepository;
        DataItemService = dataItemService;
        DataDictionaryRepository = dataDictionaryRepository;
        FormValuesService = formValuesService;
        HttpContext = httpContext;
        EncryptionService = encryptionService;
        UrlHelper = urlHelper;
    }
    
    public JJSearchBox Create()
    {
        return new JJSearchBox(HttpContext,EncryptionService,DataItemService, UrlHelper);
    }
    
    public JJSearchBox Create(FormElement formElement, FormElementField field, FormStateData formStateData, string parentName, object value)
    {
        var search = new JJSearchBox(formStateData, HttpContext,EncryptionService,DataItemService, UrlHelper)
        {
            DataItem = field.DataItem,
            Name = field.Name,
            FieldName = field.Name,
            DictionaryName = formElement.Name,
            SelectedValue = value?.ToString(),
            Visible = true,
            AutoReloadFormFields = false
        };

        return search;
    }
    
    public async Task<JJSearchBox> CreateAsync(string dictionaryName, string fieldName, PageState pageState, IDictionary<string,dynamic>userValues)
    {
        if (string.IsNullOrEmpty(dictionaryName))
            return null;

        IDictionary<string,dynamic>formValues = null;
        var formElement = await DataDictionaryRepository.GetMetadataAsync(dictionaryName);
        var dataItem = formElement.Fields[fieldName].DataItem;
        if (dataItem == null)
            throw new ArgumentNullException(nameof(dataItem));

        if (dataItem.HasSqlExpression())
        {
            formValues = FormValuesService.GetFormValuesWithMergedValuesAsync(formElement,pageState, true).GetAwaiter().GetResult();
        }

        var field = formElement.Fields[fieldName];
        var expOptions = new FormStateData(userValues, formValues, pageState);
        return Create(formElement,field, expOptions, null, dictionaryName);
    }
}