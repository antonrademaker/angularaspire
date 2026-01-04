using System.Diagnostics.Metrics;

namespace EventManagement.ServiceDefaults;

/// <summary>
/// Custom metrics for the Event Management system.
/// These metrics are automatically exported to Application Insights and OTLP.
/// </summary>
public sealed class EventManagementMetrics : IDisposable
{
    private readonly Meter _meter;

    // Counters
    private readonly Counter<long> _eventsCreated;
    private readonly Counter<long> _registrationsCompleted;
    private readonly Counter<long> _registrationsFailed;
    private readonly Counter<long> _sessionsSubscribed;
    private readonly Counter<long> _apiKeyCreated;
    private readonly Counter<long> _apiRequests;
    private readonly Counter<long> _rateLimitExceeded;

    // Histograms
    private readonly Histogram<double> _registrationProcessingTime;
    private readonly Histogram<double> _eventSearchTime;
    private readonly Histogram<double> _queueWaitTime;

    // Observable gauges
    private int _activeRegistrations;
    private int _queuedRegistrations;
    private int _activeApiKeys;

    public EventManagementMetrics(IMeterFactory meterFactory)
    {
        _meter = meterFactory.Create("EventManagement");

        // Event metrics
        _eventsCreated = _meter.CreateCounter<long>(
            "eventmanagement.events.created",
            unit: "{event}",
            description: "Number of events created");

        // Registration metrics
        _registrationsCompleted = _meter.CreateCounter<long>(
            "eventmanagement.registrations.completed",
            unit: "{registration}",
            description: "Number of successful registrations");

        _registrationsFailed = _meter.CreateCounter<long>(
            "eventmanagement.registrations.failed",
            unit: "{registration}",
            description: "Number of failed registrations");

        _registrationProcessingTime = _meter.CreateHistogram<double>(
            "eventmanagement.registrations.processing_time",
            unit: "ms",
            description: "Time to process a registration");

        // Session metrics
        _sessionsSubscribed = _meter.CreateCounter<long>(
            "eventmanagement.sessions.subscribed",
            unit: "{subscription}",
            description: "Number of session subscriptions");

        // API metrics
        _apiKeyCreated = _meter.CreateCounter<long>(
            "eventmanagement.api.keys_created",
            unit: "{key}",
            description: "Number of API keys created");

        _apiRequests = _meter.CreateCounter<long>(
            "eventmanagement.api.requests",
            unit: "{request}",
            description: "Number of API requests");

        _rateLimitExceeded = _meter.CreateCounter<long>(
            "eventmanagement.api.rate_limit_exceeded",
            unit: "{request}",
            description: "Number of rate limit exceeded responses");

        // Search metrics
        _eventSearchTime = _meter.CreateHistogram<double>(
            "eventmanagement.search.time",
            unit: "ms",
            description: "Time to execute event search");

        // Queue metrics
        _queueWaitTime = _meter.CreateHistogram<double>(
            "eventmanagement.queue.wait_time",
            unit: "ms",
            description: "Time spent waiting in registration queue");

        // Observable gauges for current state
        _meter.CreateObservableGauge(
            "eventmanagement.registrations.active",
            () => _activeRegistrations,
            unit: "{registration}",
            description: "Number of active registrations being processed");

        _meter.CreateObservableGauge(
            "eventmanagement.registrations.queued",
            () => _queuedRegistrations,
            unit: "{registration}",
            description: "Number of registrations in queue");

        _meter.CreateObservableGauge(
            "eventmanagement.api.active_keys",
            () => _activeApiKeys,
            unit: "{key}",
            description: "Number of active API keys");
    }

    // Event metrics methods
    public void RecordEventCreated(string eventType = "general")
    {
        _eventsCreated.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    // Registration metrics methods
    public void RecordRegistrationCompleted(string eventType = "general")
    {
        _registrationsCompleted.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }

    public void RecordRegistrationFailed(string reason)
    {
        _registrationsFailed.Add(1, new KeyValuePair<string, object?>("reason", reason));
    }

    public void RecordRegistrationProcessingTime(double milliseconds)
    {
        _registrationProcessingTime.Record(milliseconds);
    }

    public void SetActiveRegistrations(int count)
    {
        _activeRegistrations = count;
    }

    public void SetQueuedRegistrations(int count)
    {
        _queuedRegistrations = count;
    }

    // Session metrics methods
    public void RecordSessionSubscribed(string trackName = "general")
    {
        _sessionsSubscribed.Add(1, new KeyValuePair<string, object?>("track", trackName));
    }

    // API metrics methods
    public void RecordApiKeyCreated(string tier)
    {
        _apiKeyCreated.Add(1, new KeyValuePair<string, object?>("tier", tier));
    }

    public void RecordApiRequest(string endpoint, string method, int statusCode)
    {
        _apiRequests.Add(1,
            new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method),
            new KeyValuePair<string, object?>("status_code", statusCode));
    }

    public void RecordRateLimitExceeded(string tier)
    {
        _rateLimitExceeded.Add(1, new KeyValuePair<string, object?>("tier", tier));
    }

    public void SetActiveApiKeys(int count)
    {
        _activeApiKeys = count;
    }

    // Search metrics methods
    public void RecordEventSearchTime(double milliseconds, int resultCount)
    {
        _eventSearchTime.Record(milliseconds,
            new KeyValuePair<string, object?>("result_count", resultCount));
    }

    // Queue metrics methods
    public void RecordQueueWaitTime(double milliseconds)
    {
        _queueWaitTime.Record(milliseconds);
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
