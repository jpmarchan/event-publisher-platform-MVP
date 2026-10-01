# Scripts de base de datos

Cada microservicio tiene **su propia base de datos** (`eventservice_db`, `notificationservice_db`); nadie hace JOIN
entre servicios. La fuente de verdad del modelo son las **migraciones de EF Core** de cada `*.Infrastructure`; los
scripts de esta carpeta se generan a partir de ellas para quien prefiera aplicarlos a mano (pgAdmin, DBeaver, `psql`)
o para que el área de Base de Datos los revise antes de cada pase a QA/Staging/Producción.

| Archivo | Qué hace | Idempotente |
| --- | --- | --- |
| `eventservice/01-schema.sql` | Tablas `events`, `zones`, `idempotency_records` y las del outbox de MassTransit (`OutboxMessage`, `OutboxState`, `InboxState`) | Sí (registra cada migración en `__EFMigrationsHistory`) |
| `eventservice/02-seed.sql` | 3 eventos de demo con sus zonas (solo DEV/QA) | Sí (IDs fijos + `ON CONFLICT DO NOTHING`) |
| `notificationservice/01-schema.sql` | Tabla `notification_logs` (índice único en `MessageId`: clave de idempotencia del consumer) | Sí |

La creación de las bases en sí (`eventservice_db`, `notificationservice_db`) la hace
[`infra/postgres/init-multiple-dbs.sh`](../infra/postgres/init-multiple-dbs.sh) al primer arranque del contenedor de Postgres.

## Opción A (por defecto): automático al levantar con Docker

No hay que correr nada. Al arrancar, cada Api aplica sus migraciones (`Database.MigrateAsync()`), y EventService
carga los datos de demo si `Seed__DemoData=true` (así está en `docker-compose.yml`). El seed solo inserta si la tabla
`events` está vacía, así que reiniciar no duplica datos.

## Opción B: a mano con los scripts

```bash
# Esquema + datos de demo de EventService
docker compose exec -T postgres psql -U eventos_app -d eventservice_db < db/eventservice/01-schema.sql
docker compose exec -T postgres psql -U eventos_app -d eventservice_db < db/eventservice/02-seed.sql

# Esquema de NotificationService (no tiene datos iniciales: sus registros nacen al consumir mensajes)
docker compose exec -T postgres psql -U eventos_app -d notificationservice_db < db/notificationservice/01-schema.sql
```

Desde pgAdmin/DBeaver: conectarse a `localhost:5434` (usuario y contraseña en `.env`) y ejecutar los mismos archivos
en ese orden.

## Regenerar los scripts tras una migración nueva

```bash
docker run --rm -v "$(pwd):/src" -w //src mcr.microsoft.com/dotnet/sdk:10.0 bash -c "
  dotnet tool install --global dotnet-ef --version 10.0.12
  export PATH=\"\$PATH:/root/.dotnet/tools\"
  dotnet ef migrations script --idempotent \
    --project src/EventService/EventService.Infrastructure/EventService.Infrastructure.csproj \
    -o db/eventservice/01-schema.sql
  dotnet ef migrations script --idempotent \
    --project src/NotificationService/NotificationService.Infrastructure/NotificationService.Infrastructure.csproj \
    -o db/notificationservice/01-schema.sql
"
```

> En Producción el seed de demo no se usa (`Seed__DemoData` queda en `false`) y las migraciones se aplican como un
> paso del pipeline de despliegue, no al arrancar cada instancia (con varias réplicas competirían entre sí).
