using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Digdir.Domain.Dialogporten.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PartialTbhIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_ActivityId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_AttachmentId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_DialogContentId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_GuiActionId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_NavigationalActionId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_TransmissionContentId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_DialogId",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_TransmissionId",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActivityId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActorNameEntityId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActorTypeId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_DialogSeenLogId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_LabelAssignmentLogId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_TransmissionId",
                table: "Actor");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_ActivityId",
                table: "LocalizationSet",
                column: "ActivityId",
                unique: true,
                filter: "\"ActivityId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_AttachmentId",
                table: "LocalizationSet",
                column: "AttachmentId",
                unique: true,
                filter: "\"AttachmentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_DialogContentId",
                table: "LocalizationSet",
                column: "DialogContentId",
                unique: true,
                filter: "\"DialogContentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId",
                table: "LocalizationSet",
                column: "DialogGuiActionPrompt_GuiActionId",
                unique: true,
                filter: "\"DialogGuiActionPrompt_GuiActionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_GuiActionId",
                table: "LocalizationSet",
                column: "GuiActionId",
                unique: true,
                filter: "\"GuiActionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_NavigationalActionId",
                table: "LocalizationSet",
                column: "NavigationalActionId",
                unique: true,
                filter: "\"NavigationalActionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_TransmissionContentId",
                table: "LocalizationSet",
                column: "TransmissionContentId",
                unique: true,
                filter: "\"TransmissionContentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_DialogId",
                table: "Attachment",
                column: "DialogId",
                filter: "\"DialogId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_TransmissionId",
                table: "Attachment",
                column: "TransmissionId",
                filter: "\"TransmissionId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActivityId",
                table: "Actor",
                column: "ActivityId",
                unique: true,
                filter: "\"ActivityId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActorNameEntityId",
                table: "Actor",
                column: "ActorNameEntityId",
                filter: "\"ActorNameEntityId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActorTypeId",
                table: "Actor",
                column: "ActorTypeId",
                filter: "\"ActorTypeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_DialogSeenLogId",
                table: "Actor",
                column: "DialogSeenLogId",
                unique: true,
                filter: "\"DialogSeenLogId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_LabelAssignmentLogId",
                table: "Actor",
                column: "LabelAssignmentLogId",
                unique: true,
                filter: "\"LabelAssignmentLogId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_TransmissionId",
                table: "Actor",
                column: "TransmissionId",
                unique: true,
                filter: "\"TransmissionId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_ActivityId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_AttachmentId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_DialogContentId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_GuiActionId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_NavigationalActionId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_LocalizationSet_TransmissionContentId",
                table: "LocalizationSet");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_DialogId",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_Attachment_TransmissionId",
                table: "Attachment");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActivityId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActorNameEntityId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_ActorTypeId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_DialogSeenLogId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_LabelAssignmentLogId",
                table: "Actor");

            migrationBuilder.DropIndex(
                name: "IX_Actor_TransmissionId",
                table: "Actor");

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_ActivityId",
                table: "LocalizationSet",
                column: "ActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_AttachmentId",
                table: "LocalizationSet",
                column: "AttachmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_DialogContentId",
                table: "LocalizationSet",
                column: "DialogContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_DialogGuiActionPrompt_GuiActionId",
                table: "LocalizationSet",
                column: "DialogGuiActionPrompt_GuiActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_GuiActionId",
                table: "LocalizationSet",
                column: "GuiActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_NavigationalActionId",
                table: "LocalizationSet",
                column: "NavigationalActionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LocalizationSet_TransmissionContentId",
                table: "LocalizationSet",
                column: "TransmissionContentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_DialogId",
                table: "Attachment",
                column: "DialogId");

            migrationBuilder.CreateIndex(
                name: "IX_Attachment_TransmissionId",
                table: "Attachment",
                column: "TransmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActivityId",
                table: "Actor",
                column: "ActivityId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActorNameEntityId",
                table: "Actor",
                column: "ActorNameEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_ActorTypeId",
                table: "Actor",
                column: "ActorTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Actor_DialogSeenLogId",
                table: "Actor",
                column: "DialogSeenLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actor_LabelAssignmentLogId",
                table: "Actor",
                column: "LabelAssignmentLogId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Actor_TransmissionId",
                table: "Actor",
                column: "TransmissionId",
                unique: true);
        }
    }
}
