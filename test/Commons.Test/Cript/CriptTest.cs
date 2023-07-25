﻿using JJMasterData.Commons.Cryptography;

namespace JJMasterData.Commons.Test.Cript;

public class CriptTest
{
    [Theory]
    [InlineData("JJMasterData")]
    public void EnigmaEncryptRPTest(string content)
    {
        var service = new ReportPortalEnigmaService();
        string encripted = service.EncryptString(content,"Example");

        Assert.Equal("AFADBFC6E7C7CAD5B6C6E8B4", encripted);
    }
            

    [Theory]
    [InlineData("AFADBFC6E7C7CAD5B6C6E8B4")]
    public void EnigmaDecryptRPTest(string content)
    {
        var service = new ReportPortalEnigmaService();
        string decrypted = service.DecryptString(content,"Example");

        Assert.Equal("JJMasterData", decrypted);
    }


    [Theory]
    [InlineData("r9/COvUnoHgv6wLnbtj2Lg==")]
    public void AesDecryptTest(string content)
    {
        var service = new AesEncryptionService();
        string descripted = service.DecryptString(content,"Example");
        Assert.Equal("JJMasterData", descripted);
    }

    [Theory]
    [InlineData("JJMasterData")]
    public void AesEncryptTest(string content)
    {
        var service = new AesEncryptionService();
        string encripted = service.EncryptString(content,"Example");
        Assert.Equal("r9/COvUnoHgv6wLnbtj2Lg==", encripted);
    }

}