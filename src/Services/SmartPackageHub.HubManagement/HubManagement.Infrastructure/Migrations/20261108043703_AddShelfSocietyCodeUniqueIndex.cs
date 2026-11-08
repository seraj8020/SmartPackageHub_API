using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HubManagement.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShelfSocietyCodeUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Societies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Street = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    State = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ZipCode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ContactEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OnboardedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Societies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsDailies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    PackagesReceived = table.Column<int>(type: "integer", nullable: false),
                    PackagesDelivered = table.Column<int>(type: "integer", nullable: false),
                    OverdueCount = table.Column<int>(type: "integer", nullable: false),
                    PendingCountVersion = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CourierSplitJson = table.Column<string>(type: "text", nullable: false, defaultValue: "{}"),
                    LastRolledUpAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsDailies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsDailies_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourierAgents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    VehicleNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EmployeeOrBadgeNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierAgents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierAgents_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CourierBlacklists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    CarrierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    BlacklistedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourierBlacklists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CourierBlacklists_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecureBags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BagNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    QrCode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SealNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CapacityOrSize = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecureBags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecureBags_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Shelves",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TowerOrWing = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsColdStorage = table.Column<bool>(type: "boolean", nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    Zone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Aisle = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Tier = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shelves", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shelves_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DispatchBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TowerWing = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Floor = table.Column<int>(type: "integer", nullable: true),
                    FlatNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DispatchBatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DispatchBatches_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DispatchBatches_Users_RunnerUserId",
                        column: x => x.RunnerUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Residents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FullName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PhoneNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TowerWing = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UnitNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsMember = table.Column<bool>(type: "boolean", nullable: false),
                    AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Residents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Residents_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Residents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserSocieties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSocieties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSocieties_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserSocieties_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DigitalSignOffReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceiptNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourierPartner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourierAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourierAgentName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CourierAgentPhone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalPackagesCount = table.Column<int>(type: "integer", nullable: false),
                    DigitalSignature = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DigitalSignOffReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DigitalSignOffReceipts_CourierAgents_CourierAgentId",
                        column: x => x.CourierAgentId,
                        principalTable: "CourierAgents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DigitalSignOffReceipts_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DigitalSignOffReceipts_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Routes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginLocation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DestinationLocation = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    DistanceMetres = table.Column<int>(type: "integer", nullable: false),
                    MapProvider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Polyline = table.Column<string>(type: "text", nullable: true),
                    EstimatedDurationSecs = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Routes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Routes_DispatchBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "DispatchBatches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Routes_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuestAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    GuestName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PhoneNo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuestAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GuestAccesses_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GuestAccesses_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Memberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsAutoRenewEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Memberships", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Memberships_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Memberships_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VacationHolds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ToDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VacationHolds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VacationHolds_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_VacationHolds_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PkgNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SphTrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShelfId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShelfAssignedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SecureBagId = table.Column<Guid>(type: "uuid", nullable: true),
                    DigitalSignOffReceiptId = table.Column<Guid>(type: "uuid", nullable: true),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    VacationHoldId = table.Column<Guid>(type: "uuid", nullable: true),
                    PreferredDeliveryMethod = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    Height = table.Column<double>(type: "double precision", nullable: false),
                    Width = table.Column<double>(type: "double precision", nullable: false),
                    Length = table.Column<double>(type: "double precision", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CourierAwb = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourierPartner = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RecipientName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    TowerWing = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FlatNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsColdStorage = table.Column<bool>(type: "boolean", nullable: false),
                    IsCod = table.Column<bool>(type: "boolean", nullable: false),
                    CodAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    CodApprovalStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RecievedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Packages_DigitalSignOffReceipts_DigitalSignOffReceiptId",
                        column: x => x.DigitalSignOffReceiptId,
                        principalTable: "DigitalSignOffReceipts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_SecureBags_SecureBagId",
                        column: x => x.SecureBagId,
                        principalTable: "SecureBags",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_Shelves_ShelfId",
                        column: x => x.ShelfId,
                        principalTable: "Shelves",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Packages_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Packages_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Packages_VacationHolds_VacationHoldId",
                        column: x => x.VacationHoldId,
                        principalTable: "VacationHolds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeliverySlots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeliverySlots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DeliverySlots_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverySlots_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DeliverySlots_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChannelType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Destination = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TemplateName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    ProviderRequestId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    FallbackOfNotificationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SegmentCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Notifications_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OtpLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ExpiryTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Purpose = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsUsed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OtpLogs_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OtpLogs_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OtpLogs_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageIntakes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    StaffUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CourierAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScannedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MatchStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RawOcrText = table.Column<string>(type: "text", nullable: true),
                    OcrElapsedMs = table.Column<int>(type: "integer", nullable: true),
                    ClientRequestId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Barcode = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsDamaged = table.Column<bool>(type: "boolean", nullable: false),
                    DamageNote = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DamageImagePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageIntakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageIntakes_CourierAgents_CourierAgentId",
                        column: x => x.CourierAgentId,
                        principalTable: "CourierAgents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PackageIntakes_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageIntakes_Users_StaffUserId",
                        column: x => x.StaffUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RouteStops",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    TowerWing = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SequenceOrder = table.Column<int>(type: "integer", nullable: false),
                    IsVisited = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteStops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteStops_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RouteStops_Routes_RouteId",
                        column: x => x.RouteId,
                        principalTable: "Routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Deliveries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SocietyId = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    HandedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OtpLogId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Method = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Deliveries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Deliveries_DispatchBatches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "DispatchBatches",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Deliveries_OtpLogs_OtpLogId",
                        column: x => x.OtpLogId,
                        principalTable: "OtpLogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Deliveries_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Deliveries_Residents_ResidentId",
                        column: x => x.ResidentId,
                        principalTable: "Residents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Deliveries_Societies_SocietyId",
                        column: x => x.SocietyId,
                        principalTable: "Societies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Deliveries_Users_HandedByUserId",
                        column: x => x.HandedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackageImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PackageId = table.Column<Guid>(type: "uuid", nullable: false),
                    IntakeId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    StorageKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SHA256Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ByteSize = table.Column<long>(type: "bigint", nullable: false),
                    CapturedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageImages_PackageIntakes_IntakeId",
                        column: x => x.IntakeId,
                        principalTable: "PackageIntakes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackageImages_Packages_PackageId",
                        column: x => x.PackageId,
                        principalTable: "Packages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailies_SocietyId",
                table: "AnalyticsDailies",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsDailies_SocietyId_Day",
                table: "AnalyticsDailies",
                columns: new[] { "SocietyId", "Day" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_SocietyId",
                table: "AuditLogs",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_CourierAgents_SocietyId_Phone",
                table: "CourierAgents",
                columns: new[] { "SocietyId", "Phone" });

            migrationBuilder.CreateIndex(
                name: "IX_CourierBlacklists_SocietyId",
                table: "CourierBlacklists",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_BatchId",
                table: "Deliveries",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_HandedByUserId",
                table: "Deliveries",
                column: "HandedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_OtpLogId",
                table: "Deliveries",
                column: "OtpLogId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_PackageId",
                table: "Deliveries",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_ResidentId",
                table: "Deliveries",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Deliveries_SocietyId",
                table: "Deliveries",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySlots_PackageId",
                table: "DeliverySlots",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySlots_ResidentId",
                table: "DeliverySlots",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_DeliverySlots_SocietyId_WindowStart_WindowEnd",
                table: "DeliverySlots",
                columns: new[] { "SocietyId", "WindowStart", "WindowEnd" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_CourierAgentId",
                table: "DigitalSignOffReceipts",
                column: "CourierAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_ReceiptNumber",
                table: "DigitalSignOffReceipts",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_SocietyId_SignedAt",
                table: "DigitalSignOffReceipts",
                columns: new[] { "SocietyId", "SignedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_DigitalSignOffReceipts_StaffUserId",
                table: "DigitalSignOffReceipts",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBatches_RunnerUserId",
                table: "DispatchBatches",
                column: "RunnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DispatchBatches_SocietyId",
                table: "DispatchBatches",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestAccesses_ResidentId",
                table: "GuestAccesses",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_GuestAccesses_SocietyId",
                table: "GuestAccesses",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_ResidentId",
                table: "Memberships",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Memberships_SocietyId",
                table: "Memberships",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IdempotencyKey",
                table: "Notifications",
                column: "IdempotencyKey",
                unique: true,
                filter: "\"IdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_PackageId",
                table: "Notifications",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ProviderRequestId",
                table: "Notifications",
                column: "ProviderRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ResidentId",
                table: "Notifications",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_SocietyId",
                table: "Notifications",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_PackageId_Purpose_IsUsed",
                table: "OtpLogs",
                columns: new[] { "PackageId", "Purpose", "IsUsed" });

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_ResidentId",
                table: "OtpLogs",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_OtpLogs_SocietyId",
                table: "OtpLogs",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageImages_IntakeId",
                table: "PackageImages",
                column: "IntakeId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageImages_PackageId",
                table: "PackageImages",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageIntakes_CourierAgentId",
                table: "PackageIntakes",
                column: "CourierAgentId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageIntakes_PackageId",
                table: "PackageIntakes",
                column: "PackageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PackageIntakes_StaffUserId",
                table: "PackageIntakes",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_DigitalSignOffReceiptId",
                table: "Packages",
                column: "DigitalSignOffReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_ResidentId_Status",
                table: "Packages",
                columns: new[] { "ResidentId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SecureBagId",
                table: "Packages",
                column: "SecureBagId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_ShelfId",
                table: "Packages",
                column: "ShelfId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_SocietyId_Status_RecievedAt",
                table: "Packages",
                columns: new[] { "SocietyId", "Status", "RecievedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Packages_StaffUserId",
                table: "Packages",
                column: "StaffUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Packages_VacationHoldId",
                table: "Packages",
                column: "VacationHoldId");

            migrationBuilder.CreateIndex(
                name: "IX_Residents_SocietyId_PhoneNumber",
                table: "Residents",
                columns: new[] { "SocietyId", "PhoneNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_Residents_SocietyId_TowerWing",
                table: "Residents",
                columns: new[] { "SocietyId", "TowerWing" });

            migrationBuilder.CreateIndex(
                name: "IX_Residents_UserId",
                table: "Residents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_BatchId",
                table: "Routes",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_Routes_SocietyId",
                table: "Routes",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_PackageId",
                table: "RouteStops",
                column: "PackageId");

            migrationBuilder.CreateIndex(
                name: "IX_RouteStops_RouteId",
                table: "RouteStops",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_SecureBags_SocietyId_BagNumber",
                table: "SecureBags",
                columns: new[] { "SocietyId", "BagNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shelves_SocietyId_Code",
                table: "Shelves",
                columns: new[] { "SocietyId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSocieties_SocietyId",
                table: "UserSocieties",
                column: "SocietyId");

            migrationBuilder.CreateIndex(
                name: "IX_UserSocieties_UserId",
                table: "UserSocieties",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationHolds_ResidentId",
                table: "VacationHolds",
                column: "ResidentId");

            migrationBuilder.CreateIndex(
                name: "IX_VacationHolds_SocietyId",
                table: "VacationHolds",
                column: "SocietyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsDailies");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "CourierBlacklists");

            migrationBuilder.DropTable(
                name: "Deliveries");

            migrationBuilder.DropTable(
                name: "DeliverySlots");

            migrationBuilder.DropTable(
                name: "GuestAccesses");

            migrationBuilder.DropTable(
                name: "Memberships");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PackageImages");

            migrationBuilder.DropTable(
                name: "RouteStops");

            migrationBuilder.DropTable(
                name: "UserSocieties");

            migrationBuilder.DropTable(
                name: "OtpLogs");

            migrationBuilder.DropTable(
                name: "PackageIntakes");

            migrationBuilder.DropTable(
                name: "Routes");

            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "DispatchBatches");

            migrationBuilder.DropTable(
                name: "DigitalSignOffReceipts");

            migrationBuilder.DropTable(
                name: "SecureBags");

            migrationBuilder.DropTable(
                name: "Shelves");

            migrationBuilder.DropTable(
                name: "VacationHolds");

            migrationBuilder.DropTable(
                name: "CourierAgents");

            migrationBuilder.DropTable(
                name: "Residents");

            migrationBuilder.DropTable(
                name: "Societies");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
