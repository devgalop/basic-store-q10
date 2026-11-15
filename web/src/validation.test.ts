import { describe, expect, it } from 'vitest';
import { hasErrors, validateOrderForm } from './validation';

describe('validateOrderForm', () => {
  const valid = { customerName: 'Ana', sku: 'ABC-01', quantity: '2' };

  it.each(['1', '2', '100'])('acepta datos válidos (cantidad %s, incluidos los límites)', (quantity) => {
    expect(hasErrors(validateOrderForm({ ...valid, quantity }))).toBe(false);
  });

  it.each(['0', '101', '-3', '1.5', 'abc', '', '   '])('rechaza la cantidad %j', (quantity) => {
    const errors = validateOrderForm({ ...valid, quantity });
    expect(errors.quantity).toBeDefined();
    expect(errors.customerName).toBeUndefined();
    expect(errors.sku).toBeUndefined();
  });

  it('rechaza cliente y SKU vacíos o en blanco, y los reporta todos juntos', () => {
    const errors = validateOrderForm({ customerName: '   ', sku: '', quantity: '0' });
    expect(Object.keys(errors).sort()).toEqual(['customerName', 'quantity', 'sku']);
  });
});
