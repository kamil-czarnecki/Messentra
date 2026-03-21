using System.Security.Cryptography;
using System.Text;
using Azure.Identity;

namespace Messentra.Infrastructure.AzureServiceBus;

public interface IAuthenticationRecordStore
{
    AuthenticationRecord? Get(string key);
    void Save(string key, AuthenticationRecord record);
    void ClearAll();
}

public sealed class AuthenticationRecordStore : IAuthenticationRecordStore
{
    private readonly IFileSystem _fileSystem;

    public AuthenticationRecordStore(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    public AuthenticationRecord? Get(string key)
    {
        var path = GetPath(key);
        
        if (!_fileSystem.FileExists(path))
            return null;

        try
        {
            using var stream = _fileSystem.OpenRead(path);
            
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
            using var stream = _fileSystem.OpenWrite(path);
            
            record.Serialize(stream);
        }
        catch
        {
            TryDelete(path);
        }
    }

    public void ClearAll()
    {
        var root = GetRootPath();

        if (!_fileSystem.DirectoryExists(root))
            return;
        
        foreach (var file in _fileSystem.EnumerateFiles(root, "*.bin", SearchOption.TopDirectoryOnly))
        {
            try
            {
                _fileSystem.Delete(file);
            }
            catch
            {
                // ignored
            }
        }
    }

    private string GetPath(string key)
    {
        var root = GetRootPath();

        _fileSystem.CreateDirectory(root);

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        
        return Path.Combine(root, $"{hash}.bin");
    }

    private static string GetRootPath() =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messentra",
            "AuthRecords");
    
    private void TryDelete(string path)
    {
        try
        {
            if (_fileSystem.FileExists(path))
                _fileSystem.Delete(path);
        }
        catch
        {
            // ignored
        }
    }
}