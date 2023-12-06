using JJMasterData.Commons.Configuration.Options;
using JJMasterData.Commons.Security.Cryptography.Abstractions;
using Microsoft.Extensions.Options;

namespace JJMasterData.Commons.Security.Cryptography;

/// <summary>
/// Wrapper to IEncryptionService with the secret key loaded by IOptions.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly string _secretKey;
    private readonly IEncryptionAlgorithm _encryptionAlgorithm;

    public EncryptionService(
        IEncryptionAlgorithm encryptionAlgorithm,
        IOptions<MasterDataCommonsOptions> options)
    {
        _encryptionAlgorithm = encryptionAlgorithm;
        _secretKey = options.Value.SecretKey;
    }

    public string EncryptString(string plainText) => _encryptionAlgorithm.EncryptString(plainText, _secretKey);

    public string DecryptString(string cipherText) =>
        _encryptionAlgorithm.DecryptString(cipherText,_secretKey);
}