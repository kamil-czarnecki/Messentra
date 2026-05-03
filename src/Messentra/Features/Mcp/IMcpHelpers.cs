using Messentra.Domain;

namespace Messentra.Features.Mcp;

public interface IMcpHelpers
{
    Task<Connection?> ResolveConnection(string name, CancellationToken ct);
    Task<IReadOnlySet<string>?> ResolveFolderResourceUrls(long connectionId, string folderName, CancellationToken ct);
}
