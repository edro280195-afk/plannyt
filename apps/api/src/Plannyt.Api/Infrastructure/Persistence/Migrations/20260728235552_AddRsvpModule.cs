using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRsvpModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "derivation_key_id",
                table: "guest_access_links",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "event_accommodation_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    booking_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    booking_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    booking_deadline = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    contact_information = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_accommodation_options", x => x.id);
                    table.UniqueConstraint("ak_event_accommodation_options_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_accommodation_options_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_menus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    menu_category = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    selection_required = table.Column<bool>(type: "boolean", nullable: false),
                    minimum_selections = table.Column<int>(type: "integer", nullable: false),
                    maximum_selections = table.Column<int>(type: "integer", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_menus", x => x.id);
                    table.UniqueConstraint("ak_event_menus_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_event_menus_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_menus_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_rsvp_settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    opens_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    closes_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    time_zone = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    allow_changes_after_submission = table.Column<bool>(type: "boolean", nullable: false),
                    changes_close_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allow_tentative_response = table.Column<bool>(type: "boolean", nullable: false),
                    allow_group_decline = table.Column<bool>(type: "boolean", nullable: false),
                    require_response_for_every_named_guest = table.Column<bool>(type: "boolean", nullable: false),
                    require_companion_names = table.Column<bool>(type: "boolean", nullable: false),
                    allow_contact_information_update = table.Column<bool>(type: "boolean", nullable: false),
                    show_attendance_summary_after_submission = table.Column<bool>(type: "boolean", nullable: false),
                    confirmation_title = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    confirmation_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decline_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    closed_message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    privacy_notice = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sensitive_data_consent_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_rsvp_settings", x => x.id);
                    table.UniqueConstraint("ak_event_rsvp_settings_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_rsvp_settings_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_transport_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    direction = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    pickup_point = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    departure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    return_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    allow_waitlist = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_transport_options", x => x.id);
                    table.UniqueConstraint("ak_event_transport_options_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_transport_options_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_access_token_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    key_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_access_token_keys", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_access_token_keys_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_dietary_accessibility",
                columns: table => new
                {
                    event_guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allergies = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    dietary_restrictions = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    accessibility_requirements = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    additional_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    consent_granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_dietary_accessibility", x => x.event_guest_id);
                    table.ForeignKey(
                        name: "fk_guest_dietary_accessibility_event_guests_organization_id_ev",
                        columns: x => new { x.organization_id, x.event_id, x.event_guest_id },
                        principalTable: "event_guests",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reminder_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    channel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    segment_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    message_template = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_reminder_templates", x => x.id);
                    table.UniqueConstraint("ak_reminder_templates_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_reminder_templates_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rsvp_forms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    current_draft_version = table.Column<int>(type: "integer", nullable: false),
                    active_published_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rsvp_forms", x => x.id);
                    table.UniqueConstraint("ak_rsvp_forms_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.UniqueConstraint("ak_rsvp_forms_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_rsvp_forms_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rsvp_group_exceptions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    closed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rsvp_group_exceptions", x => x.id);
                    table.UniqueConstraint("ak_rsvp_group_exceptions_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_rsvp_group_exceptions_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rsvp_group_exceptions_invitation_groups_organization_id_eve",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_accommodation_selections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_guest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_accommodation_option_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    reservation_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    confirmation_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_accommodation_selections", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_accommodation_selections_event_accommodation_options_",
                        column: x => x.event_accommodation_option_id,
                        principalTable: "event_accommodation_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guest_accommodation_selections_event_guests_event_guest_id",
                        column: x => x.event_guest_id,
                        principalTable: "event_guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_menu_options",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_menu_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    dietary_tags = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    capacity = table.Column<int>(type: "integer", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_menu_options", x => x.id);
                    table.UniqueConstraint("ak_event_menu_options_organization_id_event_menu_id_id", x => new { x.organization_id, x.event_menu_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_menu_options_event_menus_organization_id_event_menu_id",
                        columns: x => new { x.organization_id, x.event_menu_id },
                        principalTable: "event_menus",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_transport_selections",
                columns: table => new
                {
                    event_guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_transport_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    last_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_transport_selections", x => new { x.event_guest_id, x.event_transport_option_id });
                    table.ForeignKey(
                        name: "fk_guest_transport_selections_event_guests_event_guest_id",
                        column: x => x.event_guest_id,
                        principalTable: "event_guests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guest_transport_selections_event_transport_options_event_tr",
                        column: x => x.event_transport_option_id,
                        principalTable: "event_transport_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_reminder_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminder_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    channel = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_reminder_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_reminder_logs_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_reminder_logs_invitation_groups_organization_id_event",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_reminder_logs_reminder_templates_organization_id_remi",
                        columns: x => new { x.organization_id, x.reminder_template_id },
                        principalTable: "reminder_templates",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_reminder_logs_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rsvp_form_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rsvp_form_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    settings_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    questions_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    menu_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    transport_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    accommodation_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rsvp_form_versions", x => x.id);
                    table.UniqueConstraint("ak_rsvp_form_versions_organization_id_id", x => new { x.organization_id, x.id });
                    table.UniqueConstraint("ak_rsvp_form_versions_organization_id_rsvp_form_id_id", x => new { x.organization_id, x.rsvp_form_id, x.id });
                    table.ForeignKey(
                        name: "fk_rsvp_form_versions_rsvp_forms_organization_id_rsvp_form_id",
                        columns: x => new { x.organization_id, x.rsvp_form_id },
                        principalTable: "rsvp_forms",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rsvp_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rsvp_form_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_access_link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    revision_number = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    overall_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    submitted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    submitted_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contact_name_snapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    contact_email_snapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    contact_phone_snapshot = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    user_agent_category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ip_address = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    consent_snapshot = table.Column<string>(type: "jsonb", nullable: true),
                    idempotency_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    previous_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rsvp_submissions", x => x.id);
                    table.UniqueConstraint("ak_rsvp_submissions_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_rsvp_submissions_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rsvp_submissions_guest_access_links_guest_access_link_id",
                        column: x => x.guest_access_link_id,
                        principalTable: "guest_access_links",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rsvp_submissions_invitation_groups_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_rsvp_submissions_rsvp_form_versions_organization_id_rsvp_fo",
                        columns: x => new { x.organization_id, x.rsvp_form_version_id },
                        principalTable: "rsvp_form_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "current_guest_rsvps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_guest_id = table.Column<Guid>(type: "uuid", nullable: true),
                    attendance_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    is_unnamed_companion = table.Column<bool>(type: "boolean", nullable: false),
                    companion_slot_number = table.Column<int>(type: "integer", nullable: true),
                    current_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    last_submission_id = table.Column<Guid>(type: "uuid", nullable: true),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_current_guest_rsvps", x => x.id);
                    table.UniqueConstraint("ak_current_guest_rsvps_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_current_guest_rsvps_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_current_guest_rsvps_invitation_groups_organization_id_event",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_current_guest_rsvps_rsvp_submissions_organization_id_event_",
                        columns: x => new { x.organization_id, x.event_id, x.last_submission_id },
                        principalTable: "rsvp_submissions",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "rsvp_submission_answers",
                columns: table => new
                {
                    rsvp_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    question_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    answer_value = table.Column<string>(type: "jsonb", nullable: false),
                    display_value_snapshot = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rsvp_submission_answers", x => new { x.rsvp_submission_id, x.question_id, x.guest_id });
                    table.ForeignKey(
                        name: "fk_rsvp_submission_answers_rsvp_submissions_rsvp_submission_id",
                        column: x => x.rsvp_submission_id,
                        principalTable: "rsvp_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "rsvp_submission_guests",
                columns: table => new
                {
                    rsvp_submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    age_category = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    attendance_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    menu_selections_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    transport_selection_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    accommodation_selection_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    dietary_snapshot = table.Column<string>(type: "jsonb", nullable: false),
                    is_unnamed_companion = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_rsvp_submission_guests", x => new { x.rsvp_submission_id, x.event_guest_id });
                    table.ForeignKey(
                        name: "fk_rsvp_submission_guests_rsvp_submissions_rsvp_submission_id",
                        column: x => x.rsvp_submission_id,
                        principalTable: "rsvp_submissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_current_guest_rsvps_organization_id_event_id_invitation_gro",
                table: "current_guest_rsvps",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_current_guest_rsvps_organization_id_event_id_last_submissio",
                table: "current_guest_rsvps",
                columns: new[] { "organization_id", "event_id", "last_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_menus_organization_id_event_id_sort_order",
                table: "event_menus",
                columns: new[] { "organization_id", "event_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_event_reminder_logs_created_by",
                table: "event_reminder_logs",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_event_reminder_logs_organization_id_event_id_invitation_gro",
                table: "event_reminder_logs",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_reminder_logs_organization_id_reminder_template_id",
                table: "event_reminder_logs",
                columns: new[] { "organization_id", "reminder_template_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_rsvp_settings_organization_id_event_id",
                table: "event_rsvp_settings",
                columns: new[] { "organization_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_access_token_keys_key_id",
                table: "guest_access_token_keys",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_access_token_keys_organization_id",
                table: "guest_access_token_keys",
                column: "organization_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_event_accommodation_option_id",
                table: "guest_accommodation_selections",
                column: "event_accommodation_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_event_guest_id",
                table: "guest_accommodation_selections",
                column: "event_guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_dietary_accessibility_organization_id_event_id_event_",
                table: "guest_dietary_accessibility",
                columns: new[] { "organization_id", "event_id", "event_guest_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selections_event_transport_option_id",
                table: "guest_transport_selections",
                column: "event_transport_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_reminder_templates_organization_id_event_id",
                table: "reminder_templates",
                columns: new[] { "organization_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_form_versions_organization_id_rsvp_form_id_version_num",
                table: "rsvp_form_versions",
                columns: new[] { "organization_id", "rsvp_form_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_forms_organization_id_event_id",
                table: "rsvp_forms",
                columns: new[] { "organization_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_group_exceptions_organization_id_event_id_invitation_g",
                table: "rsvp_group_exceptions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "status" },
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_guest_access_link_id",
                table: "rsvp_submissions",
                column: "guest_access_link_id");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_idempotency_key",
                table: "rsvp_submissions",
                column: "idempotency_key");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_organization_id_event_id_invitation_group_",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_organization_id_rsvp_form_version_id",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "rsvp_form_version_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "current_guest_rsvps");

            migrationBuilder.DropTable(
                name: "event_menu_options");

            migrationBuilder.DropTable(
                name: "event_reminder_logs");

            migrationBuilder.DropTable(
                name: "event_rsvp_settings");

            migrationBuilder.DropTable(
                name: "guest_access_token_keys");

            migrationBuilder.DropTable(
                name: "guest_accommodation_selections");

            migrationBuilder.DropTable(
                name: "guest_dietary_accessibility");

            migrationBuilder.DropTable(
                name: "guest_transport_selections");

            migrationBuilder.DropTable(
                name: "rsvp_group_exceptions");

            migrationBuilder.DropTable(
                name: "rsvp_submission_answers");

            migrationBuilder.DropTable(
                name: "rsvp_submission_guests");

            migrationBuilder.DropTable(
                name: "event_menus");

            migrationBuilder.DropTable(
                name: "reminder_templates");

            migrationBuilder.DropTable(
                name: "event_accommodation_options");

            migrationBuilder.DropTable(
                name: "event_transport_options");

            migrationBuilder.DropTable(
                name: "rsvp_submissions");

            migrationBuilder.DropTable(
                name: "rsvp_form_versions");

            migrationBuilder.DropTable(
                name: "rsvp_forms");

            migrationBuilder.DropColumn(
                name: "derivation_key_id",
                table: "guest_access_links");
        }
    }
}
