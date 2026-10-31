using System.Text.Json;

namespace DietTracker.Core;

/// <summary>JSON settings matching the Postgres snake_case column names (PostgREST payloads).</summary>
public static class Json
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };
}
