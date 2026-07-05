using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Smart_Medc.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AIChatSessionModif : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IncludeCurrentMedications",
                table: "AIChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeJournalEntries",
                table: "AIChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncludeMedicalRecords",
                table: "AIChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IncludePastMedications",
                table: "AIChatSessions",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IncludeCurrentMedications",
                table: "AIChatSessions");

            migrationBuilder.DropColumn(
                name: "IncludeJournalEntries",
                table: "AIChatSessions");

            migrationBuilder.DropColumn(
                name: "IncludeMedicalRecords",
                table: "AIChatSessions");

            migrationBuilder.DropColumn(
                name: "IncludePastMedications",
                table: "AIChatSessions");
        }
    }
}
