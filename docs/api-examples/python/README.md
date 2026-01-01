# Event Management API - Python Client Examples

Examples for integrating with the Event Management External API using Python.

## Requirements

- Python 3.8+
- requests library

## Installation

```bash
pip install -r requirements.txt
```

## Configuration

Set environment variables or update the configuration in the code:

```bash
export EVENT_API_KEY="your_api_key_here"
export EVENT_API_URL="https://api.example.com"
```

Or in Python:

```python
from client import EventManagementClient

client = EventManagementClient(
    base_url="https://api.example.com",
    api_key="evtmgr_std_..."
)
```

## Files

- `client.py` - Full-featured API client class
- `examples.py` - Usage examples for all endpoints
- `requirements.txt` - Dependencies

## Quick Start

```python
from client import EventManagementClient

# Create client
client = EventManagementClient(
    base_url="https://api.example.com",
    api_key="evtmgr_std_..."
)

# List events
response = client.list_events(page=1, page_size=10)
for event in response['data']['items']:
    print(f"- {event['title']}")

# Get single event
event = client.get_event("event-id-here")
print(event['data'])
```

## Error Handling

```python
from client import EventManagementClient, ApiError, RateLimitError

client = EventManagementClient(...)

try:
    events = client.list_events()
except RateLimitError as e:
    print(f"Rate limited! Retry after {e.retry_after} seconds")
except ApiError as e:
    print(f"API error: {e.message} (status: {e.status})")
```
