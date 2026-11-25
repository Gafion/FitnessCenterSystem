using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InstructorAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added by hand (D-060): every class now needs an instructor account, and the free-text
            // name can't be turned into one. The only classes so far are the synthetic development
            // schedule (D-022, D-047), so they go, with their bookings and waitlist entries; the
            // development seed schedules them again for the development instructor (D-049).
            migrationBuilder.Sql("""
                DELETE FROM booking.waitlist_entries;
                DELETE FROM booking.bookings;
                DELETE FROM booking.class_sessions;
                """);

            migrationBuilder.DropColumn(
                name: "InstructorName",
                schema: "booking",
                table: "class_sessions");

            migrationBuilder.AddColumn<Guid>(
                name: "InstructorAccountId",
                schema: "booking",
                table: "class_sessions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_InstructorAccountId",
                schema: "booking",
                table: "class_sessions",
                column: "InstructorAccountId");

            // Added by hand: a class is taught by an Identity account (D-060), which only Identity
            // maps. Integrity only, as for bookings (D-039); Booking never reads Identity's tables
            // (rule 3). Identity's migrations run first (DatabaseExtensions.MigrateDatabaseAsync).
            migrationBuilder.AddForeignKey(
                name: "FK_class_sessions_identity_AspNetUsers_InstructorAccountId",
                schema: "booking",
                table: "class_sessions",
                column: "InstructorAccountId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_class_sessions_identity_AspNetUsers_InstructorAccountId",
                schema: "booking",
                table: "class_sessions");

            migrationBuilder.DropIndex(
                name: "IX_class_sessions_InstructorAccountId",
                schema: "booking",
                table: "class_sessions");

            migrationBuilder.DropColumn(
                name: "InstructorAccountId",
                schema: "booking",
                table: "class_sessions");

            migrationBuilder.AddColumn<string>(
                name: "InstructorName",
                schema: "booking",
                table: "class_sessions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
