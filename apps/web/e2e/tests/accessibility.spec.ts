import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { expect, test } from '../fixtures/plannyt.fixture';

test.describe('accesibilidad de superficies críticas', () => {
  test('acceso público no tiene violaciones serias o críticas', async ({ page }) => {
    await page.goto('/auth/login');
    await expect(page.getByRole('heading', { name: 'Inicia sesión' })).toBeVisible();

    await expectNoSeriousViolations(page);
  });

  test('dashboard profesional no tiene violaciones serias o críticas', async ({ page, api }) => {
    api.useProfile('owner');
    await page.goto('/app/dashboard');
    await expect(page.getByRole('heading', { name: 'Todo en su lugar.' })).toBeVisible();

    await expectNoSeriousViolations(page);
  });

  test('portal del cliente no tiene violaciones serias o críticas', async ({ page, api }) => {
    api.useProfile('portal');
    await page.goto('/portal/events/event-1');
    await expect(page.getByRole('heading', { name: 'Ana & Carlos' })).toBeVisible();

    await expectNoSeriousViolations(page);
  });

  test('propuesta pública no tiene violaciones serias o críticas', async ({ page, api }) => {
    api.prepareContractingScenario();
    await page.goto('/proposal/public-token-1');
    await expect(page.getByRole('heading', { name: /Propuesta/ })).toBeVisible();

    await expectNoSeriousViolations(page);
  });

  test('diálogo de prospectos no tiene violaciones serias o críticas', async ({ page, api }) => {
    api.useProfile('owner');
    await page.goto('/app/prospects');
    const trigger = page.getByRole('button', { name: /Nuevo prospecto/ });
    await trigger.click();
    const dialog = page.getByRole('dialog');
    const closeButton = page.getByRole('button', { name: 'Cerrar registro de prospecto' });
    await expect(dialog).toBeVisible();
    await expect(closeButton).toBeFocused();

    await expectNoSeriousViolations(page);

    for (let tabIndex = 0; tabIndex < 12; tabIndex += 1) {
      await page.keyboard.press('Tab');
      await expect
        .poll(() =>
          dialog.evaluate((element) => element.contains(document.activeElement)),
        )
        .toBe(true);
    }

    await page.keyboard.press('Escape');
    await expect(dialog).toBeHidden();
    await expect(trigger).toBeFocused();
  });
});

async function expectNoSeriousViolations(page: Page): Promise<void> {
  const results = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'])
    .analyze();
  const violations = results.violations
    .filter((violation) => violation.impact === 'critical' || violation.impact === 'serious')
    .map((violation) => ({
      id: violation.id,
      impact: violation.impact,
      nodes: violation.nodes.map((node) => ({
        target: node.target.join(' '),
        html: node.html,
        summary: node.failureSummary,
      })),
    }));

  expect(violations).toEqual([]);
}
