using Microsoft.EntityFrameworkCore.Migrations;
using NodaTime;

#nullable disable

namespace Members.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MemberDateOfBirth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<LocalDate>(
                name: "DateOfBirth",
                schema: "members",
                table: "members",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateOfBirth",
                schema: "members",
                table: "members");
        }
    }
}
