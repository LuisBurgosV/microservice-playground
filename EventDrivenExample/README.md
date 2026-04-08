# Arquitectura Event-Driven con RabbitMQ, Redis e Idempotencia

Ejemplo educativo de arquitectura Event-Driven que demuestra los patrones de **Producer**, **Event Broker** y **Consumers** con implementación de idempotencia.

## 🏗️ Arquitectura

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│  Producer                                                   │
│  ├─ Publica UserCreatedEvent                               │
│  └─ Publica OrderCreatedEvent                              │
│           │                                                 │
│           ▼                                                 │
│  ┌─────────────────────────────────────┐                  │
│  │   RabbitMQ (Event Broker)           │                  │
│  │   - topic exchange                   │                  │
│  │   - events.usercreatedevent          │                  │
│  │   - events.ordercreatedevent         │                  │
│  └─────────────────────────────────────┘                  │
│           │              │                                 │
│           ▼              ▼                                 │
│  Consumer.EmailNotification    Consumer.Logging            │
│  (Envía emails)               (Registra en BD)             │
│           │                          │                     │
│           └──────────────┬───────────┘                     │
│                         ▼                                   │
│           Redis (Idempotency Store)                        │
│           - event-processed:{eventId}                      │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

## 📦 Proyectos

### Event.Shared
Contiene los eventos de dominio compartidos:
- `DomainEvent` - Clase base para todos los eventos
- `UserCreatedEvent` - Evento cuando se crea un usuario
- `OrderCreatedEvent` - Evento cuando se crea un pedido

### Producer
Publica eventos a RabbitMQ:
- `IEventPublisher` - Interfaz para publicar eventos
- `RabbitMQEventPublisher` - Implementación que publica a RabbitMQ
- Simula la creación de usuarios y pedidos

### Consumer.Shared
Componentes reutilizables para consumers:
- `IEventHandler<T>` - Interfaz para manejar eventos
- `IIdempotencyService` - Servicio de idempotencia basado en Redis
- `RedisIdempotencyService` - Implementación con Redis

### Consumer.EmailNotification
Consumer que simula el envío de emails:
- Maneja `UserCreatedEvent` (bienvenida)
- Maneja `OrderCreatedEvent` (confirmación de pedido)
- Implementa idempotencia para evitar emails duplicados
- Queue: `email-notification-queue`
- Binding: `events.usercreatedevent`, `events.ordercreatedevent`

### Consumer.Logging
Consumer que registra eventos:
- Maneja `UserCreatedEvent` y `OrderCreatedEvent`
- Implementa idempotencia para evitar registros duplicados
- Queue: `logging-queue`
- Binding: `events.*` (todos los eventos)

## 🚀 Cómo Ejecutar

### 1. Iniciar servicios de infraestructura con Docker

```bash
docker-compose up -d
```

Este comando inicia:
- **RabbitMQ** en `localhost:5672` (usuario: guest, contraseña: guest)
- **Redis** en `localhost:6379`
- **RabbitMQ Management UI** en `http://localhost:15672`

### 2. Compilar la solución

```bash
dotnet build
```

### 3. Ejecutar los Consumers (en terminales separadas)

**Terminal 1 - Consumer de Email Notification:**
```bash
cd Consumer.EmailNotification
dotnet run
```

**Terminal 2 - Consumer de Logging:**
```bash
cd Consumer.Logging
dotnet run
```

### 4. Ejecutar el Producer (en una nueva terminal)

```bash
cd Producer
dotnet run
```

## 📊 Flujo de Ejecución

1. **Producer** publica eventos a RabbitMQ
2. **RabbitMQ** enruta los eventos a las colas según los bindings
3. **Consumers** reciben los eventos
4. **Idempotency Service** verifica si el evento ya fue procesado en Redis
5. Si NO ha sido procesado:
   - Se ejecuta el handler del evento
   - Se marca el evento como procesado en Redis
6. Si YA fue procesado:
   - Se ignora (previene duplicados)

## 🔄 Idempotencia

La idempotencia se implementa usando **Redis** como almacén de eventos procesados:

```csharp
// Verificar si ya fue procesado
if (await idempotencyService.IsProcessedAsync(eventId))
{
    // Ignorar evento duplicado
    return;
}

// Procesar evento
await handler.HandleAsync(@event);

// Marcar como procesado (expira en 7 días)
await idempotencyService.MarkAsProcessedAsync(eventId);
```

**Beneficios:**
- Previene procesamiento duplicado de eventos
- Garantiza que cada evento se procesa exactamente una vez
- Los eventos marcados expiran automáticamente en 7 días

## 🧪 Pruebas de Idempotencia

Para probar la idempotencia:

1. Ejecuta el Producer
2. Mientras se procesan los eventos, mata un Consumer y reinicialo
3. Observa cómo los eventos duplicados son ignorados

En el log verás:
```
⚠️  Evento duplicado ignorado: 550e8400-e29b-41d4-a716-446655440000
```

## 🛠️ Configuración

### RabbitMQ
- **Host**: localhost
- **Port**: 5672
- **Usuario**: guest
- **Contraseña**: guest
- **Exchange**: event-driven-exchange (topic)

### Redis
- **Host**: localhost
- **Port**: 6379
- **Prefijo de clave**: `event-processed:{eventId}`
- **TTL**: 7 días

## 📚 Conceptos Clave

### Producer
Publicador de eventos que:
- Genera eventos de dominio
- Los publica a RabbitMQ con información metadata
- Usa routing keys para categorizar eventos

### Event Broker (RabbitMQ)
Intermediario que:
- Recibe eventos del producer
- Los enruta a múltiples consumers basado en suscripciones
- Garantiza entrega confiable con persistencia

### Consumers
Procesadores de eventos que:
- Se suscriben a eventos específicos
- Procesan eventos de forma asincrónica
- Implementan lógica de negocio

### Idempotencia
Garantía de que:
- Cada evento se procesa exactamente una vez
- Incluso con reintentos o duplicados
- Usando Redis como registro de eventos procesados

## 🔧 Extensiones Futuras

- ✅ Agregar Kafka como broker alternativo (opcional)
- ✅ Agregar base de datos para persistencia
- ✅ Agregar Circuit Breaker para resiliencia
- ✅ Agregar Dead Letter Queue para eventos fallidos
- ✅ Agregar métricas y monitoreo
- ✅ Agregar autenticación en RabbitMQ

## 📝 Notas

- Cada consumer tiene su propia queue
- Los eventos persisten en RabbitMQ si un consumer está caído
- La idempotencia se basa en el `MessageId` del evento
- Los consumers procesan eventos de forma secuencial (QoS = 1)

## 🆘 Troubleshooting

### Error de conexión a RabbitMQ
```
Error: Cannot connect to localhost:5672
Solución: Ejecuta docker-compose up -d y espera a que RabbitMQ esté ready
```

### Error de conexión a Redis
```
Error: Cannot connect to localhost:6379
Solución: Ejecuta docker-compose up -d y espera a que Redis esté ready
```

### RabbitMQ Management UI
Accede a: http://localhost:15672
Usuario: guest
Contraseña: guest

Aquí puedes ver:
- Exchanges y sus bindings
- Queues y mensajes pendientes
- Consumers activos
