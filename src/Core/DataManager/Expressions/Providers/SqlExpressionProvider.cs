using System.Threading.Tasks;
using JJMasterData.Commons.Data;
using JJMasterData.Commons.Data.Entity.Repository.Abstractions;
using JJMasterData.Core.DataManager.Expressions.Abstractions;
using JJMasterData.Core.DataManager.Models;

namespace JJMasterData.Core.DataManager.Expressions.Providers;

public class SqlExpressionProvider : IExpressionProvider
{
    private readonly IEntityRepository _entityRepository;
    private readonly IExpressionParser _expressionParser;

    public SqlExpressionProvider(IEntityRepository entityRepository, IExpressionParser expressionParser)
    {
        _entityRepository = entityRepository;
        _expressionParser = expressionParser;
    }

    public bool CanHandle(string expressionType) => expressionType == "sql";

    public async Task<object> EvaluateAsync(string expression, FormStateData formStateData)
    {
        var parsedSql = _expressionParser.ParseExpression(expression, formStateData, false);
        var obj = await _entityRepository.GetResultAsync(new DataAccessCommand(parsedSql!));
        return obj?.ToString() ?? string.Empty;
    }
}
