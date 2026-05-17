# Testing Guide

This document explains how to write and execute tests for the NovaMind project.

## Backend Testing

### 1. Unit Tests

1. **Location**:
   - Place unit tests in `backend/tests/Api.Tests/`.

2. **Framework**:
   - Use xUnit for unit testing.

3. **Example**:
   ```csharp
   public class LeadServiceTests
   {
       [Fact]
       public void CreateLead_ShouldReturnSuccess()
       {
           // Arrange
           var service = new LeadService();

           // Act
           var result = service.CreateLead("test@example.com");

           // Assert
           Assert.True(result.IsSuccess);
       }
   }
   ```

### 2. Integration Tests

1. **Location**:
   - Place integration tests in `backend/tests/Api.Tests/Integration/`.

2. **Setup**:
   - Use an in-memory database for integration tests.

3. **Example**:
   ```csharp
   public class LeadControllerTests
   {
       [Fact]
       public async Task GetLead_ShouldReturnLead()
       {
           // Arrange
           var client = TestServer.CreateClient();

           // Act
           var response = await client.GetAsync("/api/leads/1");

           // Assert
           response.EnsureSuccessStatusCode();
       }
   }
   ```

## Frontend Testing

### 1. Unit Tests

1. **Location**:
   - Place unit tests in `frontend/tests/`.

2. **Framework**:
   - Use Jest for unit testing.

3. **Example**:
   ```javascript
   import { render, screen } from '@testing-library/react';
   import Button from '../components/Button';

   test('renders button with text', () => {
       render(<Button text="Click me" />);
       expect(screen.getByText(/Click me/i)).toBeInTheDocument();
   });
   ```

### 2. End-to-End Tests

1. **Location**:
   - Place E2E tests in `frontend/tests/e2e/`.

2. **Framework**:
   - Use Playwright for E2E testing.

3. **Example**:
   ```javascript
   import { test, expect } from '@playwright/test';

   test('homepage has title', async ({ page }) => {
       await page.goto('/');
       await expect(page).toHaveTitle(/NovaMind/);
   });
   ```

## Load Testing

1. **Location**:
   - Place load test scripts in `backend/tests/LoadTests/`.

2. **Framework**:
   - Use Locust for load testing.

3. **Example**:
   ```python
   from locust import HttpUser, task

   class LoadTest(HttpUser):
       @task
       def get_leads(self):
           self.client.get("/api/leads")
   ```

## Running Tests

### Backend
```bash
cd backend/tests/Api.Tests
# Run all tests
 dotnet test
```

### Frontend
```bash
cd frontend
# Run unit tests
npm run test

# Run E2E tests
npx playwright test
```

### Load Tests
```bash
cd backend/tests/LoadTests
locust -f load_test.py
```