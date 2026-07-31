import { expect, test } from '../fixtures/plannyt.fixture';

test('completa el flujo comercial versionado de prospecto a evento preliminar', async ({
  page,
  api,
}) => {
  test.setTimeout(60_000);
  api.useProfile('owner');

  await page.goto('/app/prospects');
  await page.getByRole('button', { name: /Nuevo prospecto/ }).click();
  const prospectDialog = page.getByRole('dialog', { name: 'Registrar prospecto' });
  await prospectDialog.getByLabel('Nombre para mostrar').fill('María Hernández');
  await prospectDialog.getByLabel('Nombre', { exact: true }).fill('María');
  await prospectDialog.getByLabel('Apellidos').fill('Hernández');
  await prospectDialog.getByLabel('Correo').fill('maria@example.com');
  await prospectDialog.getByLabel('Teléfono').fill('+52 899 123 4567');
  await prospectDialog.getByLabel('Tipo de evento').fill('Boda');
  await prospectDialog.getByLabel('Ciudad').fill('Matamoros');
  await prospectDialog.getByRole('button', { name: 'Crear prospecto' }).click();

  await expect(page).toHaveURL(/\/app\/prospects\/prospect-1$/);
  await page.getByLabel('Asunto').fill('Enviar opciones iniciales');
  await page.getByLabel('Detalle').fill('Compartir catálogo de producción.');
  await page.getByRole('button', { name: 'Registrar actividad' }).click();
  await expect(page.getByText(/Enviar opciones iniciales/)).toBeVisible();

  await page.goto('/app/catalog');
  await page.getByRole('button', { name: /Agregar/ }).click();
  await page.getByLabel('Nombre').fill('Producción integral');
  await page.getByLabel('Categoría').fill('Producción');
  await page.getByLabel('Precio base').fill('12500');
  await page.getByRole('button', { name: 'Guardar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Producción integral' })).toBeVisible();

  await page.getByRole('button', { name: /Paquetes/ }).click();
  await page.getByRole('button', { name: /Agregar/ }).click();
  await page.getByLabel('Nombre').fill('Celebración esencial');
  await page.getByLabel('Precio del paquete').fill('12500');
  await page.getByLabel(/Producción integral/).check();
  await page.getByRole('button', { name: 'Guardar', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Celebración esencial' })).toBeVisible();

  await page.goto('/app/prospects/prospect-1');
  await page.getByRole('link', { name: 'Crear propuesta' }).click();
  await page.getByLabel('Elegir servicio').selectOption('service-1');
  await page.getByRole('button', { name: 'Agregar', exact: true }).click();
  await page.getByRole('button', { name: 'Crear borrador' }).click();

  await expect(page).toHaveURL(/\/app\/proposals\/proposal-1$/);
  await page.getByRole('button', { name: 'Publicar versión' }).click();
  await expect(page.getByText('v1')).toBeVisible();
  await page.getByRole('button', { name: 'Generar enlace privado' }).click();
  await expect(page.locator('.share-box input')).toHaveValue(/public-token-1/);

  await page.goto('/proposal/public-token-1');
  await expect(page.getByRole('heading', { name: /hagamos realidad tu evento/ })).toBeVisible();
  const firstDecision = page.locator('.decision-card');
  await firstDecision.getByLabel('Tu nombre').fill('María Hernández');
  await firstDecision.getByLabel('Mensaje opcional').fill('Ajustemos un detalle.');
  await firstDecision.getByRole('button', { name: 'Solicitar cambios' }).click();
  await expect(page.getByText('Cambios solicitados')).toBeVisible();

  await page.goto('/app/proposals/proposal-1');
  await page.getByRole('button', { name: 'Guardar borrador' }).click();
  await page.getByRole('button', { name: 'Publicar nueva versión' }).click();
  await expect(page.getByText('v2')).toBeVisible();
  await page.getByRole('button', { name: 'Generar enlace privado' }).click();
  await expect(page.locator('.share-box input')).toHaveValue(/public-token-2/);

  await page.goto('/proposal/public-token-2');
  const finalDecision = page.locator('.decision-card');
  await finalDecision.getByLabel('Tu nombre').fill('María Hernández');
  await finalDecision.getByRole('button', { name: 'Aceptar propuesta' }).click();
  await expect(page.getByRole('heading', { name: 'Propuesta aceptada' })).toBeVisible();

  // Regresión QA-018: una propuesta aceptada sin evento vinculado no debía
  // poder generar contrato; el botón "Generar contrato" quedaba inalcanzable
  // porque ningún flujo de la UI llamaba al endpoint existente
  // /proposals/{id}/preliminary-event.
  await page.goto('/app/proposals/proposal-1');
  await expect(page.getByText('Sin evento vinculado todavía')).toBeVisible();
  await expect(page.getByRole('link', { name: 'Generar contrato' })).toHaveCount(0);
  await page.getByRole('button', { name: 'Vincular evento preliminar' }).click();
  const proposalEventDialog = page.getByRole('dialog', { name: 'Vincular evento preliminar' });
  await proposalEventDialog.getByLabel('Nombre del evento').fill('Boda de María y Carlos');
  await proposalEventDialog.getByLabel('Tipo').fill('Boda');
  await proposalEventDialog.getByLabel('Fecha estimada').fill('2027-02-14T18:00');
  await proposalEventDialog.getByLabel('Ciudad').fill('Matamoros');
  await proposalEventDialog.getByLabel('Invitados estimados').fill('140');
  await proposalEventDialog.getByRole('button', { name: 'Vincular evento' }).click();
  await expect(page.getByRole('link', { name: 'Generar contrato' })).toBeVisible();
  expect(
    api.requestFor('POST', '/api/organizations/org-1/proposals/proposal-1/preliminary-event'),
  ).toBeDefined();

  await page.goto('/app/prospects/prospect-1');
  await page.getByRole('button', { name: 'Revisar conversión' }).click();
  await page.getByRole('button', { name: 'Confirmar conversión' }).click();
  await expect(page.getByText('Cliente convertido')).toBeVisible();
  await page.getByRole('button', { name: 'Crear evento preliminar' }).click();
  const eventDialog = page.getByRole('dialog');
  await eventDialog.getByLabel('Nombre del evento').fill('Boda de María y Carlos');
  await eventDialog.getByLabel('Tipo').fill('Boda');
  await eventDialog.getByLabel('Fecha estimada').fill('2027-02-14T18:00');
  await eventDialog.getByLabel('Ciudad').fill('Matamoros');
  await eventDialog.getByLabel('Invitados estimados').fill('140');
  await eventDialog.getByRole('button', { name: 'Crear preliminar' }).click();

  await expect(page).toHaveURL(/\/app\/events\/event-preliminary$/);
  expect(
    api.requestFor('POST', '/api/organizations/org-1/prospects/prospect-1/preliminary-event'),
  ).toBeDefined();
});
