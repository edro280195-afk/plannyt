export type OrganizationType = 'IndependentPlanner' | 'Agency';
export type OrganizationRole =
  | 'Owner'
  | 'OrganizationAdmin'
  | 'Planner'
  | 'Coordinator'
  | 'Assistant'
  | 'Commercial'
  | 'Finance';
export type ClientType = 'Person' | 'Company';
export type ClientStatus = 'Active' | 'Inactive' | 'Archived';
export type EventStatus =
  'Preliminary' | 'Confirmed' | 'Planning' | 'Suspended' | 'Cancelled' | 'Closed' | 'Archived';
export type EventClientRelationshipType =
  'ContractingClient' | 'PrimaryClient' | 'Payer' | 'Approver' | 'Other';
export type EventAccessRole =
  | 'ClientAuthority'
  | 'ClientPrimary'
  | 'ClientCollaborator'
  | 'ClientGuestManager'
  | 'ClientPayer'
  | 'ClientApprover'
  | 'ClientViewer';
export type DocumentVisibility = 'Internal' | 'ClientShared';
export type InvitationType = 'OrganizationMembership' | 'EventAccess';
export type InvitationPublicStatus = 'Pending' | 'Expired' | 'Accepted' | 'Revoked';

export interface AuthResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  userAccountId: string;
  email: string;
  organizationId: string | null;
}

export interface RegisterPlannerRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  organizationName: string;
  organizationType: OrganizationType;
  timeZone: string;
  countryCode: string;
  currencyCode: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  isPersistent: boolean;
}

export interface MeOrganization {
  organizationId: string;
  organizationName: string;
  membershipId: string;
  role: OrganizationRole;
  permissions: string[];
}

export interface MeEventAccess {
  organizationId: string;
  eventId: string;
  eventName: string;
  role: EventAccessRole;
}

export interface MeResponse {
  userAccountId: string;
  email: string;
  organizations: MeOrganization[];
  eventAccesses: MeEventAccess[];
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface OrganizationResponse {
  id: string;
  name: string;
  slug: string;
  organizationType: OrganizationType;
  timeZone: string;
  countryCode: string;
  currencyCode: string;
  status: string;
  createdAt: string;
  updatedAt: string;
}

export interface UpdateOrganizationRequest {
  name: string;
  organizationType: OrganizationType;
  timeZone: string;
  countryCode: string;
  currencyCode: string;
}

export interface OrganizationMember {
  membershipId: string;
  userAccountId: string;
  personId: string;
  displayName: string;
  email: string;
  role: OrganizationRole;
  status: string;
  joinedAt: string;
  expiresAt: string | null;
}

export interface PersonProfileRequest {
  firstName: string;
  lastName: string;
  contactEmail: string | null;
  contactPhone: string | null;
  preferredLanguage: string;
  timeZone: string;
}

export interface PersonProfileResponse extends PersonProfileRequest {
  id: string;
  displayName: string;
}

export interface CreateClientRequest {
  clientType: ClientType;
  displayName: string;
  companyName: string | null;
  source: string | null;
  person: PersonProfileRequest | null;
}

export interface UpdateClientRequest {
  displayName: string;
  companyName: string | null;
  source: string | null;
  person: PersonProfileRequest | null;
}

export interface ClientListItem {
  id: string;
  clientType: ClientType;
  displayName: string;
  companyName: string | null;
  status: ClientStatus;
  source: string | null;
  updatedAt: string;
}

export interface ClientContact {
  id: string;
  personId: string;
  displayName: string;
  contactEmail: string | null;
  contactPhone: string | null;
  contactRole: string;
  isPrimary: boolean;
}

export interface ClientResponse {
  id: string;
  clientType: ClientType;
  displayName: string;
  companyName: string | null;
  status: ClientStatus;
  source: string | null;
  person: PersonProfileResponse | null;
  contacts: ClientContact[];
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
}

export interface EventDetailsRequest {
  name: string;
  eventType: string;
  startDateTime: string;
  endDateTime: string | null;
  timeZone: string;
  city: string;
  countryCode: string;
  sharedDescription: string | null;
  estimatedGuestCount: number | null;
}

export interface EventListItem {
  id: string;
  name: string;
  eventType: string;
  status: EventStatus;
  startDateTime: string;
  endDateTime: string | null;
  timeZone: string;
  city: string;
  estimatedGuestCount: number | null;
  updatedAt: string;
}

export interface EventStatusHistory {
  id: string;
  previousStatus: EventStatus;
  newStatus: EventStatus;
  reason: string | null;
  changedBy: string;
  changedAt: string;
}

export interface EventResponse extends EventDetailsRequest {
  id: string;
  organizationId: string;
  status: EventStatus;
  createdBy: string;
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
  statusHistory: EventStatusHistory[];
}

export interface EventClient {
  id: string;
  clientId: string;
  clientDisplayName: string;
  relationshipType: EventClientRelationshipType;
  isPrimary: boolean;
  hasTransferAuthority: boolean;
}

export interface EventParticipant {
  id: string;
  personId: string;
  displayName: string;
  contactEmail: string | null;
  contactPhone: string | null;
  participantType: string;
  displayOrder: number;
  isVisibleToClient: boolean;
  sharedDescription: string | null;
}

export interface UpsertParticipantRequest extends PersonProfileRequest {
  participantType: string;
  displayOrder: number;
  isVisibleToClient: boolean;
  sharedDescription: string | null;
}

export interface EventAccess {
  id: string;
  userAccountId: string;
  email: string;
  role: EventAccessRole;
  status: string;
  startsAt: string;
  expiresAt: string | null;
  revokedAt: string | null;
}

export interface InvitationCreated {
  id: string;
  invitationType: InvitationType;
  targetEmail: string;
  expiresAt: string;
  invitationUrl: string;
}

export interface InvitationPublic {
  invitationType: InvitationType;
  organizationName: string;
  eventName: string | null;
  targetEmail: string;
  intendedRole: OrganizationRole | EventAccessRole;
  expiresAt: string;
  status: InvitationPublicStatus;
}

export interface AcceptInvitationRequest {
  firstName: string | null;
  lastName: string | null;
  contactPhone: string | null;
  preferredLanguage: string | null;
  timeZone: string | null;
}

export interface RegisterAndAcceptInvitationRequest {
  password: string;
  firstName: string;
  lastName: string;
  contactPhone: string | null;
  preferredLanguage: string;
  timeZone: string;
}

export interface InvitationAcceptance {
  invitationType: InvitationType;
  organizationId: string | null;
  eventId: string | null;
}

export interface DocumentResponse {
  id: string;
  documentType: string;
  fileName: string;
  mimeType: string;
  sizeBytes: number;
  visibility?: DocumentVisibility;
  uploadedBy?: string;
  createdAt: string;
}

export interface PortalParticipant {
  id: string;
  displayName: string;
  participantType: string;
  displayOrder: number;
  sharedDescription: string | null;
}

export interface PortalEvent {
  id: string;
  name: string;
  eventType: string;
  startDateTime: string;
  endDateTime: string | null;
  timeZone: string;
  city: string;
  countryCode: string;
  sharedDescription: string | null;
  estimatedGuestCount: number | null;
}

export interface PortalEventDetail extends PortalEvent {
  participants: PortalParticipant[];
  documents: DocumentResponse[];
}
