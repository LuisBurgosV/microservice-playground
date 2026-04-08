# 📊 DIAGRAMA VISUAL INTERACTIVO

## 🎬 ESCENA 1: PRODUCER PUBLICA

```
PRODUCER (Terminal 3)
│
└─► PublishAsync(UserCreatedEvent {
    UserId: "user-001",
    Email: "juan@example.com",
    EventId: "550e8400-e29b-41d4-a716-446655440000"
  })
```

## 🎬 ESCENA 2: RABBITMQ ENRUTA

```
┌─────────────────────────────────────────┐
│         RABBITMQ BROKER                 │
│      (Message Distributor)              │
│                                         │
│  Topic: events.usercreatedevent        │
│                                         │
│  ↙️ COPIA 1                ↘️ COPIA 2  │
│                                         │
└─────────────────────────────────────────┘
      │                            │
      │ Enruta por binding         │ Enruta por binding
      │ events.usercreatedevent    │ events.*
      │                            │
      ▼                            ▼
  Queue 1                      Queue 2
  email-notification-q         logging-q
  (1 mensaje esperando)        (1 mensaje esperando)
```

## 🎬 ESCENA 3: CONSUMERS CONSUMEN

```
Consumer 1 (Terminal 1)              Consumer 2 (Terminal 2)
│                                    │
└─► Consume de queue 1               └─► Consume de queue 2
    │                                    │
    └─► BasicDeliver()                   └─► BasicDeliver()
        Evento: UserCreatedEvent             Evento: UserCreatedEvent
```

## 🎬 ESCENA 4: VERIFICAN REDIS

```
Consumer 1                           Consumer 2
│                                    │
└─► Redis.GET(                       └─► Redis.GET(
    "event-processed:550e8400-...")     "event-processed:550e8400-...")
    │                                    │
    ├─► null (NO existe) ✓              ├─► null (NO existe) ✓
    │   → Continuar                     │   → Continuar
    │                                    │
    └─► Procesar                        └─► Procesar
```

## 🎬 ESCENA 5: EJECUTAN HANDLERS

```
Consumer 1                           Consumer 2
│                                    │
└─► UserCreatedEventHandler          └─► UserCreatedEventHandler
    .HandleAsync()                       .HandleAsync()
    │                                    │
    └─► 📧 Enviar email                 └─► 📝 Registrar en BD
        (await Task.Delay(1000))            (await Task.Delay(500))
        │                                    │
        ✅ Email enviado                    ✅ Registrado
```

## 🎬 ESCENA 6: MARCAN EN REDIS

```
REDIS (Port: 6379)

Consumer 1 ejecuta:
Redis.SET(
  "event-processed:550e8400-e29b-41d4-a716-446655440000",
  "processed",
  EXPIRE: 604800 segundos
)

Consumer 2 ejecuta:
Redis.SET(
  "event-processed:550e8400-e29b-41d4-a716-446655440000",
  "processed",
  EXPIRE: 604800 segundos
)

Redis almacena:
┌────────────────────────────────────────┐
│ KEY: event-processed:550e8400-...     │
│ VALUE: "processed"                     │
│ EXPIRE: 604800 seg (7 días)            │
│ TTL: 604799 seg restantes              │
└────────────────────────────────────────┘
```

## 🎬 ESCENA 7: CONFIRMAN A RABBITMQ

```
Consumer 1                           Consumer 2
│                                    │
└─► BasicAck(deliveryTag)            └─► BasicAck(deliveryTag)
    │                                    │
    └─► RabbitMQ                        └─► RabbitMQ
        "Entrega confirmada"               "Entrega confirmada"
        → Elimina de queue                 → Elimina de queue
```

## 🎬 ESCENA 8: ESTADO FINAL

```
✅ FLUJO COMPLETADO EXITOSAMENTE

RabbitMQ:
├─ email-notification-queue: 0 mensajes
└─ logging-queue: 0 mensajes

Redis:
├─ event-processed:550e8400-...: "processed"
└─ TTL: 604799 segundos

Resultado:
├─ ✉️  Email enviado
├─ 📝 Evento registrado en BD
└─ 🔄 Deduplicación garantizada
```

---

## 🔄 FLUJO SI HUBIERA REINTENTO/DUPLICADO

```
ESCENARIO: Consumer 1 se crashea antes de BasicAck

Tiempo T1: Consumer consume y procesa
├─ Consume evento
├─ Check Redis: null → Procesar
├─ Ejecuta handler: ✅ Email enviado
├─ Marca Redis: SET ✓
└─ 💥 CRASH antes de BasicAck()

Tiempo T2: RabbitMQ detecta sin ACK
├─ Timeout esperando BasicAck
├─ Detecta consumer offline
└─ Reentrega evento a queue

Tiempo T3: Consumer se reinicia
├─ Conecta a RabbitMQ
├─ Comienza a consumir
└─ Recibe el MISMO evento

Tiempo T4: Verifica idempotencia
├─ Consume evento (mismo EventId)
├─ Check Redis: GET "event-processed:550e8400-..."
├─ Resultado: "processed" ✓ YA EXISTE
├─ Decision: IGNORAR procesamiento
└─ BasicAck de todas formas

RESULTADO:
✅ Email NO se envía 2 veces
✅ Garantía de "Exactly Once" mantenida
```

---

## 📊 TABLA DE ESTADOS

| Momento | Producer | RabbitMQ | Consumer1 | Consumer2 | Redis |
|---------|----------|----------|-----------|-----------|-------|
| T0 | Publica | - | Esperando | Esperando | Empty |
| T1 | ✅ Done | Recibe | Consume | Consume | Empty |
| T2 | - | Enruta Q1,Q2 | Check | Check | Empty |
| T3 | - | 2 msgs | Handler | Handler | Empty |
| T4 | - | 1 msg | Marca | Marca | 1 key |
| T5 | - | 0 msgs | Ack | Ack | 1 key |
| T6 | - | 0 msgs | ✅ Done | ✅ Done | 1 key |

---

## 🎯 COMPONENTES POR PUERTO

```
Tu Máquina
│
├─ localhost:5672
│  └─ RabbitMQ AMQP (Protocolo de mensajería)
│     └─ Publicadores y Consumers se conectan aquí
│
├─ localhost:15672
│  └─ RabbitMQ Management UI (Visual)
│     └─ Acceso: http://localhost:15672
│        Usuario: guest / Pass: guest
│
├─ localhost:6379
│  └─ Redis (Cache de deduplicación)
│     └─ Acceso: redis-cli
│
├─ Terminal 1
│  └─ Consumer.EmailNotification.exe
│     └─ Consume de: email-notification-queue
│
├─ Terminal 2
│  └─ Consumer.Logging.exe
│     └─ Consume de: logging-queue
│
└─ Terminal 3
   └─ Producer.exe
      └─ Publica a: event-driven-exchange
```

---

## 🚀 VELOCIDAD DE PROCESAMIENTO

```
Sin reintentos (flujo limpio):

T0.00s - Producer.PublishAsync() inicia
T0.02s - BasicPublish() enviado a RabbitMQ
T0.05s - RabbitMQ enruta a 2 consumers
T0.10s - Consumers reciben BasicDelivery
T0.12s - Check Redis (50ms)
T0.15s - Handler comienza (UserCreatedHandler)
T1.15s - Handler termina (1000ms delay)
T1.16s - Marca en Redis (50ms)
T1.17s - BasicAck enviado
T1.20s - RabbitMQ confirma

TOTAL: ~1.2 segundos por evento
```

---

## 🎓 PUNTOS CLAVE

```
1. PRODUCTOR (PublishAsync)
   └─ Envía evento a Exchange

2. EXCHANGE (Topic)
   └─ Distribuye a Queues según RoutingKey

3. QUEUES (Persistencia)
   └─ Almacenan mensajes durablemente

4. CONSUMERS (Async)
   └─ Consumen de sus respectivas queues

5. REDIS (Deduplicación)
   └─ Evita procesamiento duplicado

6. HANDLERS (Lógica de negocio)
   └─ Ejecutan la acción (email, logging, etc)

7. ACK (Confirmación)
   └─ Confirma a RabbitMQ que fue procesado

GARANTÍA: Exactamente-Una-Vez (Exactly Once Semantics)
```

---

**Ahora tienes una comprensión visual completa del flujo.** 🎉
