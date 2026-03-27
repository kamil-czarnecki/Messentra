using System.Text.Json;
using Messentra.Domain;

namespace Messentra.Features.Jobs;

public abstract class TypedJob<TRequest, TResponse> : Job
{
    public required TRequest? Input
    {
        get => string.IsNullOrEmpty(InputRaw) ? default : JsonSerializer.Deserialize<TRequest>(InputRaw);
        init => InputRaw = JsonSerializer.Serialize(value);
    }
    public TResponse? Output
    {
        get => string.IsNullOrEmpty(OutputRaw) ? default : JsonSerializer.Deserialize<TResponse>(OutputRaw);
        protected set => OutputRaw = JsonSerializer.Serialize(value);
    }
}