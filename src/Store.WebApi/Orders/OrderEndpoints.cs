using Microsoft.AspNetCore.Http.HttpResults;
using Store.Core.Contracts.Orders;
using Store.Core.Models.Orders;
using Store.Core.UseCases.Orders;

namespace Store.WebApi.Orders;

public static class OrderEndpoints
{
    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/orders").WithTags("Orders");

        group.MapPost("/", CreateAsync);
        group.MapGet("/", ListAsync);
        group.MapGet("/{id:guid}", GetByIdAsync);

        return app;
    }

    /// <summary>
    /// Crea un nuevo pedido utilizando los datos proporcionados en la solicitud.
    /// </summary>
    /// <param name="request">La solicitud que contiene los datos del pedido a crear.</param>
    /// <param name="handler">El manejador que procesa la creación del pedido.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>El resultado de la operación, que puede ser un pedido creado o un problema de validación.</returns>
    private static async Task<Results<Created<OrderResponse>, ValidationProblem>> CreateAsync(
        CreateOrderRequest request, 
        CreateOrderHandler handler, 
        CancellationToken cancellationToken
    )
    {
        var result = await handler.HandleAsync(request, cancellationToken);

        if (!result.IsValid)
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>(result.Errors));

        var response = OrderResponse.From(result.Order!);
        return TypedResults.Created($"/orders/{response.Id}", response);
    }

    /// <summary>
    /// Lista todos los pedidos disponibles en la base de datos.
    /// </summary>
    /// <param name="store">El almacén de pedidos utilizado para recuperar los pedidos.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>Una lista de respuestas de pedidos.</returns>
    private static async Task<Ok<IReadOnlyList<OrderResponse>>> ListAsync(
        IOrderStore store, 
        CancellationToken cancellationToken
    )
    {
        var orders = await store.ListAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<OrderResponse>>(orders.Select(OrderResponse.From).ToList());
    }

    /// <summary>
    /// Obtiene un pedido por su identificador.
    /// </summary>
    /// <param name="id">El identificador del pedido a recuperar.</param>
    /// <param name="store">El almacén de pedidos utilizado para buscar el pedido.</param>
    /// <param name="cancellationToken">Token de cancelación para la operación asincrónica.</param>
    /// <returns>El pedido si se encuentra, de lo contrario NotFound.</returns>
    private static async Task<Results<Ok<OrderResponse>, NotFound>> GetByIdAsync(
        Guid id, 
        IOrderStore store, 
        CancellationToken cancellationToken
    )
    {
        var order = await store.FindAsync(id, cancellationToken);
        return order is null ? TypedResults.NotFound() : TypedResults.Ok(OrderResponse.From(order));
    }
}
