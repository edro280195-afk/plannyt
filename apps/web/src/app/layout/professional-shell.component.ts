import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';
import { OrganizationContextService } from '../core/auth/organization-context.service';

@Component({
  selector: 'app-professional-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="workspace-shell">
      <aside class="sidebar" [class.sidebar--open]="menuOpen()">
        <a class="brand" routerLink="/app/dashboard" (click)="closeMenu()">
          <span class="brand__mark">P</span>
          <span>
            <strong>Plannyt</strong>
            <small>Tu operación en armonía</small>
          </span>
        </a>

        <div class="org-pill">
          <span class="org-pill__avatar">
            {{ organization.organization()?.organizationName?.charAt(0) ?? 'P' }}
          </span>
          <span>
            <small>Organización</small>
            <strong>
              {{ organization.organization()?.organizationName ?? 'Plannyt' }}
            </strong>
          </span>
        </div>

        <nav class="sidebar-nav" aria-label="Navegación principal">
          <a routerLink="/app/dashboard" routerLinkActive="is-active" (click)="closeMenu()">
            <span aria-hidden="true">⌂</span> Inicio
          </a>
          @if (organization.hasPermission('clients.view')) {
            <a routerLink="/app/clients" routerLinkActive="is-active" (click)="closeMenu()">
              <span aria-hidden="true">♡</span> Clientes
            </a>
          }
          @if (organization.hasPermission('events.view')) {
            <a routerLink="/app/events" routerLinkActive="is-active" (click)="closeMenu()">
              <span aria-hidden="true">◇</span> Eventos
            </a>
          }
          @if (organization.hasPermission('organization.members.view')) {
            <a routerLink="/app/team" routerLinkActive="is-active" (click)="closeMenu()">
              <span aria-hidden="true">◎</span> Equipo
            </a>
          }
          @if (organization.hasPermission('organization.view')) {
            <a routerLink="/app/settings" routerLinkActive="is-active" (click)="closeMenu()">
              <span aria-hidden="true">⚙</span> Configuración
            </a>
          }
        </nav>

        <div class="sidebar__footer">
          <div class="user-line">
            <span class="avatar">{{ auth.me()?.email?.charAt(0)?.toUpperCase() }}</span>
            <span>
              <strong>{{ auth.me()?.email }}</strong>
              <small>{{ organization.organization()?.role }}</small>
            </span>
          </div>
          <button class="btn btn--quiet btn--full" type="button" (click)="auth.logout()">
            Cerrar sesión
          </button>
        </div>
      </aside>

      @if (menuOpen()) {
        <button
          type="button"
          class="sidebar-backdrop"
          aria-label="Cerrar menú"
          (click)="closeMenu()"
        ></button>
      }

      <main class="workspace-main">
        <header class="mobile-header">
          <button
            class="icon-button"
            type="button"
            aria-label="Abrir menú"
            (click)="menuOpen.set(true)"
          >
            ☰
          </button>
          <a class="brand brand--compact" routerLink="/app/dashboard">
            <span class="brand__mark">P</span>
            <strong>Plannyt</strong>
          </a>
        </header>
        <router-outlet />
      </main>
    </div>
  `,
})
export class ProfessionalShellComponent {
  protected readonly auth = inject(AuthService);
  protected readonly organization = inject(OrganizationContextService);
  protected readonly menuOpen = signal(false);

  protected closeMenu(): void {
    this.menuOpen.set(false);
  }
}
