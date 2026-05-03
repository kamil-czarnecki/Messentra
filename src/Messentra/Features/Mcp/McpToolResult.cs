using System.Text.Json;
using System.Text.Json.Serialization;

namespace Messentra.Features.Mcp;

[JsonConverter(typeof(McpToolResultConverterFactory))]
public sealed class McpToolResult<T>
{
    private readonly bool _isError;
    private readonly McpError? _error;
    private readonly T? _value;

    private McpToolResult(T value) => _value = value;
    private McpToolResult(McpError error) { _error = error; _isError = true; }

    public bool IsError => _isError;
    public McpError AsError => _isError ? _error! : throw new InvalidOperationException("Result is not an error.");
    public T AsSuccess => !_isError ? _value! : throw new InvalidOperationException("Result is an error.");

    internal object InnerValue => _isError ? _error! : _value!;

    public static implicit operator McpToolResult<T>(T value) => new(value);
    public static implicit operator McpToolResult<T>(McpError error) => new(error);
}

public sealed class McpToolResultConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(McpToolResult<>);

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var innerType = typeToConvert.GetGenericArguments()[0];
        
        return (JsonConverter)Activator.CreateInstance(
            typeof(McpToolResultConverter<>).MakeGenericType(innerType))!;
    }
}

internal sealed class McpToolResultConverter<T> : JsonConverter<McpToolResult<T>>
{
    public override McpToolResult<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, McpToolResult<T> value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.InnerValue, value.InnerValue.GetType(), options);
}
