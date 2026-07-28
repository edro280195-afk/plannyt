import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { AuthService } from '../core/auth/auth.service';

@Component({
  selector: 'app-portal-shell',
  imports: [RouterOutlet, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-shell">
      <header class="portal-header">
        <a class="brand brand--compact" routerLink="/portal/events">
          <span class="brand__mark">P</span>
          <span>
            <strong>Plannyt</strong>
            <small>Portal del cliente</small>
          </span>
        </a>
        <div class="portal-header__actions">
          <span class="hide-mobile">{{ auth.me()?.email }}</span>
          <button class="btn btn--quiet" type="button" (click)="auth.logout()">Salir</button>
        </div>
      </header>
      <main class="portal-main">
        <router-outlet />
      </main>
    </div>
  `,
})
export class PortalShellComponent {
  protected readonly auth = inject(AuthService);
}
