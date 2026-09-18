# Gateway

Única entrada HTTP a la plataforma, construida con **YARP** (el proxy inverso de Microsoft). Enruta por prefijo al servicio correspondiente y quita el prefijo antes de reenviar:

| Ruta pública | Servicio | Ejemplo |
|---|---|---|
| `/inventory/**` | inventory-service | `GET /inventory/api/v1/products` |
| `/orders/**` | orders-service | `POST /orders/api/v1/orders` |
| `/notifications/**` | notifications-service | `GET /notifications/api/v1/notifications?orderId=...` |

Las rutas y los destinos viven en `appsettings.json` (`ReverseProxy`) y se sobrescriben por variables de entorno (`ReverseProxy__Clusters__orders__Destinations__primary__Address`), que es como el `docker-compose` los apunta a los contenedores.

Exporta trazas, métricas y logs por OpenTelemetry igual que el resto: la traza de una petición empieza aquí y sigue por los servicios.

## Autenticación

El gateway valida el JWT de Keycloak (firma, emisor, audiencia y expiración) y rechaza con **401** toda petición sin token válido; las rutas `/inventory`, `/orders` y `/notifications` usan la política `authenticated`, y `/health/live` es público. El token se reenvía sin cambios al servicio, que lo valida de nuevo y aplica los roles (el gateway no decide permisos finos).

Configuración (`Authentication` en `appsettings.json`): `Authority` es el emisor que Keycloak escribe en los tokens (`http://localhost:8180/realms/commerce`), `Audience` es `commerce-platform`, y `MetadataAddress` (opcional) es la dirección desde donde se descargan las claves; en Docker apunta a `http://keycloak:8080/...` porque el emisor público y la dirección interna no coinciden.

## CORS y límite de peticiones

- **CORS**: `Cors:AllowedOrigins` (en `appsettings.json` los orígenes típicos de un frontend en desarrollo; en producción se sobrescribe con `Cors__AllowedOrigins__0`, ...). El *preflight* de un origen permitido se responde con 204 sin exigir token; los orígenes desconocidos no reciben cabeceras CORS.
- **Límite de peticiones**: política `per-user` de ventana deslizante (`RateLimiting:PermitLimit`, 600, y `WindowSeconds`, 60). La clave es el `sub` del token, o la IP si no hay token, así que un cliente ruidoso no afecta a los demás. Al superarlo responde `429` con `Retry-After`. Los valores se leen en cada petición. `/health/live` no está limitado.

## Ejecutar

```bash
dotnet run --project src/Gateway.API      # http://localhost:8000; apunta a los servicios en sus puertos locales (5254, 5280, 5290)
```

O con toda la plataforma: `docker compose up --build` desde la raíz (gateway en `http://localhost:8000`). Es la única entrada publicada: los servicios solo son alcanzables dentro de la red de Docker.

## Tests

```bash
dotnet test      # un servidor falso hace de servicio; comprueba el enrutamiento y que se quite el prefijo
```
