using FluentValidation;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace Messentra.Features.Settings.Connections.UpdateConnection;

public sealed class UpdateConnectionCommandHandler : ICommandHandler<UpdateConnectionCommand>
{
    private readonly MessentraDbContext _dbContext;
    private readonly UpdateConnectionCommandValidator _validator;

    public UpdateConnectionCommandHandler(MessentraDbContext dbContext, UpdateConnectionCommandValidator validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async ValueTask<Unit> Handle(UpdateConnectionCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);

        var nameExists = await _dbContext.Set<Connection>()
            .AnyAsync(x => x.Name.ToLower() == command.Name.ToLower() && x.Id != command.Id, cancellationToken);

        if (nameExists)
            throw new ValidationException($"A connection with the name '{command.Name}' already exists.");

        var connection = await _dbContext
            .Set<Connection>()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);

        if (connection is null)
            return Unit.Value;
        
        connection.Name = command.Name;
        connection.ConnectionConfig = new ConnectionConfig(
            command.ConnectionConfig.ConnectionType,
            MapConnectionStringConfig(command),
            MapEntraIdConfig(command));
        
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }

    private static ConnectionStringConfig? MapConnectionStringConfig(UpdateConnectionCommand command) =>
        command.ConnectionConfig.ConnectionType == ConnectionType.ConnectionString
            ? new ConnectionStringConfig(command.ConnectionConfig.ConnectionString!)
            : null;
    
    private static EntraIdConfig? MapEntraIdConfig(UpdateConnectionCommand command) =>
        command.ConnectionConfig.ConnectionType == ConnectionType.EntraId
            ? new EntraIdConfig(
                command.ConnectionConfig.Namespace!,
                command.ConnectionConfig.TenantId!,
                command.ConnectionConfig.ClientId!)
            : null;
}