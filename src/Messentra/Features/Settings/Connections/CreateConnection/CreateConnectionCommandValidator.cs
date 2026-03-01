using FluentValidation;
using Messentra.Domain;

namespace Messentra.Features.Settings.Connections.CreateConnection;

public sealed class CreateConnectionCommandValidator : AbstractValidator<CreateConnectionCommand>
{
    public CreateConnectionCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("{PropertyName} is required.");
        
        RuleFor(x => x.ConnectionConfig)
            .NotNull()
            .WithMessage("{PropertyName} is required.");

        RuleFor(x => x.ConnectionConfig.ConnectionType)
            .IsInEnum()
            .WithMessage("{PropertyName} must be a valid connection type.");
        
        When(x => x.ConnectionConfig.ConnectionType == ConnectionType.ConnectionString, () =>
        {
            RuleFor(x => x.ConnectionConfig.ConnectionString)
                .NotEmpty()
                .WithMessage("{PropertyName} is required when ConnectionType is ConnectionString.");
        });

        When(x => x.ConnectionConfig.ConnectionType == ConnectionType.EntraId, () =>
        {
            RuleFor(x => x.ConnectionConfig.Namespace)
                .NotEmpty()
                .WithMessage("{PropertyName} is required when ConnectionType is EntraId.");

            RuleFor(x => x.ConnectionConfig.TenantId)
                .NotEmpty()
                .WithMessage("{PropertyName} is required when ConnectionType is EntraId.");

            RuleFor(x => x.ConnectionConfig.ClientId)
                .NotEmpty()
                .WithMessage("{PropertyName} is required when ConnectionType is EntraId.");
        });
    }
}