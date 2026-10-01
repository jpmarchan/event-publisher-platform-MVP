#!/bin/bash
# Una base de datos por microservicio.
set -e

create_database() {
	local database=$1
	echo "Creando base de datos '$database'"
	psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" <<-EOSQL
	    SELECT 'CREATE DATABASE "$database"'
	    WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$database')\gexec
EOSQL
}

create_database "$EVENTSERVICE_DB"
create_database "$NOTIFICATIONSERVICE_DB"
