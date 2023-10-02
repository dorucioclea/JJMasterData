using System;
using JJMasterData.Commons.Localization;
using JJMasterData.Commons.Security.Cryptography.Abstractions;
using JJMasterData.Core.DataDictionary.Models;
using JJMasterData.Core.Http.Abstractions;
using Microsoft.Extensions.Localization;

namespace JJMasterData.Core.UI.Components;

internal class TextFileFactory : IControlFactory<JJTextFile>
{
    private IHttpRequest Request { get; }
    private IComponentFactory ComponentFactory { get; }
    
    private IEncryptionService EncryptionService { get; }
    private IStringLocalizer<JJMasterDataResources> StringLocalizer { get; }

    public TextFileFactory(
        IHttpRequest request,
        IComponentFactory componentFactory,
        IEncryptionService encryptionService,
        IStringLocalizer<JJMasterDataResources> stringLocalizer)
    {
        Request = request;
        ComponentFactory = componentFactory;
        EncryptionService = encryptionService;
        StringLocalizer = stringLocalizer;
    }
    
    
    public JJTextFile Create()
    {
        return new JJTextFile(Request,ComponentFactory, StringLocalizer, EncryptionService);
    }

    public JJTextFile Create(FormElement formElement, FormElementField field, ControlContext context)
    {
        var (formStateData, _, value) = context;

        if (field == null)
            throw new ArgumentNullException(nameof(field));

        if (field.DataFile == null)
            throw new ArgumentException("DataFile cannot be null");

        var textFile = Create();
        textFile.FormElementField = field;
        textFile.PageState = formStateData.PageState;
        textFile.Text = value != null ? value.ToString() : "";
        textFile.FortStateValues = formStateData.Values;
        textFile.Name = field.Name;
        textFile.FieldName = field.Name;
        textFile.Enabled = true;
        textFile.UserValues = formStateData.UserValues;
        textFile.FormElement = formElement;

        textFile.SetAttr(field.Attributes);

        return textFile;
    }
}