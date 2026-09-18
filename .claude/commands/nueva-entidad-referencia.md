---
description: Crea una nueva entidad de referencia simple (tipo Category, ProductType, Customer) en cualquier microservicio del monorepo
argument-hint: "[NombreDeLaEntidad] [servicio: inventory|orders|...]"
---

Argumentos crudos de esta invocación: $0 / $1

Resuelve primero, antes de leer el resto de este documento:
- **NombreEntidad** = el primer argumento (nombre de la entidad, ej. "Customer").
- **Servicio** = el segundo argumento (servicio destino, ej. "orders").

Si arriba ($0 / $1) ves literalmente el texto "$0" o "$1" sin sustituir (en vez de los
valores reales), la sustitución automática falló: en su lugar, toma el texto crudo de
`<command-args>` de esta invocación y divídelo por espacio — la primera palabra es
NombreEntidad, la segunda es Servicio. No le pidas al usuario que lo reescriba.

A partir de aquí, cada vez que este documento diga **NombreEntidad** o **Servicio**,
sustitúyelo tú mismo mentalmente por los valores resueltos arriba (ej. si NombreEntidad
es "Customer", "NombreEntidadRepository" significa `CustomerRepository`).

Crea la entidad de referencia NombreEntidad en el servicio Servicio, siguiendo el mismo
patrón de catálogo simple ya usado en `Category`/`ProductType` (inventory-service) —
pero **sin asumir de antemano** el namespace, el nombre del DbContext, el proveedor de
base de datos ni la convención de columnas: eso se infiere del servicio destino en el
paso 0.

## Paso 0 — Inferir convenciones del servicio destino (obligatorio, antes de crear nada)

1. Ubica la carpeta `Servicio-service/` y su proyecto raíz de namespace (capitalizando
   Servicio, ej. `orders` → `Orders.*`).
2. Busca el `DbContext` en `src/<Root>.Infrastructure/Persistence/*.cs` (clase que
   extiende `DbContext`) — usa su nombre exacto (ej. `OrdersDbContext`) y revisa su
   `OnModelCreating` para ver si define `HasDefaultSchema(...)`.
3. Busca `src/<Root>.Infrastructure/DependencyInjection.cs` para ver qué proveedor usa
   (`UseNpgsql` vs `UseSqlServer`, etc.) — determina snake_case+`HasColumnName` (estilo
   Postgres/Inventory) vs PascalCase sin `HasColumnName` (estilo SQL Server/Orders).
4. Abre una configuración de entidad existente en ese mismo servicio
   (`src/<Root>.Infrastructure/Persistence/Configurations/*.cs`) para confirmar el
   patrón real de columnas/tabla/índices en la práctica, no solo por el proveedor.
5. Confirma la ruta de `BaseAuditableEntity`/`DomainException` en
   `src/<Root>.Domain/Common/` y `src/<Root>.Domain/Exceptions/` de ese servicio.
6. Si algo no está claro (p. ej. el servicio no tiene todavía ninguna entidad de
   referencia previa para comparar), pregúntale al usuario antes de asumir.

## Domain (src/<Root>.Domain/NombreEntidad/)
- Clase `sealed class NombreEntidad : BaseAuditableEntity` (namespace del servicio destino).
- Constructor privado + factory estático `Create(...)` que valida los campos requeridos
  (lanza `DomainException`, mismo estilo que `Category.Create`/`Product.Create`).
  Los campos concretos dependen de la entidad — confírmalos con el usuario si no son
  obvios (ej. `Category` es `name, description?`; `Customer` sería `name, email, phone?`).
- Método `UpdateDetails(...)` con los mismos campos.
- Sin domain events (no hace falta para un catálogo simple).

## Application (src/<Root>.Application/NombreEntidad/)
- CRUD completo, un folder por operación:
  `Commands/CreateNombreEntidad/`, `Commands/UpdateNombreEntidad/`, `Commands/DeleteNombreEntidad/`
  (Command + Handler, con Validator donde el input lo requiera) y
  `Queries/GetNombreEntidadById/`, `Queries/GetNombreEntidadsList/` (sin paginar — son listas cortas).
- DTO propio en cada carpeta (no uno compartido) — con su `FromDomain(...)`.
- Interfaz `INombreEntidadRepository` en `<Root>.Application/Common/Interfaces` con `Add`,
  `GetByIdAsync`, `GetAllAsync`, y lo que Update/Delete necesiten (ej. `Remove` si es
  hard-delete, o un `XxxExistsAsync` para el campo de unicidad de negocio).

## Infrastructure (src/<Root>.Infrastructure/Persistence/)
- `NombreEntidadConfiguration` — replica el estilo de columnas/tabla/esquema inferido en
  el Paso 0 (no asumas snake_case ni el esquema "inventory" si el servicio destino no es
  inventory-service).
- `NombreEntidadRepository`.
- Agregar `DbSet<NombreEntidad>` al DbContext detectado en el Paso 0.
- Registrar `INombreEntidadRepository` en el `DependencyInjection.cs` de ese servicio.

## API (src/<Root>.API/Controllers/V1/)
- `NombreEntidadsController` thin, mismo estilo que `CategoriesController`/`OrdersController`
  del servicio destino (inyecta `ISender`, versión `api/v{version:apiVersion}/...`).

## Antes de conectar un FK a otro agregado del mismo servicio
Si la entidad se relaciona naturalmente con un agregado existente del servicio (ej.
`Customer` con `Order`, o `Brand` con `Product`), **no agregues el FK todavía**:
pregúntale al usuario si quiere conectarlo ahora (nullable u obligatorio) o dejarlo
desconectado por ahora, igual que se hizo con `Category`/`ProductType` en `Product`.

## Al terminar
1. Genera la migración EF Core desde la carpeta del servicio (`dotnet ef migrations add
   AddNombreEntidad --project src/<Root>.Infrastructure --startup-project src/<Root>.API
   --output-dir Persistence/Migrations`).
2. Corre `dotnet build` desde la carpeta del servicio (`TreatWarningsAsErrors=true`,
   cualquier warning rompe el build).
3. Muéstrame el diff de la migración antes de aplicarla a la base de datos.
