import { expect, test, type APIRequestContext } from "@playwright/test";

const apiBaseUrl = process.env.API_BASE_URL ?? "http://127.0.0.1:5165";

function uniqueEmail(prefix: string) {
  return `${prefix}_${Date.now()}_${Math.floor(Math.random() * 10000)}@mindflow.test`;
}

function uniquePhone() {
  const digits = Math.floor(Math.random() * 10_000_000)
    .toString()
    .padStart(7, "0");
  return `+1555${digits}`;
}

async function createLeadForPipeline(request: APIRequestContext) {
  const response = await request.post(`${apiBaseUrl}/api/leads/intake`, {
    data: {
      email: uniqueEmail("pipeline"),
      phone: uniquePhone(),
      source: "e2e"
    },
    headers: {
      "X-Tenant-Id": "default"
    }
  });

  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return String(body.id);
}

async function createOpportunityForPipeline(request: APIRequestContext, leadId: string) {
  const stagesResponse = await request.get(`${apiBaseUrl}/api/pipeline/stages`, {
    headers: {
      "X-Tenant-Id": "default"
    }
  });

  expect(stagesResponse.ok()).toBeTruthy();
  const stages = (await stagesResponse.json()) as Array<{ id: string; name: string }>;
  expect(stages.length).toBeGreaterThan(1);

  const title = `bulk-e2e-${Date.now()}`;
  const createResponse = await request.post(`${apiBaseUrl}/api/pipeline/opportunities`, {
    data: {
      leadId,
      title,
      value: 2500,
      stageId: stages[0].id
    },
    headers: {
      "X-Tenant-Id": "default"
    }
  });

  expect(createResponse.ok()).toBeTruthy();
  const created = (await createResponse.json()) as { id: string };

  return {
    id: String(created.id),
    title,
    sourceStageId: stages[0].id,
    targetStageId: stages[1].id,
    targetStageName: stages[1].name
  };
}

async function createRuleForToggle(request: APIRequestContext) {
  const response = await request.post(`${apiBaseUrl}/api/rules`, {
    data: {
      name: `e2e-rule-${Date.now()}`,
      trigger: "lead.created",
      isActive: true,
      conditions: [{ field: "source", operator: "eq", value: "e2e" }],
      actions: [{ type: "add_score", value: "5" }]
    },
    headers: {
      "X-Tenant-Id": "default"
    }
  });

  expect(response.ok()).toBeTruthy();
}

async function createRuleForBuilderEdit(request: APIRequestContext) {
  const response = await request.post(`${apiBaseUrl}/api/rules`, {
    data: {
      name: `e2e-builder-${Date.now()}`,
      trigger: "lead.created",
      isActive: true,
      conditions: [{ field: "source", operator: "eq", value: "website" }],
      actions: [{ type: "add_score", value: "5" }]
    },
    headers: {
      "X-Tenant-Id": "default"
    }
  });

  expect(response.ok()).toBeTruthy();
  const body = await response.json();
  return String(body.id);
}

test("dashboard renders KPI surface", async ({ page }) => {
  await page.goto("/dashboard");

  await expect(page.getByRole("heading", { name: "Dashboard" })).toBeVisible();
  await expect(page.getByText("Total Leads")).toBeVisible();
  await expect(page.getByText("Conversion Rate")).toBeVisible();
  await expect(page.getByText("Pipeline Value", { exact: true })).toBeVisible();
  await expect(page.getByLabel("Days window")).toBeVisible();
});

test("pipeline board exposes quick actions", async ({ page, request }) => {
  const leadId = await createLeadForPipeline(request);

  await page.goto("/pipeline");
  await expect(page.getByRole("heading", { name: "Pipeline Board" })).toBeVisible();
  await expect(
    page.getByRole("group", { name: "Opportunity quick actions" })
  ).toBeVisible();

  const createButton = page.getByRole("button", { name: "Create Opportunity" });
  await expect(createButton).toBeDisabled();

  await page.getByLabel("Lead Id").fill(leadId);
  await page.getByLabel("Title").fill(`e2e-deal-${Date.now()}`);
  await page.getByLabel("Value").fill("2000");
  await expect(createButton).toBeEnabled();
});

test("pipeline bulk move persists saved view", async ({ page, request }) => {
  const leadId = await createLeadForPipeline(request);
  const opportunity = await createOpportunityForPipeline(request, leadId);

  await page.goto("/pipeline");
  await expect(page.getByRole("heading", { name: "Pipeline Board" })).toBeVisible();

  await page
    .getByLabel(`Select opportunity ${opportunity.title}`)
    .check();
  await page.getByLabel("Bulk move").selectOption(opportunity.targetStageId);
  await page.getByRole("button", { name: "Bulk Move: 1" }).click();

  await expect(page.getByLabel(`Move to stage ${opportunity.title}`)).toHaveValue(
    opportunity.targetStageId
  );

  await page.getByLabel("Saved view").selectOption(opportunity.targetStageId);
  await page.getByRole("button", { name: "Apply View" }).click();
  await page.reload();

  await expect(page.getByLabel("Saved view")).toHaveValue(opportunity.targetStageId);
});

test("rules toggles active state with undo", async ({ page, request }) => {
  await createRuleForToggle(request);

  await page.goto("/rules");
  await expect(page.getByRole("heading", { name: "Rules Engine" })).toBeVisible();

  const deactivateButton = page.getByRole("button", { name: "Deactivate" }).first();
  await expect(deactivateButton).toBeVisible();
  await deactivateButton.click();
  await expect(page.getByRole("dialog")).toBeVisible();
  await page.getByRole("button", { name: "Deactivate" }).last().click();

  await expect(page.getByRole("button", { name: "Undo" })).toBeVisible();
  await page.getByRole("button", { name: "Undo" }).click();

  await expect(page.getByRole("button", { name: "Deactivate" }).first()).toBeVisible();
});

test("rule builder edits existing rule with multiple conditions and actions", async ({ page, request }) => {
  const ruleId = await createRuleForBuilderEdit(request);

  await page.goto("/rules");
  await expect(page.getByRole("heading", { name: "Rules Engine" })).toBeVisible();

  await page.getByLabel("Select rule").selectOption(ruleId);
  await page.getByRole("button", { name: "Load rule" }).click();

  await page.getByRole("button", { name: "Add condition" }).click();
  await page.getByLabel("Condition field 2").fill("priority");
  await page.getByLabel("Condition value 2").fill("High");

  await page.getByRole("button", { name: "Add action" }).click();
  await page.getByLabel("Action type 2").fill("set_priority");
  await page.getByLabel("Action value 2").fill("High");

  await page.getByRole("button", { name: "Save changes" }).click();
  await expect(page.getByText("Rule updated.")).toBeVisible();

  const response = await request.get(`${apiBaseUrl}/api/rules/${ruleId}`, {
    headers: {
      "X-Tenant-Id": "default"
    }
  });

  expect(response.ok()).toBeTruthy();
  const payload = await response.json();
  expect(payload.conditions).toHaveLength(2);
  expect(payload.actions).toHaveLength(2);
  expect(payload.conditions).toEqual(
    expect.arrayContaining([expect.objectContaining({ field: "priority", value: "High" })])
  );
  expect(payload.actions).toEqual(
    expect.arrayContaining([expect.objectContaining({ type: "set_priority", value: "High" })])
  );
});

test("smtp form saves settings", async ({ page }) => {
  await page.goto("/email/smtp");
  await expect(page.getByRole("heading", { name: "SMTP Configuration" })).toBeVisible();

  await page.getByLabel("Host").fill("smtp.e2e.local");
  await page.getByLabel("Port").fill("587");
  await page.getByLabel("Username").fill("ops@e2e.local");
  await page.getByLabel("Password").fill("E2E_password_123");
  await page.getByLabel("From Email").fill("ops@e2e.local");
  await page.getByLabel("From Name").fill("MindFlow E2E");

  await page.getByRole("button", { name: "Save SMTP" }).click();
  await expect(page.getByText("SMTP settings saved.")).toBeVisible();
});

test("admin opens UI guide", async ({ page }) => {
  await page.goto("/admin");
  await page.getByRole("button", { name: "Open UI Pattern Guide" }).click();
  await expect(page.getByRole("heading", { name: "UI Pattern Guide" })).toBeVisible();
});
