using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;

namespace Shared.Registration;

/// <summary>
/// Queue operation result
/// </summary>
public class QueueOperationResult
{
    /// <summary>
    /// Whether the operation was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Result message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Position in queue (if applicable)
    /// </summary>
    public int? Position { get; set; }

    /// <summary>
    /// Queue length after operation
    /// </summary>
    public long QueueLength { get; set; }

    /// <summary>
    /// Error code for specific failure types
    /// </summary>
    public string? ErrorCode { get; set; }
}

/// <summary>
/// Queue item data stored in Redis
/// </summary>
public class QueueItem
{
    /// <summary>
    /// Registration ID
    /// </summary>
    public int RegistrationId { get; set; }

    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Event ID
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Registration priority
    /// </summary>
    public RegistrationPriority Priority { get; set; }

    /// <summary>
    /// When the item was added to queue
    /// </summary>
    public DateTime QueuedAt { get; set; }

    /// <summary>
    /// Expiration timestamp
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Additional metadata
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Queue statistics
/// </summary>
public class QueueStatistics
{
    /// <summary>
    /// Event ID
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Current queue length
    /// </summary>
    public long QueueLength { get; set; }

    /// <summary>
    /// Queue items by priority
    /// </summary>
    public Dictionary<RegistrationPriority, long> QueueByPriority { get; set; } = new();

    /// <summary>
    /// Average wait time (in minutes)
    /// </summary>
    public double AverageWaitTimeMinutes { get; set; }

    /// <summary>
    /// Processing rate (items per minute)
    /// </summary>
    public double ProcessingRate { get; set; }

    /// <summary>
    /// Estimated time to process entire queue (in minutes)
    /// </summary>
    public double EstimatedClearTimeMinutes { get; set; }

    /// <summary>
    /// Oldest item in queue timestamp
    /// </summary>
    public DateTime? OldestItemTimestamp { get; set; }

    /// <summary>
    /// Queue health status
    /// </summary>
    public string HealthStatus { get; set; } = "Healthy";
}

/// <summary>
/// Redis-based queue service for managing event registration queues
/// Handles high-concurrency scenarios with distributed queue processing
/// </summary>
public interface IQueueService
{
    /// <summary>
    /// Add a registration to the event queue
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="userId">User ID</param>
    /// <param name="priority">Registration priority</param>
    /// <param name="expirationMinutes">Queue expiration in minutes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue operation result</returns>
    Task<QueueOperationResult> EnqueueRegistrationAsync(
        int registrationId,
        int eventId,
        Guid userId,
        RegistrationPriority priority = RegistrationPriority.Normal,
        int? expirationMinutes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove a registration from the queue
    /// </summary>
    /// <param name="registrationId">Registration ID to remove</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue operation result</returns>
    Task<QueueOperationResult> DequeueRegistrationAsync(
        int registrationId,
        int eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the next registration in queue for processing
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="count">Number of items to dequeue</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Next queue items or null if queue is empty</returns>
    Task<List<QueueItem>?> GetNextInQueueAsync(
        int eventId,
        int count = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get user's position in queue
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Position in queue (1-based) or null if not in queue</returns>
    Task<int?> GetQueuePositionAsync(
        int registrationId,
        int eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get queue statistics for an event
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Queue statistics</returns>
    Task<QueueStatistics> GetQueueStatisticsAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clear entire queue for an event (use with caution)
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items removed</returns>
    Task<long> ClearQueueAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired items from queue
    /// </summary>
    /// <param name="eventId">Event ID (null for all events)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of expired items removed</returns>
    Task<long> CleanupExpiredItemsAsync(
        int? eventId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a registration is in queue
    /// </summary>
    /// <param name="registrationId">Registration ID</param>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if in queue</returns>
    Task<bool> IsInQueueAsync(
        int registrationId,
        int eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update queue priorities (reorder queue based on new priority rules)
    /// </summary>
    /// <param name="eventId">Event ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of items reordered</returns>
    Task<int> UpdateQueuePrioritiesAsync(
        int eventId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get queue health status
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Health check results</returns>
    Task<Dictionary<string, object>> GetHealthStatusAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Redis implementation of queue service
/// </summary>
public class RedisQueueService : IQueueService, IDisposable
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;
    private readonly ILogger<RedisQueueService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _keyPrefix;
    private bool _disposed = false;

    // Redis key patterns
    private const string QUEUE_KEY_PATTERN = "event_queue:{0}"; // event_queue:123
    private const string QUEUE_ITEM_KEY_PATTERN = "queue_item:{0}:{1}"; // queue_item:123:456
    private const string QUEUE_STATS_KEY_PATTERN = "queue_stats:{0}"; // queue_stats:123
    private const string PROCESSING_RATE_KEY_PATTERN = "processing_rate:{0}"; // processing_rate:123

    // Queue scoring for priority ordering
    private const double PREMIUM_SCORE_MULTIPLIER = 1000000; // Premium priority
    private const double HIGH_SCORE_MULTIPLIER = 100000;    // High priority
    private const double NORMAL_SCORE_BASE = 10000;         // Normal priority base

    public RedisQueueService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisQueueService> logger,
        IConfiguration configuration)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _database = _connectionMultiplexer.GetDatabase();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _keyPrefix = _configuration.GetValue<string>("Redis:KeyPrefix") ?? "event_mgmt";

        _logger.LogInformation("Redis Queue Service initialized with key prefix: {KeyPrefix}", _keyPrefix);
    }

    /// <inheritdoc />
    public async Task<QueueOperationResult> EnqueueRegistrationAsync(
        int registrationId,
        int eventId,
        Guid userId,
        RegistrationPriority priority = RegistrationPriority.Normal,
        int? expirationMinutes = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);
            var itemKey = GetQueueItemKey(eventId, registrationId);

            // Check if already in queue
            if (await _database.SortedSetScoreAsync(queueKey, registrationId) != null)
            {
                return new QueueOperationResult
                {
                    Success = false,
                    Message = "Registration is already in queue",
                    ErrorCode = "ALREADY_QUEUED"
                };
            }

            // Create queue item
            var queueItem = new QueueItem
            {
                RegistrationId = registrationId,
                UserId = userId,
                EventId = eventId,
                Priority = priority,
                QueuedAt = DateTime.UtcNow,
                ExpiresAt = expirationMinutes.HasValue ? DateTime.UtcNow.AddMinutes(expirationMinutes.Value) : null
            };

            var itemJson = JsonSerializer.Serialize(queueItem);

            // Calculate score for priority ordering
            var score = CalculateQueueScore(priority, DateTime.UtcNow);

            // Use transaction to ensure atomicity
            var transaction = _database.CreateTransaction();

            // Add to sorted set (queue)
            transaction.SortedSetAddAsync(queueKey, registrationId, score);

            // Store item details
            transaction.StringSetAsync(itemKey, itemJson,
                expirationMinutes.HasValue ? TimeSpan.FromMinutes(expirationMinutes.Value + 10) : (TimeSpan?)null);

            // Update queue statistics
            _ = transaction.HashIncrementAsync(GetQueueStatsKey(eventId), "total_enqueued", 1);
            _ = transaction.HashIncrementAsync(GetQueueStatsKey(eventId), $"priority_{(int)priority}", 1);

            var success = await transaction.ExecuteAsync();

            if (success)
            {
                var queueLength = await _database.SortedSetLengthAsync(queueKey);
                var position = await GetQueuePositionAsync(registrationId, eventId, cancellationToken);

                _logger.LogInformation("Registration {RegistrationId} enqueued for event {EventId} with priority {Priority} at position {Position}",
                    registrationId, eventId, priority, position);

                return new QueueOperationResult
                {
                    Success = true,
                    Message = "Registration added to queue",
                    Position = position,
                    QueueLength = queueLength
                };
            }
            else
            {
                _logger.LogWarning("Failed to enqueue registration {RegistrationId} for event {EventId}",
                    registrationId, eventId);

                return new QueueOperationResult
                {
                    Success = false,
                    Message = "Failed to add registration to queue",
                    ErrorCode = "ENQUEUE_FAILED"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enqueueing registration {RegistrationId} for event {EventId}",
                registrationId, eventId);

            return new QueueOperationResult
            {
                Success = false,
                Message = "Internal error occurred",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    /// <inheritdoc />
    public async Task<QueueOperationResult> DequeueRegistrationAsync(
        int registrationId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);
            var itemKey = GetQueueItemKey(eventId, registrationId);

            // Use transaction for atomicity
            var transaction = _database.CreateTransaction();

            // Remove from queue
            var removedTask = transaction.SortedSetRemoveAsync(queueKey, registrationId);

            // Remove item details
            var deletedTask = transaction.KeyDeleteAsync(itemKey);

            // Update statistics
            _ = transaction.HashIncrementAsync(GetQueueStatsKey(eventId), "total_dequeued", 1);

            var success = await transaction.ExecuteAsync();

            if (success && await removedTask)
            {
                var queueLength = await _database.SortedSetLengthAsync(queueKey);

                _logger.LogInformation("Registration {RegistrationId} dequeued from event {EventId}",
                    registrationId, eventId);

                return new QueueOperationResult
                {
                    Success = true,
                    Message = "Registration removed from queue",
                    QueueLength = queueLength
                };
            }
            else
            {
                return new QueueOperationResult
                {
                    Success = false,
                    Message = "Registration not found in queue",
                    ErrorCode = "NOT_IN_QUEUE"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dequeuing registration {RegistrationId} from event {EventId}",
                registrationId, eventId);

            return new QueueOperationResult
            {
                Success = false,
                Message = "Internal error occurred",
                ErrorCode = "INTERNAL_ERROR"
            };
        }
    }

    /// <inheritdoc />
    public async Task<List<QueueItem>?> GetNextInQueueAsync(
        int eventId,
        int count = 1,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);

            // Get highest priority items (highest score first)
            var items = await _database.SortedSetRangeByScoreWithScoresAsync(
                queueKey,
                order: Order.Descending,
                take: count);

            if (items.Length == 0)
            {
                return new List<QueueItem>();
            }

            var queueItems = new List<QueueItem>();

            foreach (var item in items)
            {
                var registrationId = (int)item.Element;
                var itemKey = GetQueueItemKey(eventId, registrationId);

                var itemJson = await _database.StringGetAsync(itemKey);
                if (itemJson.HasValue)
                {
                    var queueItem = JsonSerializer.Deserialize<QueueItem>(itemJson.ToString());
                    if (queueItem != null)
                    {
                        queueItems.Add(queueItem);
                    }
                }
            }

            _logger.LogDebug("Retrieved {Count} next items from queue for event {EventId}",
                queueItems.Count, eventId);

            return queueItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting next items from queue for event {EventId}", eventId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<int?> GetQueuePositionAsync(
        int registrationId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);

            // Get rank (position) in sorted set - Redis returns 0-based, we want 1-based
            var rank = await _database.SortedSetRankAsync(queueKey, registrationId, Order.Descending);

            return rank.HasValue ? (int)rank.Value + 1 : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue position for registration {RegistrationId} in event {EventId}",
                registrationId, eventId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<QueueStatistics> GetQueueStatisticsAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);
            var statsKey = GetQueueStatsKey(eventId);

            var queueLength = await _database.SortedSetLengthAsync(queueKey);
            var stats = await _database.HashGetAllAsync(statsKey);

            var queueStats = new QueueStatistics
            {
                EventId = eventId,
                QueueLength = queueLength
            };

            // Parse statistics from Redis hash
            var statsDict = stats.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

            if (statsDict.TryGetValue("total_enqueued", out var totalEnqueued) &&
                int.TryParse(totalEnqueued, out var enqueuedCount))
            {
                // Calculate processing rate based on enqueued vs dequeued
                if (statsDict.TryGetValue("total_dequeued", out var totalDequeued) &&
                    int.TryParse(totalDequeued, out var dequeuedCount))
                {
                    queueStats.ProcessingRate = Math.Max(1, dequeuedCount / Math.Max(1, enqueuedCount / 60.0)); // per minute
                }
            }

            // Get priority distribution
            foreach (var priority in Enum.GetValues<RegistrationPriority>())
            {
                if (statsDict.TryGetValue($"priority_{(int)priority}", out var priorityCount) &&
                    long.TryParse(priorityCount, out var count))
                {
                    queueStats.QueueByPriority[priority] = count;
                }
            }

            // Calculate estimated clear time
            if (queueLength > 0 && queueStats.ProcessingRate > 0)
            {
                queueStats.EstimatedClearTimeMinutes = queueLength / queueStats.ProcessingRate;
            }

            // Get oldest item timestamp
            var oldestItem = await _database.SortedSetRangeByScoreWithScoresAsync(
                queueKey,
                order: Order.Ascending,
                take: 1);

            if (oldestItem.Length > 0)
            {
                var oldestRegistrationId = (int)oldestItem[0].Element;
                var itemKey = GetQueueItemKey(eventId, oldestRegistrationId);
                var itemJson = await _database.StringGetAsync(itemKey);

                if (itemJson.HasValue)
                {
                    var queueItem = JsonSerializer.Deserialize<QueueItem>(itemJson.ToString());
                    if (queueItem != null)
                    {
                        queueStats.OldestItemTimestamp = queueItem.QueuedAt;

                        // Calculate average wait time based on oldest item
                        var waitTime = (DateTime.UtcNow - queueItem.QueuedAt).TotalMinutes;
                        queueStats.AverageWaitTimeMinutes = waitTime;
                    }
                }
            }

            // Determine health status
            queueStats.HealthStatus = DetermineQueueHealth(queueStats);

            return queueStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting queue statistics for event {EventId}", eventId);
            return new QueueStatistics { EventId = eventId, HealthStatus = "Error" };
        }
    }

    /// <inheritdoc />
    public async Task<long> ClearQueueAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);
            var count = await _database.SortedSetLengthAsync(queueKey);

            // Get all items to clean up their detail keys
            var allItems = await _database.SortedSetRangeByScoreAsync(queueKey);

            // Delete the queue
            await _database.KeyDeleteAsync(queueKey);

            // Clean up individual item keys
            if (allItems.Length > 0)
            {
                var itemKeys = allItems.Select(item => (RedisKey)GetQueueItemKey(eventId, (int)item)).ToArray();
                await _database.KeyDeleteAsync(itemKeys);
            }

            // Reset statistics
            await _database.KeyDeleteAsync(GetQueueStatsKey(eventId));

            _logger.LogWarning("Cleared entire queue for event {EventId}, removed {Count} items",
                eventId, count);

            return count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing queue for event {EventId}", eventId);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<long> CleanupExpiredItemsAsync(
        int? eventId = null,
        CancellationToken cancellationToken = default)
    {
        long totalCleaned = 0;

        try
        {
            if (eventId.HasValue)
            {
                totalCleaned = await CleanupExpiredItemsForEventAsync(eventId.Value);
            }
            else
            {
                // Clean up all events - this is a simplified approach
                // In production, you might maintain a set of active event IDs
                _logger.LogWarning("Cleanup all events not implemented - specify eventId");
            }

            return totalCleaned;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired queue items");
            return totalCleaned;
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsInQueueAsync(
        int registrationId,
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);
            var score = await _database.SortedSetScoreAsync(queueKey, registrationId);
            return score.HasValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if registration {RegistrationId} is in queue for event {EventId}",
                registrationId, eventId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<int> UpdateQueuePrioritiesAsync(
        int eventId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var queueKey = GetQueueKey(eventId);
            var allItems = await _database.SortedSetRangeByScoreWithScoresAsync(queueKey);

            if (allItems.Length == 0) return 0;

            int updated = 0;

            foreach (var item in allItems)
            {
                var registrationId = (int)item.Element;
                var itemKey = GetQueueItemKey(eventId, registrationId);
                var itemJson = await _database.StringGetAsync(itemKey);

                if (itemJson.HasValue)
                {
                    var queueItem = JsonSerializer.Deserialize<QueueItem>(itemJson.ToString());
                    if (queueItem != null)
                    {
                        // Recalculate score based on current priority and queue time
                        var newScore = CalculateQueueScore(queueItem.Priority, queueItem.QueuedAt);

                        if (Math.Abs(newScore - item.Score) > 0.01) // Only update if score changed
                        {
                            await _database.SortedSetAddAsync(queueKey, registrationId, newScore);
                            updated++;
                        }
                    }
                }
            }

            _logger.LogInformation("Updated {Count} queue priorities for event {EventId}",
                updated, eventId);

            return updated;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating queue priorities for event {EventId}", eventId);
            return 0;
        }
    }

    /// <inheritdoc />
    public async Task<Dictionary<string, object>> GetHealthStatusAsync(
        CancellationToken cancellationToken = default)
    {
        var health = new Dictionary<string, object>
        {
            ["service"] = "RedisQueueService",
            ["timestamp"] = DateTime.UtcNow,
            ["status"] = "healthy"
        };

        try
        {
            // Test Redis connectivity
            var pingResult = await _database.PingAsync();
            health["redis_ping_ms"] = pingResult.TotalMilliseconds;
            health["redis_connected"] = _connectionMultiplexer.IsConnected;

            // Get Redis info
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.GetEndPoints()[0]);
            health["redis_memory_used"] = await server.InfoAsync("memory");

            health["status"] = "healthy";
        }
        catch (Exception ex)
        {
            health["status"] = "unhealthy";
            health["error"] = ex.Message;
            _logger.LogError(ex, "Queue service health check failed");
        }

        return health;
    }

    // Private helper methods

    private string GetQueueKey(int eventId) => $"{_keyPrefix}:" + string.Format(QUEUE_KEY_PATTERN, eventId);
    private string GetQueueItemKey(int eventId, int registrationId) => $"{_keyPrefix}:" + string.Format(QUEUE_ITEM_KEY_PATTERN, eventId, registrationId);
    private string GetQueueStatsKey(int eventId) => $"{_keyPrefix}:" + string.Format(QUEUE_STATS_KEY_PATTERN, eventId);
    private string GetProcessingRateKey(int eventId) => $"{_keyPrefix}:" + string.Format(PROCESSING_RATE_KEY_PATTERN, eventId);

    private static double CalculateQueueScore(RegistrationPriority priority, DateTime queuedAt)
    {
        // Higher score = higher priority (processed first)
        var baseScore = priority switch
        {
            RegistrationPriority.Premium => PREMIUM_SCORE_MULTIPLIER,
            RegistrationPriority.High => HIGH_SCORE_MULTIPLIER,
            RegistrationPriority.Normal => NORMAL_SCORE_BASE,
            _ => NORMAL_SCORE_BASE
        };

        // Subtract seconds from epoch to prioritize earlier registrations within same priority
        // This ensures FIFO within priority levels
        var timeComponent = (DateTimeOffset.MaxValue.ToUnixTimeSeconds() - ((DateTimeOffset)queuedAt).ToUnixTimeSeconds()) / 1000.0;

        return baseScore + timeComponent;
    }

    private async Task<long> CleanupExpiredItemsForEventAsync(int eventId)
    {
        var queueKey = GetQueueKey(eventId);
        var allItems = await _database.SortedSetRangeByScoreAsync(queueKey);

        long cleanedCount = 0;

        foreach (var item in allItems)
        {
            var registrationId = (int)item;
            var itemKey = GetQueueItemKey(eventId, registrationId);
            var itemJson = await _database.StringGetAsync(itemKey);

            if (itemJson.HasValue)
            {
                var queueItem = JsonSerializer.Deserialize<QueueItem>(itemJson.ToString());
                if (queueItem != null && queueItem.ExpiresAt.HasValue && queueItem.ExpiresAt < DateTime.UtcNow)
                {
                    // Remove expired item
                    await DequeueRegistrationAsync(registrationId, eventId);
                    cleanedCount++;
                }
            }
            else
            {
                // Orphaned queue entry - remove it
                await _database.SortedSetRemoveAsync(queueKey, registrationId);
                cleanedCount++;
            }
        }

        if (cleanedCount > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired queue items for event {EventId}",
                cleanedCount, eventId);
        }

        return cleanedCount;
    }

    private static string DetermineQueueHealth(QueueStatistics stats)
    {
        // Simple health determination logic
        if (stats.QueueLength > 10000) return "Critical"; // Very large queue
        if (stats.QueueLength > 1000) return "Warning";   // Large queue
        if (stats.AverageWaitTimeMinutes > 60) return "Warning"; // Long wait times

        return "Healthy";
    }

    /// <summary>
    /// Dispose resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            // Note: We don't dispose the connection multiplexer as it's shared
            // and managed by DI container
            _disposed = true;
        }
    }
}