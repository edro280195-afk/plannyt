import { Locator, Page } from '@playwright/test';

export class AuthPage {
  readonly email: Locator;
  readonly password: Locator;

  constructor(private readonly page: Page) {
    this.email = page.getByLabel('Correo electrónico');
    this.password = page.getByLabel('Contraseña', { exact: true });
  }

  async goToLogin(): Promise<void> {
    await this.page.goto('/auth/login');
  }

  async goToRegister(): Promise<void> {
    await this.page.goto('/auth/register');
  }

  async login(email: string, password: string): Promise<void> {
    await this.email.fill(email);
    await this.password.fill(password);
    await this.page.getByRole('button', { name: 'Entrar a Plannyt' }).click();
  }

  async registerPlanner(): Promise<void> {
    await this.page.getByLabel('Nombre', { exact: true }).fill('Mariana');
    await this.page.getByLabel('Apellido').fill('Torres');
    await this.email.fill('mariana@armonia.mx');
    await this.page
      .getByRole('textbox', {
        name: 'Contraseña Usa al menos 12 caracteres.',
      })
      .fill('UnaClaveSegura2026!');
    await this.page.getByLabel('Nombre de tu organización').fill('Armonía Eventos');
    await this.page.getByRole('button', { name: 'Crear organización' }).click();
  }
}
