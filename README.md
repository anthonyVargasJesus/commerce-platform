# Commerce Platform

Plataforma de comercio construida como microservicios en **.NET 10**: catálogo y stock, pedidos y notificaciones al cliente. Es un proyecto de aprendizaje/portafolio que aplica las prácticas de un entorno profesional: Clean Architecture, CQRS, mensajería con outbox, pruebas contra infraestructura real, CI por servicio y observabilidad distribuida.

## Arquitectura

```mermaid
flowchart LR
    client([Cliente HTTP])

    gw[gateway<br/>YARP :8000]
    kc[Keycloak<br/>emite los JWT]

    subgraph inv[inventory-service]
        inv_api[API]
        inv_db[(PostgreSQL)]
    end
    subgraph ord[orders-service]
        ord_api[API]
        ord_db[(SQL Server)]
    end
    subgraph not[notifications-service]
        not_api[API + consumidores]
        not_db[(PostgreSQL)]
    end

    rabbit{{RabbitMQ}}
    smtp[[Mailpit / SMTP]]
    otel[[Aspire Dashboard<br/>trazas, métricas, logs]]

    client --> gw
    client -. login .-> kc
    gw --> inv_api
    gw --> ord_api
    gw --> not_api
    inv_api --- inv_db
    ord_api --- ord_db
    not_api --- not_db

    ord_api -- "HTTP: consulta producto, ajusta stock" --> inv_api
    ord_api -- "eventos de orden" --> rabbit
    inv_api -- "eventos de stock" --> rabbit
    rabbit --> not_api
    not_api --> smtp
```

| Servicio | Responsabilidad | Base de datos | Puerto interno (Docker) |
|---|---|---|---|
| [gateway](gateway/README.md) | Única entrada HTTP: enruta por prefijo a los servicios (YARP) | - | 8000 |
| [inventory-service](inventory-service/README.md) | Catálogo de productos, categorías, tipos y control de stock | PostgreSQL | 8080 |
| [orders-service](orders-service/README.md) | Clientes y ciclo de vida de las órdenes (crear, confirmar, enviar, entregar, cancelar) | SQL Server | 8081 |
| [notifications-service](notifications-service/README.md) | Convierte los eventos en correos (al cliente y a operaciones) | PostgreSQL | 8082 |

Cada servicio es autónomo (su propia solución, base de datos, Dockerfile y CI) y usa la misma estructura: `Domain` → `Application` (CQRS con MediatR) → `Infrastructure` → `API`.

## Cómo se comunican

- **Síncrono (HTTP)**: Orders llama a Inventory cuando necesita una respuesta inmediata (¿existe el producto? ¿hay stock?), con reintentos y circuit breaker.
- **Asíncrono (RabbitMQ + MassTransit)**: Orders e Inventory publican eventos (`OrderConfirmed`, `ProductLowStock`, ...) y Notifications los consume. Se usa un **outbox transaccional**: el evento se guarda en la misma transacción que el cambio y se entrega después, así no se pierde si el broker cae. El consumidor usa un **inbox** para descartar mensajes duplicados, y los envíos fallidos se reintentan y terminan en una cola `_error`.
- **Contratos**: los eventos se identifican por nombre completo (`Commerce.Contracts.*`); cada consumidor declara su propia copia, sin proyectos compartidos entre servicios.

## Ejecutar todo

Requisitos: .NET 10 SDK y Docker.

```bash
docker compose up --build
```

| URL | Qué es |
|---|---|
| http://localhost:8000 | Gateway: `/inventory/**`, `/orders/**`, `/notifications/**` |
| http://localhost:8180 | Keycloak (consola `admin` / `admin`; realm `commerce`) |
| http://localhost:8025 | Mailpit: los correos que envía Notifications |
| http://localhost:15672 | Consola de RabbitMQ (`guest` / `guest`) |
| http://localhost:18888 | Aspire Dashboard: trazas, métricas y logs |

Las tres APIs **no publican sus puertos**: la única entrada es el gateway (`:8000`). Para explorarlas directamente con Swagger (`http://localhost:8080/swagger`, `:8081`, `:8082`; el botón *Authorize* acepta un token de Keycloak) levanta también el archivo de acceso directo: `docker compose -f docker-compose.yml -f docker-compose.direct-access.yml up --build`.

Las APIs corren en `Development`, así que aplican sus migraciones al arrancar e Inventory carga datos de ejemplo. Para trabajar en un solo servicio hay un `docker-compose.yml` dentro de cada carpeta (no los uses a la vez que el de la raíz: comparten puertos).

### Probar el flujo completo

Toda petición entra por el gateway con un token de Keycloak. `admin` administra el catálogo y los clientes; `maria` (rol `customer`) hace órdenes.

```bash
KC=http://localhost:8180/realms/commerce/protocol/openid-connect/token
token() { curl -s -X POST $KC -d grant_type=password -d client_id=commerce-web -d username=$1 -d password=$2 | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4; }
ADMIN=$(token admin admin); MARIA=$(token maria maria)
G=http://localhost:8000

# 1. Un producto de Inventory y un cliente en Orders (el cliente lo crea el admin)
PRODUCT=$(curl -s -H "Authorization: Bearer $MARIA" "$G/inventory/api/v1/products?pageSize=1" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
CUSTOMER=$(curl -s -X POST -H "Authorization: Bearer $ADMIN" -H "Content-Type: application/json" \
  -d '{"name":"Maria Lopez","email":"maria@example.com"}' $G/orders/api/v1/customers | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

# 2. Maria crea y confirma su orden (Orders descuenta el stock en Inventory con su cuenta de servicio)
ORDER=$(curl -s -X POST -H "Authorization: Bearer $MARIA" -H "Content-Type: application/json" \
  -d "{\"customerId\":\"$CUSTOMER\",\"items\":[{\"productId\":\"$PRODUCT\",\"quantity\":1}]}" $G/orders/api/v1/orders | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
curl -X POST -H "Authorization: Bearer $MARIA" $G/orders/api/v1/orders/$ORDER/confirm

# 3. Ver el resultado: correos en http://localhost:8025 y notificaciones guardadas (solo admin)
curl -H "Authorization: Bearer $ADMIN" "$G/notifications/api/v1/notifications?orderId=$ORDER"
```

Si el stock del producto baja de su nivel de reorden, Inventory publica `ProductLowStock` y llega una alerta al correo de operaciones.

## Observabilidad

Los tres servicios exportan por OpenTelemetry (OTLP) a un único **Aspire Dashboard**:

- **Trazas distribuidas**: una petición se sigue de punta a punta, por ejemplo `POST /orders/{id}/confirm` → consultas a SQL Server → llamada HTTP a Inventory → consultas a PostgreSQL → publicación en RabbitMQ → consumo en Notifications. Incluye las consultas de base de datos y el paso por el outbox de MassTransit.
- **Métricas**: ASP.NET Core, HTTP, runtime de .NET, Npgsql y MassTransit.
- **Logs** estructurados de Serilog enviados por OTLP, con el identificador de traza.

Fuera de Docker, los servicios exportan a `localhost:4317` (el dashboard lo publica en ese puerto).

## Pruebas

Cada servicio tiene pruebas unitarias y de integración. Las de integración usan **Testcontainers** con la infraestructura real (PostgreSQL / SQL Server, RabbitMQ y Mailpit), no dobles en memoria; Docker debe estar corriendo.

```bash
cd orders-service && dotnet test
```

Además hay pruebas de **extremo a extremo** de toda la plataforma en [`e2e-tests/`](e2e-tests/README.md): levantan el `docker compose` y recorren los escenarios reales por el gateway con tokens de Keycloak (ciclo de vida de una orden con sus correos, propiedad de las órdenes, alerta de stock bajo, seguridad y una traza única entre servicios). Corren en la CI en cada PR que toque la plataforma.

## Flujo de trabajo y CI

GitHub Flow: `main` siempre desplegable y protegida (solo PR, con CI en verde). Las ramas se nombran `<servicio>/<tipo>-<descripción>` (por ejemplo `orders/feature-create-order`). Cada servicio tiene su workflow en `.github/workflows/` que solo ejecuta sus pasos cuando cambia su carpeta, pero siempre reporta el estado para que los checks requeridos no queden pendientes.

## Decisiones de diseño

- **Monorepo** con servicios autónomos: se comparte `CLAUDE.md` y las herramientas de `.claude/` entre servicios sin acoplar el código.
- **Persistencia políglota**: PostgreSQL y SQL Server a propósito; cada servicio es dueño de su base y nadie más la lee.
- **MassTransit v8** (Apache 2.0): desde la v9 tiene licencia comercial.
- **Compensación síncrona** al confirmar/cancelar órdenes (sin saga): simplificación consciente, documentada en el README de Orders.
- Las imágenes Alpine corren en globalización invariante: el código nunca pide una cultura concreta y las imágenes que usan SQL Server instalan ICU.

## Seguridad

**Keycloak** es el proveedor de identidad (realm `commerce`, importado desde `keycloak/commerce-realm.json`) y emite JWT. El **gateway** rechaza con 401 toda petición sin un token válido, y **cada servicio vuelve a validar el token** (no confían en que venga del gateway): comprueban firma, emisor, audiencia (`commerce-platform`) y expiración, y convierten los roles de Keycloak (`realm_access.roles`) en roles de ASP.NET. Los `/health/*` son públicos.

| Rol | Quién | Puede |
|---|---|---|
| `admin` | `admin` / `admin` | Todo: catálogo, clientes, enviar/entregar órdenes, ver notificaciones |
| `customer` | `maria` / `maria` | Leer el catálogo y crear/ver/confirmar/cancelar **sus propias** órdenes |
| `service` | cliente `orders-service` | Ajustar stock en Inventory (lo usa Orders) |

| Servicio | Lectura | Escritura |
|---|---|---|
| inventory | cualquier usuario autenticado | `admin`; ajustar stock también `service` |
| orders | `customer`: solo sus órdenes; `admin`/`service`: todas | crear/confirmar/cancelar: autenticado (un `customer` solo sobre sus órdenes); enviar/entregar: `admin`; clientes: solo `admin` |
| notifications | solo `admin` | - |

**Cada cliente ve solo sus órdenes**: Orders liga al usuario con su `Customer` por el **email** del token (único en Keycloak y en la tabla de clientes; `maria@example.com` necesita un cliente con ese email, que crea el `admin`). Un `customer` solo lista sus órdenes, y ver, confirmar o cancelar la de otro responde **404** (no 403, para no revelar que existe); crear una orden para otro cliente responde 403, y un usuario sin perfil de cliente no ve nada ni puede ordenar. `admin` y `service` no tienen esta restricción. La regla vive en `OrderAccessPolicy` (capa Application) y se prueba con tests unitarios y de integración. Orders no reenvía el token del usuario a Inventory; usa su propia identidad (`orders-service`, OAuth2 *client credentials*) y cachea el token hasta poco antes de que expire. Así un `customer` puede confirmar su orden sin tener permiso de ajustar stock directamente.

Un token se pide así (también sirve para probar en Swagger o Postman):

```bash
curl -s -X POST http://localhost:8180/realms/commerce/protocol/openid-connect/token \
  -d grant_type=password -d client_id=commerce-web -d username=maria -d password=maria
```

### Simplificaciones conscientes (frente a un entorno de producción)

- Roles gruesos; en producción se usarían *scopes* más finos (`orders:read`, `orders:write`) y permisos por recurso.
- El usuario se liga con su cliente por email; un identificador estable (un atributo de Keycloak con el id del cliente, sincronizado al crearlo) sería más robusto si los usuarios pudieran cambiar su email.
- Keycloak corre en modo desarrollo (base embebida, HTTP); en producción llevaría su propia base de datos, HTTPS y alta disponibilidad. Los secretos del realm (`orders-service-dev-secret`) son de desarrollo: en producción vienen de un almacén de secretos.
- El gateway no limita el número de peticiones ni protege contra abuso.
- Las bases de datos, RabbitMQ y las herramientas (Mailpit, dashboard) publican sus puertos en el compose para trabajar en local; en producción solo el gateway quedaría expuesto, y los secretos y contraseñas vendrían de un almacén de secretos.

