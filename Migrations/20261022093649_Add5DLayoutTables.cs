using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DeviceManagementOnly.Migrations
{
    /// <inheritdoc />
    public partial class Add5DLayoutTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Rooms",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DimensionUnit",
                table: "Rooms",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "Rooms",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Rooms",
                type: "varchar(60)",
                maxLength: 60,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InnerMarginsJson",
                table: "Rooms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "MarginsJson",
                table: "Rooms",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "PowerBudgetKW",
                table: "Rooms",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoomType",
                table: "Rooms",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Rooms",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "AisleName",
                table: "Racks",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "Racks",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "DepthMm",
                table: "Racks",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FacingDirection",
                table: "Racks",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "HeightMm",
                table: "Racks",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LabelText",
                table: "Racks",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "RotationAngle",
                table: "Racks",
                type: "decimal(9,3)",
                precision: 9,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RowName",
                table: "Racks",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "Racks",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "WidthMm",
                table: "Racks",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DataCenterId",
                table: "Floors",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<decimal>(
                name: "HeightM",
                table: "Floors",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LengthM",
                table: "Floors",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LevelNumber",
                table: "Floors",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RaisedFloorHeightM",
                table: "Floors",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TileSizeM",
                table: "Floors",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WidthM",
                table: "Floors",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CoolingKW",
                table: "DeviceTypes",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultHeightMm",
                table: "DeviceTypes",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultLengthMm",
                table: "DeviceTypes",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultWidthMm",
                table: "DeviceTypes",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "HeatKW",
                table: "DeviceTypes",
                type: "decimal(18,3)",
                precision: 18,
                scale: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LayerKey",
                table: "DeviceTypes",
                type: "varchar(40)",
                maxLength: 40,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Shape",
                table: "DeviceTypes",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorHex",
                table: "DeviceRackLocations",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Face",
                table: "DeviceRackLocations",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "FaceImageFileId",
                table: "DeviceRackLocations",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "PortsJson",
                table: "DeviceRackLocations",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CableConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FromPlacementId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FromDeviceDetailId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FromEndpoint = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromFace = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToPlacementId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ToDeviceDetailId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ToEndpoint = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ToFace = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CableType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LayerKey = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrayPathJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WaypointsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LengthM = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ColorHex = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NetworkConnectionId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CableConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CableConnections_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CableTrays",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TrayCode = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Axis = table.Column<string>(type: "varchar(2)", maxLength: 2, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Line = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    FromCoord = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ToCoord = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    WidthM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Slots = table.Column<int>(type: "int", nullable: false),
                    PitchM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    NearSide = table.Column<int>(type: "int", nullable: false),
                    Level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ElevationY = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TrayType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorHex = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_CableTrays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CableTrays_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DataCenters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LengthM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    BreadthM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    HeightM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    LengthUnit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BreadthUnit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MarginTop = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MarginBottom = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MarginLeft = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MarginRight = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MarginUnit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DoorDirection = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DoorPosition = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TotalKW = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalKWUnit = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PowerCapacityKW = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    CoolingCapacityKW = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    TotalRacks = table.Column<int>(type: "int", nullable: true),
                    UtilizedRacks = table.Column<int>(type: "int", nullable: true),
                    LocationName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StreetAddress = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PinCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CountryId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    StateId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CityId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SiteId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TenantId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ContactPersonId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", precision: 10, scale: 7, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DxfFileId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SetupMode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataCenters", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DeviceFloorPlacements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DeviceDetailId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DeviceTypeId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PositionX = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PositionZ = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PositionY = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    RotationAngle = table.Column<decimal>(type: "decimal(9,3)", precision: 9, scale: 3, nullable: false),
                    Level = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LengthMm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    WidthMm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    HeightMm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    Shape = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ColorHex = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IconName = table.Column<string>(type: "varchar(60)", maxLength: 60, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LabelText = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RowName = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AisleName = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsPinned = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsVisible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsProvisional = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PlaceholderName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    MetaJson = table.Column<string>(type: "longtext", nullable: true)
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
                    table.PrimaryKey("PK_DeviceFloorPlacements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeviceFloorPlacements_DeviceDetails_DeviceDetailId",
                        column: x => x.DeviceDetailId,
                        principalTable: "DeviceDetails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DeviceFloorPlacements_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeviceFloorPlacements_Rooms_RoomId",
                        column: x => x.RoomId,
                        principalTable: "Rooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FloorTiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RoomId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TileType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PositionX = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PositionZ = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    SizeM = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    AirflowCfm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    AisleType = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
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
                    table.PrimaryKey("PK_FloorTiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorTiles_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LayoutLocks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DataCenterId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LockedBy = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LockNote = table.Column<string>(type: "varchar(300)", maxLength: 300, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnlockedBy = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnlockedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EditingBy = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EditingSessionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EditingSince = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    EditingExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Revision = table.Column<int>(type: "int", nullable: false),
                    LastSavedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastSavedBy = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LayoutLocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LayoutLocks_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LayoutViews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DataCenterId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CameraJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LayerStateJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilterStateJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailFileId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    IsShared = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_LayoutViews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LayoutViews_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PipeConnections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FloorId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FromPlacementId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    FromDeviceDetailId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ToPlacementId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    ToDeviceDetailId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    PipeType = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiameterMm = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    RouteJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    WaypointsJson = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromOffset = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ToOffset = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ElevationY = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    LengthM = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: true),
                    ColorHex = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Label = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PipeConnections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PipeConnections_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Floors_DataCenterId",
                table: "Floors",
                column: "DataCenterId");

            migrationBuilder.CreateIndex(
                name: "IX_CableConnections_FloorId_IsActive",
                table: "CableConnections",
                columns: new[] { "FloorId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_CableConnections_FromPlacementId",
                table: "CableConnections",
                column: "FromPlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_CableConnections_ToPlacementId",
                table: "CableConnections",
                column: "ToPlacementId");

            migrationBuilder.CreateIndex(
                name: "IX_CableTrays_FloorId_TrayCode",
                table: "CableTrays",
                columns: new[] { "FloorId", "TrayCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DataCenters_Code",
                table: "DataCenters",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFloorPlacements_DeviceDetailId",
                table: "DeviceFloorPlacements",
                column: "DeviceDetailId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFloorPlacements_FloorId_IsActive",
                table: "DeviceFloorPlacements",
                columns: new[] { "FloorId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceFloorPlacements_RoomId",
                table: "DeviceFloorPlacements",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_FloorTiles_FloorId_IsActive",
                table: "FloorTiles",
                columns: new[] { "FloorId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_LayoutLocks_FloorId",
                table: "LayoutLocks",
                column: "FloorId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LayoutViews_FloorId",
                table: "LayoutViews",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_PipeConnections_FloorId_IsActive",
                table: "PipeConnections",
                columns: new[] { "FloorId", "IsActive" });

            migrationBuilder.AddForeignKey(
                name: "FK_Floors_DataCenters_DataCenterId",
                table: "Floors",
                column: "DataCenterId",
                principalTable: "DataCenters",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Floors_DataCenters_DataCenterId",
                table: "Floors");

            migrationBuilder.DropTable(
                name: "CableConnections");

            migrationBuilder.DropTable(
                name: "CableTrays");

            migrationBuilder.DropTable(
                name: "DataCenters");

            migrationBuilder.DropTable(
                name: "DeviceFloorPlacements");

            migrationBuilder.DropTable(
                name: "FloorTiles");

            migrationBuilder.DropTable(
                name: "LayoutLocks");

            migrationBuilder.DropTable(
                name: "LayoutViews");

            migrationBuilder.DropTable(
                name: "PipeConnections");

            migrationBuilder.DropIndex(
                name: "IX_Floors_DataCenterId",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "DimensionUnit",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "InnerMarginsJson",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "MarginsJson",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "PowerBudgetKW",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "RoomType",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Rooms");

            migrationBuilder.DropColumn(
                name: "AisleName",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "DepthMm",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "FacingDirection",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "HeightMm",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "LabelText",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "RotationAngle",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "RowName",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "WidthMm",
                table: "Racks");

            migrationBuilder.DropColumn(
                name: "DataCenterId",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "HeightM",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "LengthM",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "LevelNumber",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "RaisedFloorHeightM",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "TileSizeM",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "WidthM",
                table: "Floors");

            migrationBuilder.DropColumn(
                name: "CoolingKW",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "DefaultHeightMm",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "DefaultLengthMm",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "DefaultWidthMm",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "HeatKW",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "LayerKey",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "Shape",
                table: "DeviceTypes");

            migrationBuilder.DropColumn(
                name: "ColorHex",
                table: "DeviceRackLocations");

            migrationBuilder.DropColumn(
                name: "Face",
                table: "DeviceRackLocations");

            migrationBuilder.DropColumn(
                name: "FaceImageFileId",
                table: "DeviceRackLocations");

            migrationBuilder.DropColumn(
                name: "PortsJson",
                table: "DeviceRackLocations");
        }
    }
}
