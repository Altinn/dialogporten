using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDialogPartyChecks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Hand-edited: EF scaffolds a '{}' default, which is not a valid collection, and cannot express the
            // backfill. IncludeDialogParty is kept (now with a default, as this version no longer writes it) so
            // pods on the previous version keep working during the rolling deploy; a follow-up drops it.
            migrationBuilder.AlterColumn<bool>(
                name: "IncludeDialogParty",
                table: "AuthorizationContext",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<string>(
                name: "DialogPartyChecks",
                table: "AuthorizationContext",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            // true -> one unrestricted dialog party check, false -> no checks.
            migrationBuilder.Sql("""
                UPDATE "AuthorizationContext"
                SET "DialogPartyChecks" = '[{"AccessRestriction": 1, "Action": null, "ServiceResource": null, "ViaParty": null}]'
                WHERE "IncludeDialogParty";
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Carry contexts written by this version back into IncludeDialogParty. Lossy: only unrestricted
            // dialog party checks without their own right can be expressed.
            migrationBuilder.Sql("""
                UPDATE "AuthorizationContext"
                SET "IncludeDialogParty" = true
                WHERE NOT "IncludeDialogParty"
                  AND EXISTS (
                    SELECT 1 FROM jsonb_array_elements("DialogPartyChecks") AS c
                    WHERE (c ->> 'AccessRestriction')::int = 1
                      AND c ->> 'ServiceResource' IS NULL
                      AND c ->> 'Action' IS NULL);
                """);

            migrationBuilder.DropColumn(
                name: "DialogPartyChecks",
                table: "AuthorizationContext");

            migrationBuilder.AlterColumn<bool>(
                name: "IncludeDialogParty",
                table: "AuthorizationContext",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);
        }
    }
}
