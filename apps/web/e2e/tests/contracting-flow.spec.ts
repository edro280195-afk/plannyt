import { expect, test } from '../fixtures/plannyt.fixture';

test('completa contratación, firmas, anticipo y confirmación del evento', async ({ page, api }) => {
  test.setTimeout(90_000);
  api.useProfile('owner');
  api.prepareContractingScenario();

  await page.goto('/app/proposals/proposal-1');
  await page.getByRole('link', { name: 'Generar contrato' }).click();
  await expect(page).toHaveURL(/\/app\/contracts\?proposalId=proposal-1$/);
  await page.getByRole('button', { name: 'Crear borrador' }).click();
  await expect(page).toHaveURL(/\/app\/contracts\/contract-1$/);

  await page.getByRole('button', { name: 'Publicar versión inmutable' }).click();
  await expect(page.getByText('SHA-256 del documento presentado')).toBeVisible();

  await page.locator('summary').filter({ hasText: 'Agregar firmante' }).click();
  await page.getByLabel('Parte representada').selectOption('party-client');
  await page.getByLabel('Nombre completo').fill('Ana Martínez');
  await page.getByLabel('Correo').fill('ana@example.com');
  await page.getByRole('button', { name: 'Agregar firmante' }).click();

  const clientSigner = page.locator('.signer-card').filter({ hasText: 'Ana Martínez' });
  await clientSigner.getByRole('button', { name: 'Crear enlace' }).click();
  await expect(clientSigner.locator('.copy-box input')).toHaveValue(/signature-token/);

  await page.goto('/sign/signature-token');
  await expect(
    page.locator('.document-title').getByRole('heading', {
      name: 'Contrato de prestación de servicios',
    }),
  ).toBeVisible();
  await page.getByLabel(/Acepto utilizar medios electrónicos/).check();
  await page.getByLabel(/Confirmo que deseo firmar/).check();
  await page.getByRole('button', { name: 'Firmar esta versión' }).click();
  await expect(page.getByText('Respuesta registrada')).toBeVisible();

  api.useProfile('owner');
  await page.goto('/app/contracts/contract-1');
  const plannerSigner = page.locator('.signer-card').filter({ hasText: 'Mariana Torres' });
  page.once('dialog', (dialog) => dialog.accept());
  await plannerSigner.getByRole('button', { name: 'Firmar aquí' }).click();
  await expect(page.getByText('Contrato completado')).toBeVisible();

  await page.goto('/app/events/event-1/contracting');
  await page.getByRole('button', { name: 'Crear plan en borrador' }).click();
  await page.getByRole('button', { name: 'Activar plan' }).click();
  await expect(page.getByText('Anticipo de contratación')).toBeVisible();

  api.useProfile('portal');
  await page.goto('/portal/payments');
  await page.getByRole('button', { name: 'Registrar pago para este plan' }).click();
  await page.getByLabel('Referencia').fill('SPEI-123');
  await page.getByLabel('Nota para la planner').fill('Anticipo del evento');
  await page.getByRole('button', { name: 'Enviar para revisión' }).click();
  await expect(page.getByText('En revisión')).toBeVisible();
  await page.locator('input[type="file"]').setInputFiles({
    name: 'comprobante.png',
    mimeType: 'image/png',
    buffer: Buffer.from(
      'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAusB9Y9ZsL8AAAAASUVORK5CYII=',
      'base64',
    ),
  });
  await expect(page.getByText('✓ comprobante.png')).toBeVisible();

  api.useProfile('owner');
  await page.goto('/app/events/event-1/contracting');
  await page.getByRole('button', { name: 'Aprobar y asignar' }).click();
  await expect(page.getByText('Listo para confirmar')).toBeVisible();
  page.once('dialog', (dialog) => dialog.accept());
  await page.getByRole('button', { name: 'Confirmar evento' }).click();
  await expect(
    page.locator('.notice--success').getByText('Evento confirmado', { exact: true }),
  ).toBeVisible();
  await expect(page.getByText('Confirmado', { exact: true })).toBeVisible();
});

test('permite rechazar expresamente una firma pública', async ({ page, api }) => {
  api.prepareContractingScenario();
  await page.goto('/sign/rejected-token');

  page.once('dialog', (dialog) => dialog.accept('No estoy de acuerdo'));
  await page.getByRole('button', { name: 'No acepto el contrato' }).click();

  await expect(page.getByText('Respuesta registrada')).toBeVisible();
});

test('mantiene el evento preliminar mientras falten contrato y anticipo', async ({ page, api }) => {
  api.useProfile('owner');
  api.prepareContractingScenario();

  await page.goto('/app/events/event-1/contracting');

  await expect(page.getByText('Aún faltan requisitos')).toBeVisible();
  await expect(page.getByText(/Contrato completado/)).toBeVisible();
  await expect(page.getByRole('button', { name: 'Confirmar evento' })).toHaveCount(0);
});
