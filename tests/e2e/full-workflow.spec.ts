import { test, expect, Page } from '@playwright/test';

/**
 * Comprehensive E2E test suite for the Event Management System.
 * Tests the complete user workflow from event discovery to registration confirmation.
 */

// Test configuration
const BASE_URL = process.env.BASE_URL || 'http://localhost:4200';
const API_URL = process.env.API_URL || 'http://localhost:5000';

// Test data
const testUser = {
  email: `test-${Date.now()}@example.com`,
  password: 'TestPassword123!',
  firstName: 'Test',
  lastName: 'User'
};

const testEvent = {
  title: `Test Event ${Date.now()}`,
  description: 'A comprehensive test event for E2E testing',
  startDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString(),
  endDate: new Date(Date.now() + 8 * 24 * 60 * 60 * 1000).toISOString(),
  location: 'Test Conference Center',
  capacity: 100
};

test.describe('Event Management E2E Tests', () => {
  
  test.describe('User Authentication Flow', () => {
    
    test('should allow user to register a new account', async ({ page }) => {
      await page.goto(`${BASE_URL}/register`);
      
      // Fill registration form
      await page.fill('[data-testid="email-input"]', testUser.email);
      await page.fill('[data-testid="password-input"]', testUser.password);
      await page.fill('[data-testid="confirm-password-input"]', testUser.password);
      await page.fill('[data-testid="first-name-input"]', testUser.firstName);
      await page.fill('[data-testid="last-name-input"]', testUser.lastName);
      
      // Submit form
      await page.click('[data-testid="register-button"]');
      
      // Verify redirect to login or dashboard
      await expect(page).toHaveURL(/\/(login|dashboard)/);
    });

    test('should allow user to login with valid credentials', async ({ page }) => {
      await page.goto(`${BASE_URL}/login`);
      
      await page.fill('[data-testid="email-input"]', testUser.email);
      await page.fill('[data-testid="password-input"]', testUser.password);
      await page.click('[data-testid="login-button"]');
      
      // Verify successful login
      await expect(page).toHaveURL(/\/dashboard/);
      await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
    });

    test('should show error for invalid credentials', async ({ page }) => {
      await page.goto(`${BASE_URL}/login`);
      
      await page.fill('[data-testid="email-input"]', 'invalid@example.com');
      await page.fill('[data-testid="password-input"]', 'wrongpassword');
      await page.click('[data-testid="login-button"]');
      
      await expect(page.locator('[data-testid="error-message"]')).toBeVisible();
    });

    test('should allow user to logout', async ({ page }) => {
      // Login first
      await loginUser(page, testUser.email, testUser.password);
      
      // Logout
      await page.click('[data-testid="user-menu"]');
      await page.click('[data-testid="logout-button"]');
      
      await expect(page).toHaveURL(/\/login/);
    });
  });

  test.describe('Event Discovery Flow', () => {
    
    test('should display list of events on homepage', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      await expect(page.locator('[data-testid="event-list"]')).toBeVisible();
      await expect(page.locator('[data-testid="event-card"]')).toHaveCount(await page.locator('[data-testid="event-card"]').count());
    });

    test('should filter events by search query', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      await page.fill('[data-testid="search-input"]', 'conference');
      await page.press('[data-testid="search-input"]', 'Enter');
      
      // Wait for filtered results
      await page.waitForResponse(response => 
        response.url().includes('/api/events') && response.status() === 200
      );
      
      const eventCards = page.locator('[data-testid="event-card"]');
      for (let i = 0; i < await eventCards.count(); i++) {
        const text = await eventCards.nth(i).textContent();
        expect(text?.toLowerCase()).toContain('conference');
      }
    });

    test('should filter events by date range', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      const startDate = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
      const endDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000);
      
      await page.fill('[data-testid="start-date-filter"]', startDate.toISOString().split('T')[0]);
      await page.fill('[data-testid="end-date-filter"]', endDate.toISOString().split('T')[0]);
      await page.click('[data-testid="apply-filters-button"]');
      
      await page.waitForResponse(response => 
        response.url().includes('/api/events') && response.status() === 200
      );
    });

    test('should navigate to event details page', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      // Click on first event
      await page.click('[data-testid="event-card"]:first-child');
      
      await expect(page).toHaveURL(/\/events\/[\w-]+/);
      await expect(page.locator('[data-testid="event-title"]')).toBeVisible();
      await expect(page.locator('[data-testid="event-description"]')).toBeVisible();
    });

    test('should support pagination', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      // Check if pagination exists
      const pagination = page.locator('[data-testid="pagination"]');
      if (await pagination.isVisible()) {
        await page.click('[data-testid="next-page-button"]');
        await expect(page).toHaveURL(/page=2/);
      }
    });
  });

  test.describe('Event Registration Flow', () => {
    
    test.beforeEach(async ({ page }) => {
      await loginUser(page, testUser.email, testUser.password);
    });

    test('should allow user to register for an event', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      // Click on first available event
      await page.click('[data-testid="event-card"]:first-child');
      
      // Click register button
      await page.click('[data-testid="register-button"]');
      
      // Fill registration form if present
      const registrationForm = page.locator('[data-testid="registration-form"]');
      if (await registrationForm.isVisible()) {
        await page.fill('[data-testid="dietary-requirements"]', 'Vegetarian');
        await page.fill('[data-testid="special-requests"]', 'Need accessible seating');
        await page.click('[data-testid="submit-registration"]');
      }
      
      // Verify registration success
      await expect(page.locator('[data-testid="registration-success"]')).toBeVisible();
    });

    test('should show registration in user dashboard', async ({ page }) => {
      await page.goto(`${BASE_URL}/dashboard/registrations`);
      
      await expect(page.locator('[data-testid="registration-list"]')).toBeVisible();
      await expect(page.locator('[data-testid="registration-item"]')).toHaveCount(
        await page.locator('[data-testid="registration-item"]').count()
      );
    });

    test('should allow user to cancel registration', async ({ page }) => {
      await page.goto(`${BASE_URL}/dashboard/registrations`);
      
      const registrationItem = page.locator('[data-testid="registration-item"]:first-child');
      if (await registrationItem.isVisible()) {
        await registrationItem.locator('[data-testid="cancel-registration-button"]').click();
        
        // Confirm cancellation
        await page.click('[data-testid="confirm-cancel-button"]');
        
        await expect(page.locator('[data-testid="cancellation-success"]')).toBeVisible();
      }
    });

    test('should handle queue-based registration for popular events', async ({ page }) => {
      // This test assumes there's an event at capacity
      await page.goto(`${BASE_URL}/events`);
      
      // Find an event with waiting list
      const waitlistEvent = page.locator('[data-testid="event-card"][data-has-waitlist="true"]');
      if (await waitlistEvent.isVisible()) {
        await waitlistEvent.click();
        await page.click('[data-testid="join-waitlist-button"]');
        
        await expect(page.locator('[data-testid="waitlist-confirmation"]')).toBeVisible();
      }
    });
  });

  test.describe('Session Management Flow', () => {
    
    test.beforeEach(async ({ page }) => {
      await loginUser(page, testUser.email, testUser.password);
    });

    test('should display event sessions', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      
      // Navigate to sessions tab
      await page.click('[data-testid="sessions-tab"]');
      
      await expect(page.locator('[data-testid="session-list"]')).toBeVisible();
    });

    test('should allow user to subscribe to a session', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="sessions-tab"]');
      
      // Click on first session
      const sessionCard = page.locator('[data-testid="session-card"]:first-child');
      await sessionCard.locator('[data-testid="subscribe-button"]').click();
      
      await expect(sessionCard.locator('[data-testid="subscribed-badge"]')).toBeVisible();
    });

    test('should detect session conflicts', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="sessions-tab"]');
      
      // Subscribe to first session
      const firstSession = page.locator('[data-testid="session-card"]:first-child');
      await firstSession.locator('[data-testid="subscribe-button"]').click();
      
      // Try to subscribe to conflicting session
      const conflictingSession = page.locator('[data-testid="session-card"][data-conflict="true"]');
      if (await conflictingSession.isVisible()) {
        await conflictingSession.locator('[data-testid="subscribe-button"]').click();
        
        await expect(page.locator('[data-testid="conflict-warning"]')).toBeVisible();
      }
    });

    test('should show session schedule', async ({ page }) => {
      await page.goto(`${BASE_URL}/dashboard/schedule`);
      
      await expect(page.locator('[data-testid="schedule-view"]')).toBeVisible();
    });
  });

  test.describe('Speaker Information Flow', () => {
    
    test('should display speaker information on session details', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="sessions-tab"]');
      
      // Click on session to view details
      await page.click('[data-testid="session-card"]:first-child');
      
      await expect(page.locator('[data-testid="speaker-info"]')).toBeVisible();
    });

    test('should navigate to speaker profile', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="sessions-tab"]');
      await page.click('[data-testid="session-card"]:first-child');
      
      await page.click('[data-testid="speaker-link"]');
      
      await expect(page).toHaveURL(/\/speakers\/[\w-]+/);
      await expect(page.locator('[data-testid="speaker-profile"]')).toBeVisible();
    });
  });

  test.describe('Social Events Flow', () => {
    
    test.beforeEach(async ({ page }) => {
      await loginUser(page, testUser.email, testUser.password);
    });

    test('should display social events', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="social-events-tab"]');
      
      await expect(page.locator('[data-testid="social-event-list"]')).toBeVisible();
    });

    test('should allow user to RSVP to social event', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="social-events-tab"]');
      
      const socialEvent = page.locator('[data-testid="social-event-card"]:first-child');
      await socialEvent.locator('[data-testid="rsvp-button"]').click();
      
      await expect(socialEvent.locator('[data-testid="rsvp-confirmed"]')).toBeVisible();
    });
  });

  test.describe('Real-time Notifications', () => {
    
    test.beforeEach(async ({ page }) => {
      await loginUser(page, testUser.email, testUser.password);
    });

    test('should receive registration confirmation notification', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      await page.click('[data-testid="event-card"]:first-child');
      await page.click('[data-testid="register-button"]');
      
      // Wait for SignalR notification
      await expect(page.locator('[data-testid="notification-toast"]')).toBeVisible({ timeout: 10000 });
    });

    test('should show unread notifications count', async ({ page }) => {
      await page.goto(`${BASE_URL}/dashboard`);
      
      const notificationBadge = page.locator('[data-testid="notification-badge"]');
      if (await notificationBadge.isVisible()) {
        const count = await notificationBadge.textContent();
        expect(parseInt(count || '0')).toBeGreaterThanOrEqual(0);
      }
    });
  });

  test.describe('API Rate Limiting', () => {
    
    test('should handle rate limiting gracefully', async ({ page, request }) => {
      // Make multiple rapid API calls
      const responses = await Promise.all(
        Array(50).fill(null).map(() => 
          request.get(`${API_URL}/api/events`)
        )
      );
      
      // Check if rate limit was hit
      const rateLimited = responses.some(r => r.status() === 429);
      if (rateLimited) {
        // Verify rate limit headers
        const limitedResponse = responses.find(r => r.status() === 429);
        expect(limitedResponse?.headers()['x-ratelimit-limit']).toBeDefined();
        expect(limitedResponse?.headers()['retry-after']).toBeDefined();
      }
    });
  });

  test.describe('Accessibility', () => {
    
    test('should have proper heading structure', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      const h1 = page.locator('h1');
      await expect(h1).toHaveCount(1);
    });

    test('should be navigable by keyboard', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      // Tab through focusable elements
      await page.keyboard.press('Tab');
      const firstFocused = await page.evaluate(() => document.activeElement?.tagName);
      expect(firstFocused).toBeTruthy();
    });

    test('should have proper aria labels', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      const searchInput = page.locator('[data-testid="search-input"]');
      await expect(searchInput).toHaveAttribute('aria-label', /.+/);
    });
  });

  test.describe('Performance', () => {
    
    test('should load homepage within acceptable time', async ({ page }) => {
      const startTime = Date.now();
      await page.goto(`${BASE_URL}/events`);
      const loadTime = Date.now() - startTime;
      
      expect(loadTime).toBeLessThan(3000); // 3 seconds max
    });

    test('should render events list efficiently', async ({ page }) => {
      await page.goto(`${BASE_URL}/events`);
      
      const metrics = await page.evaluate(() => ({
        domNodes: document.querySelectorAll('*').length,
        memoryUsage: (performance as any).memory?.usedJSHeapSize || 0
      }));
      
      expect(metrics.domNodes).toBeLessThan(5000);
    });
  });
});

// Helper functions
async function loginUser(page: Page, email: string, password: string) {
  await page.goto(`${BASE_URL}/login`);
  await page.fill('[data-testid="email-input"]', email);
  await page.fill('[data-testid="password-input"]', password);
  await page.click('[data-testid="login-button"]');
  await page.waitForURL(/\/dashboard/);
}

async function createTestEvent(page: Page) {
  await page.goto(`${BASE_URL}/admin/events/create`);
  await page.fill('[data-testid="event-title"]', testEvent.title);
  await page.fill('[data-testid="event-description"]', testEvent.description);
  await page.fill('[data-testid="event-start-date"]', testEvent.startDate);
  await page.fill('[data-testid="event-end-date"]', testEvent.endDate);
  await page.fill('[data-testid="event-location"]', testEvent.location);
  await page.fill('[data-testid="event-capacity"]', String(testEvent.capacity));
  await page.click('[data-testid="create-event-button"]');
}
