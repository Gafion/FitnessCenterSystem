using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Members.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PaymentRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "payment_records",
                schema: "members",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AmountOre = table.Column<long>(type: "bigint", nullable: false),
                    PaidAt = table.Column<Instant>(type: "timestamp with time zone", nullable: false),
                    CardLast4 = table.Column<string>(type: "character(4)", fixedLength: true, maxLength: 4, nullable: false),
                    PeriodFrom = table.Column<LocalDate>(type: "date", nullable: false),
                    PeriodTo = table.Column<LocalDate>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payment_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payment_records_members_MemberId",
                        column: x => x.MemberId,
                        principalSchema: "members",
                        principalTable: "members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_MemberId",
                schema: "members",
                table: "payment_records",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_payment_records_PaymentRef",
                schema: "members",
                table: "payment_records",
                column: "PaymentRef",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payment_records",
                schema: "members");
        }
    }
}
