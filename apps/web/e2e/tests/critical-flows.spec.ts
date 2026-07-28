import { AuthPage } from '../pages/auth.page';
import { PortalPage } from '../pages/portal.page';
import { ProfessionalPage } from '../pages/professional.page';
import { expect, test } from '../fixtures/plannyt.fixture';

test.describe('flujos críticos de Plannyt', () => {
  test('registra a la planner y crea su espacio', async ({ page, api }) => {
    const auth = new AuthPage(page);
    await auth.goToRegister();

    await auth.registerPlanner();

    await expect(page).toHaveURL(/\/app\/dashboard$/);
    await expect(page.getByRole('heading', { name: 'Todo en su lugar.' })).toBeVisible();
    expect(api.requestFor('POST', '/api/auth/register-planner')?.body).toMatchObject({
      firstName: 'Mariana',
      lastName: 'Torres',
      email: 'mariana@armonia.mx',
      organizationName: 'Armonía Eventos',
    });
  });

  test('inicia sesión y recupera el contexto profesional', async ({ page, api }) => {
    const auth = new AuthPage(page);
    await auth.goToLogin();

    await auth.login('mariana@armonia.mx', 'UnaClaveSegura2026!');

    await expect(page).toHaveURL(/\/app\/dashboard$/);
    await expect(page.getByText('Armonía Eventos')).toBeVisible();
    expect(api.requestFor('POST', '/api/auth/login')?.body).toMatchObject({
      email: 'mariana@armonia.mx',
      isPersistent: true,
    });
  });

  test('redirige una sesión expirada conservando el destino', async ({ page }) => {
    await page.goto('/app/events');

    await expect(page).toHaveURL(/\/auth\/login\?returnUrl=%2Fapp%2Fevents$/);
    await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();
  });

  test('da de alta un cliente', async ({ page, api }) => {
    api.useProfile('owner');
    const professional = new ProfessionalPage(page);

    await professional.createClient();

    await expect(page).toHaveURL(/\/app\/clients\/client-1$/);
    await expect(page.getByRole('heading', { name: 'Ana Martínez' })).toBeVisible();
    expect(api.requestFor('POST', '/api/organizations/org-1/clients')?.body).toMatchObject({
      clientType: 'Person',
      displayName: 'Ana Martínez',
      source: 'Recomendación',
    });
  });

  test('crea un evento desde el área profesional', async ({ page, api }) => {
    api.useProfile('owner');
    const professional = new ProfessionalPage(page);

    await professional.createEvent();

    await expect(page).toHaveURL(/\/app\/events\/event-1$/);
    await expect(page.getByRole('heading', { name: 'Ana & Carlos' })).toBeVisible();
    expect(api.requestFor('POST', '/api/organizations/org-1/events')?.body).toMatchObject({
      name: 'Ana & Carlos',
      eventType: 'Boda',
      city: 'Monterrey',
      estimatedGuestCount: 180,
    });
  });

  test('genera una invitación de cliente para copiarla una sola vez', async ({ page, api }) => {
    api.useProfile('owner');
    const professional = new ProfessionalPage(page);

    await professional.inviteClient();

    await expect(page.getByText('http://127.0.0.1:4200/accept-access/client-token')).toBeVisible();
    expect(
      api.requestFor('POST', '/api/organizations/org-1/events/event-1/access/invitations')?.body,
    ).toEqual({
      targetEmail: 'ana@example.com',
      intendedEventRole: 'ClientPrimary',
    });
  });

  test('acepta una invitación y entra al portal con una cuenta nueva', async ({ page, api }) => {
    const portal = new PortalPage(page);

    await portal.acceptNewAccount('client-token');

    await expect(page).toHaveURL(/\/portal\/events$/);
    await expect(
      page.getByRole('heading', {
        name: 'Todo lo que necesitas, sin el ruido.',
      }),
    ).toBeVisible();
    expect(
      api.requestFor('POST', '/api/access-invitations/client-token/register-and-accept')?.body,
    ).toMatchObject({
      firstName: 'Ana',
      lastName: 'Martínez',
    });
  });

  test('navega únicamente por información compartida en el portal', async ({ page, api }) => {
    api.useProfile('portal');
    const portal = new PortalPage(page);

    await portal.openAuthorizedEvent();

    await expect(page).toHaveURL(/\/portal\/events\/event-1$/);
    await expect(page.getByRole('heading', { name: 'Ana & Carlos' })).toBeVisible();
    await expect(page.getByText('Protagonista del evento')).toBeVisible();
    await expect(page.getByText('programa.pdf')).toBeVisible();
    await expect(page.getByText('Planning')).toHaveCount(0);
    await expect(page.getByText('user-1')).toHaveCount(0);
  });

  test('oculta navegación y acciones sin permisos efectivos', async ({ page, api }) => {
    api.useProfile('limited');

    await page.goto('/app/events');

    await expect(page.getByRole('heading', { name: 'Eventos', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: /Nuevo evento/ })).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Clientes/ })).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Equipo/ })).toHaveCount(0);
    await expect(page.getByRole('link', { name: /Configuración/ })).toHaveCount(0);
  });
});
