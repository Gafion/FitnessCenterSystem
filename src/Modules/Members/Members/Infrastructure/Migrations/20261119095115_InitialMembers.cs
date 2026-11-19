using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Members.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "members");

            migrationBuilder.CreateTable(
                name: "membership_types",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MonthlyPriceOre = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_membership_types", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "members",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    MembershipTypeId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberSince = table.Column<LocalDate>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PaidUntil = table.Column<LocalDate>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_members_membership_types_MembershipTypeId",
                        column: x => x.MembershipTypeId,
                        principalSchema: "members",
                        principalTable: "membership_types",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "consents",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    TextVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GivenAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    Channel = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_consents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_consents_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "members",
                table: "membership_types",
                columns: new[] { "Id", "MonthlyPriceOre", "Name" },
                values: new object[] { new Guid("0199a3c0-0000-7000-8000-000000000001"), 29900L, "General membership" });

            migrationBuilder.CreateIndex(
                name: "IX_consents_MemberId",
                schema: "members",
                table: "consents",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_members_AccountId",
                schema: "members",
                table: "members",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_members_MembershipTypeId",
                schema: "members",
                table: "members",
                column: "MembershipTypeId");

            // Cross-module FK for integrity only (D-014, rule 3): a member always has an
            // Identity account. Not in the EF model, because Members never maps Identity's tables.
            // Identity's migrations run first (DatabaseExtensions.MigrateDatabaseAsync).
            migrationBuilder.AddForeignKey(
                name: "FK_members_identity_AspNetUsers_AccountId",
                schema: "members",
                table: "members",
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
                name: "FK_members_identity_AspNetUsers_AccountId",
                schema: "members",
                table: "members");

            migrationBuilder.DropTable(
                name: "consents",
                schema: "members");

            migrationBuilder.DropTable(
                name: "members",
                schema: "members");

            migrationBuilder.DropTable(
                name: "membership_types",
                schema: "members");
        }
    }
}
