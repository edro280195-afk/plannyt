import { BrowserContext, Page, expect, test } from '@playwright/test';
import { AuthPage } from '../../e2e/pages/auth.page';
import { PortalPage } from '../../e2e/pages/portal.page';
import { ProfessionalPage } from '../../e2e/pages/professional.page';
import { retrySubmit, waitForStableDatabase, warmFrontend } from '../support/warm-up';

/**
 * Flujo A (encomienda §17): alta inicial completa contra la API y
 * PostgreSQL reales (ver playwright.real.config.ts). Sin interceptar
 * /api/**; cada paso depende del resultado real del paso anterior.
 */

const PLANNER_EMAIL = 'mariana@armonia.mx';
const PLANNER_PASSWORD = 'UnaClaveSegura2026!';
const CLIENT_EMAIL = 'ana@example.com';
const UUID_PATTERN = '[0-9a-fA-F-]{36}';

test.describe.serial('Flujo A · Alta inicial (API y PostgreSQL reales)', () => {
  let plannerContext: BrowserContext;
  let plannerPage: Page;
  let clientContext: BrowserContext | undefined;
  let clientPage: Page | undefined;

  let clientId = '';
  let eventId = '';
  let invitationUrl = '';
  let invitationToken = '';

  test.beforeAll(async ({ browser }) => {
    await waitForStableDatabase();

    plannerContext = await browser.newContext();
    plannerPage = await plannerContext.newPage();
    plannerPage.on('console', (msg) => {
      if (msg.type() === 'error') {
        console.log(`[console.error] ${msg.text()}`);
      }
    });
    plannerPage.on('response', (response) => {
      if (response.url().includes('/api/') && response.status() >= 400) {
        console.log(`[api ${response.status()}] ${response.request().method()} ${response.url()}`);
      }
    });
    plannerPage.on('requestfailed', (request) => {
      console.log(`[requestfailed] ${request.method()} ${request.url()} ${request.failure()?.errorText}`);
    });

    await warmFrontend(plannerPage);
  });

  test.afterAll(async () => {
    await plannerContext.close();
    await clientContext?.close();
  });

  test('1-2. registra a la planner y crea su organización', async () => {
    const auth = new AuthPage(plannerPage);
    await auth.goToRegister();

    await retrySubmit(
      plannerPage,
      () => auth.registerPlanner(),
      async () => /\/app\/dashboard$/.test(plannerPage.url()),
    );

    await expect(plannerPage).toHaveURL(/\/app\/dashboard$/);
    await expect(plannerPage.getByRole('heading', { name: 'Todo en su lugar.' })).toBeVisible();
    await expect(plannerPage.getByText('Armonía Eventos')).toBeVisible();
  });

  test('3. cierra sesión y vuelve a iniciar sesión', async () => {
    await plannerPage.getByRole('button', { name: 'Cerrar sesión' }).click();
    await expect(plannerPage).toHaveURL(/\/auth\/login$/);

    const auth = new AuthPage(plannerPage);
    await retrySubmit(
      plannerPage,
      () => auth.login(PLANNER_EMAIL, PLANNER_PASSWORD),
      async () => /\/app\/dashboard$/.test(plannerPage.url()),
    );

    await expect(plannerPage).toHaveURL(/\/app\/dashboard$/);
  });

  test('4. revisa el dashboard vacío antes de crear datos', async () => {
    await expect(plannerPage.getByRole('heading', { name: 'Todo en su lugar.' })).toBeVisible();
    await expect(
      plannerPage.getByRole('heading', { name: 'Tu agenda está lista para comenzar' }),
    ).toBeVisible();
    await expect(plannerPage.getByText('Eventos próximos')).toBeVisible();
  });

  test('5. crea un cliente real', async () => {
    const professional = new ProfessionalPage(plannerPage);
    await professional.createClient();

    await expect(plannerPage).toHaveURL(new RegExp(`/app/clients/${UUID_PATTERN}$`));
    clientId = lastUrlSegment(plannerPage);
    await expect(plannerPage.getByRole('heading', { name: 'Ana Martínez' })).toBeVisible();
  });

  test('6. crea un evento real', async () => {
    const professional = new ProfessionalPage(plannerPage);
    await professional.createEvent();

    await expect(plannerPage).toHaveURL(new RegExp(`/app/events/${UUID_PATTERN}$`));
    eventId = lastUrlSegment(plannerPage);
    await expect(plannerPage.getByRole('heading', { name: 'Ana & Carlos' })).toBeVisible();
  });

  test('7. relaciona el cliente con el evento', async () => {
    await plannerPage.goto(`/app/events/${eventId}`);
    await plannerPage.getByRole('button', { name: /^Clientes/ }).click();

    await plannerPage.getByLabel('Cliente').selectOption({ label: 'Ana Martínez' });
    await plannerPage.getByLabel('Relación').selectOption('PrimaryClient');
    await plannerPage.getByLabel('Cliente principal del evento').check();
    await plannerPage.getByRole('button', { name: 'Vincular cliente' }).click();

    await expect(plannerPage.getByText('Ana Martínez')).toBeVisible();
    await expect(plannerPage.getByText('Principal')).toBeVisible();
  });

  test('8. invita al cliente y genera el enlace de acceso', async () => {
    await plannerPage.getByRole('button', { name: /^Accesos/ }).click();
    await plannerPage.getByLabel('Correo objetivo').fill(CLIENT_EMAIL);
    await plannerPage.getByRole('button', { name: 'Generar invitación' }).click();

    const code = plannerPage.locator('.copy-box code');
    await expect(code).toBeVisible();
    invitationUrl = (await code.textContent())?.trim() ?? '';
    expect(invitationUrl).toContain('/accept-access/');
    invitationToken = invitationUrl.split('/accept-access/').pop() ?? '';
    expect(invitationToken).not.toBe('');
  });

  test('9. el cliente acepta el acceso desde una sesión nueva', async ({ browser }) => {
    clientContext = await browser.newContext();
    clientPage = await clientContext.newPage();

    const portal = new PortalPage(clientPage);
    await portal.acceptNewAccount(invitationToken);

    await expect(clientPage).toHaveURL(/\/portal\/events$/);
    await expect(
      clientPage.getByRole('heading', { name: 'Todo lo que necesitas, sin el ruido.' }),
    ).toBeVisible();
  });

  test('10. abre el portal y ve el evento compartido', async () => {
    const page = requirePage(clientPage);
    const portal = new PortalPage(page);
    await portal.openAuthorizedEvent();

    await expect(page).toHaveURL(new RegExp(`/portal/events/${eventId}$`));
    await expect(page.getByRole('heading', { name: 'Ana & Carlos' })).toBeVisible();
  });

  test('11. revoca el acceso desde el área profesional', async () => {
    await plannerPage.goto(`/app/events/${eventId}`);
    await plannerPage.getByRole('button', { name: /^Accesos/ }).click();

    const accessRow = plannerPage.locator('.contact-row', { hasText: CLIENT_EMAIL });
    await expect(accessRow).toBeVisible();

    plannerPage.once('dialog', (dialog) => void dialog.accept());
    await accessRow.getByRole('button', { name: 'Revocar' }).click();

    await expect(accessRow.locator('[data-status="Revoked"]')).toBeVisible();
  });

  test('12. confirma la pérdida inmediata de acceso del cliente', async () => {
    const page = requirePage(clientPage);

    await page.goto('/portal/events');
    await expect(page.getByRole('link', { name: /Ana & Carlos/ })).toHaveCount(0);

    await page.goto(`/portal/events/${eventId}`);
    await expect(page.getByRole('heading', { name: 'Ana & Carlos' })).toHaveCount(0);
  });
});

function lastUrlSegment(page: Page): string {
  const segments = new URL(page.url()).pathname.split('/');
  const value = segments.pop();
  if (!value) {
    throw new Error(`No se pudo extraer un identificador de la URL: ${page.url()}`);
  }
  return value;
}

function requirePage(page: Page | undefined): Page {
  if (!page) {
    throw new Error('Se esperaba una página de cliente ya inicializada en un paso previo.');
  }
  return page;
}
