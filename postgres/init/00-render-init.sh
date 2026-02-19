#!/usr/bin/env bash
set -e

echo "Rendering and executing SQL init template..."

SRC="/docker-entrypoint-initdb.d/01-init.sql.template"
TMP_SQL="/tmp/01-init.sql"

: "${APP_DB_USER:?APP_DB_USER is not set}"
: "${APP_DB_PASSWORD:?APP_DB_PASSWORD is not set}"

sed \
  -e "s|\${APP_DB_USER}|${APP_DB_USER}|g" \
  -e "s|\${APP_DB_PASSWORD}|${APP_DB_PASSWORD}|g" \
  "$SRC" > "$TMP_SQL"
  
psql \
  --username "$POSTGRES_USER" \
  --dbname "$POSTGRES_DB" \
  --file "$TMP_SQL"
  
echo "Dynamic SQL executed successfully!"