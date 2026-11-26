using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ClassChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Instant>(
                name: "CancelledAt",
                schema: "booking",
                table: "class_sessions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "booking",
                table: "class_sessions",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                // Every existing class is still on (D-068).
                defaultValue: "Scheduled");

            migrationBuilder.AddColumn<Instant>(
                name: "ClassMovedAt",
                schema: "booking",
                table: "bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_class_types_Name",
                schema: "booking",
                table: "class_types",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_class_types_Name",
                schema: "booking",
                table: "class_types");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "booking",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "booking",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "ClassMovedAt",
                schema: "booking",
                table: "bookings");
        }
    }
}
