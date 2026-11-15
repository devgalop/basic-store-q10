import { useCallback, useEffect, useRef, useState } from 'react';
import { listOrders } from '../api/ordersApi';
import type { Order } from '../api/types';

export interface OrdersState {
  orders: Order[];
  /** true solo en la primera carga: las siguientes actualizaciones no tapan la lista con un "cargando". */
  loading: boolean;
  /** Mensaje del último intento fallido; null si el último intento salió bien. */
  error: string | null;
  lastUpdated: Date | null;
  refresh: () => Promise<void>;
}

function isAbort(error: unknown): boolean {
  return error instanceof DOMException && error.name === 'AbortError';
}

/**
 * Mantiene la lista de pedidos al día con polling.
 * - Cada ciclo cancela el anterior si seguía en vuelo (nunca se pisan respuestas viejas con nuevas).
 * - Se pausa mientras la pestaña está oculta y refresca apenas vuelve a verse.
 * - Si una actualización falla, CONSERVA la última lista y expone el error; el siguiente ciclo reintenta solo.
 */
export function useOrders(intervalMs = 3000): OrdersState {
  const [orders, setOrders] = useState<Order[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastUpdated, setLastUpdated] = useState<Date | null>(null);
  const inFlight = useRef<AbortController | null>(null);

  const refresh = useCallback(async () => {
    inFlight.current?.abort();
    const controller = new AbortController();
    inFlight.current = controller;

    try {
      const data = await listOrders(controller.signal);
      setOrders(data);
      setError(null);
      setLastUpdated(new Date());
    } catch (e) {
      if (isAbort(e)) return;
      setError(e instanceof Error ? e.message : 'No se pudo actualizar la lista.');
    } finally {
      if (inFlight.current === controller) setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();

    const tick = () => {
      if (document.visibilityState === 'visible') void refresh();
    };
    const timer = setInterval(tick, intervalMs);
    document.addEventListener('visibilitychange', tick);

    return () => {
      clearInterval(timer);
      document.removeEventListener('visibilitychange', tick);
      inFlight.current?.abort();
    };
  }, [refresh, intervalMs]);

  return { orders, loading, error, lastUpdated, refresh };
}
