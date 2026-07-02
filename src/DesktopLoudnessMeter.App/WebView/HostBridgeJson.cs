using System.Text.Json;

namespace DesktopLoudnessMeter.App.WebView;

public static class HostBridgeJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(HostBridgeResponse response)
    {
        return JsonSerializer.Serialize(response, Options);
    }

    public static HostBridgeRequest? DeserializeRequest(string json)
    {
        return JsonSerializer.Deserialize<HostBridgeRequest>(json, Options);
    }

    public static string ExtractRequestId(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("id", out var idElement)
                && idElement.ValueKind == JsonValueKind.String)
            {
                var id = idElement.GetString();
                if (!string.IsNullOrWhiteSpace(id))
                {
                    return id;
                }
            }
        }
        catch (JsonException)
        {
        }

        return "unknown";
    }
}
