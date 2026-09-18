# Notifications Service

Consume los eventos de integración que publican `orders-service` (ciclo de vida de la orden) e `inventory-service` (stock bajo) a través de RabbitMQ y los convierte en notificaciones. Es un servicio puramente **reactivo**: no llama a ningún otro servicio, solo escucha.

## Cómo funciona

```
orders-service ──(outbox)──> RabbitMQ ──> OrderCreated/Confirmed/Shipped/Delivered/Cancelled consumers
                                                        │
                                            SendNotificationCommand (MediatR)
                                                        │
                        INotificationSender (email SMTP)  +  tabla notifications (PostgreSQL)
```

- **Alertas de stock**: `ProductLowStock` (de Inventory) genera una notificación de tipo `LowStock` que referencia el producto y no tiene orden ni cliente (`OrderId`/`CustomerId` son opcionales; `ProductId` identifica la alerta). Se consulta con `GET /api/v1/notifications?productId=<id>`.
- **Consumidores** (`Notifications.Infrastructure/Messaging/Consumers`): uno por evento; solo traducen el mensaje a un `SendNotificationCommand`. La lógica vive en la capa Application, igual que en los otros servicios.
- **Idempotencia**: RabbitMQ entrega *al menos una vez*, así que un mensaje puede llegar repetido. El **inbox** de MassTransit (tabla `notifications.InboxState`, mismo `MessageId`) descarta el duplicado y además guarda la notificación en la misma transacción que marca el mensaje como consumido. Hay un test de integración que publica el mismo mensaje dos veces y verifica que solo se crea una notificación.
- **Contratos**: los eventos se identifican por nombre completo (`Commerce.Contracts.Orders.*`). Este servicio declara su propia copia en `Messaging/Contracts`, idéntica a la del publicador; no hay proyecto compartido entre servicios.
- **Envío por email (SMTP)**: `SmtpNotificationSender` (MailKit) envía cada notificación como correo de texto. Los eventos de Orders llevan el nombre y el email del cliente (Orders los consulta en su base al publicar), y las alertas de stock van al correo de operaciones (`Smtp:OperationsEmail`). En desarrollo el servidor SMTP es **Mailpit**, que captura los correos en vez de entregarlos: interfaz web en http://localhost:8025. Con `Notifications:Channel = Log` solo se escribe en el log (`LoggingNotificationSender`).
- **Fallos de envío**: si el SMTP no responde, MassTransit reintenta 3 veces (cada 2 s) y luego deja el mensaje en la cola `<evento>_error` de RabbitMQ, sin perderlo; la notificación no se guarda hasta que el envío tiene éxito.
- **Base de datos propia**: PostgreSQL (`notifications`), como cada microservicio la suya.

## Requisitos

- .NET 10 SDK
- Docker (PostgreSQL local, RabbitMQ, Mailpit y Testcontainers)
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
dotnet test tests/Notifications.IntegrationTests   # requiere Docker (PostgreSQL, RabbitMQ y Mailpit reales vía Testcontainers)
```

## Endpoints

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/v1/notifications` | Lista paginada; filtros opcionales `customerId`, `orderId` y `productId` |
| GET | `/api/v1/notifications/{id}` | Detalle de una notificación |
