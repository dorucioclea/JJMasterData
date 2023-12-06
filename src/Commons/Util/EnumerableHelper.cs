﻿#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using JJMasterData.Commons.Extensions;

namespace JJMasterData.Commons.Util;

public static class EnumerableHelper
{
    
    public static List<Dictionary<string, object?>> ConvertToDictionaryList(DataTable dataTable)
    {
        var list = new List<Dictionary<string, object?>>();

        foreach (DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary<string, object?>();

            foreach (DataColumn col in dataTable.Columns)
            {
                dict[col.ColumnName] = row.IsNull(col) ? null : row[col];
            }

            list.Add(dict);
        }

        return list;
    }
    
    public static DataTable ConvertToDataTable<T>(IEnumerable<T> list)
    {
        var table = CreateDataTable<T>();
        var entityType = typeof(T);
        var properties = TypeDescriptor.GetProperties(entityType);
        foreach (T item in list)
        {
            var row = table.NewRow();
            foreach (PropertyDescriptor prop in properties)
            {
                row[prop.Name] = prop.GetValue(item)!;
            }
            table.Rows.Add(row);
        }
        return table;
    }

    private static DataTable CreateDataTable<T>()
    {
        var entityType = typeof(T);
        var table = new DataTable(entityType.Name);
        var properties = TypeDescriptor.GetProperties(entityType);
        foreach (PropertyDescriptor prop in properties)
        {
            var type = Nullable.GetUnderlyingType(prop.PropertyType) != null ? typeof(string) : prop.PropertyType;
            var dataColumn = new DataColumn(prop.Name, type);
 
            var propertyInfo = prop.ComponentType.GetProperty(prop.Name);

            dataColumn.Caption = propertyInfo.GetDisplayName();
     
   
            table.Columns.Add(dataColumn);
          
        }
        return table;
    }
}