using System.Collections.Generic;
using System.Linq;
using JJMasterData.Core.DataManager.Expressions.Abstractions;

namespace JJMasterData.Core.DataManager.Expressions.Extensions;

public static class ExpressionProvideEnumerableExtensions
{
    public static string[] GetAsyncProvidersPrefixes(this IEnumerable<IExpressionProvider> expressionProviders)
    {
        return expressionProviders.Where(p => p is IAsyncExpressionProvider).Select(p => p.Prefix).ToArray();
    }
    
    public static string[] GetBooleanProvidersPrefixes(this IEnumerable<IExpressionProvider> expressionProviders)
    {
        return expressionProviders.Where(p => p is IBooleanExpressionProvider).Select(p => p.Prefix).ToArray();
    }
}