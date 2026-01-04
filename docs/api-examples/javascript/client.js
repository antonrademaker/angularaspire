/**
 * Event Management API Client
 * 
 * A JavaScript/TypeScript client for the Event Management External API.
 * Works in both Node.js and browser environments.
 */

/**
 * @typedef {Object} ClientConfig
 * @property {string} baseUrl - The base URL of the API
 * @property {string} apiKey - Your API key
 * @property {number} [timeout] - Request timeout in milliseconds (default: 30000)
 */

/**
 * @typedef {Object} PagedResult
 * @property {Array} items - The items in the current page
 * @property {number} totalCount - Total number of items
 * @property {number} currentPage - Current page number
 * @property {number} pageSize - Items per page
 * @property {number} totalPages - Total number of pages
 * @property {boolean} hasNextPage - Whether there are more pages
 * @property {boolean} hasPreviousPage - Whether there are previous pages
 */

/**
 * @typedef {Object} ApiResponse
 * @property {boolean} success - Whether the request was successful
 * @property {*} [data] - Response data (if success)
 * @property {Object} [error] - Error details (if failure)
 * @property {string} timestamp - Response timestamp
 */

export class EventManagementClient {
  /**
   * Create a new API client instance
   * @param {ClientConfig} config - Client configuration
   */
  constructor(config) {
    this.baseUrl = config.baseUrl.replace(/\/$/, '');
    this.apiKey = config.apiKey;
    this.timeout = config.timeout || 30000;
    
    // Track rate limit info from last response
    this.rateLimit = {
      limit: null,
      remaining: null,
      reset: null
    };
  }

  /**
   * Make an API request
   * @param {string} method - HTTP method
   * @param {string} endpoint - API endpoint
   * @param {Object} [options] - Request options
   * @returns {Promise<ApiResponse>}
   */
  async request(method, endpoint, options = {}) {
    const url = new URL(`${this.baseUrl}${endpoint}`);
    
    // Add query parameters
    if (options.params) {
      Object.entries(options.params).forEach(([key, value]) => {
        if (value !== undefined && value !== null) {
          url.searchParams.append(key, String(value));
        }
      });
    }

    const headers = {
      'X-Api-Key': this.apiKey,
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      ...options.headers
    };

    const fetchOptions = {
      method,
      headers,
      signal: AbortSignal.timeout(this.timeout)
    };

    if (options.body && method !== 'GET') {
      fetchOptions.body = JSON.stringify(options.body);
    }

    try {
      const response = await fetch(url.toString(), fetchOptions);
      
      // Update rate limit info
      this.rateLimit = {
        limit: parseInt(response.headers.get('X-RateLimit-Limit')) || null,
        remaining: parseInt(response.headers.get('X-RateLimit-Remaining')) || null,
        reset: parseInt(response.headers.get('X-RateLimit-Reset')) || null
      };

      // Handle rate limiting
      if (response.status === 429) {
        const retryAfter = parseInt(response.headers.get('Retry-After')) || 60;
        const error = await response.json();
        throw new RateLimitError(error.message, retryAfter, this.rateLimit);
      }

      // Handle other errors
      if (!response.ok) {
        const error = await response.json();
        throw new ApiError(
          error.error?.message || error.message || 'Unknown error',
          response.status,
          error.error?.code || 'UNKNOWN_ERROR'
        );
      }

      // Handle no content
      if (response.status === 204) {
        return { success: true, data: null, timestamp: new Date().toISOString() };
      }

      return await response.json();
    } catch (error) {
      if (error instanceof ApiError || error instanceof RateLimitError) {
        throw error;
      }
      throw new ApiError(error.message, 0, 'NETWORK_ERROR');
    }
  }

  // ============ Events API ============

  /**
   * List all events with pagination
   * @param {Object} [options] - Query options
   * @param {number} [options.page=1] - Page number
   * @param {number} [options.pageSize=20] - Items per page
   * @param {string} [options.status] - Filter by status
   * @param {Date|string} [options.fromDate] - Filter events starting after this date
   * @param {Date|string} [options.toDate] - Filter events starting before this date
   * @returns {Promise<ApiResponse<PagedResult>>}
   */
  async listEvents(options = {}) {
    return this.request('GET', '/api/external/events', {
      params: {
        page: options.page,
        pageSize: options.pageSize,
        status: options.status,
        fromDate: options.fromDate instanceof Date ? options.fromDate.toISOString() : options.fromDate,
        toDate: options.toDate instanceof Date ? options.toDate.toISOString() : options.toDate
      }
    });
  }

  /**
   * Get a single event by ID
   * @param {string} eventId - Event ID
   * @returns {Promise<ApiResponse>}
   */
  async getEvent(eventId) {
    return this.request('GET', `/api/external/events/${eventId}`);
  }

  /**
   * Create a new event (requires Standard tier or above)
   * @param {Object} event - Event data
   * @param {string} event.title - Event title
   * @param {string} [event.description] - Event description
   * @param {Date|string} event.startDate - Event start date
   * @param {Date|string} event.endDate - Event end date
   * @param {string} [event.location] - Event location
   * @param {string} [event.venueAddress] - Venue address
   * @param {boolean} [event.isVirtual] - Whether the event is virtual
   * @param {string} [event.meetingUrl] - Virtual meeting URL
   * @param {number} [event.maxCapacity] - Maximum attendees
   * @returns {Promise<ApiResponse>}
   */
  async createEvent(event) {
    const body = {
      ...event,
      startDate: event.startDate instanceof Date ? event.startDate.toISOString() : event.startDate,
      endDate: event.endDate instanceof Date ? event.endDate.toISOString() : event.endDate
    };
    return this.request('POST', '/api/external/events', { body });
  }

  /**
   * Update an existing event (requires Standard tier or above)
   * @param {string} eventId - Event ID
   * @param {Object} updates - Fields to update
   * @returns {Promise<ApiResponse>}
   */
  async updateEvent(eventId, updates) {
    const body = { ...updates };
    if (updates.startDate instanceof Date) {
      body.startDate = updates.startDate.toISOString();
    }
    if (updates.endDate instanceof Date) {
      body.endDate = updates.endDate.toISOString();
    }
    return this.request('PUT', `/api/external/events/${eventId}`, { body });
  }

  /**
   * Delete an event (requires Premium tier or above)
   * @param {string} eventId - Event ID
   * @returns {Promise<ApiResponse>}
   */
  async deleteEvent(eventId) {
    return this.request('DELETE', `/api/external/events/${eventId}`);
  }

  // ============ Social Events API ============

  /**
   * List social events for a parent event
   * @param {string} eventId - Parent event ID
   * @returns {Promise<ApiResponse>}
   */
  async listSocialEvents(eventId) {
    return this.request('GET', `/api/external/events/${eventId}/social-events`);
  }

  /**
   * Get a single social event by ID
   * @param {string} socialEventId - Social event ID
   * @returns {Promise<ApiResponse>}
   */
  async getSocialEvent(socialEventId) {
    return this.request('GET', `/api/external/social-events/${socialEventId}`);
  }

  // ============ API Key Info ============

  /**
   * Get API key information and rate limit status
   * @returns {Promise<ApiResponse>}
   */
  async getApiKeyInfo() {
    return this.request('GET', '/api/external/me');
  }

  // ============ Utilities ============

  /**
   * Get current rate limit information (from last request)
   * @returns {Object}
   */
  getRateLimitInfo() {
    return { ...this.rateLimit };
  }

  /**
   * Check if approaching rate limit
   * @param {number} [threshold=0.1] - Threshold percentage (default: 10%)
   * @returns {boolean}
   */
  isNearRateLimit(threshold = 0.1) {
    if (!this.rateLimit.limit || !this.rateLimit.remaining) {
      return false;
    }
    return (this.rateLimit.remaining / this.rateLimit.limit) < threshold;
  }
}

/**
 * API Error class
 */
export class ApiError extends Error {
  /**
   * @param {string} message - Error message
   * @param {number} status - HTTP status code
   * @param {string} code - Error code
   */
  constructor(message, status, code) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.code = code;
  }
}

/**
 * Rate Limit Error class
 */
export class RateLimitError extends Error {
  /**
   * @param {string} message - Error message
   * @param {number} retryAfter - Seconds to wait before retrying
   * @param {Object} rateLimit - Rate limit information
   */
  constructor(message, retryAfter, rateLimit) {
    super(message);
    this.name = 'RateLimitError';
    this.retryAfter = retryAfter;
    this.rateLimit = rateLimit;
  }
}

// Default export for CommonJS compatibility
export default EventManagementClient;
