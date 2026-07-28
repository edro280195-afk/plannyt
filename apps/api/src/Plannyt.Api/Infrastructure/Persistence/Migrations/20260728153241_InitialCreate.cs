using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "organizations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    organization_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organizations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    password_hash = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    security_version = table.Column<int>(type: "integer", nullable: false),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_accounts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    event_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    status_before_suspension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    start_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    end_date_time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    estimated_guest_count = table.Column<int>(type: "integer", nullable: true),
                    shared_description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_events", x => x.id);
                    table.UniqueConstraint("ak_events_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_events_archived_at", "(status = 'Archived' AND archived_at IS NOT NULL) OR (status <> 'Archived' AND archived_at IS NULL)");
                    table.CheckConstraint("ck_events_dates", "end_date_time IS NULL OR end_date_time >= start_date_time");
                    table.CheckConstraint("ck_events_guest_count", "estimated_guest_count IS NULL OR estimated_guest_count >= 0");
                    table.ForeignKey(
                        name: "fk_events_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_events_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "people",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    linked_user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    preferred_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_people", x => x.id);
                    table.UniqueConstraint("ak_people_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_people_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_people_user_accounts_linked_user_account_id",
                        column: x => x.linked_user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "user_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refresh_token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocation_reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    replaced_by_session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_used_ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    is_persistent = table.Column<bool>(type: "boolean", nullable: false),
                    security_version_at_creation = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_sessions_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_user_sessions_user_sessions_replaced_by_session_id",
                        column: x => x.replaced_by_session_id,
                        principalTable: "user_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "access_invitations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    intended_organization_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    intended_event_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    target_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    normalized_target_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_access_invitations", x => x.id);
                    table.CheckConstraint("ck_access_invitations_completion", "accepted_at IS NULL OR revoked_at IS NULL");
                    table.CheckConstraint("ck_access_invitations_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_access_invitations_type", "(invitation_type = 'OrganizationMembership' AND event_id IS NULL AND intended_organization_role IS NOT NULL AND intended_event_role IS NULL) OR (invitation_type = 'EventAccess' AND event_id IS NOT NULL AND intended_organization_role IS NULL AND intended_event_role IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_access_invitations_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_access_invitations_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_access_invitations_user_accounts_invited_by",
                        column: x => x.invited_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    action = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ip_address = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_audit_entries_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_entries_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_audit_entries_user_accounts_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_accesses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    invited_by = table.Column<Guid>(type: "uuid", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_accesses", x => x.id);
                    table.UniqueConstraint("ak_event_accesses_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_event_accesses_dates", "expires_at IS NULL OR expires_at > starts_at");
                    table.CheckConstraint("ck_event_accesses_revoked_at", "(status = 'Revoked' AND revoked_at IS NOT NULL) OR (status <> 'Revoked' AND revoked_at IS NULL)");
                    table.ForeignKey(
                        name: "fk_event_accesses_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_accesses_user_accounts_invited_by",
                        column: x => x.invited_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_accesses_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_status_history_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_status_history_user_accounts_changed_by",
                        column: x => x.changed_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clients", x => x.id);
                    table.UniqueConstraint("ak_clients_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_clients_type", "(client_type = 'Person' AND person_id IS NOT NULL AND company_name IS NULL) OR (client_type = 'Company' AND person_id IS NULL AND company_name IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_clients_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_clients_people_organization_id_person_id",
                        columns: x => new { x.organization_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_participants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_visible_to_client = table.Column<bool>(type: "boolean", nullable: false),
                    shared_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_participants_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_participants_people_organization_id_person_id",
                        columns: x => new { x.organization_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "organization_memberships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    base_role = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    joined_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_organization_memberships", x => x.id);
                    table.UniqueConstraint("ak_organization_memberships_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_organization_memberships_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_memberships_people_organization_id_person_id",
                        columns: x => new { x.organization_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_organization_memberships_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "basic_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    document_type = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    storage_provider = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    visibility = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    uploaded_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_basic_documents", x => x.id);
                    table.UniqueConstraint("ak_basic_documents_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_basic_documents_size", "size_bytes > 0 AND size_bytes <= 10485760");
                    table.ForeignKey(
                        name: "fk_basic_documents_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_basic_documents_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_basic_documents_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_basic_documents_user_accounts_uploaded_by",
                        column: x => x.uploaded_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_contacts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    person_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contact_role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_contacts", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_contacts_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_client_contacts_people_organization_id_person_id",
                        columns: x => new { x.organization_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    has_transfer_authority = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_clients", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_clients_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_clients_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permission_grants",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_account_id = table.Column<Guid>(type: "uuid", nullable: true),
                    organization_membership_id = table.Column<Guid>(type: "uuid", nullable: true),
                    permission = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    effect = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    scope = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_permission_grants", x => x.id);
                    table.CheckConstraint("ck_permission_grants_scope", "(scope = 'Organization' AND event_id IS NULL) OR (scope = 'Event' AND event_id IS NOT NULL)");
                    table.CheckConstraint("ck_permission_grants_subject", "(user_account_id IS NOT NULL AND organization_membership_id IS NULL) OR (user_account_id IS NULL AND organization_membership_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_permission_grants_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_permission_grants_organization_memberships_organization_id_",
                        columns: x => new { x.organization_id, x.organization_membership_id },
                        principalTable: "organization_memberships",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_permission_grants_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_permission_grants_user_accounts_user_account_id",
                        column: x => x.user_account_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_access_invitations_invited_by",
                table: "access_invitations",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_access_invitations_organization_id_event_id",
                table: "access_invitations",
                columns: new[] { "organization_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_access_invitations_organization_id_normalized_target_email_",
                table: "access_invitations",
                columns: new[] { "organization_id", "normalized_target_email", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_access_invitations_token_hash",
                table: "access_invitations",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_actor_user_id",
                table: "audit_entries",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_organization_id_entity_type_entity_id",
                table: "audit_entries",
                columns: new[] { "organization_id", "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_organization_id_event_id",
                table: "audit_entries",
                columns: new[] { "organization_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_entries_organization_id_occurred_at",
                table: "audit_entries",
                columns: new[] { "organization_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_basic_documents_organization_id_client_id",
                table: "basic_documents",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_basic_documents_organization_id_event_id_visibility_deleted",
                table: "basic_documents",
                columns: new[] { "organization_id", "event_id", "visibility", "deleted_at" });

            migrationBuilder.CreateIndex(
                name: "ix_basic_documents_storage_key",
                table: "basic_documents",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_basic_documents_uploaded_by",
                table: "basic_documents",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "ix_client_contacts_organization_id_client_id",
                table: "client_contacts",
                columns: new[] { "organization_id", "client_id" },
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_client_contacts_organization_id_person_id",
                table: "client_contacts",
                columns: new[] { "organization_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_organization_id_display_name",
                table: "clients",
                columns: new[] { "organization_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_clients_organization_id_person_id",
                table: "clients",
                columns: new[] { "organization_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_accesses_invited_by",
                table: "event_accesses",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "ix_event_accesses_organization_id_event_id_user_account_id",
                table: "event_accesses",
                columns: new[] { "organization_id", "event_id", "user_account_id" },
                unique: true,
                filter: "status <> 'Revoked' AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_event_accesses_user_account_id",
                table: "event_accesses",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_clients_organization_id_client_id",
                table: "event_clients",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_clients_organization_id_event_id",
                table: "event_clients",
                columns: new[] { "organization_id", "event_id" },
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_event_clients_organization_id_event_id_client_id_relationsh",
                table: "event_clients",
                columns: new[] { "organization_id", "event_id", "client_id", "relationship_type" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_organization_id_event_id_display_order",
                table: "event_participants",
                columns: new[] { "organization_id", "event_id", "display_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_participants_organization_id_person_id",
                table: "event_participants",
                columns: new[] { "organization_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_status_history_changed_by",
                table: "event_status_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "ix_event_status_history_organization_id_event_id_changed_at",
                table: "event_status_history",
                columns: new[] { "organization_id", "event_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_events_created_by",
                table: "events",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_events_organization_id_start_date_time",
                table: "events",
                columns: new[] { "organization_id", "start_date_time" });

            migrationBuilder.CreateIndex(
                name: "ix_events_organization_id_status",
                table: "events",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_memberships_organization_id_person_id",
                table: "organization_memberships",
                columns: new[] { "organization_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_organization_memberships_organization_id_user_account_id",
                table: "organization_memberships",
                columns: new[] { "organization_id", "user_account_id" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_organization_memberships_user_account_id",
                table: "organization_memberships",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_organizations_slug",
                table: "organizations",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_people_linked_user_account_id",
                table: "people",
                column: "linked_user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_people_organization_id_linked_user_account_id",
                table: "people",
                columns: new[] { "organization_id", "linked_user_account_id" },
                unique: true,
                filter: "linked_user_account_id IS NOT NULL AND archived_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_permission_grants_organization_id_event_id",
                table: "permission_grants",
                columns: new[] { "organization_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_permission_grants_organization_id_organization_membership_i",
                table: "permission_grants",
                columns: new[] { "organization_id", "organization_membership_id", "permission", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_permission_grants_organization_id_user_account_id_permissio",
                table: "permission_grants",
                columns: new[] { "organization_id", "user_account_id", "permission", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_permission_grants_user_account_id",
                table: "permission_grants",
                column: "user_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_accounts_normalized_email",
                table: "user_accounts",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_refresh_token_hash",
                table: "user_sessions",
                column: "refresh_token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_replaced_by_session_id",
                table: "user_sessions",
                column: "replaced_by_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_sessions_user_account_id_revoked_at",
                table: "user_sessions",
                columns: new[] { "user_account_id", "revoked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_invitations");

            migrationBuilder.DropTable(
                name: "audit_entries");

            migrationBuilder.DropTable(
                name: "basic_documents");

            migrationBuilder.DropTable(
                name: "client_contacts");

            migrationBuilder.DropTable(
                name: "event_accesses");

            migrationBuilder.DropTable(
                name: "event_clients");

            migrationBuilder.DropTable(
                name: "event_participants");

            migrationBuilder.DropTable(
                name: "event_status_history");

            migrationBuilder.DropTable(
                name: "permission_grants");

            migrationBuilder.DropTable(
                name: "user_sessions");

            migrationBuilder.DropTable(
                name: "clients");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "organization_memberships");

            migrationBuilder.DropTable(
                name: "people");

            migrationBuilder.DropTable(
                name: "organizations");

            migrationBuilder.DropTable(
                name: "user_accounts");
        }
    }
}
