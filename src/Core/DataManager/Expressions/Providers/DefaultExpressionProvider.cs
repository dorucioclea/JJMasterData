#nullable enable
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataManager.Expressions.Abstractions;

namespace JJMasterData.Core.DataManager.Expressions.Providers;

public class DefaultExpressionProvider : IBooleanExpressionProvider, IAsyncExpressionProvider
{
    public string Prefix => "exp";
    public string Title => "Expression";
    private static object EvaluateObject(string replacedExpression)
    {
        using var dt = new DataTable();
        var result = dt.Compute(replacedExpression, string.Empty).ToString();

        return result!;
    }

    public bool Evaluate(string expression, IDictionary<string,object?> parsedValues)
    {
        var replacedExpression= ExpressionHelper.ReplaceExpression(expression, parsedValues);
        return StringManager.ParseBool(EvaluateObject(replacedExpression));
    }
    
    public Task<object?> EvaluateAsync(string expression, IDictionary<string,object?> parsedValues)
    {
        var replacedExpression= ExpressionHelper.ReplaceExpression(expression, parsedValues);
        return Task.FromResult<object?>(EvaluateObject(replacedExpression));
    }
}