using System.Collections.Generic;
using JJMasterData.Commons.Localization;
using JJMasterData.Core.DataDictionary;
using JJMasterData.Core.Web.Components;
using JJMasterData.Core.Web.Http.Abstractions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.Web.Factories;

public class TextRangeFactory
{
    private IHttpContext HttpContext { get; }
    private TextGroupFactory TextGroupFactory { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }

    public TextRangeFactory(IHttpContext httpContext, TextGroupFactory textGroupFactory, IStringLocalizer<JJMasterDataResources> stringLocalizer)
    {
        HttpContext = httpContext;
        TextGroupFactory = textGroupFactory;
        StringLocalizer = stringLocalizer;
    }
    internal JJTextRange CreateTextRange(FormElementField field, IDictionary<string,dynamic> values)
    {
        string valueFrom = "";
        if (values != null && values.ContainsKey(field.Name + "_from"))
        {
            valueFrom = values[field.Name + "_from"].ToString();
        }
        
        var range = new JJTextRange(HttpContext,TextGroupFactory,StringLocalizer);
        range.FieldType = field.DataType;
        range.FromField = TextGroupFactory.CreateTextGroup(field);
        range.FromField.Text = valueFrom;
        range.FromField.Name = field.Name + "_from";
        range.FromField.PlaceHolder = StringLocalizer["From"];
        
        string valueTo = "";
        if (values != null && values.ContainsKey(field.Name + "_to"))
        {
            valueTo = values[field.Name + "_to"].ToString();
        }
        
        range.ToField = TextGroupFactory.CreateTextGroup(field);
        range.ToField.Text = valueTo;
        range.ToField.Name = field.Name + "_to";
        range.ToField.PlaceHolder = StringLocalizer["To"];

        return range;
    }
}