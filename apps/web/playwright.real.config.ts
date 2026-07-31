import { defineConfig, devices } from '@playwright/test';
import { API_HTTP_URL, E2E_CONNECTION_STRING } from './e2e-real/db.config';

/**
 * Suite separada de la principal (playwright.config.ts). Esta NO intercepta
 * /api/**: levanta la API real (.NET) contra una base PostgreSQL real y
 * aislada (ver db.config.ts, global-setup.ts) y ejercita Angular servido
 * contra esa API mediante proxy.real.conf.json. Cubre la sección 17 de la
 * auditoría (flujos integrales contra API/PostgreSQL reales), que la suite
 * con mocks (plannyt.fixture.ts) no puede demostrar por diseño.
 *
 * Un solo worker: los endpoints de autenticación comparten un límite de 10
 * solicitudes sensibles por IP/minuto (RateLimitPolicies.Sensitive) y estas
 * pruebas comparten loopback.
 */
export default defineConfig({
  testDir: './e2e-real/tests',
  globalSetup: require.resolve('./e2e-real/global-setup.ts'),
  globalTeardown: require.resolve('./e2e-real/global-teardown.ts'),
  fullyParallel: false,
  workers: 1,
  forbidOnly: Boolean(process.env['CI']),
  retries: 0,
  timeout: 120_000,
  expect: {
    timeout: 15_000,
  },
  reporter: [['line'], ['html', { open: 'never', outputFolder: 'playwright-report-real' }]],
  use: {
    baseURL: 'http://127.0.0.1:4200',
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'chromium-real',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    {
      command: 'dotnet run --project ../api/src/Plannyt.Api --no-launch-profile',
      url: `${API_HTTP_URL}/health/ready`,
      reuseExistingServer: false,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: API_HTTP_URL,
        ConnectionStrings__DefaultConnection: E2E_CONNECTION_STRING,
        Database__MigrateOnStartup: 'true',
        DemoSeed__Enabled: 'false',
        Cors__AllowedOrigin: 'http://127.0.0.1:4200',
        Frontend__PublicUrl: 'http://127.0.0.1:4200',
      },
    },
    {
      command: 'npm run start -- --host 127.0.0.1 --port 4200 --proxy-config proxy.real.conf.json',
      url: 'http://127.0.0.1:4200',
      reuseExistingServer: false,
      timeout: 120_000,
    },
  ],
});
