using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AllowPlatformAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "audit_entries",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddCheckConstraint(
                name: "ck_audit_entries_event_context",
                table: "audit_entries",
                sql: "event_id IS NULL OR organization_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_audit_entries_event_context",
                table: "audit_entries");

            migrationBuilder.AlterColumn<Guid>(
                name: "organization_id",
                table: "audit_entries",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
