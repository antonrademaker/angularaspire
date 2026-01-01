import { FullConfig } from '@playwright/test';

/**
 * Global teardown for E2E tests.
 * Runs once after all tests complete.
 */
async function globalTeardown(config: FullConfig) {
  console.log('Cleaning up E2E test environment...');
  
  // Any cleanup tasks can go here
  // For example:
  // - Deleting test data from the database
  // - Cleaning up uploaded files
  // - Resetting test user accounts
  
  console.log('E2E test cleanup complete.');
}

export default globalTeardown;
