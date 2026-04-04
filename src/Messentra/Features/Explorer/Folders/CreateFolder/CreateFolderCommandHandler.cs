using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;

namespace Messentra.Features.Explorer.Folders.CreateFolder;

public sealed class CreateFolderCommandHandler : ICommandHandler<CreateFolderCommand, long>
{
    private readonly MessentraDbContext _dbContext;

    public CreateFolderCommandHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<long> Handle(CreateFolderCommand command, CancellationToken cancellationToken)
    {
        var folder = new Folder { ConnectionId = command.ConnectionId, Name = command.Name };
        await _dbContext.Set<Folder>().AddAsync(folder, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return folder.Id;
    }
}