﻿#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Commons.Data.Entity.Models;
using JJMasterData.Commons.Data.Entity.Repository;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJMasterData.Commons.Data.Entity.Providers;

public class SQLiteProvider(
    IOptionsSnapshot<MasterDataCommonsOptions> options,
    ILoggerFactory loggerFactory)
    : EntityProviderBase(options, loggerFactory)
{
    private const char Tab = '\t';
    public override string VariablePrefix => "@";

    public override string GetCreateTableScript(Element element, List<RelationshipReference>? relationships = null)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (element.Fields == null || element.Fields.Count == 0)
            throw new ArgumentNullException(nameof(element.Fields));

        StringBuilder sSql = new StringBuilder();

        sSql.AppendLine("-- TABLE");
        sSql.Append("CREATE TABLE [");
        sSql.Append(element.TableName);
        sSql.AppendLine("] (");
        bool isFirst = true;
        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        foreach (var f in fields)
        {
            if (isFirst)
                isFirst = false;
            else
                sSql.AppendLine(",");

            sSql.Append(Tab);
            sSql.Append('[');
            sSql.Append(f.Name);
            sSql.Append("] ");

            switch (f.DataType)
            {
                case FieldType.Int:
                    sSql.Append("INTEGER");
                    break;
                case FieldType.Float:
                    sSql.Append("Real");
                    break;
                case FieldType.NText:
                case FieldType.NVarchar:
                case FieldType.Varchar:
                    sSql.Append("TEXT");
                    break;
                default:
                    sSql.Append(f.DataType.ToString());
                    break;
            }

            if (f.IsRequired)
                sSql.Append(" NOT NULL");

            if (f.AutoNum && f.IsPk)
                sSql.Append(" PRIMARY KEY AUTOINCREMENT ");
        }

        isFirst = true;
        foreach (var f in fields.FindAll(x => x.IsPk && !x.AutoNum))
        {
            if (isFirst)
            {
                isFirst = false;
                sSql.AppendLine(",");
                sSql.Append(Tab);
                sSql.Append("PRIMARY KEY (");
            }
            else
            {
                sSql.Append(',');
            }

            sSql.Append('[');
            sSql.Append(f.Name);
            sSql.Append("] ");
        }

        if (!isFirst)
            sSql.Append(')');

        sSql.AppendLine("");
        sSql.AppendLine(")");
        sSql.AppendLine("GO");
        sSql.AppendLine("");

        //sSql.AppendLine(DoSqlCreateRelation(element));
        sSql.AppendLine("");

        int nIndex = 1;
        if (element.Indexes.Count > 0)
        {
            foreach (var index in element.Indexes)
            {
                sSql.Append("CREATE");
                sSql.Append(index.IsUnique ? " UNIQUE" : "");
                sSql.Append(index.IsClustered ? " CLUSTERED" : "");
                sSql.Append(" INDEX [IX_");
                sSql.Append(element.TableName);
                sSql.Append('_');
                sSql.Append(nIndex);
                sSql.Append("] ON ");
                sSql.AppendLine(element.TableName);

                sSql.Append(Tab);
                sSql.AppendLine("(");
                for (int i = 0; i < index.Columns.Count; i++)
                {
                    if (i > 0)
                        sSql.AppendLine(", ");

                    sSql.Append(Tab);
                    sSql.Append(index.Columns[i]);
                }

                sSql.AppendLine("");
                sSql.Append(Tab);
                sSql.AppendLine(")");
                sSql.AppendLine("GO");
                nIndex++;
            }
        }

        sSql.AppendLine("");
        return sSql.ToString();
    }


    // ReSharper disable once UnusedMember.Local
    private static string GetRelationshipsScript(Element element)
    {
        StringBuilder sSql = new StringBuilder();

        if (element.Relationships.Count > 0)
        {
            sSql.AppendLine("-- RELATIONSHIPS");
            var listConstraint = new List<string>();
            foreach (var r in element.Relationships)
            {
                string constraintName = $"FK_{r.ChildElement}_{element.TableName}";
                if (!listConstraint.Contains(constraintName))
                {
                    listConstraint.Add(constraintName);
                }
                else
                {
                    bool hasContraint = true;
                    for (int nCount = 1; hasContraint; nCount++)
                    {
                        if (!listConstraint.Contains(constraintName + nCount))
                        {
                            constraintName += nCount;
                            listConstraint.Add(constraintName);
                            hasContraint = false;
                        }
                    }
                }

                sSql.Append("ALTER TABLE ");
                sSql.AppendLine(r.ChildElement);
                sSql.Append("ADD CONSTRAINT [");
                sSql.Append(constraintName);
                sSql.AppendLine("] ");
                sSql.Append(Tab);
                sSql.Append("FOREIGN KEY (");

                for (int rc = 0; rc < r.Columns.Count; rc++)
                {
                    if (rc > 0)
                        sSql.Append(", ");

                    sSql.Append('[');
                    sSql.Append(r.Columns[rc].FkColumn);
                    sSql.Append(']');
                }

                sSql.AppendLine(")");
                sSql.Append(Tab);
                sSql.Append("REFERENCES ");
                sSql.Append(element.TableName);
                sSql.Append(" (");
                for (int rc = 0; rc < r.Columns.Count; rc++)
                {
                    if (rc > 0)
                        sSql.Append(", ");

                    sSql.Append('[');
                    sSql.Append(r.Columns[rc].PkColumn);
                    sSql.Append(']');
                }

                sSql.Append(')');

                if (r.UpdateOnCascade)
                {
                    sSql.AppendLine("");
                    sSql.Append(Tab).Append(Tab);
                    sSql.Append("ON UPDATE CASCADE ");
                }

                if (r.DeleteOnCascade)
                {
                    sSql.AppendLine("");
                    sSql.Append(Tab).Append(Tab);
                    sSql.Append("ON DELETE CASCADE ");
                }

                sSql.AppendLine("");
                sSql.AppendLine("GO");
            }
        }

        return sSql.ToString();
    }

    public override string GetWriteProcedureScript(Element element)
    {
        return string.Empty;
    }

    public override string GetReadProcedureScript(Element element)
    {
        return string.Empty;
    }

    public override Task<Element> GetElementFromTableAsync(string tableName, Guid? connectionId = null)
    {
        throw new NotImplementedException();
    }

    public override DataAccessCommand GetInsertCommand(Element element, Dictionary<string, object?> values)
    {
        return GetScriptInsert(element, values, false);
    }

    public override DataAccessCommand GetUpdateCommand(Element element, Dictionary<string, object?> values)
    {
        return GetScriptUpdate(element, values);
    }

    public override DataAccessCommand GetDeleteCommand(Element element, Dictionary<string, object> filters)
    {
        return GetScriptDelete(element, filters);
    }

    protected override DataAccessCommand GetInsertOrReplaceCommand(Element element, Dictionary<string, object?> values)
    {
        return GetScriptInsert(element, values, true);
    }

    public override bool TableExists(string tableName, Guid? connectionId = null)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> TableExistsAsync(string tableName, Guid? connectionId = null, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<bool> ColumnExistsAsync(string tableName, string columnName, Guid? connectionId = null,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public override Task<string?> GetStoredProcedureDefinitionAsync(string procedureName, Guid? connectionId = null)
    {
        throw new NotImplementedException();
    }

    public override Task DropStoredProcedureAsync(string procedureName, Guid? connectionId = null)
    {
        throw new NotImplementedException();
    }

    public override Task<List<string>> GetStoredProcedureListAsync(Guid? connectionId = null)
    {
        throw new NotImplementedException();
    }

    public override DataAccessCommand GetReadCommand(Element element, EntityParameters entityParameters,
        DataAccessParameter totalOfRecordsParameter)
    {
        var (filters, orderBy, currentPage, recordsPerPage) = entityParameters;
        var isFirst = true;
        var sqlScript = new StringBuilder();

        sqlScript.Append("SELECT * FROM ");
        sqlScript.Append(element.TableName);

        foreach (var filter in filters)
        {
            sqlScript.Append(Tab).Append(Tab);
            if (isFirst)
            {
                sqlScript.Append(" WHERE ");
                isFirst = false;
            }
            else
            {
                sqlScript.Append("AND ");
            }

            sqlScript.Append(filter.Key);
            sqlScript.Append(" = ");
            sqlScript.AppendLine("?");
        }

        if (!string.IsNullOrEmpty(orderBy.ToQueryParameter()))
        {
            sqlScript.Append(" ORDER BY ");
            sqlScript.Append(orderBy);
        }

        if ((int)totalOfRecordsParameter.Value == 0 && recordsPerPage > 0)
        {
            var offset = (currentPage - 1) * recordsPerPage;
            sqlScript.Append("LIMIT ");
            sqlScript.Append(recordsPerPage);
            sqlScript.Append(" OFFSET");
            sqlScript.Append(offset);
        }

        DataAccessCommand command = new DataAccessCommand
        {
            Type = CommandType.Text,
            Sql = sqlScript.ToString(),
        };

        foreach (var filter in filters)
        {
            ElementField f = element.Fields[filter.Key];
            var param = new DataAccessParameter
            {
                Direction = ParameterDirection.Input,
                Value = filter.Value,
                Type = GetDbType(f.DataType)
            };
            command.Parameters.Add(param);
        }

        return command;
    }

    private static DataAccessCommand GetScriptInsert(Element element, Dictionary<string, object?> values, bool isReplace)
    {
        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real
                          && !x.AutoNum);

        var sSql = new StringBuilder();
        if (isReplace)
            sSql.Append("REPLACE INTO ");
        else
            sSql.Append("INSERT INTO ");

        sSql.Append(element.TableName);
        sSql.Append(" (");

        bool isFirst = true;
        foreach (var c in fields)
        {
            if (isFirst)
                isFirst = false;
            else
                sSql.AppendLine(",");

            sSql.Append(c.Name);
        }

        sSql.Append(')');
        sSql.Append(" VALUES (");
        isFirst = true;
        foreach (var unused in fields)
        {
            if (isFirst)
                isFirst = false;
            else
                sSql.AppendLine(",");

            sSql.Append('?');
        }

        sSql.Append(')');

        var cmd = new DataAccessCommand
        {
            Type = CommandType.Text,
            Sql = sSql.ToString()
        };

        foreach (var f in fields)
        {
            object value = GetElementValue(f, values);
            var param = new DataAccessParameter
            {
                Direction = ParameterDirection.Input,
                Value = value,
                Type = GetDbType(f.DataType)
            };
            cmd.Parameters.Add(param);
        }

        return cmd;
    }

    private DataAccessCommand GetScriptUpdate(Element element, Dictionary<string, object?> values)
    {
        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        var sSql = new StringBuilder();
        sSql.Append("UPDATE ");
        sSql.Append(element.TableName);
        sSql.Append(" SET ");

        bool isFirst = true;
        foreach (var c in fields)
        {
            if (!c.IsPk)
            {
                if (isFirst)
                    isFirst = false;
                else
                    sSql.AppendLine(",");

                sSql.Append(c.Name);
                sSql.Append(" = ");
                sSql.Append(VariablePrefix + c.Name);
            }
        }

        isFirst = true;
        foreach (var f in fields)
        {
            if (f.IsPk)
            {
                sSql.Append(Tab).Append(Tab);
                if (isFirst)
                {
                    sSql.Append(" WHERE ");
                    isFirst = false;
                }
                else
                {
                    sSql.Append("AND ");
                }

                sSql.Append(f.Name);
                sSql.Append(" = ");
                sSql.AppendLine(VariablePrefix + f.Name);
            }
        }


        var cmd = new DataAccessCommand
        {
            Type = CommandType.Text,
            Sql = sSql.ToString()
        };

        foreach (var f in fields)
        {
            object value = GetElementValue(f, values);
            var param = new DataAccessParameter
            {
                Name = string.Format(VariablePrefix + f.Name),
                //param.Size = f.Size;
                Direction = ParameterDirection.Input,
                Value = value,
                Type = GetDbType(f.DataType)
            };
            cmd.Parameters.Add(param);
        }

        return cmd;
    }

    private DataAccessCommand GetScriptDelete(Element element, Dictionary<string, object> values)
    {
        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        bool isFirst = true;
        var sqlScript = new StringBuilder();

        sqlScript.Append("DELETE FROM ");
        sqlScript.Append(element.TableName);
        foreach (var f in fields)
        {
            if (f.IsPk)
            {
                sqlScript.Append(Tab).Append(Tab);
                if (isFirst)
                {
                    sqlScript.Append(" WHERE ");
                    isFirst = false;
                }
                else
                {
                    sqlScript.Append("AND ");
                }

                sqlScript.Append(f.Name);
                sqlScript.Append(" = ");
                sqlScript.AppendLine(VariablePrefix + f.Name);
            }
        }

        var cmd = new DataAccessCommand
        {
            Type = CommandType.Text,
            Sql = sqlScript.ToString()
        };

        foreach (var f in fields)
        {
            object value = GetElementValue(f, values!);
            var param = new DataAccessParameter
            {
                Name = string.Format(VariablePrefix + f.Name),
                Direction = ParameterDirection.Input,
                Value = value,
                Type = GetDbType(f.DataType)
            };
            cmd.Parameters.Add(param);
        }

        return cmd;
    }

    private static object GetElementValue(ElementField f, Dictionary<string, object?> values)
    {
        if (!values.TryGetValue(f.Name, out object? value))
            return DBNull.Value;

        if (value == null)
        {
            return DBNull.Value;
        }

        if (f.DataType is FieldType.Date or FieldType.DateTime or FieldType.Float or FieldType.Int &&
            string.IsNullOrEmpty(value.ToString()))
        {
            return DBNull.Value;
        }

        return value;
    }

    private static DbType GetDbType(FieldType dataType)
    {
        DbType t = DbType.AnsiString;
        switch (dataType)
        {
            case FieldType.Date:
                t = DbType.Date;
                break;
            case FieldType.DateTime:
                t = DbType.DateTime;
                break;
            case FieldType.DateTime2:
                t = DbType.DateTime2;
                break;
            case FieldType.Float:
                t = DbType.Single;
                break;
            case FieldType.Int:
                t = DbType.Int32;
                break;
        }

        return t;
    }

    // ReSharper disable once UnusedMember.Local
    private static DataAccessCommand GetScriptCount(Element element, Dictionary<string, object?> filters)
    {
        var fields = element.Fields
            .ToList()
            .FindAll(x => x.DataBehavior == FieldBehavior.Real);

        var isFirst = true;
        var sqlScript = new StringBuilder();

        sqlScript.Append("SELECT Count(*) FROM ");
        sqlScript.Append(element.TableName);

        foreach (var f in fields)
        {
            if (f.IsPk)
            {
                sqlScript.Append(Tab).Append(Tab);
                if (isFirst)
                {
                    sqlScript.Append(" WHERE ");
                    isFirst = false;
                }
                else
                {
                    sqlScript.Append("AND ");
                }

                sqlScript.Append(f.Name);
                sqlScript.Append(" = ");
                sqlScript.AppendLine("?");
            }
        }

        var cmd = new DataAccessCommand();
        cmd.Sql = sqlScript.ToString();

        foreach (var f in fields)
        {
            object value = GetElementValue(f, filters);
            var param = new DataAccessParameter
            {
                //param.Name = string.Format(f.Name);
                //param.Size = f.Size;
                Direction = ParameterDirection.Input,
                Value = value,
                Type = GetDbType(f.DataType)
            };
            cmd.Parameters.Add(param);
        }

        return cmd;
    }


    public override string GetAlterTableScript(Element element, IEnumerable<ElementField> fields)
    {
        throw new NotImplementedException();
    }
}