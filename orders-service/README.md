# Orders Service

Microservicio de pedidos construido en **.NET 10** siguiendo Clean Architecture, CQRS y los mismos lineamientos profesionales de `inventory-service`. Es el segundo servicio de la arquitectura de microservicios (Inventory, Orders, y luego Notifications + API Gateway).

## Decisión deliberada: persistencia poliglota

A diferencia de Inventory (PostgreSQL), Orders usa **SQL Server** — cada microservicio es dueño exclusivo de su propia base de datos y elige la tecnología que mejor le convenga, sin compartir esquema ni instancia con ningún otro servicio.

## Arquitectura

```
src/
  Orders.Domain          -> Agregado Order (con ciclo de vida completo), OrderItem, eventos de dominio
  Orders.Application     -> CQRS (MediatR), validaciones (FluentValidation), puertos (IOrderRepository,
                             IInventoryServiceClient) que Infrastructure implementa
  Orders.Infrastructure  -> EF Core + SQL Server, cliente HTTP resiliente hacia inventory-service (Polly)
  Orders.API             -> Controllers, versionado de API, Swagger, health checks, OpenTelemetry
tests/
  Orders.UnitTests        -> Pruebas de dominio y de handlers (Moq + Shouldly), incluye la lógica de
                              compensación de ConfirmOrder
  Orders.IntegrationTests -> Pruebas end-to-end con SQL Server real (Testcontainers) e inventory-service
                              simulado con WireMock.Net
```

## El ciclo de vida del pedido (y por qué toca a Inventory solo en 2 puntos)

```
Pending --Confirm--> Confirmed --Ship--> Shipped --Deliver--> Delivered
   |                     |
   +------Cancel---------+
             |
             v
         Cancelled
```

- **`CreateOrder`**: solo toma un **snapshot** de precio/nombre/disponibilidad de cada producto vía HTTP GET a Inventory — no descuenta stock. El pedido nace en `Pending`.
- **`ConfirmOrder`**: acá sí se descuenta stock de verdad (`POST /adjust-stock` con delta negativo por cada línea). Si algún ítem no tiene stock suficiente, **se compensan** (se devuelven) los ítems que ya se habían descontado en esa misma llamada, y la confirmación falla sin dejar nada a medias.
- **`CancelOrder`**: si el pedido estaba `Confirmed`, se devuelve el stock a Inventory (delta positivo) como compensación.
- **`ShipOrder`/`DeliverOrder`**: solo cambian el estado internamente, sin tocar Inventory.

**Nota de diseño**: esta compensación es una llamada síncrona directa — no hay saga. Es una simplificación consciente (documentada), no un descuido. (El outbox descrito abajo cubre la publicación de eventos hacia otros servicios, no esta compensación.)

## Resiliencia en la comunicación con Inventory

El `HttpClient` hacia `inventory-service` usa `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler()`), que aplica reintentos, circuit breaker y timeouts por defecto — si Inventory está caído momentáneamente, Orders no falla de una, reintenta con backoff.

## Seguridad

La API exige un JWT de Keycloak (roles `admin`, `customer`, `service`; ver el README de la raíz). Las llamadas a Inventory no reenvían el token del usuario: `ServiceTokenHandler` adjunta el token propio de `orders-service` (OAuth2 *client credentials*, configurado en `ServiceAuth`), que `ServiceTokenProvider` cachea hasta 30 s antes de que expire. El secreto de `appsettings.json` es solo de desarrollo.

## Eventos de integración (RabbitMQ + MassTransit outbox)

Los eventos de dominio del agregado `Order` (created/confirmed/shipped/delivered/cancelled) se publican a RabbitMQ como eventos de integración (`Commerce.Contracts.Orders.*`). `DomainEventsOutboxInterceptor` los toma en el `SaveChanges` y los guarda en la tabla `orders.OutboxMessage` **dentro de la misma transacción** que el cambio de la orden; el bus outbox de MassTransit los entrega después a RabbitMQ. Así un evento nunca se pierde si el broker está caído, ni se publica si la transacción falla.

- MassTransit se fija en la v8 (Apache 2.0): desde la v9 es de licencia comercial.
- Los contratos se identifican por nombre completo (namespace + tipo): cada consumidor declara su propia copia bajo el namespace `Commerce.Contracts.Orders`, sin proyecto compartido entre servicios.
- Consola de RabbitMQ: http://localhost:15672 (guest/guest).

## Requisitos

- .NET 10 SDK
- Docker (para SQL Server y RabbitMQ locales, y Testcontainers)
- `inventory-service` corriendo (para que `CreateOrder`/`ConfirmOrder`/`CancelOrder` funcionen de verdad)

## Ejecutar localmente

```bash
docker compose up -d orders-db
dotnet ef database update --project src/Orders.Infrastructure --startup-project src/Orders.API
dotnet run --project src/Orders.API
```

Swagger disponible en `/swagger` en entorno `Development`. Asegúrate de que `inventory-service` esté corriendo en la URL configurada en `Services:Inventory:BaseUrl` (por defecto `http://localhost:5254`).

## Tests

```bash
dotnet test tests/Orders.UnitTests
dotnet test tests/Orders.IntegrationTests   # requiere Docker corriendo (SQL Server y RabbitMQ reales vía Testcontainers)
```

## Endpoints principales

| Método | Ruta                              | Descripción                                  |
|--------|-----------------------------------|-----------------------------------------------|
| GET    | `/api/v1/orders`                  | Lista paginada de pedidos                      |
| GET    | `/api/v1/orders/{id}`             | Obtiene un pedido por id                       |
| POST   | `/api/v1/orders`                  | Crea un pedido (`Pending`, snapshot de precios)|
| POST   | `/api/v1/orders/{id}/confirm`     | Confirma el pedido (descuenta stock real)      |
| POST   | `/api/v1/orders/{id}/cancel`      | Cancela el pedido (devuelve stock si aplica)   |
| POST   | `/api/v1/orders/{id}/ship`        | Marca el pedido como enviado                   |
| POST   | `/api/v1/orders/{id}/deliver`     | Marca el pedido como entregado                 |
