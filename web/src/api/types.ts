/** Contrato de la Orders API (ver Store.WebApi/Orders/OrderResponse.cs). El estado viaja como string. */
export type OrderStatus = 'Pending' | 'Confirmed' | 'Rejected';

export interface Order {
  id: string;
  customerName: string;
  sku: string;
  quantity: number;
  status: OrderStatus;
  createdAt: string;
  updatedAt: string | null;
}

export interface CreateOrderInput {
  customerName: string;
  sku: string;
  quantity: number;
}

export type FieldName = keyof CreateOrderInput;

/**
 * Errores de validación por campo. `general` agrupa los que no corresponden a ningún campo conocido,
 * para que ningún mensaje del servidor se pierda en silencio.
 */
export type FieldErrors = Partial<Record<FieldName | 'general', string[]>>;
