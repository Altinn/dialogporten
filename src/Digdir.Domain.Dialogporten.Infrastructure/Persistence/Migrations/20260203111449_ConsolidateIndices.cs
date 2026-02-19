using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConsolidateIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Leftover from previous migration squash; not part of the current configuration.
            migrationBuilder.DropIndex(
                name: "IX_Dialog_Deleted",
                table: "Dialog");

            // Leftover from previous migration squash; not part of the current configuration.
            migrationBuilder.DropIndex(
                name: "IX_Dialog_StatusId",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Localization_Value",
                table: "Localization");

            migrationBuilder.DropIndex(
                name: "IX_DialogSearchTag_Value",
                table: "DialogSearchTag");

            migrationBuilder.DropIndex(
                name: "IX_DialogSearch_SearchVector",
                schema: "search",
                table: "DialogSearch");

            migrationBuilder.DropIndex(
                name: "IX_DialogActivity_DialogId",
                table: "DialogActivity");

            migrationBuilder.DropIndex(
                name: "IX_DialogActivity_TransmissionId",
                table: "DialogActivity");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ContentUpdatedAt",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_CreatedAt",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_DueAt",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ExtendedStatus",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_ExternalReference",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_IsApiOnly",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Org",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Process",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_UpdatedAt",
                table: "Dialog");

            migrationBuilder.CreateIndex(
                name: "IX_DialogActivity_DialogId_CreatedAt_Id",
                table: "DialogActivity",
                columns: new[] { "DialogId", "CreatedAt", "Id" },
                descending: new[] { false, true, true });

            migrationBuilder.CreateIndex(
                name: "IX_DialogActivity_TransmissionId_TypeId",
                table: "DialogActivity",
                columns: new[] { "TransmissionId", "TypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog",
                column: "Id")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "Deleted", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt", "ContentUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true },
                filter: "\"Deleted\" = false")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DialogActivity_DialogId_CreatedAt_Id",
                table: "DialogActivity");

            migrationBuilder.DropIndex(
                name: "IX_DialogActivity_TransmissionId_TypeId",
                table: "DialogActivity");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog");

            migrationBuilder.DropIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog");

            migrationBuilder.CreateIndex(
                name: "IX_Localization_Value",
                table: "Localization",
                column: "Value")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_DialogSearchTag_Value",
                table: "DialogSearchTag",
                column: "Value")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_DialogSearch_SearchVector",
                schema: "search",
                table: "DialogSearch",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_DialogActivity_DialogId",
                table: "DialogActivity",
                column: "DialogId");

            migrationBuilder.CreateIndex(
                name: "IX_DialogActivity_TransmissionId",
                table: "DialogActivity",
                column: "TransmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ContentUpdatedAt",
                table: "Dialog",
                column: "ContentUpdatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_CreatedAt",
                table: "Dialog",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_DueAt",
                table: "Dialog",
                column: "DueAt");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ExtendedStatus",
                table: "Dialog",
                column: "ExtendedStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_ExternalReference",
                table: "Dialog",
                column: "ExternalReference");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Id_Covering",
                table: "Dialog",
                column: "Id")
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt", "ContentUpdatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_IsApiOnly",
                table: "Dialog",
                column: "IsApiOnly");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Org",
                table: "Dialog",
                column: "Org");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party",
                table: "Dialog",
                column: "Party");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Party_ContentUpdatedAt_Id_Covering",
                table: "Dialog",
                columns: new[] { "Party", "ContentUpdatedAt", "Id" },
                descending: new[] { false, true, true })
                .Annotation("Npgsql:IndexInclude", new[] { "ServiceResource", "IsApiOnly", "StatusId", "Org", "VisibleFrom", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_Process",
                table: "Dialog",
                column: "Process");

            migrationBuilder.CreateIndex(
                name: "IX_Dialog_UpdatedAt",
                table: "Dialog",
                column: "UpdatedAt");
        }
    }
}
