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

export type ContractStatus =
  | 'Draft'
  | 'Ready'
  | 'Sent'
  | 'Viewed'
  | 'PartiallySigned'
  | 'FullySigned'
  | 'Completed'
  | 'Declined'
  | 'Expired'
  | 'Cancelled';
export type ContractSourceType = 'GeneratedFromProposal' | 'Manual' | 'ExternalUpload';
export type ContractPartyType = 'PlannerOrganization' | 'Client' | 'Other';
export type ContractSignerStatus =
  'Pending' | 'Invited' | 'Viewed' | 'Signed' | 'Declined' | 'Expired' | 'Revoked';
export type SigningMethod = 'Drawn' | 'Typed' | 'AuthenticatedConfirmation' | 'External';
export type DepositRequirementType = 'None' | 'FixedAmount' | 'PercentageOfContract';
export type ConfirmationMode = 'Automatic' | 'ManualAfterRequirements';

export interface ContractTemplate {
  id: string;
  name: string;
  description: string | null;
  content: string;
  contentFormat: 'SanitizedHtml';
  isDefault: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  archivedAt: string | null;
}

export interface ContractParty {
  id: string;
  partyType: ContractPartyType;
  clientId: string | null;
  organizationPartyId: string | null;
  displayName: string;
  legalName: string | null;
  taxId: string | null;
  address: string | null;
  sortOrder: number;
}

export interface ContractSigner {
  id: string;
  contractPartyId: string;
  personId: string | null;
  userAccountId: string | null;
  name: string;
  email: string;
  signerRole: string;
  signingOrder: number;
  isRequired: boolean;
  status: ContractSignerStatus;
  signedAt: string | null;
  declinedAt: string | null;
  activeSignatureRequestId: string | null;
}

export interface ContractVersion {
  id: string;
  versionNumber: number;
  templateId: string | null;
  sourceProposalVersionId: string | null;
  renderedContent: string;
  documentFileName: string | null;
  documentSizeBytes: number | null;
  documentSha256: string | null;
  consentText: string;
  validUntil: string | null;
  createdAt: string;
  publishedAt: string | null;
  supersededAt: string | null;
}

export interface ContractRequirements {
  requireAcceptedProposal: boolean;
  requireCompletedContract: boolean;
  depositRequirementType: DepositRequirementType;
  depositRequirementValue: number;
  requiredDepositAmount: number;
  currencyCode: string;
  confirmationMode: ConfirmationMode;
  createdAt: string;
}

export interface ContractListItem {
  id: string;
  eventId: string;
  clientId: string;
  contractNumber: string;
  name: string;
  sourceType: ContractSourceType;
  status: ContractStatus;
  currentVersionNumber: number;
  contractGrandTotal: number;
  currencyCode: string;
  updatedAt: string;
}

export interface ContractResponse extends ContractListItem {
  organizationId: string;
  acceptedProposalId: string | null;
  acceptedProposalVersionId: string | null;
  versions: ContractVersion[];
  parties: ContractParty[];
  signers: ContractSigner[];
  requirements: ContractRequirements;
  createdAt: string;
  completedAt: string | null;
  cancelledAt: string | null;
  cancellationReason: string | null;
}

export interface SignatureRequestLink {
  id: string;
  contractVersionId: string;
  contractSignerId: string;
  expiresAt: string;
  signingUrl: string;
}

export interface SignatureEvidenceSummary {
  id: string;
  contractVersionId: string;
  contractSignerId: string;
  signingMethod: SigningMethod;
  declaredSignerName: string;
  declaredSignerEmail: string;
  documentSha256: string;
  signedAt: string;
}

export interface PublicSignatureContract {
  contractId: string;
  contractVersionId: string;
  contractSignerId: string;
  contractNumber: string;
  name: string;
  versionNumber: number;
  organizationName: string;
  signerName: string;
  signerEmail: string;
  parties: string[];
  renderedContent: string;
  validUntil: string | null;
  consentText: string;
  documentSha256: string;
  signers: { signerRole: string; status: ContractSignerStatus; signedAt: string | null }[];
  canSign: boolean;
}

export interface ContractingReadiness {
  proposalAccepted: boolean;
  contractCompleted: boolean;
  requiredDepositAmount: number;
  approvedDepositAmount: number;
  depositSatisfied: boolean;
  missingRequiredSigners: number;
  missingRequirements: string[];
  readyForConfirmation: boolean;
  confirmationMode: ConfirmationMode;
  eventStatus: EventStatus;
}

export type PaymentPlanStatus = 'Draft' | 'Active' | 'Completed' | 'Cancelled';
export type InstallmentType = 'Deposit' | 'ScheduledPayment' | 'FinalPayment' | 'AdditionalCharge';
export type PaymentInstallmentStatus =
  'Pending' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled';
export type PaymentMethod =
  'Cash' | 'BankTransfer' | 'Deposit' | 'CardExternal' | 'Check' | 'Other';
export type PaymentRecordStatus =
  'PendingReview' | 'Approved' | 'Rejected' | 'Cancelled' | 'Refunded';

export interface PaymentInstallment {
  id: string;
  sequenceNumber: number;
  description: string;
  dueDate: string;
  amount: number;
  approvedAmount: number;
  pendingAmount: number;
  installmentType: InstallmentType;
  status: PaymentInstallmentStatus;
}

export interface PaymentPlan {
  id: string;
  eventId: string;
  clientId: string;
  contractId: string | null;
  proposalVersionId: string | null;
  currencyCode: string;
  totalAmount: number;
  status: PaymentPlanStatus;
  approvedAmount: number;
  pendingAmount: number;
  installments: PaymentInstallment[];
  createdAt: string;
  updatedAt: string;
}

export interface PaymentRecord {
  id: string;
  eventId: string;
  clientId: string;
  paymentPlanId: string | null;
  paymentDate: string;
  amount: number;
  currencyCode: string;
  method: PaymentMethod;
  reference: string | null;
  status: PaymentRecordStatus;
  notesShared: string | null;
  internalNotes: string | null;
  submittedByClient: boolean;
  rejectionReason: string | null;
  allocations: { id: string; paymentInstallmentId: string; amount: number }[];
  receipts: {
    documentId: string;
    fileName: string;
    mimeType: string;
    sizeBytes: number;
    createdAt: string;
  }[];
  createdAt: string;
  updatedAt: string;
}

export interface PortalContractListItem {
  id: string;
  eventId: string;
  contractNumber: string;
  name: string;
  status: ContractStatus;
  currentVersionNumber: number;
  hasPendingSignature: boolean;
  hasFinalDocument: boolean;
}

export interface PortalContract {
  id: string;
  eventId: string;
  contractNumber: string;
  name: string;
  status: ContractStatus;
  version: ContractVersion;
  parties: ContractParty[];
  signers: { signerRole: string; status: ContractSignerStatus; signedAt: string | null }[];
  pendingSignerId: string | null;
  pendingSignerName: string | null;
  hasFinalDocument: boolean;
}

export interface PortalPaymentRecord {
  id: string;
  paymentDate: string;
  amount: number;
  currencyCode: string;
  method: PaymentMethod;
  reference: string | null;
  status: PaymentRecordStatus;
  notesShared: string | null;
  rejectionReason: string | null;
  receipts: {
    documentId: string;
    fileName: string;
    mimeType: string;
    sizeBytes: number;
    createdAt: string;
  }[];
  createdAt: string;
}

export type GuestType =
  | 'Standard'
  | 'Family'
  | 'Friend'
  | 'Colleague'
  | 'Vendor'
  | 'WeddingParty'
  | 'SponsorOrGodparent'
  | 'StaffGuest'
  | 'VendorGuest'
  | 'Vip'
  | 'Other';
export type AgeCategory = 'Adult' | 'Teen' | 'Child' | 'Infant' | 'Unknown';
export type InvitationGroupType =
  'Individual' | 'Couple' | 'Family' | 'Group' | 'Company' | 'CorporateTable' | 'Other';
export type InvitationGroupStatus =
  'Draft' | 'Ready' | 'LinkGenerated' | 'SharedManually' | 'Opened' | 'Revoked' | 'Archived';
export type GuestPlanTier = 'Community' | 'EventComplete' | 'PlannerPro';

export interface GuestTag {
  id: string;
  name: string;
  colorToken: string;
}

export interface InvitationGroup {
  id: string;
  groupType: InvitationGroupType;
  displayName: string;
  contactName: string | null;
  contactPhone: string | null;
  contactEmail: string | null;
  allowedGuestCount: number;
  namedGuestCount: number;
  availableGuestCount: number;
  allowUnnamedCompanions: boolean;
  maxUnnamedCompanions: number;
  status: InvitationGroupStatus;
  source: string;
  internalNotes: string | null;
  capacityOverrideApplied: boolean;
  tags: GuestTag[];
  updatedAt: string;
  archivedAt: string | null;
}

export interface EventGuest {
  id: string;
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
  isActive: boolean;
  sortOrder: number;
  internalNotes: string | null;
  updatedAt: string;
  archivedAt: string | null;
}

export interface GuestPlanUsage {
  tier: GuestPlanTier;
  activeGuests: number;
  limit: number;
  percentage: number;
  warning80: boolean;
  warning90: boolean;
  isAtLimit: boolean;
}

export interface GuestDashboard {
  activeGuestCount: number;
  groupCount: number;
  linkCount: number;
  openedLinkCount: number;
  plan: GuestPlanUsage;
  groups: InvitationGroup[];
  guests: EventGuest[];
}

export interface GuestDuplicateSuggestion {
  kind: string;
  reason: string;
  guestIds: string[];
  suggestedAction: string;
}

export interface GuestImportRowPreview {
  rowNumber: number;
  groupName: string | null;
  guestName: string | null;
  isValid: boolean;
  errors: string[];
}

export type GuestImportTemplateFormat = 'csv' | 'xlsx';
export type GuestImportTemplateLanguage = 'es' | 'en';

export interface GuestImportAnalysis {
  importId: string;
  status: 'Analyzed' | 'Completed' | 'Failed';
  headers: string[];
  mapping: Record<string, string>;
  totalRows: number;
  validRows: number;
  errorRows: number;
  preview: GuestImportRowPreview[];
}

export interface GuestImportResult {
  importId: string;
  status: 'Completed';
  createdGroups: number;
  createdGuests: number;
  reusedGroups: number;
  skippedRows: number;
  errors: GuestImportRowPreview[];
  completedAt: string;
}

export type GuestExperienceStatus = 'Draft' | 'Ready' | 'Published' | 'Suspended' | 'Archived';
export type InvitationDesignStatus =
  'Draft' | 'InReview' | 'ChangesRequested' | 'Approved' | 'Published' | 'Archived';
export type InvitationBlockType =
  | 'Cover'
  | 'Greeting'
  | 'Participants'
  | 'EventDate'
  | 'Countdown'
  | 'Story'
  | 'Image'
  | 'GalleryPreview'
  | 'Text'
  | 'Divider'
  | 'DressCode'
  | 'Contact'
  | 'CustomButton'
  | 'Footer';
export type BlockVisibility = 'Everyone' | 'InvitationGroup' | 'HasTag' | 'GuestType' | 'VipOnly';
export type InvitationAnimationLevel = 'None' | 'Reduced' | 'Standard';
export type InvitationBlockValue = string | number | boolean | null;

export interface InvitationTheme {
  backgroundColor: string;
  surfaceColor: string;
  textColor: string;
  accentColor: string;
  headingFont: string;
  bodyFont: string;
  radiusToken: string;
  spacingToken: string;
  coverStyle: string;
  buttonStyle: string;
  animation: InvitationAnimationLevel;
}

export interface InvitationBlock {
  id: string;
  type: InvitationBlockType;
  visible: boolean;
  visibility: BlockVisibility;
  visibilityValue: string | null;
  sortOrder: number;
  content: Record<string, InvitationBlockValue>;
  presentation: Record<string, InvitationBlockValue>;
}

export interface InvitationVersion {
  id: string;
  versionNumber: number;
  theme: InvitationTheme;
  blocks: InvitationBlock[];
  createdAt: string;
  approvedAt: string | null;
  publishedAt: string | null;
}

export interface InvitationComment {
  id: string;
  versionId: string;
  decision: 'Comment' | 'Approved' | 'ChangesRequested';
  message: string;
  createdAt: string;
}

export interface InvitationDesign {
  id: string;
  eventId: string;
  name: string;
  status: InvitationDesignStatus;
  theme: InvitationTheme;
  blocks: InvitationBlock[];
  nextVersionNumber: number;
  approvedVersionId: string | null;
  versions: InvitationVersion[];
  comments: InvitationComment[];
  accessibilityWarnings: string[];
  updatedAt: string;
}

export interface InvitationTemplate {
  id: string;
  isGlobal: boolean;
  name: string;
  description: string;
  theme: InvitationTheme;
  blocks: InvitationBlock[];
}

export interface GuestExperience {
  id: string;
  eventId: string;
  status: GuestExperienceStatus;
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
  activeInvitationDesignId: string | null;
  activeVersionId: string | null;
  updatedAt: string;
}

export type GuestAccessLinkStatus = 'Active' | 'Revoked' | 'Expired' | 'Replaced';

export interface GuestAccessLink {
  id: string;
  invitationGroupId: string;
  status: GuestAccessLinkStatus;
  publicUrl: string | null;
  expiresAt: string | null;
  firstOpenedAt: string | null;
  lastOpenedAt: string | null;
  openCount: number;
  sharedManuallyAt: string | null;
  createdAt: string;
}

export interface PublicGuest {
  firstName: string;
  lastName: string;
  guestType: GuestType;
  ageCategory: AgeCategory;
  isPrimaryContact: boolean;
  isVip: boolean;
}

export interface PublicInvitation {
  status: 'available';
  language: string;
  publicTitle: string;
  celebrantDisplayName: string;
  welcomeMessage: string | null;
  closingMessage: string | null;
  eventName: string | null;
  eventStartsAt: string | null;
  eventTimeZone: string;
  city: string | null;
  countryCode: string | null;
  groupDisplayName: string;
  allowedGuestCount: number;
  participants: PublicGuest[];
  theme: InvitationTheme;
  blocks: InvitationBlock[];
}

export interface PortalInvitationGroup {
  id: string;
  groupType: InvitationGroupType;
  displayName: string;
  allowedGuestCount: number;
  namedGuestCount: number;
  allowUnnamedCompanions: boolean;
  maxUnnamedCompanions: number;
}

export interface PortalGuest {
  id: string;
  invitationGroupId: string | null;
  firstName: string;
  lastName: string;
  guestType: GuestType;
  ageCategory: AgeCategory;
  isPrimaryContact: boolean;
  isVip: boolean;
}

export interface PortalGuestWorkspace {
  eventId: string;
  groups: PortalInvitationGroup[];
  guests: PortalGuest[];
  design: InvitationDesign | null;
}

// === RSVP Types ===

export type RsvpSettingsStatus = 'Draft' | 'Ready' | 'Open' | 'Closed' | 'Suspended' | 'Archived';
export type RsvpFormStatus = 'Draft' | 'InReview' | 'ChangesRequested' | 'Approved' | 'Published' | 'Archived';
export type GuestAttendanceStatus = 'Pending' | 'Attending' | 'NotAttending' | 'Tentative' | 'CancelledAfterConfirmation';
export type RsvpSubmissionSource = 'GuestPrivateLink' | 'PlannerManual' | 'ClientPortal' | 'Imported' | 'SupportCorrection';
export type RsvpOverallStatus = 'Confirmed' | 'Declined' | 'Mixed' | 'Tentative' | 'Incomplete';
export type RsvpQuestionType = 'ShortText' | 'LongText' | 'YesNo' | 'SingleChoice' | 'MultipleChoice' | 'Number' | 'Date' | 'InformationalConsent';
export type RsvpQuestionScope = 'InvitationGroup' | 'IndividualGuest' | 'PrimaryContact';
export type RsvpQuestionCategory = 'General' | 'Dietary' | 'Transportation' | 'Accommodation' | 'Accessibility' | 'Consent' | 'Other';
export type RsvpVisibilityConditionType =
  | 'Always'
  | 'AttendanceStatusEquals'
  | 'GuestAgeCategoryEquals'
  | 'GuestTypeEquals'
  | 'GroupHasTag'
  | 'PreviousAnswerEquals'
  | 'PreviousAnswerContains'
  | 'IsUnnamedCompanion'
  | 'IsPrimaryContact'
  | 'All'
  | 'Any';
export type MenuCategory = 'AdultMeal' | 'ChildMeal' | 'TeenMeal' | 'Beverage' | 'Dessert' | 'LateSnack' | 'Other';
export type TransportDirection = 'ToCeremony' | 'ToReception' | 'Return' | 'RoundTrip' | 'Other';
export type TransportSelectionStatus = 'Requested' | 'Confirmed' | 'Waitlisted' | 'NotNeeded' | 'Cancelled';
export type AccommodationSelectionStatus = 'NotNeeded' | 'Interested' | 'PlanningToBook' | 'Booked' | 'NeedAssistance';
export type ReminderChannel = 'WhatsAppManual' | 'EmailCopy' | 'GeneralCopy';

export interface RsvpSettingsResponse {
  id: string;
  status: RsvpSettingsStatus;
  opensAt: string | null;
  closesAt: string | null;
  timeZone: string;
  allowChangesAfterSubmission: boolean;
  changesCloseAt: string | null;
  allowTentativeResponse: boolean;
  allowGroupDecline: boolean;
  requireResponseForEveryNamedGuest: boolean;
  requireCompanionNames: boolean;
  allowContactInformationUpdate: boolean;
  showAttendanceSummaryAfterSubmission: boolean;
  confirmationTitle: string | null;
  confirmationMessage: string | null;
  declineMessage: string | null;
  closedMessage: string | null;
  privacyNotice: string | null;
  sensitiveDataConsentText: string | null;
  updatedAt: string;
}

export interface RsvpSettingsRequest {
  opensAt: string | null;
  closesAt: string | null;
  timeZone: string;
  allowChangesAfterSubmission: boolean;
  changesCloseAt: string | null;
  allowTentativeResponse: boolean;
  allowGroupDecline: boolean;
  requireResponseForEveryNamedGuest: boolean;
  requireCompanionNames: boolean;
  allowContactInformationUpdate: boolean;
  showAttendanceSummaryAfterSubmission: boolean;
  confirmationTitle: string | null;
  confirmationMessage: string | null;
  declineMessage: string | null;
  closedMessage: string | null;
  privacyNotice: string | null;
  sensitiveDataConsentText: string | null;
}

export interface RsvpFormResponse {
  id: string;
  status: RsvpFormStatus;
  currentDraftVersion: number;
  activePublishedVersionId: string | null;
  updatedAt: string;
}

export interface RsvpFormVersionResponse {
  id: string;
  rsvpFormId: string;
  versionNumber: number;
  settingsSnapshot: string;
  questionsSnapshot: string;
  menuSnapshot: string;
  transportSnapshot: string;
  accommodationSnapshot: string;
  createdAt: string;
  approvedBy: string | null;
  approvedAt: string | null;
  publishedAt: string | null;
}

export interface RsvpQuestion {
  id: string;
  questionType: RsvpQuestionType;
  scope: RsvpQuestionScope;
  category: RsvpQuestionCategory;
  label: string;
  helpText: string | null;
  isRequired: boolean;
  isSensitive: boolean;
  isActive: boolean;
  sortOrder: number;
  options: RsvpQuestionOption[];
  visibilityRule: RsvpVisibilityRule;
  validationRules: RsvpValidationRules;
}

export interface RsvpQuestionOption {
  key: string;
  label: string;
  isActive: boolean;
  sortOrder: number;
}

export interface RsvpVisibilityRule {
  conditionType: RsvpVisibilityConditionType;
  referenceQuestionId: string | null;
  expectedValue: string | null;
  conditions: RsvpVisibilityRule[];
}

export interface RsvpValidationRules {
  required?: boolean | null;
  minLength?: number | null;
  maxLength?: number | null;
  minimumSelections?: number | null;
  maximumSelections?: number | null;
  minimum?: number | null;
  maximum?: number | null;
  integerOnly?: boolean | null;
  minimumDate?: string | null;
  maximumDate?: string | null;
}

export interface RsvpQuestionCatalog {
  questionTypes: RsvpQuestionType[];
  questionScopes: RsvpQuestionScope[];
  questionCategories: RsvpQuestionCategory[];
  visibilityConditionTypes: RsvpVisibilityConditionType[];
  compatibleRules: Record<RsvpQuestionType, string[]>;
  maximumQuestions: number;
  maximumQuestionLabelLength: number;
  maximumHelpTextLength: number;
  maximumOptionLabelLength: number;
  maximumShortTextLength: number;
  maximumLongTextLength: number;
  maximumVisibilityDepth: number;
  maximumVisibilityConditions: number;
}

export interface RsvpSubmissionRequest {
  rsvpFormVersionId: string;
  expectedRevision: number;
  overallStatus: RsvpOverallStatus;
  contactName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  guests: RsvpSubmissionGuestRequest[];
  answers: RsvpSubmissionAnswerRequest[];
  consentSnapshot: string | null;
}

export interface RsvpSubmissionGuestRequest {
  responseGuestId: string;
  eventGuestId: string | null;
  displayName: string;
  ageCategory: string;
  attendanceStatus: GuestAttendanceStatus;
  menuSelectionsJson: string;
  transportSelectionJson: string;
  accommodationSelectionJson: string;
  dietaryJson: string;
  isUnnamedCompanion: boolean;
}

export interface RsvpSubmissionAnswerRequest {
  questionId: string;
  guestId: string | null;
  answerValue: string;
  displayValue: string | null;
}

export interface RsvpSubmissionResponse {
  id: string;
  invitationGroupId: string;
  revisionNumber: number;
  source: RsvpSubmissionSource;
  overallStatus: RsvpOverallStatus;
  submittedAt: string;
  contactNameSnapshot: string | null;
  contactEmailSnapshot: string | null;
  contactPhoneSnapshot: string | null;
  confirmationCode: string | null;
  guests: RsvpSubmissionGuestResponse[];
  answers: RsvpSubmissionAnswerResponse[];
}

export interface RsvpSubmissionGuestResponse {
  responseGuestId: string;
  eventGuestId: string | null;
  displayName: string;
  ageCategory: string;
  attendanceStatus: GuestAttendanceStatus;
  menuSelectionsJson: string;
  transportSelectionJson: string;
  accommodationSelectionJson: string;
  dietaryJson: string;
  isUnnamedCompanion: boolean;
}

export interface RsvpSubmissionAnswerResponse {
  questionId: string;
  guestId: string | null;
  answerValue: string;
  displayValue: string | null;
  questionLabelSnapshot: string;
  questionTypeSnapshot: RsvpQuestionType;
  optionLabelsSnapshot: string;
}

export interface GuestRsvpStateResponse {
  groupId: string;
  groupName: string;
  allowedGuestCount: number;
  maxUnnamedCompanions: number;
  allowUnnamedCompanions: boolean;
  canRespond: boolean;
  canModify: boolean;
  closedMessage: string | null;
  settings: RsvpSettingsResponse | null;
  activeForm: RsvpFormVersionResponse | null;
  currentResponse: RsvpSubmissionResponse | null;
  revisionVersion: number;
  guests: GuestRsvpInviteeResponse[];
  groupTags: string[];
}

export interface GuestRsvpInviteeResponse {
  eventGuestId: string;
  displayName: string;
  ageCategory: string;
  guestType: string;
  isPrimaryContact: boolean;
}

export interface SensitiveGuestDataResponse {
  eventGuestId: string;
  displayName: string;
  allergies: string | null;
  dietaryRestrictions: string | null;
  accessibilityRequirements: string | null;
  additionalNotes: string | null;
  consentGrantedAt: string | null;
  updatedAt: string;
}

export interface SensitiveQuestionAnswerResponse {
  submissionId: string;
  revisionNumber: number;
  questionId: string;
  guestId: string | null;
  guestDisplayName: string | null;
  questionLabel: string;
  questionType: RsvpQuestionType;
  answerValue: string;
  displayValue: string | null;
  optionLabelsSnapshot: string;
  submittedAt: string;
}

export interface RsvpDashboardResponse {
  totalGroups: number;
  totalGuestsGranted: number;
  guestsConfirmed: number;
  guestsNotAttending: number;
  guestsTentative: number;
  guestsPending: number;
  partialResponses: number;
  changedAfterSubmission: number;
  closesAt: string | null;
  groups: RsvpGroupSummaryResponse[];
}

export interface RsvpGroupSummaryResponse {
  groupId: string;
  groupName: string;
  status: RsvpOverallStatus | null;
  confirmedCount: number;
  declinedCount: number;
  pendingCount: number;
  hasMenuSelection: boolean;
  hasTransport: boolean;
  hasAccommodation: boolean;
  hasSensitiveData: boolean;
  lastResponseAt: string | null;
}

export interface EventMenuResponse {
  id: string;
  name: string;
  description: string | null;
  menuCategory: MenuCategory;
  isActive: boolean;
  selectionRequired: boolean;
  minimumSelections: number;
  maximumSelections: number;
  sortOrder: number;
  options: EventMenuOptionResponse[];
  updatedAt: string;
}

export interface EventMenuOptionResponse {
  id: string;
  name: string;
  description: string | null;
  dietaryTags: string;
  isActive: boolean;
  capacity: number | null;
  selectionCount: number;
  sortOrder: number;
}

export interface EventMenuRequest {
  name: string;
  description: string | null;
  menuCategory: MenuCategory;
  selectionRequired: boolean;
  minimumSelections: number;
  maximumSelections: number;
  sortOrder: number;
}

export interface EventMenuOptionRequest {
  name: string;
  description: string | null;
  dietaryTags: string;
  capacity: number | null;
  sortOrder: number;
}

export interface EventTransportOptionResponse {
  id: string;
  name: string;
  description: string | null;
  direction: TransportDirection;
  pickupPoint: string | null;
  departureAt: string | null;
  returnAt: string | null;
  capacity: number | null;
  allowWaitlist: boolean;
  isActive: boolean;
  sortOrder: number;
  confirmedCount: number;
  waitlistCount: number;
}

export interface EventTransportOptionRequest {
  name: string;
  description: string | null;
  direction: TransportDirection;
  pickupPoint: string | null;
  departureAt: string | null;
  returnAt: string | null;
  capacity: number | null;
  allowWaitlist: boolean;
  sortOrder: number;
}

export interface EventAccommodationOptionResponse {
  id: string;
  name: string;
  description: string | null;
  address: string | null;
  bookingUrl: string | null;
  bookingCode: string | null;
  bookingDeadline: string | null;
  contactInformation: string | null;
  isActive: boolean;
  sortOrder: number;
  interestedCount: number;
}

export interface EventAccommodationOptionRequest {
  name: string;
  description: string | null;
  address: string | null;
  bookingUrl: string | null;
  bookingCode: string | null;
  bookingDeadline: string | null;
  contactInformation: string | null;
  sortOrder: number;
}

export interface ReminderTemplateResponse {
  id: string;
  name: string;
  channel: ReminderChannel;
  segmentType: string;
  messageTemplate: string;
  isActive: boolean;
  updatedAt: string;
}

export interface ReminderTemplateRequest {
  name: string;
  channel: ReminderChannel;
  segmentType: string;
  messageTemplate: string;
}

export interface MarkReminderRequest {
  note: string | null;
}

export interface ManualRsvpRequest {
  source: RsvpSubmissionSource;
  reason: string;
  submission: RsvpSubmissionRequest;
}

export interface OpenGroupExceptionRequest {
  expiresAt: string;
  reason: string;
}
