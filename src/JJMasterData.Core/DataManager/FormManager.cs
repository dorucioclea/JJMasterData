﻿using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using JJMasterData.Commons.Data.Entity.Abstractions;
using JJMasterData.Commons.Localization;

namespace JJMasterData.Core.DataManager;

public class FormManager
{
    public IEntityRepository EntityRepository => Expression.EntityRepository;

    public ExpressionManager Expression { get; private set; }

    public FormElement FormElement { get; private set; }

    public FormManager(FormElement formElement, ExpressionManager expression)
    {
        if (formElement == null)
            throw new ArgumentNullException(nameof(formElement));

        if (expression == null)
            throw new ArgumentNullException(nameof(expression));

        FormElement = formElement;
        Expression = expression;
    }

    /// <summary>
    /// Validates form fields and returns a list of errors found
    /// </summary>
    /// <param name="formValues">Form values</param>
    /// <param name="pageState">Context</param>
    /// <param name="enableErrorLink">Add html link in error fields</param>
    /// <returns>
    /// Key = Field name
    /// Value = Error message
    /// </returns>
    public IDictionary ValidateFields(IDictionary formValues, PageState pageState, bool enableErrorLink)
    {
        if (formValues == null)
            throw new ArgumentNullException(nameof(formValues));

        var errors = new Dictionary<string,dynamic>(StringComparer.InvariantCultureIgnoreCase);

        foreach (var field in FormElement.Fields)
        {
            bool isVisible = Expression.GetBoolValue(field.VisibleExpression, field.Name, pageState, formValues);
            if (!isVisible)
                continue;

            bool isEnable = Expression.GetBoolValue(field.EnableExpression, field.Name, pageState, formValues);
            if (!isEnable)
                continue;

            string value;
            if (formValues.Contains(field.Name) && formValues[field.Name] != null)
                value = formValues[field.Name].ToString();
            else
                value = "";

            var error = FieldValidator.ValidateField(field, field.Name, value, enableErrorLink);
            if (!string.IsNullOrEmpty(error))
                errors.Add(field.Name, error);
        }
        return errors;
    }

    /// <summary>
    /// Apply default and triggers expression values
    /// </summary> 
    /// <param name="formValues">Form values</param>
    /// <param name="pageState">Context</param>
    /// <param name="replaceNullValues">Change the field's default value even if it is empty</param>
    /// <returns>
    /// Returns a new hashtable with the updated values
    /// </returns>
    public IDictionary MergeWithExpressionValues(IDictionary formValues, PageState pageState, bool replaceNullValues)
    {
        if (formValues == null)
            throw new ArgumentNullException(Translate.Key("Invalid parameter or not found"), nameof(formValues));

        IDictionary newValues = new Dictionary<string,dynamic>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var f in FormElement.Fields)
        {
            if (formValues.Contains(f.Name))
            {
                object val = ClearSpecialChars(f, formValues[f.Name]);
                newValues.Add(f.Name, val);
            }
        }

        ApplyDefaultValues(ref newValues, pageState, replaceNullValues);
        ApplyTriggerValues(ref newValues, pageState);

        return newValues;
    }

    public Hashtable GetDefaultValues(IDictionary formValues, PageState state)
    {
        var filters = new Hashtable(StringComparer.InvariantCultureIgnoreCase);
        var list = FormElement.Fields
            .ToList()
            .FindAll(x => !string.IsNullOrEmpty(x.DefaultValue));

        foreach (var e in list)
        {
            string val = Expression.GetDefaultValue(e, state, formValues);
            if (!string.IsNullOrEmpty(val))
            {
                filters.Add(e.Name, val);
            }
        }

        return filters;
    }

    public IDictionary MergeWithDefaultValues(IDictionary formValues, PageState pageState)
    {
        IDictionary values = new Dictionary<string,dynamic>(StringComparer.InvariantCultureIgnoreCase);
        if (formValues != null)
        {
            foreach (DictionaryEntry v in formValues)
                values.Add(v.Key.ToString(), v.Value);
        }

        ApplyDefaultValues(ref values, pageState, false);
        return values;
    }

    private void ApplyDefaultValues(ref IDictionary formValues, PageState pageState, bool replaceNullValues)
    {
        var defaultValues = GetDefaultValues(formValues, pageState);
        if (defaultValues == null)
            return;

        foreach (DictionaryEntry d in defaultValues)
        {
            if (!formValues.Contains(d.Key))
            {
                formValues.Add(d.Key, d.Value);
            }
            else
            {
                if ((formValues[d.Key] == null || string.IsNullOrEmpty(formValues[d.Key].ToString()))
                    && replaceNullValues)
                {
                    formValues[d.Key] = d.Value;
                }
            }
        }
    }

    private void ApplyTriggerValues(ref IDictionary formValues, PageState pageState)
    {
        var listFields = FormElement.Fields
            .ToList()
            .FindAll(x => !string.IsNullOrEmpty(x.TriggerExpression));
        foreach (var e in listFields)
        {
            string val = Expression.GetTriggerValue(e, pageState, formValues);
            if (val != null)
            {
                if (formValues.Contains(e.Name))
                    formValues[e.Name] = val;
                else
                    formValues.Add(e.Name, val);
            }
        }
    }

    private object ClearSpecialChars(FormElementField f, object val)
    {
        if (val != null)
        {
            if (f.Component == FormComponent.Cnpj ||
                f.Component == FormComponent.Cnpj ||
                f.Component == FormComponent.CnpjCpf)
            {
                val = StringManager.ClearCpfCnpjChars(val.ToString());
            }
            else if (f.Component == FormComponent.Tel)
            {
                val = StringManager.ClearTelChars(val.ToString());
            }
            else if (f.Component == FormComponent.Cep)
            {
                val = val.ToString().Replace("-", "");
            }
        }

        return val;
    }

    public IList<DataItemValue> GetDataItemValues(FormElementDataItem DataItem, IDictionary formValues, PageState pageState)
    {
        if (DataItem == null)
            return null;

        IList<DataItemValue> values = new List<DataItemValue>();
        if (DataItem.Command != null && !string.IsNullOrEmpty(DataItem.Command.Sql))
        {

            string sql = DataItem.Command.Sql;
            if (sql.Contains("{"))
            {
                sql = Expression.ParseExpression(sql, pageState, false, formValues);
            }

            DataTable dt = EntityRepository.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                var item = new DataItemValue
                {
                    Id = row[0].ToString(),
                    Description = row[1].ToString()?.Trim()
                };
                if (DataItem.ShowImageLegend)
                {
                    item.Icon = (IconType)int.Parse(row[2].ToString() ?? string.Empty);
                    item.ImageColor = row[3].ToString();
                }
                values.Add(item);
            }
        }
        else
        {
            values = DataItem.Items;
        }

        return values;
    }

}