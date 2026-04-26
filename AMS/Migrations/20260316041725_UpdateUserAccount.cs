using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "UserType",
                table: "UserAccounts",
                type: "smallint",
                nullable: false,
                defaultValue: (byte)0);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Classes",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Classes_UserId",
                table: "Classes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Classes_UserAccounts_UserId",
                table: "Classes",
                column: "UserId",
                principalTable: "UserAccounts",
                principalColumn: "Oid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Classes_UserAccounts_UserId",
                table: "Classes");

            migrationBuilder.DropIndex(
                name: "IX_Classes_UserId",
                table: "Classes");

            migrationBuilder.DropColumn(
                name: "UserType",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Classes");
        }
    }
}
