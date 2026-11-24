using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Members.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MemberCancellation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<LocalDate>(
                name: "CancellationEffectiveOn",
                schema: "members",
                table: "members",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<Instant>(
                name: "CancelledAt",
                schema: "members",
                table: "members",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationEffectiveOn",
                schema: "members",
                table: "members");

            migrationBuilder.DropColumn(
                name: "CancelledAt",
                schema: "members",
                table: "members");
        }
    }
}
