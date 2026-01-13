using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shared.EventManagement.Data.Migrations;

/// <inheritdoc />
public partial class AddFoundationalEntities : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "event_management");

        migrationBuilder.CreateTable(
            name: "locations",
            schema: "event_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                MapUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                AccessibilityInfo = table.Column<string>(type: "text", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_locations", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "people",
            schema: "event_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<string>(type: "text", nullable: true),
                FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Bio = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                PhotoUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Company = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                JobTitle = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                LinkedInUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                TwitterUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                WebsiteUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_people", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "tags",
            schema: "event_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Type = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "User",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                AvatarUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                OAuthProvider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                OAuthProviderId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Role = table.Column<int>(type: "integer", nullable: false),
                Status = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastLoginAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                NotificationPreferences = table.Column<bool>(type: "boolean", nullable: false),
                Timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_User", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "rooms",
            schema: "event_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LocationId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Capacity = table.Column<int>(type: "integer", nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedBy = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_rooms", x => x.Id);
                table.ForeignKey(
                    name: "FK_rooms_locations_LocationId",
                    column: x => x.LocationId,
                    principalSchema: "event_management",
                    principalTable: "locations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "events",
            schema: "event_management",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                detailed_description = table.Column<string>(type: "text", nullable: true),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                timezone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "UTC"),
                max_attendees = table.Column<int>(type: "integer", nullable: false),
                current_attendees = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                registration_open_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                registration_close_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                visibility = table.Column<string>(type: "text", nullable: false),
                venue_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                venue_address = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_virtual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                virtual_meeting_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                banner_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                logo_image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                website_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                contact_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                custom_fields = table.Column<string>(type: "jsonb", nullable: true),
                MaxCapacity = table.Column<int>(type: "integer", nullable: true),
                tags = table.Column<string>(type: "jsonb", nullable: true),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_events", x => x.id);
                table.ForeignKey(
                    name: "fk_events_created_by_user",
                    column: x => x.created_by_user_id,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "room_configurations",
            schema: "event_management",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RoomId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Capacity = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_room_configurations", x => x.Id);
                table.ForeignKey(
                    name: "FK_room_configurations_rooms_RoomId",
                    column: x => x.RoomId,
                    principalSchema: "event_management",
                    principalTable: "rooms",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "social_events",
            schema: "event_management",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                event_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                type = table.Column<string>(type: "text", nullable: false),
                start_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                location = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                room = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                max_capacity = table.Column<int>(type: "integer", nullable: true),
                current_rsvp_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                waitlist_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                rsvp_required = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                rsvp_deadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                guests_allowed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                max_guests_per_attendee = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                dress_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                cost_per_person = table.Column<decimal>(type: "numeric(10,2)", nullable: false, defaultValue: 0m),
                currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                dietary_info = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                notes = table.Column<string>(type: "text", nullable: true),
                image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                status = table.Column<string>(type: "text", nullable: false),
                display_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                tags = table.Column<string>(type: "jsonb", nullable: true),
                custom_fields = table.Column<string>(type: "jsonb", nullable: true),
                created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_social_events", x => x.id);
                table.ForeignKey(
                    name: "fk_social_events_created_by_user",
                    column: x => x.created_by_user_id,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "fk_social_events_event",
                    column: x => x.event_id,
                    principalSchema: "event_management",
                    principalTable: "events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "social_event_rsvps",
            schema: "event_management",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                social_event_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                guest_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                guest_names = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                dietary_requirements = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                is_waitlisted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                waitlist_position = table.Column<int>(type: "integer", nullable: true),
                registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                declined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                no_show_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                is_checked_in = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                amount_paid = table.Column<decimal>(type: "numeric(10,2)", nullable: true),
                payment_status = table.Column<string>(type: "text", nullable: false, defaultValue: "NotRequired"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_social_event_rsvps", x => x.id);
                table.ForeignKey(
                    name: "fk_social_event_rsvps_social_event",
                    column: x => x.social_event_id,
                    principalSchema: "event_management",
                    principalTable: "social_events",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "fk_social_event_rsvps_user",
                    column: x => x.user_id,
                    principalTable: "User",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

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

        migrationBuilder.CreateIndex(
            name: "IX_room_configurations_RoomId",
            schema: "event_management",
            table: "room_configurations",
            column: "RoomId");

        migrationBuilder.CreateIndex(
            name: "IX_rooms_LocationId",
            schema: "event_management",
            table: "rooms",
            column: "LocationId");

        migrationBuilder.CreateIndex(
            name: "ix_social_event_rsvps_event_status",
            schema: "event_management",
            table: "social_event_rsvps",
            columns: new[] { "social_event_id", "status" });

        migrationBuilder.CreateIndex(
            name: "ix_social_event_rsvps_registered_at",
            schema: "event_management",
            table: "social_event_rsvps",
            column: "registered_at");

        migrationBuilder.CreateIndex(
            name: "ix_social_event_rsvps_social_event_id",
            schema: "event_management",
            table: "social_event_rsvps",
            column: "social_event_id");

        migrationBuilder.CreateIndex(
            name: "ix_social_event_rsvps_status",
            schema: "event_management",
            table: "social_event_rsvps",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_social_event_rsvps_unique_user",
            schema: "event_management",
            table: "social_event_rsvps",
            columns: new[] { "social_event_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_social_event_rsvps_user_id",
            schema: "event_management",
            table: "social_event_rsvps",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_social_events_created_by_user_id",
            schema: "event_management",
            table: "social_events",
            column: "created_by_user_id");

        migrationBuilder.CreateIndex(
            name: "ix_social_events_discovery",
            schema: "event_management",
            table: "social_events",
            columns: new[] { "event_id", "status", "start_time" });

        migrationBuilder.CreateIndex(
            name: "ix_social_events_event_id",
            schema: "event_management",
            table: "social_events",
            column: "event_id");

        migrationBuilder.CreateIndex(
            name: "ix_social_events_event_slug",
            schema: "event_management",
            table: "social_events",
            columns: new[] { "event_id", "slug" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_social_events_start_time",
            schema: "event_management",
            table: "social_events",
            column: "start_time");

        migrationBuilder.CreateIndex(
            name: "ix_social_events_status",
            schema: "event_management",
            table: "social_events",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "ix_social_events_type",
            schema: "event_management",
            table: "social_events",
            column: "type");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "people",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "room_configurations",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "social_event_rsvps",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "tags",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "rooms",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "social_events",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "locations",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "events",
            schema: "event_management");

        migrationBuilder.DropTable(
            name: "User");
    }
}
