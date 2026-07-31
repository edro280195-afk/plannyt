export const POSTGRES_CONTAINER_NAME = 'plannyt-postgres-1';
export const POSTGRES_SUPERUSER = 'plannyt';
export const POSTGRES_PORT = 5434;
export const E2E_DATABASE_NAME = 'plannyt_e2e';

// Mismo valor de desarrollo ya presente en appsettings.Development.json;
// no es un secreto nuevo ni se usa fuera de contenedores locales.
export const POSTGRES_PASSWORD = 'change-me-development-only';

export const API_HTTP_URL = 'http://localhost:5092';

// 127.0.0.1 explícito, no "localhost": con Docker Desktop en Windows el
// pool de Npgsql resolvía "localhost" a IPv4/IPv6 de forma inconsistente
// entre conexiones del mismo proceso, produciendo "database does not
// exist" o conexiones abortadas de forma intermitente contra el mismo
// Postgres ya migrado. Ver docs/qa/known-limitations.md.
export const E2E_CONNECTION_STRING =
  `Host=127.0.0.1;Port=${POSTGRES_PORT};Database=${E2E_DATABASE_NAME};` +
  `Username=${POSTGRES_SUPERUSER};Password=${POSTGRES_PASSWORD}`;
