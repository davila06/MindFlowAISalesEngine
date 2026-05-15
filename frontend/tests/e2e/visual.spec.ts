import { expect, test } from "@playwright/test";

interface VisualRoute {
  path: string;
  locator: string;
  readySelector?: string;
  beforeNavigate?: (page: Parameters<typeof test>[0]["page"]) => Promise<void>;
  beforeScreenshot?: (page: Parameters<typeof test>[0]["page"]) => Promise<void>;
  maxDiffPixelRatio?: number;
}

const routes: VisualRoute[] = [
  { path: "/dashboard", locator: "main", readySelector: undefined },
  {
    path: "/pipeline",
    locator: "main",
    readySelector: '[aria-label="Lead Id"]',
    maxDiffPixelRatio: 0.05,
    beforeNavigate: async (page) => {
      await page.addInitScript(() => {
        window.localStorage.setItem("mindflow.pipeline.viewFilter", "all");
      });
    },
    beforeScreenshot: async (page) => {
      await page.addStyleTag({
        content: `
          .board {
            display: none !important;
          }
        `
      });
    }
  },
  {
    path: "/rules",
    locator: ".rule-builder-grid",
    readySelector: '[aria-label="Rule name"]'
  },
  { path: "/email/logs", locator: "main", readySelector: undefined }
];

for (const route of routes) {
  test(`visual snapshot ${route.path}`, async ({ page }) => {
    if (route.beforeNavigate) {
      await route.beforeNavigate(page);
    }
    await page.goto(route.path);
    await page.waitForLoadState("networkidle");
    if (route.readySelector) {
      await page.waitForSelector(route.readySelector, { state: "visible", timeout: 15_000 });
    }
    if (route.beforeScreenshot) {
      await route.beforeScreenshot(page);
    }

    await expect(page.locator(route.locator)).toHaveScreenshot(
      `${route.path.replaceAll("/", "_") || "root"}.png`,
      {
        maxDiffPixelRatio: route.maxDiffPixelRatio ?? 0.03,
        animations: "disabled"
      }
    );
  });
}
