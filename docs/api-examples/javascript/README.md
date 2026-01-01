# JavaScript/TypeScript API Client Examples

Examples for integrating with the Event Management External API using JavaScript/TypeScript.

## Installation

```bash
npm install
```

## Configuration

Set your API key and base URL in the environment or directly in the code:

```javascript
const API_KEY = 'your_api_key_here';
const BASE_URL = 'https://api.example.com';
```

## Files

- `client.js` - Full-featured API client class
- `examples.js` - Usage examples for all endpoints
- `package.json` - Dependencies

## Quick Start

```javascript
import { EventManagementClient } from './client.js';

const client = new EventManagementClient({
  baseUrl: 'https://api.example.com',
  apiKey: 'evtmgr_std_...'
});

// List events
const events = await client.listEvents({ page: 1, pageSize: 10 });
console.log(events.data.items);

// Get single event
const event = await client.getEvent('event-id-here');
console.log(event.data);
```

## Browser Usage

The client works in both Node.js and browser environments. For browsers, you may need to handle CORS:

```html
<script type="module">
  import { EventManagementClient } from './client.js';
  
  const client = new EventManagementClient({
    baseUrl: 'https://api.example.com',
    apiKey: 'evtmgr_std_...'
  });
  
  // Use the client
</script>
```
