# Event Management API - cURL Examples

This directory contains cURL command examples for interacting with the Event Management External API.

## Prerequisites

- cURL installed on your system
- A valid API key (obtain from your account dashboard)

## Authentication

All requests must include the `X-Api-Key` header:

```bash
-H "X-Api-Key: your_api_key_here"
```

## Examples

### Get API Key Info

```bash
# Check your API key status and rate limits
curl -X GET "http://localhost:5000/api/external/me" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"
```

### List Events

```bash
# List all published events (paginated)
curl -X GET "http://localhost:5000/api/external/events?page=1&pageSize=10" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"

# Filter by status
curl -X GET "http://localhost:5000/api/external/events?status=Published" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"

# With verbose output to see rate limit headers
curl -v -X GET "http://localhost:5000/api/external/events" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"
```

### Get Single Event

```bash
# Get event by ID
curl -X GET "http://localhost:5000/api/external/events/{event_id}" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"
```

### Create Event (Standard+ Tier)

```bash
# Create a new event
curl -X POST "http://localhost:5000/api/external/events" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "title": "Tech Conference 2024",
    "description": "Annual technology conference",
    "startDate": "2024-06-15T09:00:00Z",
    "endDate": "2024-06-15T17:00:00Z",
    "location": "Convention Center",
    "maxCapacity": 500,
    "isVirtual": false
  }'
```

### Update Event (Standard+ Tier)

```bash
# Update an existing event
curl -X PUT "http://localhost:5000/api/external/events/{event_id}" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d '{
    "title": "Tech Conference 2024 - Updated",
    "maxCapacity": 600
  }'
```

### Delete Event (Premium+ Tier)

```bash
# Delete an event
curl -X DELETE "http://localhost:5000/api/external/events/{event_id}" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"
```

### List Social Events

```bash
# Get social events for a specific event
curl -X GET "http://localhost:5000/api/external/events/{event_id}/social-events" \
  -H "X-Api-Key: your_api_key_here" \
  -H "Accept: application/json"
```

## Rate Limit Headers

All responses include rate limiting information:

```
X-RateLimit-Limit: 100        # Max requests per minute
X-RateLimit-Remaining: 95     # Remaining requests
X-RateLimit-Reset: 1704067260 # Unix timestamp when limit resets
```

When rate limited (429 Too Many Requests):
```
Retry-After: 30               # Seconds to wait before retrying
```

## Error Response Format

```json
{
  "success": false,
  "error": {
    "code": "ERROR_CODE",
    "message": "Human-readable error message"
  }
}
```

## Common Error Codes

| Code | Description |
|------|-------------|
| `AUTHENTICATION_REQUIRED` | Missing API key |
| `INVALID_API_KEY` | Invalid or expired API key |
| `INSUFFICIENT_PERMISSIONS` | Missing required scope |
| `TIER_UPGRADE_REQUIRED` | Operation requires higher tier |
| `RATE_LIMIT_EXCEEDED` | Too many requests |
| `EVENT_NOT_FOUND` | Event does not exist |
| `VALIDATION_ERROR` | Request validation failed |

## Shell Scripts

### check-rate-limit.sh

```bash
#!/bin/bash
# Check your current rate limit status

API_KEY="${EVENT_API_KEY:-your_api_key_here}"
BASE_URL="${EVENT_API_URL:-http://localhost:5000}"

response=$(curl -s -i -X GET "$BASE_URL/api/external/me" \
  -H "X-Api-Key: $API_KEY" \
  -H "Accept: application/json")

echo "Rate Limit Headers:"
echo "$response" | grep -i "X-RateLimit"
echo ""
echo "Response Body:"
echo "$response" | tail -1 | jq '.'
```

### list-events.sh

```bash
#!/bin/bash
# List events with optional filtering

API_KEY="${EVENT_API_KEY:-your_api_key_here}"
BASE_URL="${EVENT_API_URL:-http://localhost:5000}"
PAGE="${1:-1}"
STATUS="${2:-Published}"

curl -s -X GET "$BASE_URL/api/external/events?page=$PAGE&status=$STATUS" \
  -H "X-Api-Key: $API_KEY" \
  -H "Accept: application/json" | jq '.'
```

### create-event.sh

```bash
#!/bin/bash
# Create a new event

API_KEY="${EVENT_API_KEY:-your_api_key_here}"
BASE_URL="${EVENT_API_URL:-http://localhost:5000}"

TITLE="${1:-Test Event}"
DESCRIPTION="${2:-Event created via shell script}"
START_DATE="${3:-$(date -d '+7 days' -Iseconds)}"
END_DATE="${4:-$(date -d '+7 days +8 hours' -Iseconds)}"

curl -s -X POST "$BASE_URL/api/external/events" \
  -H "X-Api-Key: $API_KEY" \
  -H "Content-Type: application/json" \
  -H "Accept: application/json" \
  -d "{
    \"title\": \"$TITLE\",
    \"description\": \"$DESCRIPTION\",
    \"startDate\": \"$START_DATE\",
    \"endDate\": \"$END_DATE\",
    \"maxCapacity\": 100,
    \"isVirtual\": false
  }" | jq '.'
```

## PowerShell Examples (Windows)

### List Events

```powershell
$ApiKey = $env:EVENT_API_KEY ?? "your_api_key_here"
$BaseUrl = $env:EVENT_API_URL ?? "http://localhost:5000"

$headers = @{
    "X-Api-Key" = $ApiKey
    "Accept" = "application/json"
}

$response = Invoke-RestMethod -Uri "$BaseUrl/api/external/events" `
    -Method Get -Headers $headers

$response.data.items | Format-Table id, title, status, startDate
```

### Create Event

```powershell
$ApiKey = $env:EVENT_API_KEY ?? "your_api_key_here"
$BaseUrl = $env:EVENT_API_URL ?? "http://localhost:5000"

$headers = @{
    "X-Api-Key" = $ApiKey
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

$body = @{
    title = "PowerShell Created Event"
    description = "Event created via PowerShell"
    startDate = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ")
    endDate = (Get-Date).AddDays(7).AddHours(8).ToString("yyyy-MM-ddTHH:mm:ssZ")
    maxCapacity = 100
    isVirtual = $false
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri "$BaseUrl/api/external/events" `
    -Method Post -Headers $headers -Body $body

Write-Host "Created event: $($response.data.id)"
```

## Tips

1. **Use environment variables** for API keys to avoid exposing them in command history
2. **Use `-v` flag** to see response headers including rate limits
3. **Use `jq`** for JSON formatting and parsing
4. **Handle 429 responses** by waiting for the `Retry-After` duration
5. **Check `X-RateLimit-Remaining`** before making bulk requests
