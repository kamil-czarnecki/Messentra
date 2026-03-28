using System.Diagnostics.CodeAnalysis;

namespace Messentra.Infrastructure;

public interface IFileSystem
{
    string GetRootPath();
    void CreateDirectory(string path);
    void DeleteDirectory(string path, bool recursive);
    bool DirectoryExists(string path);
    IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);
    bool FileExists(string path);
    Stream OpenRead(string path);
    Stream OpenWrite(string path, int bufferSize = 4098, bool useAsync = false);
    void Delete(string path);
}

[ExcludeFromCodeCoverage]
public sealed class FileSystem : IFileSystem
{
    public string GetRootPath()
    {
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Messentra");
    }

    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void DeleteDirectory(string path, bool recursive) => Directory.Delete(path, recursive);
    public bool DirectoryExists(string path) => Directory.Exists(path);
    public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) =>
        Directory.EnumerateFiles(path, searchPattern, searchOption);
    public bool FileExists(string path) => File.Exists(path);
    public Stream OpenRead(string path) => File.OpenRead(path);
    public Stream OpenWrite(string path, int bufferSize = 4098, bool useAsync = false) => new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync);

    public void Delete(string path) => File.Delete(path);
}