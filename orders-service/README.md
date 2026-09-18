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

**Nota de diseño**: esta compensación es una llamada síncrona directa — no existe todavía un mecanismo de saga/outbox. Es una simplificación consciente (documentada), no un descuido.

## Resiliencia en la comunicación con Inventory

El `HttpClient` hacia `inventory-service` usa `Microsoft.Extensions.Http.Resilience` (`AddStandardResilienceHandler()`), que aplica reintentos, circuit breaker y timeouts por defecto — si Inventory está caído momentáneamente, Orders no falla de una, reintenta con backoff.

## Requisitos

- .NET 10 SDK
- Docker (para SQL Server local y Testcontainers)
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
dotnet test tests/Orders.IntegrationTests   # requiere Docker corriendo (SQL Server real vía Testcontainers)
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
