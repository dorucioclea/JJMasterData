﻿using JJMasterData.Commons.Cryptography;
using JJMasterData.Commons.DI;
using JJMasterData.Core.DataManager;
using Microsoft.Extensions.DependencyInjection;

namespace JJMasterData.Core.Web.Components.Scripts
{
    internal class ScriptHelper
    {

        public static string GetUrl(string dictionaryName)
        {
            var encryptionService = JJService.Provider.GetService<JJMasterDataEncryptionService>();
            string dictionaryNameEncrypted = encryptionService.EncryptString(dictionaryName);
            return $"{ConfigurationHelper.GetUrlMasterData()}Form/GetGrid?dictionaryNameEncrypted={dictionaryNameEncrypted}";
        }

    }
}
