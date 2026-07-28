using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContractsSignaturesAndPayments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "contract_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    content_format = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_templates", x => x.id);
                    table.UniqueConstraint("ak_contract_templates_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_contract_templates_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_templates_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_proposal_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted_proposal_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contract_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_version_number = table.Column<int>(type: "integer", nullable: false),
                    contract_grand_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    cancellation_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracts", x => x.id);
                    table.UniqueConstraint("ak_contracts_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_contracts_source", "(source_type = 'GeneratedFromProposal' AND accepted_proposal_id IS NOT NULL AND accepted_proposal_version_id IS NOT NULL) OR (source_type <> 'GeneratedFromProposal' AND accepted_proposal_id IS NULL AND accepted_proposal_version_id IS NULL)");
                    table.CheckConstraint("ck_contracts_total", "contract_grand_total >= 0");
                    table.ForeignKey(
                        name: "fk_contracts_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_proposal_versions_organization_id_accepted_propos",
                        columns: x => new { x.organization_id, x.accepted_proposal_version_id },
                        principalTable: "proposal_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_proposals_organization_id_accepted_proposal_id",
                        columns: x => new { x.organization_id, x.accepted_proposal_id },
                        principalTable: "proposals",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contracts_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_contracting_policies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_accepted_proposal = table.Column<bool>(type: "boolean", nullable: false),
                    require_completed_contract = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_requirement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    deposit_requirement_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    confirmation_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_contracting_policies", x => x.id);
                    table.UniqueConstraint("ak_organization_contracting_policies_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_contracting_policy_deposit", "deposit_requirement_value >= 0 AND (deposit_requirement_type <> 'PercentageOfContract' OR deposit_requirement_value <= 100) AND (deposit_requirement_type <> 'None' OR deposit_requirement_value = 0)");
                    table.ForeignKey(
                        name: "fk_organization_contracting_policies_organizations_organizatio",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_parties",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    party_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_party_id = table.Column<Guid>(type: "uuid", nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    legal_name = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    tax_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_parties", x => x.id);
                    table.UniqueConstraint("ak_contract_parties_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_contract_parties_type", "(party_type = 'Client' AND client_id IS NOT NULL) OR (party_type = 'PlannerOrganization' AND organization_party_id IS NOT NULL) OR party_type = 'Other'");
                    table.ForeignKey(
                        name: "fk_contract_parties_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_parties_contracts_organization_id_contract_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_parties_organizations_organization_party_id",
                        column: x => x.organization_party_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_proposal_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rendered_content = table.Column<string>(type: "text", nullable: false),
                    document_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    document_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    document_mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    document_size_bytes = table.Column<long>(type: "bigint", nullable: true),
                    document_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    consent_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    superseded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_versions", x => x.id);
                    table.UniqueConstraint("ak_contract_versions_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_contract_versions_document", "(published_at IS NULL AND document_storage_key IS NULL AND document_sha256 IS NULL) OR (published_at IS NOT NULL AND document_storage_key IS NOT NULL AND document_size_bytes > 0 AND length(document_sha256) = 64)");
                    table.CheckConstraint("ck_contract_versions_validity", "valid_until IS NULL OR valid_until > created_at");
                    table.ForeignKey(
                        name: "fk_contract_versions_contract_templates_organization_id_templa",
                        columns: x => new { x.organization_id, x.template_id },
                        principalTable: "contract_templates",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_versions_contracts_organization_id_contract_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_versions_proposal_versions_organization_id_source_",
                        columns: x => new { x.organization_id, x.source_proposal_version_id },
                        principalTable: "proposal_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_versions_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contracting_requirement_snapshots",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    require_accepted_proposal = table.Column<bool>(type: "boolean", nullable: false),
                    require_completed_contract = table.Column<bool>(type: "boolean", nullable: false),
                    deposit_requirement_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    deposit_requirement_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    required_deposit_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    confirmation_mode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contracting_requirement_snapshots", x => x.id);
                    table.CheckConstraint("ck_requirement_snapshot_amounts", "deposit_requirement_value >= 0 AND required_deposit_amount >= 0");
                    table.ForeignKey(
                        name: "fk_contracting_requirement_snapshots_contracts_organization_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proposal_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    activated_total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_plans", x => x.id);
                    table.UniqueConstraint("ak_payment_plans_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_payment_plans_amount", "total_amount >= 0 AND (activated_total_amount IS NULL OR activated_total_amount >= 0)");
                    table.ForeignKey(
                        name: "fk_payment_plans_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_plans_contracts_organization_id_contract_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_plans_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_plans_proposal_versions_organization_id_proposal_ve",
                        columns: x => new { x.organization_id, x.proposal_version_id },
                        principalTable: "proposal_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_plans_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_signers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_party_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    signer_role = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    signing_order = table.Column<int>(type: "integer", nullable: false),
                    is_required = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    declined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_signers", x => x.id);
                    table.UniqueConstraint("ak_contract_signers_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_contract_signers_contract_parties_organization_id_contract_",
                        columns: x => new { x.organization_id, x.contract_party_id },
                        principalTable: "contract_parties",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_signers_contracts_organization_id_contract_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_signers_people_organization_id_person_id",
                        columns: x => new { x.organization_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_signers_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "contract_final_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_contract_final_documents", x => x.id);
                    table.CheckConstraint("ck_contract_final_document", "size_bytes > 0 AND length(sha256) = 64");
                    table.ForeignKey(
                        name: "fk_contract_final_documents_contract_versions_organization_id_",
                        columns: x => new { x.organization_id, x.contract_version_id },
                        principalTable: "contract_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_final_documents_contracts_organization_id_contract",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_installments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence_number = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    due_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    installment_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_installments", x => x.id);
                    table.UniqueConstraint("ak_payment_installments_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_payment_installments_values", "sequence_number > 0 AND amount >= 0");
                    table.ForeignKey(
                        name: "fk_payment_installments_payment_plans_organization_id_payment_",
                        columns: x => new { x.organization_id, x.payment_plan_id },
                        principalTable: "payment_plans",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                    payment_date = table.Column<DateOnly>(type: "date", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    notes_shared = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    recorded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    submitted_by_client = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejected_by = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_records", x => x.id);
                    table.UniqueConstraint("ak_payment_records_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_payment_records_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_records_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_records_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_records_payment_plans_organization_id_payment_plan_",
                        columns: x => new { x.organization_id, x.payment_plan_id },
                        principalTable: "payment_plans",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_records_user_accounts_approved_by",
                        column: x => x.approved_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_records_user_accounts_recorded_by",
                        column: x => x.recorded_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_records_user_accounts_rejected_by",
                        column: x => x.rejected_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "signature_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_signer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signature_requests", x => x.id);
                    table.UniqueConstraint("ak_signature_requests_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_signature_requests_expiry", "expires_at > created_at");
                    table.ForeignKey(
                        name: "fk_signature_requests_contract_signers_organization_id_contrac",
                        columns: x => new { x.organization_id, x.contract_signer_id },
                        principalTable: "contract_signers",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_requests_contract_versions_organization_id_contra",
                        columns: x => new { x.organization_id, x.contract_version_id },
                        principalTable: "contract_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_requests_contracts_organization_id_contract_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_requests_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_allocations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_installment_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reversed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_allocations", x => x.id);
                    table.CheckConstraint("ck_payment_allocations_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_allocations_payment_installments_organization_id_pa",
                        columns: x => new { x.organization_id, x.payment_installment_id },
                        principalTable: "payment_installments",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_allocations_payment_records_organization_id_payment",
                        columns: x => new { x.organization_id, x.payment_record_id },
                        principalTable: "payment_records",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payment_receipts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    payment_record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_receipts_basic_documents_organization_id_document_id",
                        columns: x => new { x.organization_id, x.document_id },
                        principalTable: "basic_documents",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_receipts_payment_records_organization_id_payment_re",
                        columns: x => new { x.organization_id, x.payment_record_id },
                        principalTable: "payment_records",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "signature_evidence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_signer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signature_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signing_method = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    declared_signer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    declared_signer_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    signature_image_storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    document_sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    consent_text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    signed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signature_evidence", x => x.id);
                    table.UniqueConstraint("ak_signature_evidence_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_signature_evidence_sha256", "length(document_sha256) = 64");
                    table.ForeignKey(
                        name: "fk_signature_evidence_contract_signers_organization_id_contrac",
                        columns: x => new { x.organization_id, x.contract_signer_id },
                        principalTable: "contract_signers",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_evidence_contract_versions_organization_id_contra",
                        columns: x => new { x.organization_id, x.contract_version_id },
                        principalTable: "contract_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_evidence_contracts_organization_id_contract_id",
                        columns: x => new { x.organization_id, x.contract_id },
                        principalTable: "contracts",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_evidence_signature_requests_organization_id_signa",
                        columns: x => new { x.organization_id, x.signature_request_id },
                        principalTable: "signature_requests",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_signature_evidence_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_contract_final_documents_organization_id_contract_id",
                table: "contract_final_documents",
                columns: new[] { "organization_id", "contract_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_final_documents_organization_id_contract_version_id",
                table: "contract_final_documents",
                columns: new[] { "organization_id", "contract_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_final_documents_storage_key",
                table: "contract_final_documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_parties_organization_id_client_id",
                table: "contract_parties",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_parties_organization_id_contract_id_sort_order",
                table: "contract_parties",
                columns: new[] { "organization_id", "contract_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_parties_organization_party_id",
                table: "contract_parties",
                column: "organization_party_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_signers_organization_id_contract_id_signing_order",
                table: "contract_signers",
                columns: new[] { "organization_id", "contract_id", "signing_order" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_signers_organization_id_contract_party_id",
                table: "contract_signers",
                columns: new[] { "organization_id", "contract_party_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_signers_organization_id_person_id",
                table: "contract_signers",
                columns: new[] { "organization_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_signers_user_account_id",
                table: "contract_signers",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_templates_created_by",
                table: "contract_templates",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_contract_templates_organization_id_is_active_is_default",
                table: "contract_templates",
                columns: new[] { "organization_id", "is_active", "is_default" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_versions_created_by",
                table: "contract_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_contract_versions_document_storage_key",
                table: "contract_versions",
                column: "document_storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_versions_organization_id_contract_id_version_number",
                table: "contract_versions",
                columns: new[] { "organization_id", "contract_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contract_versions_organization_id_source_proposal_version_id",
                table: "contract_versions",
                columns: new[] { "organization_id", "source_proposal_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_versions_organization_id_template_id",
                table: "contract_versions",
                columns: new[] { "organization_id", "template_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contracting_requirement_snapshots_organization_id_contract_",
                table: "contracting_requirement_snapshots",
                columns: new[] { "organization_id", "contract_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_created_by",
                table: "contracts",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_contracts_organization_id_accepted_proposal_id",
                table: "contracts",
                columns: new[] { "organization_id", "accepted_proposal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_organization_id_accepted_proposal_version_id",
                table: "contracts",
                columns: new[] { "organization_id", "accepted_proposal_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_organization_id_client_id",
                table: "contracts",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_contracts_organization_id_contract_number",
                table: "contracts",
                columns: new[] { "organization_id", "contract_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_contracts_organization_id_event_id_status",
                table: "contracts",
                columns: new[] { "organization_id", "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_contracting_policies_organization_id",
                table: "organization_contracting_policies",
                column: "organization_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_organization_id_payment_installment_id",
                table: "payment_allocations",
                columns: new[] { "organization_id", "payment_installment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_allocations_organization_id_payment_record_id_payme",
                table: "payment_allocations",
                columns: new[] { "organization_id", "payment_record_id", "payment_installment_id", "reversed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_installments_organization_id_payment_plan_id_sequen",
                table: "payment_installments",
                columns: new[] { "organization_id", "payment_plan_id", "sequence_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_plans_created_by",
                table: "payment_plans",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_plans_organization_id_client_id",
                table: "payment_plans",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_plans_organization_id_contract_id",
                table: "payment_plans",
                columns: new[] { "organization_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_plans_organization_id_event_id_status",
                table: "payment_plans",
                columns: new[] { "organization_id", "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_plans_organization_id_proposal_version_id",
                table: "payment_plans",
                columns: new[] { "organization_id", "proposal_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_receipts_organization_id_document_id",
                table: "payment_receipts",
                columns: new[] { "organization_id", "document_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_receipts_organization_id_payment_record_id_document",
                table: "payment_receipts",
                columns: new[] { "organization_id", "payment_record_id", "document_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_approved_by",
                table: "payment_records",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_organization_id_client_id",
                table: "payment_records",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_organization_id_event_id_status",
                table: "payment_records",
                columns: new[] { "organization_id", "event_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_organization_id_payment_plan_id",
                table: "payment_records",
                columns: new[] { "organization_id", "payment_plan_id" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_recorded_by",
                table: "payment_records",
                column: "recorded_by");

            migrationBuilder.CreateIndex(
                name: "ix_payment_records_rejected_by",
                table: "payment_records",
                column: "rejected_by");

            migrationBuilder.CreateIndex(
                name: "ix_signature_evidence_organization_id_contract_id",
                table: "signature_evidence",
                columns: new[] { "organization_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "ix_signature_evidence_organization_id_contract_signer_id",
                table: "signature_evidence",
                columns: new[] { "organization_id", "contract_signer_id" });

            migrationBuilder.CreateIndex(
                name: "ix_signature_evidence_organization_id_contract_version_id_cont",
                table: "signature_evidence",
                columns: new[] { "organization_id", "contract_version_id", "contract_signer_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_signature_evidence_organization_id_signature_request_id",
                table: "signature_evidence",
                columns: new[] { "organization_id", "signature_request_id" });

            migrationBuilder.CreateIndex(
                name: "ix_signature_evidence_user_account_id",
                table: "signature_evidence",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_signature_requests_created_by",
                table: "signature_requests",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_signature_requests_organization_id_contract_id",
                table: "signature_requests",
                columns: new[] { "organization_id", "contract_id" });

            migrationBuilder.CreateIndex(
                name: "ix_signature_requests_organization_id_contract_signer_id_revok",
                table: "signature_requests",
                columns: new[] { "organization_id", "contract_signer_id", "revoked_at", "signed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_signature_requests_organization_id_contract_version_id",
                table: "signature_requests",
                columns: new[] { "organization_id", "contract_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_signature_requests_token_hash",
                table: "signature_requests",
                column: "token_hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contract_final_documents");

            migrationBuilder.DropTable(
                name: "contracting_requirement_snapshots");

            migrationBuilder.DropTable(
                name: "organization_contracting_policies");

            migrationBuilder.DropTable(
                name: "payment_allocations");

            migrationBuilder.DropTable(
                name: "payment_receipts");

            migrationBuilder.DropTable(
                name: "signature_evidence");

            migrationBuilder.DropTable(
                name: "payment_installments");

            migrationBuilder.DropTable(
                name: "payment_records");

            migrationBuilder.DropTable(
                name: "signature_requests");

            migrationBuilder.DropTable(
                name: "payment_plans");

            migrationBuilder.DropTable(
                name: "contract_signers");

            migrationBuilder.DropTable(
                name: "contract_versions");

            migrationBuilder.DropTable(
                name: "contract_parties");

            migrationBuilder.DropTable(
                name: "contract_templates");

            migrationBuilder.DropTable(
                name: "contracts");
        }
    }
}
