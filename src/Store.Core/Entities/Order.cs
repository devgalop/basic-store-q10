using Store.Core.Enums.Orders;
using Store.Core.Exceptions;

namespace Store.Core.Entities;

/// <summary>
/// Representa un pedido de un único SKU. Encapsula su máquina de estados:
/// Pending -> Confirmed | Rejected. Confirmed y Rejected son finales.
/// </summary>
public sealed class Order
{
    /// <summary>
    /// Cantidad mínima permitida para un pedido.
    /// </summary>
    public const int MIN_QUANTITY = 1;
    /// <summary>
    /// Cantidad máxima permitida para un pedido.
    /// </summary>
    public const int MAX_QUANTITY = 100;

    /// <summary>
    /// Identificador único del pedido.
    /// </summary>
    public Guid Id { get; private set; }
    /// <summary>
    /// Nombre del cliente que realiza el pedido.
    /// </summary>
    public string CustomerName { get; private set; } = string.Empty;
    /// <summary>
    /// Identificador del producto (SKU) del pedido.
    /// </summary>
    public string Sku { get; private set; } = string.Empty;
    /// <summary>
    /// Cantidad solicitada en el pedido.
    /// </summary>
    public int Quantity { get; private set; }
    /// <summary>
    /// Estado actual del pedido.
    /// </summary>
    public EOrderStatus Status { get; private set; }
    /// <summary>
    /// Fecha y hora de creación del pedido.
    /// </summary>
    public DateTime CreatedAt { get; private set; }
    /// <summary>
    /// Fecha y hora de la última actualización del pedido.
    /// </summary>
    public DateTime? UpdatedAt { get; private set; }

    private Order() { }

    /// <summary>
    /// Crea un pedido. Su estado inicial por defecto es Pending.
    /// </summary>
    /// <param name="customerName">Nombre del cliente que realiza el pedido.</param>
    /// <param name="sku">Identificador del producto (SKU) del pedido.</param>
    /// <param name="quantity">Cantidad solicitada en el pedido.</param>
    /// <param name="utcNow">Fecha y hora actual en UTC.</param>
    /// <returns>Una nueva instancia de Order en estado Pending.</returns>
    /// <exception cref="ArgumentException">Se lanza si el nombre del cliente o el SKU son nulos, vacíos o contienen solo espacios en blanco.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Se lanza si la cantidad solicitada está fuera del rango permitido.</exception>
    public static Order Create(string customerName, string sku, int quantity, DateTime utcNow)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("El nombre del cliente es obligatorio.", nameof(customerName));
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("El SKU es obligatorio.", nameof(sku));
        if (quantity is < MIN_QUANTITY or > MAX_QUANTITY)
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity,
                $"La cantidad debe estar entre {MIN_QUANTITY} y {MAX_QUANTITY}.");

        return new Order
        {
            Id = Guid.CreateVersion7(utcNow),
            CustomerName = customerName.Trim(),
            Sku = sku.Trim(),
            Quantity = quantity,
            Status = EOrderStatus.Pending,
            CreatedAt = utcNow,
        };
    }

    /// <summary>
    /// Cambia el estado del pedido a Confirmed.
    /// </summary>
    /// <param name="utcNow">Fecha y hora actual en UTC.</param>
    public void Confirm(DateTime utcNow) => TransitionTo(EOrderStatus.Confirmed, utcNow);

    /// <summary>
    /// Cambia el estado del pedido a Rejected.
    /// </summary>
    /// <param name="utcNow">Fecha y hora actual en UTC.</param>
    public void Reject(DateTime utcNow) => TransitionTo(EOrderStatus.Rejected, utcNow);

    /// <summary>
    /// Realiza la transición del estado del pedido al estado especificado.
    /// </summary>
    /// <param name="next">El estado al que se desea cambiar el pedido.</param>
    /// <param name="utcNow">Fecha y hora actual en UTC.</param>
    /// <exception cref="InvalidOrderStatusTransitionException">Se lanza si la transición de estado no es válida.</exception>
    private void TransitionTo(EOrderStatus next, DateTime utcNow)
    {
        if (Status != EOrderStatus.Pending)
            throw new InvalidOrderStatusTransitionException(Id, Status, next);

        Status = next;
        UpdatedAt = utcNow;
    }
}
