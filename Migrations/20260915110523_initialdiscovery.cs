using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeviceManagementOnly.Migrations
{
    /// <inheritdoc />
    public partial class initialdiscovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BacnetDeviceId",
                table: "DeviceDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiscoveryMessage",
                table: "DeviceDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DiscoveryStatus",
                table: "DeviceDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GatewayIPAddress",
                table: "DeviceDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDiscoveredAt",
                table: "DeviceDetails",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "DeviceDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SnmpCommunity",
                table: "DeviceDetails",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "UnitId",
                table: "DeviceDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "modelprotocolprofiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ModelSpecificationId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Protocol = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultPort = table.Column<int>(type: "int", nullable: true),
                    DefaultUnitId = table.Column<int>(type: "int", nullable: true),
                    FunctionCode = table.Column<int>(type: "int", nullable: true),
                    WordOrder = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, defaultValue: "BIG")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ByteOrder = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true, defaultValue: "BIG")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddressBase = table.Column<byte>(type: "tinyint unsigned", nullable: true, defaultValue: (byte)0),
                    PollPeriodMs = table.Column<int>(type: "int", nullable: true, defaultValue: 10000),
                    Source = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsVerified = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    VerifiedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelprotocolprofiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_modelprotocolprofiles_ModelSpecifications_ModelSpecification~",
                        column: x => x.ModelSpecificationId,
                        principalTable: "ModelSpecifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "modelprotocolpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ProfileId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    PointKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Address = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FunctionCode = table.Column<int>(type: "int", nullable: true),
                    RegisterCount = table.Column<int>(type: "int", nullable: true, defaultValue: 1),
                    DataType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Multiplier = table.Column<double>(type: "double", nullable: true, defaultValue: 1.0),
                    Unit = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Confidence = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true, defaultValue: "confirmed")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_modelprotocolpoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_modelprotocolpoints_modelprotocolprofiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "modelprotocolprofiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_profile_point",
                table: "modelprotocolpoints",
                columns: new[] { "ProfileId", "PointKey" });

            migrationBuilder.CreateIndex(
                name: "ix_model_protocol",
                table: "modelprotocolprofiles",
                columns: new[] { "ModelSpecificationId", "Protocol" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "modelprotocolpoints");

            migrationBuilder.DropTable(
                name: "modelprotocolprofiles");

            migrationBuilder.DropColumn(
                name: "BacnetDeviceId",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "DiscoveryMessage",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "DiscoveryStatus",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "GatewayIPAddress",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "LastDiscoveredAt",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "SnmpCommunity",
                table: "DeviceDetails");

            migrationBuilder.DropColumn(
                name: "UnitId",
                table: "DeviceDetails");
        }
    }
}
