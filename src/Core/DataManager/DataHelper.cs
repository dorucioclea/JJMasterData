﻿#nullable enable

using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Core.DataDictionary;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.DataManager;

public static class DataHelper
{
    public static string? GetCurrentUserId(IHttpSession session, IDictionary<string, object>? userValues)
    {
        if (userValues != null && userValues.TryGetValue("USERID", out var value))
        {
            return value.ToString();
        }

        return session["USERID"];
    }

    public static IDictionary<string, object?> GetElementValues(Element element, IDictionary<string, object?> values)
    {
        var elementValues = new Dictionary<string, object?>();

        foreach (var entry in values)
        {
            if (element.Fields.ContainsKey(entry.Key))
            {
                elementValues[entry.Key] = entry.Value;
            }
        }

        return elementValues;
    }
    
    public static bool ContainsPkValues(Element element, IDictionary<string, object?> values)
    {
        var elementPks = GetElementPrimaryKeys(element);

        return elementPks.Count != 0 && elementPks.All(field => values.ContainsKey(field.Name));
    }

    public static IDictionary<string, object> GetPkValues(Element element, IDictionary<string, object?> values)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var primaryKeys = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
        var elementPks = GetElementPrimaryKeys(element);

        if (elementPks.Count == 0)
            throw new JJMasterDataException($"Primary key not defined for dictionary {element.Name}");

        foreach (var field in elementPks)
        {
            if (!values.ContainsKey(field.Name))
                throw new JJMasterDataException($"Primary key from {field.Name} not entered");

            var value = values[field.Name];

            if (value is null)
                throw new JJMasterDataException($"Primary key value from {field.Name} cannot be null");
            
            primaryKeys.Add(field.Name, value);
        }

        return primaryKeys;
    }

    public static Dictionary<string, object> GetPkValues(Element element, string parsedValues, char separator)
    {
        var primaryKeys = new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);

        var values = parsedValues.Split(separator);
        if (values == null || values.Length == 0)
            throw new ArgumentException("Invalid parameter or not found in values");

        var elementPks = element.Fields.ToList().FindAll(x => x.IsPk);
        if (values.Length != elementPks.Count)
            throw new JJMasterDataException("Invalid primary key");

        for (int i = 0; i < values.Length; i++)
        {
            object value = values[i];
            if (DateTime.TryParse(value.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None,out var invariantDateValue))
            {
                value = invariantDateValue;
            }
            primaryKeys.Add(elementPks[i].Name, value);
        }

        return primaryKeys;
    }
    
    private static List<ElementField> GetElementPrimaryKeys(Element element)
    {
        return element.Fields.Where(x => x.IsPk).ToList();
    }

    public static IDictionary<string, object> GetFkValues(FormElement formElement, IDictionary<string, object?> values)
    {
        var foreignKeys = new Dictionary<string, object>();
        var relationships = formElement.Relationships.GetElementRelationships();

        foreach (var entry in values)
        {
            var matchingRelationship = relationships.FirstOrDefault(r => r.Columns.Any(c => c.FkColumn == entry.Key));

            if (matchingRelationship != null)
            {
                var matchingColumn = matchingRelationship.Columns.First(c => c.FkColumn == entry.Key);

                if (entry.Value is not null)
                {
                    foreignKeys[matchingColumn.FkColumn] = entry.Value;
                }
            }
        }

        return foreignKeys;
    }
    
    /// <summary>
    /// Concat primary keys with separator characters
    /// </summary>
    public static string ParsePkValues(FormElement formElement, IDictionary<string, object?> formValues, char separator)
    {
        if (formElement == null)
            throw new ArgumentNullException(nameof(formElement));

        if (formValues == null)
            throw new ArgumentNullException(nameof(formValues));

        var elementPks = formElement.Fields.ToList().FindAll(x => x.IsPk);
        if (elementPks == null || elementPks.Count == 0)
            throw new JJMasterDataException($"Primary key not defined for dictionary {formElement.Name}");

        string name = string.Empty;
        foreach (var field in elementPks)
        {
            if (name.Length > 0)
                name += separator.ToString();
                
            if (!formValues.ContainsKey(field.Name))
                throw new JJMasterDataException($"Primary key {field.Name} not entered");


            string value;
            
            if (field.DataType is FieldType.DateTime or FieldType.Date)
            {
                if (DateTime.TryParse(formValues[field.Name]?.ToString(), CultureInfo.CurrentCulture, DateTimeStyles.None,out DateTime dateValue))
                {
                    value = dateValue.ToString(CultureInfo.InvariantCulture);
                }
                else if (DateTime.TryParse(formValues[field.Name]?.ToString(), CultureInfo.InvariantCulture, DateTimeStyles.None,out DateTime invariantDateValue))
                {
                    value = invariantDateValue.ToString(CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new JJMasterDataException($"Invalid DateTime for field {field.Name}: {formValues[field.Name]}");
                }
            }
            else
            {
                value = formValues[field.Name]?.ToString() ?? throw new JJMasterDataException($"Primary key value from {field.Name} cannot be null");
            }
            
            if (value.Contains(separator))
                throw new JJMasterDataException($"Primary key value {value} contains invalid characters.");
                
            name += value;
        }

        return name;
    }

    /// <summary>
    /// Preserves the original name of the field as registered in the dictionary
    /// and validates if the field exists
    /// </summary>
    public static IDictionary<string, object?>? ParseOriginalName(FormElement formElement, IDictionary<string, object?>? paramValues)
    {
        if (paramValues == null)
            return null;

        var filters = new Dictionary<string, object?>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var entry in paramValues)
        {
            var field = formElement.Fields[entry.Key];
            if (!filters.ContainsKey(entry.Key))
                filters.Add(field.Name, entry.Value);
        }

        return filters;
    }

    public static void CopyIntoDictionary(IDictionary<string, object?> valuesToBeReceived, IDictionary<string, object?>? valuesToBeCopied, bool replaceIfExistKey)
    {
        if (valuesToBeCopied == null || valuesToBeCopied.Count == 0)
            return;

        foreach (var entry in valuesToBeCopied)
        {
            if (valuesToBeReceived.ContainsKey(entry.Key))
            {
                if (replaceIfExistKey)
                    valuesToBeReceived[entry.Key] = entry.Value;
            }
            else
            {
                valuesToBeReceived.Add(entry.Key, entry.Value);
            }
        }
    }

}