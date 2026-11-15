namespace Store.Core.Constants;

/// <summary>
/// Contiene los nombres de las variables de entorno utilizadas para la configuración de la base de datos PostgreSQL.
/// </summary>
public static class DbEnvironmentVariables
{
    /// <summary>
    /// Nombre de la variable de entorno que contiene el host de PostgreSQL.
    /// </summary>
    public const string POSTGRES_HOST = "POSTGRES_HOST";
    /// <summary>
    /// Nombre de la variable de entorno que contiene el puerto de PostgreSQL.
    /// </summary>
    public const string POSTGRES_PORT = "POSTGRES_PORT";
    /// <summary>
    /// Nombre de la variable de entorno que contiene el usuario de PostgreSQL.
    /// </summary>
    public const string POSTGRES_USER = "POSTGRES_USER";
    /// <summary>
    /// Nombre de la variable de entorno que contiene la contraseña de PostgreSQL.
    /// </summary>
    public const string POSTGRES_PASSWORD = "POSTGRES_PASSWORD";
    /// <summary>
    /// Nombre de la variable de entorno que contiene el nombre de la base de datos de PostgreSQL.
    /// </summary>
    public const string POSTGRES_DB = "POSTGRES_DATABASE_NAME";
}
