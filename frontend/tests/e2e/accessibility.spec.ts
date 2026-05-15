import AxeBuilder from "@axe-core/playwright";
import { expect, test } from "@playwright/test";

const criticalRoutes = ["/dashboard", "/pipeline", "/rules", "/email/logs"];

for (const route of criticalRoutes) {
  test(`a11y ${route} has no serious violations`, async ({ page }) => {
    await page.goto(route);

    const results = await new AxeBuilder({ page })
      .withTags(["wcag2a", "wcag2aa"])
      .analyze();

    const seriousViolations = results.violations.filter((item) =>
      ["serious", "critical"].includes(item.impact ?? "")
    );

    expect(seriousViolations, JSON.stringify(seriousViolations, null, 2)).toEqual([]);
  });
}
