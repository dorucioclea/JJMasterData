﻿using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Commons.Data.Entity.Models;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JJMasterData.Commons.Data.Entity.Providers;

public class SqlServerWriteProcedureScripts(
    IOptions<MasterDataCommonsOptions> options,
    SqlServerInfo sqlServerInfo)
    : SqlServerScriptsBase
{
    private const string InsertInitial = "I";
    private const string UpdateInitial = "A";
    private const string DeleteInitial = "E";
    private MasterDataCommonsOptions Options { get; } = options.Value;
    private SqlServerInfo SqlServerInfo { get; } = sqlServerInfo;

    public string GetWriteProcedureScript(Element element)
    {
        if (element == null)
            throw new ArgumentNullException(nameof(element));

        if (element.Fields == null || element.Fields.Count == 0)
            throw new ArgumentNullException(nameof(Element.Fields));

        var sql = new StringBuilder();
        string procedureFinalName = Options.GetWriteProcedureName(element);

        if (SqlServerInfo.GetCompatibilityLevel() >= 130)
        {
            sql.Append("CREATE OR ALTER PROCEDURE [");
        }
        else
        {
            sql.AppendLine(GetSqlDropIfExists(procedureFinalName));
            sql.Append("CREATE PROCEDURE [");
        }

        sql.Append(procedureFinalName);
        sql.AppendLine("] ");
        sql.AppendLine("@action varchar(1), ");

        var fields = element.Fields
            .Where(f => f.DataBehavior is FieldBehavior.Real)
            .ToList();

        foreach (var field in fields)
        {
            sql.Append("@");
            sql.Append(field.Name);
            sql.Append(" ");
            sql.Append(field.DataType);
            if (field.DataType is FieldType.Varchar or FieldType.NVarchar or FieldType.DateTime2)
            {
                sql.Append("(");
                sql.Append(field.Size == -1 ? "MAX" : field.Size);
                sql.Append(")");
            }

            if (!field.IsRequired)
            {
                sql.Append(" = NULL");
            }

            sql.AppendLine(", ");
        }

        sql.AppendLine("@RET INT OUTPUT ");
        sql.AppendLine("AS ");
        sql.AppendLine("BEGIN ");
        sql.Append(GetWriteScript(element, fields));
        sql.AppendLine("END ");
        sql.AppendLine("GO ");

        return sql.ToString();
    }

    internal static string GetWriteScript(Element element, IReadOnlyCollection<ElementField> fields)
    {
        var sql = new StringBuilder();
        var pks = element.Fields.ToList().FindAll(x => x.IsPk);
        bool updateScript = HasUpdateFields(element);
        sql.Append(Tab);
        sql.AppendLine("DECLARE @TYPEACTION VARCHAR(1) ");
        sql.Append(Tab);
        sql.AppendLine("SET @TYPEACTION = @action ");
        sql.Append(Tab);
        sql.AppendLine("IF @TYPEACTION = ' ' ");
        sql.Append(Tab);
        sql.AppendLine("BEGIN ");
        sql.Append(Tab).Append(Tab);
        sql.AppendLine($"SET @TYPEACTION = '{InsertInitial}' ");

        bool isFirst = true;
        if (pks.Count > 0)
        {
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("DECLARE @NCOUNT INT ");
            sql.AppendLine(" ");

            //Check
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("SELECT @NCOUNT = COUNT(*) ");
            sql.Append(Tab).Append(Tab);
            sql.Append("FROM ");
            sql.Append(element.TableName);
            sql.AppendLine(" WITH (NOLOCK) ");

            foreach (var f in pks)
            {
                if (isFirst)
                {
                    sql.Append(Tab).Append(Tab);
                    sql.Append("WHERE ");
                    isFirst = false;
                }
                else
                {
                    sql.Append(Tab).Append(Tab);
                    sql.Append("AND ");
                }

                sql.Append(f.Name);
                sql.Append(" = @");
                sql.AppendLine(f.Name);
            }

            sql.AppendLine(" ");
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("IF @NCOUNT > 0 ");
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("BEGIN ");
            sql.Append(Tab).Append(Tab).Append(Tab);
            sql.AppendLine($"SET @TYPEACTION = '{UpdateInitial}'");
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("END ");
        }

        sql.Append(Tab);
        sql.AppendLine("END ");
        sql.AppendLine(" ");


        //Insert Script
        sql.Append(Tab);
        sql.AppendLine($"IF @TYPEACTION = '{InsertInitial}' ");
        sql.Append(Tab);
        sql.AppendLine("BEGIN ");
        sql.Append(Tab).Append(Tab);
        sql.Append("INSERT INTO [");
        sql.Append(element.TableName);
        sql.AppendLine("] (");

        isFirst = true;

        foreach (var field in fields.Where(f => !f.AutoNum))
        {
            if (isFirst)
                isFirst = false;
            else
                sql.AppendLine(",");

            sql.Append("			");
            sql.Append($"[{field.Name}]");
        }

        sql.AppendLine(")");
        sql.AppendLine();

        var autonumericFields = pks.FindAll(x => x.AutoNum);

        if (autonumericFields.Any())
        {
            sql.Append(Tab, 2);
            sql.Append("OUTPUT ");
        }

        for (var i = 0; i < autonumericFields.Count; i++)
        {
            var fieldName = autonumericFields[i].Name;

            if (i > 0)
            {
                sql.Append(",");
            }

            sql.Append("Inserted." + fieldName);
        }

        sql.AppendLine();
        sql.Append(Tab, 2);
        sql.AppendLine("VALUES (");

        isFirst = true;
        foreach (var f in fields.Where(f => !f.AutoNum))
        {
            if (isFirst)
                isFirst = false;
            else
                sql.AppendLine(",");

            sql.Append(Tab).Append(Tab).Append(Tab);
            sql.Append("@");
            sql.Append(f.Name);
        }

        sql.AppendLine(")");
        sql.Append(Tab).Append(Tab);
        sql.AppendLine("SET @RET = 0; ");

        sql.Append(Tab);
        sql.AppendLine("END ");

        //Update Script
        if (updateScript)
        {
            sql.Append(Tab);
            sql.AppendLine($"ELSE IF @TYPEACTION = '{UpdateInitial}' ");
            sql.Append(Tab);
            sql.AppendLine("BEGIN ");
            sql.Append(Tab).Append(Tab);
            sql.Append("UPDATE [");
            sql.Append(element.TableName);
            sql.AppendLine("] SET ");

            isFirst = true;
            foreach (var field in fields.Where(f => !f.IsPk))
            {
                if (isFirst)
                    isFirst = false;
                else
                    sql.AppendLine(", ");

                sql.Append(Tab).Append(Tab).Append(Tab);
                sql.Append($"[{field.Name}]");
                sql.Append(" = @");
                sql.Append(field.Name);
            }

            sql.AppendLine("");

            isFirst = true;
            foreach (var f in fields.Where(f => f.IsPk))
            {
                if (isFirst)
                {
                    sql.Append(Tab).Append(Tab);
                    sql.Append("WHERE ");
                    isFirst = false;
                }
                else
                {
                    sql.Append(Tab).Append(Tab);
                    sql.Append("AND ");
                }

                sql.Append(f.Name);
                sql.Append(" = @");
                sql.AppendLine(f.Name);
            }

            sql.Append(Tab).Append(Tab);
            sql.AppendLine("SET @RET = 1; ");
            sql.Append(Tab);
            sql.AppendLine("END ");
        }
        else
        {
            sql.Append(Tab);
            sql.AppendLine($"ELSE IF @TYPEACTION = '{UpdateInitial}' ");
            sql.Append(Tab);
            sql.AppendLine("BEGIN ");
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("--NO UPDATABLED");
            sql.Append(Tab).Append(Tab);
            sql.AppendLine("SET @RET = 1; ");
            sql.Append(Tab);
            sql.AppendLine("END ");
        }

        //Delete Script
        sql.Append(Tab);
        sql.AppendLine($"ELSE IF @TYPEACTION = '{DeleteInitial}' ");
        sql.Append(Tab);
        sql.AppendLine("BEGIN ");
        sql.Append(Tab).Append(Tab);
        sql.Append("DELETE FROM [");
        sql.Append(element.TableName);
        sql.AppendLine("] ");

        isFirst = true;
        foreach (var field in fields.Where(f => f.IsPk && f.EnableOnDelete))
        {
            if (isFirst)
            {
                sql.Append(Tab).Append(Tab);
                sql.Append("WHERE ");
                isFirst = false;
            }
            else
            {
                sql.Append(Tab).Append(Tab);
                sql.Append("AND ");
            }

            sql.Append($"[{field.Name}]");
            sql.Append(" = @");
            sql.AppendLine(field.Name);
        }

        sql.Append(Tab).Append(Tab);
        sql.AppendLine("SET @RET = 2; ");
        sql.Append(Tab);
        sql.AppendLine("END ");
        sql.AppendLine(" ");

        return sql.ToString();
    }

    private static bool HasUpdateFields(Element element)
    {
        return element.Fields.Any(f => !f.IsPk && f.DataBehavior == FieldBehavior.Real);
    }

}