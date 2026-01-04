namespace Shared.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsSuccess { get; protected set; }

    /// <summary>
    /// Indicates whether the operation failed
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Error message if the operation failed
    /// </summary>
    public string? Error { get; protected set; }

    /// <summary>
    /// Additional error details or validation errors
    /// </summary>
    public IEnumerable<string>? Errors { get; protected set; }

    /// <summary>
    /// Protected constructor for creating results
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="error">Error message if failed</param>
    /// <param name="errors">Additional errors if any</param>
    protected Result(bool isSuccess, string? error = null, IEnumerable<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Error = error;
        Errors = errors;
    }

    /// <summary>
    /// Creates a successful result
    /// </summary>
    /// <returns>Success result</returns>
    public static Result Success()
    {
        return new Result(true);
    }

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    /// <param name="error">Error message</param>
    /// <returns>Failure result</returns>
    public static Result Failure(string error)
    {
        return new Result(false, error);
    }

    /// <summary>
    /// Creates a failed result with multiple errors
    /// </summary>
    /// <param name="errors">Collection of error messages</param>
    /// <returns>Failure result</returns>
    public static Result Failure(IEnumerable<string> errors)
    {
        return new Result(false, errors.FirstOrDefault(), errors);
    }

    /// <summary>
    /// Implicitly converts a boolean to a Result
    /// </summary>
    /// <param name="success">Whether the operation succeeded</param>
    public static implicit operator Result(bool success)
    {
        return success ? Success() : Failure("Operation failed");
    }
}

/// <summary>
/// Represents the result of an operation that returns a value and can either succeed or fail
/// </summary>
/// <typeparam name="T">The type of value returned on success</typeparam>
public class Result<T> : Result
{
    /// <summary>
    /// The value returned on success
    /// </summary>
    public T? Value { get; private set; }

    /// <summary>
    /// Protected constructor for creating results with values
    /// </summary>
    /// <param name="isSuccess">Whether the operation succeeded</param>
    /// <param name="value">The value returned on success</param>
    /// <param name="error">Error message if failed</param>
    /// <param name="errors">Additional errors if any</param>
    protected Result(bool isSuccess, T? value = default, string? error = null, IEnumerable<string>? errors = null)
        : base(isSuccess, error, errors)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a successful result with a value
    /// </summary>
    /// <param name="value">The value to return</param>
    /// <returns>Success result with value</returns>
    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value);
    }

    /// <summary>
    /// Creates a failed result with an error message
    /// </summary>
    /// <param name="error">Error message</param>
    /// <returns>Failure result</returns>
    public new static Result<T> Failure(string error)
    {
        return new Result<T>(false, default, error);
    }

    /// <summary>
    /// Creates a failed result with multiple errors
    /// </summary>
    /// <param name="errors">Collection of error messages</param>
    /// <returns>Failure result</returns>
    public new static Result<T> Failure(IEnumerable<string> errors)
    {
        return new Result<T>(false, default, errors.FirstOrDefault(), errors);
    }

    /// <summary>
    /// Implicitly converts a value to a successful Result
    /// </summary>
    /// <param name="value">The value to wrap in a successful result</param>
    public static implicit operator Result<T>(T value)
    {
        return Success(value);
    }
}

/// <summary>
/// Represents a paged result containing items and pagination information
/// </summary>
/// <typeparam name="T">The type of items in the result</typeparam>
/// <remarks>
/// Initializes a new PagedResult
/// </remarks>
/// <param name="items">The items on the current page</param>
/// <param name="totalCount">Total number of items</param>
/// <param name="currentPage">Current page number</param>
/// <param name="pageSize">Items per page</param>
public class PagedResult<T>(IEnumerable<T> items, int totalCount, int currentPage, int pageSize)
{
    /// <summary>
    /// The items on the current page
    /// </summary>
    public IEnumerable<T> Items { get; set; } = items;

    /// <summary>
    /// Total number of items across all pages
    /// </summary>
    public int TotalCount { get; set; } = totalCount;

    /// <summary>
    /// Current page number (1-based)
    /// </summary>
    public int CurrentPage { get; set; } = currentPage;

    /// <summary>
    /// Number of items per page
    /// </summary>
    public int PageSize { get; set; } = pageSize;

    /// <summary>
    /// Total number of pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool HasPrevious => CurrentPage > 1;

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool HasNext => CurrentPage < TotalPages;
}

/// <summary>
/// Base class for paginated requests
/// </summary>
public class PagedRequest
{
    private int _page = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Number of items per page (max 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            < 1 => 20,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Number of items to skip for pagination
    /// </summary>
    public int Skip => (Page - 1) * PageSize;
}
