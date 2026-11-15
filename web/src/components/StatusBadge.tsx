import type { OrderStatus } from '../api/types';

const LABELS: Record<OrderStatus, string> = {
  Pending: 'Pendiente',
  Confirmed: 'Confirmado',
  Rejected: 'Rechazado',
};

export function StatusBadge({ status }: { status: OrderStatus }) {
  // El color nunca es la única señal: siempre hay texto.
  return (
    <span className={`badge badge-${status.toLowerCase()}`} data-status={status}>
      {LABELS[status] ?? status}
    </span>
  );
}
