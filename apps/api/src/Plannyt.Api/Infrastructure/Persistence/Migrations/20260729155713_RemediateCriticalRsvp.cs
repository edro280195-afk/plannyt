using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemediateCriticalRsvp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_guest_accommodation_selections_event_accommodation_options_",
                table: "guest_accommodation_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_accommodation_selections_event_guests_event_guest_id",
                table: "guest_accommodation_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_transport_selections_event_guests_event_guest_id",
                table: "guest_transport_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_transport_selections_event_transport_options_event_tr",
                table: "guest_transport_selections");

            migrationBuilder.DropTable(
                name: "guest_access_token_keys");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submissions_idempotency_key",
                table: "rsvp_submissions");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submissions_organization_id_event_id_invitation_group_",
                table: "rsvp_submissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_rsvp_submission_guests",
                table: "rsvp_submission_guests");

            migrationBuilder.DropPrimaryKey(
                name: "pk_rsvp_submission_answers",
                table: "rsvp_submission_answers");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_group_exceptions_organization_id_event_id_invitation_g",
                table: "rsvp_group_exceptions");

            migrationBuilder.DropIndex(
                name: "ix_guest_transport_selections_event_transport_option_id",
                table: "guest_transport_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_accommodation_selections_event_accommodation_option_id",
                table: "guest_accommodation_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_accommodation_selections_event_guest_id",
                table: "guest_accommodation_selections");

            migrationBuilder.AddColumn<string>(
                name: "request_fingerprint",
                table: "rsvp_submissions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "event_guest_id",
                table: "rsvp_submission_guests",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "rsvp_submission_guests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "companion_slot_number",
                table: "rsvp_submission_guests",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "guest_id",
                table: "rsvp_submission_answers",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "rsvp_submission_answers",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "closed_by",
                table: "rsvp_group_exceptions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_id",
                table: "guest_transport_selections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "guest_transport_selections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "requested_at",
                table: "guest_transport_selections",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "waitlist_sequence",
                table: "guest_transport_selections",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "event_id",
                table: "guest_accommodation_selections",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "organization_id",
                table: "guest_accommodation_selections",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE rsvp_submissions
                SET request_fingerprint = 'LEGACY' || md5(id::text)
                WHERE request_fingerprint IS NULL;

                WITH revisions AS (
                    SELECT id,
                           lag(id) OVER (
                               PARTITION BY organization_id, event_id, invitation_group_id
                               ORDER BY revision_number, created_at, id) AS previous_id
                    FROM rsvp_submissions
                )
                UPDATE rsvp_submissions AS submission
                SET previous_submission_id = revisions.previous_id
                FROM revisions
                WHERE submission.id = revisions.id
                  AND submission.revision_number > 1
                  AND submission.previous_submission_id IS NULL;

                UPDATE rsvp_submission_guests
                SET id = gen_random_uuid()
                WHERE id IS NULL;

                UPDATE rsvp_submission_guests
                SET companion_slot_number = companions.slot
                FROM (
                    SELECT id,
                           row_number() OVER (
                               PARTITION BY rsvp_submission_id
                               ORDER BY display_name, id)::integer AS slot
                    FROM rsvp_submission_guests
                    WHERE event_guest_id IS NULL
                ) AS companions
                WHERE rsvp_submission_guests.id = companions.id;

                UPDATE rsvp_submission_answers
                SET id = gen_random_uuid()
                WHERE id IS NULL;

                UPDATE guest_transport_selections AS selection
                SET organization_id = option.organization_id,
                    event_id = option.event_id,
                    requested_at = selection.updated_at
                FROM event_transport_options AS option
                WHERE option.id = selection.event_transport_option_id;

                WITH waitlist AS (
                    SELECT event_guest_id,
                           event_transport_option_id,
                           row_number() OVER (
                               PARTITION BY event_transport_option_id
                               ORDER BY updated_at, event_guest_id) AS sequence
                    FROM guest_transport_selections
                    WHERE status = 'Waitlisted'
                )
                UPDATE guest_transport_selections AS selection
                SET waitlist_sequence = waitlist.sequence
                FROM waitlist
                WHERE selection.event_guest_id = waitlist.event_guest_id
                  AND selection.event_transport_option_id =
                      waitlist.event_transport_option_id;

                UPDATE guest_accommodation_selections AS selection
                SET organization_id = context.organization_id,
                    event_id = context.event_id
                FROM (
                    SELECT selection_id,
                           max(organization_id::text)::uuid AS organization_id,
                           max(event_id::text)::uuid AS event_id
                    FROM (
                        SELECT selection.id AS selection_id,
                               option.organization_id,
                               option.event_id
                        FROM guest_accommodation_selections AS selection
                        JOIN event_accommodation_options AS option
                          ON option.id = selection.event_accommodation_option_id
                        UNION ALL
                        SELECT selection.id,
                               guest.organization_id,
                               guest.event_id
                        FROM guest_accommodation_selections AS selection
                        JOIN event_guests AS guest
                          ON guest.id = selection.event_guest_id
                        UNION ALL
                        SELECT selection.id,
                               invitation_group.organization_id,
                               invitation_group.event_id
                        FROM guest_accommodation_selections AS selection
                        JOIN invitation_groups AS invitation_group
                          ON invitation_group.id = selection.invitation_group_id
                    ) AS candidates
                    GROUP BY selection_id
                ) AS context
                WHERE selection.id = context.selection_id;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "request_fingerprint",
                table: "rsvp_submissions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64,
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "rsvp_submission_guests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "rsvp_submission_answers",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "event_id",
                table: "guest_transport_selections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "guest_transport_selections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "requested_at",
                table: "guest_transport_selections",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "event_id",
                table: "guest_accommodation_selections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "guest_accommodation_selections",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM rsvp_submissions
                        GROUP BY organization_id,
                                 event_id,
                                 invitation_group_id,
                                 idempotency_key
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION USING
                            MESSAGE = 'RSVP-IDEMPOTENCY-DUPLICATES: existen llaves duplicadas; preserve evidencia y resuelva manualmente antes de reintentar.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM rsvp_submissions
                        GROUP BY organization_id,
                                 event_id,
                                 invitation_group_id,
                                 revision_number
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION USING
                            MESSAGE = 'RSVP-REVISION-DUPLICATES: existen revisiones duplicadas; preserve evidencia y resuelva manualmente antes de reintentar.';
                    END IF;

                    IF EXISTS (
                        SELECT 1
                        FROM rsvp_group_exceptions
                        WHERE status = 'Active'
                        GROUP BY organization_id,
                                 event_id,
                                 invitation_group_id
                        HAVING count(*) > 1
                    ) THEN
                        RAISE EXCEPTION USING
                            MESSAGE = 'RSVP-EXCEPTION-DUPLICATES: existe más de una excepción activa por grupo.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AddPrimaryKey(
                name: "pk_rsvp_submission_guests",
                table: "rsvp_submission_guests",
                column: "id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_rsvp_submission_answers",
                table: "rsvp_submission_answers",
                column: "id");

            migrationBuilder.CreateTable(
                name: "guest_transport_selection_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_guest_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_transport_option_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true),
                    new_status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    waitlist_sequence = table.Column<long>(type: "bigint", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_guest_transport_selection_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_guest_transport_selection_history_event_guests_organization",
                        columns: x => new { x.organization_id, x.event_id, x.event_guest_id },
                        principalTable: "event_guests",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guest_transport_selection_history_event_transport_options_o",
                        columns: x => new { x.organization_id, x.event_id, x.event_transport_option_id },
                        principalTable: "event_transport_options",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_guest_transport_selection_history_rsvp_submissions_organiza",
                        columns: x => new { x.organization_id, x.event_id, x.submission_id },
                        principalTable: "rsvp_submissions",
                        principalColumns: new[] { "organization_id", "event_id", "id" },
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_organization_id_event_id_previous_submissi",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "event_id", "previous_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ux_rsvp_submissions_idempotency",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_rsvp_submissions_revision",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "revision_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submission_guests_rsvp_submission_id_companion_slot_nu",
                table: "rsvp_submission_guests",
                columns: new[] { "rsvp_submission_id", "companion_slot_number" },
                unique: true,
                filter: "companion_slot_number IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submission_guests_rsvp_submission_id_event_guest_id",
                table: "rsvp_submission_guests",
                columns: new[] { "rsvp_submission_id", "event_guest_id" },
                unique: true,
                filter: "event_guest_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submission_answers_rsvp_submission_id_question_id_gues",
                table: "rsvp_submission_answers",
                columns: new[] { "rsvp_submission_id", "question_id", "guest_id" },
                unique: true,
                filter: "guest_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_group_exceptions_organization_id_event_id_invitation_g",
                table: "rsvp_group_exceptions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "status" },
                unique: true,
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selections_organization_id_event_id_event_g",
                table: "guest_transport_selections",
                columns: new[] { "organization_id", "event_id", "event_guest_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selections_organization_id_event_id_event_t",
                table: "guest_transport_selections",
                columns: new[] { "organization_id", "event_id", "event_transport_option_id", "waitlist_sequence" },
                unique: true,
                filter: "waitlist_sequence IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selections_organization_id_event_id_last_su",
                table: "guest_transport_selections",
                columns: new[] { "organization_id", "event_id", "last_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_dietary_accessibility_organization_id_event_id_last_s",
                table: "guest_dietary_accessibility",
                columns: new[] { "organization_id", "event_id", "last_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_eve",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "event_accommodation_option_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_eve1",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "event_guest_id" },
                unique: true,
                filter: "event_guest_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_inv",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_las",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "last_submission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selection_history_organization_id_event_id_",
                table: "guest_transport_selection_history",
                columns: new[] { "organization_id", "event_id", "event_guest_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selection_history_organization_id_event_id_1",
                table: "guest_transport_selection_history",
                columns: new[] { "organization_id", "event_id", "submission_id" });

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selection_history_organization_id_event_id_2",
                table: "guest_transport_selection_history",
                columns: new[] { "organization_id", "event_id", "event_transport_option_id", "occurred_at" });

            migrationBuilder.AddForeignKey(
                name: "fk_guest_accommodation_selections_event_accommodation_options_",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "event_accommodation_option_id" },
                principalTable: "event_accommodation_options",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_accommodation_selections_event_guests_organization_id",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "event_guest_id" },
                principalTable: "event_guests",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_accommodation_selections_invitation_groups_organizati",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" },
                principalTable: "invitation_groups",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_accommodation_selections_rsvp_submissions_organizatio",
                table: "guest_accommodation_selections",
                columns: new[] { "organization_id", "event_id", "last_submission_id" },
                principalTable: "rsvp_submissions",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_dietary_accessibility_rsvp_submissions_organization_i",
                table: "guest_dietary_accessibility",
                columns: new[] { "organization_id", "event_id", "last_submission_id" },
                principalTable: "rsvp_submissions",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_transport_selections_event_guests_organization_id_eve",
                table: "guest_transport_selections",
                columns: new[] { "organization_id", "event_id", "event_guest_id" },
                principalTable: "event_guests",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_transport_selections_event_transport_options_organiza",
                table: "guest_transport_selections",
                columns: new[] { "organization_id", "event_id", "event_transport_option_id" },
                principalTable: "event_transport_options",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_transport_selections_rsvp_submissions_organization_id",
                table: "guest_transport_selections",
                columns: new[] { "organization_id", "event_id", "last_submission_id" },
                principalTable: "rsvp_submissions",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_rsvp_submissions_rsvp_submissions_organization_id_event_id_",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "event_id", "previous_submission_id" },
                principalTable: "rsvp_submissions",
                principalColumns: new[] { "organization_id", "event_id", "id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_guest_accommodation_selections_event_accommodation_options_",
                table: "guest_accommodation_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_accommodation_selections_event_guests_organization_id",
                table: "guest_accommodation_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_accommodation_selections_invitation_groups_organizati",
                table: "guest_accommodation_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_accommodation_selections_rsvp_submissions_organizatio",
                table: "guest_accommodation_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_dietary_accessibility_rsvp_submissions_organization_i",
                table: "guest_dietary_accessibility");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_transport_selections_event_guests_organization_id_eve",
                table: "guest_transport_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_transport_selections_event_transport_options_organiza",
                table: "guest_transport_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_guest_transport_selections_rsvp_submissions_organization_id",
                table: "guest_transport_selections");

            migrationBuilder.DropForeignKey(
                name: "fk_rsvp_submissions_rsvp_submissions_organization_id_event_id_",
                table: "rsvp_submissions");

            migrationBuilder.DropTable(
                name: "guest_transport_selection_history");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submissions_organization_id_event_id_previous_submissi",
                table: "rsvp_submissions");

            migrationBuilder.DropIndex(
                name: "ux_rsvp_submissions_idempotency",
                table: "rsvp_submissions");

            migrationBuilder.DropIndex(
                name: "ux_rsvp_submissions_revision",
                table: "rsvp_submissions");

            migrationBuilder.DropPrimaryKey(
                name: "pk_rsvp_submission_guests",
                table: "rsvp_submission_guests");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submission_guests_rsvp_submission_id_companion_slot_nu",
                table: "rsvp_submission_guests");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submission_guests_rsvp_submission_id_event_guest_id",
                table: "rsvp_submission_guests");

            migrationBuilder.DropPrimaryKey(
                name: "pk_rsvp_submission_answers",
                table: "rsvp_submission_answers");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submission_answers_rsvp_submission_id_question_id_gues",
                table: "rsvp_submission_answers");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_group_exceptions_organization_id_event_id_invitation_g",
                table: "rsvp_group_exceptions");

            migrationBuilder.DropIndex(
                name: "ix_guest_transport_selections_organization_id_event_id_event_g",
                table: "guest_transport_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_transport_selections_organization_id_event_id_event_t",
                table: "guest_transport_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_transport_selections_organization_id_event_id_last_su",
                table: "guest_transport_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_dietary_accessibility_organization_id_event_id_last_s",
                table: "guest_dietary_accessibility");

            migrationBuilder.DropIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_eve",
                table: "guest_accommodation_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_eve1",
                table: "guest_accommodation_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_inv",
                table: "guest_accommodation_selections");

            migrationBuilder.DropIndex(
                name: "ix_guest_accommodation_selections_organization_id_event_id_las",
                table: "guest_accommodation_selections");

            migrationBuilder.DropColumn(
                name: "request_fingerprint",
                table: "rsvp_submissions");

            migrationBuilder.DropColumn(
                name: "id",
                table: "rsvp_submission_guests");

            migrationBuilder.DropColumn(
                name: "companion_slot_number",
                table: "rsvp_submission_guests");

            migrationBuilder.DropColumn(
                name: "id",
                table: "rsvp_submission_answers");

            migrationBuilder.DropColumn(
                name: "closed_by",
                table: "rsvp_group_exceptions");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "guest_transport_selections");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "guest_transport_selections");

            migrationBuilder.DropColumn(
                name: "requested_at",
                table: "guest_transport_selections");

            migrationBuilder.DropColumn(
                name: "waitlist_sequence",
                table: "guest_transport_selections");

            migrationBuilder.DropColumn(
                name: "event_id",
                table: "guest_accommodation_selections");

            migrationBuilder.DropColumn(
                name: "organization_id",
                table: "guest_accommodation_selections");

            migrationBuilder.AlterColumn<Guid>(
                name: "event_guest_id",
                table: "rsvp_submission_guests",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "guest_id",
                table: "rsvp_submission_answers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "pk_rsvp_submission_guests",
                table: "rsvp_submission_guests",
                columns: new[] { "rsvp_submission_id", "event_guest_id" });

            migrationBuilder.AddPrimaryKey(
                name: "pk_rsvp_submission_answers",
                table: "rsvp_submission_answers",
                columns: new[] { "rsvp_submission_id", "question_id", "guest_id" });

            migrationBuilder.CreateTable(
                name: "guest_access_token_keys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    activated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    key_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    organization_id = table.Column<Guid>(type: "uuid", nullable: false),
                    retired_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_idempotency_key",
                table: "rsvp_submissions",
                column: "idempotency_key");

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submissions_organization_id_event_id_invitation_group_",
                table: "rsvp_submissions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_group_exceptions_organization_id_event_id_invitation_g",
                table: "rsvp_group_exceptions",
                columns: new[] { "organization_id", "event_id", "invitation_group_id", "status" },
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "ix_guest_transport_selections_event_transport_option_id",
                table: "guest_transport_selections",
                column: "event_transport_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_event_accommodation_option_id",
                table: "guest_accommodation_selections",
                column: "event_accommodation_option_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_accommodation_selections_event_guest_id",
                table: "guest_accommodation_selections",
                column: "event_guest_id");

            migrationBuilder.CreateIndex(
                name: "ix_guest_access_token_keys_key_id",
                table: "guest_access_token_keys",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_guest_access_token_keys_organization_id",
                table: "guest_access_token_keys",
                column: "organization_id");

            migrationBuilder.AddForeignKey(
                name: "fk_guest_accommodation_selections_event_accommodation_options_",
                table: "guest_accommodation_selections",
                column: "event_accommodation_option_id",
                principalTable: "event_accommodation_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_accommodation_selections_event_guests_event_guest_id",
                table: "guest_accommodation_selections",
                column: "event_guest_id",
                principalTable: "event_guests",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_transport_selections_event_guests_event_guest_id",
                table: "guest_transport_selections",
                column: "event_guest_id",
                principalTable: "event_guests",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_guest_transport_selections_event_transport_options_event_tr",
                table: "guest_transport_selections",
                column: "event_transport_option_id",
                principalTable: "event_transport_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
