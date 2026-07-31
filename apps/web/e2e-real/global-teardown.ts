import { execFileSync } from 'node:child_process';
import { E2E_DATABASE_NAME, POSTGRES_CONTAINER_NAME, POSTGRES_SUPERUSER } from './db.config';

/**
 * Elimina la base `plannyt_e2e` al terminar, para no dejar datos de prueba
 * acumulándose en el PostgreSQL de desarrollo. La API (levantada por
 * webServer) ya fue detenida por Playwright antes de esta función.
 */
export default async function globalTeardown(): Promise<void> {
  if (process.env['KEEP_E2E_DB']) {
    return;
  }
  try {
    runSql(
      `SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${E2E_DATABASE_NAME}' AND pid <> pg_backend_pid();`,
    );
    runSql(`DROP DATABASE IF EXISTS ${E2E_DATABASE_NAME};`);
  } catch (error) {
    console.warn(
      `No se pudo eliminar la base ${E2E_DATABASE_NAME} al finalizar. ` +
        `Elimínala manualmente si vas a repetir la corrida. Detalle: ${(error as Error).message}`,
    );
  }
}

function runSql(statement: string): void {
  execFileSync(
    'docker',
    ['exec', POSTGRES_CONTAINER_NAME, 'psql', '-U', POSTGRES_SUPERUSER, '-d', 'plannyt', '-v', 'ON_ERROR_STOP=1', '-c', statement],
    { stdio: 'inherit' },
  );
}
