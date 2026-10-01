# Plataforma de Eventos

MVP de una plataforma de eventos online: dos microservicios .NET (**EventService** y **NotificationService**)
comunicados de forma asíncrona por un broker de mensajes, con Clean Architecture/DDD, CQRS ligero, autenticación
OIDC/OAuth 2.0 y un frontend React para registrar eventos.

| Documento | Contenido |
| --- | --- |
| [docs/architecture.md](docs/architecture.md) | Arquitectura objetivo de la plataforma completa (microservicios, bases de datos, comunicación síncrona/asíncrona, sobreventa, AWS) y detalle del MVP |
| [docs/backlog-roadmap.md](docs/backlog-roadmap.md) · [.xlsx](docs/backlog-roadmap.xlsx) | Backlog por épicas y roadmap de 6 meses (Scrum, sprints de 2 semanas) |
| [db/README.md](db/README.md) | Modelo de datos, migraciones, scripts SQL y datos iniciales |

## Stack

| Capa | Tecnología |
| --- | --- |
| Backend | .NET 10 (LTS), minimal APIs, EF Core 10, MediatR, FluentValidation, MassTransit, Polly, MailKit |
| Mensajería | Amazon SNS + SQS (emulados con LocalStack) |
| Persistencia | PostgreSQL (una base por servicio), Redis (cache) |
| Identidad | Keycloak (OIDC / OAuth 2.0) |
| Observabilidad | Serilog, OpenTelemetry + Jaeger, health checks |
| Frontend | React 18, TypeScript, Vite, Tailwind CSS, oidc-client-ts |
| Infraestructura local | Docker Compose |

## Cómo levantarlo

Requisito: **Docker Desktop** en ejecución. No hace falta el SDK de .NET, Node ni una cuenta de AWS.

```bash
cp .env.example .env          # en PowerShell: copy .env.example .env
docker compose up -d --build
docker compose ps             # esperar a que postgres, redis, localstack y keycloak estén "healthy"
```

Al arrancar, cada Api aplica sus migraciones y EventService carga 3 eventos de ejemplo.
Para apagar: `docker compose down` (conserva los datos) o `docker compose down -v` (empieza de cero).

| Servicio | URL |
| --- | --- |
| Frontend (Registrar Evento) | http://localhost:5173 |
| EventService API (Swagger) | http://localhost:5101/swagger |
| NotificationService API (Swagger) | http://localhost:5102/swagger |
| Keycloak | http://localhost:8180 |
| MailHog (correos enviados) | http://localhost:8025 |
| Jaeger (trazas distribuidas) | http://localhost:16686 |
| Health checks | http://localhost:5101/health/ready · http://localhost:5102/health/ready |
| PostgreSQL / Redis / LocalStack | `localhost:5434` · `localhost:6381` · `localhost:4566` |

### Credenciales (solo desarrollo local)

| Uso | Usuario / cliente | Contraseña / secreto |
| --- | --- | --- |
| Login en la app y en Swagger — puede **crear** eventos | `admin` | `Admin123!` |
| Login en la app y en Swagger — solo **consulta** eventos | `user` | `User123!` |
| Consola de Keycloak | `admin` | `KEYCLOAK_ADMIN_PASSWORD` del `.env` |
| PostgreSQL (`eventservice_db`, `notificationservice_db`) | `eventos_app` | `POSTGRES_PASSWORD` del `.env` |
| Cliente máquina a máquina (consulta `GET /notifications`) | `ops-monitor` | `ops-monitor-dev-secret` |
| Cliente de la prueba de carga | `load-tester` | `load-tester-dev-secret` |

## Recorrido rápido

1. Abrir el **frontend** e iniciar sesión con `admin`. El login ocurre en Keycloak (Authorization Code + PKCE).
2. Crear un evento con una o más zonas. EventService lo guarda junto con el mensaje `EventCreated` en una sola
   transacción (outbox) y lo publica en SNS.
3. NotificationService consume el mensaje desde SQS, registra la notificación y envía el correo: verlo en **MailHog**.
4. En **Jaeger** (servicio `eventservice-api` → *Find Traces*), la traza del `POST /events` recorre ambos servicios.
5. Iniciar sesión con `user`: puede consultar eventos pero no crearlos (la Api responde 403).

## Estructura del repositorio

```
src/
  Shared.Contracts/                  Contrato del mensaje EventCreated
  EventService/
    EventService.Domain/             Agregado Event (con sus zonas) e invariantes
    EventService.Application/        Casos de uso (CQRS con MediatR), puertos, idempotencia
    EventService.Infrastructure/     EF Core + outbox, cache (HybridCache/Redis), MassTransit, seed
    EventService.Api/                Minimal API, autenticación/autorización, rate limiting, observabilidad
  NotificationService/
    NotificationService.Domain/      NotificationLog (Processing → Sent / Failed)
    NotificationService.Application/ Procesamiento idempotente de EventCreated y registro de fallas
    NotificationService.Infrastructure/  EF Core, consumers de MassTransit, MailKit
    NotificationService.Api/         GET /notifications, health checks, observabilidad
frontend/event-registration/         React + Vite + TypeScript + Tailwind
db/                                  Scripts SQL de esquema y datos iniciales
infra/                               Realm de Keycloak e inicialización de PostgreSQL
tests/load/                          Prueba de carga y concurrencia (k6)
docs/                                Arquitectura, backlog y roadmap
docker-compose.yml                   Entorno local completo
Directory.Build.props / Directory.Packages.props   Target framework y versiones NuGet centralizadas
```

Cada servicio sigue Clean Architecture: `Domain` no depende de nada, `Application` define los puertos
(repositorios, publicador, cache) e `Infrastructure` los implementa.

## API

| Endpoint | Servicio | Autorización | Descripción |
| --- | --- | --- | --- |
| `POST /events` | EventService | scope `events:write` + rol `Admin` | Crea un evento con sus zonas y publica `EventCreated`. Acepta el header `Idempotency-Key` |
| `GET /events` | EventService | scope `events:read` | Lista los eventos (con cache) |
| `GET /events/{id}` | EventService | scope `events:read` | Detalle de un evento |
| `GET /notifications` | NotificationService | scope `notifications:read` | Registro de notificaciones (estado, intentos, motivo de falla) |
| `GET /health/live`, `/health/ready` | Ambos | Anónimo | Liveness y readiness |

Mensaje `EventCreated` publicado en el broker:

```json
{ "messageId": "uuid", "eventId": "uuid", "name": "string", "occurredAt": "ISO-8601", "correlationId": "uuid", "version": 1 }
```

## Autenticación y autorización

Las Apis son *resource servers* OAuth 2.0: no emiten tokens, validan los JWT que emite **Keycloak** (firma RS256
contra el JWKS publicado por el IdP, `iss`, `aud` y expiración). La configuración del IdP — roles, clientes, scopes y
usuarios — está versionada en [`infra/keycloak/plataforma-eventos-realm.json`](infra/keycloak/plataforma-eventos-realm.json).

| Cliente | Tipo | Flujo | Uso |
| --- | --- | --- | --- |
| `event-registration-spa` | Público | Authorization Code + PKCE | Frontend React |
| `swagger-ui` | Público | Authorization Code + PKCE | Botón *Authorize* de Swagger UI |
| `ops-monitor` | Confidencial | Client Credentials | `GET /notifications` |
| `load-tester` | Confidencial | Client Credentials | Prueba de carga k6 |

La autorización combina el **scope** (qué puede hacer la aplicación cliente) con el **rol** (qué puede hacer el
usuario). Además: errores sin detalles internos, rate limiting por usuario, CORS restringido, Redis con contraseña y
las Apis corren sin privilegios de root.

Para obtener un token de servicio y consultar NotificationService (bash):

```bash
OPS_TOKEN=$(curl -s -X POST http://localhost:8180/realms/plataforma-eventos/protocol/openid-connect/token \
  -d grant_type=client_credentials -d client_id=ops-monitor -d client_secret=ops-monitor-dev-secret \
  | python3 -c "import sys,json;print(json.load(sys.stdin)['access_token'])")

curl http://localhost:5102/notifications -H "Authorization: Bearer $OPS_TOKEN"
```

Para EventService lo más simple es Swagger UI: *Authorize* → marcar los scopes → login con `admin`.

## Mensajería

EventService publica `EventCreated` en un tópico **SNS** y NotificationService lo consume desde su propia cola
**SQS** suscrita al tópico (fan-out: otros servicios podrían suscribirse sin cambiar el publicador). MassTransit crea
la topología al arrancar: tópico `Shared_Contracts-EventCreated`, colas `EventCreated` y `EventCreatedFault`, y la DLQ
`EventCreated_error`. Para usar AWS real basta con quitar `Aws:ServiceUrl` y usar credenciales de IAM.

- **Outbox transaccional**: el mensaje se guarda en la misma transacción que el evento y se entrega en segundo plano.
- **Consumidor idempotente**: `messageId` único; el mensaje se reclama antes de enviar el correo y un duplicado se ignora.
- **Reintentos** con backoff exponencial, **DLQ** y estado `Failed` con el motivo; **kill switch** que pausa el
  consumo si falla la mayoría de los mensajes.

Inspeccionar colas y tópicos sin instalar el AWS CLI:

```bash
docker run --rm --network plataforma-eventos_default \
  -e AWS_ACCESS_KEY_ID=test -e AWS_SECRET_ACCESS_KEY=test -e AWS_DEFAULT_REGION=us-east-1 \
  amazon/aws-cli --endpoint-url=http://localstack:4566 sqs list-queues
```

## Base de datos

Cada servicio tiene su propia base en PostgreSQL. El modelo se define con migraciones de EF Core, que se aplican
solas al arrancar; EventService además carga datos de ejemplo si `Seed__DemoData=true` (activado en el compose, solo
inserta si la tabla está vacía). Los mismos scripts en SQL, para aplicarlos a mano:

```bash
docker compose exec -T postgres psql -U eventos_app -d eventservice_db < db/eventservice/01-schema.sql
docker compose exec -T postgres psql -U eventos_app -d eventservice_db < db/eventservice/02-seed.sql
docker compose exec -T postgres psql -U eventos_app -d notificationservice_db < db/notificationservice/01-schema.sql
```

Cómo crear migraciones nuevas y regenerar los scripts: [db/README.md](db/README.md).

## Resiliencia y alta concurrencia

| Patrón | Dónde | Qué evita |
| --- | --- | --- |
| Transactional Outbox | EventService | Evento guardado sin su mensaje (o al revés) si el broker falla |
| Idempotency-Key en `POST /events` | EventService | Eventos duplicados por reintentos o doble clic, incluso concurrentes |
| Consumidor idempotente | NotificationService | Correos duplicados ante reentregas del broker |
| Reintentos con backoff + DLQ | NotificationService | Perder mensajes por fallos transitorios |
| Kill switch (circuit breaker del consumidor) | NotificationService | Mandar toda la cola a la DLQ durante una caída del proveedor de correo |
| Bulkhead (8 mensajes en paralelo) | NotificationService | Saturar SMTP o la base al drenar una cola acumulada |
| Circuit breaker + timeout sobre Redis (Polly) | EventService | Que una caída de Redis tumbe la Api: se lee de PostgreSQL |
| HybridCache (memoria + Redis, anti-stampede) | EventService | Que muchas requests simultáneas golpeen la base al expirar el cache |
| Reintento de errores transitorios de PostgreSQL | Ambos | Fallar por un corte breve o un failover |
| Rate limiting por usuario + `Retry-After` | EventService | Que un cliente agote la capacidad de todos |
| Timeouts explícitos (SMTP, Redis) | Ambos | Operaciones colgadas indefinidamente |
| Health checks (`live` / `ready`) | Ambos | Tráfico hacia una instancia sin base de datos |

Escenarios para verlo funcionar (`OPS_TOKEN` como en la sección de autenticación):

```bash
# Broker caído: la Api sigue aceptando eventos; el outbox los entrega al volver.
docker compose pause localstack     # crear un evento -> 201
docker compose unpause localstack   # el correo llega en 1-2 s

# Redis caído: el listado sigue respondiendo desde PostgreSQL; /health/ready informa "Degraded".
docker compose stop redis
docker compose start redis          # el circuito se cierra solo a los ~30 s

# Proveedor de correo caído: ~1 min de reintentos, luego DLQ y estado "Failed" con el motivo.
docker compose stop mailhog         # crear un evento, esperar ~1 min, consultar GET /notifications
docker compose start mailhog
```

Nota: en LocalStack Community, `stop`/`start` borra tópicos y colas; para simular la caída del broker usar
`pause`/`unpause`.

### Prueba de carga (k6)

```bash
docker compose --profile loadtest run --rm k6
```

[`tests/load/events-load.js`](tests/load/events-load.js) ejecuta dos escenarios con umbrales:

- **Lecturas**: 50 usuarios virtuales sobre `GET /events` durante 30 s (referencia en una laptop: ~5.500 req/s,
  p95 ≈ 20 ms, 0 errores).
- **Ráfaga idempotente**: 15 `POST /events` simultáneos con la misma `Idempotency-Key` → 1 evento creado, 9
  respuestas repetidas y 5 rechazos `429` con `Retry-After` (límite de 10 por minuto por usuario; esperar 1 minuto
  entre ejecuciones).

## Observabilidad

- **Correlation ID**: cada request lleva `X-Correlation-Id` (se respeta el del cliente o se genera uno). Se devuelve
  en la respuesta, aparece en cada línea de log de ambos servicios y viaja dentro del mensaje.
- **Trazas distribuidas**: ambas Apis exportan OpenTelemetry a Jaeger; una misma traza cubre la request HTTP, el SQL,
  la publicación en SNS y el procesamiento en NotificationService.
- **Health checks** y **`GET /notifications`** para el estado de dependencias y de cada notificación.

```bash
docker compose logs eventservice-api notificationservice-api | grep <correlation-id>
```

## Desarrollo local

- **Frontend sin Docker**: `cd frontend/event-registration && npm install && npm run dev` (usa
  `VITE_EVENT_SERVICE_URL` y `VITE_OIDC_AUTHORITY`, con valores por defecto para el entorno local).
- **Backend**: la solución es `plataforma-eventos.slnx`. El target framework y las versiones de NuGet están
  centralizados en `Directory.Build.props` y `Directory.Packages.props`.
- **Dependencias**: MediatR se mantiene en 12.x y MassTransit en 8.x, sus últimas versiones open source (las
  siguientes mayores tienen licencia comercial). `dotnet list package --vulnerable` y `npm audit` no reportan
  vulnerabilidades.
