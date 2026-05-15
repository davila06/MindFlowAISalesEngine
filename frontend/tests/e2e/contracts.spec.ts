import { expect, test } from "@playwright/test";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://127.0.0.1:5165";

test("contract pipeline board shape", async ({ request }) => {
  const response = await request.get(`${apiBaseUrl}/api/pipeline/board`, {
    headers: { "X-Tenant-Id": "default" }
  });

  expect(response.ok()).toBeTruthy();
  const payload = await response.json();

  expect(Array.isArray(payload.stages)).toBeTruthy();
  expect(Array.isArray(payload.opportunities)).toBeTruthy();
});

test("contract rules list shape", async ({ request }) => {
  const response = await request.get(`${apiBaseUrl}/api/rules?page=1&pageSize=5`, {
    headers: { "X-Tenant-Id": "default" }
  });

  expect(response.ok()).toBeTruthy();
  const payload = await response.json();
  expect(Array.isArray(payload)).toBeTruthy();

  if (payload.length > 0) {
    expect(payload[0]).toMatchObject({
      id: expect.any(String),
      name: expect.any(String),
      trigger: expect.any(String)
    });
  }
});

test("contract email logs shape", async ({ request }) => {
  const response = await request.get(`${apiBaseUrl}/api/email/logs?page=1&pageSize=5`, {
    headers: { "X-Tenant-Id": "default" }
  });

  expect(response.ok()).toBeTruthy();
  const payload = await response.json();
  expect(Array.isArray(payload)).toBeTruthy();

  if (payload.length > 0) {
    expect(payload[0]).toMatchObject({
      id: expect.any(String),
      templateName: expect.any(String),
      status: expect.any(String),
      sentAtUtc: expect.any(String)
    });
  }
});
