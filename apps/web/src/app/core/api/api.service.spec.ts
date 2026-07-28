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
