using System.Text.Json;
using Messentra.Domain;
using DatabaseJsonSerializerOptions = Messentra.Infrastructure.Database.JsonSerializerOptions;

namespace Messentra.Features.Jobs;

public abstract class TypedJob<TRequest, TResponse> : Job
{
    public required TRequest? Input
    {
        get => string.IsNullOrEmpty(InputRaw)
            ? default
            : JsonSerializer.Deserialize<TRequest>(InputRaw, DatabaseJsonSerializerOptions.Default);
        init => InputRaw = JsonSerializer.Serialize(value, DatabaseJsonSerializerOptions.Default);
    }
    public TResponse? Output
    {
        get => string.IsNullOrEmpty(OutputRaw)
            ? default
            : JsonSerializer.Deserialize<TResponse>(OutputRaw, DatabaseJsonSerializerOptions.Default);
        protected set => OutputRaw = JsonSerializer.Serialize(value, DatabaseJsonSerializerOptions.Default);
    }
}