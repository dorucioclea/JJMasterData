using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JJMasterData.Brasil.Abstractions;
using JJMasterData.Brasil.Models;
using JJMasterData.Commons.Exceptions;
using JJMasterData.Commons.Security.Hashing;
using JJMasterData.Commons.Util;
using JJMasterData.Core.DataDictionary.Models.Actions;
using JJMasterData.Core.DataManager.Expressions;
using JJMasterData.Core.UI.Html;

namespace JJMasterData.Brasil.Actions;

public class CpfPluginActionHandler : BrasilPluginActionHandler
{
    private IReceitaFederalService ReceitaFederalService { get; }

    private const string BirthDateFieldKey = "BirthDath";
    
    public override Guid Id => GuidGenerator.FromValue(nameof(CpfPluginActionHandler));
    public override string Title => "Cpf";
    public override HtmlBuilder? AdditionalInformationHtml => null;
    protected override IEnumerable<string> CustomFieldMapKeys
    {
        get
        {
            yield return nameof(CpfResult.NomeDaPf);
            yield return nameof(CpfResult.ComprovanteEmitido);
            yield return nameof(CpfResult.SituacaoCadastral);
        }
    }

    protected override IEnumerable<PluginConfigurationField> CustomConfigurationFields
    {
        get
        {
            yield return new PluginConfigurationField
            {
                Name = BirthDateFieldKey,
                Label = "Data de Nascimento",
                Required = true,
                Type = PluginConfigurationFieldType.FormElementField
            };
        }
    }

    public CpfPluginActionHandler(IReceitaFederalService receitaFederalService, ExpressionsService expressionsService) : base(expressionsService)
    {
        ReceitaFederalService = receitaFederalService;
    }
    
    protected override async Task<Dictionary<string, object?>> GetResultAsync(PluginFieldActionContext context)
    {
        var values = context.Values;
        
        var cpf = StringManager.ClearCpfCnpjChars(values[context.FieldName!]!.ToString());
        var birthDate = values[context.ConfigurationMap[BirthDateFieldKey].ToString()];

        if (birthDate is not DateTime birthDateTime)
            throw new ArgumentNullException(nameof(birthDate));
        
        var cpfResult = await ReceitaFederalService.SearchCpfAsync(cpf, birthDateTime);

        return cpfResult.ToDictionary();
    }
    
}