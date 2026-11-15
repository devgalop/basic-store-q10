namespace Store.Core.Constants;

/// <summary>
/// Claves de variables de entorno para la configuración de RabbitMQ.
/// </summary>
public static class MessagingEnvironmentVariables
{
    /// <summary>
    /// Clave de la variable de entorno para el host de RabbitMQ.
    /// </summary>
    public const string RABBIT_HOST = "RABBITMQ_HOST";
    /// <summary>
    /// Clave de la variable de entorno para el puerto de RabbitMQ.
    /// </summary>
    public const string RABBIT_PORT = "RABBITMQ_PORT";
    /// <summary>
    /// Clave de la variable de entorno para el usuario de RabbitMQ.
    /// </summary>
    public const string RABBIT_USER = "RABBITMQ_USER";
    /// <summary>
    /// Clave de la variable de entorno para la contraseña de RabbitMQ.
    /// </summary>
    public const string RABBIT_PASSWORD = "RABBITMQ_PASSWORD";
    /// <summary>
    /// Clave de la variable de entorno para el delay de reintento de RabbitMQ en segundos.
    /// </summary>
    public const string RABBIT_RETRY_DELAY_SECONDS = "RABBITMQ_RETRY_DELAY_SECONDS";
}
