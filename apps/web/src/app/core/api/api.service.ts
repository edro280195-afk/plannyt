import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AcceptInvitationRequest,
  AuthResponse,
  CatalogPackage,
  ClientContact,
  ClientListItem,
  ClientMatchSuggestion,
  ClientResponse,
  ClientType,
  ContractListItem,
  ContractResponse,
  ContractTemplate,
  ContractingReadiness,
  ConvertProspectResponse,
  Coupon,
  CouponRequest,
  CreateProspectActivityRequest,
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
  PackageRequest,
  PagedResponse,
  PaymentMethod,
  PaymentPlan,
  PaymentRecord,
  ProposalComment,
  ProposalDraftRequest,
  ProposalListItem,
  ProposalPublicResponse,
  ProposalResponse,
  ProposalShareLink,
  ProposalStatus,
  ProspectDetailsRequest,
  ProspectListItem,
  ProspectResponse,
  ProspectStatus,
  PortalEvent,
  PortalEventDetail,
  PortalContract,
  PortalContractListItem,
  PortalPaymentRecord,
  PublicSignatureContract,
  RegisterAndAcceptInvitationRequest,
  RegisterPlannerRequest,
  ServiceCatalogItem,
  ServiceCatalogItemRequest,
  SignatureRequestLink,
  SigningMethod,
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

  getProspects(
    organizationId: string,
    filters: {
      search?: string;
      status?: ProspectStatus;
      assignedUserId?: string;
      eventType?: string;
      dateFrom?: string;
      dateTo?: string;
      page?: number;
      pageSize?: number;
    } = {},
  ): Observable<PagedResponse<ProspectListItem>> {
    let params = new HttpParams()
      .set('page', filters.page ?? 1)
      .set('pageSize', filters.pageSize ?? 100);
    for (const [key, value] of Object.entries(filters)) {
      if (value !== undefined && key !== 'page' && key !== 'pageSize') {
        params = params.set(key, value);
      }
    }
    return this.http.get<PagedResponse<ProspectListItem>>(
      `${this.organizationUrl(organizationId)}/prospects`,
      { params },
    );
  }

  getProspect(organizationId: string, prospectId: string): Observable<ProspectResponse> {
    return this.http.get<ProspectResponse>(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}`,
    );
  }

  createProspect(
    organizationId: string,
    request: ProspectDetailsRequest,
  ): Observable<ProspectResponse> {
    return this.http.post<ProspectResponse>(
      `${this.organizationUrl(organizationId)}/prospects`,
      request,
    );
  }

  updateProspect(
    organizationId: string,
    prospectId: string,
    request: ProspectDetailsRequest,
  ): Observable<ProspectResponse> {
    return this.http.put<ProspectResponse>(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}`,
      request,
    );
  }

  changeProspectStatus(
    organizationId: string,
    prospectId: string,
    newStatus: ProspectStatus,
    reason: string | null,
  ): Observable<ProspectResponse> {
    return this.http.post<ProspectResponse>(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}/status`,
      { newStatus, reason },
    );
  }

  addProspectActivity(
    organizationId: string,
    prospectId: string,
    request: CreateProspectActivityRequest,
  ): Observable<unknown> {
    return this.http.post(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}/activities`,
      request,
    );
  }

  completeProspectActivity(
    organizationId: string,
    prospectId: string,
    activityId: string,
  ): Observable<unknown> {
    return this.http.post(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}/activities/${activityId}/complete`,
      null,
    );
  }

  getProspectMatches(
    organizationId: string,
    prospectId: string,
  ): Observable<ClientMatchSuggestion[]> {
    return this.http.get<ClientMatchSuggestion[]>(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}/client-matches`,
    );
  }

  convertProspect(
    organizationId: string,
    prospectId: string,
    request: {
      existingClientId: string | null;
      newClientType: ClientType | null;
      confirmCreateDespiteMatches: boolean;
    },
  ): Observable<ConvertProspectResponse> {
    return this.http.post<ConvertProspectResponse>(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}/convert`,
      request,
    );
  }

  linkProspectPreliminaryEvent(
    organizationId: string,
    prospectId: string,
    request: {
      existingEventId: string | null;
      name: string | null;
      eventType: string | null;
      startDateTime: string | null;
      timeZone: string | null;
      city: string | null;
      countryCode: string | null;
      estimatedGuestCount: number | null;
    },
  ): Observable<{ prospectId: string; eventId: string; createdNewEvent: boolean }> {
    return this.http.post<{ prospectId: string; eventId: string; createdNewEvent: boolean }>(
      `${this.organizationUrl(organizationId)}/prospects/${prospectId}/preliminary-event`,
      request,
    );
  }

  getCatalogServices(organizationId: string): Observable<ServiceCatalogItem[]> {
    return this.http.get<ServiceCatalogItem[]>(
      `${this.organizationUrl(organizationId)}/catalog/services`,
    );
  }

  createCatalogService(
    organizationId: string,
    request: ServiceCatalogItemRequest,
  ): Observable<ServiceCatalogItem> {
    return this.http.post<ServiceCatalogItem>(
      `${this.organizationUrl(organizationId)}/catalog/services`,
      request,
    );
  }

  updateCatalogService(
    organizationId: string,
    serviceId: string,
    request: ServiceCatalogItemRequest,
  ): Observable<ServiceCatalogItem> {
    return this.http.put<ServiceCatalogItem>(
      `${this.organizationUrl(organizationId)}/catalog/services/${serviceId}`,
      request,
    );
  }

  getPackages(organizationId: string): Observable<CatalogPackage[]> {
    return this.http.get<CatalogPackage[]>(
      `${this.organizationUrl(organizationId)}/catalog/packages`,
    );
  }

  createPackage(organizationId: string, request: PackageRequest): Observable<CatalogPackage> {
    return this.http.post<CatalogPackage>(
      `${this.organizationUrl(organizationId)}/catalog/packages`,
      request,
    );
  }

  getCoupons(organizationId: string): Observable<Coupon[]> {
    return this.http.get<Coupon[]>(`${this.organizationUrl(organizationId)}/catalog/coupons`);
  }

  createCoupon(organizationId: string, request: CouponRequest): Observable<Coupon> {
    return this.http.post<Coupon>(
      `${this.organizationUrl(organizationId)}/catalog/coupons`,
      request,
    );
  }

  getProposals(
    organizationId: string,
    search = '',
    status?: ProposalStatus,
  ): Observable<PagedResponse<ProposalListItem>> {
    let params = new HttpParams().set('page', 1).set('pageSize', 100).set('search', search);
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<PagedResponse<ProposalListItem>>(
      `${this.organizationUrl(organizationId)}/proposals`,
      { params },
    );
  }

  getProposal(organizationId: string, proposalId: string): Observable<ProposalResponse> {
    return this.http.get<ProposalResponse>(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}`,
    );
  }

  createProposal(
    organizationId: string,
    request: ProposalDraftRequest,
  ): Observable<ProposalResponse> {
    return this.http.post<ProposalResponse>(
      `${this.organizationUrl(organizationId)}/proposals`,
      request,
    );
  }

  updateProposalDraft(
    organizationId: string,
    proposalId: string,
    request: ProposalDraftRequest,
  ): Observable<ProposalResponse> {
    return this.http.put<ProposalResponse>(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}/draft`,
      request,
    );
  }

  publishProposal(organizationId: string, proposalId: string): Observable<unknown> {
    return this.http.post(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}/publish`,
      null,
    );
  }

  sendProposal(
    organizationId: string,
    proposalId: string,
    expiresAt: string | null,
  ): Observable<ProposalShareLink> {
    return this.http.post<ProposalShareLink>(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}/send`,
      { expiresAt },
    );
  }

  downloadAdminProposalPdf(
    organizationId: string,
    proposalId: string,
    versionId: string,
  ): Observable<Blob> {
    return this.http.get(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}/versions/${versionId}/pdf`,
      { responseType: 'blob' },
    );
  }

  addProposalComment(
    organizationId: string,
    proposalId: string,
    request: {
      proposalVersionId: string;
      proposalLineId: string | null;
      authorDisplayName: string;
      content: string;
      visibility: 'Internal' | 'ClientShared';
      parentCommentId: string | null;
    },
  ): Observable<ProposalComment> {
    return this.http.post<ProposalComment>(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}/comments`,
      request,
    );
  }

  getPublicProposal(token: string): Observable<ProposalPublicResponse> {
    return this.http.get<ProposalPublicResponse>(
      `${this.baseUrl}/public/proposals/${encodeURIComponent(token)}`,
    );
  }

  addPublicProposalComment(
    token: string,
    request: {
      authorDisplayName: string;
      content: string;
      proposalLineId: string | null;
      parentCommentId: string | null;
    },
  ): Observable<ProposalComment> {
    return this.http.post<ProposalComment>(
      `${this.baseUrl}/public/proposals/${encodeURIComponent(token)}/comments`,
      request,
    );
  }

  decidePublicProposal(
    token: string,
    action: 'request-changes' | 'accept' | 'reject',
    authorDisplayName: string | null,
    reason: string | null,
  ): Observable<ProposalPublicResponse> {
    return this.http.post<ProposalPublicResponse>(
      `${this.baseUrl}/public/proposals/${encodeURIComponent(token)}/${action}`,
      { authorDisplayName, reason },
    );
  }

  downloadPublicProposalPdf(token: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/public/proposals/${encodeURIComponent(token)}/pdf`, {
      responseType: 'blob',
    });
  }

  getPortalProposals(): Observable<ProposalListItem[]> {
    return this.http.get<ProposalListItem[]>(`${this.baseUrl}/client-portal/proposals`);
  }

  getPortalProposal(proposalId: string): Observable<ProposalPublicResponse> {
    return this.http.get<ProposalPublicResponse>(
      `${this.baseUrl}/client-portal/proposals/${proposalId}`,
    );
  }

  downloadPortalProposalPdf(proposalId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/client-portal/proposals/${proposalId}/pdf`, {
      responseType: 'blob',
    });
  }

  getContractTemplates(organizationId: string): Observable<ContractTemplate[]> {
    return this.http.get<ContractTemplate[]>(
      `${this.organizationUrl(organizationId)}/contract-templates`,
    );
  }

  createContractTemplate(
    organizationId: string,
    request: {
      name: string;
      description: string | null;
      content: string;
      isDefault: boolean;
      isActive: boolean;
    },
  ): Observable<ContractTemplate> {
    return this.http.post<ContractTemplate>(
      `${this.organizationUrl(organizationId)}/contract-templates`,
      request,
    );
  }

  updateContractTemplate(
    organizationId: string,
    templateId: string,
    request: {
      name: string;
      description: string | null;
      content: string;
      isDefault: boolean;
      isActive: boolean;
    },
  ): Observable<ContractTemplate> {
    return this.http.put<ContractTemplate>(
      `${this.organizationUrl(organizationId)}/contract-templates/${templateId}`,
      request,
    );
  }

  previewContractTemplate(
    organizationId: string,
    request: {
      content: string;
      eventId: string | null;
      clientId: string | null;
      proposalVersionId: string | null;
      contractId: string | null;
      validUntil: string | null;
    },
  ): Observable<{
    renderedContent: string;
    unknownVariables: string[];
    missingVariables: string[];
    canPublish: boolean;
  }> {
    return this.http.post<{
      renderedContent: string;
      unknownVariables: string[];
      missingVariables: string[];
      canPublish: boolean;
    }>(`${this.organizationUrl(organizationId)}/contract-templates/preview`, request);
  }

  getContracts(organizationId: string, eventId?: string): Observable<ContractListItem[]> {
    const params = eventId ? new HttpParams().set('eventId', eventId) : undefined;
    return this.http.get<ContractListItem[]>(`${this.organizationUrl(organizationId)}/contracts`, {
      params,
    });
  }

  getContract(organizationId: string, contractId: string): Observable<ContractResponse> {
    return this.http.get<ContractResponse>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}`,
    );
  }

  createContractFromProposal(
    organizationId: string,
    request: {
      proposalId: string;
      name: string;
      templateId: string | null;
      content: string | null;
      consentText: string;
      validUntil: string | null;
    },
  ): Observable<ContractResponse> {
    return this.http.post<ContractResponse>(
      `${this.organizationUrl(organizationId)}/contracts/from-proposal`,
      request,
    );
  }

  createExternalContract(
    organizationId: string,
    request: {
      eventId: string;
      clientId: string;
      name: string;
      contractGrandTotal: number;
      currencyCode: string;
      validUntil: string | null;
      file: File;
    },
  ): Observable<ContractResponse> {
    const form = new FormData();
    form.append('eventId', request.eventId);
    form.append('clientId', request.clientId);
    form.append('name', request.name);
    form.append('contractGrandTotal', String(request.contractGrandTotal));
    form.append('currencyCode', request.currencyCode);
    if (request.validUntil) {
      form.append('validUntil', request.validUntil);
    }
    form.append('file', request.file);
    return this.http.post<ContractResponse>(
      `${this.organizationUrl(organizationId)}/contracts/external`,
      form,
    );
  }

  updateContractDraft(
    organizationId: string,
    contractId: string,
    request: {
      name: string;
      templateId: string | null;
      content: string;
      consentText: string;
      validUntil: string | null;
    },
  ): Observable<ContractResponse> {
    return this.http.put<ContractResponse>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/draft`,
      request,
    );
  }

  publishContract(organizationId: string, contractId: string): Observable<unknown> {
    return this.http.post(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/publish`,
      null,
    );
  }

  validateExternalContract(
    organizationId: string,
    contractId: string,
    signedAt: string,
  ): Observable<ContractResponse> {
    return this.http.post<ContractResponse>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/validate-external`,
      { signedAt },
    );
  }

  downloadContractVersion(
    organizationId: string,
    contractId: string,
    versionId: string,
  ): Observable<Blob> {
    return this.http.get(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/versions/${versionId}/pdf`,
      { responseType: 'blob' },
    );
  }

  downloadFinalContract(organizationId: string, contractId: string): Observable<Blob> {
    return this.http.get(`${this.organizationUrl(organizationId)}/contracts/${contractId}/final`, {
      responseType: 'blob',
    });
  }

  addContractSigner(
    organizationId: string,
    contractId: string,
    request: {
      contractPartyId: string;
      personId: string | null;
      userAccountId: string | null;
      name: string;
      email: string;
      signerRole: string;
      signingOrder: number;
      isRequired: boolean;
    },
  ): Observable<ContractResponse['signers'][number]> {
    return this.http.post<ContractResponse['signers'][number]>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/signers`,
      request,
    );
  }

  createSignatureRequest(
    organizationId: string,
    contractId: string,
    signerId: string,
  ): Observable<SignatureRequestLink> {
    return this.http.post<SignatureRequestLink>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/signers/${signerId}/requests`,
      { expiresAt: null },
    );
  }

  signAsOrganization(
    organizationId: string,
    contractId: string,
    signerId: string,
    declaredSignerName: string,
  ): Observable<ContractResponse> {
    return this.http.post<ContractResponse>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/signers/${signerId}/sign`,
      {
        signingMethod: 'AuthenticatedConfirmation',
        declaredSignerName,
        acceptElectronicMeans: true,
        confirmDisplayedVersion: true,
        signatureDataUrl: null,
      },
    );
  }

  getContractingReadiness(
    organizationId: string,
    eventId: string,
  ): Observable<ContractingReadiness> {
    return this.http.get<ContractingReadiness>(
      `${this.eventUrl(organizationId, eventId)}/contracting-readiness`,
    );
  }

  getPortalContractingReadiness(eventId: string): Observable<ContractingReadiness> {
    return this.http.get<ContractingReadiness>(
      `${this.baseUrl}/client-portal/events/${eventId}/contracting-readiness`,
    );
  }

  confirmContractedEvent(
    organizationId: string,
    eventId: string,
  ): Observable<ContractingReadiness> {
    return this.http.post<ContractingReadiness>(
      `${this.eventUrl(organizationId, eventId)}/confirm`,
      null,
    );
  }

  getPaymentPlans(organizationId: string, eventId?: string): Observable<PaymentPlan[]> {
    const params = eventId ? new HttpParams().set('eventId', eventId) : undefined;
    return this.http.get<PaymentPlan[]>(`${this.organizationUrl(organizationId)}/payment-plans`, {
      params,
    });
  }

  createPaymentPlan(
    organizationId: string,
    request: {
      eventId: string;
      clientId: string;
      contractId: string | null;
      proposalVersionId: string | null;
      currencyCode: string;
      totalAmount: number;
      installments: {
        sequenceNumber: number;
        description: string;
        dueDate: string;
        amount: number;
        installmentType: 'Deposit' | 'ScheduledPayment' | 'FinalPayment' | 'AdditionalCharge';
      }[];
    },
  ): Observable<PaymentPlan> {
    return this.http.post<PaymentPlan>(
      `${this.organizationUrl(organizationId)}/payment-plans`,
      request,
    );
  }

  activatePaymentPlan(organizationId: string, planId: string): Observable<PaymentPlan> {
    return this.http.post<PaymentPlan>(
      `${this.organizationUrl(organizationId)}/payment-plans/${planId}/activate`,
      null,
    );
  }

  getPayments(organizationId: string, eventId?: string): Observable<PaymentRecord[]> {
    const params = eventId ? new HttpParams().set('eventId', eventId) : undefined;
    return this.http.get<PaymentRecord[]>(`${this.organizationUrl(organizationId)}/payments`, {
      params,
    });
  }

  createPayment(
    organizationId: string,
    request: {
      eventId: string;
      clientId: string;
      paymentPlanId: string | null;
      paymentDate: string;
      amount: number;
      currencyCode: string;
      method: PaymentMethod;
      reference: string | null;
      notesShared: string | null;
      internalNotes: string | null;
    },
  ): Observable<PaymentRecord> {
    return this.http.post<PaymentRecord>(
      `${this.organizationUrl(organizationId)}/payments`,
      request,
    );
  }

  approvePayment(organizationId: string, paymentId: string): Observable<PaymentRecord> {
    return this.http.post<PaymentRecord>(
      `${this.organizationUrl(organizationId)}/payments/${paymentId}/approve`,
      null,
    );
  }

  rejectPayment(
    organizationId: string,
    paymentId: string,
    reason: string,
  ): Observable<PaymentRecord> {
    return this.http.post<PaymentRecord>(
      `${this.organizationUrl(organizationId)}/payments/${paymentId}/reject`,
      { reason },
    );
  }

  allocatePayment(
    organizationId: string,
    paymentId: string,
    allocations: { paymentInstallmentId: string; amount: number }[],
  ): Observable<PaymentRecord> {
    return this.http.post<PaymentRecord>(
      `${this.organizationUrl(organizationId)}/payments/${paymentId}/allocations`,
      allocations,
    );
  }

  getPublicSignature(token: string): Observable<PublicSignatureContract> {
    return this.http.get<PublicSignatureContract>(
      `${this.baseUrl}/public/signatures/${encodeURIComponent(token)}`,
    );
  }

  submitPublicSignature(
    token: string,
    request: {
      signingMethod: SigningMethod;
      declaredSignerName: string;
      acceptElectronicMeans: boolean;
      confirmDisplayedVersion: boolean;
      signatureDataUrl: string | null;
    },
  ): Observable<PublicSignatureContract> {
    return this.http.post<PublicSignatureContract>(
      `${this.baseUrl}/public/signatures/${encodeURIComponent(token)}/sign`,
      request,
    );
  }

  declinePublicSignature(token: string, reason: string | null): Observable<void> {
    return this.http.post<void>(
      `${this.baseUrl}/public/signatures/${encodeURIComponent(token)}/decline`,
      { reason },
    );
  }

  downloadPublicContractPdf(token: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/public/signatures/${encodeURIComponent(token)}/pdf`, {
      responseType: 'blob',
    });
  }

  getPortalContracts(): Observable<PortalContractListItem[]> {
    return this.http.get<PortalContractListItem[]>(`${this.baseUrl}/client-portal/contracts`);
  }

  getPortalContract(contractId: string): Observable<PortalContract> {
    return this.http.get<PortalContract>(`${this.baseUrl}/client-portal/contracts/${contractId}`);
  }

  downloadPortalContract(contractId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/client-portal/contracts/${contractId}/pdf`, {
      responseType: 'blob',
    });
  }

  downloadPortalFinalContract(contractId: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/client-portal/contracts/${contractId}/final`, {
      responseType: 'blob',
    });
  }

  signPortalContract(
    contractId: string,
    signerId: string,
    declaredSignerName: string,
  ): Observable<PublicSignatureContract> {
    return this.http.post<PublicSignatureContract>(
      `${this.baseUrl}/client-portal/contracts/${contractId}/signers/${signerId}/sign`,
      {
        signingMethod: 'AuthenticatedConfirmation',
        declaredSignerName,
        acceptElectronicMeans: true,
        confirmDisplayedVersion: true,
        signatureDataUrl: null,
      },
    );
  }

  getPortalPaymentPlans(eventId?: string): Observable<PaymentPlan[]> {
    const params = eventId ? new HttpParams().set('eventId', eventId) : undefined;
    return this.http.get<PaymentPlan[]>(`${this.baseUrl}/client-portal/payment-plans`, { params });
  }

  getPortalPayments(eventId?: string): Observable<PortalPaymentRecord[]> {
    const params = eventId ? new HttpParams().set('eventId', eventId) : undefined;
    return this.http.get<PortalPaymentRecord[]>(`${this.baseUrl}/client-portal/payments`, {
      params,
    });
  }

  createPortalPayment(request: {
    paymentPlanId: string;
    paymentDate: string;
    amount: number;
    method: PaymentMethod;
    reference: string | null;
    notesShared: string | null;
  }): Observable<PortalPaymentRecord> {
    return this.http.post<PortalPaymentRecord>(`${this.baseUrl}/client-portal/payments`, request);
  }

  uploadPortalPaymentReceipt(
    paymentId: string,
    file: File,
  ): Observable<PortalPaymentRecord['receipts'][number]> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<PortalPaymentRecord['receipts'][number]>(
      `${this.baseUrl}/client-portal/payments/${paymentId}/receipt`,
      form,
    );
  }

  private organizationUrl(organizationId: string): string {
    return `${this.baseUrl}/organizations/${organizationId}`;
  }

  private eventUrl(organizationId: string, eventId: string): string {
    return `${this.organizationUrl(organizationId)}/events/${eventId}`;
  }
}
