using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AMS.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMigrationScheduleTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ScheduleTime",
                table: "ClassSchedules",
                newName: "StartScheduleTime");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndScheduleTime",
                table: "ClassSchedules",
                type: "interval",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EndScheduleTime",
                table: "ClassSchedules");

            migrationBuilder.RenameColumn(
                name: "StartScheduleTime",
                table: "ClassSchedules",
                newName: "ScheduleTime");
        }
    }
}
