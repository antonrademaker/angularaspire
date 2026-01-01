# Event Management API - C# Client

A strongly-typed C# client for the Event Management External API.

## Installation

### Option 1: Add files directly

Copy the `EventManagementClient.cs` file into your project.

### Option 2: Create a class library

```bash
dotnet new classlib -n EventManagement.Client
cd EventManagement.Client
# Copy EventManagementClient.cs to the project
dotnet add package System.Net.Http.Json
```

## Usage

```csharp
using EventManagement.Client;

// Create client
var client = new EventManagementClient("http://localhost:5000", "your_api_key_here");

// List events
var events = await client.ListEventsAsync(page: 1, pageSize: 10);
foreach (var evt in events.Data.Items)
{
    Console.WriteLine($"- {evt.Title} ({evt.Status})");
}

// Get single event
var singleEvent = await client.GetEventAsync(eventId);
Console.WriteLine($"Event: {singleEvent.Data.Title}");

// Create event
var newEvent = await client.CreateEventAsync(new CreateEventRequest
{
    Title = "New Event",
    Description = "Event description",
    StartDate = DateTime.UtcNow.AddDays(7),
    EndDate = DateTime.UtcNow.AddDays(7).AddHours(8),
    Location = "Conference Center",
    MaxCapacity = 100,
    IsVirtual = false
});
Console.WriteLine($"Created: {newEvent.Data.Id}");

// Check rate limits
var rateLimitInfo = client.GetRateLimitInfo();
Console.WriteLine($"Remaining: {rateLimitInfo.Remaining}/{rateLimitInfo.Limit}");
```

## Dependency Injection

```csharp
// In Program.cs or Startup.cs
services.AddHttpClient<IEventManagementClient, EventManagementClient>(client =>
{
    client.BaseAddress = new Uri(configuration["EventApi:BaseUrl"]);
    client.DefaultRequestHeaders.Add("X-Api-Key", configuration["EventApi:ApiKey"]);
});
```

## Error Handling

```csharp
try
{
    var events = await client.ListEventsAsync();
}
catch (RateLimitException ex)
{
    Console.WriteLine($"Rate limited. Retry after {ex.RetryAfter} seconds");
    await Task.Delay(TimeSpan.FromSeconds(ex.RetryAfter));
    // Retry...
}
catch (ApiException ex)
{
    Console.WriteLine($"API Error: {ex.Code} - {ex.Message}");
}
```

## Features

- Strongly-typed request and response models
- Automatic rate limit header tracking
- Built-in retry support with exponential backoff
- Async/await pattern
- IDisposable pattern for HttpClient management
- Exception hierarchy for different error types
