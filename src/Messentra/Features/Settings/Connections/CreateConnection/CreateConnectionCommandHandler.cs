using FluentValidation;
using Mediator;
using Messentra.Domain;
using Messentra.Infrastructure;
using Messentra.Infrastructure.Database;

namespace Messentra.Features.Settings.Connections.CreateConnection;

public sealed class CreateConnectionCommandHandler : ICommandHandler<CreateConnectionCommand>
{
    private readonly MessentraDbContext _dbContext;
    private readonly CreateConnectionCommandValidator _validator;

    public CreateConnectionCommandHandler(MessentraDbContext dbContext, CreateConnectionCommandValidator validator)
    {
        _dbContext = dbContext;
        _validator = validator;
    }

    public async ValueTask<Unit> Handle(CreateConnectionCommand command, CancellationToken cancellationToken)
    {
        await _validator.ValidateAndThrowAsync(command, cancellationToken);
        
        var connection = new Connection
        {
            Name = command.Name,
            ConnectionConfig = new ConnectionConfig(
                command.ConnectionConfig.ConnectionType,
                MapConnectionStringConfig(command),
                MapEntraIdConfig(command))
        };
        
        await _dbContext.Set<Connection>().AddAsync(connection, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        
        return Unit.Value;
    }

    private static ConnectionStringConfig? MapConnectionStringConfig(CreateConnectionCommand command) =>
        command.ConnectionConfig.ConnectionType == ConnectionType.ConnectionString
            ? new ConnectionStringConfig(command.ConnectionConfig.ConnectionString!)
            : null;
    
    private static EntraIdConfig? MapEntraIdConfig(CreateConnectionCommand command) =>
        command.ConnectionConfig.ConnectionType == ConnectionType.EntraId
            ? new EntraIdConfig(
                command.ConnectionConfig.Namespace!,
                command.ConnectionConfig.TenantId!,
                command.ConnectionConfig.ClientId!)
            : null;
}