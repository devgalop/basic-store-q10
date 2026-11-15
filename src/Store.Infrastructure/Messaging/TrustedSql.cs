using Microsoft.EntityFrameworkCore;

namespace Store.Infrastructure.Messaging;

/// <summary>
/// Único lugar donde se ejecuta SQL con identificadores interpolados.
///
/// Un nombre de tabla NO se puede enviar como parámetro SQL, así que debe ir en el texto. Es seguro
/// porque sale del modelo de EF (Table&lt;T&gt;), nunca de datos de entrada; los VALORES siempre
/// viajan como parámetros ({0}, {1}...). Concentrarlo acá deja una sola supresión del analizador EF1002,
/// justificada, en lugar de repartirla por el código.
/// </summary>
internal static class TrustedSql
{
    /// <summary>
    /// Crea el nombre de la tabla como EF
    /// </summary>
    /// <typeparam name="TEntity">Tipo de entidad</typeparam>
    /// <param name="db">Contexto de base de datos</param>
    /// <returns>Nombre de la tabla en formato EF</returns>
    public static string Table<TEntity>(DbContext db)
    {
        var entity = db.Model.FindEntityType(typeof(TEntity))!;
        return $"\"{entity.GetSchema()}\".\"{entity.GetTableName()}\"";
    }

    /// <summary>
    /// Ejecuta el comando SQL con los parámetros interpolados
    /// </summary>
    /// <param name="db">Contexto de base de datos</param>
    /// <param name="sql">Comando SQL</param>
    /// <param name="parameters">Interpolación de parámetros</param>
    /// <param name="cancellationToken">Token de cancelación</param>
    /// <returns>Una tarea que representa la operación asincrónica y contiene el número de filas afectadas.</returns>
    public static Task<int> ExecuteAsync(DbContext db, string sql, object[] parameters, CancellationToken cancellationToken)
    {
#pragma warning disable EF1002 // identificadores del modelo de EF, no input externo (ver resumen de la clase)
        return db.Database.ExecuteSqlRawAsync(sql, parameters, cancellationToken);
#pragma warning restore EF1002
    }
}
