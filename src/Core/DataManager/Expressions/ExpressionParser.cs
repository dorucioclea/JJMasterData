using JJMasterData.Commons.Util;
using JJMasterData.Core.DataManager.Services;
using JJMasterData.Core.Web.Http.Abstractions;

namespace JJMasterData.Core.DataManager.Expressions;

public class ExpressionParser : IExpressionParser
{
    private IHttpContext HttpContext { get; }

    public ExpressionParser(IHttpContext httpContext)
    {
        HttpContext = httpContext;
    }
        public string? ParseExpression(
        string? expression,
        FormStateData formStateData,
        bool quotationMarks,
        ExpressionManagerInterval? interval = null)
    {
        if (expression is null)
            return null;
        
        var parsedExpression = expression
            .Replace("val:", "")
            .Replace("exp:", "")
            .Replace("sql:", "")
            .Replace("protheus:", "")
            .Trim();

        interval ??= new ExpressionManagerInterval('{', '}');

        var list = StringManager.FindValuesByInterval(expression, interval.Begin, interval.End);
        var userValues = formStateData.UserValues;
        var state = formStateData.PageState;
        var values = formStateData.FormValues;

        foreach (var field in list)
        {
            string? val;
            if (userValues != null && userValues.TryGetValue(field, out var value))
            {
                val = $"{value}";
            }
            else if ("pagestate".Equals(field.ToLower()))
            {
                val = $"{state}";
            }
            else if (values != null && values.TryGetValue(field, out var objVal))
            {
                val = objVal != null ? $"{objVal}" : "";
            }
            else if ("objname".Equals(field.ToLower()))
            {
                val = $"{HttpContext.Request["objname"]}";
            }
            else if (HttpContext.Session?[field] != null)
            {
                val = HttpContext.Session[field];
            }
            else
            {
                val = "";
            }

            if (val == null) continue;

            if (quotationMarks)
                val = "'" + val + "'";

            if (interval.Begin == '{' && interval.End == '}')
            {
                parsedExpression = parsedExpression.Replace($"{{{field}}}", val);
            }
            else
            {
                parsedExpression =
                    parsedExpression.Replace(string.Format($"{interval.Begin}{{0}}{interval.End}", field), val);
            }
        }

        return parsedExpression;
    }
}

public interface IExpressionParser
{
    string? ParseExpression(
        string? expression,
        FormStateData formStateData,
        bool quotationMarks,
        ExpressionManagerInterval? interval = null);
}