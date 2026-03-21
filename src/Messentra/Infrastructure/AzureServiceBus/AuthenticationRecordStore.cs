using System.Security.Cryptography;
using System.Text;
using Azure.Identity;

namespace Messentra.Infrastructure.AzureServiceBus;

public interface IAuthenticationRecordStore
{
    AuthenticationRecord? Get(string key);
    void Save(string key, AuthenticationRecord record);
}

public sealed class AuthenticationRecordStore : IAuthenticationRecordStore
{
    public AuthenticationRecord? Get(string key)
    {
        var path = GetPath(key);
        
        if (!File.Exists(path))
            return null;

        try
        {
            using var stream = File.OpenRead(path);
            
            return AuthenticationRecord.Deserialize(stream);
        }
        catch
        {
            TryDelete(path);
            
            return null;
        }
    }

    public void Save(string key, AuthenticationRecord record)
    {
        var path = GetPath(key);
        
        try
        {
            using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
            
            record.Serialize(stream);
        }
        catch
        {
            TryDelete(path);
        }
    }
    
    private static string GetPath(string key)
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messentra",
            "AuthRecords");

        Directory.CreateDirectory(root);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        
        return Path.Combine(root, $"{hash}.bin");
    }
    
    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
}