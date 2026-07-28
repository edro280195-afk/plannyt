import { Page } from '@playwright/test';

export class PortalPage {
  constructor(private readonly page: Page) {}

  async acceptNewAccount(token: string): Promise<void> {
    await this.page.goto(`/accept-access/${token}`);
    await this.page.getByLabel('Nombre', { exact: true }).fill('Ana');
    await this.page.getByLabel('Apellido').fill('Martínez');
    await this.page.getByLabel('Crea una contraseña').fill('OtraClaveSegura2026!');
    await this.page.getByRole('button', { name: 'Crear cuenta y aceptar' }).click();
  }

  async openAuthorizedEvent(): Promise<void> {
    await this.page.goto('/portal/events');
    await this.page.getByRole('link', { name: /Ana & Carlos/ }).click();
  }
}
