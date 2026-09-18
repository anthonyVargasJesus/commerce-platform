# Gateway

Única entrada HTTP a la plataforma, construida con **YARP** (el proxy inverso de Microsoft). Enruta por prefijo al servicio correspondiente y quita el prefijo antes de reenviar:

| Ruta pública | Servicio | Ejemplo |
|---|---|---|
| `/inventory/**` | inventory-service | `GET /inventory/api/v1/products` |
| `/orders/**` | orders-service | `POST /orders/api/v1/orders` |
| `/notifications/**` | notifications-service | `GET /notifications/api/v1/notifications?orderId=...` |

Las rutas y los destinos viven en `appsettings.json` (`ReverseProxy`) y se sobrescriben por variables de entorno (`ReverseProxy__Clusters__orders__Destinations__primary__Address`), que es como el `docker-compose` los apunta a los contenedores.

Exporta trazas, métricas y logs por OpenTelemetry igual que el resto: la traza de una petición empieza aquí y sigue por los servicios.

## Estado

Por ahora solo enruta. La autenticación (validar el JWT que emite Keycloak y aplicar roles) llega en los siguientes cambios.

## Ejecutar

```bash
dotnet run --project src/Gateway.API      # http://localhost:8000; apunta a los servicios en sus puertos locales (5254, 5280, 5290)
```

O con toda la plataforma: `docker compose up --build` desde la raíz (gateway en `http://localhost:8000`).

## Tests

```bash
dotnet test      # un servidor falso hace de servicio; comprueba el enrutamiento y que se quite el prefijo
```
