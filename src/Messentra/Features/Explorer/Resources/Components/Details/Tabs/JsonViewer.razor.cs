using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Messentra.Features.Explorer.Resources.Components.Details.Tabs;

public partial class JsonViewer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        IndentSize = 4
    };
    
    [Parameter] public string Json { get; set; } = string.Empty;

    private string FormattedJson => FormatAndHighlight(Json);

    private static string FormatAndHighlight(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return string.Empty;

        try
        {
            // Pretty format JSON
            using var doc = JsonDocument.Parse(json);
            json = JsonSerializer.Serialize(doc, JsonOptions);
        }
        catch
        {
            // If invalid JSON, show raw
        }

        var lineNumber = 1;

        var highlighted = MyRegex().Replace(json, match =>
            {
                var value = WebUtility.HtmlEncode(match.Value);
                string cls;

                if (match.Value.StartsWith('\"'))
                    cls = match.Value.EndsWith(':') ? "json-key" : "json-string";
                else
                    cls = match.Value switch
                    {
                        "true" or "false" => "json-boolean",
                        "null" => "json-null",
                        _ => "json-number"
                    };

                return $"<span class='{cls}'>{value}</span>";
            });

        var lines = highlighted.Split('\n');

        return string.Join("",
            lines.Select(line =>
                $"<div class='json-line'><span class='json-ln'>{lineNumber++}</span><span class='json-code'>{line}</span></div>"));
    }

    [GeneratedRegex("""("(\\u[a-zA-Z0-9]{4}|\\[^u]|[^\\\"])*"(\s*:)?|\b(true|false|null)\b|\b-?\d+(\.\d+)?\b)""")]
    private static partial Regex MyRegex();
}