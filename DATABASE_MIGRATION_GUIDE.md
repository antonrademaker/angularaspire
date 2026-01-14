# Database Migration Guide

## Current Issue

The database has tables from old migrations that conflict with the new consolidated schema structure. The new migration expects a clean schema with proper schema separation (`event_management`, `session_management`, `user_management`).

## Solution Options

### Option 1: Automatic Database Reset (Recommended for Development)

The PrivateApi now supports automatic database reset for development environments.

**Steps:**

1. Stop the application if it's running
2. Edit `src/PrivateApi/appsettings.Development.json` and set:
   ```json
   {
     "ResetDatabase": true
   }
   ```
3. Start the application (the database will be dropped and recreated)
4. **Important:** After successful startup, change `ResetDatabase` back to `false` to prevent accidental deletion on next restart

### Option 2: Manual Database Reset (Alternative)

If you prefer to manually control the database reset:

**Using psql or pgAdmin:**

```sql
-- Connect to postgres database (not eventdb)
DROP DATABASE IF EXISTS eventdb;
CREATE DATABASE eventdb;
```

**Using PowerShell:**

```powershell
# Connect to the postgres container
docker exec -it <postgres-container-name> psql -U postgres

# Then run:
DROP DATABASE IF EXISTS eventdb;
CREATE DATABASE eventdb;
\q
```

**Using Docker Compose:**

```powershell
# Stop containers
docker-compose down

# Remove volumes (this deletes all data)
docker-compose down -v

# Start fresh
docker-compose up -d
```

### Option 3: Create a Fresh Migration from Current Database

If you want to keep the current database state as-is:

1. Delete all migration files in `src/Shared/EventManagement/Data/Migrations/`
2. Run:
   ```powershell
   cd src/Shared
   dotnet ef migrations add InitialCreate --context AppDbContext --output-dir "EventManagement\Data\Migrations"
   ```

## After Database Reset

Once the database is reset, the application will:

1. Detect pending migrations
2. Apply the `20260114193807_ConsolidatedDbContextComplete` migration
3. Create all tables with proper schema separation
4. Start successfully

## Warnings and Shadow Properties

You may see these warnings in the logs:

```
warn: Microsoft.EntityFrameworkCore.Model.Validation[10625]
      The foreign key property 'Track.CreatedByUserId1' was created in shadow state...
```

These warnings indicate that EF Core is creating additional foreign key properties because of naming conflicts. To fix these:

1. Review the `Track` entity and ensure foreign key properties are properly configured
2. Use explicit foreign key configuration in `AppDbContext.OnModelCreating`

## Prevention

To prevent this issue in the future:

1. **Always** ensure migrations are applied before creating new ones
2. Use `dotnet ef migrations list` to check current migration status
3. Keep development databases in sync with the migration history
4. Document any manual database changes

## Need Help?

If you continue to experience issues:

1. Check that the PostgreSQL container is running
2. Verify connection string in configuration
3. Check the `__EFMigrationsHistory` table in the database
4. Review application logs for detailed error messages
