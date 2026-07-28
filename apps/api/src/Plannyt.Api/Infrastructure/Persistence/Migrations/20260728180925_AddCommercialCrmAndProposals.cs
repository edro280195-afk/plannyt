using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCommercialCrmAndProposals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "coupons",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    discount_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    starts_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ends_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    maximum_uses = table.Column<int>(type: "integer", nullable: true),
                    current_uses = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_coupons", x => x.id);
                    table.UniqueConstraint("ak_coupons_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_coupons_dates", "ends_at >= starts_at");
                    table.CheckConstraint("ck_coupons_uses", "current_uses >= 0 AND (maximum_uses IS NULL OR maximum_uses > 0)");
                    table.CheckConstraint("ck_coupons_value", "discount_value >= 0");
                    table.ForeignKey(
                        name: "fk_coupons_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_negotiable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packages", x => x.id);
                    table.UniqueConstraint("ak_packages_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_packages_base_price", "base_price >= 0");
                    table.ForeignKey(
                        name: "fk_packages_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    company_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: true),
                    phone = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    event_type_interest = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    estimated_event_date = table.Column<DateOnly>(type: "date", nullable: true),
                    estimated_guest_count = table.Column<int>(type: "integer", nullable: true),
                    estimated_budget = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    lost_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    converted_client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prospects", x => x.id);
                    table.UniqueConstraint("ak_prospects_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_prospects_budget", "estimated_budget IS NULL OR estimated_budget >= 0");
                    table.CheckConstraint("ck_prospects_guest_count", "estimated_guest_count IS NULL OR estimated_guest_count >= 0");
                    table.CheckConstraint("ck_prospects_lost_reason", "(status = 'Lost' AND lost_reason IS NOT NULL) OR status <> 'Lost'");
                    table.ForeignKey(
                        name: "fk_prospects_clients_organization_id_converted_client_id",
                        columns: x => new { x.organization_id, x.converted_client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prospects_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prospects_user_accounts_assigned_user_id",
                        column: x => x.assigned_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_catalog_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    pricing_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    base_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    tax_behavior = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    is_negotiable = table.Column<bool>(type: "boolean", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    archived_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_service_catalog_items", x => x.id);
                    table.UniqueConstraint("ak_service_catalog_items_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_catalog_base_price", "base_price >= 0");
                    table.CheckConstraint("ck_catalog_sort_order", "sort_order >= 0");
                    table.ForeignKey(
                        name: "fk_service_catalog_items_organizations_organization_id",
                        column: x => x.organization_id,
                        principalTable: "organizations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_activities",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    scheduled_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    visibility = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prospect_activities", x => x.id);
                    table.ForeignKey(
                        name: "fk_prospect_activities_prospects_organization_id_prospect_id",
                        columns: x => new { x.organization_id, x.prospect_id },
                        principalTable: "prospects",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prospect_activities_user_accounts_assigned_user_id",
                        column: x => x.assigned_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prospect_activities_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "prospect_status_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    new_status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    changed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_prospect_status_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_prospect_status_history_prospects_organization_id_prospect_",
                        columns: x => new { x.organization_id, x.prospect_id },
                        principalTable: "prospects",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_prospect_status_history_user_accounts_changed_by",
                        column: x => x.changed_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "package_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    included_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_package_items", x => x.id);
                    table.CheckConstraint("ck_package_items_price", "included_price IS NULL OR included_price >= 0");
                    table.CheckConstraint("ck_package_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_package_items_packages_organization_id_package_id",
                        columns: x => new { x.organization_id, x.package_id },
                        principalTable: "packages",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_package_items_service_catalog_items_organization_id_service",
                        columns: x => new { x.organization_id, x.service_catalog_item_id },
                        principalTable: "service_catalog_items",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_comments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_line_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    author_display_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    content = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    visibility = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    parent_comment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_comments", x => x.id);
                    table.UniqueConstraint("ak_proposal_comments_organization_id_id", x => new { x.organization_id, x.id });
                    table.ForeignKey(
                        name: "fk_proposal_comments_proposal_comments_organization_id_parent_",
                        columns: x => new { x.organization_id, x.parent_comment_id },
                        principalTable: "proposal_comments",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_comments_user_accounts_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_draft_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    service_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_draft_lines", x => x.id);
                    table.CheckConstraint("ck_draft_lines_discount", "discount_value >= 0");
                    table.CheckConstraint("ck_draft_lines_price", "unit_price >= 0");
                    table.CheckConstraint("ck_draft_lines_quantity", "quantity > 0");
                    table.CheckConstraint("ck_draft_lines_tax", "tax_rate >= 0 AND tax_rate <= 100");
                    table.ForeignKey(
                        name: "fk_proposal_draft_lines_packages_organization_id_package_id",
                        columns: x => new { x.organization_id, x.package_id },
                        principalTable: "packages",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_draft_lines_service_catalog_items_organization_id_",
                        columns: x => new { x.organization_id, x.service_catalog_item_id },
                        principalTable: "service_catalog_items",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_lines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    service_catalog_item_id = table.Column<Guid>(type: "uuid", nullable: true),
                    package_id = table.Column<Guid>(type: "uuid", nullable: true),
                    quantity = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    unit_price = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(7,4)", precision: 7, scale: 4, nullable: false),
                    line_subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_discount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_tax = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    is_optional = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_lines", x => x.id);
                    table.UniqueConstraint("ak_proposal_lines_organization_id_id", x => new { x.organization_id, x.id });
                });

            migrationBuilder.CreateTable(
                name: "proposal_share_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    first_viewed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_share_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_proposal_share_links_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposal_versions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    proposal_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    subtotal = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    grand_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    shared_introduction = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    shared_terms = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    general_discount_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    general_discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    general_discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    coupon_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    coupon_discount_total = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposal_versions", x => x.id);
                    table.UniqueConstraint("ak_proposal_versions_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_proposal_versions_totals", "subtotal >= 0 AND discount_total >= 0 AND tax_total >= 0 AND grand_total >= 0");
                    table.ForeignKey(
                        name: "fk_proposal_versions_coupons_organization_id_coupon_id",
                        columns: x => new { x.organization_id, x.coupon_id },
                        principalTable: "coupons",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposal_versions_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "proposals",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    prospect_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    proposal_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_version_number = table.Column<int>(type: "integer", nullable: false),
                    currency_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    valid_until = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    shared_introduction = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    shared_terms = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    internal_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    general_discount_type = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    general_discount_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    coupon_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    rejected_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_proposals", x => x.id);
                    table.UniqueConstraint("ak_proposals_organization_id_id", x => new { x.organization_id, x.id });
                    table.CheckConstraint("ck_proposals_discount", "general_discount_value >= 0");
                    table.CheckConstraint("ck_proposals_target", "prospect_id IS NOT NULL OR client_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_proposals_clients_organization_id_client_id",
                        columns: x => new { x.organization_id, x.client_id },
                        principalTable: "clients",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_coupons_organization_id_coupon_id",
                        columns: x => new { x.organization_id, x.coupon_id },
                        principalTable: "coupons",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_events_organization_id_event_id",
                        columns: x => new { x.organization_id, x.event_id },
                        principalTable: "events",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_proposal_versions_organization_id_accepted_versio",
                        columns: x => new { x.organization_id, x.accepted_version_id },
                        principalTable: "proposal_versions",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_prospects_organization_id_prospect_id",
                        columns: x => new { x.organization_id, x.prospect_id },
                        principalTable: "prospects",
                        principalColumns: new[] { "organization_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_proposals_user_accounts_created_by",
                        column: x => x.created_by,
                        principalTable: "user_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_coupons_organization_id_code",
                table: "coupons",
                columns: new[] { "organization_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_items_organization_id_package_id_service_catalog_it",
                table: "package_items",
                columns: new[] { "organization_id", "package_id", "service_catalog_item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_package_items_organization_id_service_catalog_item_id",
                table: "package_items",
                columns: new[] { "organization_id", "service_catalog_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_packages_organization_id_is_active",
                table: "packages",
                columns: new[] { "organization_id", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_comments_author_user_id",
                table: "proposal_comments",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_comments_organization_id_parent_comment_id",
                table: "proposal_comments",
                columns: new[] { "organization_id", "parent_comment_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_comments_organization_id_proposal_id",
                table: "proposal_comments",
                columns: new[] { "organization_id", "proposal_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_comments_organization_id_proposal_line_id",
                table: "proposal_comments",
                columns: new[] { "organization_id", "proposal_line_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_comments_organization_id_proposal_version_id_creat",
                table: "proposal_comments",
                columns: new[] { "organization_id", "proposal_version_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_draft_lines_organization_id_package_id",
                table: "proposal_draft_lines",
                columns: new[] { "organization_id", "package_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_draft_lines_organization_id_proposal_id_sort_order",
                table: "proposal_draft_lines",
                columns: new[] { "organization_id", "proposal_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_draft_lines_organization_id_service_catalog_item_id",
                table: "proposal_draft_lines",
                columns: new[] { "organization_id", "service_catalog_item_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_lines_organization_id_proposal_version_id_sort_ord",
                table: "proposal_lines",
                columns: new[] { "organization_id", "proposal_version_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_share_links_created_by",
                table: "proposal_share_links",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_share_links_organization_id_proposal_id_revoked_at",
                table: "proposal_share_links",
                columns: new[] { "organization_id", "proposal_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_share_links_organization_id_proposal_version_id",
                table: "proposal_share_links",
                columns: new[] { "organization_id", "proposal_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_share_links_token_hash",
                table: "proposal_share_links",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposal_versions_created_by",
                table: "proposal_versions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_proposal_versions_organization_id_coupon_id",
                table: "proposal_versions",
                columns: new[] { "organization_id", "coupon_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposal_versions_organization_id_proposal_id_version_number",
                table: "proposal_versions",
                columns: new[] { "organization_id", "proposal_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposals_created_by",
                table: "proposals",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_accepted_version_id",
                table: "proposals",
                columns: new[] { "organization_id", "accepted_version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_client_id",
                table: "proposals",
                columns: new[] { "organization_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_coupon_id",
                table: "proposals",
                columns: new[] { "organization_id", "coupon_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_event_id",
                table: "proposals",
                columns: new[] { "organization_id", "event_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_proposal_number",
                table: "proposals",
                columns: new[] { "organization_id", "proposal_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_prospect_id",
                table: "proposals",
                columns: new[] { "organization_id", "prospect_id" });

            migrationBuilder.CreateIndex(
                name: "ix_proposals_organization_id_status",
                table: "proposals",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_prospect_activities_assigned_user_id",
                table: "prospect_activities",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_prospect_activities_created_by",
                table: "prospect_activities",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "ix_prospect_activities_organization_id_prospect_id_scheduled_at",
                table: "prospect_activities",
                columns: new[] { "organization_id", "prospect_id", "scheduled_at" });

            migrationBuilder.CreateIndex(
                name: "ix_prospect_status_history_changed_by",
                table: "prospect_status_history",
                column: "changed_by");

            migrationBuilder.CreateIndex(
                name: "ix_prospect_status_history_organization_id_prospect_id_changed",
                table: "prospect_status_history",
                columns: new[] { "organization_id", "prospect_id", "changed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_prospects_assigned_user_id",
                table: "prospects",
                column: "assigned_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_prospects_organization_id_assigned_user_id",
                table: "prospects",
                columns: new[] { "organization_id", "assigned_user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_prospects_organization_id_converted_client_id",
                table: "prospects",
                columns: new[] { "organization_id", "converted_client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_prospects_organization_id_email",
                table: "prospects",
                columns: new[] { "organization_id", "email" });

            migrationBuilder.CreateIndex(
                name: "ix_prospects_organization_id_phone",
                table: "prospects",
                columns: new[] { "organization_id", "phone" });

            migrationBuilder.CreateIndex(
                name: "ix_prospects_organization_id_status",
                table: "prospects",
                columns: new[] { "organization_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_service_catalog_items_organization_id_is_active_sort_order",
                table: "service_catalog_items",
                columns: new[] { "organization_id", "is_active", "sort_order" });

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_comments_proposal_lines_organization_id_proposal_l",
                table: "proposal_comments",
                columns: new[] { "organization_id", "proposal_line_id" },
                principalTable: "proposal_lines",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_comments_proposal_versions_organization_id_proposa",
                table: "proposal_comments",
                columns: new[] { "organization_id", "proposal_version_id" },
                principalTable: "proposal_versions",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_comments_proposals_organization_id_proposal_id",
                table: "proposal_comments",
                columns: new[] { "organization_id", "proposal_id" },
                principalTable: "proposals",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_draft_lines_proposals_organization_id_proposal_id",
                table: "proposal_draft_lines",
                columns: new[] { "organization_id", "proposal_id" },
                principalTable: "proposals",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_lines_proposal_versions_organization_id_proposal_v",
                table: "proposal_lines",
                columns: new[] { "organization_id", "proposal_version_id" },
                principalTable: "proposal_versions",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_share_links_proposal_versions_organization_id_prop",
                table: "proposal_share_links",
                columns: new[] { "organization_id", "proposal_version_id" },
                principalTable: "proposal_versions",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_share_links_proposals_organization_id_proposal_id",
                table: "proposal_share_links",
                columns: new[] { "organization_id", "proposal_id" },
                principalTable: "proposals",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_proposal_versions_proposals_organization_id_proposal_id",
                table: "proposal_versions",
                columns: new[] { "organization_id", "proposal_id" },
                principalTable: "proposals",
                principalColumns: new[] { "organization_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_proposals_proposal_versions_organization_id_accepted_versio",
                table: "proposals");

            migrationBuilder.DropTable(
                name: "package_items");

            migrationBuilder.DropTable(
                name: "proposal_comments");

            migrationBuilder.DropTable(
                name: "proposal_draft_lines");

            migrationBuilder.DropTable(
                name: "proposal_share_links");

            migrationBuilder.DropTable(
                name: "prospect_activities");

            migrationBuilder.DropTable(
                name: "prospect_status_history");

            migrationBuilder.DropTable(
                name: "proposal_lines");

            migrationBuilder.DropTable(
                name: "packages");

            migrationBuilder.DropTable(
                name: "service_catalog_items");

            migrationBuilder.DropTable(
                name: "proposal_versions");

            migrationBuilder.DropTable(
                name: "proposals");

            migrationBuilder.DropTable(
                name: "coupons");

            migrationBuilder.DropTable(
                name: "prospects");
        }
    }
}
