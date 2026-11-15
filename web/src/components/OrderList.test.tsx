import { render, screen, within } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import type { Order } from '../api/types';
import { OrderList } from './OrderList';

const order = (id: string, status: Order['status'], sku: string): Order => ({
  id,
  customerName: `Cliente ${sku}`,
  sku,
  quantity: 1,
  status,
  createdAt: '2026-11-15T10:00:00Z',
  updatedAt: status === 'Pending' ? null : '2026-11-15T10:00:02Z',
});

const orders = [
  order('aaaaaaaa-0000-0000-0000-000000000001', 'Pending', 'ABC-01'),
  order('bbbbbbbb-0000-0000-0000-000000000002', 'Confirmed', 'XYZ-02'),
  order('cccccccc-0000-0000-0000-000000000003', 'Rejected', 'OUT-04'),
];

describe('OrderList', () => {
  it('muestra el estado de cada pedido con texto, no solo con color', () => {
    render(<OrderList orders={orders} loading={false} error={null} lastUpdated={null} />);

    const rows = screen.getAllByRole('row').slice(1); // sin el encabezado
    expect(within(rows[0]!).getByText('Pendiente')).toBeInTheDocument();
    expect(within(rows[1]!).getByText('Confirmado')).toBeInTheDocument();
    expect(within(rows[2]!).getByText('Rechazado')).toBeInTheDocument();
  });

  it('con una lista vacía explica cómo empezar', () => {
    render(<OrderList orders={[]} loading={false} error={null} lastUpdated={null} />);
    expect(screen.getByText(/Todavía no hay pedidos/)).toBeInTheDocument();
  });

  it('en la primera carga muestra «cargando» en lugar de «no hay pedidos»', () => {
    render(<OrderList orders={[]} loading error={null} lastUpdated={null} />);
    expect(screen.getByText('Cargando pedidos…')).toBeInTheDocument();
    expect(screen.queryByText(/Todavía no hay pedidos/)).not.toBeInTheDocument();
  });

  it('si falla una actualización avisa, pero CONSERVA la última lista en pantalla', () => {
    render(<OrderList orders={orders} loading={false} error="No se pudo conectar con el servidor." lastUpdated={null} />);

    expect(screen.getByRole('alert')).toHaveTextContent('No se pudo conectar con el servidor.');
    expect(screen.getByText('Confirmado')).toBeInTheDocument(); // la lista sigue ahí
  });
});
