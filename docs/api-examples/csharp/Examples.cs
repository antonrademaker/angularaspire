using EventManagement.Client;

// Example usage of the Event Management API client

var apiKey = Environment.GetEnvironmentVariable("EVENT_API_KEY") ?? "your_api_key_here";
var baseUrl = Environment.GetEnvironmentVariable("EVENT_API_URL") ?? "http://localhost:5000";

Console.WriteLine("Event Management API - C# Examples");
Console.WriteLine(new string('=', 42));

using var client = new EventManagementClient(baseUrl, apiKey);

try
{
    // Example 1: Get API Key Info
    Console.WriteLine("\n=== API Key Info ===\n");
    var keyInfo = await client.GetApiKeyInfoAsync();
    Console.WriteLine($"Tier: {keyInfo.Data!.Tier}");
    Console.WriteLine($"Rate Limit: {keyInfo.Data.RateLimit}/min");
    Console.WriteLine($"Remaining: {keyInfo.Data.RemainingRequests}");
    Console.WriteLine($"Scopes: {string.Join(", ", keyInfo.Data.Scopes)}");

    // Example 2: List Events
    Console.WriteLine("\n=== List Events ===\n");
    var events = await client.ListEventsAsync(page: 1, pageSize: 10, status: "Published");
    Console.WriteLine($"Found {events.Data!.TotalCount} events");
    foreach (var evt in events.Data.Items.Take(5))
    {
        Console.WriteLine($"- {evt.Title} ({evt.Status})");
        Console.WriteLine($"  Date: {evt.StartDate:yyyy-MM-dd}");
        Console.WriteLine($"  Registrations: {evt.CurrentRegistrations}/{evt.MaxCapacity}");
    }

    // Example 3: Check rate limits
    Console.WriteLine("\n=== Rate Limit Status ===\n");
    var rateLimits = client.GetRateLimitInfo();
    Console.WriteLine($"Limit: {rateLimits.Limit}");
    Console.WriteLine($"Remaining: {rateLimits.Remaining}");
    Console.WriteLine($"Reset: {rateLimits.Reset:HH:mm:ss}");
    
    if (client.IsNearRateLimit(threshold: 20))
    {
        Console.WriteLine("⚠️ Approaching rate limit!");
    }

    // Example 4: Create Event (requires Standard tier)
    // Uncomment to test
    /*
    Console.WriteLine("\n=== Create Event ===\n");
    var newEvent = await client.CreateEventAsync(new CreateEventRequest
    {
        Title = "C# Client Test Event",
        Description = "Event created via C# client",
        StartDate = DateTime.UtcNow.AddDays(7),
        EndDate = DateTime.UtcNow.AddDays(7).AddHours(8),
        Location = "Conference Center",
        MaxCapacity = 100,
        IsVirtual = false
    });
    Console.WriteLine($"Created event: {newEvent.Data!.Id}");

    // Example 5: Get single event
    var singleEvent = await client.GetEventAsync(newEvent.Data.Id);
    Console.WriteLine($"Retrieved: {singleEvent.Data!.Title}");

    // Example 6: Update Event
    var updatedEvent = await client.UpdateEventAsync(newEvent.Data.Id, new UpdateEventRequest
    {
        Title = "Updated C# Client Event",
        MaxCapacity = 150
    });
    Console.WriteLine($"Updated: {updatedEvent.Data!.Title}");

    // Example 7: List social events
    var socialEvents = await client.ListSocialEventsAsync(newEvent.Data.Id);
    Console.WriteLine($"Found {socialEvents.Data!.Count} social events");

    // Example 8: Delete Event (requires Premium tier)
    // await client.DeleteEventAsync(newEvent.Data.Id);
    // Console.WriteLine("Event deleted");
    */

    Console.WriteLine("\n✅ Examples completed!");
}
catch (RateLimitException ex)
{
    Console.WriteLine($"\n❌ Rate limit exceeded!");
    Console.WriteLine($"   Retry after: {ex.RetryAfter} seconds");
    Console.WriteLine($"   Current limit: {ex.RateLimitInfo.Limit}");
}
catch (ApiException ex)
{
    Console.WriteLine($"\n❌ API Error: {ex.Code}");
    Console.WriteLine($"   Message: {ex.Message}");
    Console.WriteLine($"   Status: {ex.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine($"\n❌ Unexpected error: {ex.Message}");
}
