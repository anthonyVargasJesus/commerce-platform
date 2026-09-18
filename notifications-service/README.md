# Notifications Service

Consume los eventos de integración que publica `orders-service` a través de RabbitMQ y los convierte en notificaciones para el cliente. Es un servicio puramente **reactivo**: no llama a ningún otro servicio, solo escucha.

## Cómo funciona

```
orders-service ──(outbox)──> RabbitMQ ──> OrderCreated/Confirmed/Shipped/Delivered/Cancelled consumers
                                                        │
                                            SendNotificationCommand (MediatR)
                                                        │
                                  INotificationSender (log)  +  tabla notifications (PostgreSQL)
```

- **Consumidores** (`Notifications.Infrastructure/Messaging/Consumers`): uno por evento; solo traducen el mensaje a un `SendNotificationCommand`. La lógica vive en la capa Application, igual que en los otros servicios.
- **Idempotencia**: RabbitMQ entrega *al menos una vez*, así que un mensaje puede llegar repetido. El **inbox** de MassTransit (tabla `notifications.InboxState`, mismo `MessageId`) descarta el duplicado y además guarda la notificación en la misma transacción que marca el mensaje como consumido. Hay un test de integración que publica el mismo mensaje dos veces y verifica que solo se crea una notificación.
- **Contratos**: los eventos se identifican por nombre completo (`Commerce.Contracts.Orders.*`). Este servicio declara su propia copia en `Messaging/Contracts`, idéntica a la del publicador; no hay proyecto compartido entre servicios.
- **Envío simulado**: `LoggingNotificationSender` solo escribe en el log. Un canal real (email/SMS) necesitaría los datos de contacto del cliente, que viven en `orders-service` y aún no viajan en los eventos.
- **Base de datos propia**: PostgreSQL (`notifications`), como cada microservicio la suya.

## Requisitos

- .NET 10 SDK
- Docker (PostgreSQL local, RabbitMQ y Testcontainers)
- RabbitMQ corriendo: se levanta desde `orders-service/docker-compose.yml` (`docker compose up -d rabbitmq`), porque es el broker compartido de la plataforma.

## Ejecutar localmente

```bash
docker compose up -d notifications-db
dotnet ef database update --project src/Notifications.Infrastructure --startup-project src/Notifications.API
dotnet run --project src/Notifications.API
```

Swagger en `/swagger` (Development). Para verlo funcionar: con `orders-service` e `inventory-service` corriendo, confirma una orden y consulta `GET /api/v1/notifications?orderId=<id>`.

## Tests

```bash
dotnet test tests/Notifications.UnitTests
dotnet test tests/Notifications.IntegrationTests   # requiere Docker (PostgreSQL y RabbitMQ reales vía Testcontainers)
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/notifications` | Lista paginada; filtros opcionales `customerId` y `orderId` |
| GET | `/api/v1/notifications/{id}` | Detalle de una notificación |
