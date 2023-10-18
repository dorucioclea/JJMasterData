using System.Collections.Generic;
using System.Threading.Tasks;
using JJMasterData.Core.DataDictionary.Models;


namespace JJMasterData.Core.DataManager.Services;

public class FieldsService 
{
    private FieldFormattingService FieldFormattingService { get; }
    private FieldValidationService FieldValidationService { get; }
    private FieldValuesService  FieldValuesService { get; }
    
    public FieldsService(
        FieldFormattingService fieldFormattingService,
        FieldValuesService fieldValuesService,
        FieldValidationService fieldValidationService)
    {
        FieldFormattingService = fieldFormattingService;
        FieldValidationService = fieldValidationService;
        FieldValuesService = fieldValuesService;
    }
    
    public IDictionary<string, string> ValidateFields(FormElement formElement, IDictionary<string, object> formValues, PageState pageState, bool enableErrorLink)
    {
       return FieldValidationService.ValidateFields(formElement, formValues, pageState, enableErrorLink);
    }

    public string ValidateField(FormElementField field, string fieldId, string value, bool enableErrorLink = true)
    {
        return FieldValidationService.ValidateField(field, fieldId, value, enableErrorLink);
    }

    public async Task<string> FormatGridValueAsync(FormElementField field, IDictionary<string, object> values, IDictionary<string, object> userValues)
    {
        return await FieldFormattingService.FormatGridValueAsync(field, values, userValues);
    }

    public string FormatValue(FormElementField field, object value)
    {
        return FieldFormattingService.FormatValue(field, value);
    }

    public async Task<IDictionary<string, object>> MergeWithExpressionValuesAsync(FormElement formElement, IDictionary<string, object> formValues, PageState pageState,
        bool replaceNullValues)
    {
        return await FieldValuesService.MergeWithExpressionValuesAsync(formElement, formValues, pageState, replaceNullValues);
    }

    public async Task<IDictionary<string, object>> GetDefaultValuesAsync(FormElement formElement, IDictionary<string, object> formValues, PageState pageState)
    {
        return await FieldValuesService.GetDefaultValuesAsync(formElement, formValues, pageState);
    }

    public async Task<IDictionary<string, object>>  MergeWithDefaultValuesAsync(FormElement formElement, IDictionary<string, object> formValues, PageState pageState)
    {
        return await FieldValuesService.MergeWithDefaultValuesAsync(formElement, formValues, pageState);
    }
}