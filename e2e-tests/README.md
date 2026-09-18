# End-to-end tests

Pruebas de **caja negra** de toda la plataforma: no conocen el código de los servicios, solo hablan HTTP con lo que un cliente real vería: el gateway (`:8000`), Keycloak (`:8180`), Mailpit (`:8025`, los correos) y el Aspire Dashboard (`:18888`, las trazas).

## Qué comprueban

| Escenario | Qué recorre |
|---|---|
| Ciclo de vida de una orden | Crear → confirmar (el stock baja en Inventory, con la identidad de servicio de Orders) → enviar y entregar (solo `admin`) → un correo por paso al cliente y 4 notificaciones registradas |
| Cancelar una orden confirmada | El stock vuelve a Inventory y el cliente recibe el correo de cancelación |
| Stock insuficiente | La confirmación se rechaza (409) y el stock queda intacto |
| Propiedad de las órdenes | Un `customer` solo ve y toca las suyas (404 para las ajenas, 403 al crear una para otro); `admin` ve todas |
| Alerta de stock bajo | Inventory → RabbitMQ → Notifications → correo de operaciones |
| Seguridad | Sin token 401 en todas las rutas; un `customer` no puede usar funciones de `admin`; los puertos de los servicios no están publicados |
| Observabilidad | Una sola traza cruza gateway, Orders, Inventory y Notifications (incluido el salto por RabbitMQ) |

Cada prueba crea su propio producto, así que no depende de los datos de ejemplo ni de las demás pruebas.

## Ejecutar

Requisitos: .NET 10 SDK y Docker, **con los puertos de la plataforma libres** (detén tu entorno de desarrollo antes: usa los mismos puertos).

```bash
dotnet test        # levanta la plataforma con docker compose (proyecto commerce-e2e), corre los escenarios y la baja con `down -v`
```

El primer arranque construye las imágenes y tarda unos minutos.

| Variable | Efecto |
|---|---|
| `E2E_USE_RUNNING_PLATFORM=1` | Reutiliza una plataforma que ya está corriendo (no la levanta ni la baja): para iterar rápido sobre las pruebas |
| `E2E_DIRECT_ACCESS=1` | Omite la comprobación de que los puertos de los servicios están cerrados (si usas `docker-compose.direct-access.yml`) |
| `E2E_LOG_FILE=<ruta>` | Guarda los logs de todos los contenedores antes de bajarlos (así los sube la CI) |
| `E2E_GATEWAY_URL`, `E2E_KEYCLOAK_URL`, `E2E_MAILPIT_URL`, `E2E_DASHBOARD_URL` | Otras direcciones, si la plataforma no está en localhost |

## CI

`.github/workflows/e2e-ci.yml` corre estas pruebas en cada PR que toque algún servicio, el gateway, Keycloak, el compose o estas pruebas (con el mismo esquema que los demás workflows: si el PR no las afecta, el job termina de inmediato en verde). Sube los logs de la plataforma como artefacto.
