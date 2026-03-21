using System.Diagnostics.CodeAnalysis;

namespace Messentra.Infrastructure;

public interface IFileSystem
{
    void CreateDirectory(string path);
    bool DirectoryExists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    bool FileExists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path);
    void Delete(string path);
}

[ExcludeFromCodeCoverage]
public sealed class FileSystem : IFileSystem
{
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);
    public bool FileExists(string path) => File.Exists(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Stream OpenWrite(string path) => File.OpenWrite(path);

    public void Delete(string path) => File.Delete(path);
}