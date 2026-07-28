import { Route } from '@playwright/test';
import { expect, test } from '../fixtures/plannyt.fixture';

test.describe('Sprint 2A · invitados y experiencia digital', () => {
  test('completa el flujo privado desde grupos hasta revisión pública y portal', async ({
    page,
    api,
  }) => {
    api.useProfile('owner');
    const state = createGuestExperienceState();
    await page.route('https://localhost:7139/api/**', async (route) => {
      if (!(await handleGuestExperienceApi(route, state))) {
        await route.fallback();
      }
    });

    // 1. Abrir tablero profesional y comprobar cuota.
    await page.goto('/app/events/event-1/guests');
    await expect(page.getByRole('heading', { name: 'Invitados y grupos' })).toBeVisible();
    await expect(page.getByText('de 100 en Community')).toBeVisible();

    // 2. Crear grupo con capacidad explícita.
    await page.getByRole('button', { name: 'Grupos', exact: true }).click();
    await page.getByLabel('Nombre visible').fill('Familia Luna');
    await page.getByLabel('Capacidad').fill('3');
    await page.getByRole('button', { name: 'Guardar grupo' }).click();
    await expect(page.getByRole('heading', { name: 'Familia Luna' })).toBeVisible();

    // 3. Agregar invitado principal.
    await page.getByRole('button', { name: 'Invitados', exact: true }).click();
    await page.locator('select[formcontrolname="invitationGroupId"]').selectOption('group-1');
    await page.locator('input[formcontrolname="firstName"]').fill('Elena');
    await page.locator('input[formcontrolname="lastName"]').fill('Luna');
    await page.getByLabel('Contacto principal del grupo').check();
    await page.getByLabel('Invitado VIP').check();
    await page.getByRole('button', { name: 'Guardar invitado' }).click();
    await expect(page.getByText('Elena Luna')).toBeVisible();

    // 4. Analizar y confirmar un CSV.
    await page.getByRole('button', { name: 'Importar CSV' }).click();
    await page.locator('input[type="file"]').setInputFiles({
      name: 'invitados.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from('GroupName,GuestFirstName,GuestLastName\nFamilia Luna,Mateo,Luna'),
    });
    await expect(page.getByText('1', { exact: true }).first()).toBeVisible();
    await page.getByRole('button', { name: 'Confirmar importación' }).click();
    await expect(page.getByText('Se crearon 1 invitados en 0 grupos.')).toBeVisible();

    // 5. Revisar sugerencias sin fusión automática.
    await page.getByRole('button', { name: 'Duplicados' }).click();
    await expect(page.getByText('No se realizará ninguna fusión automática.')).toBeVisible();

    // 6. Entrar al estudio y elegir plantilla.
    await page.getByRole('link', { name: 'Diseñar invitación' }).click();
    await expect(page.getByRole('heading', { name: 'Diseño y publicación' })).toBeVisible();
    await page.getByRole('button', { name: /Minimalista/ }).click();
    await expect(page.getByText('Vista previa', { exact: true })).toBeVisible();

    // 7. Editar un bloque y esperar el guardado automático.
    await page
      .getByRole('button', { name: /Portada/ })
      .first()
      .click();
    await page.getByLabel('Título', { exact: true }).fill('Noche de verano');
    await expect(page.getByText('Guardado', { exact: true })).toBeVisible();

    // 8. Crear versión inmutable y enviarla a revisión.
    await page.getByRole('button', { name: 'Enviar a revisión' }).click();
    await expect(page.getByText('En revisión', { exact: true }).first()).toBeVisible();

    // 9. Comentar y aprobar exactamente esa versión.
    await page.getByLabel('Comentario').fill('Aprobada por la familia.');
    await page.getByRole('button', { name: 'Aprobar' }).click();
    await expect(page.getByText('Aprobado', { exact: true }).first()).toBeVisible();

    // 10. Publicar la versión aprobada.
    await page.getByRole('button', { name: 'Publicar versión aprobada' }).click();
    await expect(page.getByText('Publicado', { exact: true }).first()).toBeVisible();

    // 11. Generar enlace privado por grupo.
    await page.getByRole('button', { name: 'Generar enlace' }).click();
    await expect(page.locator('.copy-box input')).toHaveValue('http://127.0.0.1:4200/i/test-token');

    // 12. Abrir la experiencia anónima personalizada.
    await page.goto('/i/test-token');
    await expect(page.getByRole('heading', { name: 'Noche de verano' })).toBeVisible();
    await expect(page.getByText('Elena Luna')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Confirmar asistencia' })).toBeDisabled();
    await expect(page.getByText(/Demostración/)).toBeVisible();

    // 13. Confirmar que datos privados no aparecen.
    await expect(page.getByText('elena@example.com')).toHaveCount(0);
    await expect(page.getByText('Nota interna')).toHaveCount(0);

    // 14. Cambiar al portal y revisar el mismo espacio sin permisos expansivos.
    api.useProfile('portal');
    await page.goto('/portal/events/event-1/guest-experience');
    await expect(
      page.getByRole('heading', { name: 'Invitados e invitación digital' }),
    ).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Familia Luna' })).toBeVisible();

    // 15. Verificar que el portal no ofrece publicación ni revocación.
    await page.getByRole('button', { name: 'Diseño' }).click();
    await expect(page.getByRole('button', { name: /Publicar/ })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Revocar/ })).toHaveCount(0);
  });
});

interface GuestExperienceState {
  groups: Array<Record<string, unknown>>;
  guests: Array<Record<string, unknown>>;
  design: Record<string, unknown> | null;
  experienceStatus: 'Draft' | 'Ready' | 'Published';
  linkCreated: boolean;
}

function createGuestExperienceState(): GuestExperienceState {
  return {
    groups: [],
    guests: [],
    design: null,
    experienceStatus: 'Draft',
    linkCreated: false,
  };
}

async function handleGuestExperienceApi(
  route: Route,
  state: GuestExperienceState,
): Promise<boolean> {
  const request = route.request();
  const url = new URL(request.url());
  const path = url.pathname;
  const method = request.method();
  const guestBase = '/api/organizations/org-1/events/event-1/guests';
  const invitationBase = '/api/organizations/org-1/events/event-1/invitations';

  if (path === `${guestBase}/dashboard` && method === 'GET') {
    await json(route, dashboard(state));
    return true;
  }
  if (path === `${guestBase}/tags` && method === 'GET') {
    await json(route, [
      { id: 'tag-1', name: 'Familia', colorToken: 'emerald' },
      { id: 'tag-2', name: 'VIP', colorToken: 'rose' },
    ]);
    return true;
  }
  if (path === `${guestBase}/groups` && method === 'POST') {
    state.groups = [groupResponse()];
    await json(route, state.groups[0]!, 201);
    return true;
  }
  if (path === guestBase && method === 'POST') {
    state.guests = [guestResponse()];
    await json(route, state.guests[0]!, 201);
    return true;
  }
  if (path === `${guestBase}/duplicates` && method === 'GET') {
    await json(route, [
      {
        kind: 'name',
        reason: 'Nombre similar dentro del mismo grupo',
        guestIds: ['guest-1', 'guest-2'],
        suggestedAction: 'Ignorar, editar o mover; no se fusionará automáticamente.',
      },
    ]);
    return true;
  }
  if (path === `${guestBase}/imports/analyze` && method === 'POST') {
    await json(route, {
      importId: 'import-1',
      status: 'Analyzed',
      headers: ['GroupName', 'GuestFirstName', 'GuestLastName'],
      mapping: {
        GroupName: 'GroupName',
        GuestFirstName: 'GuestFirstName',
        GuestLastName: 'GuestLastName',
      },
      totalRows: 1,
      validRows: 1,
      errorRows: 0,
      preview: [
        {
          rowNumber: 2,
          groupName: 'Familia Luna',
          guestName: 'Mateo Luna',
          isValid: true,
          errors: [],
        },
      ],
    });
    return true;
  }
  if (path === `${guestBase}/imports/import-1/confirm` && method === 'POST') {
    state.guests.push({
      ...guestResponse(),
      id: 'guest-2',
      firstName: 'Mateo',
      isPrimaryContact: false,
      isVip: false,
    });
    await json(route, {
      importId: 'import-1',
      status: 'Completed',
      createdGroups: 0,
      createdGuests: 1,
      reusedGroups: 1,
      skippedRows: 0,
      errors: [],
      completedAt: '2026-07-28T18:00:00Z',
    });
    return true;
  }
  if (path === `${invitationBase}/templates` && method === 'GET') {
    await json(route, [templateResponse()]);
    return true;
  }
  if (path === `${invitationBase}/designs` && method === 'GET') {
    await json(route, state.design ? [state.design] : []);
    return true;
  }
  if (path === `${invitationBase}/experience` && method === 'GET') {
    await json(route, experienceResponse(state));
    return true;
  }
  if (path === `${invitationBase}/links` && method === 'GET') {
    await json(route, state.linkCreated ? [linkResponse()] : []);
    return true;
  }
  if (path === `${invitationBase}/designs` && method === 'POST') {
    state.design = designResponse('Draft');
    state.experienceStatus = 'Ready';
    await json(route, state.design, 201);
    return true;
  }
  if (path === `${invitationBase}/designs/design-1` && method === 'PUT') {
    const body = request.postDataJSON() as {
      name: string;
      theme: Record<string, unknown>;
      blocks: Array<Record<string, unknown>>;
    };
    state.design = {
      ...designResponse('Draft'),
      name: body.name,
      theme: body.theme,
      blocks: body.blocks,
    };
    await json(route, state.design);
    return true;
  }
  if (path === `${invitationBase}/designs/design-1/submit-review` && method === 'POST') {
    state.design = designResponse('InReview', true);
    await json(route, state.design);
    return true;
  }
  if (path.endsWith('/versions/version-1/approve') && method === 'POST') {
    state.design = designResponse('Approved', true, true);
    await json(route, state.design);
    return true;
  }
  if (path === `${invitationBase}/designs/design-1/publish` && method === 'POST') {
    state.design = designResponse('Published', true, true);
    state.experienceStatus = 'Published';
    await json(route, state.design);
    return true;
  }
  if (path === `${invitationBase}/groups/group-1/links` && method === 'POST') {
    state.linkCreated = true;
    await json(route, linkResponse(), 201);
    return true;
  }
  if (path === '/api/public/invitations/test-token' && method === 'GET') {
    await json(route, publicInvitationResponse(state));
    return true;
  }
  if (path === '/api/client-portal/events/event-1/guest-experience' && method === 'GET') {
    await json(route, {
      eventId: 'event-1',
      groups: state.groups.map((group) => ({
        id: group['id'],
        displayName: group['displayName'],
        allowedGuestCount: group['allowedGuestCount'],
        namedGuestCount: state.guests.length,
        allowUnnamedCompanions: false,
        maxUnnamedCompanions: 0,
      })),
      guests: state.guests.map((guest) => ({
        id: guest['id'],
        invitationGroupId: guest['invitationGroupId'],
        firstName: guest['firstName'],
        lastName: guest['lastName'],
        guestType: guest['guestType'],
        ageCategory: guest['ageCategory'],
        isPrimaryContact: guest['isPrimaryContact'],
        isVip: guest['isVip'],
      })),
      design: state.design,
    });
    return true;
  }
  return false;
}

function dashboard(state: GuestExperienceState): object {
  return {
    activeGuestCount: state.guests.length,
    groupCount: state.groups.length,
    linkCount: state.linkCreated ? 1 : 0,
    openedLinkCount: 0,
    plan: {
      tier: 'Community',
      activeGuests: state.guests.length,
      limit: 100,
      percentage: state.guests.length,
      warning80: false,
      warning90: false,
      isAtLimit: false,
    },
    groups: state.groups,
    guests: state.guests,
  };
}

function groupResponse(): Record<string, unknown> {
  return {
    id: 'group-1',
    groupType: 'Family',
    displayName: 'Familia Luna',
    contactName: null,
    contactPhone: null,
    contactEmail: null,
    allowedGuestCount: 3,
    namedGuestCount: 0,
    availableGuestCount: 3,
    allowUnnamedCompanions: false,
    maxUnnamedCompanions: 0,
    status: 'Draft',
    source: 'Manual',
    internalNotes: null,
    capacityOverrideApplied: false,
    tags: [],
    updatedAt: '2026-07-28T18:00:00Z',
    archivedAt: null,
  };
}

function guestResponse(): Record<string, unknown> {
  return {
    id: 'guest-1',
    invitationGroupId: 'group-1',
    personId: null,
    firstName: 'Elena',
    lastName: 'Luna',
    email: null,
    phone: null,
    guestType: 'Other',
    ageCategory: 'Adult',
    isPrimaryContact: true,
    isNamed: true,
    isPlusOne: false,
    isVip: true,
    isActive: true,
    sortOrder: 0,
    internalNotes: null,
    updatedAt: '2026-07-28T18:00:00Z',
    archivedAt: null,
  };
}

function themeResponse(): Record<string, unknown> {
  return {
    backgroundColor: '#FAFAFA',
    surfaceColor: '#FFFFFF',
    textColor: '#171717',
    accentColor: '#404040',
    headingFont: 'inter',
    bodyFont: 'inter',
    radiusToken: 'lg',
    spacingToken: 'comfortable',
    coverStyle: 'card',
    buttonStyle: 'solid',
    animation: 'Reduced',
  };
}

function blocksResponse(): Array<Record<string, unknown>> {
  return [
    {
      id: 'block-cover',
      type: 'Cover',
      visible: true,
      visibility: 'Everyone',
      visibilityValue: null,
      sortOrder: 0,
      content: {
        eyebrow: 'Estás invitado',
        title: 'Noche de verano',
        subtitle: '{{group.displayName}}',
      },
      presentation: {
        backgroundToken: 'default',
        textAlign: 'center',
        emphasis: 'normal',
        width: 'content',
      },
    },
    {
      id: 'block-participants',
      type: 'Participants',
      visible: true,
      visibility: 'Everyone',
      visibilityValue: null,
      sortOrder: 1,
      content: { heading: 'Esta invitación incluye a', format: 'list' },
      presentation: {
        backgroundToken: 'default',
        textAlign: 'center',
        emphasis: 'normal',
        width: 'content',
      },
    },
  ];
}

function templateResponse(): object {
  return {
    id: 'template-1',
    isGlobal: true,
    name: 'Minimalista',
    description: 'Composición limpia y directa.',
    theme: themeResponse(),
    blocks: blocksResponse(),
  };
}

function designResponse(
  status: string,
  withVersion = false,
  approved = false,
): Record<string, unknown> {
  return {
    id: 'design-1',
    eventId: 'event-1',
    name: 'Minimalista · Invitación',
    status,
    theme: themeResponse(),
    blocks: blocksResponse(),
    nextVersionNumber: withVersion ? 2 : 1,
    approvedVersionId: approved ? 'version-1' : null,
    versions: withVersion
      ? [
          {
            id: 'version-1',
            versionNumber: 1,
            theme: themeResponse(),
            blocks: blocksResponse(),
            createdAt: '2026-07-28T18:00:00Z',
            approvedAt: approved ? '2026-07-28T18:05:00Z' : null,
            publishedAt: status === 'Published' ? '2026-07-28T18:10:00Z' : null,
          },
        ]
      : [],
    comments: approved
      ? [
          {
            id: 'comment-1',
            versionId: 'version-1',
            decision: 'Approved',
            message: 'Aprobada por la familia.',
            createdAt: '2026-07-28T18:05:00Z',
          },
        ]
      : [],
    accessibilityWarnings: [],
    updatedAt: '2026-07-28T18:00:00Z',
  };
}

function experienceResponse(state: GuestExperienceState): object {
  return {
    id: 'experience-1',
    eventId: 'event-1',
    status: state.experienceStatus,
    language: 'es',
    publicTitle: 'Noche de verano',
    celebrantDisplayName: 'Ana & Carlos',
    welcomeMessage: 'Nos encantará compartir este momento.',
    closingMessage: 'Con cariño, Ana & Carlos.',
    showEventName: true,
    showEventDate: true,
    showParticipantNames: true,
    showCity: true,
    privateAccessOnly: true,
    activeInvitationDesignId: state.experienceStatus === 'Published' ? 'design-1' : null,
    activeVersionId: state.experienceStatus === 'Published' ? 'version-1' : null,
    updatedAt: '2026-07-28T18:00:00Z',
  };
}

function linkResponse(): object {
  return {
    id: 'link-1',
    invitationGroupId: 'group-1',
    status: 'Active',
    publicUrl: 'http://127.0.0.1:4200/i/test-token',
    expiresAt: null,
    firstOpenedAt: null,
    lastOpenedAt: null,
    openCount: 0,
    sharedManuallyAt: null,
    createdAt: '2026-07-28T18:00:00Z',
  };
}

function publicInvitationResponse(state: GuestExperienceState): object {
  return {
    status: 'available',
    language: 'es',
    publicTitle: 'Noche de verano',
    celebrantDisplayName: 'Ana & Carlos',
    welcomeMessage: 'Nos encantará compartir este momento.',
    eventName: 'Ana & Carlos',
    eventStartsAt: '2027-03-20T18:00:00-06:00',
    eventTimeZone: 'America/Matamoros',
    city: 'Monterrey',
    countryCode: 'MX',
    groupDisplayName: 'Familia Luna',
    allowedGuestCount: 3,
    participants: state.guests.map((guest) => ({
      firstName: guest['firstName'],
      lastName: guest['lastName'],
      guestType: guest['guestType'],
      ageCategory: guest['ageCategory'],
      isPrimaryContact: guest['isPrimaryContact'],
      isVip: guest['isVip'],
    })),
    theme: themeResponse(),
    blocks: blocksResponse(),
    closingMessage: 'Con cariño, Ana & Carlos.',
  };
}

async function json(route: Route, value: object | object[], status = 200): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(value),
  });
}
