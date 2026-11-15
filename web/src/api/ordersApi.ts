import type { CreateOrderInput, FieldErrors, FieldName, Order } from './types';

/** El servidor rechazó los datos (HTTP 400). Trae los mensajes agrupados por campo. */
export class ApiValidationError extends Error {
  readonly fieldErrors: FieldErrors;

  constructor(fieldErrors: FieldErrors) {
    super('Los datos del pedido no son válidos.');
    this.name = 'ApiValidationError';
    this.fieldErrors = fieldErrors;
  }
}

/** Falla que no es de validación: sin conexión, error del servidor, respuesta inesperada. */
export class ApiError extends Error {
  readonly status?: number;

  constructor(message: string, status?: number) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
  }
}

// nginx (Docker) y el servidor de Vite (desarrollo) reenvían /api/* a la API: el navegador solo habla con su propio origen.
const BASE_URL = '/api';

const KNOWN_FIELDS: readonly FieldName[] = ['customerName', 'sku', 'quantity'];

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}

async function send(path: string, init?: RequestInit): Promise<Response> {
  try {
    return await fetch(`${BASE_URL}${path}`, init);
  } catch (error) {
    if (isAbort(error)) throw error; // una cancelación (p. ej. al desmontar) no es un error para mostrar
    throw new ApiError('No se pudo conectar con el servidor. Verificá que el sistema esté levantado e intentá de nuevo.');
  }
}

/** ASP.NET devuelve los errores como { errors: { campo: [mensajes] } } (ValidationProblemDetails). */
function toFieldErrors(body: unknown): FieldErrors | null {
  const errors = (body as { errors?: unknown } | null)?.errors;
  if (!errors || typeof errors !== 'object') return null;

  const result: FieldErrors = {};
  for (const [key, messages] of Object.entries(errors as Record<string, unknown>)) {
    const list = Array.isArray(messages) ? messages.map(String) : [String(messages)];
    const field = KNOWN_FIELDS.find((f) => f.toLowerCase() === key.toLowerCase());
    const target = field ?? 'general';
    result[target] = [...(result[target] ?? []), ...list];
  }
  return result;
}

async function readJson(response: Response): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return null;
  }
}

async function failWith(response: Response): Promise<never> {
  const body = await readJson(response);
  if (response.status === 400) {
    const fieldErrors = toFieldErrors(body);
    if (fieldErrors && Object.keys(fieldErrors).length > 0) throw new ApiValidationError(fieldErrors);
    throw new ApiError('La solicitud no es válida.', 400);
  }
  const title = (body as { title?: unknown } | null)?.title;
  throw new ApiError(
    `El servidor no pudo procesar la solicitud (HTTP ${response.status}${typeof title === 'string' ? `: ${title}` : ''}).`,
    response.status,
  );
}

export async function listOrders(signal?: AbortSignal): Promise<Order[]> {
  const response = await send('/orders', { signal, headers: { Accept: 'application/json' } });
  if (!response.ok) await failWith(response);
  return (await response.json()) as Order[];
}

export async function createOrder(input: CreateOrderInput): Promise<Order> {
  const response = await send('/orders', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(input),
  });
  if (!response.ok) await failWith(response);
  return (await response.json()) as Order;
}
