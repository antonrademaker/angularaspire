"""
Event Management API Client

A Python client for the Event Management External API.
"""

import requests
from typing import Optional, Dict, Any, List
from datetime import datetime
from dataclasses import dataclass


class ApiError(Exception):
    """API Error exception"""
    
    def __init__(self, message: str, status: int, code: str):
        super().__init__(message)
        self.message = message
        self.status = status
        self.code = code


class RateLimitError(ApiError):
    """Rate Limit Error exception"""
    
    def __init__(self, message: str, retry_after: int, rate_limit: Dict[str, int]):
        super().__init__(message, 429, "RATE_LIMIT_EXCEEDED")
        self.retry_after = retry_after
        self.rate_limit = rate_limit


@dataclass
class RateLimitInfo:
    """Rate limit information from response headers"""
    limit: Optional[int] = None
    remaining: Optional[int] = None
    reset: Optional[int] = None


class EventManagementClient:
    """
    Event Management External API Client
    
    Args:
        base_url: The base URL of the API
        api_key: Your API key
        timeout: Request timeout in seconds (default: 30)
    """
    
    def __init__(self, base_url: str, api_key: str, timeout: int = 30):
        self.base_url = base_url.rstrip('/')
        self.api_key = api_key
        self.timeout = timeout
        self.rate_limit = RateLimitInfo()
        
        # Create session for connection reuse
        self.session = requests.Session()
        self.session.headers.update({
            'X-Api-Key': self.api_key,
            'Content-Type': 'application/json',
            'Accept': 'application/json'
        })
    
    def _request(
        self,
        method: str,
        endpoint: str,
        params: Optional[Dict] = None,
        json: Optional[Dict] = None
    ) -> Dict[str, Any]:
        """Make an API request"""
        
        url = f"{self.base_url}{endpoint}"
        
        # Filter out None values from params
        if params:
            params = {k: v for k, v in params.items() if v is not None}
        
        try:
            response = self.session.request(
                method=method,
                url=url,
                params=params,
                json=json,
                timeout=self.timeout
            )
            
            # Update rate limit info
            self.rate_limit.limit = int(response.headers.get('X-RateLimit-Limit', 0)) or None
            self.rate_limit.remaining = int(response.headers.get('X-RateLimit-Remaining', 0)) or None
            self.rate_limit.reset = int(response.headers.get('X-RateLimit-Reset', 0)) or None
            
            # Handle rate limiting
            if response.status_code == 429:
                retry_after = int(response.headers.get('Retry-After', 60))
                error_data = response.json()
                raise RateLimitError(
                    error_data.get('message', 'Rate limit exceeded'),
                    retry_after,
                    {
                        'limit': self.rate_limit.limit,
                        'remaining': self.rate_limit.remaining,
                        'reset': self.rate_limit.reset
                    }
                )
            
            # Handle other errors
            if not response.ok:
                error_data = response.json()
                error_info = error_data.get('error', {})
                raise ApiError(
                    error_info.get('message', error_data.get('message', 'Unknown error')),
                    response.status_code,
                    error_info.get('code', 'UNKNOWN_ERROR')
                )
            
            # Handle no content
            if response.status_code == 204:
                return {'success': True, 'data': None, 'timestamp': datetime.utcnow().isoformat()}
            
            return response.json()
            
        except requests.exceptions.RequestException as e:
            raise ApiError(str(e), 0, 'NETWORK_ERROR')
    
    # ============ Events API ============
    
    def list_events(
        self,
        page: int = 1,
        page_size: int = 20,
        status: Optional[str] = None,
        from_date: Optional[datetime] = None,
        to_date: Optional[datetime] = None
    ) -> Dict[str, Any]:
        """
        List all events with pagination
        
        Args:
            page: Page number (default: 1)
            page_size: Items per page (default: 20, max: 100)
            status: Filter by event status
            from_date: Filter events starting after this date
            to_date: Filter events starting before this date
            
        Returns:
            API response with paginated events
        """
        params = {
            'page': page,
            'pageSize': page_size,
            'status': status,
            'fromDate': from_date.isoformat() if from_date else None,
            'toDate': to_date.isoformat() if to_date else None
        }
        return self._request('GET', '/api/external/events', params=params)
    
    def get_event(self, event_id: str) -> Dict[str, Any]:
        """
        Get a single event by ID
        
        Args:
            event_id: Event ID
            
        Returns:
            API response with event data
        """
        return self._request('GET', f'/api/external/events/{event_id}')
    
    def create_event(
        self,
        title: str,
        start_date: datetime,
        end_date: datetime,
        description: Optional[str] = None,
        location: Optional[str] = None,
        venue_address: Optional[str] = None,
        is_virtual: bool = False,
        meeting_url: Optional[str] = None,
        max_capacity: int = 100
    ) -> Dict[str, Any]:
        """
        Create a new event (requires Standard tier or above)
        
        Args:
            title: Event title
            start_date: Event start date
            end_date: Event end date
            description: Event description
            location: Event location
            venue_address: Venue address
            is_virtual: Whether the event is virtual
            meeting_url: Virtual meeting URL
            max_capacity: Maximum attendees
            
        Returns:
            API response with created event
        """
        body = {
            'title': title,
            'description': description,
            'startDate': start_date.isoformat(),
            'endDate': end_date.isoformat(),
            'location': location,
            'venueAddress': venue_address,
            'isVirtual': is_virtual,
            'meetingUrl': meeting_url,
            'maxCapacity': max_capacity
        }
        return self._request('POST', '/api/external/events', json=body)
    
    def update_event(self, event_id: str, **updates) -> Dict[str, Any]:
        """
        Update an existing event (requires Standard tier or above)
        
        Args:
            event_id: Event ID
            **updates: Fields to update
            
        Returns:
            API response with updated event
        """
        # Convert datetime objects to ISO format
        if 'start_date' in updates and isinstance(updates['start_date'], datetime):
            updates['startDate'] = updates.pop('start_date').isoformat()
        if 'end_date' in updates and isinstance(updates['end_date'], datetime):
            updates['endDate'] = updates.pop('end_date').isoformat()
        
        return self._request('PUT', f'/api/external/events/{event_id}', json=updates)
    
    def delete_event(self, event_id: str) -> Dict[str, Any]:
        """
        Delete an event (requires Premium tier or above)
        
        Args:
            event_id: Event ID
            
        Returns:
            API response
        """
        return self._request('DELETE', f'/api/external/events/{event_id}')
    
    # ============ Social Events API ============
    
    def list_social_events(self, event_id: str) -> Dict[str, Any]:
        """
        List social events for a parent event
        
        Args:
            event_id: Parent event ID
            
        Returns:
            API response with social events
        """
        return self._request('GET', f'/api/external/events/{event_id}/social-events')
    
    def get_social_event(self, social_event_id: str) -> Dict[str, Any]:
        """
        Get a single social event by ID
        
        Args:
            social_event_id: Social event ID
            
        Returns:
            API response with social event data
        """
        return self._request('GET', f'/api/external/social-events/{social_event_id}')
    
    # ============ API Key Info ============
    
    def get_api_key_info(self) -> Dict[str, Any]:
        """
        Get API key information and rate limit status
        
        Returns:
            API response with API key info
        """
        return self._request('GET', '/api/external/me')
    
    # ============ Utilities ============
    
    def get_rate_limit_info(self) -> RateLimitInfo:
        """
        Get current rate limit information (from last request)
        
        Returns:
            RateLimitInfo object
        """
        return self.rate_limit
    
    def is_near_rate_limit(self, threshold: float = 0.1) -> bool:
        """
        Check if approaching rate limit
        
        Args:
            threshold: Threshold percentage (default: 10%)
            
        Returns:
            True if near rate limit
        """
        if not self.rate_limit.limit or not self.rate_limit.remaining:
            return False
        return (self.rate_limit.remaining / self.rate_limit.limit) < threshold
    
    def close(self):
        """Close the session"""
        self.session.close()
    
    def __enter__(self):
        return self
    
    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close()
