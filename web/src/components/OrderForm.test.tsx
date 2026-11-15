import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiError, ApiValidationError, createOrder } from '../api/ordersApi';
import type { Order } from '../api/types';
import { OrderForm } from './OrderForm';

vi.mock('../api/ordersApi', async (importOriginal) => ({
  ...(await importOriginal<typeof import('../api/ordersApi')>()), // se conservan las clases de error reales
  createOrder: vi.fn(),
}));

const created: Order = {
  id: '01a1dbc6-3b2a-76db-8524-4dd2e3c89b8c',
  customerName: 'Ana',
  sku: 'ABC-01',
  quantity: 2,
  status: 'Pending',
  createdAt: '2026-11-15T10:00:00Z',
  updatedAt: null,
};

async function fill(user: ReturnType<typeof userEvent.setup>, name: string, sku: string, quantity: string) {
  await user.type(screen.getByLabelText('Cliente'), name);
  await user.type(screen.getByLabelText('SKU'), sku);
  await user.clear(screen.getByLabelText('Cantidad'));
  if (quantity) await user.type(screen.getByLabelText('Cantidad'), quantity);
}

describe('OrderForm', () => {
  beforeEach(() => vi.mocked(createOrder).mockReset());

  it('valida en el cliente: muestra los errores en pantalla y NO llama a la API', async () => {
    const user = userEvent.setup();
    render(<OrderForm onCreated={vi.fn()} />);

    await fill(user, '', '', '0');
    await user.click(screen.getByRole('button', { name: 'Crear pedido' }));

    expect(screen.getByText('El nombre del cliente es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText('El SKU es obligatorio.')).toBeInTheDocument();
    expect(screen.getByText(/La cantidad debe ser un número entero entre 1 y 100/)).toBeInTheDocument();
    expect(screen.getByLabelText('Cliente')).toHaveAttribute('aria-invalid', 'true');
    expect(createOrder).not.toHaveBeenCalled();
  });

  it('muestra junto al campo el error que devuelve el servidor (p. ej. un SKU inexistente)', async () => {
    vi.mocked(createOrder).mockRejectedValue(new ApiValidationError({ sku: ["El SKU 'NOPE-99' no existe en el catálogo."] }));
    const user = userEvent.setup();
    render(<OrderForm onCreated={vi.fn()} />);

    await fill(user, 'Ana', 'NOPE-99', '1');
    await user.click(screen.getByRole('button', { name: 'Crear pedido' }));

    expect(await screen.findByText("El SKU 'NOPE-99' no existe en el catálogo.")).toBeInTheDocument();
    expect(screen.getByLabelText('SKU')).toHaveAttribute('aria-invalid', 'true');
    expect(screen.getByLabelText('Cliente')).toHaveAttribute('aria-invalid', 'false');
  });

  it('un fallo de conexión se muestra como alerta visible, no en la consola', async () => {
    vi.mocked(createOrder).mockRejectedValue(new ApiError('No se pudo conectar con el servidor.'));
    const user = userEvent.setup();
    render(<OrderForm onCreated={vi.fn()} />);

    await fill(user, 'Ana', 'ABC-01', '1');
    await user.click(screen.getByRole('button', { name: 'Crear pedido' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('No se pudo conectar con el servidor.');
  });

  it('si el pedido se crea, avisa al padre y deja el formulario limpio', async () => {
    vi.mocked(createOrder).mockResolvedValue(created);
    const onCreated = vi.fn();
    const user = userEvent.setup();
    render(<OrderForm onCreated={onCreated} />);

    await fill(user, '  Ana  ', ' ABC-01 ', '2');
    await user.click(screen.getByRole('button', { name: 'Crear pedido' }));

    expect(createOrder).toHaveBeenCalledWith({ customerName: 'Ana', sku: 'ABC-01', quantity: 2 }); // se envía sin espacios
    expect(onCreated).toHaveBeenCalledWith(created);
    expect(screen.getByLabelText('Cliente')).toHaveValue('');
  });
});
