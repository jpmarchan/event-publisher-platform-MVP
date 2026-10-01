-- Datos de demo (DEV/QA). Idempotente. Requiere 01-schema.sql.

BEGIN;

INSERT INTO events ("Id", "Name", "Date", "Venue", "Status") VALUES
    ('a1b2c3d4-0001-4000-8000-000000000001', 'Festival de Rock Lima',
        date_trunc('day', now() AT TIME ZONE 'UTC') AT TIME ZONE 'UTC' + interval '60 days 20 hours', 'Estadio Nacional', 'Published'),
    ('a1b2c3d4-0002-4000-8000-000000000002', 'Conferencia .NET Latam',
        date_trunc('day', now() AT TIME ZONE 'UTC') AT TIME ZONE 'UTC' + interval '30 days 9 hours', 'Centro de Convenciones', 'Published'),
    ('a1b2c3d4-0003-4000-8000-000000000003', 'Stand-up Comedy Night',
        date_trunc('day', now() AT TIME ZONE 'UTC') AT TIME ZONE 'UTC' + interval '14 days 21 hours', 'Teatro Municipal', 'Published')
ON CONFLICT ("Id") DO NOTHING;

INSERT INTO zones ("Id", "EventId", "Name", "Price", "Capacity") VALUES
    ('b1b2c3d4-0001-4000-8000-000000000001', 'a1b2c3d4-0001-4000-8000-000000000001', 'General',      150.00, 20000),
    ('b1b2c3d4-0002-4000-8000-000000000002', 'a1b2c3d4-0001-4000-8000-000000000001', 'Preferencial', 320.00,  5000),
    ('b1b2c3d4-0003-4000-8000-000000000003', 'a1b2c3d4-0001-4000-8000-000000000001', 'VIP',          650.00,   800),
    ('b1b2c3d4-0004-4000-8000-000000000004', 'a1b2c3d4-0002-4000-8000-000000000002', 'Asistente',     80.00,  1200),
    ('b1b2c3d4-0005-4000-8000-000000000005', 'a1b2c3d4-0002-4000-8000-000000000002', 'Workshop',     200.00,   150),
    ('b1b2c3d4-0006-4000-8000-000000000006', 'a1b2c3d4-0003-4000-8000-000000000003', 'Platea',        90.00,   400),
    ('b1b2c3d4-0007-4000-8000-000000000007', 'a1b2c3d4-0003-4000-8000-000000000003', 'Mezanine',      60.00,   250)
ON CONFLICT ("Id") DO NOTHING;

COMMIT;
