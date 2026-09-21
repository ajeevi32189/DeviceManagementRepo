using System;
using DeviceManagement.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeviceManagementOnly.Migrations
{
    // NOTE: is project mein har migration ka [Migration] attribute normally
    // uske alag "<Name>.Designer.cs" file mein hota hai, jo "dotnet ef migrations add"
    // khud generate karta hai. Yahan dotnet SDK available nahi tha isliye attribute
    // seedha is class pe laga diya hai — EF Core ke liye yeh 100% valid hai
    // (Designer.cs sirf future scaffolding-diff ke liye tooling convenience hai,
    // runtime apply ke liye sirf [Migration("id")] attribute hi chahiye hota hai).
    [DbContext(typeof(DBContext))]
    [Migration("20260821060000_AddRoomGeometryFields")]
    public partial class AddRoomGeometryFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PositionX",
                table: "Rooms",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PositionY",
                table: "Rooms",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Width",
                table: "Rooms",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Length",
                table: "Rooms",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RotationAngle",
                table: "Rooms",
                type: "decimal(65,30)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "PositionX", table: "Rooms");
            migrationBuilder.DropColumn(name: "PositionY", table: "Rooms");
            migrationBuilder.DropColumn(name: "Width", table: "Rooms");
            migrationBuilder.DropColumn(name: "Length", table: "Rooms");
            migrationBuilder.DropColumn(name: "RotationAngle", table: "Rooms");
        }
    }
}
