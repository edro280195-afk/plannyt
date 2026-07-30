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
  prepareContractingScenario(): void;
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
  'contract-templates.view',
  'contract-templates.manage',
  'contracts.view',
  'contracts.create',
  'contracts.update-draft',
  'contracts.publish',
  'contracts.send',
  'contracts.cancel',
  'contracts.upload-external',
  'contracts.validate-external',
  'contracts.view-internal',
  'signatures.view',
  'signatures.manage-signers',
  'signatures.create-request',
  'signatures.revoke-request',
  'signatures.countersign',
  'signatures.view-evidence',
  'payment-plans.view',
  'payment-plans.create',
  'payment-plans.update-draft',
  'payment-plans.activate',
  'payment-plans.cancel',
  'payments.view',
  'payments.create',
  'payments.approve',
  'payments.reject',
  'payments.cancel',
  'payments.refund',
  'payments.view-internal',
  'events.confirm',
  'guests.view',
  'guests.create',
  'guests.update',
  'guests.archive',
  'guests.import',
  'guests.export',
  'guests.view-private',
  'guests.manage-tags',
  'invitation-groups.view',
  'invitation-groups.create',
  'invitation-groups.update',
  'invitation-groups.archive',
  'invitation-groups.manage-capacity',
  'invitation-groups.view-private',
  'invitation-designs.view',
  'invitation-designs.create',
  'invitation-designs.update-draft',
  'invitation-designs.submit-review',
  'invitation-designs.approve',
  'invitation-designs.publish',
  'invitation-designs.archive',
  'invitation-designs.manage-templates',
  'guest-links.view',
  'guest-links.generate',
  'guest-links.regenerate',
  'guest-links.revoke',
  'guest-links.mark-shared',
  'rsvp-settings.view',
  'rsvp-responses.view',
  'rsvp-responses.create-manual',
  'rsvp-responses.correct',
  'guest-sensitive-data.view',
  'guest-sensitive-data.manage',
  'guest-sensitive-data.export',
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
  contractCreated: boolean;
  contractPublished: boolean;
  clientSignerAdded: boolean;
  clientSigned: boolean;
  plannerSigned: boolean;
  planStatus: 'None' | 'Draft' | 'Active';
  paymentStatus: 'None' | 'PendingReview' | 'Approved';
  receiptUploaded: boolean;
  paymentAllocated: boolean;
  eventConfirmed: boolean;
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
  api: [
    async ({ context }, use) => {
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
        contractCreated: false,
        contractPublished: false,
        clientSignerAdded: false,
        clientSigned: false,
        plannerSigned: false,
        planStatus: 'None',
        paymentStatus: 'None',
        receiptUploaded: false,
        paymentAllocated: false,
        eventConfirmed: false,
      };
      const api: ApiMock = {
        requests,
        useProfile(value): void {
          profile = value;
        },
        prepareContractingScenario(): void {
          commercial.prospectCreated = true;
          commercial.prospectConverted = true;
          commercial.proposalCreated = true;
          commercial.proposalStatus = 'Accepted';
          commercial.proposalVersion = 1;
        },
        requestFor(method, path) {
          return requests.find((request) => request.method === method && request.path === path);
        },
      };

      await context.route('**/api/**', async (route) => {
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
          body: parseRecordedBody(bodyText),
        });
        await fulfillApi(route, profile, commercial);
      });

      await use(api);
    },
    { auto: true },
  ],
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

  if (path === '/api/organizations/org-1/contract-templates' && method === 'GET') {
    await json(route, []);
    return;
  }

  if (path === '/api/organizations/org-1/contracts' && method === 'GET') {
    await json(route, commercial.contractCreated ? [contractSummary(commercial)] : []);
    return;
  }

  if (path === '/api/organizations/org-1/contracts/from-proposal' && method === 'POST') {
    commercial.contractCreated = true;
    await json(route, contractDetail(commercial), 201);
    return;
  }

  if (path === '/api/organizations/org-1/contracts/contract-1' && method === 'GET') {
    await json(route, contractDetail(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/contracts/contract-1/publish' && method === 'POST') {
    commercial.contractPublished = true;
    await json(route, contractDetail(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/contracts/contract-1/signers' && method === 'POST') {
    commercial.clientSignerAdded = true;
    await json(route, contractSigners(commercial).at(-1) ?? {}, 201);
    return;
  }

  if (
    path === '/api/organizations/org-1/contracts/contract-1/signers/signer-client/requests' &&
    method === 'POST'
  ) {
    await json(route, {
      id: 'signature-request-1',
      contractVersionId: 'contract-version-1',
      contractSignerId: 'signer-client',
      expiresAt: '2027-02-07T12:00:00Z',
      signingUrl: 'http://127.0.0.1:4200/sign/signature-token',
    });
    return;
  }

  if (
    path === '/api/organizations/org-1/contracts/contract-1/signers/signer-planner/sign' &&
    method === 'POST'
  ) {
    commercial.plannerSigned = true;
    await json(route, contractDetail(commercial));
    return;
  }

  if (path === '/api/public/signatures/signature-token' && method === 'GET') {
    await json(route, publicSignature(commercial));
    return;
  }

  if (path === '/api/public/signatures/signature-token/sign' && method === 'POST') {
    commercial.clientSigned = true;
    await json(route, publicSignature(commercial));
    return;
  }

  if (path === '/api/public/signatures/rejected-token/decline' && method === 'POST') {
    await json(route, {});
    return;
  }

  if (path === '/api/public/signatures/rejected-token' && method === 'GET') {
    await json(route, { ...publicSignature(commercial), canSign: true });
    return;
  }

  if (
    path === '/api/organizations/org-1/events/event-1/contracting-readiness' &&
    method === 'GET'
  ) {
    await json(route, contractingReadiness(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/events/event-1/confirm' && method === 'POST') {
    commercial.eventConfirmed = true;
    await json(route, contractingReadiness(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/payment-plans' && method === 'GET') {
    await json(route, commercial.planStatus === 'None' ? [] : [paymentPlan(commercial)]);
    return;
  }

  if (path === '/api/organizations/org-1/payment-plans' && method === 'POST') {
    commercial.planStatus = 'Draft';
    await json(route, paymentPlan(commercial), 201);
    return;
  }

  if (path === '/api/organizations/org-1/payment-plans/plan-1/activate' && method === 'POST') {
    commercial.planStatus = 'Active';
    await json(route, paymentPlan(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/payments' && method === 'GET') {
    await json(route, commercial.paymentStatus === 'None' ? [] : [paymentRecord(commercial)]);
    return;
  }

  if (path === '/api/organizations/org-1/payments/payment-1/approve' && method === 'POST') {
    commercial.paymentStatus = 'Approved';
    await json(route, paymentRecord(commercial));
    return;
  }

  if (path === '/api/organizations/org-1/payments/payment-1/allocations' && method === 'POST') {
    commercial.paymentAllocated = true;
    await json(route, paymentRecord(commercial));
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

  if (path === '/api/client-portal/events/event-1/contracting-readiness' && method === 'GET') {
    await json(route, contractingReadiness(commercial));
    return;
  }

  if (path === '/api/client-portal/contracts' && method === 'GET') {
    await json(route, commercial.contractCreated ? [portalContractSummary(commercial)] : []);
    return;
  }

  if (path === '/api/client-portal/contracts/contract-1' && method === 'GET') {
    await json(route, portalContract(commercial));
    return;
  }

  if (path === '/api/client-portal/payment-plans' && method === 'GET') {
    await json(route, commercial.planStatus === 'None' ? [] : [paymentPlan(commercial)]);
    return;
  }

  if (path === '/api/client-portal/payments' && method === 'GET') {
    await json(route, commercial.paymentStatus === 'None' ? [] : [portalPayment(commercial)]);
    return;
  }

  if (path === '/api/client-portal/payments' && method === 'POST') {
    commercial.paymentStatus = 'PendingReview';
    await json(route, portalPayment(commercial), 201);
    return;
  }

  if (path === '/api/client-portal/payments/payment-1/receipt' && method === 'POST') {
    commercial.receiptUploaded = true;
    await json(route, paymentReceipt());
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
        permissions:
          profile === 'limited'
            ? ['events.view', 'rsvp-settings.view', 'rsvp-responses.view']
            : ownerPermissions,
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
      contractCreated: false,
      contractPublished: false,
      clientSignerAdded: false,
      clientSigned: false,
      plannerSigned: false,
      planStatus: 'None',
      paymentStatus: 'None',
      receiptUploaded: false,
      paymentAllocated: false,
      eventConfirmed: false,
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

function contractStatus(commercial: CommercialState): string {
  if (commercial.clientSigned && commercial.plannerSigned) {
    return 'Completed';
  }
  if (commercial.clientSigned || commercial.plannerSigned) {
    return 'PartiallySigned';
  }
  return commercial.contractPublished ? 'Ready' : 'Draft';
}

function contractSummary(commercial: CommercialState): object {
  return {
    id: 'contract-1',
    eventId: 'event-1',
    clientId: 'client-1',
    contractNumber: 'C-20260728-ABC123',
    name: 'Contrato de prestación de servicios',
    sourceType: 'GeneratedFromProposal',
    status: contractStatus(commercial),
    currentVersionNumber: 1,
    contractGrandTotal: 14500,
    currencyCode: 'MXN',
    updatedAt: '2026-07-28T12:00:00Z',
  };
}

function contractVersion(commercial: CommercialState): object {
  return {
    id: 'contract-version-1',
    versionNumber: 1,
    templateId: null,
    sourceProposalVersionId: 'version-1',
    renderedContent:
      '<h1>Contrato de prestación de servicios</h1><p>Documento completo para Ana Martínez.</p>',
    documentFileName: commercial.contractPublished ? 'contrato-C-20260728-ABC123-v1.pdf' : null,
    documentSizeBytes: commercial.contractPublished ? 4096 : null,
    documentSha256: commercial.contractPublished
      ? 'c7a2f5e89d9e7f6ef912bb62ca014678e2fd42ca8fbd67fe84fd5fb667ae1111'
      : null,
    consentText:
      'Declaro que revisé el documento y acepto utilizar medios electrónicos para firmarlo.',
    validUntil: '2027-02-07T12:00:00Z',
    createdAt: '2026-07-28T12:00:00Z',
    publishedAt: commercial.contractPublished ? '2026-07-28T13:00:00Z' : null,
    supersededAt: null,
  };
}

function contractParties(): object[] {
  return [
    {
      id: 'party-planner',
      partyType: 'PlannerOrganization',
      clientId: null,
      organizationPartyId: 'org-1',
      displayName: 'Armonía Eventos',
      legalName: null,
      taxId: null,
      address: null,
      sortOrder: 0,
    },
    {
      id: 'party-client',
      partyType: 'Client',
      clientId: 'client-1',
      organizationPartyId: null,
      displayName: 'Ana Martínez',
      legalName: null,
      taxId: null,
      address: null,
      sortOrder: 1,
    },
  ];
}

function contractSigners(commercial: CommercialState): object[] {
  const signers: object[] = [
    {
      id: 'signer-planner',
      contractPartyId: 'party-planner',
      personId: null,
      userAccountId: 'user-1',
      name: 'Mariana Torres',
      email: 'mariana@armonia.mx',
      signerRole: 'Representante de la organización',
      signingOrder: 2,
      isRequired: true,
      status: commercial.plannerSigned ? 'Signed' : 'Pending',
      signedAt: commercial.plannerSigned ? '2026-07-28T15:00:00Z' : null,
      declinedAt: null,
    },
  ];
  if (commercial.clientSignerAdded) {
    signers.push({
      id: 'signer-client',
      contractPartyId: 'party-client',
      personId: null,
      userAccountId: 'user-2',
      name: 'Ana Martínez',
      email: 'ana@example.com',
      signerRole: 'Cliente contratante',
      signingOrder: 1,
      isRequired: true,
      status: commercial.clientSigned ? 'Signed' : 'Invited',
      signedAt: commercial.clientSigned ? '2026-07-28T14:00:00Z' : null,
      declinedAt: null,
    });
  }
  return signers;
}

function contractRequirements(): object {
  return {
    requireAcceptedProposal: true,
    requireCompletedContract: true,
    depositRequirementType: 'PercentageOfContract',
    depositRequirementValue: 20,
    requiredDepositAmount: 2900,
    currencyCode: 'MXN',
    confirmationMode: 'ManualAfterRequirements',
    createdAt: '2026-07-28T12:00:00Z',
  };
}

function contractDetail(commercial: CommercialState): object {
  return {
    ...contractSummary(commercial),
    organizationId: 'org-1',
    acceptedProposalId: 'proposal-1',
    acceptedProposalVersionId: 'version-1',
    versions: [contractVersion(commercial)],
    parties: contractParties(),
    signers: contractSigners(commercial),
    requirements: contractRequirements(),
    createdAt: '2026-07-28T12:00:00Z',
    completedAt:
      commercial.clientSigned && commercial.plannerSigned ? '2026-07-28T15:00:00Z' : null,
    cancelledAt: null,
    cancellationReason: null,
  };
}

function publicSignature(commercial: CommercialState): object {
  return {
    contractId: 'contract-1',
    contractVersionId: 'contract-version-1',
    contractSignerId: 'signer-client',
    contractNumber: 'C-20260728-ABC123',
    name: 'Contrato de prestación de servicios',
    versionNumber: 1,
    organizationName: 'Armonía Eventos',
    signerName: 'Ana Martínez',
    signerEmail: 'ana@example.com',
    parties: ['Armonía Eventos', 'Ana Martínez'],
    renderedContent:
      '<h1>Contrato de prestación de servicios</h1><p>Documento completo para Ana Martínez.</p>',
    validUntil: '2027-02-07T12:00:00Z',
    consentText:
      'Declaro que revisé el documento y acepto utilizar medios electrónicos para firmarlo.',
    documentSha256: 'c7a2f5e89d9e7f6ef912bb62ca014678e2fd42ca8fbd67fe84fd5fb667ae1111',
    signers: contractSigners(commercial).map((signer) => {
      const value = signer as {
        signerRole: string;
        status: string;
        signedAt: string | null;
      };
      return {
        signerRole: value.signerRole,
        status: value.status,
        signedAt: value.signedAt,
      };
    }),
    canSign: !commercial.clientSigned,
  };
}

function paymentPlan(commercial: CommercialState): object {
  const approvedAmount = commercial.paymentAllocated ? 2900 : 0;
  return {
    id: 'plan-1',
    eventId: 'event-1',
    clientId: 'client-1',
    contractId: 'contract-1',
    proposalVersionId: 'version-1',
    currencyCode: 'MXN',
    totalAmount: 14500,
    status: commercial.planStatus,
    approvedAmount,
    pendingAmount: 14500 - approvedAmount,
    installments: [
      {
        id: 'installment-deposit',
        sequenceNumber: 1,
        description: 'Anticipo de contratación',
        dueDate: '2026-08-04',
        amount: 2900,
        approvedAmount,
        pendingAmount: 2900 - approvedAmount,
        installmentType: 'Deposit',
        status: commercial.paymentAllocated ? 'Paid' : 'Pending',
      },
      {
        id: 'installment-final',
        sequenceNumber: 2,
        description: 'Pago final',
        dueDate: '2027-01-30',
        amount: 11600,
        approvedAmount: 0,
        pendingAmount: 11600,
        installmentType: 'FinalPayment',
        status: 'Pending',
      },
    ],
    createdAt: '2026-07-28T12:00:00Z',
    updatedAt: '2026-07-28T12:00:00Z',
  };
}

function paymentReceipt(): object {
  return {
    documentId: 'receipt-document-1',
    fileName: 'comprobante.png',
    mimeType: 'image/png',
    sizeBytes: 128,
    createdAt: '2026-07-28T16:00:00Z',
  };
}

function paymentRecord(commercial: CommercialState): object {
  return {
    id: 'payment-1',
    eventId: 'event-1',
    clientId: 'client-1',
    paymentPlanId: 'plan-1',
    paymentDate: '2026-07-28',
    amount: 2900,
    currencyCode: 'MXN',
    method: 'BankTransfer',
    reference: 'SPEI-123',
    status: commercial.paymentStatus,
    notesShared: 'Anticipo del evento',
    internalNotes: 'Visible solo para finanzas',
    submittedByClient: true,
    rejectionReason: null,
    allocations: commercial.paymentAllocated
      ? [{ id: 'allocation-1', paymentInstallmentId: 'installment-deposit', amount: 2900 }]
      : [],
    receipts: commercial.receiptUploaded ? [paymentReceipt()] : [],
    createdAt: '2026-07-28T16:00:00Z',
    updatedAt: '2026-07-28T16:00:00Z',
  };
}

function portalPayment(commercial: CommercialState): object {
  const record = paymentRecord(commercial) as {
    id: string;
    paymentDate: string;
    amount: number;
    currencyCode: string;
    method: string;
    reference: string | null;
    status: string;
    notesShared: string | null;
    rejectionReason: string | null;
    receipts: object[];
    createdAt: string;
  };
  return {
    id: record.id,
    paymentDate: record.paymentDate,
    amount: record.amount,
    currencyCode: record.currencyCode,
    method: record.method,
    reference: record.reference,
    status: record.status,
    notesShared: record.notesShared,
    rejectionReason: record.rejectionReason,
    receipts: record.receipts,
    createdAt: record.createdAt,
  };
}

function contractingReadiness(commercial: CommercialState): object {
  const contractCompleted = commercial.clientSigned && commercial.plannerSigned;
  const depositSatisfied = commercial.paymentAllocated;
  const readyForConfirmation = contractCompleted && depositSatisfied;
  const missingRequirements: string[] = [];
  if (!contractCompleted) {
    missingRequirements.push('Contrato completado');
  }
  if (!depositSatisfied) {
    missingRequirements.push('Anticipo cubierto');
  }
  return {
    proposalAccepted: true,
    contractCompleted,
    requiredDepositAmount: 2900,
    approvedDepositAmount: depositSatisfied ? 2900 : 0,
    depositSatisfied,
    missingRequiredSigners: contractCompleted
      ? 0
      : Number(!commercial.clientSigned) + Number(!commercial.plannerSigned),
    missingRequirements,
    readyForConfirmation,
    confirmationMode: 'ManualAfterRequirements',
    eventStatus: commercial.eventConfirmed ? 'Confirmed' : 'Preliminary',
  };
}

function portalContractSummary(commercial: CommercialState): object {
  return {
    id: 'contract-1',
    eventId: 'event-1',
    contractNumber: 'C-20260728-ABC123',
    name: 'Contrato de prestación de servicios',
    status: contractStatus(commercial),
    currentVersionNumber: 1,
    hasPendingSignature: commercial.clientSignerAdded && !commercial.clientSigned,
    hasFinalDocument: commercial.clientSigned && commercial.plannerSigned,
  };
}

function portalContract(commercial: CommercialState): object {
  return {
    id: 'contract-1',
    eventId: 'event-1',
    contractNumber: 'C-20260728-ABC123',
    name: 'Contrato de prestación de servicios',
    status: contractStatus(commercial),
    version: contractVersion(commercial),
    parties: contractParties(),
    signers: contractSigners(commercial).map((signer) => {
      const value = signer as {
        signerRole: string;
        status: string;
        signedAt: string | null;
      };
      return {
        signerRole: value.signerRole,
        status: value.status,
        signedAt: value.signedAt,
      };
    }),
    pendingSignerId:
      commercial.clientSignerAdded && !commercial.clientSigned ? 'signer-client' : null,
    pendingSignerName:
      commercial.clientSignerAdded && !commercial.clientSigned ? 'Ana Martínez' : null,
    hasFinalDocument: commercial.clientSigned && commercial.plannerSigned,
  };
}

function parseRecordedBody(bodyText: string | null): unknown {
  if (!bodyText) {
    return null;
  }
  try {
    return JSON.parse(bodyText) as unknown;
  } catch {
    return bodyText;
  }
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
