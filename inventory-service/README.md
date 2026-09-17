# Inventory Service

Microservicio de catálogo e inventario construido en **.NET 10** siguiendo Clean Architecture, CQRS y buenas prácticas de nivel profesional. Es el primer servicio de una arquitectura de microservicios (Inventory, Orders, Notifications + API Gateway).

## Arquitectura

```
src/
  Inventory.Domain          -> Entidades, value objects, eventos de dominio, invariantes de negocio
  Inventory.Application     -> CQRS (MediatR), validaciones (FluentValidation), interfaces de puertos
  Inventory.Infrastructure  -> EF Core + PostgreSQL, repositorios, migraciones
  Inventory.API             -> Controllers, versionado de API, Swagger, health checks, OpenTelemetry
tests/
  Inventory.UnitTests        -> Pruebas de dominio y de handlers (Moq + Shouldly)
  Inventory.IntegrationTests -> Pruebas end-to-end con Testcontainers (PostgreSQL real)
```

Las dependencias fluyen hacia el dominio: `API -> Application/Infrastructure -> Domain`. La capa `Domain` no depende de nada externo.

## Lineamientos aplicados

- **Clean Architecture** con separación estricta de capas.
- **CQRS** con MediatR (comandos y queries independientes) + pipeline de validación automática.
- **DDD táctico**: agregado `Product`, value object `Sku`, eventos de dominio (`ProductCreatedEvent`, `StockAdjustedEvent`, `ProductLowStockEvent`).
- **Repository + Unit of Work** para desacoplar el dominio de EF Core.
- **Manejo global de errores** vía `IExceptionHandler` con respuestas `ProblemDetails`.
- **Versionado de API** (`/api/v1/...`) con `Asp.Versioning`.
- **Observabilidad**: logging estructurado (Serilog), health checks (`/health/live`, `/health/ready`), OpenTelemetry (trazas y métricas vía OTLP).
- **Documentación**: Swagger/OpenAPI por versión.
- **Auditoría**: `CreatedAt`/`LastModifiedAt` automáticos en `SaveChangesAsync`.
- **Tests**: unitarios (dominio + handlers) e integración con contenedor real de PostgreSQL.
- **Docker**: imagen multi-stage, no-root, lista para producción.
- **CI**: build + tests + build de imagen Docker en GitHub Actions.

## Requisitos

- .NET 10 SDK
- Docker (para PostgreSQL local y Testcontainers)

## Ejecutar localmente

```bash
docker compose up -d inventory-db
dotnet ef database update --project src/Inventory.Infrastructure --startup-project src/Inventory.API
dotnet run --project src/Inventory.API
```

La API queda disponible en `https://localhost:5001` (o el puerto asignado), con Swagger UI en `/swagger` en entorno `Development`.

## Ejecutar todo con Docker Compose

```bash
docker compose up --build
```

## Tests

```bash
dotnet test tests/Inventory.UnitTests
dotnet test tests/Inventory.IntegrationTests   # requiere Docker corriendo (Testcontainers)
```

## Endpoints principales

| Método | Ruta                                   | Descripción                     |
|--------|-----------------------------------------|----------------------------------|
| GET    | `/api/v1/products`                      | Lista paginada de productos      |
| GET    | `/api/v1/products/{id}`                 | Obtiene un producto por id       |
| POST   | `/api/v1/products`                      | Crea un producto                 |
| PUT    | `/api/v1/products/{id}`                 | Actualiza datos de un producto   |
| POST   | `/api/v1/products/{id}/adjust-stock`    | Ajusta el stock (+/-)            |
| DELETE | `/api/v1/products/{id}`                 | Desactiva un producto (soft delete) |

## Próximos pasos

- Publicar eventos de dominio a un broker de mensajería (RabbitMQ/MassTransit) cuando se agregue el servicio `Notifications`.
- Añadir el servicio `Orders`, que consumirá `Inventory` vía HTTP para reservar stock.
- Añadir el `API Gateway` (YARP) delante de ambos servicios.
