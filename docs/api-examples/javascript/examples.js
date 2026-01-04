/**
 * Event Management API - Usage Examples
 * 
 * Run with: node examples.js
 */

import { EventManagementClient, ApiError, RateLimitError } from './client.js';

// Configuration - replace with your values
const API_KEY = process.env.EVENT_API_KEY || 'evtmgr_std_your_api_key_here';
const BASE_URL = process.env.EVENT_API_URL || 'http://localhost:5000';

// Create client
const client = new EventManagementClient({
  baseUrl: BASE_URL,
  apiKey: API_KEY
});

/**
 * Example 1: List events with pagination
 */
async function listEventsExample() {
  console.log('\n=== List Events Example ===\n');
  
  try {
    const response = await client.listEvents({
      page: 1,
      pageSize: 10,
      status: 'Published'
    });

    if (response.success) {
      console.log(`Found ${response.data.totalCount} events`);
      console.log(`Page ${response.data.currentPage} of ${response.data.totalPages}`);
      
      for (const event of response.data.items) {
        console.log(`- ${event.title} (${event.status})`);
        console.log(`  ID: ${event.id}`);
        console.log(`  Date: ${new Date(event.startDate).toLocaleDateString()}`);
        console.log(`  Registrations: ${event.currentRegistrations}/${event.maxCapacity}`);
      }
    }
  } catch (error) {
    handleError(error);
  }
}

/**
 * Example 2: Get single event
 */
async function getEventExample(eventId) {
  console.log('\n=== Get Event Example ===\n');
  
  try {
    const response = await client.getEvent(eventId);

    if (response.success) {
      const event = response.data;
      console.log(`Event: ${event.title}`);
      console.log(`Description: ${event.description}`);
      console.log(`Location: ${event.location || 'Virtual'}`);
      console.log(`Start: ${new Date(event.startDate).toLocaleString()}`);
      console.log(`End: ${new Date(event.endDate).toLocaleString()}`);
    }
  } catch (error) {
    handleError(error);
  }
}

/**
 * Example 3: Create a new event (requires Standard tier)
 */
async function createEventExample() {
  console.log('\n=== Create Event Example ===\n');
  
  try {
    const response = await client.createEvent({
      title: 'API Test Event',
      description: 'This event was created via the API',
      startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000), // 1 week from now
      endDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000 + 8 * 60 * 60 * 1000), // +8 hours
      location: 'Conference Center',
      maxCapacity: 100,
      isVirtual: false
    });

    if (response.success) {
      console.log(`Created event: ${response.data.title}`);
      console.log(`Event ID: ${response.data.id}`);
      return response.data.id;
    }
  } catch (error) {
    handleError(error);
  }
  return null;
}

/**
 * Example 4: Update an event (requires Standard tier)
 */
async function updateEventExample(eventId) {
  console.log('\n=== Update Event Example ===\n');
  
  try {
    const response = await client.updateEvent(eventId, {
      title: 'Updated API Test Event',
      description: 'This event was updated via the API',
      maxCapacity: 150
    });

    if (response.success) {
      console.log(`Updated event: ${response.data.title}`);
      console.log(`New capacity: ${response.data.maxCapacity}`);
    }
  } catch (error) {
    handleError(error);
  }
}

/**
 * Example 5: Delete an event (requires Premium tier)
 */
async function deleteEventExample(eventId) {
  console.log('\n=== Delete Event Example ===\n');
  
  try {
    const response = await client.deleteEvent(eventId);

    if (response.success) {
      console.log(`Event ${eventId} deleted successfully`);
    }
  } catch (error) {
    handleError(error);
  }
}

/**
 * Example 6: List social events for an event
 */
async function listSocialEventsExample(eventId) {
  console.log('\n=== List Social Events Example ===\n');
  
  try {
    const response = await client.listSocialEvents(eventId);

    if (response.success) {
      console.log(`Found ${response.data.length} social events`);
      
      for (const event of response.data) {
        console.log(`- ${event.name} (${event.status})`);
        console.log(`  Time: ${new Date(event.startTime).toLocaleString()}`);
        console.log(`  Location: ${event.location || 'TBD'}`);
        console.log(`  RSVPs: ${event.currentRsvps}/${event.maxCapacity}`);
      }
    }
  } catch (error) {
    handleError(error);
  }
}

/**
 * Example 7: Get API key info and rate limits
 */
async function getApiKeyInfoExample() {
  console.log('\n=== API Key Info Example ===\n');
  
  try {
    const response = await client.getApiKeyInfo();

    if (response.success) {
      const info = response.data;
      console.log(`API Key ID: ${info.apiKeyId}`);
      console.log(`Tier: ${info.tier}`);
      console.log(`Scopes: ${info.scopes.join(', ')}`);
      console.log(`Rate Limit: ${info.rateLimit} requests/minute`);
      console.log(`Current Usage: ${info.currentUsage}`);
      console.log(`Remaining: ${info.remainingRequests}`);
    }

    // Also show local rate limit tracking
    const rateLimit = client.getRateLimitInfo();
    console.log('\nRate limit from headers:');
    console.log(`  Limit: ${rateLimit.limit}`);
    console.log(`  Remaining: ${rateLimit.remaining}`);
    console.log(`  Reset: ${rateLimit.reset ? new Date(rateLimit.reset * 1000).toLocaleString() : 'N/A'}`);
    
    if (client.isNearRateLimit()) {
      console.log('\n⚠️ Warning: Approaching rate limit!');
    }
  } catch (error) {
    handleError(error);
  }
}

/**
 * Example 8: Handle rate limiting with retry
 */
async function rateLimitRetryExample() {
  console.log('\n=== Rate Limit Retry Example ===\n');
  
  const maxRetries = 3;
  let retries = 0;

  while (retries < maxRetries) {
    try {
      const response = await client.listEvents({ page: 1, pageSize: 1 });
      console.log('Request successful');
      return response;
    } catch (error) {
      if (error instanceof RateLimitError) {
        retries++;
        console.log(`Rate limited. Retry ${retries}/${maxRetries} after ${error.retryAfter}s`);
        
        if (retries < maxRetries) {
          // Wait before retrying (with exponential backoff)
          const delay = error.retryAfter * 1000 * Math.pow(2, retries - 1);
          console.log(`Waiting ${delay / 1000} seconds...`);
          await new Promise(resolve => setTimeout(resolve, delay));
        }
      } else {
        throw error;
      }
    }
  }
  
  console.log('Max retries exceeded');
  return null;
}

/**
 * Error handling helper
 */
function handleError(error) {
  if (error instanceof RateLimitError) {
    console.error(`❌ Rate limit exceeded!`);
    console.error(`   Retry after: ${error.retryAfter} seconds`);
    console.error(`   Current limit: ${error.rateLimit.limit}`);
  } else if (error instanceof ApiError) {
    console.error(`❌ API Error: ${error.message}`);
    console.error(`   Status: ${error.status}`);
    console.error(`   Code: ${error.code}`);
  } else {
    console.error(`❌ Unexpected error: ${error.message}`);
  }
}

/**
 * Run all examples
 */
async function runExamples() {
  console.log('Event Management API - JavaScript Examples');
  console.log('==========================================');
  console.log(`Base URL: ${BASE_URL}`);
  console.log(`API Key: ${API_KEY.substring(0, 20)}...`);

  // Run examples in sequence
  await getApiKeyInfoExample();
  await listEventsExample();
  
  // These require higher tiers, so they might fail with Free tier
  // Uncomment to test with appropriate tier
  
  // const eventId = await createEventExample();
  // if (eventId) {
  //   await getEventExample(eventId);
  //   await updateEventExample(eventId);
  //   await listSocialEventsExample(eventId);
  //   await deleteEventExample(eventId);
  // }

  console.log('\n✅ Examples completed!');
}

// Run if executed directly
runExamples().catch(console.error);
