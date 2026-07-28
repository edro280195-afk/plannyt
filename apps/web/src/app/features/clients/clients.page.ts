import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { ClientListItem } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-clients-page',
  imports: [FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">CRM</span>
          <h1>Clientes</h1>
          <p>Personas y empresas que dan origen a cada celebración.</p>
        </div>
        @if (organization.hasPermission('clients.create')) {
          <a class="btn btn--primary" routerLink="/app/clients/new"> ＋ Nuevo cliente </a>
        }
      </header>

      <section class="card">
        <div class="toolbar">
          <label class="search-field">
            <span aria-hidden="true">⌕</span>
            <input
              type="search"
              [(ngModel)]="search"
              (keyup.enter)="load()"
              placeholder="Buscar por nombre o empresa"
              aria-label="Buscar clientes"
            />
          </label>
          <button class="btn btn--quiet" type="button" (click)="load()">Buscar</button>
          <span class="toolbar__count">{{ totalCount() }} clientes</span>
        </div>

        @if (loading()) {
          <div class="list-skeleton">
            @for (item of [1, 2, 3, 4]; track item) {
              <div class="skeleton skeleton--row"></div>
            }
          </div>
        } @else {
          <div class="responsive-table">
            <table>
              <thead>
                <tr>
                  <th>Cliente</th>
                  <th>Tipo</th>
                  <th>Origen</th>
                  <th>Estado</th>
                  <th><span class="sr-only">Acciones</span></th>
                </tr>
              </thead>
              <tbody>
                @for (client of clients(); track client.id) {
                  <tr>
                    <td>
                      <a class="entity-cell" [routerLink]="['/app/clients', client.id]">
                        <span class="avatar avatar--soft">
                          {{ client.displayName.charAt(0) }}
                        </span>
                        <span>
                          <strong>{{ client.displayName }}</strong>
                          <small>{{ client.companyName ?? 'Cliente particular' }}</small>
                        </span>
                      </a>
                    </td>
                    <td>{{ client.clientType === 'Person' ? 'Persona' : 'Empresa' }}</td>
                    <td>{{ client.source ?? 'Sin especificar' }}</td>
                    <td>
                      <span class="status-chip" [attr.data-status]="client.status">
                        {{ client.status === 'Active' ? 'Activo' : client.status }}
                      </span>
                    </td>
                    <td>
                      <a
                        class="icon-button"
                        [routerLink]="['/app/clients', client.id]"
                        aria-label="Abrir cliente"
                      >
                        →
                      </a>
                    </td>
                  </tr>
                } @empty {
                  <tr>
                    <td colspan="5">
                      <div class="empty-state">
                        <span class="empty-state__icon">♡</span>
                        <h3>No hay clientes todavía</h3>
                        <p>Agrega el primero para empezar a construir su evento.</p>
                        @if (organization.hasPermission('clients.create')) {
                          <a class="btn btn--secondary" routerLink="/app/clients/new">
                            Agregar cliente
                          </a>
                        }
                      </div>
                    </td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        }
      </section>
    </div>
  `,
})
export class ClientsPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly clients = signal<ClientListItem[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected search = '';

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.api
      .getClients(this.organization.requireOrganizationId(), this.search)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.clients.set(response.items);
          this.totalCount.set(response.totalCount);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }
}
