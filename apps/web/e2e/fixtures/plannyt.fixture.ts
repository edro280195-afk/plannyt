import { expect, Route, test as base } from '@playwright/test';

type ProfileKind = 'anonymous' | 'owner' | 'portal' | 'limited';

interface RecordedRequest {
  method: string;
  path: string;
  body: unknown;
}

interface ApiMock {
  requests: RecordedRequest[];
  useProfile(profile: ProfileKind): void;
  requestFor(method: string, path: string): RecordedRequest | undefined;
}

interface PlannytFixtures {
  api: ApiMock;
}

const ownerPermissions = [
  'organization.view',
  'organization.update',
  'organization.members.view',
  'organization.members.invite',
  'organization.members.update',
  'organization.members.revoke',
  'clients.view',
  'clients.create',
  'clients.update',
  'clients.archive',
  'events.view',
  'events.create',
  'events.update',
  'events.archive',
  'events.members.view',
  'events.members.invite',
  'events.members.update',
  'events.members.revoke',
  'participants.view',
  'participants.manage',
  'documents.view-shared',
  'documents.upload-shared',
  'documents.view-internal',
  'documents.upload-internal',
  'documents.delete',
];

const eventSummary = {
  id: 'event-1',
  name: 'Ana & Carlos',
  eventType: 'Boda',
  status: 'Planning',
  startDateTime: '2027-03-20T18:00:00-06:00',
  endDateTime: '2027-03-21T01:00:00-06:00',
  timeZone: 'America/Matamoros',
  city: 'Monterrey',
  countryCode: 'MX',
  sharedDescription: 'Una celebración al aire libre.',
  estimatedGuestCount: 180,
  updatedAt: '2026-07-28T12:00:00Z',
};

const eventDetail = {
  ...eventSummary,
  organizationId: 'org-1',
  createdBy: 'user-1',
  createdAt: '2026-07-28T12:00:00Z',
  archivedAt: null,
  statusHistory: [],
};

const clientDetail = {
  id: 'client-1',
  clientType: 'Person',
  displayName: 'Ana Martínez',
  companyName: null,
  status: 'Active',
  source: 'Recomendación',
  person: {
    id: 'person-2',
    firstName: 'Ana',
    lastName: 'Martínez',
    displayName: 'Ana Martínez',
    contactEmail: 'ana@example.com',
    contactPhone: null,
    preferredLanguage: 'es',
    timeZone: 'America/Matamoros',
  },
  contacts: [],
  createdAt: '2026-07-28T12:00:00Z',
  updatedAt: '2026-07-28T12:00:00Z',
  archivedAt: null,
};

export const test = base.extend<PlannytFixtures>({
  api: async ({ page }, use) => {
    let profile: ProfileKind = 'anonymous';
    const requests: RecordedRequest[] = [];
    const api: ApiMock = {
      requests,
      useProfile(value): void {
        profile = value;
      },
      requestFor(method, path) {
        return requests.find((request) => request.method === method && request.path === path);
      },
    };

    await page.route('https://localhost:7139/api/**', async (route) => {
      const request = route.request();
      const url = new URL(request.url());
      if (url.pathname === '/api/auth/login' || url.pathname === '/api/auth/register-planner') {
        profile = 'owner';
      } else if (url.pathname === '/api/access-invitations/client-token/register-and-accept') {
        profile = 'portal';
      }
      const bodyText = request.postData();
      requests.push({
        method: request.method(),
        path: url.pathname,
        body: bodyText ? (JSON.parse(bodyText) as unknown) : null,
      });
      await fulfillApi(route, profile);
    });

    await use(api);
  },
});

export { expect };

async function fulfillApi(route: Route, profile: ProfileKind): Promise<void> {
  const request = route.request();
  const url = new URL(request.url());
  const path = url.pathname;
  const method = request.method();

  if (path === '/api/auth/refresh') {
    if (profile === 'anonymous') {
      await problem(route, 401, 'La sesión no está disponible.');
      return;
    }
    await json(route, authResponse(profile));
    return;
  }

  if (path === '/api/auth/login' || path === '/api/auth/register-planner') {
    await json(route, authResponse('owner'));
    return;
  }

  if (path === '/api/access-invitations/client-token/register-and-accept') {
    await json(route, authResponse('portal'));
    return;
  }

  if (path === '/api/auth/me') {
    await json(route, meResponse(profile === 'anonymous' ? 'owner' : profile));
    return;
  }

  if (path === '/api/organizations/org-1/clients' && method === 'GET') {
    await json(route, {
      items: [
        {
          id: 'client-1',
          clientType: 'Person',
          displayName: 'Ana Martínez',
          companyName: null,
          status: 'Active',
          source: 'Recomendación',
          updatedAt: '2026-07-28T12:00:00Z',
        },
      ],
      page: 1,
      pageSize: 50,
      totalCount: 1,
    });
    return;
  }

  if (path === '/api/organizations/org-1/clients' && method === 'POST') {
    await json(route, clientDetail, 201);
    return;
  }

  if (path === '/api/organizations/org-1/clients/client-1') {
    await json(route, clientDetail);
    return;
  }

  if (path === '/api/organizations/org-1/events' && method === 'GET') {
    await json(route, {
      items: [eventSummary],
      page: 1,
      pageSize: 50,
      totalCount: 1,
    });
    return;
  }

  if (path === '/api/organizations/org-1/events' && method === 'POST') {
    await json(route, eventDetail, 201);
    return;
  }

  if (path === '/api/organizations/org-1/events/event-1') {
    await json(route, eventDetail);
    return;
  }

  if (path.endsWith('/events/event-1/clients')) {
    await json(route, []);
    return;
  }

  if (path.endsWith('/events/event-1/participants')) {
    await json(route, []);
    return;
  }

  if (path.endsWith('/events/event-1/access') && method === 'GET') {
    await json(route, []);
    return;
  }

  if (path.endsWith('/events/event-1/access/invitations') && method === 'POST') {
    await json(route, {
      id: 'invitation-1',
      invitationType: 'EventAccess',
      targetEmail: 'ana@example.com',
      expiresAt: '2026-08-04T12:00:00Z',
      invitationUrl: 'http://127.0.0.1:4200/accept-access/client-token',
    });
    return;
  }

  if (path.endsWith('/events/event-1/documents')) {
    await json(route, []);
    return;
  }

  if (path === '/api/access-invitations/client-token') {
    await json(route, {
      invitationType: 'EventAccess',
      organizationName: 'Armonía Eventos',
      eventName: 'Ana & Carlos',
      targetEmail: 'ana@example.com',
      intendedRole: 'ClientPrimary',
      expiresAt: '2026-08-04T12:00:00Z',
      status: 'Pending',
    });
    return;
  }

  if (path === '/api/client-portal/events' && method === 'GET') {
    await json(route, [portalEvent()]);
    return;
  }

  if (path === '/api/client-portal/events/event-1') {
    await json(route, {
      ...portalEvent(),
      participants: [
        {
          id: 'participant-1',
          displayName: 'Ana Martínez',
          participantType: 'Novia',
          displayOrder: 1,
          sharedDescription: 'Protagonista del evento',
        },
      ],
      documents: [
        {
          id: 'document-1',
          documentType: 'Programa',
          fileName: 'programa.pdf',
          mimeType: 'application/pdf',
          sizeBytes: 2048,
          createdAt: '2026-07-28T12:00:00Z',
        },
      ],
    });
    return;
  }

  await problem(route, 404, `Ruta simulada no definida: ${method} ${path}`);
}

function authResponse(profile: Exclude<ProfileKind, 'anonymous'>): object {
  return {
    accessToken: `token-${profile}`,
    accessTokenExpiresAt: '2026-07-28T18:00:00Z',
    userAccountId: profile === 'portal' ? 'user-2' : 'user-1',
    email: profile === 'portal' ? 'ana@example.com' : 'mariana@armonia.mx',
    organizationId: profile === 'portal' ? null : 'org-1',
  };
}

function meResponse(profile: Exclude<ProfileKind, 'anonymous'>): object {
  if (profile === 'portal') {
    return {
      userAccountId: 'user-2',
      email: 'ana@example.com',
      organizations: [],
      eventAccesses: [
        {
          organizationId: 'org-1',
          eventId: 'event-1',
          eventName: 'Ana & Carlos',
          role: 'ClientPrimary',
        },
      ],
    };
  }

  return {
    userAccountId: 'user-1',
    email: 'mariana@armonia.mx',
    organizations: [
      {
        organizationId: 'org-1',
        organizationName: 'Armonía Eventos',
        membershipId: 'membership-1',
        role: profile === 'limited' ? 'Assistant' : 'Owner',
        permissions: profile === 'limited' ? ['events.view'] : ownerPermissions,
      },
    ],
    eventAccesses: [],
  };
}

function portalEvent(): object {
  return {
    id: eventSummary.id,
    name: eventSummary.name,
    eventType: eventSummary.eventType,
    startDateTime: eventSummary.startDateTime,
    endDateTime: eventSummary.endDateTime,
    timeZone: eventSummary.timeZone,
    city: eventSummary.city,
    countryCode: eventSummary.countryCode,
    sharedDescription: eventSummary.sharedDescription,
    estimatedGuestCount: eventSummary.estimatedGuestCount,
  };
}

async function json(route: Route, value: object | object[], status = 200): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/json',
    body: JSON.stringify(value),
  });
}

async function problem(route: Route, status: number, detail: string): Promise<void> {
  await route.fulfill({
    status,
    contentType: 'application/problem+json',
    body: JSON.stringify({ title: 'Solicitud rechazada', detail, status }),
  });
}
