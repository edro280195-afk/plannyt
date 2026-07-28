using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGuestsAndDigitalInvitations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_guest_experiences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    language = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    public_title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    celebrant_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    welcome_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    closing_message = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    show_event_name = table.Column<bool>(type: "boolean", nullable: false),
                    show_event_date = table.Column<bool>(type: "boolean", nullable: false),
                    show_participant_names = table.Column<bool>(type: "boolean", nullable: false),
                    show_city = table.Column<bool>(type: "boolean", nullable: false),
                    private_access_only = table.Column<bool>(type: "boolean", nullable: false),
                    active_invitation_design_id = table.Column<Guid>(type: "uuid", nullable: true),
                    active_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    suspended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_guest_experiences", x => x.id);
                    table.UniqueConstraint("ak_event_guest_experiences_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_guest_experiences_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_import_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    csv_content = table.Column<string>(type: "text", nullable: false),
                    mapping_json = table.Column<string>(type: "jsonb", nullable: false),
                    analysis_json = table.Column<string>(type: "jsonb", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    total_rows = table.Column<int>(type: "integer", nullable: false),
                    valid_rows = table.Column<int>(type: "integer", nullable: false),
                    error_rows = table.Column<int>(type: "integer", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_import_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_import_batches_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    color_token = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_tags", x => x.id);
                    table.UniqueConstraint("ak_guest_tags_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_guest_tags_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_designs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    draft_theme_json = table.Column<string>(type: "jsonb", nullable: false),
                    draft_content_json = table.Column<string>(type: "jsonb", nullable: false),
                    next_version_number = table.Column<int>(type: "integer", nullable: false),
                    approved_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_designs", x => x.id);
                    table.UniqueConstraint("ak_invitation_designs_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_invitation_designs_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_groups",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    contact_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    contact_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    allowed_guest_count = table.Column<int>(type: "integer", nullable: false),
                    allow_unnamed_companions = table.Column<bool>(type: "boolean", nullable: false),
                    max_unnamed_companions = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    source = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    internal_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    capacity_override_applied = table.Column<bool>(type: "boolean", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_groups", x => x.id);
                    table.UniqueConstraint("ak_invitation_groups_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.CheckConstraint("ck_invitation_groups_capacity", "allowed_guest_count >= 1 AND max_unnamed_companions >= 0 AND max_unnamed_companions <= allowed_guest_count");
                    table.ForeignKey(
                        name: "fk_invitation_groups_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_global = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    theme_json = table.Column<string>(type: "jsonb", nullable: false),
                    content_json = table.Column<string>(type: "jsonb", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "invitation_design_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_design_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    theme_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    content_snapshot_json = table.Column<string>(type: "jsonb", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_design_versions", x => x.id);
                    table.UniqueConstraint("ak_invitation_design_versions_organization_id_event_id_invitat", x => new { x.organization_id, x.event_id, x.invitation_design_id, x.id });
                    table.ForeignKey(
                        name: "fk_invitation_design_versions_invitation_designs_organization_",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_design_id },
                        principalTable: "invitation_designs",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_guests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    person_id = table.Column<Guid>(type: "uuid", nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    guest_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    age_category = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_primary_contact = table.Column<bool>(type: "boolean", nullable: false),
                    is_named = table.Column<bool>(type: "boolean", nullable: false),
                    is_plus_one = table.Column<bool>(type: "boolean", nullable: false),
                    is_vip = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    internal_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_guests", x => x.id);
                    table.UniqueConstraint("ak_event_guests_organization_id_event_id_id", x => new { x.organization_id, x.event_id, x.id });
                    table.ForeignKey(
                        name: "fk_event_guests_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_guests_invitation_groups_organization_id_event_id_inv",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_event_guests_people_organization_id_person_id",
                        columns: x => new { x.organization_id, x.person_id },
                        principalTable: "people",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "guest_access_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    replaced_by_link_id = table.Column<Guid>(type: "uuid", nullable: true),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_opened_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    open_count = table.Column<int>(type: "integer", nullable: false),
                    shared_manually_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_access_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_access_links_invitation_groups_organization_id_event_",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invitation_group_tags",
                columns: table => new
                {
                    invitation_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    guest_tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_group_tags", x => new { x.invitation_group_id, x.guest_tag_id });
                    table.ForeignKey(
                        name: "fk_invitation_group_tags_guest_tags_organization_id_event_id_g",
                        columns: x => new { x.organization_id, x.event_id, x.guest_tag_id },
                        principalTable: "guest_tags",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invitation_group_tags_invitation_groups_organization_id_eve",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_group_id },
                        principalTable: "invitation_groups",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invitation_design_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_design_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invitation_design_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decision = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    message = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invitation_design_comments", x => x.id);
                    table.ForeignKey(
                        name: "fk_invitation_design_comments_invitation_design_versions_organ",
                        columns: x => new { x.organization_id, x.event_id, x.invitation_design_id, x.invitation_design_version_id },
                        principalTable: "invitation_design_versions",
                        principalColumns: new[] { "organization_id", "event_id", "invitation_design_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_guest_experiences_organization_id_event_id",
                table: "event_guest_experiences",
                columns: new[] { "organization_id", "event_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_guests_organization_id_event_id_invitation_group_id",
                table: "event_guests",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_event_guests_organization_id_event_id_invitation_group_id_i",
                table: "event_guests",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "is_primary_contact" },
                unique: true,
                filter: "archived_at IS NULL AND is_primary_contact = TRUE");

            migrationBuilder.CreateIndex(
                name: "ix_event_guests_organization_id_person_id",
                table: "event_guests",
                columns: new[] { "organization_id", "person_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_access_links_organization_id_event_id_invitation_grou",
                table: "guest_access_links",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "status" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_guest_access_links_token_hash",
                table: "guest_access_links",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_import_batches_organization_id_event_id_created_at",
                table: "guest_import_batches",
                columns: new[] { "organization_id", "event_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_tags_organization_id_event_id_name",
                table: "guest_tags",
                columns: new[] { "organization_id", "event_id", "name" },
                unique: true,
                filter: "archived_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_invitation_design_comments_organization_id_event_id_invitat",
                table: "invitation_design_comments",
                columns: new[] { "organization_id", "event_id", "invitation_design_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_design_comments_organization_id_event_id_invitat1",
                table: "invitation_design_comments",
                columns: new[] { "organization_id", "event_id", "invitation_design_id", "invitation_design_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_design_versions_organization_id_event_id_invitat",
                table: "invitation_design_versions",
                columns: new[] { "organization_id", "event_id", "invitation_design_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invitation_designs_organization_id_event_id_updated_at",
                table: "invitation_designs",
                columns: new[] { "organization_id", "event_id", "updated_at" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_group_tags_organization_id_event_id_guest_tag_id",
                table: "invitation_group_tags",
                columns: new[] { "organization_id", "event_id", "guest_tag_id" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_group_tags_organization_id_event_id_invitation_g",
                table: "invitation_group_tags",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_groups_organization_id_event_id_display_name",
                table: "invitation_groups",
                columns: new[] { "organization_id", "event_id", "display_name" });

            migrationBuilder.CreateIndex(
                name: "ix_invitation_templates_organization_id_name",
                table: "invitation_templates",
                columns: new[] { "organization_id", "name" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_guest_experiences");

            migrationBuilder.DropTable(
                name: "event_guests");

            migrationBuilder.DropTable(
                name: "guest_access_links");

            migrationBuilder.DropTable(
                name: "guest_import_batches");

            migrationBuilder.DropTable(
                name: "invitation_design_comments");

            migrationBuilder.DropTable(
                name: "invitation_group_tags");

            migrationBuilder.DropTable(
                name: "invitation_templates");

            migrationBuilder.DropTable(
                name: "invitation_design_versions");

            migrationBuilder.DropTable(
                name: "guest_tags");

            migrationBuilder.DropTable(
                name: "invitation_groups");

            migrationBuilder.DropTable(
                name: "invitation_designs");
        }
    }
}
