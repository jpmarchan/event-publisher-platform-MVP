CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001041015_InitialCreate') THEN
    CREATE TABLE notification_logs (
        "Id" uuid NOT NULL,
        "MessageId" uuid NOT NULL,
        "EventId" uuid NOT NULL,
        "EventName" character varying(200) NOT NULL,
        "OccurredAt" timestamp with time zone NOT NULL,
        "CorrelationId" uuid NOT NULL,
        "PayloadHash" character varying(64) NOT NULL,
        "Status" character varying(20) NOT NULL,
        "FailureReason" character varying(2000),
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_notification_logs" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001041015_InitialCreate') THEN
    CREATE UNIQUE INDEX "IX_notification_logs_MessageId" ON notification_logs ("MessageId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001041015_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261001041015_InitialCreate', '10.0.12');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001095955_AddNotificationProcessingState') THEN
    ALTER TABLE notification_logs ADD "Attempts" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001095955_AddNotificationProcessingState') THEN
    ALTER TABLE notification_logs ADD "UpdatedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20261001095955_AddNotificationProcessingState') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20261001095955_AddNotificationProcessingState', '10.0.12');
    END IF;
END $EF$;
COMMIT;

