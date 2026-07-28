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

export type ProspectStatus =
  | 'New'
  | 'Contacted'
  | 'Qualified'
  | 'Opportunity'
  | 'ProposalDraft'
  | 'ProposalSent'
  | 'Negotiation'
  | 'Won'
  | 'Lost'
  | 'Archived';
export type ProspectActivityType =
  | 'Note'
  | 'Call'
  | 'WhatsApp'
  | 'Email'
  | 'Meeting'
  | 'FollowUp'
  | 'StatusChange'
  | 'ProposalSent'
  | 'ClientComment';
export type CommercialVisibility = 'Internal' | 'ClientShared';
export type PricingType = 'Fixed' | 'StartingAt' | 'PerUnit' | 'Custom';
export type TaxBehavior = 'Inclusive' | 'Exclusive' | 'Exempt';
export type DiscountType = 'None' | 'FixedAmount' | 'Percentage';
export type ProposalStatus =
  | 'Draft'
  | 'Ready'
  | 'Sent'
  | 'Viewed'
  | 'ChangesRequested'
  | 'Negotiation'
  | 'Accepted'
  | 'Rejected'
  | 'Expired'
  | 'Cancelled';
export type ProposalCommentVisibility = 'Internal' | 'ClientShared';
export type ProposalCommentStatus = 'Pending' | 'Resolved';

export interface ProspectDetailsRequest {
  displayName: string;
  firstName: string | null;
  lastName: string | null;
  companyName: string | null;
  email: string | null;
  phone: string | null;
  source: string | null;
  eventTypeInterest: string | null;
  estimatedEventDate: string | null;
  estimatedGuestCount: number | null;
  estimatedBudget: number | null;
  currencyCode: string;
  city: string | null;
  notes: string | null;
  assignedUserId: string | null;
}

export interface ProspectListItem {
  id: string;
  displayName: string;
  email: string | null;
  phone: string | null;
  eventTypeInterest: string | null;
  estimatedEventDate: string | null;
  estimatedBudget: number | null;
  currencyCode: string;
  assignedUserId: string | null;
  status: ProspectStatus;
  updatedAt: string;
}

export interface ProspectActivity {
  id: string;
  activityType: ProspectActivityType;
  subject: string;
  description: string | null;
  scheduledAt: string | null;
  completedAt: string | null;
  assignedUserId: string | null;
  visibility: CommercialVisibility;
  createdBy: string;
  createdAt: string;
}

export interface ProspectStatusHistory {
  id: string;
  previousStatus: ProspectStatus;
  newStatus: ProspectStatus;
  reason: string | null;
  changedBy: string;
  changedAt: string;
}

export interface ProspectResponse extends ProspectDetailsRequest {
  id: string;
  status: ProspectStatus;
  lostReason: string | null;
  convertedClientId: string | null;
  activities: ProspectActivity[];
  statusHistory: ProspectStatusHistory[];
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
}

export interface CreateProspectActivityRequest {
  activityType: ProspectActivityType;
  subject: string;
  description: string | null;
  scheduledAt: string | null;
  completedAt: string | null;
  assignedUserId: string | null;
  visibility: CommercialVisibility;
}

export interface ClientMatchSuggestion {
  clientId: string;
  displayName: string;
  matchField: string;
  matchValue: string;
}

export interface ConvertProspectResponse {
  prospectId: string;
  clientId: string;
  createdNewClient: boolean;
}

export interface ServiceCatalogItemRequest {
  name: string;
  description: string | null;
  category: string;
  pricingType: PricingType;
  basePrice: number;
  currencyCode: string;
  taxBehavior: TaxBehavior;
  isNegotiable: boolean;
  isActive: boolean;
  sortOrder: number;
}

export interface ServiceCatalogItem extends ServiceCatalogItemRequest {
  id: string;
  updatedAt: string;
  archivedAt: string | null;
}

export interface PackageItemRequest {
  serviceCatalogItemId: string;
  quantity: number;
  isOptional: boolean;
  includedPrice: number | null;
  sortOrder: number;
}

export interface PackageRequest {
  name: string;
  description: string | null;
  basePrice: number;
  currencyCode: string;
  isNegotiable: boolean;
  isActive: boolean;
  items: PackageItemRequest[];
}

export interface PackageItem extends PackageItemRequest {
  id: string;
  serviceName: string;
}

export interface CatalogPackage extends Omit<PackageRequest, 'items'> {
  id: string;
  items: PackageItem[];
  updatedAt: string;
  archivedAt: string | null;
}

export interface CouponRequest {
  code: string;
  description: string | null;
  discountType: DiscountType;
  discountValue: number;
  startsAt: string;
  endsAt: string;
  maximumUses: number | null;
  isActive: boolean;
}

export interface Coupon extends CouponRequest {
  id: string;
  currentUses: number;
}

export interface ProposalDraftLineRequest {
  description: string;
  serviceCatalogItemId: string | null;
  packageId: string | null;
  quantity: number;
  unitPrice: number;
  discountType: DiscountType;
  discountValue: number;
  taxRate: number;
  isOptional: boolean;
  sortOrder: number;
}

export interface ProposalDraftRequest {
  prospectId: string | null;
  clientId: string | null;
  eventId: string | null;
  currencyCode: string;
  validUntil: string;
  sharedIntroduction: string | null;
  sharedTerms: string | null;
  internalNotes: string | null;
  generalDiscountType: DiscountType;
  generalDiscountValue: number;
  couponId: string | null;
  lines: ProposalDraftLineRequest[];
}

export interface ProposalTotals {
  subtotal: number;
  discountTotal: number;
  generalDiscountTotal: number;
  couponDiscountTotal: number;
  taxTotal: number;
  grandTotal: number;
}

export interface ProposalDraftLine extends ProposalDraftLineRequest {
  id: string;
  lineSubtotal: number;
  lineDiscount: number;
  lineTax: number;
  lineTotal: number;
}

export interface ProposalVersionSummary {
  id: string;
  versionNumber: number;
  grandTotal: number;
  currencyCode: string;
  validUntil: string;
  publishedAt: string | null;
}

export interface ProposalComment {
  id: string;
  proposalVersionId: string;
  proposalLineId: string | null;
  authorUserId: string | null;
  authorDisplayName: string;
  content: string;
  visibility: ProposalCommentVisibility;
  status: ProposalCommentStatus;
  parentCommentId: string | null;
  createdAt: string;
}

export interface ProposalListItem {
  id: string;
  proposalNumber: string;
  prospectId: string | null;
  clientId: string | null;
  eventId: string | null;
  targetDisplayName: string;
  status: ProposalStatus;
  currentVersionNumber: number;
  currencyCode: string;
  validUntil: string;
  grandTotal: number | null;
  updatedAt: string;
}

export interface ProposalResponse {
  id: string;
  proposalNumber: string;
  prospectId: string | null;
  clientId: string | null;
  eventId: string | null;
  status: ProposalStatus;
  currentVersionNumber: number;
  currencyCode: string;
  validUntil: string;
  sharedIntroduction: string | null;
  sharedTerms: string | null;
  internalNotes: string | null;
  generalDiscountType: DiscountType;
  generalDiscountValue: number;
  couponId: string | null;
  draftTotals: ProposalTotals;
  draftLines: ProposalDraftLine[];
  versions: ProposalVersionSummary[];
  comments: ProposalComment[];
  acceptedVersionId: string | null;
  acceptedAt: string | null;
  rejectedAt: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface ProposalShareLink {
  id: string;
  proposalVersionId: string;
  expiresAt: string;
  shareUrl: string;
}

export interface ProposalPublicLine {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineDiscount: number;
  lineTax: number;
  lineTotal: number;
  isOptional: boolean;
  sortOrder: number;
}

export interface ProposalPublicComment {
  id: string;
  proposalLineId: string | null;
  authorDisplayName: string;
  content: string;
  status: ProposalCommentStatus;
  parentCommentId: string | null;
  createdAt: string;
}

export interface ProposalPublicResponse {
  proposalId: string;
  versionId: string;
  proposalNumber: string;
  versionNumber: number;
  organizationName: string;
  recipientName: string;
  eventSummary: string | null;
  status: ProposalStatus;
  currencyCode: string;
  validUntil: string;
  sharedIntroduction: string | null;
  sharedTerms: string | null;
  totals: ProposalTotals;
  lines: ProposalPublicLine[];
  comments: ProposalPublicComment[];
}
