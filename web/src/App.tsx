import { useState } from 'react';
import type { Order } from './api/types';
import { OrderForm } from './components/OrderForm';
import { OrderList } from './components/OrderList';
import { useOrders } from './hooks/useOrders';

type View = 'create' | 'orders';

export default function App() {
  const [view, setView] = useState<View>('orders');
  const [notice, setNotice] = useState<string | null>(null);
  // La lista se mantiene al día aunque se esté mirando el formulario: al volver, ya está actualizada.
  const { orders, loading, error, lastUpdated, refresh } = useOrders();

  function handleCreated(order: Order) {
    setNotice(`Pedido ${order.id.slice(0, 8)} creado. Se está procesando: el estado cambia solo en unos segundos.`);
    setView('orders');
    void refresh(); // aparece de inmediato como «Pendiente», sin esperar al próximo ciclo
  }

  function show(next: View) {
    setView(next);
    if (next === 'create') setNotice(null);
  }

  return (
    <div className="app">
      <header>
        <h1>OrderFlow</h1>
        <nav aria-label="Vistas">
          <button type="button" aria-current={view === 'orders' ? 'page' : undefined} onClick={() => show('orders')}>
            Pedidos
          </button>
          <button type="button" aria-current={view === 'create' ? 'page' : undefined} onClick={() => show('create')}>
            Nuevo pedido
          </button>
        </nav>
      </header>

      <main>
        {view === 'create' ? (
          <OrderForm onCreated={handleCreated} />
        ) : (
          <>
            {notice && (
              <p className="banner banner-ok" role="status">
                {notice}
              </p>
            )}
            <OrderList orders={orders} loading={loading} error={error} lastUpdated={lastUpdated} />
          </>
        )}
      </main>
    </div>
  );
}
