using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class WaitlistOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_waitlist_entries_ClassSessionId_AccountId",
                schema: "booking",
                table: "waitlist_entries");

            migrationBuilder.AddColumn<Instant>(
                name: "OfferExpiresAt",
                schema: "booking",
                table: "waitlist_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "OfferedAt",
                schema: "booking",
                table: "waitlist_entries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_waitlist_entries_ClassSessionId_AccountId",
                schema: "booking",
                table: "waitlist_entries",
                columns: new[] { "ClassSessionId", "AccountId" },
                unique: true,
                filter: "\"Status\" IN ('Waiting', 'Offered')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_waitlist_entries_ClassSessionId_AccountId",
                schema: "booking",
                table: "waitlist_entries");

            migrationBuilder.DropColumn(
                name: "OfferExpiresAt",
                schema: "booking",
                table: "waitlist_entries");

            migrationBuilder.DropColumn(
                name: "OfferedAt",
                schema: "booking",
                table: "waitlist_entries");

            migrationBuilder.CreateIndex(
                name: "IX_waitlist_entries_ClassSessionId_AccountId",
                schema: "booking",
                table: "waitlist_entries",
                columns: new[] { "ClassSessionId", "AccountId" },
                unique: true,
                filter: "\"Status\" = 'Waiting'");
        }
    }
}
