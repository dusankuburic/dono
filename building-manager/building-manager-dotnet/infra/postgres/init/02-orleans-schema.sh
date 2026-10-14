#!/bin/sh
set -eu
ORLEANS_SQL_DIR=/orleans-sql PGUSER="$POSTGRES_USER" sh /orleans-sql/apply.sh
