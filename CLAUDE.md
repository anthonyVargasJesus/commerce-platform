# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository layout

This is a **monorepo intended to hold multiple .NET 10 microservices** (`inventory-service/` on PostgreSQL, `orders-service/` on SQL Server and `notifications-service/` on PostgreSQL exist; `gateway/` (YARP) is the single HTTP entry point and Keycloak (`keycloak/commerce-realm.json`, users admin/maria) is the identity provider; every API and the gateway validate the Keycloak JWT and apply roles admin/customer/service; Orders calls Inventory with its own client-credentials token, see the root README). Orders calls Inventory over HTTP and publishes its order events to RabbitMQ through a MassTransit transactional outbox; Notifications consumes them (see each service's README). The per-service commands below are written for `inventory-service/`; the other services use the same layout and `dotnet ef` invocations with their own project names, and each has its own `docker-compose.yml` (RabbitMQ is started from `orders-service/`). Root-level files (`.gitignore`, `.github/workflows/`) apply to the whole repo; each service is self-contained under its own top-level folder with its own `.sln`. Chosen deliberately over polyrepo because this is a single-developer learning project where sharing `CLAUDE.md` and `.claude/` commands/skills across services matters more than per-team repo isolation.

## Git workflow

- **GitHub Flow, not Git Flow**: `main` is always deployable, no `develop` branch. Each change gets its own branch, merged via PR — chosen because these services deploy continuously (CI triggers per push), not in scheduled batches that would benefit from a `develop` integration branch.
- **Branch naming includes the service**, since multiple services share this repo: `<service>/<type>-<short-description>`, e.g. `inventory/fix-product-soft-delete`, `orders/feature-create-order`. A bare `fix/...` or `feature/...` without the service prefix is ambiguous once more than one service exists in the repo.
- **`main` has branch protection**: no direct pushes (PRs only), required status checks `build-and-test` and `docker-build` must pass, `enforce_admins` is on (no bypass). CI workflows are path-filtered per service (see `.github/workflows/inventory-ci.yml`'s `paths: inventory-service/**`), so a PR only triggers the CI of the service(s) it actually touches.

## Whole platform (run from the repo root)

```bash
docker compose up --build      # RabbitMQ, Keycloak, 3 databases, the 3 APIs and the gateway (:8000, the only published entry point)
docker compose down             # add -v to also delete the data volumes
```

The APIs are not published by the root compose; `docker compose -f docker-compose.yml -f docker-compose.direct-access.yml up` also publishes them on :8080/:8081/:8082 for Swagger (whose Authorize button takes a Keycloak token) and debugging. The root `docker-compose.yml` and each service's own compose file publish the same host ports for the databases and RabbitMQ, so run one or the other. Container images run on Alpine in globalization-invariant mode: never ask for a specific culture (e.g. `CultureInfo.GetCultureInfo("en-US")`) in code, and images that talk to SQL Server need ICU (see the orders Dockerfile). CI only builds the images, so this kind of failure only shows when the containers actually run.

End-to-end tests: `e2e-tests/` (black box; its fixture runs the root compose as project `commerce-e2e`, needs the platform ports free; `E2E_USE_RUNNING_PLATFORM=1` reuses a running platform). The CI job is `e2e-tests` (`.github/workflows/e2e-ci.yml`).

Authentication: each API has `Configuration/AuthenticationExtensions.cs` (JwtBearer + `realm_access.roles` mapped to role claims; `Authentication:Authority` is the public issuer, `Authentication:MetadataAddress` the internal Keycloak URL in Docker). Controllers use `[Authorize]`/`[Authorize(Roles = ...)]`. Integration tests sign real JWTs with a test key (`TestTokens`, `factory.CreateClientWithRoles(...)`); all test classes of a project share one factory through the `ApiCollection` xUnit collection because the factories configure the host through process-wide environment variables.

Observability: every API exports traces, metrics and Serilog logs over OTLP to the Aspire Dashboard (UI on http://localhost:18888; the compose files publish OTLP on host port 4317, the default exporter endpoint). Traces cross services, including the RabbitMQ hop (`AddSource("MassTransit")`). Add new services to this setup the same way (`AddSource`/`AddMeter` for the DB driver and MassTransit, `Serilog.Sinks.OpenTelemetry` with an explicit `service.name`). The root `README.md` describes the whole platform.

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

Three aggregates exist: `Product` (rich aggregate — stock rules, domain events) and `Category`/`ProductType` (simple reference/catalog entities with full CRUD, no domain events). Category and ProductType were scaffolded via the `entidad-referencia` command/skill (`.claude/commands/nueva-entidad-referencia.md`, `.claude/skills/entidad-referencia/`) — use that when adding another simple reference entity rather than hand-rolling the pattern.

Key conventions specific to this service:

- **CQRS shape**: every command/query lives in its own folder (`<Aggregate>/Commands|Queries/<Name>/`) with the request record, its handler, and (where the input needs validating) a FluentValidation validator. Validators run automatically via `ValidationBehaviour<,>`, a MediatR pipeline behavior registered in `Inventory.Application/DependencyInjection.cs` — handlers never validate input themselves. **DTO placement rule**: a DTO used by only one command/query lives in that folder (Category/ProductType do this — each operation has its own dedicated DTO, deliberately not shared, so one operation's response shape can never be constrained by another's); a DTO shared by several operations of the same aggregate lives in `<Aggregate>/Dtos/` (`Product` does this — `ProductDto` is reused by Create/Update/AdjustStock/GetById/GetList because they all want the identical full shape); a DTO shared across aggregates would go in `Application/Common/`. There is no fixed file-count-per-folder rule — the file count follows from how many DTOs that operation actually owns.
- **Soft delete is enforced at the model level for `Product` only**: `DeleteProductCommand` calls `Product.Deactivate()` (`IsActive = false`), and `ProductConfiguration` declares `builder.HasQueryFilter(p => p.IsActive)` — every query against `Products` (via `ProductRepository`) excludes inactive rows automatically, so don't re-add manual `&& p.IsActive` checks in new repository methods. The `Sku` unique index is partial (`HasFilter("is_active = true")`) so a deleted product's SKU can be reused. `Category` and `ProductType` hard-delete instead (`repository.Remove(...)`) and block deletion via `IProductRepository.ExistsForCategoryAsync`/`ExistsForProductTypeAsync` when products still reference them — this soft/hard-delete split between aggregates is a known inconsistency, not an intentional design choice; don't assume new aggregates should copy either pattern without deciding first.
- **Domain events** are raised inside `Product` (e.g. `ProductCreatedEvent`, `StockAdjustedEvent`, `ProductLowStockEvent`) via `BaseEntity.AddDomainEvent`. `DomainEventsOutboxInterceptor` maps them to integration events (`Commerce.Contracts.Inventory.*`) on `SaveChanges` and stores them in the MassTransit EF outbox in the same transaction; MassTransit then delivers them to RabbitMQ. Orders uses the same pattern (see the service READMEs). Consumers declare their own copy of the contracts under the same namespace.
- **Repository + Unit of Work**: `Inventory.Application` only depends on `I<Aggregate>Repository`/`IUnitOfWork` interfaces; `InventoryDbContext` itself implements `IUnitOfWork` (see `Inventory.Infrastructure/DependencyInjection.cs`).
- **Sku** is a value object (`Inventory.Domain/Products/Sku.cs`) that normalizes to uppercase on creation; the EF configuration converts it to/from `string` — always create it via `Sku.Create(...)`, never compare raw strings against `Product.Sku` directly.
- **Name uniqueness for Category/ProductType**: checked explicitly via `NameExistsAsync(name, excludingId, ct)` before insert/update (mirrors `Product`'s `SkuExistsAsync` pattern), throwing `ConflictException` — this is a deliberate pre-check, not redundant with the DB unique index; removing it would let an unhandled `DbUpdateException` reach the client as a generic 500 instead of a clean 409. Update handlers skip the query entirely when the name is unchanged.
- **Error handling**: exceptions map to HTTP status via `GlobalExceptionHandler` (`Inventory.API/Middleware/`) — `NotFoundException` → 404, `ConflictException` → 409, `Inventory.Application.Common.Exceptions.ValidationException` → 400, `Domain.Exceptions.DomainException` → 422, anything else → 500. Throw the appropriate `Application.Common.Exceptions.*` type from handlers rather than returning error codes from controllers.
- **API versioning**: routes are `api/v{version}/...`; new controller versions go in `Controllers/V{n}/` and need a matching entry picked up by `ConfigureSwaggerOptions` (already generic, no changes needed there when adding versions).
- **Directory.Build.props** (repo-wide per service) sets `TreatWarningsAsErrors=true` — a build with warnings will fail; `Asp.Versioning`'s analyzers (`AV00xx`) are treated as errors too, so versioning setup in `Program.cs` must use `.AddApiVersioning().AddMvc().AddApiExplorer()` (not `AddApiVersioning()` alone) or the build breaks.
