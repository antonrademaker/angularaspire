# Quickstart: Event Administration UI

**Branch**: `003-event-admin-ui`

## Prerequisites

- Docker Desktop running
- .NET 10 SDK installed
- Node.js 20+ installed

## Running the Solution

1. **Start the Aspire Host**:
   ```powershell
   dotnet run --project src/AppHost/AppHost.csproj
   ```

2. **Access the Applications**:
   - **Aspire Dashboard**: https://localhost:18888
   - **PrivateApp (Admin)**: https://localhost:4200 (or port assigned by Aspire)
   - **PublicApp**: https://localhost:4300 (or port assigned by Aspire)
   - **PrivateApi Swagger**: https://localhost:7100/swagger
   - **PublicApi Swagger**: https://localhost:7200/swagger

## Feature Verification

### 1. Create an Event
1. Log in to **PrivateApp** as an admin.
2. Navigate to **Events**.
3. Click **Create Event**.
4. Enter "TechConf 2026", dates, and save.
5. Verify event appears in the list as "Draft".

### 2. Configure Tracks & Slots
1. Open the created event.
2. Go to **Tracks** tab.
3. Add "Main Track" and "Workshop Track".
4. Go to **Schedule** tab.
5. Add time slots (e.g., 09:00-10:00 Keynote).

### 3. Schedule a Session
1. Go to **Sessions** tab.
2. Create a session "Keynote: Future of Tech".
3. Go back to **Schedule**.
4. Drag the session to the Keynote slot.
5. Verify assignment is saved.

### 4. Publish & Verify
1. Change event status to **Published**.
2. Open **PublicApp**.
3. Verify the event and schedule are visible.

## Troubleshooting

- **Database Issues**: Check PostgreSQL container logs in Docker.
- **API Errors**: Check OpenTelemetry traces in Aspire Dashboard.
- **UI Issues**: Check browser console and network tab.
