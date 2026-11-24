using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Members.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CancellationHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "cancellations",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    CancelledAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    EffectiveOn = table.Column<LocalDate>(type: "date", nullable: false),
                    UndoneAt = table.Column<Instant>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cancellations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cancellations_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_cancellations_MemberId",
                schema: "members",
                table: "cancellations",
                column: "MemberId");

            // Cancellations made before the history existed (FIT-70) become its first records.
            migrationBuilder.Sql(
                """
                INSERT INTO members.cancellations ("Id", "MemberId", "CancelledAt", "EffectiveOn")
                SELECT gen_random_uuid(), "Id", "CancelledAt", "CancellationEffectiveOn"
                FROM members.members
                WHERE "CancelledAt" IS NOT NULL AND "CancellationEffectiveOn" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cancellations",
                schema: "members");
        }
    }
}
