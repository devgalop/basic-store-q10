import type { FieldErrors } from './api/types';

// Mismos límites que el backend (Order.MinQuantity / MaxQuantity). El servidor sigue siendo la autoridad:
// esto solo evita viajes de ida y vuelta por errores evidentes.
export const MIN_QUANTITY = 1;
export const MAX_QUANTITY = 100;

export interface OrderFormValues {
  customerName: string;
  sku: string;
  quantity: string; // el input entrega texto; se convierte al enviar
}

export function validateOrderForm(values: OrderFormValues): FieldErrors {
  const errors: FieldErrors = {};

  if (!values.customerName.trim()) errors.customerName = ['El nombre del cliente es obligatorio.'];
  if (!values.sku.trim()) errors.sku = ['El SKU es obligatorio.'];

  const quantity = Number(values.quantity);
  if (values.quantity.trim() === '' || !Number.isInteger(quantity) || quantity < MIN_QUANTITY || quantity > MAX_QUANTITY) {
    errors.quantity = [`La cantidad debe ser un número entero entre ${MIN_QUANTITY} y ${MAX_QUANTITY}.`];
  }

  return errors;
}

export function hasErrors(errors: FieldErrors): boolean {
  return Object.values(errors).some((messages) => messages && messages.length > 0);
}
