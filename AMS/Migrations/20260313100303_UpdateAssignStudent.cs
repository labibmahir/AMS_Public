using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAssignStudent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignedStudents_ClassSchedules_ClassScheduleId",
                table: "AssignedStudents");

            migrationBuilder.DropIndex(
                name: "IX_AssignedStudents_ClassScheduleId",
                table: "AssignedStudents");

            migrationBuilder.DropColumn(
                name: "ClassScheduleId",
                table: "AssignedStudents");

            migrationBuilder.AddColumn<int>(
                name: "ClassId",
                table: "AssignedStudents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AssignedStudents_ClassId",
                table: "AssignedStudents",
                column: "ClassId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignedStudents_Classes_ClassId",
                table: "AssignedStudents",
                column: "ClassId",
                principalTable: "Classes",
                principalColumn: "Oid",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AssignedStudents_Classes_ClassId",
                table: "AssignedStudents");

            migrationBuilder.DropIndex(
                name: "IX_AssignedStudents_ClassId",
                table: "AssignedStudents");

            migrationBuilder.DropColumn(
                name: "ClassId",
                table: "AssignedStudents");

            migrationBuilder.AddColumn<Guid>(
                name: "ClassScheduleId",
                table: "AssignedStudents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AssignedStudents_ClassScheduleId",
                table: "AssignedStudents",
                column: "ClassScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AssignedStudents_ClassSchedules_ClassScheduleId",
                table: "AssignedStudents",
                column: "ClassScheduleId",
                principalTable: "ClassSchedules",
                principalColumn: "Oid",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
