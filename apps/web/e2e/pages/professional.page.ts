import { Page } from '@playwright/test';

export class ProfessionalPage {
  constructor(private readonly page: Page) {}

  async createClient(): Promise<void> {
    await this.page.goto('/app/clients/new');
    await this.page.getByLabel('Nombre visible').fill('Ana Martínez');
    await this.page.getByLabel('Nombre', { exact: true }).fill('Ana');
    await this.page.getByLabel('Apellido').fill('Martínez');
    await this.page.getByLabel('Correo de contacto').fill('ana@example.com');
    await this.page.getByLabel('¿Cómo llegó contigo?').fill('Recomendación');
    await this.page.getByRole('button', { name: 'Guardar cliente' }).click();
  }

  async createEvent(): Promise<void> {
    await this.page.goto('/app/events/new');
    await this.page.getByLabel('Nombre del evento').fill('Ana & Carlos');
    await this.page.getByLabel('Tipo').fill('Boda');
    await this.page.getByLabel('Invitados estimados').fill('180');
    await this.page.getByLabel('Inicio').fill('2027-03-20T18:00');
    await this.page.getByLabel('Fin').fill('2027-03-21T01:00');
    await this.page.getByLabel('Ciudad').fill('Monterrey');
    await this.page.getByLabel('Descripción compartida').fill('Una celebración al aire libre.');
    await this.page.getByRole('button', { name: 'Guardar evento' }).click();
  }

  async inviteClient(): Promise<void> {
    await this.page.goto('/app/events/event-1');
    await this.page.getByRole('button', { name: /Accesos/ }).click();
    await this.page.getByLabel('Correo objetivo').fill('ana@example.com');
    await this.page.getByRole('button', { name: 'Generar invitación' }).click();
  }
}
