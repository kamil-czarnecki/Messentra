using Microsoft.AspNetCore.DataProtection;

namespace Messentra.Infrastructure.Security;

public static class ConnectionStringProtection
{
    private static IDataProtector? _protector;

    public static void Initialize(IDataProtectionProvider provider)
    {
        if (_protector is not null)
            return;

        _protector = provider.CreateProtector("Messentra.ConnectionStrings");
    }

    public static string Protect(string plainText)
    {
        EnsureInitialized();
        return _protector!.Protect(plainText);
    }

    public static string Unprotect(string cipherText)
    {
        EnsureInitialized();
        return _protector!.Unprotect(cipherText);
    }

    private static void EnsureInitialized()
    {
        if (_protector is null)
            throw new InvalidOperationException(
                $"{nameof(ConnectionStringProtection)} has not been initialized. Call {nameof(Initialize)} at application startup.");
    }
}
