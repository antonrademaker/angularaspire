"""
Event Management API - Usage Examples

Run with: python examples.py
"""

import os
import time
from datetime import datetime, timedelta
from client import EventManagementClient, ApiError, RateLimitError


# Configuration - replace with your values or use environment variables
API_KEY = os.getenv('EVENT_API_KEY', 'evtmgr_std_your_api_key_here')
BASE_URL = os.getenv('EVENT_API_URL', 'http://localhost:5000')


def list_events_example(client: EventManagementClient):
    """Example 1: List events with pagination"""
    print("\n=== List Events Example ===\n")
    
    try:
        response = client.list_events(
            page=1,
            page_size=10,
            status='Published'
        )
        
        if response['success']:
            data = response['data']
            print(f"Found {data['totalCount']} events")
            print(f"Page {data['currentPage']} of {data['totalPages']}")
            
            for event in data['items']:
                print(f"- {event['title']} ({event['status']})")
                print(f"  ID: {event['id']}")
                print(f"  Date: {event['startDate'][:10]}")
                print(f"  Registrations: {event['currentRegistrations']}/{event['maxCapacity']}")
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)


def get_event_example(client: EventManagementClient, event_id: str):
    """Example 2: Get single event"""
    print("\n=== Get Event Example ===\n")
    
    try:
        response = client.get_event(event_id)
        
        if response['success']:
            event = response['data']
            print(f"Event: {event['title']}")
            print(f"Description: {event.get('description', 'N/A')}")
            print(f"Location: {event.get('location') or 'Virtual'}")
            print(f"Start: {event['startDate']}")
            print(f"End: {event['endDate']}")
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)


def create_event_example(client: EventManagementClient) -> str | None:
    """Example 3: Create a new event (requires Standard tier)"""
    print("\n=== Create Event Example ===\n")
    
    try:
        start_date = datetime.now() + timedelta(days=7)  # 1 week from now
        end_date = start_date + timedelta(hours=8)
        
        response = client.create_event(
            title='API Test Event',
            description='This event was created via the Python API client',
            start_date=start_date,
            end_date=end_date,
            location='Conference Center',
            max_capacity=100,
            is_virtual=False
        )
        
        if response['success']:
            event = response['data']
            print(f"Created event: {event['title']}")
            print(f"Event ID: {event['id']}")
            return event['id']
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)
    
    return None


def update_event_example(client: EventManagementClient, event_id: str):
    """Example 4: Update an event (requires Standard tier)"""
    print("\n=== Update Event Example ===\n")
    
    try:
        response = client.update_event(
            event_id,
            title='Updated API Test Event',
            description='This event was updated via the Python API client',
            maxCapacity=150
        )
        
        if response['success']:
            event = response['data']
            print(f"Updated event: {event['title']}")
            print(f"New capacity: {event['maxCapacity']}")
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)


def delete_event_example(client: EventManagementClient, event_id: str):
    """Example 5: Delete an event (requires Premium tier)"""
    print("\n=== Delete Event Example ===\n")
    
    try:
        response = client.delete_event(event_id)
        
        if response['success']:
            print(f"Event {event_id} deleted successfully")
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)


def list_social_events_example(client: EventManagementClient, event_id: str):
    """Example 6: List social events for an event"""
    print("\n=== List Social Events Example ===\n")
    
    try:
        response = client.list_social_events(event_id)
        
        if response['success']:
            events = response['data']
            print(f"Found {len(events)} social events")
            
            for event in events:
                print(f"- {event['name']} ({event['status']})")
                print(f"  Time: {event['startTime']}")
                print(f"  Location: {event.get('location') or 'TBD'}")
                print(f"  RSVPs: {event['currentRsvps']}/{event['maxCapacity']}")
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)


def get_api_key_info_example(client: EventManagementClient):
    """Example 7: Get API key info and rate limits"""
    print("\n=== API Key Info Example ===\n")
    
    try:
        response = client.get_api_key_info()
        
        if response['success']:
            info = response['data']
            print(f"API Key ID: {info['apiKeyId']}")
            print(f"Tier: {info['tier']}")
            print(f"Scopes: {', '.join(info['scopes'])}")
            print(f"Rate Limit: {info['rateLimit']} requests/minute")
            print(f"Current Usage: {info['currentUsage']}")
            print(f"Remaining: {info['remainingRequests']}")
        
        # Also show local rate limit tracking
        rate_limit = client.get_rate_limit_info()
        print("\nRate limit from headers:")
        print(f"  Limit: {rate_limit.limit}")
        print(f"  Remaining: {rate_limit.remaining}")
        if rate_limit.reset:
            print(f"  Reset: {datetime.fromtimestamp(rate_limit.reset)}")
        
        if client.is_near_rate_limit():
            print("\n⚠️ Warning: Approaching rate limit!")
    
    except (ApiError, RateLimitError) as e:
        handle_error(e)


def rate_limit_retry_example(client: EventManagementClient):
    """Example 8: Handle rate limiting with retry"""
    print("\n=== Rate Limit Retry Example ===\n")
    
    max_retries = 3
    retries = 0
    
    while retries < max_retries:
        try:
            response = client.list_events(page=1, page_size=1)
            print("Request successful")
            return response
        
        except RateLimitError as e:
            retries += 1
            print(f"Rate limited. Retry {retries}/{max_retries} after {e.retry_after}s")
            
            if retries < max_retries:
                # Wait before retrying (with exponential backoff)
                delay = e.retry_after * (2 ** (retries - 1))
                print(f"Waiting {delay} seconds...")
                time.sleep(delay)
        
        except ApiError as e:
            handle_error(e)
            return None
    
    print("Max retries exceeded")
    return None


def handle_error(error):
    """Error handling helper"""
    if isinstance(error, RateLimitError):
        print(f"❌ Rate limit exceeded!")
        print(f"   Retry after: {error.retry_after} seconds")
        print(f"   Current limit: {error.rate_limit.get('limit')}")
    elif isinstance(error, ApiError):
        print(f"❌ API Error: {error.message}")
        print(f"   Status: {error.status}")
        print(f"   Code: {error.code}")
    else:
        print(f"❌ Unexpected error: {error}")


def main():
    """Run all examples"""
    print("Event Management API - Python Examples")
    print("=" * 42)
    print(f"Base URL: {BASE_URL}")
    print(f"API Key: {API_KEY[:20]}...")
    
    # Create client using context manager
    with EventManagementClient(BASE_URL, API_KEY) as client:
        # Run examples in sequence
        get_api_key_info_example(client)
        list_events_example(client)
        
        # These require higher tiers, so they might fail with Free tier
        # Uncomment to test with appropriate tier
        
        # event_id = create_event_example(client)
        # if event_id:
        #     get_event_example(client, event_id)
        #     update_event_example(client, event_id)
        #     list_social_events_example(client, event_id)
        #     delete_event_example(client, event_id)
    
    print("\n✅ Examples completed!")


if __name__ == '__main__':
    main()
