using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Access.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "access");

            migrationBuilder.CreateTable(
                name: "access_cards",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Uid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_access_cards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "attendances",
                schema: "access",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessCardId = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    SwipedAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_attendances_access_cards_AccessCardId",
                        column: x => x.AccessCardId,
                        principalSchema: "access",
                        principalTable: "access_cards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_access_cards_MemberId",
                schema: "access",
                table: "access_cards",
                column: "MemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_access_cards_Uid",
                schema: "access",
                table: "access_cards",
                column: "Uid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_attendances_AccessCardId",
                schema: "access",
                table: "attendances",
                column: "AccessCardId");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_MemberId_SwipedAt",
                schema: "access",
                table: "attendances",
                columns: new[] { "MemberId", "SwipedAt" });

            // Added by hand: cards and attendances belong to a member (D-003), which only Members
            // maps. Integrity only; Access never reads Members' tables (rule 3). Members' migrations
            // run first (DatabaseExtensions.MigrateDatabaseAsync).
            migrationBuilder.AddForeignKey(
                name: "FK_access_cards_members_members_MemberId",
                schema: "access",
                table: "access_cards",
                column: "MemberId",
                principalSchema: "members",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_attendances_members_members_MemberId",
                schema: "access",
                table: "attendances",
                column: "MemberId",
                principalSchema: "members",
                principalTable: "members",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_attendances_members_members_MemberId",
                schema: "access",
                table: "attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_access_cards_members_members_MemberId",
                schema: "access",
                table: "access_cards");

            migrationBuilder.DropTable(
                name: "attendances",
                schema: "access");

            migrationBuilder.DropTable(
                name: "access_cards",
                schema: "access");
        }
    }
}
