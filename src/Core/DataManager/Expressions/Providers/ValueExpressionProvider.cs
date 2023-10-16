using System.Threading.Tasks;
using JJMasterData.Core.DataManager.Expressions.Abstractions;
using JJMasterData.Core.DataManager.Models;

namespace JJMasterData.Core.DataManager.Expressions.Providers;

public class ValueExpressionProvider : IExpressionProvider
{
    private readonly ExpressionParser _expressionParser;

    public ValueExpressionProvider(ExpressionParser expressionParser)
    {
        _expressionParser = expressionParser;
    }

    public string Prefix => "val";

    public async Task<object> EvaluateAsync(string expression, FormStateData formStateData)
    {
        if (expression.Contains("{"))
            return await Task.FromResult<object>(_expressionParser.ParseExpression(expression, formStateData, false)!);
        
        return await Task.FromResult<object>(expression.Replace("val:", "").Trim());
    }
}