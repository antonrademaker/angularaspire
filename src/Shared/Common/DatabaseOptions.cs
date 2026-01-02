namespace Shared.Common;

/// <summary>
/// Configuration options for database behavior.
/// Used to configure test vs production database modes.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Configuration section name for binding
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// When true, indicates the application is running with an in-memory database (test mode).
    /// Services can use this to skip features not supported by InMemory provider
    /// (e.g., transactions, certain Include patterns, Redis operations).
    /// </summary>
    public bool UseInMemoryDatabase { get; set; }
}
