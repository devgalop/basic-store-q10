import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, ApiValidationError, createOrder, listOrders } from './ordersApi';

const json = (body: unknown, status = 200) =>
  new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });

const mockFetch = (impl: () => Promise<Response>) => {
  const fn = vi.fn<typeof fetch>(impl); // con la firma real de fetch, para poder inspeccionar los argumentos
  vi.stubGlobal('fetch', fn);
  return fn;
};

afterEach(() => vi.unstubAllGlobals());

describe('ordersApi', () => {
  const input = { customerName: 'Ana', sku: 'ABC-01', quantity: 2 };

  it('llama a /api/orders (mismo origen: nginx o el proxy de Vite reenvían a la API)', async () => {
    const fetchMock = mockFetch(async () => json([]));
    await listOrders();
    expect(fetchMock.mock.calls[0]![0]).toBe('/api/orders');
  });

  it('un 400 con errores por campo se convierte en ApiValidationError, sin perder ningún mensaje', async () => {
    mockFetch(async () =>
      json({ errors: { customerName: ['El nombre del cliente es obligatorio.'], SKU: ['El SKU no existe.'], '$.otra': ['Algo raro.'] } }, 400),
    );

    const error = await createOrder(input).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiValidationError);
    const { fieldErrors } = error as ApiValidationError;
    expect(fieldErrors.customerName).toEqual(['El nombre del cliente es obligatorio.']);
    expect(fieldErrors.sku).toEqual(['El SKU no existe.']); // tolera diferencias de mayúsculas en la clave
    expect(fieldErrors.general).toEqual(['Algo raro.']); // un campo desconocido no se descarta en silencio
  });

  it('un fallo de red se traduce a un mensaje entendible, no al TypeError del navegador', async () => {
    mockFetch(async () => Promise.reject(new TypeError('Failed to fetch')));
    await expect(createOrder(input)).rejects.toThrow(/No se pudo conectar con el servidor/);
  });

  it('un 500 se informa con su código y título, sin tratarlo como error de validación', async () => {
    mockFetch(async () => json({ title: 'An error occurred while processing your request.' }, 500));

    const error = await listOrders().catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).not.toBeInstanceOf(ApiValidationError);
    expect((error as ApiError).status).toBe(500);
  });

  it('una cancelación (AbortError) se propaga tal cual y no se confunde con un fallo de conexión', async () => {
    mockFetch(async () => Promise.reject(new DOMException('cancelado', 'AbortError')));
    await expect(listOrders()).rejects.toMatchObject({ name: 'AbortError' });
  });
});
