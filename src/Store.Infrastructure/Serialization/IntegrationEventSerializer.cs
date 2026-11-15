using System.Text.Json;
using System.Text.Json.Serialization;

namespace Store.Infrastructure.Serialization;

/// <summary>
/// Único punto de configuración de la serialización de eventos.
/// camelCase + enums como string: el contrato es legible y tolerante a agregar campos
/// (los consumidores ignoran propiedades desconocidas).
/// </summary>
public static class IntegrationEventSerializer
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize<T>(T @event) => JsonSerializer.Serialize(@event, Options);

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new JsonException($"El payload no se pudo deserializar como {typeof(T).Name}.");
}
