# Riesgos

Este documento registra los riesgos identificados en el diseño del sistema, cuáles se mitigaron en la implementación y cuáles quedan abiertos de forma consciente. Complementa a [ARCHITECTURE.md](ARCHITECTURE.md).

## Tabla de contenido

- [Cómo se calcula la criticidad](#cómo-se-calcula-la-criticidad)
- [Riesgos mitigados](#riesgos-mitigados)
- [Riesgos residuales (abiertos)](#riesgos-residuales-abiertos)

---

## Cómo se calcula la criticidad

Los riesgos se redactan como **"Consecuencia debido a Causa"**. La probabilidad e impacto se evalúan **sin la mitigación** (riesgo inherente): así se ve qué problema resuelve cada decisión.

El **impacto pesa el doble que la probabilidad**: un riesgo poco probable pero de alto impacto se prioriza por encima de uno muy probable pero de bajo impacto.

| Probabilidad \ Impacto | Bajo | Medio | Alto |
| ---------------------- | :--: | :---: | :--: |
| **Alta** | Media | Alta | **Crítica** |
| **Media** | Baja | Media | Alta |
| **Baja** | Baja | Media | Alta |

> Regla: con Baja = 1, Media = 2 y Alta = 3, el puntaje es `2 × Impacto + Probabilidad`. **9** → Crítica · **7–8** → Alta · **5–6** → Media · **3–4** → Baja.

## Riesgos mitigados

| # | Riesgo detectado | Probabilidad | Impacto | Criticidad | Cómo lo mitigamos |
| - | ---------------- | :----------: | :-----: | :--------: | ----------------- |
| R1 | Sobreventa de inventario (stock negativo o pedidos confirmados sin stock) debido a reservas concurrentes del mismo SKU con un esquema leer-y-luego-escribir. | Alta | Alto | **Crítica** | La verificación de disponibilidad y el descuento de stock ocurren en una única operación atómica en la base de datos, reforzada por una restricción que impide stock negativo. Cubierto con pruebas de concurrencia. |
| R2 | Stock descontado más de una vez por el mismo pedido debido a re-entregas del broker (*at-least-once*, caída entre COMMIT y ACK, varias instancias). | Alta | Alto | **Crítica** | Patrón Inbox: cada evento procesado se registra por su identificador en la misma transacción que el efecto de negocio. Si el evento ya fue procesado, se descarta sin repetir el efecto. Cubierto con pruebas de entregas duplicadas. |
| R3 | Pedido guardado sin evento publicado (o evento publicado sin pedido) debido a la escritura doble en BD y broker, que no puede ser atómica. | Media | Alto | **Alta** | Patrón Transactional Outbox: el cambio de estado y el evento asociado se guardan juntos en una sola transacción, y un proceso independiente se encarga de publicarlos. Aplica tanto en la API como en el Worker. |
| R4 | Eventos perdidos mientras RabbitMQ está caído debido a publicar directamente desde la petición o el consumidor. | Media | Alto | **Alta** | Los eventos permanecen almacenados hasta que el broker confirma su recepción. Ante fallos se reintenta con espera exponencial y se registra el error; ningún evento se descarta. |
| R5 | Evento no marcado como publicado (y reenviado) debido a una caída del proceso entre la confirmación del broker y el `UPDATE published_at`. | Media | Medio | **Media** | Un evento solo se marca como publicado después de que el broker confirma (garantía *at-least-once*). El posible duplicado lo absorbe el Inbox (R2). Mensajes y colas configurados como persistentes. |
| R6 | Mensajes duplicados o eventos en espera indefinida debido a varias instancias del publicador sin coordinación (o una instancia caída con eventos reclamados). | Media | Medio | **Media** | Cada instancia reclama lotes de eventos de forma exclusiva y sin bloquear a las demás, con un tiempo de reserva limitado. Si una instancia cae, la reserva expira y otra retoma esos eventos. |
| R7 | Pedidos rechazados por error (o confirmados por error) debido a que un fallo técnico (BD caída, timeout) se trata como una decisión de negocio. | Media | Alto | **Alta** | Se separan explícitamente los resultados de negocio (falta de stock → rechazo) de los fallos técnicos (se revierte todo y se reintenta). Un error técnico nunca se convierte en rechazo. Cubierto con pruebas. |
| R8 | Cola bloqueada o reprocesada en bucle debido a un mensaje veneno (tipo desconocido, versión no soportada o JSON inválido). | Media | Medio | **Media** | Los mensajes que no se pueden interpretar se envían directamente a una cola de mensajes fallidos (DLQ) sin reintentos. Los eventos están versionados para no interpretar a ciegas formatos desconocidos. |
| R9 | Mensajes perdidos o consumidor saturado debido a fallos técnicos transitorios sin estrategia de reintento. | Media | Alto | **Alta** | Los fallos transitorios se reintentan con una espera intermedia que no bloquea al consumidor. Tras un número máximo de intentos, el mensaje va a la DLQ con el detalle del error. Si eso falla, el mensaje vuelve a reintento en lugar de perderse. |
| R10 | Estado de pedido incoherente debido a resultados repetidos o contradictorios (`StockReserved` tras un `Rejected`). | Baja | Alto | **Alta** | El pedido se modela como una máquina de estados con transiciones válidas limitadas y estados finales inmutables. Los resultados repetidos se ignoran y los contradictorios se escalan a la DLQ en lugar de ocultarse. Cubierto con pruebas. |
| R11 | Consumidor inactivo sin que nadie lo note debido a un canal cerrado con la conexión TCP aún viva (la auto-recuperación del cliente no lo cubre). | Media | Alto | **Alta** | Un supervisor periódico verifica la salud del consumidor y lo recrea si detecta que dejó de recibir mensajes. Al arrancar, se reintenta la conexión hasta que el broker esté disponible. |
| R12 | Servicios que no arrancan o API que responde sin el Worker listo debido al orden de arranque de las dependencias (BD, broker, migraciones, seed). | Alta | Medio | **Alta** | Los servicios esperan a que sus dependencias estén sanas antes de iniciar, la inicialización de la base de datos se reintenta y el chequeo de disponibilidad solo responde OK cuando todo está listo. El sistema se levanta completo con un solo comando. |
| R13 | Pedidos inválidos persistidos (SKU inexistente, cantidad fuera de rango, cliente vacío) debido a entrada sin validar. | Alta | Medio | **Alta** | Validación de la entrada en la API con respuestas de error claras, y las mismas reglas protegidas dentro del dominio para que un pedido inválido no pueda construirse. |
| R14 | Filtración de credenciales debido a secretos escritos en el código o el repositorio. | Media | Alto | **Alta** | Las credenciales se leen de variables de entorno y el sistema no arranca si faltan. Los valores incluidos en el repositorio son solo para desarrollo local y se sobreescriben con un archivo no versionado. |
| R15 | Inyección SQL debido a SQL crudo con interpolación en el outbox y el inbox. | Baja | Alto | **Alta** | El SQL escrito a mano está centralizado en un único punto; los identificadores provienen del modelo de datos, nunca de la entrada del usuario, y los valores siempre viajan parametrizados. |
| R16 | Pérdida de pedidos, eventos o mensajes ante un reinicio debido a almacenamiento efímero. | Baja | Alto | **Alta** | Almacenamiento persistente para la base de datos y el broker, colas y mensajes durables, y reinicio automático de los servicios. |

## Riesgos existentes

Riesgos que **no** están mitigados por completo. Se dejan documentados para una futura entrega.

| # | Riesgo detectado | Probabilidad | Impacto | Criticidad | Mitigación actual / plan |
| - | ---------------- | :----------: | :-----: | :--------: | ------------------------ |
| RE1 | Pedidos `Pending` indefinidamente debido a un evento en la DLQ o un Worker caído por mucho tiempo (no hay estado `ProcessingFailed` ni *timeout*). | Media | Alto | **Alta** | *Parcial*: los mensajes fallidos quedan visibles en la consola del broker con el motivo del error; su reprocesamiento es manual. *Plan*: detectar pedidos pendientes por demasiado tiempo y alertar; evaluar un estado de fallo explícito. |
| RE2 | Acceso no autorizado a crear y consultar pedidos debido a que la API no tiene autenticación ni autorización. | Alta | Alto | **Crítica** | *Sin mitigar* (fuera del alcance de la prueba). *Plan*: autenticación estándar, autorización por rol y limitación de tasa de peticiones. |
| RE3 | Degradación de la base de datos con el tiempo debido al crecimiento sin límite de `outbox_messages` y `processed_events` (no se purgan). | Media | Medio | **Media** | *Parcial*: las consultas sobre eventos pendientes están optimizadas para no degradarse con el volumen. *Plan*: política de retención que purgue registros antiguos. |
| RE4 | Diagnóstico lento de incidentes debido a que solo hay logs (sin métricas ni tracing distribuido). | Media | Medio | **Media** | *Parcial*: logs estructurados con identificadores de correlación por pedido y estado del outbox consultable. *Plan*: incorporar métricas y trazas distribuidas (p. ej. OpenTelemetry). |
| RE5 | Indisponibilidad total del sistema debido a instancias únicas de PostgreSQL y RabbitMQ (punto único de fallo). | Baja | Alto | **Alta** | *Aceptado* para este entorno. Los componentes se recuperan solos cuando las dependencias vuelven. *Plan*: replicación de la base de datos y clúster del broker. |
| RE6 | Sobrecarga de una dependencia lenta debido a reintentos con delay fijo en el consumidor (no exponencial, a diferencia del publicador). | Baja | Medio | **Media** | *Aceptado*: los reintentos están acotados y su espera es configurable. |
| RE7 | Pérdida de pedidos de varios productos o reservas parciales debido a que el modelo soporta un único SKU por pedido y el `UPDATE` condicional no cubre varias filas. | Baja | Medio | **Media** | *Aceptado* por alcance. Si el negocio lo requiere: reservar todos los productos en una sola transacción con un orden de bloqueo consistente (todo o nada). |
