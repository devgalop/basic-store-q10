using Microsoft.Extensions.Configuration;
using Npgsql;
using Store.Core.Constants;

namespace Store.Infrastructure.Persistence;

/// <summary>
/// Construye la cadena de conexión de PostgreSQL a partir de la configuración proporcionada.
/// </summary>
internal static class PostgresConnectionString
{
    private const string DEFAULT_HOST = "localhost";
    private const int DEFAULT_PORT = 5432;

    public static string From(IConfiguration configuration)
    {
        var user = configuration[DbEnvironmentVariables.POSTGRES_USER];
        var password = configuration[DbEnvironmentVariables.POSTGRES_PASSWORD];
        var database = configuration[DbEnvironmentVariables.POSTGRES_DB];

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(user)) missing.Add(DbEnvironmentVariables.POSTGRES_USER);
        if (string.IsNullOrWhiteSpace(password)) missing.Add(DbEnvironmentVariables.POSTGRES_PASSWORD);
        if (string.IsNullOrWhiteSpace(database)) missing.Add(DbEnvironmentVariables.POSTGRES_DB);
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Falta configuración obligatoria de PostgreSQL: {string.Join(", ", missing)}. " +
                "Se deben definir como variables de entorno o user-secrets en desarrollo.");

        var portValue = configuration[DbEnvironmentVariables.POSTGRES_PORT];
        var port = DEFAULT_PORT;
        if (!string.IsNullOrWhiteSpace(portValue) && !int.TryParse(portValue, out port))
            throw new InvalidOperationException(
                $"{DbEnvironmentVariables.POSTGRES_PORT} debe ser un número, pero vale '{portValue}'.");

        var host = configuration[DbEnvironmentVariables.POSTGRES_HOST];
        if (string.IsNullOrWhiteSpace(host))
            host = DEFAULT_HOST;

        return new NpgsqlConnectionStringBuilder
        {
            Host = host,
            Port = port,
            Username = user,
            Password = password,
            Database = database,
        }.ConnectionString;
    }
}
