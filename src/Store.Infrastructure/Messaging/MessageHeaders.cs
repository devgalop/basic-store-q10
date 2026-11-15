namespace Store.Infrastructure.Messaging;

/// <summary>
/// Contiene constantes que representan los nombres de los encabezados utilizados en los mensajes de RabbitMQ, especialmente para el manejo de errores y reintentos.
/// </summary>
internal static class MessageHeaders
{
    /// <summary>
    /// Nombre del encabezado que indica la versión del evento.
    /// </summary>
    public const string EventVersion = "event_version";
    /// <summary>
    /// Nombre del encabezado que indica que un mensaje ha sido enviado a la Dead Letter Queue (DLQ).
    /// </summary>
    public const string Death = "x-death";

    /// <summary>
    /// Nombre del encabezado que indica que ocurrió un error al procesar el mensaje.
    /// </summary>
    public const string Error = "x-error";
    /// <summary>
    /// Nombre del encabezado que indica la cantidad de intentos fallidos al procesar el mensaje.
    /// </summary>
    public const string FailedAttempts = "x-failed-attempts";
    /// <summary>
    /// Nombre del encabezado que indica la cola original de donde proviene el mensaje.
    /// </summary>
    public const string OriginalQueue = "x-original-queue";
    /// <summary>
    /// Nombre del encabezado que indica la fecha y hora del último intento fallido.
    /// </summary>
    public const string FailedAt = "x-failed-at";
}
