using System.Net;

namespace PrivateApi.Middleware;

public class FileUploadValidator
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FileUploadValidator> _logger;
    private const long MaxFileSize = 50 * 1024 * 1024; // 50MB
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
    
    // Magic numbers for file signature validation
    private static readonly Dictionary<string, List<byte[]>> FileSignatures = new()
    {
        { ".jpg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".jpeg", new List<byte[]> { new byte[] { 0xFF, 0xD8, 0xFF } } },
        { ".png", new List<byte[]> { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } } },
        { ".gif", new List<byte[]> { new byte[] { 0x47, 0x49, 0x46, 0x38 } } },
        { ".pdf", new List<byte[]> { new byte[] { 0x25, 0x50, 0x44, 0x46 } } }
    };

    public FileUploadValidator(RequestDelegate next, ILogger<FileUploadValidator> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            await _next(context);
            return;
        }

        var form = await context.Request.ReadFormAsync();

        if (form.Files.Count == 0)
        {
            await _next(context);
            return;
        }

        foreach (var file in form.Files)
        {
            // 1. Validate Size
            if (file.Length > MaxFileSize)
            {
                _logger.LogWarning("File upload rejected: {FileName} exceeds size limit ({Size} bytes)", file.FileName, file.Length);
                context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
                await context.Response.WriteAsJsonAsync(new { error = "File too large", message = $"File {file.FileName} exceeds the 50MB limit." });
                return;
            }

            // 2. Validate Extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("File upload rejected: {FileName} has invalid extension {Extension}", file.FileName, extension);
                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid file type", message = $"File type {extension} is not allowed." });
                return;
            }

            // 3. Validate Signature (Magic Numbers)
            if (!IsValidSignature(file, extension))
            {
                _logger.LogWarning("File upload rejected: {FileName} signature does not match extension {Extension}", file.FileName, extension);
                context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid file content", message = "File content does not match its extension." });
                return;
            }
        }

        await _next(context);
    }

    private bool IsValidSignature(IFormFile file, string extension)
    {
        if (!FileSignatures.TryGetValue(extension, out var signatures))
        {
            return true; // No signature check for this type (or unknown), strictly relying on extension if not in map
        }

        using var reader = new BinaryReader(file.OpenReadStream());
        if (file.Length == 0) return false;

        var headerBytes = reader.ReadBytes(signatures.Max(m => m.Length));

        return signatures.Any(signature => 
            headerBytes.Take(signature.Length).SequenceEqual(signature));
    }
}

public static class FileUploadValidatorExtensions
{
    public static IApplicationBuilder UseFileUploadValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<FileUploadValidator>();
    }
}
