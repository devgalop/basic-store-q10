# Contexto - Prueba Técnica Q10

Una tienda en línea recibe pedidos de productos. Cada pedido debe reservar inventario antes de confirmarse. Si hay
stock suficiente, el pedido se confirma; si no, se rechaza. El equipo de operaciones necesita un panel web para
registrar pedidos manualmente y ver en qué estado va cada uno. Así de simple es el negocio; la gracia está en cómo
lo resuelves técnicamente.

## Reglas de negocio

- Un pedido debe reservar inventario antes de confirmarse.
- Si hay stock suficiente, el pedido se confirma.
- Si no hay stock suficiente, el pedido se rechaza.
- El equipo de operaciones debe poder registrar pedidos manualmente y ver su estado en un panel web.

## Restricciones técnicas

- **Debe ser idempotente**: procesar el mismo evento varias veces no debe alterar el estado más allá de la primera vez.
- **Debe manejar correctamente la concurrencia**: múltiples eventos relacionados con el mismo pedido pueden llegar casi simultáneamente.
- **Debe ser resiliente a fallos**: los fallos técnicos no deben dejar el sistema en un estado inconsistente.
- **Debe ser capaz de integrarse con un sistema de mensajería** para recibir eventos de stock reservado y stock rechazado.
