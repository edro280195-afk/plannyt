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
  EventGuest,
  GuestAccessLink,
  GuestDashboard,
  GuestDuplicateSuggestion,
  GuestExperience,
  GuestImportAnalysis,
  GuestImportResult,
  GuestImportTemplateFormat,
  GuestImportTemplateLanguage,
  GuestTag,
  GuestType,
  AgeCategory,
  InvitationBlock,
  InvitationDesign,
  InvitationGroup,
  InvitationGroupType,
  InvitationTemplate,
  InvitationTheme,
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
  PortalGuest,
  PortalGuestWorkspace,
  PortalInvitationGroup,
  PublicInvitation,
  PublicSignatureContract,
  RegisterAndAcceptInvitationRequest,
  RegisterPlannerRequest,
  ServiceCatalogItem,
  ServiceCatalogItemRequest,
  SignatureEvidenceSummary,
  SignatureRequestLink,
  SigningMethod,
  UpdateClientRequest,
  UpdateOrganizationRequest,
  UpsertParticipantRequest,
  EventAccommodationOptionRequest,
  EventAccommodationOptionResponse,
  EventMenuOptionRequest,
  EventMenuOptionResponse,
  EventMenuRequest,
  EventMenuResponse,
  EventTransportOptionRequest,
  EventTransportOptionResponse,
  GuestRsvpStateResponse,
  ManualRsvpRequest,
  MarkReminderRequest,
  OpenGroupExceptionRequest,
  ReminderTemplateRequest,
  ReminderTemplateResponse,
  RsvpDashboardResponse,
  RsvpFormResponse,
  RsvpFormVersionResponse,
  RsvpQuestionCatalog,
  RsvpGroupSummaryResponse,
  RsvpSettingsRequest,
  RsvpSettingsResponse,
  RsvpSubmissionRequest,
  RsvpSubmissionResponse,
  SensitiveGuestDataResponse,
  SensitiveQuestionAnswerResponse,
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

  linkProposalPreliminaryEvent(
    organizationId: string,
    proposalId: string,
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
  ): Observable<{ eventId: string }> {
    return this.http.post<{ eventId: string }>(
      `${this.organizationUrl(organizationId)}/proposals/${proposalId}/preliminary-event`,
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

  archiveContractTemplate(organizationId: string, templateId: string): Observable<unknown> {
    return this.http.delete(
      `${this.organizationUrl(organizationId)}/contract-templates/${templateId}`,
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

  cancelContract(
    organizationId: string,
    contractId: string,
    reason: string,
  ): Observable<unknown> {
    return this.http.post(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/cancel`,
      { reason },
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

  revokeSignatureRequest(
    organizationId: string,
    contractId: string,
    requestId: string,
  ): Observable<unknown> {
    return this.http.delete(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/requests/${requestId}`,
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

  getContractEvidence(
    organizationId: string,
    contractId: string,
  ): Observable<SignatureEvidenceSummary[]> {
    return this.http.get<SignatureEvidenceSummary[]>(
      `${this.organizationUrl(organizationId)}/contracts/${contractId}/evidence`,
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

  getGuestDashboard(
    organizationId: string,
    eventId: string,
    filters?: { search?: string; groupId?: string; tagId?: string; includeArchived?: boolean },
  ): Observable<GuestDashboard> {
    let params = new HttpParams();
    if (filters?.search) params = params.set('search', filters.search);
    if (filters?.groupId) params = params.set('groupId', filters.groupId);
    if (filters?.tagId) params = params.set('tagId', filters.tagId);
    if (filters?.includeArchived) params = params.set('includeArchived', true);
    return this.http.get<GuestDashboard>(`${this.guestUrl(organizationId, eventId)}/dashboard`, {
      params,
    });
  }

  createInvitationGroup(
    organizationId: string,
    eventId: string,
    request: {
      groupType: InvitationGroupType;
      displayName: string;
      contactName: string | null;
      contactPhone: string | null;
      contactEmail: string | null;
      allowedGuestCount: number;
      allowUnnamedCompanions: boolean;
      maxUnnamedCompanions: number;
      internalNotes: string | null;
      tagIds: string[];
      applyCapacityOverride?: boolean;
    },
  ): Observable<InvitationGroup> {
    return this.http.post<InvitationGroup>(
      `${this.guestUrl(organizationId, eventId)}/groups`,
      request,
    );
  }

  updateInvitationGroup(
    organizationId: string,
    eventId: string,
    groupId: string,
    request: {
      groupType: InvitationGroupType;
      displayName: string;
      contactName: string | null;
      contactPhone: string | null;
      contactEmail: string | null;
      allowedGuestCount: number;
      allowUnnamedCompanions: boolean;
      maxUnnamedCompanions: number;
      internalNotes: string | null;
      tagIds: string[];
      applyCapacityOverride?: boolean;
    },
  ): Observable<InvitationGroup> {
    return this.http.put<InvitationGroup>(
      `${this.guestUrl(organizationId, eventId)}/groups/${groupId}`,
      request,
    );
  }

  archiveInvitationGroup(
    organizationId: string,
    eventId: string,
    groupId: string,
  ): Observable<void> {
    return this.http.delete<void>(`${this.guestUrl(organizationId, eventId)}/groups/${groupId}`);
  }

  createEventGuest(
    organizationId: string,
    eventId: string,
    request: {
      invitationGroupId: string | null;
      personId: string | null;
      firstName: string;
      lastName: string;
      email: string | null;
      phone: string | null;
      guestType: GuestType;
      ageCategory: AgeCategory;
      isPrimaryContact: boolean;
      isNamed: boolean;
      isPlusOne: boolean;
      isVip: boolean;
      sortOrder: number;
      internalNotes: string | null;
    },
  ): Observable<EventGuest> {
    return this.http.post<EventGuest>(this.guestUrl(organizationId, eventId), request);
  }

  updateEventGuest(
    organizationId: string,
    eventId: string,
    guestId: string,
    request: {
      invitationGroupId: string | null;
      personId: string | null;
      firstName: string;
      lastName: string;
      email: string | null;
      phone: string | null;
      guestType: GuestType;
      ageCategory: AgeCategory;
      isPrimaryContact: boolean;
      isNamed: boolean;
      isPlusOne: boolean;
      isVip: boolean;
      sortOrder: number;
      internalNotes: string | null;
    },
  ): Observable<EventGuest> {
    return this.http.put<EventGuest>(
      `${this.guestUrl(organizationId, eventId)}/${guestId}`,
      request,
    );
  }

  archiveEventGuest(organizationId: string, eventId: string, guestId: string): Observable<void> {
    return this.http.delete<void>(`${this.guestUrl(organizationId, eventId)}/${guestId}`);
  }

  getGuestTags(organizationId: string, eventId: string): Observable<GuestTag[]> {
    return this.http.get<GuestTag[]>(`${this.guestUrl(organizationId, eventId)}/tags`);
  }

  createGuestTag(
    organizationId: string,
    eventId: string,
    request: { name: string; colorToken: string },
  ): Observable<GuestTag> {
    return this.http.post<GuestTag>(`${this.guestUrl(organizationId, eventId)}/tags`, request);
  }

  updateGuestTag(
    organizationId: string,
    eventId: string,
    tagId: string,
    request: { name: string; colorToken: string },
  ): Observable<GuestTag> {
    return this.http.put<GuestTag>(
      `${this.guestUrl(organizationId, eventId)}/tags/${tagId}`,
      request,
    );
  }

  archiveGuestTag(organizationId: string, eventId: string, tagId: string): Observable<void> {
    return this.http.delete<void>(`${this.guestUrl(organizationId, eventId)}/tags/${tagId}`);
  }

  getGuestDuplicates(
    organizationId: string,
    eventId: string,
  ): Observable<GuestDuplicateSuggestion[]> {
    return this.http.get<GuestDuplicateSuggestion[]>(
      `${this.guestUrl(organizationId, eventId)}/duplicates`,
    );
  }

  analyzeGuestImport(
    organizationId: string,
    eventId: string,
    file: File,
  ): Observable<GuestImportAnalysis> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<GuestImportAnalysis>(
      `${this.guestUrl(organizationId, eventId)}/imports/analyze`,
      form,
    );
  }

  updateGuestImportMapping(
    organizationId: string,
    eventId: string,
    importId: string,
    mapping: Record<string, string>,
  ): Observable<GuestImportAnalysis> {
    return this.http.put<GuestImportAnalysis>(
      `${this.guestUrl(organizationId, eventId)}/imports/${importId}/mapping`,
      { mapping },
    );
  }

  confirmGuestImport(
    organizationId: string,
    eventId: string,
    importId: string,
  ): Observable<GuestImportResult> {
    return this.http.post<GuestImportResult>(
      `${this.guestUrl(organizationId, eventId)}/imports/${importId}/confirm`,
      null,
    );
  }

  downloadGuestTemplate(
    organizationId: string,
    eventId: string,
    format: GuestImportTemplateFormat = 'csv',
    language: GuestImportTemplateLanguage = 'es',
  ): Observable<Blob> {
    return this.http.get(
      `${this.guestUrl(organizationId, eventId)}/imports/template?format=${format}&language=${language}`,
      { responseType: 'blob' },
    );
  }

  exportGuests(organizationId: string, eventId: string): Observable<Blob> {
    return this.http.get(`${this.guestUrl(organizationId, eventId)}/export`, {
      responseType: 'blob',
    });
  }

  getGuestExperience(organizationId: string, eventId: string): Observable<GuestExperience> {
    return this.http.get<GuestExperience>(
      `${this.invitationUrl(organizationId, eventId)}/experience`,
    );
  }

  updateGuestExperience(
    organizationId: string,
    eventId: string,
    request: {
      language: string;
      publicTitle: string;
      celebrantDisplayName: string;
      welcomeMessage: string | null;
      closingMessage: string | null;
      showEventName: boolean;
      showEventDate: boolean;
      showParticipantNames: boolean;
      showCity: boolean;
      privateAccessOnly: boolean;
    },
  ): Observable<GuestExperience> {
    return this.http.put<GuestExperience>(
      `${this.invitationUrl(organizationId, eventId)}/experience`,
      request,
    );
  }

  suspendGuestExperience(organizationId: string, eventId: string): Observable<GuestExperience> {
    return this.http.post<GuestExperience>(
      `${this.invitationUrl(organizationId, eventId)}/experience/suspend`,
      null,
    );
  }

  resumeGuestExperience(organizationId: string, eventId: string): Observable<GuestExperience> {
    return this.http.post<GuestExperience>(
      `${this.invitationUrl(organizationId, eventId)}/experience/resume`,
      null,
    );
  }

  getInvitationTemplates(
    organizationId: string,
    eventId: string,
  ): Observable<InvitationTemplate[]> {
    return this.http.get<InvitationTemplate[]>(
      `${this.invitationUrl(organizationId, eventId)}/templates`,
    );
  }

  createInvitationTemplate(
    organizationId: string,
    eventId: string,
    request: {
      name: string;
      description: string;
      theme: InvitationTheme;
      blocks: InvitationBlock[];
    },
  ): Observable<InvitationTemplate> {
    return this.http.post<InvitationTemplate>(
      `${this.invitationUrl(organizationId, eventId)}/templates`,
      request,
    );
  }

  updateInvitationTemplate(
    organizationId: string,
    eventId: string,
    templateId: string,
    request: {
      name: string;
      description: string;
      theme: InvitationTheme;
      blocks: InvitationBlock[];
    },
  ): Observable<InvitationTemplate> {
    return this.http.put<InvitationTemplate>(
      `${this.invitationUrl(organizationId, eventId)}/templates/${templateId}`,
      request,
    );
  }

  archiveInvitationTemplate(
    organizationId: string,
    eventId: string,
    templateId: string,
  ): Observable<void> {
    return this.http.delete<void>(
      `${this.invitationUrl(organizationId, eventId)}/templates/${templateId}`,
    );
  }

  getInvitationDesigns(organizationId: string, eventId: string): Observable<InvitationDesign[]> {
    return this.http.get<InvitationDesign[]>(
      `${this.invitationUrl(organizationId, eventId)}/designs`,
    );
  }

  createInvitationDesign(
    organizationId: string,
    eventId: string,
    request: { name: string; templateId: string | null },
  ): Observable<InvitationDesign> {
    return this.http.post<InvitationDesign>(
      `${this.invitationUrl(organizationId, eventId)}/designs`,
      request,
    );
  }

  updateInvitationDesign(
    organizationId: string,
    eventId: string,
    designId: string,
    request: { name: string; theme: InvitationTheme; blocks: InvitationBlock[] },
  ): Observable<InvitationDesign> {
    return this.http.put<InvitationDesign>(
      `${this.invitationUrl(organizationId, eventId)}/designs/${designId}`,
      request,
    );
  }

  submitInvitationReview(
    organizationId: string,
    eventId: string,
    designId: string,
  ): Observable<InvitationDesign> {
    return this.http.post<InvitationDesign>(
      `${this.invitationUrl(organizationId, eventId)}/designs/${designId}/submit-review`,
      null,
    );
  }

  reviewInvitationDesign(
    organizationId: string,
    eventId: string,
    designId: string,
    versionId: string,
    action: 'comments' | 'approve' | 'request-changes',
    message: string,
  ): Observable<InvitationDesign> {
    return this.http.post<InvitationDesign>(
      `${this.invitationUrl(organizationId, eventId)}/designs/${designId}/versions/${versionId}/${action}`,
      { message },
    );
  }

  publishInvitationDesign(
    organizationId: string,
    eventId: string,
    designId: string,
    bypassApprovalForTesting = false,
  ): Observable<InvitationDesign> {
    return this.http.post<InvitationDesign>(
      `${this.invitationUrl(organizationId, eventId)}/designs/${designId}/publish`,
      { bypassApprovalForTesting },
    );
  }

  getGuestLinks(organizationId: string, eventId: string): Observable<GuestAccessLink[]> {
    return this.http.get<GuestAccessLink[]>(`${this.invitationUrl(organizationId, eventId)}/links`);
  }

  generateGuestLink(
    organizationId: string,
    eventId: string,
    groupId: string,
    expiresAt: string | null,
  ): Observable<GuestAccessLink> {
    return this.http.post<GuestAccessLink>(
      `${this.invitationUrl(organizationId, eventId)}/groups/${groupId}/links`,
      { expiresAt },
    );
  }

  regenerateGuestLink(
    organizationId: string,
    eventId: string,
    linkId: string,
    expiresAt: string | null,
  ): Observable<GuestAccessLink> {
    return this.http.post<GuestAccessLink>(
      `${this.invitationUrl(organizationId, eventId)}/links/${linkId}/regenerate`,
      { expiresAt },
    );
  }

  markGuestLinkShared(
    organizationId: string,
    eventId: string,
    linkId: string,
  ): Observable<GuestAccessLink> {
    return this.http.post<GuestAccessLink>(
      `${this.invitationUrl(organizationId, eventId)}/links/${linkId}/mark-shared`,
      null,
    );
  }

  revokeGuestLink(organizationId: string, eventId: string, linkId: string): Observable<void> {
    return this.http.delete<void>(`${this.invitationUrl(organizationId, eventId)}/links/${linkId}`);
  }

  getPublicInvitation(token: string): Observable<PublicInvitation> {
    return this.http.get<PublicInvitation>(
      `${this.baseUrl}/public/invitations/${encodeURIComponent(token)}`,
    );
  }

  getPortalGuestWorkspace(eventId: string): Observable<PortalGuestWorkspace> {
    return this.http.get<PortalGuestWorkspace>(`${this.portalGuestUrl(eventId)}`);
  }

  createPortalInvitationGroup(
    eventId: string,
    request: {
      groupType: InvitationGroupType;
      displayName: string;
      allowedGuestCount: number;
      allowUnnamedCompanions: boolean;
      maxUnnamedCompanions: number;
    },
  ): Observable<PortalInvitationGroup> {
    return this.http.post<PortalInvitationGroup>(`${this.portalGuestUrl(eventId)}/groups`, request);
  }

  updatePortalInvitationGroup(
    eventId: string,
    groupId: string,
    request: {
      groupType: InvitationGroupType;
      displayName: string;
      allowedGuestCount: number;
      allowUnnamedCompanions: boolean;
      maxUnnamedCompanions: number;
    },
  ): Observable<PortalInvitationGroup> {
    return this.http.put<PortalInvitationGroup>(
      `${this.portalGuestUrl(eventId)}/groups/${groupId}`,
      request,
    );
  }

  archivePortalInvitationGroup(eventId: string, groupId: string): Observable<void> {
    return this.http.delete<void>(`${this.portalGuestUrl(eventId)}/groups/${groupId}`);
  }

  createPortalGuest(
    eventId: string,
    request: {
      invitationGroupId: string | null;
      firstName: string;
      lastName: string;
      guestType: GuestType;
      ageCategory: AgeCategory;
      isPrimaryContact: boolean;
      isVip: boolean;
      sortOrder: number;
    },
  ): Observable<PortalGuest> {
    return this.http.post<PortalGuest>(`${this.portalGuestUrl(eventId)}/guests`, request);
  }

  updatePortalGuest(
    eventId: string,
    guestId: string,
    request: {
      invitationGroupId: string | null;
      firstName: string;
      lastName: string;
      guestType: GuestType;
      ageCategory: AgeCategory;
      isPrimaryContact: boolean;
      isVip: boolean;
      sortOrder: number;
    },
  ): Observable<PortalGuest> {
    return this.http.put<PortalGuest>(`${this.portalGuestUrl(eventId)}/guests/${guestId}`, request);
  }

  archivePortalGuest(eventId: string, guestId: string): Observable<void> {
    return this.http.delete<void>(`${this.portalGuestUrl(eventId)}/guests/${guestId}`);
  }

  reviewPortalInvitation(
    eventId: string,
    designId: string,
    versionId: string,
    action: 'comments' | 'approve' | 'request-changes',
    message: string,
  ): Observable<InvitationDesign> {
    return this.http.post<InvitationDesign>(
      `${this.portalGuestUrl(eventId)}/designs/${designId}/versions/${versionId}/${action}`,
      { message },
    );
  }

  analyzePortalGuestImport(eventId: string, file: File): Observable<GuestImportAnalysis> {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<GuestImportAnalysis>(
      `${this.portalGuestUrl(eventId)}/imports/analyze`,
      form,
    );
  }

  updatePortalGuestImportMapping(
    eventId: string,
    importId: string,
    mapping: Record<string, string>,
  ): Observable<GuestImportAnalysis> {
    return this.http.put<GuestImportAnalysis>(
      `${this.portalGuestUrl(eventId)}/imports/${importId}/mapping`,
      { mapping },
    );
  }

  confirmPortalGuestImport(eventId: string, importId: string): Observable<GuestImportResult> {
    return this.http.post<GuestImportResult>(
      `${this.portalGuestUrl(eventId)}/imports/${importId}/confirm`,
      null,
    );
  }

  downloadPortalGuestImportTemplate(
    eventId: string,
    format: GuestImportTemplateFormat = 'csv',
    language: GuestImportTemplateLanguage = 'es',
  ): Observable<Blob> {
    return this.http.get(
      `${this.portalGuestUrl(eventId)}/imports/template?format=${format}&language=${language}`,
      { responseType: 'blob' },
    );
  }

  getPortalGuestDuplicates(eventId: string): Observable<GuestDuplicateSuggestion[]> {
    return this.http.get<GuestDuplicateSuggestion[]>(`${this.portalGuestUrl(eventId)}/duplicates`);
  }

  getPortalGuestLinks(eventId: string): Observable<GuestAccessLink[]> {
    return this.http.get<GuestAccessLink[]>(`${this.portalGuestUrl(eventId)}/links`);
  }

  markPortalGuestLinkShared(eventId: string, linkId: string): Observable<GuestAccessLink> {
    return this.http.post<GuestAccessLink>(
      `${this.portalGuestUrl(eventId)}/links/${linkId}/mark-shared`,
      null,
    );
  }

  // === RSVP ===

  getRsvpSettings(organizationId: string, eventId: string): Observable<RsvpSettingsResponse> {
    return this.http.get<RsvpSettingsResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/settings`);
  }

  updateRsvpSettings(organizationId: string, eventId: string, request: RsvpSettingsRequest): Observable<RsvpSettingsResponse> {
    return this.http.put<RsvpSettingsResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/settings`, request);
  }

  publishRsvpSettings(organizationId: string, eventId: string): Observable<RsvpSettingsResponse> {
    return this.http.post<RsvpSettingsResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/settings/publish`, {});
  }

  openRsvp(organizationId: string, eventId: string): Observable<RsvpSettingsResponse> {
    return this.http.post<RsvpSettingsResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/settings/open`, {});
  }

  closeRsvp(organizationId: string, eventId: string): Observable<RsvpSettingsResponse> {
    return this.http.post<RsvpSettingsResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/settings/close`, {});
  }

  getRsvpForm(organizationId: string, eventId: string): Observable<RsvpFormResponse> {
    return this.http.get<RsvpFormResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/form`);
  }

  createRsvpForm(organizationId: string, eventId: string): Observable<RsvpFormResponse> {
    return this.http.post<RsvpFormResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/form`, {});
  }

  createRsvpFormDraft(
    organizationId: string,
    eventId: string,
  ): Observable<RsvpFormResponse> {
    return this.http.post<RsvpFormResponse>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/form/new-draft`,
      {},
    );
  }

  getRsvpQuestionCatalog(
    organizationId: string,
    eventId: string,
  ): Observable<RsvpQuestionCatalog> {
    return this.http.get<RsvpQuestionCatalog>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/form/question-catalog`,
    );
  }

  getRsvpFormVersion(
    organizationId: string,
    eventId: string,
    versionId: string,
  ): Observable<RsvpFormVersionResponse> {
    return this.http.get<RsvpFormVersionResponse>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/form/versions/${versionId}`,
    );
  }

  getRsvpDraftFormVersion(
    organizationId: string,
    eventId: string,
  ): Observable<RsvpFormVersionResponse> {
    return this.http.get<RsvpFormVersionResponse>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/form/draft-version`,
    );
  }

  createRsvpFormVersion(organizationId: string, eventId: string, questionsJson: string, menuJson: string, transportJson: string, accommodationJson: string): Observable<RsvpFormVersionResponse> {
    return this.http.post<RsvpFormVersionResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/form/version`, { questionsJson, menuJson, transportJson, accommodationJson });
  }

  submitRsvpFormReview(organizationId: string, eventId: string): Observable<RsvpFormResponse> {
    return this.http.post<RsvpFormResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/form/submit-review`, {});
  }

  approveRsvpForm(organizationId: string, eventId: string, versionId: string): Observable<RsvpFormVersionResponse> {
    return this.http.post<RsvpFormVersionResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/form/versions/${versionId}/approve`, {});
  }

  publishRsvpForm(organizationId: string, eventId: string, versionId: string): Observable<RsvpFormVersionResponse> {
    return this.http.post<RsvpFormVersionResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/form/versions/${versionId}/publish`, {});
  }

  getRsvpDashboard(organizationId: string, eventId: string): Observable<RsvpDashboardResponse> {
    return this.http.get<RsvpDashboardResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/dashboard`);
  }

  getPortalRsvpDashboard(eventId: string): Observable<RsvpDashboardResponse> {
    return this.http.get<RsvpDashboardResponse>(
      `${this.baseUrl}/client-portal/events/${eventId}/rsvp/dashboard`,
    );
  }

  getPortalRsvpForm(
    eventId: string,
  ): Observable<RsvpFormVersionResponse> {
    return this.http.get<RsvpFormVersionResponse>(
      `${this.baseUrl}/client-portal/events/${eventId}/rsvp/form`,
    );
  }

  getRsvpSensitiveData(
    organizationId: string,
    eventId: string,
  ): Observable<SensitiveGuestDataResponse[]> {
    return this.http.get<SensitiveGuestDataResponse[]>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/sensitive-data`,
    );
  }

  getRsvpSensitiveQuestionAnswers(
    organizationId: string,
    eventId: string,
  ): Observable<SensitiveQuestionAnswerResponse[]> {
    return this.http.get<SensitiveQuestionAnswerResponse[]>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/sensitive-question-answers`,
    );
  }

  exportRsvpSensitiveData(organizationId: string, eventId: string): Observable<Blob> {
    return this.http.get(
      `${this.eventUrl(organizationId, eventId)}/rsvp/exports/sensitive`,
      { responseType: 'blob' },
    );
  }

  manualRsvpCapture(
    organizationId: string,
    eventId: string,
    groupId: string,
    request: ManualRsvpRequest,
    idempotencyKey: string,
  ): Observable<RsvpSubmissionResponse> {
    return this.http.post<RsvpSubmissionResponse>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/groups/${groupId}/manual-capture`,
      request,
      { headers: { 'Idempotency-Key': idempotencyKey } },
    );
  }

  manualPortalRsvpCapture(
    eventId: string,
    groupId: string,
    request: ManualRsvpRequest,
    idempotencyKey: string,
  ): Observable<RsvpSubmissionResponse> {
    return this.http.post<RsvpSubmissionResponse>(
      `${this.baseUrl}/client-portal/events/${eventId}/rsvp/groups/${groupId}/manual-capture`,
      request,
      { headers: { 'Idempotency-Key': idempotencyKey } },
    );
  }

  openRsvpGroupException(organizationId: string, eventId: string, groupId: string, request: OpenGroupExceptionRequest): Observable<void> {
    return this.http.post<void>(`${this.eventUrl(organizationId, eventId)}/rsvp/groups/${groupId}/exception`, request);
  }

  closeRsvpGroupException(
    organizationId: string,
    eventId: string,
    groupId: string,
  ): Observable<void> {
    return this.http.post<void>(
      `${this.eventUrl(organizationId, eventId)}/rsvp/groups/${groupId}/exception/close`,
      null,
    );
  }

  // Menus
  getEventMenus(organizationId: string, eventId: string): Observable<EventMenuResponse[]> {
    return this.http.get<EventMenuResponse[]>(`${this.eventUrl(organizationId, eventId)}/menus`);
  }

  createEventMenu(organizationId: string, eventId: string, request: EventMenuRequest): Observable<EventMenuResponse> {
    return this.http.post<EventMenuResponse>(`${this.eventUrl(organizationId, eventId)}/menus`, request);
  }

  addMenuOption(organizationId: string, eventId: string, menuId: string, request: EventMenuOptionRequest): Observable<EventMenuOptionResponse> {
    return this.http.post<EventMenuOptionResponse>(`${this.eventUrl(organizationId, eventId)}/menus/${menuId}/options`, request);
  }

  // Transport
  getTransportOptions(organizationId: string, eventId: string): Observable<EventTransportOptionResponse[]> {
    return this.http.get<EventTransportOptionResponse[]>(`${this.eventUrl(organizationId, eventId)}/transport`);
  }

  createTransportOption(organizationId: string, eventId: string, request: EventTransportOptionRequest): Observable<EventTransportOptionResponse> {
    return this.http.post<EventTransportOptionResponse>(`${this.eventUrl(organizationId, eventId)}/transport`, request);
  }

  // Accommodation
  getAccommodationOptions(organizationId: string, eventId: string): Observable<EventAccommodationOptionResponse[]> {
    return this.http.get<EventAccommodationOptionResponse[]>(`${this.eventUrl(organizationId, eventId)}/accommodation`);
  }

  createAccommodationOption(organizationId: string, eventId: string, request: EventAccommodationOptionRequest): Observable<EventAccommodationOptionResponse> {
    return this.http.post<EventAccommodationOptionResponse>(`${this.eventUrl(organizationId, eventId)}/accommodation`, request);
  }

  // Reminders
  getReminderTemplates(organizationId: string, eventId: string): Observable<ReminderTemplateResponse[]> {
    return this.http.get<ReminderTemplateResponse[]>(`${this.eventUrl(organizationId, eventId)}/rsvp/reminders/templates`);
  }

  createReminderTemplate(organizationId: string, eventId: string, request: ReminderTemplateRequest): Observable<ReminderTemplateResponse> {
    return this.http.post<ReminderTemplateResponse>(`${this.eventUrl(organizationId, eventId)}/rsvp/reminders/templates`, request);
  }

  markReminderSent(organizationId: string, eventId: string, groupId: string, templateId: string, request: MarkReminderRequest): Observable<void> {
    return this.http.post<void>(`${this.eventUrl(organizationId, eventId)}/rsvp/reminders/groups/${groupId}/templates/${templateId}/mark-sent`, request);
  }

  // Public RSVP
  getGuestRsvpState(token: string): Observable<GuestRsvpStateResponse> {
    return this.http.get<GuestRsvpStateResponse>(`${this.baseUrl}/guest/rsvp/${token}/state`);
  }

  submitGuestRsvp(
    token: string,
    request: RsvpSubmissionRequest,
    idempotencyKey: string,
  ): Observable<RsvpSubmissionResponse> {
    return this.http.post<RsvpSubmissionResponse>(
      `${this.baseUrl}/guest/rsvp/${token}/submit`,
      request,
      { headers: { 'Idempotency-Key': idempotencyKey } },
    );
  }

  private organizationUrl(organizationId: string): string {
    return `${this.baseUrl}/organizations/${organizationId}`;
  }

  private eventUrl(organizationId: string, eventId: string): string {
    return `${this.organizationUrl(organizationId)}/events/${eventId}`;
  }

  private guestUrl(organizationId: string, eventId: string): string {
    return `${this.eventUrl(organizationId, eventId)}/guests`;
  }

  private invitationUrl(organizationId: string, eventId: string): string {
    return `${this.eventUrl(organizationId, eventId)}/invitations`;
  }

  private portalGuestUrl(eventId: string): string {
    return `${this.baseUrl}/client-portal/events/${eventId}/guest-experience`;
  }
}
