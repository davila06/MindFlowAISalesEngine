import { defineConfig } from "@playwright/test";

const frontendBaseUrl = process.env.FRONTEND_BASE_URL ?? "http://127.0.0.1:3100";
const apiBaseUrl = process.env.API_BASE_URL ?? "http://127.0.0.1:5165";

export default defineConfig({
  testDir: "./tests/e2e",
  timeout: 60_000,
  expect: {
    timeout: 10_000
  },
  use: {
    baseURL: frontendBaseUrl,
    trace: "on-first-retry"
  },
  webServer: [
    {
      command:
        process.env.PLAYWRIGHT_API_COMMAND ??
        "dotnet run --project ../backend/src/Api/Api.csproj --urls http://127.0.0.1:5165",
      url: `${apiBaseUrl}/health/live`,
      env: {
        ...process.env,
        ASPNETCORE_ENVIRONMENT: "Development"
      },
      timeout: 120_000,
      reuseExistingServer: false
    },
    {
      command:
        process.env.PLAYWRIGHT_FRONTEND_COMMAND ??
        "npm start -- --hostname 127.0.0.1 --port 3100",
      url: frontendBaseUrl,
      env: {
        ...process.env,
        NEXT_PUBLIC_API_URL: apiBaseUrl
      },
      timeout: 120_000,
      reuseExistingServer: false
    }
  ],
  reporter: [["list"], ["html", { open: "never" }]]
});
