using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Medc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AIModification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AIChatSessions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AIChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "AIChatMessages",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "AIChatMessages",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TokensUsed",
                table: "AIChatMessages",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIChatSessions_PatientId_IsDeleted",
                table: "AIChatSessions",
                columns: new[] { "PatientId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessages_SessionId_IsDeleted",
                table: "AIChatMessages",
                columns: new[] { "SessionId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_AIChatMessageAttachments_StoragePath",
                table: "AIChatMessageAttachments",
                column: "StoragePath",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIChatSessions_PatientId_IsDeleted",
                table: "AIChatSessions");

            migrationBuilder.DropIndex(
                name: "IX_AIChatMessages_SessionId_IsDeleted",
                table: "AIChatMessages");

            migrationBuilder.DropIndex(
                name: "IX_AIChatMessageAttachments_StoragePath",
                table: "AIChatMessageAttachments");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AIChatSessions");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AIChatSessions");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "AIChatMessages");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "AIChatMessages");

            migrationBuilder.DropColumn(
                name: "TokensUsed",
                table: "AIChatMessages");
        }
    }
}
