using Microsoft.AspNetCore.DataProtection;
using Topstar.Application.SocialSync;

namespace Topstar.Infrastructure.SocialSync;

internal sealed class SocialTokenCipher(IDataProtectionProvider provider) : ISocialTokenCipher
{
    private readonly IDataProtector _protector = provider.CreateProtector("Topstar.SocialOAuthTokens.v1");

    public string Protect(string plainText)
    {
        return string.IsNullOrWhiteSpace(plainText) ? string.Empty : _protector.Protect(plainText);
    }

    public string Unprotect(string cipherText)
    {
        return string.IsNullOrWhiteSpace(cipherText) ? string.Empty : _protector.Unprotect(cipherText);
    }
}
