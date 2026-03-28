using System.Text.Json;
using Messentra.Domain;

namespace Messentra.Features.Jobs;

public abstract class TypedJob<TRequest, TResponse> : Job
{
    public required TRequest? Input
    {
        get => string.IsNullOrEmpty(InputRaw)
            ? default
            : JsonSerializer.Deserialize<TRequest>(InputRaw, Infrastructure.Database.JsonSerializerOptions.Default);
        init => InputRaw = JsonSerializer.Serialize(value, Infrastructure.Database.JsonSerializerOptions.Default);
    }
    public TResponse? Output
    {
        get => string.IsNullOrEmpty(OutputRaw)
            ? default
            : JsonSerializer.Deserialize<TResponse>(OutputRaw, Infrastructure.Database.JsonSerializerOptions.Default);
        protected set => OutputRaw = JsonSerializer.Serialize(value, Infrastructure.Database.JsonSerializerOptions.Default);
    }
}