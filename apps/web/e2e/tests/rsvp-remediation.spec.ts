import { Page, Route } from '@playwright/test';
import { expect, test } from '../fixtures/plannyt.fixture';

test.describe('Sprint 2B.2 · remediación RSVP', () => {
  test.describe.configure({ timeout: 60_000 });

  test('envío normal y doble clic conservan una sola operación con llave de cliente', async ({
    page,
  }) => {
    const submissions: RecordedSubmission[] = [];
    await installPublicRsvpMock(page, {
      submit: async (route) => {
        submissions.push(recordSubmission(route));
        await new Promise((resolve) => setTimeout(resolve, 150));
        await json(route, submissionResponse(1));
      },
    });
    await openCompletedWizard(page, 'normal-token');

    await page.getByRole('button', { name: 'Enviar respuesta' }).dblclick();

    await expect(page.getByRole('heading', { name: '¡Respuesta registrada!' })).toBeVisible();
    expect(submissions).toHaveLength(1);
    expect(submissions[0]?.idempotencyKey).toBeTruthy();
    expect(submissions[0]?.body.expectedRevision).toBe(0);
  });

  test('reintento tras timeout reutiliza la llave; conflicto 409 recarga la revisión reciente', async ({
    page,
  }) => {
    const submissions: RecordedSubmission[] = [];
    let submitCount = 0;
    let stateRevision = 0;
    await installPublicRsvpMock(page, {
      state: () => rsvpState(stateRevision),
      submit: async (route) => {
        submitCount += 1;
        submissions.push(recordSubmission(route));
        if (submitCount === 1) {
          await problem(route, 504, 'El servidor tardó demasiado.');
          return;
        }
        if (submitCount === 2) {
          await json(route, submissionResponse(1));
          return;
        }
        stateRevision = 1;
        await problem(
          route,
          409,
          'Existe una respuesta más reciente; recarga antes de continuar.',
          true,
        );
      },
    });
    await openCompletedWizard(page, 'retry-token');

    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByText('El servidor tardó demasiado.')).toBeVisible();
    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByRole('heading', { name: '¡Respuesta registrada!' })).toBeVisible();
    expect(submissions[0]?.idempotencyKey).toBe(submissions[1]?.idempotencyKey);

    await page.getByRole('button', { name: 'Modificar respuesta' }).click();
    await advanceToReview(page);
    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByText(/Recargamos los datos más recientes/)).toBeVisible();
    await expect.poll(() => stateRevision).toBe(1);
  });

  test('misma llave con contenido distinto recibe 409 sin crear otra revisión', async ({
    page,
  }) => {
    const submissions: RecordedSubmission[] = [];
    await installPublicRsvpMock(page, {
      submit: async (route) => {
        const current = recordSubmission(route);
        const original = submissions[0];
        if (
          original &&
          original.idempotencyKey === current.idempotencyKey &&
          JSON.stringify(original.body) !== JSON.stringify(current.body)
        ) {
          await problem(
            route,
            409,
            'La llave de idempotencia ya fue usada con contenido diferente.',
          );
          return;
        }
        submissions.push(current);
        await json(route, submissionResponse(1));
      },
    });
    await openCompletedWizard(page, 'fingerprint-conflict-token');
    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByRole('heading', { name: '¡Respuesta registrada!' })).toBeVisible();
    const original = submissions[0];
    expect(original).toBeDefined();

    const status = await page.evaluate(async ({ idempotencyKey, body }) => {
      const changed = {
        ...body,
        contactName: 'Contenido deliberadamente distinto',
      };
      const response = await fetch('/api/guest/rsvp/fingerprint-conflict-token/submit', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': idempotencyKey,
        },
        body: JSON.stringify(changed),
      });
      return response.status;
    }, original!);

    expect(status).toBe(409);
    expect(submissions).toHaveLength(1);
  });

  test('dos navegadores editando el mismo grupo fuerzan recarga en el editor obsoleto', async ({
    page,
    context,
  }) => {
    let currentRevision = 0;
    const submit = async (route: Route): Promise<void> => {
      const body = route.request().postDataJSON() as SubmissionBody;
      if (body.expectedRevision !== currentRevision) {
        await problem(route, 409, 'La respuesta cambió en otro navegador.', true);
        return;
      }
      currentRevision += 1;
      await json(route, submissionResponse(currentRevision));
    };
    await installPublicRsvpMock(page, {
      state: () => rsvpState(currentRevision),
      submit,
    });
    const secondPage = await context.newPage();
    await installPublicRsvpMock(secondPage, {
      state: () => rsvpState(currentRevision),
      submit,
    });
    await openCompletedWizard(page, 'concurrency-token');
    await openCompletedWizard(secondPage, 'concurrency-token');

    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByRole('heading', { name: '¡Respuesta registrada!' })).toBeVisible();
    await secondPage.getByRole('button', { name: 'Enviar respuesta' }).click();

    await expect(secondPage.getByText(/Recargamos los datos más recientes/)).toBeVisible();
    expect(currentRevision).toBe(1);
    await secondPage.close();
  });

  test('RSVP cerrado, excepción y enlaces inválido, suspendido, revocado, expirado o histórico proyectan estado seguro', async ({
    page,
  }) => {
    test.slow();
    await page.route('**/api/guest/rsvp/**', async (route) => {
      const token = route.request().url().split('/').at(-2);
      if (token === 'invalid-token') {
        await problem(route, 404, 'Enlace inválido.');
        return;
      }
      if (token === 'revoked-token') {
        await problem(route, 404, 'El enlace ya no está activo.');
        return;
      }
      if (token === 'expired-token') {
        await problem(route, 410, 'El enlace de invitado expiró.');
        return;
      }
      if (token === 'closed-token' || token === 'exception-closed-token') {
        await json(route, {
          ...rsvpState(0),
          canRespond: false,
          canModify: false,
          closedMessage: 'La confirmación está cerrada.',
        });
        return;
      }
      if (token === 'suspended-token') {
        await json(route, {
          ...rsvpState(0),
          canRespond: false,
          canModify: false,
          closedMessage: 'El evento está suspendido.',
        });
        return;
      }
      await json(route, rsvpState(0));
    });

    await page.goto('/rsvp/invalid-token');
    await expect(page.getByText('Enlace inválido.')).toBeVisible();
    await page.goto('/rsvp/suspended-token');
    await expect(page.getByText('El evento está suspendido.')).toBeVisible();
    await page.goto('/rsvp/revoked-token');
    await expect(page.getByText('El enlace ya no está activo.')).toBeVisible();
    await page.goto('/rsvp/expired-token');
    await expect(page.getByText('El enlace de invitado expiró.')).toBeVisible();
    await page.goto('/rsvp/closed-token');
    await expect(page.getByText('El RSVP no está disponible en este momento.')).toBeVisible();
    await expect(page.getByText('La confirmación está cerrada.')).toBeVisible();
    await page.goto('/rsvp/exception-open-token');
    await expect(page.getByRole('heading', { name: 'Familia Luna' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Siguiente' })).toBeEnabled();
    await page.goto('/rsvp/exception-closed-token');
    await expect(page.getByText('El RSVP no está disponible en este momento.')).toBeVisible();
    await page.goto('/rsvp/historic-key-token');
    await expect(page.getByRole('heading', { name: 'Familia Luna' })).toBeVisible();
  });

  test('captura manual seguida de SupportCorrection crea dos intentos y revisiones distintas', async ({
    page,
    api,
  }) => {
    api.useProfile('portal');
    const captures: RecordedSubmission[] = [];
    let releaseFormRequest!: () => void;
    const formRequestGate = new Promise<void>((resolve) => {
      releaseFormRequest = resolve;
    });
    await page.route('**/api/client-portal/events/event-1/rsvp/form', async (route) => {
      await formRequestGate;
      await json(route, {
        id: 'version-1',
      });
    });
    await page.route(
      '**/api/client-portal/events/event-1/rsvp/groups/group-1/manual-capture',
      async (route) => {
        captures.push(recordSubmission(route));
        await json(route, submissionResponse(captures.length), 201);
      },
    );
    await page.goto('/portal/events/event-1/rsvp/capture');
    await page.getByLabel('Grupo').fill('group-1');
    await page.getByLabel('Nombre del contacto').fill('Familia Luna');
    await page.getByLabel('Motivo / nota').fill('Captura telefónica');
    const submitButton = page.getByRole('button', { name: 'Registrar respuesta' });
    await expect(submitButton).toBeDisabled();
    releaseFormRequest();
    await expect(submitButton).toBeEnabled();
    await submitButton.click();
    await expect(page.getByText(/Respuesta registrada:/)).toBeVisible();

    await page.getByLabel('Revisión observada').fill('1');
    await page.getByLabel('Fuente').selectOption('SupportCorrection');
    await page.getByLabel('Motivo / nota').fill('Corrección solicitada');
    await page.getByRole('button', { name: 'Registrar respuesta' }).click();
    await expect(page.getByText(/Respuesta registrada: RSVP-2/)).toBeVisible();

    expect(captures).toHaveLength(2);
    expect(captures[0]?.idempotencyKey).not.toBe(captures[1]?.idempotencyKey);
    expect(captures[0]?.body.submission?.expectedRevision).toBe(0);
    expect(captures[1]?.body.submission?.expectedRevision).toBe(1);
    expect(captures[1]?.body.source).toBe('SupportCorrection');
  });

  test('transporte lleno, lista de espera, promoción y rollback muestran respuestas operativas sin duplicar', async ({
    page,
  }) => {
    let attempts = 0;
    const keys: string[] = [];
    await installPublicRsvpMock(page, {
      transport: true,
      submit: async (route) => {
        attempts += 1;
        keys.push(route.request().headers()['idempotency-key'] ?? '');
        if (attempts === 1) {
          await problem(route, 409, 'No quedan lugares y la lista de espera está deshabilitada.');
          return;
        }
        if (attempts === 2) {
          await problem(route, 500, 'La entrega falló y fue revertida por completo.');
          return;
        }
        await json(route, submissionResponse(1, 'Waitlisted'));
      },
    });
    await openCompletedWizard(page, 'transport-token', true);

    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByText(/No quedan lugares/)).toBeVisible();
    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByText(/revertida por completo/)).toBeVisible();
    await page.getByRole('button', { name: 'Enviar respuesta' }).click();
    await expect(page.getByRole('heading', { name: '¡Respuesta registrada!' })).toBeVisible();
    expect(keys[0]).toBe(keys[1]);
    expect(keys[1]).toBe(keys[2]);
  });

  test('usuario sin permiso sensible no ve controles ni indicadores', async ({ page, api }) => {
    api.useProfile('limited');
    await installProfessionalRsvpDashboardMock(page);

    await page.goto('/app/events/event-1/rsvp');

    await expect(page.getByRole('heading', { name: 'RSVP' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Ver datos sensibles' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Exportar datos sensibles' })).toHaveCount(0);
    await expect(page.getByText('Sensible', { exact: true })).toHaveCount(0);
  });

  test('Owner consulta y exporta datos sensibles mediante operaciones separadas', async ({
    page,
    api,
  }) => {
    api.useProfile('owner');
    let exports = 0;
    await installProfessionalRsvpDashboardMock(page, () => {
      exports += 1;
    });

    await page.goto('/app/events/event-1/rsvp');
    await page.getByRole('button', { name: 'Ver datos sensibles' }).click();
    await expect(page.getByRole('region', { name: 'Datos sensibles de invitados' })).toBeVisible();
    await expect(page.getByText('Nuez de prueba')).toBeVisible();
    await page.getByRole('button', { name: 'Exportar datos sensibles' }).click();

    await expect.poll(() => exports).toBe(1);
  });
});

interface SubmissionBody {
  expectedRevision: number;
  source?: string;
  submission?: { expectedRevision: number };
}

interface RecordedSubmission {
  idempotencyKey: string;
  body: SubmissionBody;
}

interface PublicMockOptions {
  state?: () => object;
  submit(route: Route): Promise<void>;
  transport?: boolean;
}

async function installPublicRsvpMock(page: Page, options: PublicMockOptions): Promise<void> {
  await page.route('**/api/guest/rsvp/**', async (route) => {
    const request = route.request();
    if (request.method() === 'GET') {
      const state = options.state?.() ?? rsvpState(0, options.transport);
      await json(route, state);
      return;
    }
    await options.submit(route);
  });
}

async function openCompletedWizard(
  page: Page,
  token: string,
  selectTransport = false,
): Promise<void> {
  await page.goto(`/rsvp/${token}`);
  await expect(page.getByRole('heading', { name: 'Familia Luna' })).toBeVisible();
  await page.getByRole('button', { name: 'Siguiente' }).click();
  await page.locator('select').first().selectOption('Attending');
  for (let step = 0; step < 7; step += 1) {
    await page.getByRole('button', { name: 'Siguiente' }).click();
    if (selectTransport && step === 3) {
      await page.locator('input[name="transport"][value="transport-1"]').check();
    }
  }
  await expect(page.getByRole('button', { name: 'Enviar respuesta' })).toBeVisible();
}

async function advanceToReview(page: Page): Promise<void> {
  const submit = page.getByRole('button', { name: 'Enviar respuesta' });
  for (let step = 0; step < 8; step += 1) {
    const next = page.getByRole('button', { name: 'Siguiente' });
    await expect(next).toBeVisible();
    await next.click();
  }
  await expect(submit).toBeVisible();
}

function recordSubmission(route: Route): RecordedSubmission {
  return {
    idempotencyKey: route.request().headers()['idempotency-key'] ?? '',
    body: route.request().postDataJSON() as SubmissionBody,
  };
}

function rsvpState(revision: number, transport = false): object {
  return {
    groupId: 'group-1',
    groupName: 'Familia Luna',
    allowedGuestCount: 1,
    maxUnnamedCompanions: 0,
    allowUnnamedCompanions: false,
    canRespond: true,
    canModify: true,
    closedMessage: null,
    settings: {
      id: 'settings-1',
      status: 'Open',
      opensAt: null,
      closesAt: null,
      timeZone: 'America/Matamoros',
      allowChangesAfterSubmission: true,
      changesCloseAt: null,
      allowTentativeResponse: false,
      allowGroupDecline: true,
      requireResponseForEveryNamedGuest: true,
      requireCompanionNames: false,
      allowContactInformationUpdate: true,
      showAttendanceSummaryAfterSubmission: true,
      confirmationTitle: null,
      confirmationMessage: null,
      declineMessage: null,
      closedMessage: null,
      privacyNotice: null,
      sensitiveDataConsentText: null,
      updatedAt: '2026-07-29T12:00:00Z',
    },
    activeForm: {
      id: 'version-1',
      rsvpFormId: 'form-1',
      versionNumber: 1,
      settingsSnapshot: '{}',
      questionsSnapshot: '[]',
      menuSnapshot: '[]',
      transportSnapshot: transport
        ? JSON.stringify([
            {
              id: 'transport-1',
              name: 'Camioneta',
              description: null,
              direction: 'ToCeremony',
              pickupPoint: 'Lobby',
              departureAt: null,
              returnAt: null,
              capacity: 1,
              allowWaitlist: true,
              isActive: true,
              sortOrder: 0,
              confirmedCount: 1,
              waitlistCount: 0,
            },
          ])
        : '[]',
      accommodationSnapshot: '[]',
      createdAt: '2026-07-29T12:00:00Z',
      approvedBy: 'user-1',
      approvedAt: '2026-07-29T12:00:00Z',
      publishedAt: '2026-07-29T12:00:00Z',
    },
    currentResponse: revision > 0 ? submissionResponse(revision) : null,
    revisionVersion: revision,
    groupTags: [],
    guests: [
      {
        eventGuestId: 'guest-1',
        displayName: 'Elena Luna',
        ageCategory: 'Adult',
        guestType: 'Adult',
        isPrimaryContact: true,
      },
    ],
  };
}

function submissionResponse(revision: number, transportStatus = 'Confirmed'): object {
  return {
    id: `submission-${revision}`,
    invitationGroupId: 'group-1',
    revisionNumber: revision,
    source: 'GuestPrivateLink',
    overallStatus: 'Confirmed',
    submittedAt: '2026-07-29T12:00:00Z',
    contactNameSnapshot: 'Familia Luna',
    contactEmailSnapshot: null,
    contactPhoneSnapshot: null,
    confirmationCode: `RSVP-${revision}`,
    guests: [
      {
        responseGuestId: 'guest-1',
        eventGuestId: 'guest-1',
        displayName: 'Elena Luna',
        ageCategory: 'Adult',
        attendanceStatus: 'Attending',
        menuSelectionsJson: '{}',
        transportSelectionJson: JSON.stringify({
          transportOptionId: 'transport-1',
          status: transportStatus,
        }),
        accommodationSelectionJson: '{}',
        dietaryJson: '{}',
        isUnnamedCompanion: false,
      },
    ],
    answers: [],
  };
}

async function json(route: Route, value: object, status = 200): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(value),
  });
}

async function problem(
  route: Route,
  status: number,
  detail: string,
  reloadRequired = false,
): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/problem+json',
    body: JSON.stringify({
      title: 'Solicitud rechazada',
      detail,
      status,
      reloadRequired,
    }),
  });
}

async function installProfessionalRsvpDashboardMock(
  page: Page,
  onSensitiveExport: () => void = () => undefined,
): Promise<void> {
  const eventUrl = '**/api/organizations/org-1/events/event-1';
  await page.route(`${eventUrl}/rsvp/dashboard`, async (route) => {
    await json(route, {
      totalGroups: 1,
      totalGuestsGranted: 1,
      guestsConfirmed: 1,
      guestsNotAttending: 0,
      guestsTentative: 0,
      guestsPending: 0,
      partialResponses: 0,
      changedAfterSubmission: 0,
      closesAt: null,
      groups: [
        {
          groupId: 'group-1',
          groupName: 'Familia Luna',
          status: 'Confirmed',
          confirmedCount: 1,
          declinedCount: 0,
          pendingCount: 0,
          hasMenuSelection: false,
          hasTransport: false,
          hasAccommodation: false,
          hasSensitiveData: true,
          lastResponseAt: '2026-07-29T12:00:00Z',
        },
      ],
    });
  });
  await page.route(`${eventUrl}/rsvp/settings`, async (route) => {
    await json(route, {
      id: 'settings-1',
      status: 'Open',
      opensAt: null,
      closesAt: null,
      timeZone: 'America/Matamoros',
      allowChangesAfterSubmission: true,
      changesCloseAt: null,
      allowTentativeResponse: false,
      allowGroupDecline: true,
      requireResponseForEveryNamedGuest: true,
      requireCompanionNames: false,
      allowContactInformationUpdate: true,
      showAttendanceSummaryAfterSubmission: true,
      confirmationTitle: null,
      confirmationMessage: null,
      declineMessage: null,
      closedMessage: null,
      privacyNotice: null,
      sensitiveDataConsentText: null,
      updatedAt: '2026-07-29T12:00:00Z',
    });
  });
  await page.route(`${eventUrl}/rsvp/sensitive-data`, async (route) => {
    await json(route, [
      {
        eventGuestId: 'guest-1',
        displayName: 'Elena Luna',
        allergies: 'Nuez de prueba',
        dietaryRestrictions: null,
        accessibilityRequirements: null,
        additionalNotes: null,
        consentGrantedAt: '2026-07-29T12:00:00Z',
        updatedAt: '2026-07-29T12:00:00Z',
      },
    ]);
  });
  await page.route(`${eventUrl}/rsvp/sensitive-question-answers`, async (route) => {
    await json(route, []);
  });
  await page.route(`${eventUrl}/rsvp/exports/sensitive`, async (route) => {
    onSensitiveExport();
    await route.fulfill({
      status: 200,
      contentType: 'text/csv; charset=utf-8',
      body: 'Invitado,Alergias\r\nElena Luna,Nuez de prueba\r\n',
    });
  });
}
