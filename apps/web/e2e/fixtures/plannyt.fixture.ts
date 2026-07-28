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
  'prospects.view',
  'prospects.create',
  'prospects.update',
  'prospects.assign',
  'prospects.change-status',
  'prospects.archive',
  'prospects.private-notes.view',
  'prospects.private-notes.manage',
  'catalog.view',
  'catalog.manage',
  'packages.view',
  'packages.manage',
  'coupons.view',
  'coupons.manage',
  'proposals.view',
  'proposals.create',
  'proposals.update-draft',
  'proposals.publish',
  'proposals.send',
  'proposals.cancel',
  'proposals.view-internal',
  'proposals.manage-comments',
  'proposals.convert-client',
];

interface CommercialState {
  prospectCreated: boolean;
  prospectConverted: boolean;
  activityCreated: boolean;
  serviceCreated: boolean;
  packageCreated: boolean;
  proposalCreated: boolean;
  proposalStatus: string;
  proposalVersion: number;
}

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
    const commercial: CommercialState = {
      prospectCreated: false,
      prospectConverted: false,
      activityCreated: false,
      serviceCreated: false,
      packageCreated: false,
      proposalCreated: false,
      proposalStatus: 'Draft',
      proposalVersion: 0,
    };
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
      await fulfillApi(route, profile, commercial);
    });

    await use(api);
  },
});

export { expect };

async function fulfillApi(
  route: Route,
  profile: ProfileKind,
  commercial: CommercialState,
): Promise<void> {
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

  if (path === '/api/organizations/org-1/prospects' && method === 'GET') {
    await json(route, {
      items: commercial.prospectCreated ? [prospectSummary(commercial)] : [],
      page: 1,
      pageSize: 100,
      totalCount: commercial.prospectCreated ? 1 : 0,
    });
    return;
  }

  if (path === '/api/organizations/org-1/prospects' && method === 'POST') {
    commercial.prospectCreated = true;
    await json(route, prospectDetail(commercial), 201);
    return;
  }

  if (path === '/api/organizations/org-1/prospects/prospect-1' && method === 'GET') {
    await json(route, prospectDetail(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/prospects/prospect-1/activities' && method === 'POST') {
    commercial.activityCreated = true;
    await json(route, prospectActivity(), 201);
    return;
  }

  if (path === '/api/organizations/org-1/prospects/prospect-1/client-matches' && method === 'GET') {
    await json(route, []);
    return;
  }

  if (path === '/api/organizations/org-1/prospects/prospect-1/convert' && method === 'POST') {
    commercial.prospectConverted = true;
    await json(route, {
      prospectId: 'prospect-1',
      clientId: 'client-1',
      createdNewClient: true,
    });
    return;
  }

  if (
    path === '/api/organizations/org-1/prospects/prospect-1/preliminary-event' &&
    method === 'POST'
  ) {
    await json(route, {
      prospectId: 'prospect-1',
      eventId: 'event-preliminary',
      createdNewEvent: true,
    });
    return;
  }

  if (path === '/api/organizations/org-1/catalog/services' && method === 'GET') {
    await json(route, commercial.serviceCreated ? [catalogService()] : []);
    return;
  }

  if (path === '/api/organizations/org-1/catalog/services' && method === 'POST') {
    commercial.serviceCreated = true;
    await json(route, catalogService(), 201);
    return;
  }

  if (path === '/api/organizations/org-1/catalog/packages' && method === 'GET') {
    await json(route, commercial.packageCreated ? [catalogPackage()] : []);
    return;
  }

  if (path === '/api/organizations/org-1/catalog/packages' && method === 'POST') {
    commercial.packageCreated = true;
    await json(route, catalogPackage(), 201);
    return;
  }

  if (path === '/api/organizations/org-1/catalog/coupons' && method === 'GET') {
    await json(route, []);
    return;
  }

  if (path === '/api/organizations/org-1/proposals' && method === 'GET') {
    await json(route, {
      items: commercial.proposalCreated ? [proposalSummary(commercial)] : [],
      page: 1,
      pageSize: 100,
      totalCount: commercial.proposalCreated ? 1 : 0,
    });
    return;
  }

  if (path === '/api/organizations/org-1/proposals' && method === 'POST') {
    commercial.proposalCreated = true;
    commercial.proposalStatus = 'Draft';
    await json(route, proposalDetail(commercial), 201);
    return;
  }

  if (path === '/api/organizations/org-1/proposals/proposal-1' && method === 'GET') {
    await json(route, proposalDetail(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/proposals/proposal-1/draft' && method === 'PUT') {
    commercial.proposalStatus = 'Negotiation';
    await json(route, proposalDetail(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/proposals/proposal-1/publish' && method === 'POST') {
    commercial.proposalVersion += 1;
    commercial.proposalStatus = 'Ready';
    await json(route, proposalVersion(commercial.proposalVersion));
    return;
  }

  if (path === '/api/organizations/org-1/proposals/proposal-1/send' && method === 'POST') {
    commercial.proposalStatus = 'Sent';
    await json(route, {
      id: `share-${commercial.proposalVersion}`,
      proposalVersionId: `version-${commercial.proposalVersion}`,
      expiresAt: '2027-02-01T12:00:00Z',
      shareUrl: `http://127.0.0.1:4200/proposal/public-token-${commercial.proposalVersion}`,
    });
    return;
  }

  if (path.startsWith('/api/public/proposals/public-token-') && method === 'GET') {
    await json(route, publicProposal(commercial));
    return;
  }

  if (path.endsWith('/request-changes') && path.startsWith('/api/public/proposals/')) {
    commercial.proposalStatus = 'ChangesRequested';
    await json(route, publicProposal(commercial));
    return;
  }

  if (path.endsWith('/accept') && path.startsWith('/api/public/proposals/')) {
    commercial.proposalStatus = 'Accepted';
    await json(route, publicProposal(commercial));
    return;
  }

  if (
    path.endsWith('/comments') &&
    path.startsWith('/api/public/proposals/') &&
    method === 'POST'
  ) {
    await json(route, publicComment(), 201);
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

  if (path === '/api/organizations/org-1/events/event-preliminary') {
    await json(route, {
      ...eventDetail,
      id: 'event-preliminary',
      name: 'Boda de María y Carlos',
      status: 'Preliminary',
    });
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

  if (path === '/api/client-portal/proposals' && method === 'GET') {
    await json(route, [portalProposalSummary()]);
    return;
  }

  if (path === '/api/client-portal/proposals/proposal-1' && method === 'GET') {
    await json(route, portalProposal());
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

function portalProposalSummary(): object {
  return {
    id: 'proposal-1',
    proposalNumber: 'P-20260728-ABC123',
    prospectId: null,
    clientId: 'client-1',
    eventId: 'event-1',
    targetDisplayName: 'Ana Martínez',
    status: 'Sent',
    currentVersionNumber: 1,
    currencyCode: 'MXN',
    validUntil: '2027-02-01T12:00:00Z',
    grandTotal: 14500,
    updatedAt: '2026-07-28T12:00:00Z',
  };
}

function portalProposal(): object {
  return {
    ...publicProposal({
      prospectCreated: false,
      prospectConverted: true,
      activityCreated: false,
      serviceCreated: true,
      packageCreated: true,
      proposalCreated: true,
      proposalStatus: 'Sent',
      proposalVersion: 1,
    }),
    recipientName: 'Ana Martínez',
  };
}

function prospectSummary(commercial: CommercialState): object {
  return {
    id: 'prospect-1',
    displayName: 'María Hernández',
    email: 'maria@example.com',
    phone: '+52 899 123 4567',
    eventTypeInterest: 'Boda',
    estimatedEventDate: '2027-02-14',
    estimatedBudget: 180000,
    currencyCode: 'MXN',
    assignedUserId: 'user-1',
    status: commercial.prospectConverted ? 'Won' : 'Opportunity',
    updatedAt: '2026-07-28T12:00:00Z',
  };
}

function prospectActivity(): object {
  return {
    id: 'activity-1',
    activityType: 'FollowUp',
    subject: 'Enviar opciones iniciales',
    description: 'Compartir catálogo de producción.',
    scheduledAt: '2026-08-01T16:00:00Z',
    completedAt: null,
    assignedUserId: 'user-1',
    visibility: 'Internal',
    createdBy: 'user-1',
    createdAt: '2026-07-28T12:00:00Z',
  };
}

function prospectDetail(commercial: CommercialState): object {
  return {
    ...prospectSummary(commercial),
    firstName: 'María',
    lastName: 'Hernández',
    companyName: null,
    source: 'Instagram',
    estimatedGuestCount: 140,
    city: 'Matamoros',
    notes: 'Prefiere contacto por WhatsApp.',
    lostReason: null,
    convertedClientId: commercial.prospectConverted ? 'client-1' : null,
    activities: commercial.activityCreated ? [prospectActivity()] : [],
    statusHistory: [],
    createdAt: '2026-07-28T12:00:00Z',
    archivedAt: null,
  };
}

function catalogService(): object {
  return {
    id: 'service-1',
    name: 'Producción integral',
    description: 'Planeación y coordinación.',
    category: 'Producción',
    pricingType: 'Fixed',
    basePrice: 12500,
    currencyCode: 'MXN',
    taxBehavior: 'Exclusive',
    isNegotiable: true,
    isActive: true,
    sortOrder: 0,
    updatedAt: '2026-07-28T12:00:00Z',
    archivedAt: null,
  };
}

function catalogPackage(): object {
  return {
    id: 'package-1',
    name: 'Celebración esencial',
    description: 'Paquete inicial.',
    basePrice: 12500,
    currencyCode: 'MXN',
    isNegotiable: false,
    isActive: true,
    items: [
      {
        id: 'package-item-1',
        serviceCatalogItemId: 'service-1',
        serviceName: 'Producción integral',
        quantity: 1,
        isOptional: false,
        includedPrice: 12500,
        sortOrder: 0,
      },
    ],
    updatedAt: '2026-07-28T12:00:00Z',
    archivedAt: null,
  };
}

function draftLine(): object {
  return {
    id: 'draft-line-1',
    description: 'Producción integral',
    serviceCatalogItemId: 'service-1',
    packageId: null,
    quantity: 1,
    unitPrice: 12500,
    discountType: 'None',
    discountValue: 0,
    taxRate: 16,
    lineSubtotal: 12500,
    lineDiscount: 0,
    lineTax: 2000,
    lineTotal: 14500,
    isOptional: false,
    sortOrder: 0,
  };
}

function totals(): object {
  return {
    subtotal: 12500,
    discountTotal: 0,
    generalDiscountTotal: 0,
    couponDiscountTotal: 0,
    taxTotal: 2000,
    grandTotal: 14500,
  };
}

function proposalVersion(versionNumber: number): object {
  return {
    id: `version-${versionNumber}`,
    versionNumber,
    totals: totals(),
    currencyCode: 'MXN',
    validUntil: '2027-02-01T12:00:00Z',
    sharedIntroduction: 'Una propuesta preparada especialmente para tu evento.',
    sharedTerms: 'Vigencia de catorce días.',
    couponCode: null,
    lines: [draftLine()],
    publishedAt: '2026-07-28T12:00:00Z',
  };
}

function proposalSummary(commercial: CommercialState): object {
  return {
    id: 'proposal-1',
    proposalNumber: 'P-20260728-ABC123',
    prospectId: 'prospect-1',
    clientId: null,
    eventId: null,
    targetDisplayName: 'María Hernández',
    status: commercial.proposalStatus,
    currentVersionNumber: commercial.proposalVersion,
    currencyCode: 'MXN',
    validUntil: '2027-02-01T12:00:00Z',
    grandTotal: commercial.proposalVersion ? 14500 : null,
    updatedAt: '2026-07-28T12:00:00Z',
  };
}

function proposalDetail(commercial: CommercialState): object {
  return {
    ...proposalSummary(commercial),
    sharedIntroduction: 'Una propuesta preparada especialmente para tu evento.',
    sharedTerms: 'Vigencia de catorce días.',
    internalNotes: null,
    generalDiscountType: 'None',
    generalDiscountValue: 0,
    couponId: null,
    draftTotals: totals(),
    draftLines: commercial.proposalCreated ? [draftLine()] : [],
    versions: Array.from({ length: commercial.proposalVersion }, (_, index) => ({
      id: `version-${index + 1}`,
      versionNumber: index + 1,
      grandTotal: 14500,
      currencyCode: 'MXN',
      validUntil: '2027-02-01T12:00:00Z',
      publishedAt: '2026-07-28T12:00:00Z',
    })),
    comments: [],
    acceptedVersionId:
      commercial.proposalStatus === 'Accepted' ? `version-${commercial.proposalVersion}` : null,
    acceptedAt: commercial.proposalStatus === 'Accepted' ? '2026-07-28T14:00:00Z' : null,
    rejectedAt: null,
    createdAt: '2026-07-28T12:00:00Z',
    updatedAt: '2026-07-28T12:00:00Z',
  };
}

function publicComment(): object {
  return {
    id: 'comment-1',
    proposalVersionId: 'version-1',
    proposalLineId: null,
    authorUserId: null,
    authorDisplayName: 'María Hernández',
    content: 'Quisiera ajustar un concepto.',
    visibility: 'ClientShared',
    status: 'Pending',
    parentCommentId: null,
    createdAt: '2026-07-28T13:00:00Z',
  };
}

function publicProposal(commercial: CommercialState): object {
  return {
    proposalId: 'proposal-1',
    versionId: `version-${commercial.proposalVersion}`,
    proposalNumber: 'P-20260728-ABC123',
    versionNumber: commercial.proposalVersion,
    organizationName: 'Armonía Eventos',
    recipientName: 'María Hernández',
    eventSummary: 'Boda · 14/02/2027 · Matamoros',
    status: commercial.proposalStatus,
    currencyCode: 'MXN',
    validUntil: '2027-02-01T12:00:00Z',
    sharedIntroduction: 'Una propuesta preparada especialmente para tu evento.',
    sharedTerms: 'Vigencia de catorce días.',
    totals: totals(),
    lines: [
      {
        id: 'line-1',
        description: 'Producción integral',
        quantity: 1,
        unitPrice: 12500,
        lineDiscount: 0,
        lineTax: 2000,
        lineTotal: 14500,
        isOptional: false,
        sortOrder: 0,
      },
    ],
    comments: [],
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
