import type { Order } from '../api/types';
import { StatusBadge } from './StatusBadge';

interface Props {
  orders: Order[];
  loading: boolean;
  error: string | null;
  lastUpdated: Date | null;
}

const formatDate = (iso: string | null) => (iso ? new Date(iso).toLocaleString('es') : '—');

export function OrderList({ orders, loading, error, lastUpdated }: Props) {
  return (
    <section aria-labelledby="orders-title">
      <div className="section-header">
        <h2 id="orders-title">Pedidos</h2>
        <span className="muted" aria-live="polite">
          {lastUpdated ? `Actualizado a las ${lastUpdated.toLocaleTimeString('es')} · se refresca solo` : ''}
        </span>
      </div>

      {error && (
        // La lista de abajo es la última que se pudo leer: no se borra, se avisa que está desactualizada.
        <p className="banner banner-error" role="alert">
          {error} <span className="muted">Reintentando automáticamente…</span>
        </p>
      )}

      {loading ? (
        <p className="muted">Cargando pedidos…</p>
      ) : orders.length === 0 ? (
        <p className="muted">Todavía no hay pedidos. Creá el primero desde «Nuevo pedido».</p>
      ) : (
        <div className="table-wrapper">
          <table>
            <thead>
              <tr>
                <th>Pedido</th>
                <th>Cliente</th>
                <th>SKU</th>
                <th className="num">Cantidad</th>
                <th>Estado</th>
                <th>Creado</th>
                <th>Actualizado</th>
              </tr>
            </thead>
            <tbody>
              {orders.map((order) => (
                <tr key={order.id}>
                  <td title={order.id}>
                    <code>{order.id.slice(0, 8)}</code>
                  </td>
                  <td>{order.customerName}</td>
                  <td>{order.sku}</td>
                  <td className="num">{order.quantity}</td>
                  <td>
                    <StatusBadge status={order.status} />
                  </td>
                  <td>{formatDate(order.createdAt)}</td>
                  <td>{formatDate(order.updatedAt)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
