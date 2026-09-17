# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

This is a **monorepo intended to hold multiple .NET 10 microservices** (currently only `inventory-service/` exists; `orders-service/`, `notifications-service/`, and an API gateway are planned). Root-level files (`.gitignore`, `.github/workflows/`) apply to the whole repo; each service is self-contained under its own top-level folder with its own `.sln`.

## Commands (run from `inventory-service/`)

```bash
# Build / test
dotnet build
dotnet test tests/Inventory.UnitTests                 # domain + handler tests (Moq + Shouldly), no external deps
dotnet test tests/Inventory.IntegrationTests           # spins up a real Postgres via Testcontainers — requires Docker running
dotnet test tests/Inventory.UnitTests --filter "FullyQualifiedName~ProductTests"   # single test class

# Local database (Docker)
docker compose up -d inventory-db                      # starts only Postgres
docker compose up --build                               # starts Postgres + the containerized API

# EF Core migrations (run from inventory-service/)
dotnet ef migrations add <Name> --project src/Inventory.Infrastructure --startup-project src/Inventory.API --output-dir Persistence/Migrations
dotnet ef database update --project src/Inventory.Infrastructure --startup-project src/Inventory.API

# Run the API
dotnet run --project src/Inventory.API
```

Note: in `Development`, `Program.cs` calls `context.Database.MigrateAsync()` and seeds sample data (`InventoryDbContextSeed`) automatically on startup — manual `dotnet ef database update` is only required outside Development or before the first migration exists.

## Architecture: `inventory-service`

Clean Architecture, dependencies point inward: `API -> Application/Infrastructure -> Domain`. `Domain` has zero external dependencies.

```
src/
  Inventory.Domain          Aggregates, value objects, domain events, domain exceptions. No EF/MediatR references.
  Inventory.Application     CQRS via MediatR (one folder per Command/Query under Products/), FluentValidation validators,
                             port interfaces (IProductRepository, IUnitOfWork) that Infrastructure implements.
  Inventory.Infrastructure  EF Core + Npgsql, InventoryDbContext, entity configurations, repository implementations, migrations.
  Inventory.API             Controllers (versioned under Controllers/V1), Program.cs composition root, global exception handler.
```

Key conventions specific to this service:

- **CQRS shape**: every command/query lives in its own folder (`Products/Commands/<Name>/`) with up to 3 files — the request record, its handler, and its FluentValidation validator. Validators run automatically via `ValidationBehaviour<,>`, a MediatR pipeline behavior registered in `Inventory.Application/DependencyInjection.cs` — handlers never validate input themselves.
- **Domain events** are raised inside `Product` (e.g. `ProductCreatedEvent`, `StockAdjustedEvent`, `ProductLowStockEvent`) via `BaseEntity.AddDomainEvent`, but nothing currently dispatches them (no outbox/publisher wired up yet) — they only exist in `Product.DomainEvents` for now. Wiring these to a broker is expected when `notifications-service` is added.
- **Repository + Unit of Work**: `Inventory.Application` only depends on `IProductRepository`/`IUnitOfWork` interfaces; `InventoryDbContext` itself implements `IUnitOfWork` (see `Inventory.Infrastructure/DependencyInjection.cs`).
- **Sku** is a value object (`Inventory.Domain/Products/Sku.cs`) that normalizes to uppercase on creation; the EF configuration converts it to/from `string` — always create it via `Sku.Create(...)`, never compare raw strings against `Product.Sku` directly.
- **Error handling**: exceptions map to HTTP status via `GlobalExceptionHandler` (`Inventory.API/Middleware/`) — `NotFoundException` → 404, `ConflictException` → 409, `Inventory.Application.Common.Exceptions.ValidationException` → 400, `Domain.Exceptions.DomainException` → 422, anything else → 500. Throw the appropriate `Application.Common.Exceptions.*` type from handlers rather than returning error codes from controllers.
- **API versioning**: routes are `api/v{version}/...`; new controller versions go in `Controllers/V{n}/` and need a matching entry picked up by `ConfigureSwaggerOptions` (already generic, no changes needed there when adding versions).
- **Soft delete**: `DeleteProductCommand` calls `Product.Deactivate()` (sets `IsActive = false`), it does not remove the row — there is intentionally no hard-delete path.
- **Directory.Build.props** (repo-wide per service) sets `TreatWarningsAsErrors=true` — a build with warnings will fail; `Asp.Versioning`'s analyzers (`AV00xx`) are treated as errors too, so versioning setup in `Program.cs` must use `.AddApiVersioning().AddMvc().AddApiExplorer()` (not `AddApiVersioning()` alone) or the build breaks.
