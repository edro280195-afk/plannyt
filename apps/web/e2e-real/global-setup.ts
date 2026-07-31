import { execFileSync } from 'node:child_process';
import { E2E_DATABASE_NAME, POSTGRES_CONTAINER_NAME, POSTGRES_SUPERUSER } from './db.config';

/**
 * Las pruebas en e2e-real/ corren contra la API y PostgreSQL reales, sin
 * interceptar /api/**. Reutilizan el contenedor de desarrollo ya existente
 * (docker-compose.yml, servicio postgres) y aíslan los datos en una base
 * `plannyt_e2e` separada de `plannyt`, para no tocar datos de desarrollo o
 * del seed demo. Esta base se recrea vacía en cada corrida.
 */
export default async function globalSetup(): Promise<void> {
  try {
    execFileSync('docker', ['exec', POSTGRES_CONTAINER_NAME, 'pg_isready', '-U', POSTGRES_SUPERUSER], {
      stdio: 'pipe',
    });
  } catch (error) {
    throw new Error(
      `No se pudo alcanzar el contenedor "${POSTGRES_CONTAINER_NAME}". ` +
        'Levanta PostgreSQL antes de correr las pruebas reales: docker compose up -d postgres. ' +
        `Detalle: ${(error as Error).message}`,
    );
  }

  // Solo se garantiza que NO exista. La propia API la crea y migra al
  // arrancar (Database__MigrateOnStartup=true → DatabaseInitializer →
  // MigrateAsync, que autocrea la base si falta). Crearla también aquí
  // producía una carrera real entre este script y el primer intento de
  // conexión de la API: en varias corridas, `user_accounts` aparecía
  // creada en el log de migración pero la base terminaba sin tablas, y el
  // log de PostgreSQL mostraba "database plannyt_e2e does not exist"
  // seguido de "relation user_accounts does not exist" en la misma
  // ejecución. Dejar la creación exclusivamente a la API eliminó la
  // carrera. Ver docs/qa/known-limitations.md.
  runSql(
    'plannyt',
    `SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${E2E_DATABASE_NAME}' AND pid <> pg_backend_pid();`,
  );
  runSql('plannyt', `DROP DATABASE IF EXISTS ${E2E_DATABASE_NAME};`);
}

function runSql(database: string, statement: string): void {
  execFileSync(
    'docker',
    ['exec', POSTGRES_CONTAINER_NAME, 'psql', '-U', POSTGRES_SUPERUSER, '-d', database, '-v', 'ON_ERROR_STOP=1', '-c', statement],
    { stdio: 'inherit' },
  );
}
