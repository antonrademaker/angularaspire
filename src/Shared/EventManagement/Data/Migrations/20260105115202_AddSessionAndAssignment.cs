using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.EventManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAndAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sessions",
                schema: "event_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShortCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Abstract = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false),
                    Duration = table.Column<int>(type: "integer", nullable: false),
                    Level = table.Column<string>(type: "text", nullable: false),
                    Language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: true),
                    SubmissionStatus = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sessions_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event_management",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "session_assignments",
                schema: "event_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeSlotId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoomConfigurationId = table.Column<Guid>(type: "uuid", nullable: true),
                    RoomSnapshot = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_assignments_room_configurations_RoomConfigurationId",
                        column: x => x.RoomConfigurationId,
                        principalSchema: "event_management",
                        principalTable: "room_configurations",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_session_assignments_sessions_SessionId",
                        column: x => x.SessionId,
                        principalSchema: "event_management",
                        principalTable: "sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_session_assignments_time_slots_TimeSlotId",
                        column: x => x.TimeSlotId,
                        principalSchema: "event_management",
                        principalTable: "time_slots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_session_assignments_tracks_TrackId",
                        column: x => x.TrackId,
                        principalTable: "tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_session_assignments_RoomConfigurationId",
                schema: "event_management",
                table: "session_assignments",
                column: "RoomConfigurationId");

            migrationBuilder.CreateIndex(
                name: "IX_session_assignments_SessionId",
                schema: "event_management",
                table: "session_assignments",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_session_assignments_TimeSlotId",
                schema: "event_management",
                table: "session_assignments",
                column: "TimeSlotId");

            migrationBuilder.CreateIndex(
                name: "IX_session_assignments_TrackId",
                schema: "event_management",
                table: "session_assignments",
                column: "TrackId");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_EventId",
                schema: "event_management",
                table: "sessions",
                column: "EventId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "session_assignments",
                schema: "event_management");

            migrationBuilder.DropTable(
                name: "sessions",
                schema: "event_management");
        }
    }
}
