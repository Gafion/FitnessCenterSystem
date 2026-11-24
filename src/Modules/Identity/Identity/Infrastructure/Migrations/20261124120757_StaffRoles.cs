using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class StaffRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "identity",
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a01"), "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a01", "Admin", "ADMIN" },
                    { new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a02"), "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a02", "Manager", "MANAGER" },
                    { new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a03"), "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a03", "FrontDesk", "FRONTDESK" },
                    { new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a04"), "0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a04", "Instructor", "INSTRUCTOR" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a01"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a02"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a03"));

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("0b9c1a52-3a0e-4d8e-9f0c-2a5e6c1d7a04"));
        }
    }
}
