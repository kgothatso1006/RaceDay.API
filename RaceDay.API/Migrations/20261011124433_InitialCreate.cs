using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace RaceDay.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    UserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Role = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.UserID);
                    table.CheckConstraint("CK_Users_Role", "[Role] IN ('Organiser','Participant')");
                });

            migrationBuilder.CreateTable(
                name: "Organisers",
                columns: table => new
                {
                    OrganiserID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    OrganisationName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Organisers", x => x.OrganiserID);
                    table.ForeignKey(
                        name: "FK_Organisers_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Participants",
                columns: table => new
                {
                    ParticipantID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<int>(type: "int", nullable: false),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmergencyContact = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyPhone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Participants", x => x.ParticipantID);
                    table.ForeignKey(
                        name: "FK_Participants_Users_UserID",
                        column: x => x.UserID,
                        principalTable: "Users",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    EventID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrganiserID = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EventDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Venue = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.EventID);
                    table.CheckConstraint("CK_Events_Status", "[Status] IN ('Upcoming','Open','Completed','Cancelled')");
                    table.ForeignKey(
                        name: "FK_Events_Organisers_OrganiserID",
                        column: x => x.OrganiserID,
                        principalTable: "Organisers",
                        principalColumn: "OrganiserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    CategoryID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventID = table.Column<int>(type: "int", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DistanceKm = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    MaxParticipants = table.Column<int>(type: "int", nullable: false),
                    EntryFee = table.Column<decimal>(type: "decimal(10,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.CategoryID);
                    table.CheckConstraint("CK_Categories_Distance", "[DistanceKm] > 0");
                    table.ForeignKey(
                        name: "FK_Categories_Events_EventID",
                        column: x => x.EventID,
                        principalTable: "Events",
                        principalColumn: "EventID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Enrolments",
                columns: table => new
                {
                    EnrolmentID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParticipantID = table.Column<int>(type: "int", nullable: false),
                    CategoryID = table.Column<int>(type: "int", nullable: false),
                    EnrolmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnrolmentStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RaceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enrolments", x => x.EnrolmentID);
                    table.CheckConstraint("CK_Enrolments_Status", "[EnrolmentStatus] IN ('Registered','Cancelled','Completed')");
                    table.ForeignKey(
                        name: "FK_Enrolments_Categories_CategoryID",
                        column: x => x.CategoryID,
                        principalTable: "Categories",
                        principalColumn: "CategoryID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Enrolments_Participants_ParticipantID",
                        column: x => x.ParticipantID,
                        principalTable: "Participants",
                        principalColumn: "ParticipantID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Results",
                columns: table => new
                {
                    ResultID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EnrolmentID = table.Column<int>(type: "int", nullable: false),
                    FinishTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Position = table.Column<int>(type: "int", nullable: true),
                    ResultStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Results", x => x.ResultID);
                    table.CheckConstraint("CK_Results_Status", "[ResultStatus] IN ('Finished','DidNotFinish','Disqualified')");
                    table.ForeignKey(
                        name: "FK_Results_Enrolments_EnrolmentID",
                        column: x => x.EnrolmentID,
                        principalTable: "Enrolments",
                        principalColumn: "EnrolmentID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "UserID", "CreatedAt", "Email", "FirstName", "LastName", "PasswordHash", "Phone", "Role" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "thabo@raceday.co.za", "Thabo", "Mokoena", "HASHED_PASSWORD_1", "0821112233", "Organiser" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "lerato@raceday.co.za", "Lerato", "Maseko", "HASHED_PASSWORD_2", "0832223344", "Organiser" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "rich@example.com", "Rich", "Ringane", "HASHED_PASSWORD_3", "0843334455", "Participant" },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "kabelo@example.com", "Kabelo", "Ndlovu", "HASHED_PASSWORD_4", "0854445566", "Participant" }
                });

            migrationBuilder.InsertData(
                table: "Organisers",
                columns: new[] { "OrganiserID", "OrganisationName", "UserID" },
                values: new object[,]
                {
                    { 1, "Limpopo Race Events", 1 },
                    { 2, "Makhado Sports Management", 2 }
                });

            migrationBuilder.InsertData(
                table: "Participants",
                columns: new[] { "ParticipantID", "DateOfBirth", "EmergencyContact", "EmergencyPhone", "UserID" },
                values: new object[,]
                {
                    { 1, new DateTime(2005, 4, 15, 0, 0, 0, 0, DateTimeKind.Unspecified), "Mpho Ringane", "0715556677", 3 },
                    { 2, new DateTime(2004, 9, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), "Neo Ndlovu", "0726667788", 4 }
                });

            migrationBuilder.InsertData(
                table: "Events",
                columns: new[] { "EventID", "CreatedAt", "Description", "EventDate", "EventName", "Location", "OrganiserID", "StartTime", "Status", "Venue" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Community road running event with multiple race categories.", new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Limpopo Spring Run", "Polokwane, Limpopo", 1, new TimeSpan(0, 7, 0, 0, 0), "Open", "Polokwane Sports Complex" },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Annual marathon celebrating sport and local heritage.", new DateTime(2026, 10, 18, 0, 0, 0, 0, DateTimeKind.Unspecified), "Makhado Heritage Marathon", "Makhado, Limpopo", 1, new TimeSpan(0, 6, 30, 0, 0), "Upcoming", "Makhado Show Grounds" },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), "Charity race supporting youth development initiatives.", new DateTime(2026, 11, 8, 0, 0, 0, 0, DateTimeKind.Unspecified), "Thohoyandou Charity Race", "Thohoyandou, Limpopo", 2, new TimeSpan(0, 7, 30, 0, 0), "Upcoming", "Thohoyandou Stadium" }
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "CategoryID", "CategoryName", "DistanceKm", "EntryFee", "EventID", "MaxParticipants" },
                values: new object[,]
                {
                    { 1, "5 km Fun Run", 5.00m, 80.00m, 1, 300 },
                    { 2, "10 km Challenge", 10.00m, 120.00m, 1, 250 },
                    { 3, "21 km Half Marathon", 21.10m, 220.00m, 2, 500 },
                    { 4, "42 km Marathon", 42.20m, 350.00m, 2, 600 },
                    { 5, "5 km Charity Run", 5.00m, 60.00m, 3, 400 },
                    { 6, "10 km Charity Challenge", 10.00m, 100.00m, 3, 300 }
                });

            migrationBuilder.InsertData(
                table: "Enrolments",
                columns: new[] { "EnrolmentID", "CategoryID", "EnrolmentDate", "EnrolmentStatus", "ParticipantID", "RaceNumber" },
                values: new object[,]
                {
                    { 1, 1, new DateTime(2026, 8, 1, 9, 0, 0, 0, DateTimeKind.Unspecified), "Registered", 1, "LR001" },
                    { 2, 3, new DateTime(2026, 8, 5, 10, 30, 0, 0, DateTimeKind.Unspecified), "Registered", 1, "MH001" },
                    { 3, 2, new DateTime(2026, 8, 3, 11, 15, 0, 0, DateTimeKind.Unspecified), "Registered", 2, "LR002" },
                    { 4, 5, new DateTime(2026, 8, 10, 14, 0, 0, 0, DateTimeKind.Unspecified), "Registered", 2, "TC001" }
                });

            migrationBuilder.InsertData(
                table: "Results",
                columns: new[] { "ResultID", "EnrolmentID", "FinishTime", "Position", "RecordedAt", "ResultStatus" },
                values: new object[,]
                {
                    { 1, 1, new TimeSpan(0, 0, 31, 42, 0), 12, new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finished" },
                    { 2, 3, new TimeSpan(0, 0, 55, 18, 0), 8, new DateTime(2026, 9, 20, 0, 0, 0, 0, DateTimeKind.Unspecified), "Finished" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_EventID",
                table: "Categories",
                column: "EventID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_CategoryID",
                table: "Enrolments",
                column: "CategoryID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_ParticipantID",
                table: "Enrolments",
                column: "ParticipantID");

            migrationBuilder.CreateIndex(
                name: "IX_Enrolments_RaceNumber",
                table: "Enrolments",
                column: "RaceNumber",
                unique: true,
                filter: "[RaceNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OrganiserID",
                table: "Events",
                column: "OrganiserID");

            migrationBuilder.CreateIndex(
                name: "IX_Organisers_UserID",
                table: "Organisers",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Participants_UserID",
                table: "Participants",
                column: "UserID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Results_EnrolmentID",
                table: "Results",
                column: "EnrolmentID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Phone",
                table: "Users",
                column: "Phone",
                unique: true,
                filter: "[Phone] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Results");

            migrationBuilder.DropTable(
                name: "Enrolments");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Participants");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Organisers");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
