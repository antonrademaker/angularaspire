# Event Management API - Client Examples

This directory contains example code for integrating with the Event Management External API.

## Available Examples

### Language-Specific Examples

- [**JavaScript/TypeScript**](./javascript/) - Node.js and browser examples
- [**Python**](./python/) - Python 3 examples using requests library
- [**C#/.NET**](./csharp/) - .NET examples using HttpClient
- [**cURL**](./curl/) - Command-line examples

### Getting Started

1. **Get an API Key**: Contact your administrator or create one from the admin portal
2. **Choose your language**: Pick the example that matches your technology stack
3. **Install dependencies**: Follow the README in each language directory
4. **Update configuration**: Set your API key and base URL
5. **Run the examples**: Test the integration with your environment

## Authentication

All API requests require authentication via API key. Include your key in one of these ways:

```bash
# Header (recommended)
X-Api-Key: your_api_key_here

# Bearer token
Authorization: Bearer your_api_key_here

# Query parameter (not recommended for production)
?api_key=your_api_key_here
```

## Rate Limits

| Tier | Requests/Minute | Access Level |
|------|-----------------|--------------|
| Free | 100 | Read-only |
| Standard | 500 | Read/Write |
| Premium | 2000 | Full access |
| Enterprise | 10000 | Full access + priority |

## Response Headers

Monitor these headers to track your usage:

- `X-RateLimit-Limit` - Your rate limit
- `X-RateLimit-Remaining` - Requests remaining
- `X-RateLimit-Reset` - Unix timestamp when limit resets

## Error Handling

All examples include error handling for common scenarios:

- `401 Unauthorized` - Invalid or missing API key
- `403 Forbidden` - Insufficient permissions
- `429 Too Many Requests` - Rate limit exceeded
- `404 Not Found` - Resource not found

## Support

For API issues or questions:
- Check the API documentation at `/api/docs`
- Review the OpenAPI specification at `/openapi/v1.json`
- Contact support with your API key ID (not the key itself)
