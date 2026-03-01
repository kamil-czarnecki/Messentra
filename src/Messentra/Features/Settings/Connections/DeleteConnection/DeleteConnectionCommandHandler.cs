using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Settings.Connections.DeleteConnection;

public sealed class DeleteConnectionCommandHandler : ICommandHandler<DeleteConnectionCommand>
{
    private readonly MessentraDbContext _dbContext;

    public DeleteConnectionCommandHandler(MessentraDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<Unit> Handle(DeleteConnectionCommand command, CancellationToken cancellationToken)
    {
        await _dbContext
            .Set<Connection>()
            .Where(x => x.Id == command.Id)
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }
}