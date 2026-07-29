import { expect, test } from '../fixtures/plannyt.fixture';

const viewports = [
  { width: 360, height: 800 },
  { width: 393, height: 873 },
  { width: 768, height: 1024 },
  { width: 1024, height: 768 },
  { width: 1366, height: 768 },
  { width: 1920, height: 1080 },
];

test('dashboard y portal evitan scroll horizontal global en viewports objetivo', async (
  { page, api },
  testInfo,
) => {
  test.skip(
    testInfo.project.name !== 'chromium',
    'La matriz se ejecuta una sola vez porque establece cada viewport de forma explícita.',
  );

  for (const viewport of viewports) {
    await page.setViewportSize(viewport);

    api.useProfile('owner');
    await page.goto('/app/dashboard');
    await expect(page.getByRole('heading', { name: 'Todo en su lugar.' })).toBeVisible();
    await expectGlobalWidthToFit(page);

    api.useProfile('portal');
    await page.goto('/portal/events/event-1');
    await expect(page.getByRole('heading', { name: 'Ana & Carlos' })).toBeVisible();
    await expectGlobalWidthToFit(page);
  }
});

async function expectGlobalWidthToFit(page: import('@playwright/test').Page): Promise<void> {
  const dimensions = await page.evaluate(() => ({
    viewportWidth: window.innerWidth,
    documentWidth: document.documentElement.scrollWidth,
  }));

  expect(dimensions.documentWidth).toBeLessThanOrEqual(dimensions.viewportWidth);
}
