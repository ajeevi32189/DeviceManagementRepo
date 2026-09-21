using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeviceManagementOnly.Migrations
{
    /// <inheritdoc />
    public partial class updatedevicelocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "UnitMasters",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "UnitMasters",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "ModelCategories",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "ModelCategories",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "EntityTypes",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "EntityTypes",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "DocumentTypes",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DocumentTypes",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "DocumentIssuedBies",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DocumentIssuedBies",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "DeviceTypes",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DeviceTypes",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "CreatedAt",
            //    table: "DeviceModelMappings",
            //    type: "datetime(6)",
            //    nullable: false,
            //    defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "DeviceModelMappings",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<DateTime>(
            //    name: "UpdatedAt",
            //    table: "DeviceModelMappings",
            //    type: "datetime(6)",
            //    nullable: true);

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DeviceModelMappings",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DeviceMasters",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "DeviceDetails",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DeviceDetails",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "CreatedBy",
            //    table: "DeviceCategories",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.AddColumn<string>(
            //    name: "UpdatedBy",
            //    table: "DeviceCategories",
            //    type: "longtext",
            //    nullable: true)
            //    .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeviceLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DeviceDetailId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LocationType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddressLine1 = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddressLine2 = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CountryId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    StateId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CityId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PinCode = table.Column<int>(type: "int", nullable: true),
                    Landmark = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Latitude = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    TimeZoneId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    LocationContactNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntityType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLocations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            //migrationBuilder.UpdateData(
            //    table: "EntityTypes",
            //    keyColumn: "Id",
            //    keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            //    columns: new[] { "CreatedBy", "UpdatedBy" },
            //    values: new object[] { null, null });

            //migrationBuilder.UpdateData(
            //    table: "EntityTypes",
            //    keyColumn: "Id",
            //    keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            //    columns: new[] { "CreatedBy", "UpdatedBy" },
            //    values: new object[] { null, null });

            //migrationBuilder.UpdateData(
            //    table: "EntityTypes",
            //    keyColumn: "Id",
            //    keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            //    columns: new[] { "CreatedBy", "UpdatedBy" },
            //    values: new object[] { null, null });

            //migrationBuilder.UpdateData(
            //    table: "EntityTypes",
            //    keyColumn: "Id",
            //    keyValue: new Guid("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            //    columns: new[] { "CreatedBy", "UpdatedBy" },
            //    values: new object[] { null, null });

            //migrationBuilder.UpdateData(
            //    table: "EntityTypes",
            //    keyColumn: "Id",
            //    keyValue: new Guid("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            //    columns: new[] { "CreatedBy", "UpdatedBy" },
            //    values: new object[] { null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DeviceLocations");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "UnitMasters");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "UnitMasters");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "ModelCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "ModelCategories");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "EntityTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "EntityTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DocumentTypes");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DocumentIssuedBies");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DocumentIssuedBies");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "DeviceModelMappings");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DeviceModelMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DeviceModelMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DeviceModelMappings");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DeviceMasters");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "DeviceCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "DeviceCategories");
        }
    }
}
