namespace PrivateApi.SessionManagement;

/// <summary>
/// Standardized API error response
/// </summary>
public class ApiError
{
    public string Code { get; set; }
    public string Message { get; set; }
    public Dictionary<string, object>? Details { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public ApiError(string code, string message, Dictionary<string, object>? details = null)
    {
        Code = code;
        Message = message;
        Details = details;
    }
}