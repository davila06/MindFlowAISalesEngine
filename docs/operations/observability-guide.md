# Observability Guide

This document explains how to configure and use observability tools for NovaMind, including Azure Monitor and Application Insights.

## Prerequisites

1. **Azure Subscription**:
   - Ensure you have access to an Azure subscription.

2. **Application Insights Resource**:
   - Create an Application Insights resource in the Azure portal.

## Configuration

### 1. Backend Instrumentation

1. Add the Application Insights SDK:
   ```bash
   dotnet add package Microsoft.ApplicationInsights.AspNetCore
   ```

2. Update `Program.cs`:
   ```csharp
   builder.Services.AddApplicationInsightsTelemetry();
   ```

3. Configure the Instrumentation Key:
   - Use an environment variable:
     ```bash
     export APPLICATIONINSIGHTS_CONNECTION_STRING="InstrumentationKey=<YOUR_KEY>"
     ```

### 2. Frontend Instrumentation

1. Add the Application Insights JavaScript SDK:
   ```bash
   npm install @microsoft/applicationinsights-web
   ```

2. Initialize the SDK:
   ```javascript
   import { ApplicationInsights } from '@microsoft/applicationinsights-web';

   const appInsights = new ApplicationInsights({
       config: {
           instrumentationKey: '<YOUR_KEY>'
       }
   });
   appInsights.loadAppInsights();
   appInsights.trackPageView();
   ```

### 3. Azure Monitor Alerts

1. Create Alerts:
   - Go to Azure Monitor > Alerts > New Alert Rule.
   - Define conditions (e.g., server response time > 2s).

2. Example KQL Query:
   ```kql
   requests
   | where duration > 2000
   | order by timestamp desc
   ```

## Best Practices

1. **Distributed Tracing**:
   - Ensure both backend and frontend include correlation IDs.

2. **Dashboards**:
   - Use Azure Monitor Workbooks to create custom dashboards.

3. **Log Retention**:
   - Set appropriate retention policies for logs in Application Insights.