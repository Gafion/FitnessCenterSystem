using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Waitlist : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "waitlist_entries",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    JoinedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    LeftAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_waitlist_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_waitlist_entries_class_sessions_ClassSessionId",
                        column: x => x.ClassSessionId,
                        principalSchema: "booking",
                        principalTable: "class_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_waitlist_entries_AccountId",
                schema: "booking",
                table: "waitlist_entries",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_waitlist_entries_ClassSessionId_AccountId",
                schema: "booking",
                table: "waitlist_entries",
                columns: new[] { "ClassSessionId", "AccountId" },
                unique: true,
                filter: "\"Status\" = 'Waiting'");

            migrationBuilder.CreateIndex(
                name: "IX_waitlist_entries_ClassSessionId_Number",
                schema: "booking",
                table: "waitlist_entries",
                columns: new[] { "ClassSessionId", "Number" },
                unique: true);

            // Added by hand, like bookings in InitialBooking: a waitlist entry belongs to an
            // Identity account (D-039, D-051). Integrity only; Booking never reads Identity's
            // tables (rule 3).
            migrationBuilder.AddForeignKey(
                name: "FK_waitlist_entries_identity_AspNetUsers_AccountId",
                schema: "booking",
                table: "waitlist_entries",
                column: "AccountId",
                principalSchema: "identity",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_waitlist_entries_identity_AspNetUsers_AccountId",
                schema: "booking",
                table: "waitlist_entries");

            migrationBuilder.DropTable(
                name: "waitlist_entries",
                schema: "booking");
        }
    }
}
