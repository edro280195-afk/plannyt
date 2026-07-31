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
  EventAccommodationOptionRequest,
  EventDetailsRequest,
  EventMenuOptionRequest,
  EventMenuRequest,
  EventTransportOptionRequest,
  LoginRequest,
  RegisterAndAcceptInvitationRequest,
  RegisterPlannerRequest,
  ReminderTemplateRequest,
  RsvpSubmissionRequest,
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

  it('links a preliminary event to a proposal', () => {
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
    service.linkProposalPreliminaryEvent('org-1', 'proposal-1', preliminary).subscribe();
    expectRequest('POST', `${organizationUrl}/proposals/proposal-1/preliminary-event`, preliminary);
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

  it('maps contract publication, signatures and readiness', () => {
    service.getContracts('org-1', 'event-1').subscribe();
    expectRequest('GET', `${organizationUrl}/contracts?eventId=event-1`);
    service
      .createContractFromProposal('org-1', {
        proposalId: 'proposal-1',
        name: 'Contrato',
        templateId: null,
        content: null,
        consentText: 'Acepto medios electrónicos.',
        validUntil: null,
      })
      .subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/from-proposal`, {
      proposalId: 'proposal-1',
      name: 'Contrato',
      templateId: null,
      content: null,
      consentText: 'Acepto medios electrónicos.',
      validUntil: null,
    });
    service.publishContract('org-1', 'contract-1').subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/contract-1/publish`, null);
    service.createSignatureRequest('org-1', 'contract-1', 'signer-1').subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/contract-1/signers/signer-1/requests`, {
      expiresAt: null,
    });
    service.getContractingReadiness('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/contracting-readiness`);
    service.confirmContractedEvent('org-1', 'event-1').subscribe();
    expectRequest('POST', `${eventUrl}/confirm`, null);
  });

  it('maps public signature and portal payment without leaking an organization id', () => {
    service.getPublicSignature('token/private').subscribe();
    expectRequest('GET', `${baseUrl}/public/signatures/token%2Fprivate`);
    service
      .submitPublicSignature('token/private', {
        signingMethod: 'Typed',
        declaredSignerName: 'Ana Martínez',
        acceptElectronicMeans: true,
        confirmDisplayedVersion: true,
        signatureDataUrl: null,
      })
      .subscribe();
    expectRequest('POST', `${baseUrl}/public/signatures/token%2Fprivate/sign`, {
      signingMethod: 'Typed',
      declaredSignerName: 'Ana Martínez',
      acceptElectronicMeans: true,
      confirmDisplayedVersion: true,
      signatureDataUrl: null,
    });
    service.getPortalContractingReadiness('event-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/events/event-1/contracting-readiness`);
    service.getPortalContracts().subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/contracts`);
    service.getPortalPaymentPlans().subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/payment-plans`);
    service
      .createPortalPayment({
        paymentPlanId: 'plan-1',
        paymentDate: '2026-07-28',
        amount: 2000,
        method: 'BankTransfer',
        reference: 'SPEI-123',
        notesShared: null,
      })
      .subscribe();
    expectRequest('POST', `${baseUrl}/client-portal/payments`, {
      paymentPlanId: 'plan-1',
      paymentDate: '2026-07-28',
      amount: 2000,
      method: 'BankTransfer',
      reference: 'SPEI-123',
      notesShared: null,
    });
  });

  it('maps the remaining contract administration operations', () => {
    const template = {
      name: 'Servicios',
      description: null,
      content: '<h1>{{contract.number}}</h1>',
      isDefault: true,
      isActive: true,
    };
    service.getContractTemplates('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/contract-templates`);
    service.createContractTemplate('org-1', template).subscribe();
    expectRequest('POST', `${organizationUrl}/contract-templates`, template);
    service.updateContractTemplate('org-1', 'template-1', template).subscribe();
    expectRequest('PUT', `${organizationUrl}/contract-templates/template-1`, template);
    service.archiveContractTemplate('org-1', 'template-1').subscribe();
    expectRequest('DELETE', `${organizationUrl}/contract-templates/template-1`);
    const preview = {
      content: template.content,
      eventId: 'event-1',
      clientId: 'client-1',
      proposalVersionId: 'version-1',
      contractId: null,
      validUntil: null,
    };
    service.previewContractTemplate('org-1', preview).subscribe();
    expectRequest('POST', `${organizationUrl}/contract-templates/preview`, preview);

    service.getContracts('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/contracts`);
    service.getContract('org-1', 'contract-1').subscribe();
    expectRequest('GET', `${organizationUrl}/contracts/contract-1`);
    const draft = {
      name: 'Contrato',
      templateId: 'template-1',
      content: '<p>Contenido</p>',
      consentText: 'Acepto.',
      validUntil: null,
    };
    service.updateContractDraft('org-1', 'contract-1', draft).subscribe();
    expectRequest('PUT', `${organizationUrl}/contracts/contract-1/draft`, draft);

    const externalFile = new File(['pdf'], 'externo.pdf', {
      type: 'application/pdf',
    });
    service
      .createExternalContract('org-1', {
        eventId: 'event-1',
        clientId: 'client-1',
        name: 'Contrato externo',
        contractGrandTotal: 10000,
        currencyCode: 'MXN',
        validUntil: null,
        file: externalFile,
      })
      .subscribe();
    const external = controller.expectOne(`${organizationUrl}/contracts/external`);
    expect(external.request.method).toBe('POST');
    const externalForm = external.request.body as FormData;
    expect(externalForm.get('eventId')).toBe('event-1');
    expect(externalForm.get('file')).toBe(externalFile);
    external.flush({});

    service.validateExternalContract('org-1', 'contract-1', '2026-07-28T12:00:00Z').subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/contract-1/validate-external`, {
      signedAt: '2026-07-28T12:00:00Z',
    });
    service.downloadContractVersion('org-1', 'contract-1', 'version-1').subscribe();
    expectBlobRequest(`${organizationUrl}/contracts/contract-1/versions/version-1/pdf`);
    service.downloadFinalContract('org-1', 'contract-1').subscribe();
    expectBlobRequest(`${organizationUrl}/contracts/contract-1/final`);

    const signer = {
      contractPartyId: 'party-1',
      personId: null,
      userAccountId: null,
      name: 'Ana Martínez',
      email: 'ana@example.com',
      signerRole: 'Cliente',
      signingOrder: 1,
      isRequired: true,
    };
    service.addContractSigner('org-1', 'contract-1', signer).subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/contract-1/signers`, signer);
    service.signAsOrganization('org-1', 'contract-1', 'signer-1', 'Mariana Torres').subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/contract-1/signers/signer-1/sign`, {
      signingMethod: 'AuthenticatedConfirmation',
      declaredSignerName: 'Mariana Torres',
      acceptElectronicMeans: true,
      confirmDisplayedVersion: true,
      signatureDataUrl: null,
    });
    service.revokeSignatureRequest('org-1', 'contract-1', 'request-1').subscribe();
    expectRequest('DELETE', `${organizationUrl}/contracts/contract-1/requests/request-1`);
    service.cancelContract('org-1', 'contract-1', 'El cliente ya no participa.').subscribe();
    expectRequest('POST', `${organizationUrl}/contracts/contract-1/cancel`, {
      reason: 'El cliente ya no participa.',
    });
    service.getContractEvidence('org-1', 'contract-1').subscribe();
    expectRequest('GET', `${organizationUrl}/contracts/contract-1/evidence`);
  });

  it('maps plans, payments and every portal contract operation', () => {
    service.getPaymentPlans('org-1', 'event-1').subscribe();
    expectRequest('GET', `${organizationUrl}/payment-plans?eventId=event-1`);
    const plan = {
      eventId: 'event-1',
      clientId: 'client-1',
      contractId: 'contract-1',
      proposalVersionId: 'version-1',
      currencyCode: 'MXN',
      totalAmount: 10000,
      installments: [
        {
          sequenceNumber: 1,
          description: 'Anticipo',
          dueDate: '2026-08-01',
          amount: 2000,
          installmentType: 'Deposit' as const,
        },
        {
          sequenceNumber: 2,
          description: 'Final',
          dueDate: '2027-01-01',
          amount: 8000,
          installmentType: 'FinalPayment' as const,
        },
      ],
    };
    service.createPaymentPlan('org-1', plan).subscribe();
    expectRequest('POST', `${organizationUrl}/payment-plans`, plan);
    service.activatePaymentPlan('org-1', 'plan-1').subscribe();
    expectRequest('POST', `${organizationUrl}/payment-plans/plan-1/activate`, null);

    service.getPayments('org-1', 'event-1').subscribe();
    expectRequest('GET', `${organizationUrl}/payments?eventId=event-1`);
    const payment = {
      eventId: 'event-1',
      clientId: 'client-1',
      paymentPlanId: 'plan-1',
      paymentDate: '2026-07-28',
      amount: 2000,
      currencyCode: 'MXN',
      method: 'BankTransfer' as const,
      reference: 'SPEI-123',
      notesShared: null,
      internalNotes: null,
    };
    service.createPayment('org-1', payment).subscribe();
    expectRequest('POST', `${organizationUrl}/payments`, payment);
    service.approvePayment('org-1', 'payment-1').subscribe();
    expectRequest('POST', `${organizationUrl}/payments/payment-1/approve`, null);
    service.rejectPayment('org-1', 'payment-1', 'No localizado').subscribe();
    expectRequest('POST', `${organizationUrl}/payments/payment-1/reject`, {
      reason: 'No localizado',
    });
    service
      .allocatePayment('org-1', 'payment-1', [
        { paymentInstallmentId: 'installment-1', amount: 2000 },
      ])
      .subscribe();
    expectRequest('POST', `${organizationUrl}/payments/payment-1/allocations`, [
      { paymentInstallmentId: 'installment-1', amount: 2000 },
    ]);

    service.declinePublicSignature('token', 'No acepto').subscribe();
    expectRequest('POST', `${baseUrl}/public/signatures/token/decline`, {
      reason: 'No acepto',
    });
    service.downloadPublicContractPdf('token').subscribe();
    expectBlobRequest(`${baseUrl}/public/signatures/token/pdf`);
    service.getPortalContract('contract-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/contracts/contract-1`);
    service.downloadPortalContract('contract-1').subscribe();
    expectBlobRequest(`${baseUrl}/client-portal/contracts/contract-1/pdf`);
    service.downloadPortalFinalContract('contract-1').subscribe();
    expectBlobRequest(`${baseUrl}/client-portal/contracts/contract-1/final`);
    service.signPortalContract('contract-1', 'signer-1', 'Ana Martínez').subscribe();
    expectRequest('POST', `${baseUrl}/client-portal/contracts/contract-1/signers/signer-1/sign`, {
      signingMethod: 'AuthenticatedConfirmation',
      declaredSignerName: 'Ana Martínez',
      acceptElectronicMeans: true,
      confirmDisplayedVersion: true,
      signatureDataUrl: null,
    });
    service.getPortalPaymentPlans('event-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/payment-plans?eventId=event-1`);
    service.getPortalPayments('event-1').subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/payments?eventId=event-1`);

    const receiptFile = new File(['image'], 'comprobante.png', {
      type: 'image/png',
    });
    service.uploadPortalPaymentReceipt('payment-1', receiptFile).subscribe();
    const receipt = controller.expectOne(`${baseUrl}/client-portal/payments/payment-1/receipt`);
    expect(receipt.request.method).toBe('POST');
    expect((receipt.request.body as FormData).get('file')).toBe(receiptFile);
    receipt.flush({});
  });

  it('maps guest groups, records, tags, duplicates and CSV operations', () => {
    const guestUrl = `${eventUrl}/guests`;
    const group = {
      groupType: 'Family' as const,
      displayName: 'Familia Luna',
      contactName: 'Elena Luna',
      contactPhone: null,
      contactEmail: null,
      allowedGuestCount: 3,
      allowUnnamedCompanions: true,
      maxUnnamedCompanions: 1,
      internalNotes: null,
      tagIds: ['tag-1'],
      applyCapacityOverride: false,
    };
    const guest = {
      invitationGroupId: 'group-1',
      personId: null,
      firstName: 'Elena',
      lastName: 'Luna',
      email: null,
      phone: null,
      guestType: 'Family' as const,
      ageCategory: 'Adult' as const,
      isPrimaryContact: true,
      isNamed: true,
      isPlusOne: false,
      isVip: true,
      sortOrder: 0,
      internalNotes: null,
    };

    service
      .getGuestDashboard('org-1', 'event-1', {
        search: 'Elena',
        groupId: 'group-1',
        tagId: 'tag-1',
        includeArchived: true,
      })
      .subscribe();
    expectRequest(
      'GET',
      `${guestUrl}/dashboard?search=Elena&groupId=group-1&tagId=tag-1&includeArchived=true`,
    );
    service.createInvitationGroup('org-1', 'event-1', group).subscribe();
    expectRequest('POST', `${guestUrl}/groups`, group);
    service.updateInvitationGroup('org-1', 'event-1', 'group-1', group).subscribe();
    expectRequest('PUT', `${guestUrl}/groups/group-1`, group);
    service.archiveInvitationGroup('org-1', 'event-1', 'group-1').subscribe();
    expectRequest('DELETE', `${guestUrl}/groups/group-1`);
    service.createEventGuest('org-1', 'event-1', guest).subscribe();
    expectRequest('POST', guestUrl, guest);
    service.updateEventGuest('org-1', 'event-1', 'guest-1', guest).subscribe();
    expectRequest('PUT', `${guestUrl}/guest-1`, guest);
    service.archiveEventGuest('org-1', 'event-1', 'guest-1').subscribe();
    expectRequest('DELETE', `${guestUrl}/guest-1`);
    service.getGuestTags('org-1', 'event-1').subscribe();
    expectRequest('GET', `${guestUrl}/tags`);
    service.createGuestTag('org-1', 'event-1', { name: 'VIP', colorToken: 'rose' }).subscribe();
    expectRequest('POST', `${guestUrl}/tags`, { name: 'VIP', colorToken: 'rose' });
    service
      .updateGuestTag('org-1', 'event-1', 'tag-1', {
        name: 'Familia',
        colorToken: 'sky',
      })
      .subscribe();
    expectRequest('PUT', `${guestUrl}/tags/tag-1`, {
      name: 'Familia',
      colorToken: 'sky',
    });
    service.archiveGuestTag('org-1', 'event-1', 'tag-1').subscribe();
    expectRequest('DELETE', `${guestUrl}/tags/tag-1`);
    service.getGuestDuplicates('org-1', 'event-1').subscribe();
    expectRequest('GET', `${guestUrl}/duplicates`);

    const file = new File(['csv'], 'invitados.csv', { type: 'text/csv' });
    service.analyzeGuestImport('org-1', 'event-1', file).subscribe();
    const analyze = controller.expectOne(`${guestUrl}/imports/analyze`);
    expect(analyze.request.method).toBe('POST');
    expect((analyze.request.body as FormData).get('file')).toBe(file);
    analyze.flush({});
    service
      .updateGuestImportMapping('org-1', 'event-1', 'import-1', {
        GroupName: 'Grupo',
      })
      .subscribe();
    expectRequest('PUT', `${guestUrl}/imports/import-1/mapping`, {
      mapping: { GroupName: 'Grupo' },
    });
    service.confirmGuestImport('org-1', 'event-1', 'import-1').subscribe();
    expectRequest('POST', `${guestUrl}/imports/import-1/confirm`, null);
    service.downloadGuestTemplate('org-1', 'event-1').subscribe();
    expectBlobRequest(`${guestUrl}/imports/template`);
    service.exportGuests('org-1', 'event-1').subscribe();
    expectBlobRequest(`${guestUrl}/export`);
  });

  it('maps invitation design, review, publication and private links', () => {
    const invitationUrl = `${eventUrl}/invitations`;
    const theme = {
      backgroundColor: '#FFFFFF',
      surfaceColor: '#FFFFFF',
      textColor: '#111111',
      accentColor: '#805641',
      headingFont: 'playfair',
      bodyFont: 'inter',
      radiusToken: 'lg',
      spacingToken: 'comfortable',
      coverStyle: 'card',
      buttonStyle: 'solid',
      animation: 'Reduced' as const,
    };
    const blocks = [
      {
        id: 'block-1',
        type: 'Text' as const,
        visible: true,
        visibility: 'Everyone' as const,
        visibilityValue: null,
        sortOrder: 0,
        content: { body: 'Hola' },
        presentation: { textAlign: 'center' },
      },
    ];

    service.getGuestExperience('org-1', 'event-1').subscribe();
    expectRequest('GET', `${invitationUrl}/experience`);
    const experience = {
      language: 'es',
      publicTitle: 'Nuestro evento',
      celebrantDisplayName: 'Ana & Carlos',
      welcomeMessage: null,
      closingMessage: 'Te esperamos',
      showEventName: true,
      showEventDate: true,
      showParticipantNames: true,
      showCity: true,
      privateAccessOnly: true,
    };
    service.updateGuestExperience('org-1', 'event-1', experience).subscribe();
    expectRequest('PUT', `${invitationUrl}/experience`, experience);
    service.suspendGuestExperience('org-1', 'event-1').subscribe();
    expectRequest('POST', `${invitationUrl}/experience/suspend`, null);
    service.resumeGuestExperience('org-1', 'event-1').subscribe();
    expectRequest('POST', `${invitationUrl}/experience/resume`, null);
    service.getInvitationTemplates('org-1', 'event-1').subscribe();
    expectRequest('GET', `${invitationUrl}/templates`);
    const template = {
      name: 'Propia',
      description: 'Plantilla de la organización',
      theme,
      blocks,
    };
    service.createInvitationTemplate('org-1', 'event-1', template).subscribe();
    expectRequest('POST', `${invitationUrl}/templates`, template);
    service.updateInvitationTemplate('org-1', 'event-1', 'template-1', template).subscribe();
    expectRequest('PUT', `${invitationUrl}/templates/template-1`, template);
    service.archiveInvitationTemplate('org-1', 'event-1', 'template-1').subscribe();
    expectRequest('DELETE', `${invitationUrl}/templates/template-1`);
    service.getInvitationDesigns('org-1', 'event-1').subscribe();
    expectRequest('GET', `${invitationUrl}/designs`);
    service
      .createInvitationDesign('org-1', 'event-1', {
        name: 'Principal',
        templateId: 'template-1',
      })
      .subscribe();
    expectRequest('POST', `${invitationUrl}/designs`, {
      name: 'Principal',
      templateId: 'template-1',
    });
    service
      .updateInvitationDesign('org-1', 'event-1', 'design-1', {
        name: 'Principal',
        theme,
        blocks,
      })
      .subscribe();
    expectRequest('PUT', `${invitationUrl}/designs/design-1`, {
      name: 'Principal',
      theme,
      blocks,
    });
    service.submitInvitationReview('org-1', 'event-1', 'design-1').subscribe();
    expectRequest('POST', `${invitationUrl}/designs/design-1/submit-review`, null);
    service
      .reviewInvitationDesign('org-1', 'event-1', 'design-1', 'version-1', 'approve', 'Aprobada')
      .subscribe();
    expectRequest('POST', `${invitationUrl}/designs/design-1/versions/version-1/approve`, {
      message: 'Aprobada',
    });
    service.publishInvitationDesign('org-1', 'event-1', 'design-1').subscribe();
    expectRequest('POST', `${invitationUrl}/designs/design-1/publish`, {
      bypassApprovalForTesting: false,
    });
    service.getGuestLinks('org-1', 'event-1').subscribe();
    expectRequest('GET', `${invitationUrl}/links`);
    service.generateGuestLink('org-1', 'event-1', 'group-1', null).subscribe();
    expectRequest('POST', `${invitationUrl}/groups/group-1/links`, { expiresAt: null });
    service.regenerateGuestLink('org-1', 'event-1', 'link-1', null).subscribe();
    expectRequest('POST', `${invitationUrl}/links/link-1/regenerate`, { expiresAt: null });
    service.markGuestLinkShared('org-1', 'event-1', 'link-1').subscribe();
    expectRequest('POST', `${invitationUrl}/links/link-1/mark-shared`, null);
    service.revokeGuestLink('org-1', 'event-1', 'link-1').subscribe();
    expectRequest('DELETE', `${invitationUrl}/links/link-1`);
    service.getPublicInvitation('private/token').subscribe();
    expectRequest('GET', `${baseUrl}/public/invitations/private%2Ftoken`);
  });

  it('maps the safe client portal guest collaboration surface', () => {
    const portalUrl = `${baseUrl}/client-portal/events/event-1/guest-experience`;
    const group = {
      groupType: 'Family' as const,
      displayName: 'Familia Luna',
      allowedGuestCount: 3,
      allowUnnamedCompanions: false,
      maxUnnamedCompanions: 0,
    };
    const guest = {
      invitationGroupId: 'group-1',
      firstName: 'Elena',
      lastName: 'Luna',
      guestType: 'Family' as const,
      ageCategory: 'Adult' as const,
      isPrimaryContact: true,
      isVip: false,
      sortOrder: 0,
    };

    service.getPortalGuestWorkspace('event-1').subscribe();
    expectRequest('GET', portalUrl);
    service.createPortalInvitationGroup('event-1', group).subscribe();
    expectRequest('POST', `${portalUrl}/groups`, group);
    service.updatePortalInvitationGroup('event-1', 'group-1', group).subscribe();
    expectRequest('PUT', `${portalUrl}/groups/group-1`, group);
    service.archivePortalInvitationGroup('event-1', 'group-1').subscribe();
    expectRequest('DELETE', `${portalUrl}/groups/group-1`);
    service.createPortalGuest('event-1', guest).subscribe();
    expectRequest('POST', `${portalUrl}/guests`, guest);
    service.updatePortalGuest('event-1', 'guest-1', guest).subscribe();
    expectRequest('PUT', `${portalUrl}/guests/guest-1`, guest);
    service.archivePortalGuest('event-1', 'guest-1').subscribe();
    expectRequest('DELETE', `${portalUrl}/guests/guest-1`);
    service
      .reviewPortalInvitation(
        'event-1',
        'design-1',
        'version-1',
        'request-changes',
        'Ajustar saludo',
      )
      .subscribe();
    expectRequest('POST', `${portalUrl}/designs/design-1/versions/version-1/request-changes`, {
      message: 'Ajustar saludo',
    });

    const file = new File(['csv'], 'invitados.csv', { type: 'text/csv' });
    service.analyzePortalGuestImport('event-1', file).subscribe();
    const analyze = controller.expectOne(`${portalUrl}/imports/analyze`);
    expect(analyze.request.method).toBe('POST');
    expect((analyze.request.body as FormData).get('file')).toBe(file);
    analyze.flush({});
    service
      .updatePortalGuestImportMapping('event-1', 'import-1', {
        GroupName: 'Grupo',
      })
      .subscribe();
    expectRequest('PUT', `${portalUrl}/imports/import-1/mapping`, {
      mapping: { GroupName: 'Grupo' },
    });
    service.confirmPortalGuestImport('event-1', 'import-1').subscribe();
    expectRequest('POST', `${portalUrl}/imports/import-1/confirm`, null);
    service.downloadPortalGuestImportTemplate('event-1').subscribe();
    expectBlobRequest(`${portalUrl}/imports/template`);
    service.getPortalGuestDuplicates('event-1').subscribe();
    expectRequest('GET', `${portalUrl}/duplicates`);
    service.getPortalGuestLinks('event-1').subscribe();
    expectRequest('GET', `${portalUrl}/links`);
    service.markPortalGuestLinkShared('event-1', 'link-1').subscribe();
    expectRequest('POST', `${portalUrl}/links/link-1/mark-shared`, null);
  });

  it('maps RSVP idempotency, exceptions and sensitive operations', () => {
    const submission: RsvpSubmissionRequest = {
      rsvpFormVersionId: 'version-1',
      expectedRevision: 4,
      overallStatus: 'Confirmed',
      contactName: 'Familia Luna',
      contactEmail: null,
      contactPhone: null,
      guests: [],
      answers: [],
      consentSnapshot: null,
    };
    const manual = {
      source: 'SupportCorrection' as const,
      reason: 'Corrección solicitada por soporte',
      submission,
    };

    service
      .submitGuestRsvp(
        'private/token',
        submission,
        'attempt-public-rsvp-000001',
      )
      .subscribe();
    const publicSubmit = controller.expectOne(
      `${baseUrl}/guest/rsvp/private/token/submit`,
    );
    expect(publicSubmit.request.method).toBe('POST');
    expect(publicSubmit.request.body).toEqual(submission);
    expect(publicSubmit.request.headers.get('Idempotency-Key')).toBe(
      'attempt-public-rsvp-000001',
    );
    publicSubmit.flush({});

    service
      .manualRsvpCapture(
        'org-1',
        'event-1',
        'group-1',
        manual,
        'attempt-manual-rsvp-000001',
      )
      .subscribe();
    const manualSubmit = controller.expectOne(
      `${eventUrl}/rsvp/groups/group-1/manual-capture`,
    );
    expect(manualSubmit.request.method).toBe('POST');
    expect(manualSubmit.request.body).toEqual(manual);
    expect(manualSubmit.request.headers.get('Idempotency-Key')).toBe(
      'attempt-manual-rsvp-000001',
    );
    manualSubmit.flush({});

    service.getPortalRsvpDashboard('event-1').subscribe();
    expectRequest(
      'GET',
      `${baseUrl}/client-portal/events/event-1/rsvp/dashboard`,
    );
    service
      .manualPortalRsvpCapture(
        'event-1',
        'group-1',
        manual,
        'attempt-portal-rsvp-000001',
      )
      .subscribe();
    const portalSubmit = controller.expectOne(
      `${baseUrl}/client-portal/events/event-1/rsvp/groups/group-1/manual-capture`,
    );
    expect(portalSubmit.request.method).toBe('POST');
    expect(portalSubmit.request.body).toEqual(manual);
    expect(portalSubmit.request.headers.get('Idempotency-Key')).toBe(
      'attempt-portal-rsvp-000001',
    );
    portalSubmit.flush({});

    service.closeRsvpGroupException('org-1', 'event-1', 'group-1').subscribe();
    expectRequest(
      'POST',
      `${eventUrl}/rsvp/groups/group-1/exception/close`,
      null,
    );
    service.getRsvpSensitiveData('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/rsvp/sensitive-data`);
    service.exportRsvpSensitiveData('org-1', 'event-1').subscribe();
    expectBlobRequest(`${eventUrl}/rsvp/exports/sensitive`);

    const groupException = {
      expiresAt: '2027-01-01T00:00:00Z',
      reason: 'Atención autorizada',
    };
    service
      .openRsvpGroupException(
        'org-1',
        'event-1',
        'group-1',
        groupException,
      )
      .subscribe();
    expectRequest(
      'POST',
      `${eventUrl}/rsvp/groups/group-1/exception`,
      groupException,
    );

    const menu: EventMenuRequest = {
      name: 'Cena',
      description: null,
      menuCategory: 'AdultMeal',
      selectionRequired: true,
      minimumSelections: 1,
      maximumSelections: 1,
      sortOrder: 0,
    };
    const menuOption: EventMenuOptionRequest = {
      name: 'Vegetariano',
      description: null,
      dietaryTags: 'vegetariano',
      capacity: null,
      sortOrder: 0,
    };
    service.getEventMenus('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/menus`);
    service.createEventMenu('org-1', 'event-1', menu).subscribe();
    expectRequest('POST', `${eventUrl}/menus`, menu);
    service
      .addMenuOption(
        'org-1',
        'event-1',
        'menu-1',
        menuOption,
      )
      .subscribe();
    expectRequest(
      'POST',
      `${eventUrl}/menus/menu-1/options`,
      menuOption,
    );

    const transport: EventTransportOptionRequest = {
      name: 'Camioneta',
      description: null,
      direction: 'ToCeremony',
      pickupPoint: 'Lobby',
      departureAt: null,
      returnAt: null,
      capacity: 10,
      allowWaitlist: true,
      sortOrder: 0,
    };
    service.getTransportOptions('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/transport`);
    service
      .createTransportOption('org-1', 'event-1', transport)
      .subscribe();
    expectRequest('POST', `${eventUrl}/transport`, transport);

    const accommodation: EventAccommodationOptionRequest = {
      name: 'Hotel',
      description: null,
      address: null,
      bookingUrl: null,
      bookingCode: null,
      bookingDeadline: null,
      contactInformation: null,
      sortOrder: 0,
    };
    service.getAccommodationOptions('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/accommodation`);
    service
      .createAccommodationOption(
        'org-1',
        'event-1',
        accommodation,
      )
      .subscribe();
    expectRequest(
      'POST',
      `${eventUrl}/accommodation`,
      accommodation,
    );

    const reminder: ReminderTemplateRequest = {
      name: 'Pendientes',
      channel: 'GeneralCopy',
      segmentType: 'Pending',
      messageTemplate: 'Confirma tu asistencia',
    };
    service.getReminderTemplates('org-1', 'event-1').subscribe();
    expectRequest('GET', `${eventUrl}/rsvp/reminders/templates`);
    service
      .createReminderTemplate('org-1', 'event-1', reminder)
      .subscribe();
    expectRequest(
      'POST',
      `${eventUrl}/rsvp/reminders/templates`,
      reminder,
    );
    service
      .markReminderSent(
        'org-1',
        'event-1',
        'group-1',
        'template-1',
        { note: 'Enviado manualmente' },
      )
      .subscribe();
    expectRequest(
      'POST',
      `${eventUrl}/rsvp/reminders/groups/group-1/templates/template-1/mark-sent`,
      { note: 'Enviado manualmente' },
    );

    service.getGuestRsvpState('private-token').subscribe();
    expectRequest(
      'GET',
      `${baseUrl}/guest/rsvp/private-token/state`,
    );
  });

  it('maps optional query and multipart variants', () => {
    service.getProspects('org-1').subscribe();
    expectRequest(
      'GET',
      `${organizationUrl}/prospects?page=1&pageSize=100`,
    );
    service.getProposals('org-1').subscribe();
    expectRequest(
      'GET',
      `${organizationUrl}/proposals?page=1&pageSize=100&search=`,
    );

    const externalFile = new File(['pdf'], 'vigente.pdf', {
      type: 'application/pdf',
    });
    service
      .createExternalContract('org-1', {
        eventId: 'event-1',
        clientId: 'client-1',
        name: 'Contrato vigente',
        contractGrandTotal: 10000,
        currencyCode: 'MXN',
        validUntil: '2027-01-01T00:00:00Z',
        file: externalFile,
      })
      .subscribe();
    const external = controller.expectOne(
      `${organizationUrl}/contracts/external`,
    );
    const form = external.request.body as FormData;
    expect(form.get('validUntil')).toBe('2027-01-01T00:00:00Z');
    external.flush({});

    service.getPaymentPlans('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/payment-plans`);
    service.getPayments('org-1').subscribe();
    expectRequest('GET', `${organizationUrl}/payments`);
    service.getPortalPayments().subscribe();
    expectRequest('GET', `${baseUrl}/client-portal/payments`);
    service.getGuestDashboard('org-1', 'event-1').subscribe();
    expectRequest(
      'GET',
      `${eventUrl}/guests/dashboard`,
    );
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
