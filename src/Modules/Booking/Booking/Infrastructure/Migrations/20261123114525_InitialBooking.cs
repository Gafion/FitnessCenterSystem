using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.CreateTable(
                name: "class_types",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "class_sessions",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstructorName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartsAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    EndsAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_class_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_class_sessions_class_types_ClassTypeId",
                        column: x => x.ClassTypeId,
                        principalSchema: "booking",
                        principalTable: "class_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ClassSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    BookedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CancelledAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bookings_class_sessions_ClassSessionId",
                        column: x => x.ClassSessionId,
                        principalSchema: "booking",
                        principalTable: "class_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "booking",
                table: "class_types",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { new Guid("0199a3c0-0000-7000-8000-000000000101"), "Spinning" },
                    { new Guid("0199a3c0-0000-7000-8000-000000000102"), "CrossFit" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_bookings_AccountId",
                schema: "booking",
                table: "bookings",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_bookings_ClassSessionId_AccountId",
                schema: "booking",
                table: "bookings",
                columns: new[] { "ClassSessionId", "AccountId" },
                unique: true,
                filter: "\"Status\" = 'Booked'");

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_ClassTypeId",
                schema: "booking",
                table: "class_sessions",
                column: "ClassTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_class_sessions_StartsAt",
                schema: "booking",
                table: "class_sessions",
                column: "StartsAt");

            // Added by hand: a booking belongs to an Identity account (D-039), which only Identity
            // maps. Integrity only; Booking never reads Identity's tables (rule 3). Identity's
            // migrations run first (DatabaseExtensions.MigrateDatabaseAsync).
            migrationBuilder.AddForeignKey(
                name: "FK_bookings_identity_AspNetUsers_AccountId",
                schema: "booking",
                table: "bookings",
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
                name: "FK_bookings_identity_AspNetUsers_AccountId",
                schema: "booking",
                table: "bookings");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "class_sessions",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "class_types",
                schema: "booking");
        }
    }
}
