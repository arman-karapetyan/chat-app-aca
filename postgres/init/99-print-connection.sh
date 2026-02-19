#!/usr/bin/env bash
set -e

if [ "${DEBUG}" = "true" ]; then
  echo ""
  echo "================================================"
  echo " PostgreSQL READY"
  echo ""
  echo " Environment : ${APP_ENVIRONMENT}"
  echo " Container   : ${PG_CONTAINER_NAME}"
  echo " Debug   : ${DEBUG}"
  echo ""
  echo " Connection string = "
  echo " Host=localhost;Port=${PG_PORT:-5432};Database=${POSTGRES_DB};Username=${APP_DB_USER};Password=${APP_DB_PASSWORD}"
  echo ""
  echo "================================================"
  echo ""
fi