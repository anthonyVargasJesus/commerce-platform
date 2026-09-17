---
name: entidad-referencia
description: Crea una nueva entidad de referencia simple (catálogo, tipo Category o ProductType) en inventory-service, con CRUD completo en las 4 capas de Clean Architecture. Úsalo cuando el usuario pida agregar una tabla/catálogo/lista de referencia nueva (ej. "necesito una tabla de marcas", "agrega un catálogo de proveedores"), no para agregados ricos con reglas de negocio complejas como Product.
argument-hint: "[NombreDeLaEntidad]"
disable-model-invocation: false
---

Crea la entidad de referencia **$ARGUMENTS** en inventory-service, siguiendo exactamente
los mismos lineamientos que Product (documentados en CLAUDE.md), pero simplificada porque
es un catálogo de referencia, no un agregado con reglas de negocio complejas.

## Domain (src/Inventory.Domain/$ARGUMENTS/)
- Clase `sealed class $ARGUMENTS : BaseAuditableEntity`.
- Constructor privado + factory estático `Create(string name, string? description = null)`
  que valida `name` no vacío (lanza `DomainException`, igual que Product.Create).
- Método `UpdateDetails(name, description)`.
- Sin domain events (no hace falta para un catálogo simple).

## Application (src/Inventory.Application/$ARGUMENTS/)
- CRUD completo, un folder por operación (mismo patrón que Products):
  `Commands/Create$ARGUMENTS/`, `Commands/Update$ARGUMENTS/`, `Commands/Delete$ARGUMENTS/`
  (Command + Handler, con Validator donde el input lo requiera) y
  `Queries/Get$ARGUMENTSById/`, `Queries/Get$ARGUMENTSsList/` (sin paginar — son listas cortas).
- DTO propio en cada carpeta (no uno compartido) — con su `FromDomain(...)`.
- Interfaz `I$ARGUMENTSRepository` en Common/Interfaces con `Add`, `GetByIdAsync`,
  `GetAllAsync`, y lo que Update/Delete necesiten (ej. `Remove` si es hard-delete).

## Infrastructure (src/Inventory.Infrastructure/Persistence/)
- `$ARGUMENTSConfiguration` (tabla en snake_case, schema "inventory", igual que ProductConfiguration).
- `$ARGUMENTSRepository`.
- Agregar `DbSet<$ARGUMENTS>` a InventoryDbContext.

## API (src/Inventory.API/Controllers/V1/)
- `$ARGUMENTSsController` thin, mismo estilo que ProductsController (inyecta ISender).

## Antes de tocar Product
Pregúntame si el FK correspondiente en Product debe ser nullable u obligatorio —
no lo agregues todavía sin confirmar.

## Al terminar
1. Genera la migración EF Core (`dotnet ef migrations add Add$ARGUMENTS ...`).
2. Corre `dotnet build` (TreatWarningsAsErrors=true, cualquier warning rompe el build).
3. Muéstrame el diff antes de aplicar la migración a la base de datos.
