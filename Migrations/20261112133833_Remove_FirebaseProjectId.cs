using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BuildingManagementMvc.Migrations
{
    /// <inheritdoc />
    public partial class Remove_FirebaseProjectId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. حذف FirebaseProjectId من GlobalSettings
            migrationBuilder.DropColumn(
                name: "FirebaseProjectId",
                table: "GlobalSettings");

            // 2. جدول Residents موجود بالفعل في قاعدة البيانات (تم إنشاؤه سابقاً)
            // لذلك تم تخطي إنشاء الجدول لتجنب خطأ التكرار.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirebaseProjectId",
                table: "GlobalSettings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}