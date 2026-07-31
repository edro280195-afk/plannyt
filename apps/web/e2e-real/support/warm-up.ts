import { Page } from '@playwright/test';
import { API_HTTP_URL } from '../db.config';

/**
 * En este entorno (Docker Desktop en Windows) el primer tramo de vida de
 * la API, justo después de migrar una base recién creada, es
 * intermitente: consultas contra `user_accounts` fallan alternando entre
 * "connection aborted" y "database plannyt_e2e does not exist" durante
 * hasta un minuto, incluso con IPv4 explícito y sin ninguna creación
 * externa de la base (ver historial de commits de este archivo y
 * docs/qa/known-limitations.md). Las mismas llamadas contra la API ya
 * estable, en pruebas manuales repetidas con curl directo y a través del
 * proxy, siempre fueron consistentes — no es un defecto de negocio.
 *
 * Se exige aquí estabilidad real: golpear directamente el endpoint de
 * login (ejercita la misma consulta a `user_accounts` que register) hasta
 * lograr `requiredConsecutive` éxitos seguidos, antes de dejar que
 * cualquier prueba dependa de una respuesta correcta.
 */
export async function waitForStableDatabase(
  requiredConsecutive = 4,
  maxTotalWaitMs = 60_000,
): Promise<void> {
  const deadline = Date.now() + maxTotalWaitMs;
  let consecutive = 0;

  while (Date.now() < deadline) {
    const stable = await probesOnce();
    consecutive = stable ? consecutive + 1 : 0;

    if (consecutive >= requiredConsecutive) {
      return;
    }

    await new Promise((resolve) => setTimeout(resolve, 750));
  }

  throw new Error(
    `La base de datos de la API real no se estabilizó en ${maxTotalWaitMs}ms. ` +
      'Ver docs/qa/known-limitations.md (arranque en frío intermitente en Docker Desktop/Windows).',
  );
}

async function probesOnce(): Promise<boolean> {
  try {
    const response = await fetch(`${API_HTTP_URL}/api/auth/login`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Origin: 'http://127.0.0.1:4200' },
      body: JSON.stringify({ email: 'warmup-probe@plannyt-test.invalid', password: 'no-such-password' }),
    });
    // 401 (credenciales inválidas) prueba que la consulta a user_accounts
    // se resolvió correctamente; cualquier otra cosa (500, red) no cuenta.
    return response.status === 401;
  } catch {
    return false;
  }
}

/** Recorre las rutas públicas del Flujo A para que Vite optimice sus dependencias antes de cronometrar nada. */
export async function warmFrontend(page: Page): Promise<void> {
  const routes = ['/auth/login', '/auth/register'];
  for (const route of routes) {
    await page.goto(route, { waitUntil: 'networkidle' }).catch(() => undefined);
    await page.waitForTimeout(500);
  }
}

/**
 * Red de seguridad adicional: repite una acción mutante si la condición de
 * arranque en frío documentada arriba interrumpe la solicitud a pesar de
 * waitForStableDatabase. Repite exactamente la acción del usuario; si el
 * estado de error persiste, la aserción posterior de la prueba falla igual.
 */
export async function retrySubmit(
  page: Page,
  submit: () => Promise<void>,
  isDone: () => Promise<boolean>,
  attempts = 3,
): Promise<void> {
  for (let attempt = 1; attempt <= attempts; attempt += 1) {
    await submit();

    const deadline = Date.now() + 6_000;
    while (Date.now() < deadline) {
      if (await isDone()) {
        return;
      }
      await page.waitForTimeout(250);
    }

    if (await isDone()) {
      return;
    }
  }
}
