using Microsoft.Extensions.Diagnostics.HealthChecks;
using Store.Core.Contracts.Catalog;

namespace Store.WebApi.Health;

/// <summary>
/// Comprueba si la API está lista para atender pedidos verificando la disponibilidad del catálogo de productos.
/// </summary>
/// <param name="catalog">El catálogo de productos utilizado para verificar la disponibilidad.</param>
internal sealed class CatalogReadinessCheck(IProductCatalog catalog) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Tabla inexistente = el Worker aún no migró (la consulta lanza). Tabla vacía = migró pero aún
            // no terminó el seed: en esa ventana un POST recibiría un falso "el SKU no existe".
            return await catalog.AnyAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("El catálogo de productos está vacío: el Worker aún no cargó el seed.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("No se puede leer el catálogo de productos.", ex);
        }
    }
}
