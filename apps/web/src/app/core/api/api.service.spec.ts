import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import {
  AcceptInvitationRequest,
  CreateClientRequest,
  EventDetailsRequest,
  LoginRequest,
  RegisterAndAcceptInvitationRequest,
  RegisterPlannerRequest,
  UpdateClientRequest,
  UpdateOrganizationRequest,
  UpsertParticipantRequest,
} from '../models/api.models';
import { ApiService } from './api.service';

describe('ApiService', () => {
  const baseUrl = environment.apiBaseUrl;
  const organizationUrl = `${baseUrl}/organizations/org-1`;
  const eventUrl = `${organizationUrl}/events/event-1`;
  let service: ApiService;
  let controller: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ApiService);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    controller.verify();
  });

  it('maps all authentication operations to their HTTP contracts', () => {
    const register: RegisterPlannerRequest = {
      email: 'planner@plannyt.mx',
      password: 'a-secure-password',
      firstName: 'Mariana',
      lastName: 'López',
      organizationName: 'Armonía',
      organizationType: 'Agency',
      timeZone: 'America/Matamoros',
      countryCode: 'MX',
      currencyCode: 'MXN',
    };
    const login: LoginRequest = {
      email: register.email,
      password: register.password,
      isPersistent: true,
    };

    service.registerPlanner(register).subscribe();
    expectRequest('POST', `${baseUrl}/auth/register-planner`, register);
    service.login(login).subscribe();
    expectRequest('POST', `${baseUrl}/auth/login`, login);
    service.refresh().subscribe();
    expectRequest('POST', `${baseUrl}/auth/refresh`, null);
    service.logout().subscribe();
    expectRequest('POST', `${baseUrl}/auth/logout`, null);
    service.logoutAll().subscribe();
    expectRequest('POST', `${baseUrl}/auth/logout-all`, null);
    service.getMe().subscribe();
    expectRequest('GET', `${baseUrl}/auth/me`);
  });

  it('maps organization and membership operations', () => {
    const update: UpdateOrganizationRequest = {
      name: 'Armonía Eventos',
      organizationType: 'Agency',
      timeZone: 'America/Matamoros',
      countryCode: 'MX',
      currencyCode: 'MXN',
    };

    service.getOrganization('org-1').subscribe();
    expectRequest('GET', organizationUrl);
    service.updateOrganization('org-1', update).subscribe();
    expectRequest('PUT', organizationUrl, update);
    service.getMembers('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/members`);
    service.revokeMember('org-1', 'membership-1').subscribe();
    expectRequest('DELETE', `${organizationUrl}/members/membership-1`);
    service.inviteMember('org-1', 'coordinator@plannyt.mx', 'Coordinator').subscribe();
    expectRequest('POST', `${organizationUrl}/members/invitations`, {
      targetEmail: 'coordinator@plannyt.mx',
      intendedOrganizationRole: 'Coordinator',
    });
    service.revokeOrganizationInvitation('org-1', 'invitation-1').subscribe();
    expectRequest('DELETE', `${organizationUrl}/members/invitations/invitation-1`);
  });

  it('maps client and contact operations including pagination', () => {
    const create: CreateClientRequest = {
      clientType: 'Person',
      displayName: 'Sofía Campos',
      companyName: null,
      source: 'Recomendación',
      person: {
        firstName: 'Sofía',
        lastName: 'Campos',
        contactEmail: 'sofia@example.com',
        contactPhone: null,
        preferredLanguage: 'es',
        timeZone: 'America/Matamoros',
      },
    };
    const update: UpdateClientRequest = {
      displayName: create.displayName,
      companyName: null,
      source: 'Instagram',
      person: create.person,
    };
    const contact = {
      firstName: 'Diego',
      lastName: 'Campos',
      contactEmail: 'diego@example.com',
      contactPhone: null,
      preferredLanguage: 'es',
      timeZone: 'America/Matamoros',
      contactRole: 'Pareja',
      isPrimary: true,
    };

    service.getClients('org-1', 'Sofía', 2, 25).subscribe();
    const list = controller.expectOne(
      `${organizationUrl}/clients?page=2&pageSize=25&search=Sof%C3%ADa`,
    );
    expect(list.request.method).toBe('GET');
    list.flush({ items: [], page: 2, pageSize: 25, totalCount: 0 });
    service.getClient('org-1', 'client-1').subscribe();
    expectRequest('GET', `${organizationUrl}/clients/client-1`);
    service.createClient('org-1', create).subscribe();
    expectRequest('POST', `${organizationUrl}/clients`, create);
    service.updateClient('org-1', 'client-1', update).subscribe();
    expectRequest('PUT', `${organizationUrl}/clients/client-1`, update);
    service.archiveClient('org-1', 'client-1').subscribe();
    expectRequest('POST', `${organizationUrl}/clients/client-1/archive`, null);
    service.addClientContact('org-1', 'client-1', contact).subscribe();
    expectRequest('POST', `${organizationUrl}/clients/client-1/contacts`, contact);
  });

  it('maps event lifecycle and client relationships', () => {
    const details: EventDetailsRequest = {
      name: 'Boda Sofía y Diego',
      eventType: 'Boda',
      startDateTime: '2027-03-20T18:00:00-06:00',
      endDateTime: null,
      timeZone: 'America/Matamoros',
      city: 'Monterrey',
      countryCode: 'MX',
      sharedDescription: 'Una celebración al aire libre.',
      estimatedGuestCount: 180,
    };
    const relation = {
      clientId: 'client-1',
      relationshipType: 'PrimaryClient' as const,
      isPrimary: true,
      hasTransferAuthority: true,
    };

    service.getEvents('org-1', 'Boda', 3, 10).subscribe();
    const list = controller.expectOne(`${organizationUrl}/events?page=3&pageSize=10&search=Boda`);
    expect(list.request.method).toBe('GET');
    list.flush({ items: [], page: 3, pageSize: 10, totalCount: 0 });
    service.getEvent('org-1', 'event-1').subscribe();
    expectRequest('GET', eventUrl);
    service.createEvent('org-1', details).subscribe();
    expectRequest('POST', `${organizationUrl}/events`, details);
    service.updateEvent('org-1', 'event-1', details).subscribe();
    expectRequest('PUT', eventUrl, details);
    service.changeEventStatus('org-1', 'event-1', 'Confirmed', 'Contrato firmado').subscribe();
    expectRequest('POST', `${eventUrl}/status`, {
      newStatus: 'Confirmed',
      reason: 'Contrato firmado',
    });
    service.getEventClients('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/clients`);
    service.addEventClient('org-1', 'event-1', relation).subscribe();
    expectRequest('POST', `${eventUrl}/clients`, relation);
    service.removeEventClient('org-1', 'event-1', 'relation-1').subscribe();
    expectRequest('DELETE', `${eventUrl}/clients/relation-1`);
  });

  it('maps participants, access and document operations', () => {
    const participant: UpsertParticipantRequest = {
      firstName: 'Sofía',
      lastName: 'Campos',
      contactEmail: 'sofia@example.com',
      contactPhone: null,
      preferredLanguage: 'es',
      timeZone: 'America/Matamoros',
      participantType: 'Novia',
      displayOrder: 1,
      isVisibleToClient: true,
      sharedDescription: 'Cliente principal',
    };

    service.getParticipants('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/participants`);
    service.addParticipant('org-1', 'event-1', participant).subscribe();
    expectRequest('POST', `${eventUrl}/participants`, participant);
    service.getEventAccesses('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/access`);
    service.inviteEventAccess('org-1', 'event-1', 'sofia@example.com', 'ClientPrimary').subscribe();
    expectRequest('POST', `${eventUrl}/access/invitations`, {
      targetEmail: 'sofia@example.com',
      intendedEventRole: 'ClientPrimary',
    });
    service.revokeEventInvitation('org-1', 'event-1', 'invitation-1').subscribe();
    expectRequest('DELETE', `${eventUrl}/access/invitations/invitation-1`);
    service.revokeEventAccess('org-1', 'event-1', 'access-1').subscribe();
    expectRequest('DELETE', `${eventUrl}/access/access-1`);
    service.getDocuments('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/documents`);

    const file = new File(['pdf'], 'contrato.pdf', {
      type: 'application/pdf',
    });
    service.uploadDocument('org-1', 'event-1', file, 'Contrato', 'ClientShared').subscribe();
    const upload = controller.expectOne(`${eventUrl}/documents`);
    expect(upload.request.method).toBe('POST');
    const form = upload.request.body as FormData;
    expect(form.get('file')).toBe(file);
    expect(form.get('documentType')).toBe('Contrato');
    expect(form.get('visibility')).toBe('ClientShared');
    upload.flush({});

    service.deleteDocument('org-1', 'event-1', 'document-1').subscribe();
    expectRequest('DELETE', `${eventUrl}/documents/document-1`);
    service.downloadAdminDocument('org-1', 'event-1', 'document-1').subscribe();
    expectBlobRequest(`${eventUrl}/documents/document-1/download`);
  });

  it('maps invitations and the isolated client portal', () => {
    const token = 'token / private';
    const encodedToken = 'token%20%2F%20private';
    const register: RegisterAndAcceptInvitationRequest = {
      password: 'a-secure-password',
      firstName: 'Sofía',
      lastName: 'Campos',
      contactPhone: null,
      preferredLanguage: 'es',
      timeZone: 'America/Matamoros',
    };
    const acceptance: AcceptInvitationRequest = {
      firstName: null,
      lastName: null,
      contactPhone: null,
      preferredLanguage: 'es',
      timeZone: 'America/Matamoros',
    };

    service.getInvitation(token).subscribe();
    expectRequest('GET', `${baseUrl}/access-invitations/${encodedToken}`);
    service.registerAndAcceptInvitation(token, register).subscribe();
    expectRequest(
      'POST',
      `${baseUrl}/access-invitations/${encodedToken}/register-and-accept`,
      register,
    );
    service.acceptInvitation(token, acceptance).subscribe();
    expectRequest('POST', `${baseUrl}/access-invitations/${encodedToken}/accept`, acceptance);
    service.getPortalEvents().subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/events`);
    service.getPortalEvent('event-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/events/event-1`);
    service.getPortalDocuments('event-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/events/event-1/documents`);
    service.downloadPortalDocument('event-1', 'document-1').subscribe();
    expectBlobRequest(`${baseUrl}/client-portal/events/event-1/documents/document-1/download`);
  });

  it('maps CRM, catalog and proposal operations', () => {
    service
      .getProspects('org-1', {
        search: 'María',
        status: 'Opportunity',
        page: 1,
        pageSize: 100,
      })
      .subscribe();
    expectRequest(
      'GET',
      `${organizationUrl}/prospects?page=1&pageSize=100&search=Mar%C3%ADa&status=Opportunity`,
    );
    service.changeProspectStatus('org-1', 'prospect-1', 'Qualified', null).subscribe();
    expectRequest('POST', `${organizationUrl}/prospects/prospect-1/status`, {
      newStatus: 'Qualified',
      reason: null,
    });
    service.getCatalogServices('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/catalog/services`);
    service.getPackages('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/catalog/packages`);
    service.getCoupons('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/catalog/coupons`);
    service.getProposals('org-1', 'P-2026', 'Sent').subscribe();
    expectRequest(
      'GET',
      `${organizationUrl}/proposals?page=1&pageSize=100&search=P-2026&status=Sent`,
    );
    service.publishProposal('org-1', 'proposal-1').subscribe();
    expectRequest('POST', `${organizationUrl}/proposals/proposal-1/publish`, null);
    service.sendProposal('org-1', 'proposal-1', null).subscribe();
    expectRequest('POST', `${organizationUrl}/proposals/proposal-1/send`, { expiresAt: null });
  });

  it('maps private proposal access without an organization context', () => {
    const token = 'private/token';
    const publicUrl = `${baseUrl}/public/proposals/private%2Ftoken`;

    service.getPublicProposal(token).subscribe();
    expectRequest('GET', publicUrl);
    service.decidePublicProposal(token, 'accept', 'María', 'De acuerdo').subscribe();
    expectRequest('POST', `${publicUrl}/accept`, {
      authorDisplayName: 'María',
      reason: 'De acuerdo',
    });
    service.downloadPublicProposalPdf(token).subscribe();
    expectBlobRequest(`${publicUrl}/pdf`);
    service.getPortalProposals().subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/proposals`);
    service.getPortalProposal('proposal-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/proposals/proposal-1`);
    service.downloadPortalProposalPdf('proposal-1').subscribe();
    expectBlobRequest(`${baseUrl}/client-portal/proposals/proposal-1/pdf`);
  });

  it('maps complete prospect management operations', () => {
    const prospect = {
      displayName: 'María Hernández',
      firstName: 'María',
      lastName: 'Hernández',
      companyName: null,
      email: 'maria@example.com',
      phone: null,
      source: 'Instagram',
      eventTypeInterest: 'Boda',
      estimatedEventDate: '2027-02-14',
      estimatedGuestCount: 140,
      estimatedBudget: 180000,
      currencyCode: 'MXN',
      city: 'Matamoros',
      notes: null,
      assignedUserId: null,
    };
    const activity = {
      activityType: 'FollowUp' as const,
      subject: 'Llamar',
      description: null,
      scheduledAt: null,
      completedAt: null,
      assignedUserId: null,
      visibility: 'Internal' as const,
    };

    service.getProspect('org-1', 'prospect-1').subscribe();
    expectRequest('GET', `${organizationUrl}/prospects/prospect-1`);
    service.createProspect('org-1', prospect).subscribe();
    expectRequest('POST', `${organizationUrl}/prospects`, prospect);
    service.updateProspect('org-1', 'prospect-1', prospect).subscribe();
    expectRequest('PUT', `${organizationUrl}/prospects/prospect-1`, prospect);
    service.addProspectActivity('org-1', 'prospect-1', activity).subscribe();
    expectRequest('POST', `${organizationUrl}/prospects/prospect-1/activities`, activity);
    service.completeProspectActivity('org-1', 'prospect-1', 'activity-1').subscribe();
    expectRequest(
      'POST',
      `${organizationUrl}/prospects/prospect-1/activities/activity-1/complete`,
      null,
    );
    service.getProspectMatches('org-1', 'prospect-1').subscribe();
    expectRequest('GET', `${organizationUrl}/prospects/prospect-1/client-matches`);
    service
      .convertProspect('org-1', 'prospect-1', {
        existingClientId: null,
        newClientType: 'Person',
        confirmCreateDespiteMatches: true,
      })
      .subscribe();
    expectRequest('POST', `${organizationUrl}/prospects/prospect-1/convert`, {
      existingClientId: null,
      newClientType: 'Person',
      confirmCreateDespiteMatches: true,
    });
    const preliminary = {
      existingEventId: null,
      name: 'Boda',
      eventType: 'Wedding',
      startDateTime: '2027-02-14T18:00:00Z',
      timeZone: 'America/Matamoros',
      city: 'Matamoros',
      countryCode: 'MX',
      estimatedGuestCount: 140,
    };
    service.linkProspectPreliminaryEvent('org-1', 'prospect-1', preliminary).subscribe();
    expectRequest('POST', `${organizationUrl}/prospects/prospect-1/preliminary-event`, preliminary);
  });

  it('maps catalog mutations and proposal drafts', () => {
    const catalogService = {
      name: 'Producción',
      description: null,
      category: 'Operación',
      pricingType: 'Fixed' as const,
      basePrice: 1000,
      currencyCode: 'MXN',
      taxBehavior: 'Exclusive' as const,
      isNegotiable: true,
      isActive: true,
      sortOrder: 0,
    };
    const catalogPackage = {
      name: 'Esencial',
      description: null,
      basePrice: 1000,
      currencyCode: 'MXN',
      isNegotiable: false,
      isActive: true,
      items: [],
    };
    const coupon = {
      code: 'NUEVO',
      description: null,
      discountType: 'Percentage' as const,
      discountValue: 10,
      startsAt: '2026-07-01T00:00:00Z',
      endsAt: '2026-08-01T00:00:00Z',
      maximumUses: null,
      isActive: true,
    };
    const draft = {
      prospectId: 'prospect-1',
      clientId: null,
      eventId: null,
      currencyCode: 'MXN',
      validUntil: '2026-08-01T00:00:00Z',
      sharedIntroduction: null,
      sharedTerms: null,
      internalNotes: null,
      generalDiscountType: 'None' as const,
      generalDiscountValue: 0,
      couponId: null,
      lines: [
        {
          description: 'Producción',
          serviceCatalogItemId: 'service-1',
          packageId: null,
          quantity: 1,
          unitPrice: 1000,
          discountType: 'None' as const,
          discountValue: 0,
          taxRate: 16,
          isOptional: false,
          sortOrder: 0,
        },
      ],
    };

    service.createCatalogService('org-1', catalogService).subscribe();
    expectRequest('POST', `${organizationUrl}/catalog/services`, catalogService);
    service.updateCatalogService('org-1', 'service-1', catalogService).subscribe();
    expectRequest('PUT', `${organizationUrl}/catalog/services/service-1`, catalogService);
    service.createPackage('org-1', catalogPackage).subscribe();
    expectRequest('POST', `${organizationUrl}/catalog/packages`, catalogPackage);
    service.createCoupon('org-1', coupon).subscribe();
    expectRequest('POST', `${organizationUrl}/catalog/coupons`, coupon);
    service.getProposal('org-1', 'proposal-1').subscribe();
    expectRequest('GET', `${organizationUrl}/proposals/proposal-1`);
    service.createProposal('org-1', draft).subscribe();
    expectRequest('POST', `${organizationUrl}/proposals`, draft);
    service.updateProposalDraft('org-1', 'proposal-1', draft).subscribe();
    expectRequest('PUT', `${organizationUrl}/proposals/proposal-1/draft`, draft);
    service.downloadAdminProposalPdf('org-1', 'proposal-1', 'version-1').subscribe();
    expectBlobRequest(`${organizationUrl}/proposals/proposal-1/versions/version-1/pdf`);
    const comment = {
      proposalVersionId: 'version-1',
      proposalLineId: null,
      authorDisplayName: 'Mariana',
      content: 'Revisado',
      visibility: 'Internal' as const,
      parentCommentId: null,
    };
    service.addProposalComment('org-1', 'proposal-1', comment).subscribe();
    expectRequest('POST', `${organizationUrl}/proposals/proposal-1/comments`, comment);
    const publicComment = {
      authorDisplayName: 'María',
      content: 'Comentario',
      proposalLineId: null,
      parentCommentId: null,
    };
    service.addPublicProposalComment('private', publicComment).subscribe();
    expectRequest('POST', `${baseUrl}/public/proposals/private/comments`, publicComment);
  });

  function expectRequest(method: string, url: string, body?: unknown): TestRequest {
    const request = controller.expectOne(url);
    expect(request.request.method).toBe(method);
    if (body !== undefined) {
      expect(request.request.body).toEqual(body);
    }
    request.flush({});
    return request;
  }

  function expectBlobRequest(url: string): void {
    const request = controller.expectOne(url);
    expect(request.request.method).toBe('GET');
    expect(request.request.responseType).toBe('blob');
    request.flush(new Blob());
  }
});
