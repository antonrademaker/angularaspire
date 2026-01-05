using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.EventManagement.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_events_created_by_user",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_created_at",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_created_by_user",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_custom_fields_gin",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_discovery",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_end_date",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_fulltext_search",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_slug",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_start_date",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_status",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_tags_gin",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropIndex(
                name: "ix_events_visibility",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "MaxCapacity",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "banner_image_url",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "contact_email",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "current_attendees",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "custom_fields",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "is_virtual",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "logo_image_url",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "max_attendees",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "registration_close_date",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "tags",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "timezone",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "venue_address",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "venue_name",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "virtual_meeting_url",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "visibility",
                schema: "event_management",
                table: "events");

            migrationBuilder.RenameColumn(
                name: "title",
                schema: "event_management",
                table: "events",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "status",
                schema: "event_management",
                table: "events",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "slug",
                schema: "event_management",
                table: "events",
                newName: "Slug");

            migrationBuilder.RenameColumn(
                name: "description",
                schema: "event_management",
                table: "events",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "id",
                schema: "event_management",
                table: "events",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "updated_at",
                schema: "event_management",
                table: "events",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "start_date",
                schema: "event_management",
                table: "events",
                newName: "StartDate");

            migrationBuilder.RenameColumn(
                name: "end_date",
                schema: "event_management",
                table: "events",
                newName: "EndDate");

            migrationBuilder.RenameColumn(
                name: "detailed_description",
                schema: "event_management",
                table: "events",
                newName: "DetailedDescription");

            migrationBuilder.RenameColumn(
                name: "created_at",
                schema: "event_management",
                table: "events",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "website_url",
                schema: "event_management",
                table: "events",
                newName: "LogoUrl");

            migrationBuilder.RenameColumn(
                name: "registration_open_date",
                schema: "event_management",
                table: "events",
                newName: "DeletedAt");

            migrationBuilder.RenameColumn(
                name: "created_by_user_id",
                schema: "event_management",
                table: "events",
                newName: "UpdatedBy");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                schema: "event_management",
                table: "events",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAt",
                schema: "event_management",
                table: "events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                schema: "event_management",
                table: "events",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedBy",
                schema: "event_management",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PrimaryColor",
                schema: "event_management",
                table: "events",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SeriesId",
                schema: "event_management",
                table: "events",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "event_series",
                schema: "event_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    LogoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_series", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "time_slots",
                schema: "event_management",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    IsEventLevel = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_time_slots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_time_slots_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event_management",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tracks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tracks_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "event_management",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_events_SeriesId",
                schema: "event_management",
                table: "events",
                column: "SeriesId");

            migrationBuilder.CreateIndex(
                name: "IX_time_slots_EventId",
                schema: "event_management",
                table: "time_slots",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_tracks_EventId",
                table: "tracks",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_events_event_series_SeriesId",
                schema: "event_management",
                table: "events",
                column: "SeriesId",
                principalSchema: "event_management",
                principalTable: "event_series",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_events_event_series_SeriesId",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropTable(
                name: "event_series",
                schema: "event_management");

            migrationBuilder.DropTable(
                name: "time_slots",
                schema: "event_management");

            migrationBuilder.DropTable(
                name: "tracks");

            migrationBuilder.DropIndex(
                name: "IX_events_SeriesId",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "PrimaryColor",
                schema: "event_management",
                table: "events");

            migrationBuilder.DropColumn(
                name: "SeriesId",
                schema: "event_management",
                table: "events");

            migrationBuilder.RenameColumn(
                name: "Title",
                schema: "event_management",
                table: "events",
                newName: "title");

            migrationBuilder.RenameColumn(
                name: "Status",
                schema: "event_management",
                table: "events",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "Slug",
                schema: "event_management",
                table: "events",
                newName: "slug");

            migrationBuilder.RenameColumn(
                name: "Description",
                schema: "event_management",
                table: "events",
                newName: "description");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "event_management",
                table: "events",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                schema: "event_management",
                table: "events",
                newName: "updated_at");

            migrationBuilder.RenameColumn(
                name: "StartDate",
                schema: "event_management",
                table: "events",
                newName: "start_date");

            migrationBuilder.RenameColumn(
                name: "EndDate",
                schema: "event_management",
                table: "events",
                newName: "end_date");

            migrationBuilder.RenameColumn(
                name: "DetailedDescription",
                schema: "event_management",
                table: "events",
                newName: "detailed_description");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                schema: "event_management",
                table: "events",
                newName: "created_at");

            migrationBuilder.RenameColumn(
                name: "UpdatedBy",
                schema: "event_management",
                table: "events",
                newName: "created_by_user_id");

            migrationBuilder.RenameColumn(
                name: "LogoUrl",
                schema: "event_management",
                table: "events",
                newName: "website_url");

            migrationBuilder.RenameColumn(
                name: "DeletedAt",
                schema: "event_management",
                table: "events",
                newName: "registration_open_date");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "event_management",
                table: "events",
                type: "text",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                schema: "event_management",
                table: "events",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<int>(
                name: "MaxCapacity",
                schema: "event_management",
                table: "events",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "banner_image_url",
                schema: "event_management",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "contact_email",
                schema: "event_management",
                table: "events",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "current_attendees",
                schema: "event_management",
                table: "events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "custom_fields",
                schema: "event_management",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_virtual",
                schema: "event_management",
                table: "events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "logo_image_url",
                schema: "event_management",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_attendees",
                schema: "event_management",
                table: "events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "registration_close_date",
                schema: "event_management",
                table: "events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "tags",
                schema: "event_management",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                schema: "event_management",
                table: "events",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "UTC");

            migrationBuilder.AddColumn<string>(
                name: "venue_address",
                schema: "event_management",
                table: "events",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "venue_name",
                schema: "event_management",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "virtual_meeting_url",
                schema: "event_management",
                table: "events",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                schema: "event_management",
                table: "events",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_events_created_at",
                schema: "event_management",
                table: "events",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_events_created_by_user",
                schema: "event_management",
                table: "events",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_events_custom_fields_gin",
                schema: "event_management",
                table: "events",
                column: "custom_fields")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_events_discovery",
                schema: "event_management",
                table: "events",
                columns: new[] { "status", "visibility", "start_date" });

            migrationBuilder.CreateIndex(
                name: "ix_events_end_date",
                schema: "event_management",
                table: "events",
                column: "end_date");

            migrationBuilder.CreateIndex(
                name: "ix_events_fulltext_search",
                schema: "event_management",
                table: "events",
                columns: new[] { "title", "description" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:TsVectorConfig", "english");

            migrationBuilder.CreateIndex(
                name: "ix_events_slug",
                schema: "event_management",
                table: "events",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_events_start_date",
                schema: "event_management",
                table: "events",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "ix_events_status",
                schema: "event_management",
                table: "events",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_events_tags_gin",
                schema: "event_management",
                table: "events",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_events_visibility",
                schema: "event_management",
                table: "events",
                column: "visibility");

            migrationBuilder.AddForeignKey(
                name: "fk_events_created_by_user",
                schema: "event_management",
                table: "events",
                column: "created_by_user_id",
                principalTable: "User",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
