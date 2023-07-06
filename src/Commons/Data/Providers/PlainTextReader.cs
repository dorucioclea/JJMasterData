﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using JJMasterData.Commons.Data.Entity;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Logging;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace JJMasterData.Commons.Data.Providers;

public class PlainTextReader
{
    private ILogger<PlainTextReader> Logger { get; }
    private readonly BaseProvider _provider;
    public bool ShowLogInfo { get; set; }
    public string Delimiter { get; set; }


    public PlainTextReader(BaseProvider provider, ILogger<PlainTextReader> logger)
    {
        Logger = logger;
        _provider = provider;
        Delimiter = "|";
    }


    public string GetListFieldsAsText(Element element, IDictionary filters, string orderby, int regporpag, int pag)
    {
        var sRet = new StringBuilder();
        var dStart = DateTime.Now;
        var culture = CultureInfo.CreateSpecificCulture("en-US");
        var dataAccess = _provider.DataAccess;
        string currentField = null;
        DbConnection conn = null;

        try
        {
            var pTot = new DataAccessParameter(_provider.VariablePrefix + "qtdtotal", 1, DbType.Int32, 0,
                ParameterDirection.InputOutput);
            var cmd = _provider.GetReadCommand(element, filters, orderby, regporpag, pag, ref pTot);
            var providerFactory = SqlClientFactory.Instance;
            conn = providerFactory.CreateConnection();
            if (conn == null)
                throw new JJMasterDataException("Error on create connection object");

            conn.ConnectionString = dataAccess.ConnectionString;
            conn.Open();

            using var dbCmd = providerFactory.CreateCommand();
            if (dbCmd == null)
                throw new JJMasterDataException("Error on create DbCommand");

            foreach (DataAccessParameter parm in cmd.Parameters)
            {
                DbParameter oPar = providerFactory.CreateParameter();
                if (oPar == null)
                    throw new JJMasterDataException("Error on create DbParameter");

                oPar.DbType = parm.Type;
                oPar.Value = parm.Value ?? DBNull.Value;
                oPar.ParameterName = parm.Name;
                dbCmd.Parameters.Add(oPar);
            }

            dbCmd.CommandType = cmd.CmdType;
            dbCmd.CommandText = cmd.Sql;
            dbCmd.Connection = conn;
            dbCmd.CommandTimeout = dataAccess.TimeOut;
            var dr = dbCmd.ExecuteReader();

            int col = 0;
            int qtd = 0;
            var columns = new Dictionary<string, int>();

            if (dr.HasRows)
            {
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    columns.Add(dr.GetName(i), i);
                }

                while (dr.Read())
                {
                    qtd++;
                    foreach (ElementField field in element.Fields)
                    {
                        currentField = field.Name;
                        if (!columns.ContainsKey(field.Name))
                            throw new JJMasterDataException(Translate.Key("{0} field not found", field.Name));

                        if (col > 0)
                            sRet.Append(Delimiter);

                        int ordinal = columns[field.Name];
                        if (!dr.IsDBNull(ordinal))
                        {
                            switch (field.DataType)
                            {
                                case FieldType.Date:
                                case FieldType.DateTime:
                                    sRet.Append(dr.GetDateTime(ordinal).ToString("yyyy-MM-dd HH:mm:ss"));
                                    break;
                                case FieldType.NVarchar:
                                case FieldType.Varchar:
                                case FieldType.Text:
                                    string val = dr.GetString(ordinal)
                                        .TrimEnd()
                                        .Replace("\r\n", "&#182;")
                                        .Replace("\r", "&#182;")
                                        .Replace("\n", "&#182;")
                                        .Replace(Delimiter, "&#124;");
                                    sRet.Append(val);
                                    break;
                                case FieldType.Int:
                                    sRet.Append(dr.GetInt32(ordinal));
                                    break;
                                case FieldType.Float:
                                    sRet.Append(double.Parse(dr.GetValue(ordinal).ToString()).ToString("G", culture));
                                    break;
                                default:
                                    sRet.Append(dr.GetValue(ordinal).ToString().TrimEnd());
                                    break;
                            }
                        }

                        col++;
                    }

                    sRet.AppendLine("");
                    col = 0;
                }
            }

            dr.Close();
            dr.Dispose();
            dbCmd.Dispose();
            
            if (ShowLogInfo)
            {
                var ts = DateTime.Now - dStart;
                var logMessage = new StringBuilder();
                logMessage.Append("Synchronizing");
                logMessage.Append(" ");
                logMessage.AppendLine(element.Name);

                if (filters != null)
                {
                    logMessage.Append("- ");
                    logMessage.Append("Filters");
                    logMessage.Append(": ");

                    foreach (DictionaryEntry val in filters)
                    {
                        logMessage.Append("  ");
                        logMessage.Append(val.Key);
                        logMessage.Append("=");
                        logMessage.Append(val.Value);
                    }

                    logMessage.AppendLine("");
                }

                logMessage.Append("- ");
                logMessage.Append("TotPerPage");
                logMessage.Append(": ");
                logMessage.AppendLine(regporpag.ToString());
                logMessage.Append("- ");
                logMessage.Append("CurrentPage");
                logMessage.Append(": ");
                logMessage.AppendLine(pag.ToString());

                logMessage.AppendLine($"{qtd.ToString()} records sync. Time {ts.TotalMilliseconds:N3}ms");
                Logger.LogInformation(logMessage.ToString());
            }
        }
        catch (Exception ex)
        {
            var message = new StringBuilder();

            message.AppendLine("Error synchronizing.");
            message.AppendLine($"Object: {element.Name}");
            message.AppendLine($"Page: {pag}");
            message.AppendLine($"Field: {currentField}");
            message.AppendLine($"Exception: {ex.Message}");
            message.AppendLine($"Stacktrace: {ex.StackTrace}");

            
            var exception = new JJMasterDataException(message.ToString(), ex);

            throw exception;


        }
        finally
        {
            if (conn != null)
            {
                conn.Close();
                conn.Dispose();
            }
        }

        return sRet.ToString();
    }

}

