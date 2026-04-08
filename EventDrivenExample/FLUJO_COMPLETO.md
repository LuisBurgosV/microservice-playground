# 📊 FLUJO COMPLETO: PRODUCER → RABBITMQ → REDIS → CONSUMERS

## 🎯 DIAGRAMA GENERAL DEL SISTEMA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                             │
│                    EVENT-DRIVEN ARCHITECTURE COMPLETO                      │
│                                                                             │
│  ┌─────────────┐                                                            │
│  │  PRODUCER   │                                                            │
│  │ (Terminal 3)│                                                            │
│  └──────┬──────┘                                                            │
│         │                                                                   │
│         │ 1. Publica evento                                               │
│         │    UserCreatedEvent {                                           │
│         │      EventId: "550e8400-..."                                    │
│         │      UserId: "user-001"                                         │
│         │      Email: "juan@example.com"                                  │
│         │    }                                                            │
│         │                                                                   │
│         ▼                                                                   │
│    ┌────────────────────────────────┐                                     │
│    │   RABBITMQ (Message Broker)    │                                     │
│    │   PORT: 5672 (AMQP)            │                                     │
│    │   PORT: 15672 (Management UI)  │                                     │
│    │                                │                                     │
│    │  Exchange: event-driven-ex...  │                                     │
│    │  Type: topic                   │                                     │
│    │  RoutingKey: events.usercreated│                                     │
│    │                                │                                     │
│    │  ┌──────────────────────────┐ │                                     │
│    │  │ QUEUES (Persistencia):   │ │                                     │
│    │  │                          │ │                                     │
│    │  │ 📨 email-notification-q │ │                                     │
│    │  │    ├─ Binding: events.user* │ │                                     │
│    │  │    ├─ Binding: events.order* │ │                                     │
│    │  │    └─ Messages: [evento1,   │ │                                     │
│    │  │           evento2, evento3] │ │                                     │
│    │  │                          │ │                                     │
│    │  │ 📝 logging-queue         │ │                                     │
│    │  │    ├─ Binding: events.*  │ │                                     │
│    │  │    └─ Messages: [evento1,   │ │                                     │
│    │  │           evento2, evento3] │ │                                     │
│    │  └──────────────────────────┘ │                                     │
│    └────────┬──────────────────┬───┘                                     │
│             │                  │                                         │
│             │ 2. Enrutamiento  │ 2. Enrutamiento                         │
│             │    por topic      │    por topic                            │
│             │                  │                                         │
│    ┌────────▼──────┐  ┌────────▼──────┐                                 │
│    │   CONSUMER 1  │  │   CONSUMER 2  │                                 │
│    │ EmailNotif    │  │   Logging     │                                 │
│    │ (Terminal 1)  │  │  (Terminal 2) │                                 │
│    └────────┬──────┘  └────────┬──────┘                                 │
│             │                  │                                         │
│             │ 3. Consume       │ 3. Consume                              │
│             │    evento        │    evento                               │
│             │                  │                                         │
│    ┌────────▼────────────────────────────┐                             │
│    │  IDEMPOTENCY CHECK (Redis)          │                             │
│    │  PORT: 6379                         │                             │
│    │                                     │                             │
│    │  Verificar:                         │                             │
│    │  GET event-processed:550e8400-...  │                             │
│    │                                     │                             │
│    │  ¿Ya procesado? NO ✓               │                             │
│    │  Continuar...                       │                             │
│    └────────┬────────────────────────────┘                             │
│             │                                                            │
│             │ 4. Ejecutar handler                                       │
│             │                                                            │
│    ┌────────▼──────┐  ┌────────────────┐                              │
│    │HANDLER 1      │  │ HANDLER 2      │                              │
│    │Enviar Email   │  │ Registrar en BD│                              │
│    │✉️              │  │📝              │                              │
│    └────────┬──────┘  └────────┬───────┘                              │
│             │                  │                                        │
│             │ 5. Marcar        │ 5. Marcar                             │
│             │ como procesado   │ como procesado                        │
│             │                  │                                        │
│    ┌────────▼──────────────────▼───────┐                              │
│    │  REDIS (Deduplicación)            │                              │
│    │                                   │                              │
│    │  SET event-processed:550e8400-... │                              │
│    │  Value: "processed"               │                              │
│    │  EXPIRE: 604800 segundos (7 días) │                              │
│    │                                   │                              │
│    │  ✅ Evento marcado                 │                              │
│    └───────────────────────────────────┘                              │
│             │                  │                                        │
│             │ 6. BasicAck      │ 6. BasicAck                           │
│             │ a RabbitMQ       │ a RabbitMQ                            │
│             │                  │                                        │
│    ┌────────▼────────────────────────────┐                            │
│    │  RABBITMQ                           │                            │
│    │  Confirmar entrega (eliminar queue) │                            │
│    │                                     │                            │
│    │  ✅ Evento eliminado de colas       │                            │
│    └─────────────────────────────────────┘                            │
│                                                                        │
│  RESULTADO:                                                           │
│  ✅ Evento procesado EXACTAMENTE UNA VEZ                             │
│  ✅ Email enviado                                                    │
│  ✅ Registro en BD                                                   │
│  ✅ Deduplicación garantizada                                       │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 📍 PASO A PASO DETALLADO

### **FASE 1: PRODUCCIÓN DEL EVENTO**

```
┌──────────────────────────────────────┐
│ Producer.Program.cs (Terminal 3)     │
│                                      │
│ var evt = new UserCreatedEvent {    │
│   UserId = "user-001",              │
│   Email = "juan@example.com",       │
│   FullName = "Juan Pérez"           │
│ };                                   │
│                                      │
│ await publisher.PublishAsync(evt);  │
└──────────────────┬───────────────────┘
                   │
                   ▼
        ┌──────────────────────┐
        │ IEventPublisher      │
        │ RabbitMQEventPublisher
        │                      │
        │ 1. Serializa evento  │
        │    a JSON            │
        │                      │
        │ 2. Crea BasicProperties
        │    - MessageId: evt.EventId
        │    - Persistent: true
        │    - ContentType: json
        │                      │
        │ 3. Calcula RoutingKey
        │    "events.usercreated"
        │                      │
        │ 4. BasicPublish()    │
        └──────────┬───────────┘
                   │
                   ▼
            ┌─────────────────┐
            │ RabbitMQ        │
            │ (Recibe evento) │
            └────────┬────────┘
```

### **FASE 2: ENRUTAMIENTO EN RABBITMQ**

```
┌─────────────────────────────────────────────────────────┐
│ RabbitMQ Exchange: event-driven-exchange (topic)        │
│                                                         │
│ Evento recibido:                                        │
│ RoutingKey: "events.usercreatedevent"                 │
│ MessageId: "550e8400-e29b-41d4-a716-446655440000"     │
│                                                         │
│ Buscar BINDINGS:                                        │
│ ┌─────────────────────────────────────────────────────┐│
│ │ Queue: email-notification-queue                    ││
│ │ Binding: events.usercreatedevent  ✓ MATCH          ││
│ │ Binding: events.ordercreatedevent ✓ MATCH          ││
│ │ Binding: events.*                 ✓ MATCH          ││
│ │                                                     ││
│ │ → Entregar a: email-notification-queue             ││
│ └─────────────────────────────────────────────────────┘│
│                                                         │
│ ┌─────────────────────────────────────────────────────┐│
│ │ Queue: logging-queue                               ││
│ │ Binding: events.*                 ✓ MATCH          ││
│ │                                                     ││
│ │ → Entregar a: logging-queue                        ││
│ └─────────────────────────────────────────────────────┘│
│                                                         │
└─────────────────────────────────────────────────────────┘
         │                              │
         ▼                              ▼
    Queue 1                         Queue 2
    (Persistido)                    (Persistido)
```

### **FASE 3: CONSUMO Y VERIFICACIÓN DE IDEMPOTENCIA**

```
CONSUMER.EMAILNOTIFICATION                CONSUMER.LOGGING
(Terminal 1)                              (Terminal 2)

┌─────────────────────────┐          ┌──────────────────────┐
│ RabbitMQConsumer        │          │ RabbitMQConsumer     │
│ BasicConsume(...)       │          │ BasicConsume(...)    │
│                         │          │                      │
│ Esperando mensajes...   │          │ Esperando mensajes...│
└──────────┬──────────────┘          └──────────┬───────────┘
           │                                    │
           │ Delivery Event                     │ Delivery Event
           │                                    │
           ▼                                    ▼
  ┌──────────────────────┐          ┌────────────────────────┐
  │ HandleMessageAsync() │          │ HandleMessageAsync()   │
  │                      │          │                        │
  │ 1. Obtener EventId   │          │ 1. Obtener EventId     │
  │    "550e8400-..."    │          │    "550e8400-..."      │
  │                      │          │                        │
  │ 2. Conectar a Redis  │          │ 2. Conectar a Redis    │
  └──────────┬───────────┘          └────────────┬───────────┘
             │                                   │
             ▼                                   ▼
    ┌────────────────────────────────────────────────────────┐
    │ RedisIdempotencyService.IsProcessedAsync()            │
    │                                                        │
    │ GET event-processed:550e8400-e29b-41d4-a716-...      │
    │                                                        │
    │ Primera vez → null (NO existe)                         │
    │ → Continuar procesamiento                              │
    │                                                        │
    │ Si hubiera reintentos:                                 │
    │ → "processed" (YA existe)                              │
    │ → IGNORAR, solo BasicAck                               │
    └────────────────────────────────────────────────────────┘
             │                                   │
             ▼ (NO existe)                       ▼ (NO existe)
    ┌──────────────────────┐          ┌────────────────────────┐
    │ PROCESAR EVENTO      │          │ PROCESAR EVENTO        │
    │                      │          │                        │
    │ RouteAndHandle()     │          │ RouteAndHandle()       │
    │                      │          │                        │
    │ if (usercreated)     │          │ if (usercreated)       │
    │  UserCreatedHandler  │          │  UserCreatedHandler    │
    │    .HandleAsync()    │          │    .HandleAsync()      │
    └──────────┬───────────┘          └────────────┬───────────┘
               │                                  │
               ▼                                  ▼
       ┌───────────────────┐          ┌───────────────────┐
       │ 📧 Enviar Email   │          │ 📝 Registrar BD   │
       │                   │          │                   │
       │ await Task.Delay()│          │ await Task.Delay()│
       │ (simular delay)   │          │ (simular delay)   │
       │                   │          │                   │
       │ ✅ Email enviado  │          │ ✅ Registrado     │
       └──────────┬────────┘          └────────────┬──────┘
```

### **FASE 4: MARCAR EN REDIS (DEDUPLICACIÓN)**

```
┌─────────────────────────────────────────────────────────────┐
│ Ambos consumers marcan en Redis (en paralelo)               │
│                                                             │
│ Consumer 1 y Consumer 2:                                    │
│                                                             │
│ RedisIdempotencyService.MarkAsProcessedAsync()             │
│                                                             │
│ SET event-processed:550e8400-e29b-41d4-a716-...           │
│     Value: "processed"                                     │
│     EXPIRE: 604800 (7 días)                                │
│                                                             │
│ Redis almacena:                                             │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ KEY: event-processed:550e8400-e29b-41d4-a716-446655 │  │
│ │ VALUE: "processed"                                   │  │
│ │ TTL: 604800 segundos (7 días)                        │  │
│ │ EXPIRA EN: 2026-04-13 20:14:00                       │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                             │
│ Próximos reintentos/duplicados:                            │
│ GET event-processed:550e8400-...                           │
│ → "processed" ✓ IGNORAR                                    │
│                                                             │
└─────────────────────────────────────────────────────────────┘
             │                          │
             ▼                          ▼
       BasicAck #1               BasicAck #2
    (Confirma entrega)        (Confirma entrega)
       a RabbitMQ              a RabbitMQ
             │                          │
             └──────────┬───────────────┘
                        ▼
              ┌──────────────────────┐
              │ RabbitMQ             │
              │ Elimina evento de    │
              │ email-notification-  │
              │ queue y logging-q    │
              │                      │
              │ ✅ Completado        │
              └──────────────────────┘
```

### **FASE 5: ESCENARIO DE REINTENTO (Si Consumer falla)**

```
ESCENARIO: Consumer 1 se crashea DURANTE el procesamiento

Tiempo 1: Consumer consume evento
┌────────────────────────────┐
│ Consumer.EmailNotification │
│ Recibe: UserCreatedEvent   │
│ EventId: 550e8400-...      │
│                            │
│ Inicia procesamiento...    │
│ ✉️ Enviando email...        │
│ 💥 CRASH (Ctrl+C)          │
│                            │
│ ❌ NO envió BasicAck()     │
└────────────────────────────┘
         │
         │ Timeout (30 segundos)
         │
         ▼
RabbitMQ detecta:
├─ No recibió BasicAck
├─ Consumer desconectado
└─ Evento vuelve a Queue

Tiempo 2: Consumer se reinicia
┌────────────────────────────┐
│ Consumer.EmailNotification │
│ Reiniciado                 │
│ Conecta a RabbitMQ...      │
│ Comienza a consumir...     │
└────────────────────────────┘
         │
         ▼
RabbitMQ reentrega evento
(El mismo evento, mismo EventId)
         │
         ▼
Check Idempotencia en Redis:
GET event-processed:550e8400-...
         │
         ├─ Si NO existe (primera vez genuina)
         │  └─ Procesar normalmente
         │
         └─ Si existe "processed" ✓ (duplicado)
            └─ IGNORAR procesamiento
            └─ BasicAck de todas formas
            └─ ✅ PREVENIR EMAIL DUPLICADO

GARANTÍA: Email NO se envía 2 veces ✓
```

---

## 🔄 TIMELINE COMPLETO (Temporal)

```
TIEMPO    PRODUCER              RABBITMQ              REDIS              CONSUMERS
────────────────────────────────────────────────────────────────────────────────────

0s        PublishAsync()        
          ▼                     

0.1s                            ExchangeDeclare()
                                QueueBind()
                                ▼

0.2s                            BasicPublish()
                                Enruta a queues
                                ▼

0.3s                                                                      BasicConsume()
                                                                          ▼

0.4s                                                                      HandleMessageAsync()
                                                                          ▼

0.5s                                                  IsProcessedAsync()
                                                      GET event-processed:xxx
                                                      → null (NO existe)
                                                      ▼

0.6s                                                                      RouteAndHandle()
                                                                          ▼

1.5s                                                                      Handler.HandleAsync()
                                                                          (delay de 1s)
                                                                          ▼

1.6s                                                  MarkAsProcessedAsync()
                                                      SET event-processed:xxx
                                                      EXPIRE 604800
                                                      ▼

1.7s                                                                      BasicAck()
                                                      ▼

1.8s                            BasicAck procesado
                                Evento eliminado de queue
                                ▼

TOTAL: ~1.8 segundos por evento
```

---

## 💾 ESTADO EN CADA HERRAMIENTA

### **Estado en RabbitMQ Management UI**

```
http://localhost:15672

EXCHANGES:
├─ event-driven-exchange
   ├─ Type: topic
   ├─ Durable: ✓
   ├─ Bindings:
   │  ├─ → email-notification-queue (events.usercreatedevent)
   │  ├─ → email-notification-queue (events.ordercreatedevent)
   │  └─ → logging-queue (events.*)

QUEUES:
├─ email-notification-queue
│  ├─ Messages: 0 (procesadas)
│  ├─ Consumers: 1
│  └─ Idle: 100%
│
└─ logging-queue
   ├─ Messages: 0 (procesadas)
   ├─ Consumers: 1
   └─ Idle: 100%
```

### **Estado en Redis CLI**

```
redis-cli

KEYS event-processed:*
→ 6 claves (6 eventos procesados)

GET event-processed:550e8400-e29b-41d4-a716-446655440000
→ "processed"

TTL event-processed:550e8400-e29b-41d4-a716-446655440000
→ 604799 (segundos restantes)

DBSIZE
→ 6 (o más si hay otros datos)
```

---

## ✨ RESUMEN DE GARANTÍAS

```
┌─────────────────────────────────────────────────────────┐
│ GARANTÍAS DEL SISTEMA                                   │
│                                                         │
│ 1️⃣  Persistencia en RabbitMQ                            │
│    ✓ Si consumer falla, eventos esperan                │
│    ✓ Si broker se reinicia, eventos persisten          │
│                                                         │
│ 2️⃣  Enrutamiento correcto                              │
│    ✓ Topic exchange distribuye a ambos consumers       │
│    ✓ Cada consumer tiene su propia copia               │
│                                                         │
│ 3️⃣  Idempotencia con Redis                             │
│    ✓ Cada evento procesado se marca                    │
│    ✓ Duplicados son detectados y ignorados             │
│    ✓ Expire automático cada 7 días                     │
│                                                         │
│ 4️⃣  Procesamiento asincrónico                          │
│    ✓ No bloquea otras operaciones                      │
│    ✓ Múltiples eventos en paralelo                     │
│                                                         │
│ 5️⃣  Error handling                                     │
│    ✓ Si handler falla → Nack → Reintentar             │
│    ✓ Si consumer cae → RabbitMQ lo detecta             │
│    ✓ Dead Letter Queue para fallos permanentes        │
│                                                         │
│ RESULTADO: Cada evento se procesa EXACTAMENTE UNA VEZ ✓
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

**Este es el flujo completo de tu arquitectura Event-Driven. 🎉**
