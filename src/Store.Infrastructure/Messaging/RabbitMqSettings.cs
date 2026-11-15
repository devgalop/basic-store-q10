using Microsoft.Extensions.Configuration;
using Store.Core.Constants;

namespace Store.Infrastructure.Messaging;

/// <summary>
/// Configuración de RabbitMQ. Contiene host, puerto, usuario, contraseña y delay de reintento.
/// </summary>
/// <param name="Host">Host de RabbitMQ.</param>
/// <param name="Port">Puerto de RabbitMQ.</param>
/// <param name="User">Usuario de RabbitMQ.</param>
/// <param name="Password">Contraseña de RabbitMQ.</param>
/// <param name="RetryDelay">Delay de reintento de RabbitMQ.</param>
public sealed record RabbitMqSettings(
    string Host, 
    int Port, 
    string User, 
    string Password, 
    TimeSpan RetryDelay
)
{
    private const string DEFAULT_HOST = "localhost";
    private const int DEFAULT_PORT = 5672;
    private const int DEFAULT_RETRY_DELAY_SECONDS = 10;

    /// <summary>
    /// Crea una instancia de RabbitMqSettings a partir de la configuración proporcionada.
    /// </summary>
    /// <param name="configuration">Configuración desde la cual se leerán las variables de entorno de RabbitMQ.</param>
    /// <returns>Instancia de RabbitMqSettings con los valores leídos de la configuración.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si falta alguna configuración obligatoria de RabbitMQ o si algún valor no es válido.</exception>
    public static RabbitMqSettings From(IConfiguration configuration)
    {
        var user = configuration[MessagingEnvironmentVariables.RABBIT_USER];
        var password = configuration[MessagingEnvironmentVariables.RABBIT_PASSWORD];

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(user)) missing.Add(MessagingEnvironmentVariables.RABBIT_USER);
        if (string.IsNullOrWhiteSpace(password)) missing.Add(MessagingEnvironmentVariables.RABBIT_PASSWORD);
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Falta configuración obligatoria de RabbitMQ: {string.Join(", ", missing)}. " +
                "Se deben definir como variables de entorno o user-secrets en desarrollo.");

        var host = configuration[MessagingEnvironmentVariables.RABBIT_HOST];

        return new RabbitMqSettings(
            Host: string.IsNullOrWhiteSpace(host) ? DEFAULT_HOST : host,
            Port: ReadPositiveInt(configuration, MessagingEnvironmentVariables.RABBIT_PORT, DEFAULT_PORT),
            User: user!,
            Password: password!,
            RetryDelay: TimeSpan.FromSeconds(ReadPositiveInt(configuration, MessagingEnvironmentVariables.RABBIT_RETRY_DELAY_SECONDS, DEFAULT_RETRY_DELAY_SECONDS)));
    }

    /// <summary>
    /// Lee un valor entero positivo desde la configuración, usando un valor de fallback si no está presente.
    /// </summary>
    /// <param name="configuration">Configuración desde la cual se leerá el valor.</param>
    /// <param name="key">Clave de la configuración a leer.</param>
    /// <param name="fallback">Valor de fallback si la configuración no está presente.</param>
    /// <returns>Valor entero positivo leído desde la configuración o el fallback.</returns>
    /// <exception cref="InvalidOperationException">Se lanza si el valor no es un entero positivo.</exception>
    private static int ReadPositiveInt(IConfiguration configuration, string key, int fallback)
    {
        var raw = configuration[key];
        if (string.IsNullOrWhiteSpace(raw)) return fallback;
        if (int.TryParse(raw, out var value) && value > 0) return value;
        throw new InvalidOperationException($"{key} debe ser un entero positivo, pero vale '{raw}'.");
    }
}
