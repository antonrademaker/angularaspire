import { FullConfig } from '@playwright/test';

/**
 * Global setup for E2E tests.
 * Runs once before all tests.
 */
async function globalSetup(config: FullConfig) {
  console.log('Setting up E2E test environment...');
  
  // Wait for services to be ready
  const baseURL = config.projects[0].use?.baseURL || 'http://localhost:4200';
  const apiURL = process.env.API_URL || 'http://localhost:5000';
  
  // Health check for frontend
  await waitForService(`${baseURL}`, 'Frontend');
  
  // Health check for API
  await waitForService(`${apiURL}/health`, 'API');
  
  console.log('E2E test environment is ready.');
}

async function waitForService(url: string, serviceName: string, maxAttempts = 30, delay = 2000) {
  for (let attempt = 1; attempt <= maxAttempts; attempt++) {
    try {
      const response = await fetch(url);
      if (response.ok) {
        console.log(`${serviceName} is ready at ${url}`);
        return;
      }
    } catch (error) {
      // Service not ready yet
    }
    
    if (attempt < maxAttempts) {
      console.log(`Waiting for ${serviceName}... (attempt ${attempt}/${maxAttempts})`);
      await new Promise(resolve => setTimeout(resolve, delay));
    }
  }
  
  throw new Error(`${serviceName} at ${url} did not become ready within timeout`);
}

export default globalSetup;
