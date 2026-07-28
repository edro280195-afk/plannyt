import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AcceptInvitationRequest,
  AuthResponse,
  ClientContact,
  ClientListItem,
  ClientResponse,
  CreateClientRequest,
  DocumentResponse,
  EventAccess,
  EventAccessRole,
  EventClient,
  EventClientRelationshipType,
  EventDetailsRequest,
  EventListItem,
  EventParticipant,
  EventResponse,
  EventStatus,
  InvitationAcceptance,
  InvitationCreated,
  InvitationPublic,
  LoginRequest,
  MeResponse,
  OrganizationMember,
  OrganizationResponse,
  OrganizationRole,
  PagedResponse,
  PortalEvent,
  PortalEventDetail,
  RegisterAndAcceptInvitationRequest,
  RegisterPlannerRequest,
  UpdateClientRequest,
  UpdateOrganizationRequest,
  UpsertParticipantRequest,
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiBaseUrl;

  registerPlanner(request: RegisterPlannerRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/register-planner`, request);
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/login`, request);
  }

  refresh(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/auth/refresh`, null);
  }

  logout(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/logout`, null);
  }

  logoutAll(): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/auth/logout-all`, null);
  }

  getMe(): Observable<MeResponse> {
    return this.http.get<MeResponse>(`${this.baseUrl}/auth/me`);
  }

  getOrganization(organizationId: string): Observable<OrganizationResponse> {
    return this.http.get<OrganizationResponse>(`${this.organizationUrl(organizationId)}`);
  }

  updateOrganization(
    organizationId: string,
    request: UpdateOrganizationRequest,
  ): Observable<OrganizationResponse> {
    return this.http.put<OrganizationResponse>(`${this.organizationUrl(organizationId)}`, request);
  }

  getMembers(organizationId: string): Observable<OrganizationMember[]> {
    return this.http.get<OrganizationMember[]>(`${this.organizationUrl(organizationId)}/members`);
  }

  revokeMember(organizationId: string, membershipId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.organizationUrl(organizationId)}/members/${membershipId}`,
    );
  }

  inviteMember(
    organizationId: string,
    targetEmail: string,
    intendedOrganizationRole: OrganizationRole,
  ): Observable<InvitationCreated> {
    return this.http.post<InvitationCreated>(
      `${this.organizationUrl(organizationId)}/members/invitations`,
      { targetEmail, intendedOrganizationRole },
    );
  }

  revokeOrganizationInvitation(organizationId: string, invitationId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.organizationUrl(organizationId)}/members/invitations/${invitationId}`,
    );
  }

  getClients(
    organizationId: string,
    search = '',
    page = 1,
    pageSize = 50,
  ): Observable<PagedResponse<ClientListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search);
    return this.http.get<PagedResponse<ClientListItem>>(
      `${this.organizationUrl(organizationId)}/clients`,
      { params },
    );
  }

  getClient(organizationId: string, clientId: string): Observable<ClientResponse> {
    return this.http.get<ClientResponse>(
      `${this.organizationUrl(organizationId)}/clients/${clientId}`,
    );
  }

  createClient(organizationId: string, request: CreateClientRequest): Observable<ClientResponse> {
    return this.http.post<ClientResponse>(
      `${this.organizationUrl(organizationId)}/clients`,
      request,
    );
  }

  updateClient(
    organizationId: string,
    clientId: string,
    request: UpdateClientRequest,
  ): Observable<ClientResponse> {
    return this.http.put<ClientResponse>(
      `${this.organizationUrl(organizationId)}/clients/${clientId}`,
      request,
    );
  }

  archiveClient(organizationId: string, clientId: string): Observable<void> {
    return this.http.post<void>(
      `${this.organizationUrl(organizationId)}/clients/${clientId}/archive`,
      null,
    );
  }

  addClientContact(
    organizationId: string,
    clientId: string,
    request: {
      firstName: string;
      lastName: string;
      contactEmail: string | null;
      contactPhone: string | null;
      preferredLanguage: string;
      timeZone: string;
      contactRole: string;
      isPrimary: boolean;
    },
  ): Observable<ClientContact> {
    return this.http.post<ClientContact>(
      `${this.organizationUrl(organizationId)}/clients/${clientId}/contacts`,
      request,
    );
  }

  getEvents(
    organizationId: string,
    search = '',
    page = 1,
    pageSize = 50,
  ): Observable<PagedResponse<EventListItem>> {
    const params = new HttpParams()
      .set('page', page)
      .set('pageSize', pageSize)
      .set('search', search);
    return this.http.get<PagedResponse<EventListItem>>(
      `${this.organizationUrl(organizationId)}/events`,
      { params },
    );
  }

  getEvent(organizationId: string, eventId: string): Observable<EventResponse> {
    return this.http.get<EventResponse>(`${this.eventUrl(organizationId, eventId)}`);
  }

  createEvent(organizationId: string, request: EventDetailsRequest): Observable<EventResponse> {
    return this.http.post<EventResponse>(`${this.organizationUrl(organizationId)}/events`, request);
  }

  updateEvent(
    organizationId: string,
    eventId: string,
    request: EventDetailsRequest,
  ): Observable<EventResponse> {
    return this.http.put<EventResponse>(this.eventUrl(organizationId, eventId), request);
  }

  changeEventStatus(
    organizationId: string,
    eventId: string,
    newStatus: EventStatus,
    reason: string | null,
  ): Observable<EventResponse> {
    return this.http.post<EventResponse>(`${this.eventUrl(organizationId, eventId)}/status`, {
      newStatus,
      reason,
    });
  }

  getEventClients(organizationId: string, eventId: string): Observable<EventClient[]> {
    return this.http.get<EventClient[]>(`${this.eventUrl(organizationId, eventId)}/clients`);
  }

  addEventClient(
    organizationId: string,
    eventId: string,
    request: {
      clientId: string;
      relationshipType: EventClientRelationshipType;
      isPrimary: boolean;
      hasTransferAuthority: boolean;
    },
  ): Observable<EventClient> {
    return this.http.post<EventClient>(
      `${this.eventUrl(organizationId, eventId)}/clients`,
      request,
    );
  }

  removeEventClient(organizationId: string, eventId: string, relationId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.eventUrl(organizationId, eventId)}/clients/${relationId}`,
    );
  }

  getParticipants(organizationId: string, eventId: string): Observable<EventParticipant[]> {
    return this.http.get<EventParticipant[]>(
      `${this.eventUrl(organizationId, eventId)}/participants`,
    );
  }

  addParticipant(
    organizationId: string,
    eventId: string,
    request: UpsertParticipantRequest,
  ): Observable<EventParticipant> {
    return this.http.post<EventParticipant>(
      `${this.eventUrl(organizationId, eventId)}/participants`,
      request,
    );
  }

  getEventAccesses(organizationId: string, eventId: string): Observable<EventAccess[]> {
    return this.http.get<EventAccess[]>(`${this.eventUrl(organizationId, eventId)}/access`);
  }

  inviteEventAccess(
    organizationId: string,
    eventId: string,
    targetEmail: string,
    intendedEventRole: EventAccessRole,
  ): Observable<InvitationCreated> {
    return this.http.post<InvitationCreated>(
      `${this.eventUrl(organizationId, eventId)}/access/invitations`,
      { targetEmail, intendedEventRole },
    );
  }

  revokeEventInvitation(
    organizationId: string,
    eventId: string,
    invitationId: string,
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.eventUrl(organizationId, eventId)}/access/invitations/${invitationId}`,
    );
  }

  revokeEventAccess(organizationId: string, eventId: string, accessId: string): Observable<void> {
    return this.http.delete<void>(`${this.eventUrl(organizationId, eventId)}/access/${accessId}`);
  }

  getDocuments(organizationId: string, eventId: string): Observable<DocumentResponse[]> {
    return this.http.get<DocumentResponse[]>(`${this.eventUrl(organizationId, eventId)}/documents`);
  }

  uploadDocument(
    organizationId: string,
    eventId: string,
    file: File,
    documentType: string,
    visibility: string,
  ): Observable<DocumentResponse> {
    const form = new FormData();
    form.append('file', file);
    form.append('documentType', documentType);
    form.append('visibility', visibility);
    return this.http.post<DocumentResponse>(
      `${this.eventUrl(organizationId, eventId)}/documents`,
      form,
    );
  }

  deleteDocument(organizationId: string, eventId: string, documentId: string): Observable<void> {
    return this.http.delete<void>(
      `${this.eventUrl(organizationId, eventId)}/documents/${documentId}`,
    );
  }

  downloadAdminDocument(
    organizationId: string,
    eventId: string,
    documentId: string,
  ): Observable<Blob> {
    return this.http.get(
      `${this.eventUrl(organizationId, eventId)}/documents/${documentId}/download`,
      { responseType: 'blob' },
    );
  }

  getInvitation(token: string): Observable<InvitationPublic> {
    return this.http.get<InvitationPublic>(
      `${this.baseUrl}/access-invitations/${encodeURIComponent(token)}`,
    );
  }

  registerAndAcceptInvitation(
    token: string,
    request: RegisterAndAcceptInvitationRequest,
  ): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(
      `${this.baseUrl}/access-invitations/${encodeURIComponent(token)}/register-and-accept`,
      request,
    );
  }

  acceptInvitation(
    token: string,
    request: AcceptInvitationRequest,
  ): Observable<InvitationAcceptance> {
    return this.http.post<InvitationAcceptance>(
      `${this.baseUrl}/access-invitations/${encodeURIComponent(token)}/accept`,
      request,
    );
  }

  getPortalEvents(): Observable<PortalEvent[]> {
    return this.http.get<PortalEvent[]>(`${this.baseUrl}/client-portal/events`);
  }

  getPortalEvent(eventId: string): Observable<PortalEventDetail> {
    return this.http.get<PortalEventDetail>(`${this.baseUrl}/client-portal/events/${eventId}`);
  }

  getPortalDocuments(eventId: string): Observable<DocumentResponse[]> {
    return this.http.get<DocumentResponse[]>(
      `${this.baseUrl}/client-portal/events/${eventId}/documents`,
    );
  }

  downloadPortalDocument(eventId: string, documentId: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/client-portal/events/${eventId}/documents/${documentId}/download`,
      { responseType: 'blob' },
    );
  }

  private organizationUrl(organizationId: string): string {
    return `${this.baseUrl}/organizations/${organizationId}`;
  }

  private eventUrl(organizationId: string, eventId: string): string {
    return `${this.organizationUrl(organizationId)}/events/${eventId}`;
  }
}
