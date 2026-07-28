import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { EventListItem } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-events-page',
  imports: [DatePipe, FormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Operación</span>
          <h1>Eventos</h1>
          <p>Una vista clara de lo que viene y lo que necesita atención.</p>
        </div>
        @if (organization.hasPermission('events.create')) {
          <a class="btn btn--primary" routerLink="/app/events/new"> ＋ Nuevo evento </a>
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
              placeholder="Buscar evento o tipo"
              aria-label="Buscar eventos"
            />
          </label>
          <button class="btn btn--quiet" type="button" (click)="load()">Buscar</button>
          <span class="toolbar__count">{{ totalCount() }} eventos</span>
        </div>

        @if (loading()) {
          <div class="list-skeleton">
            @for (item of [1, 2, 3, 4]; track item) {
              <div class="skeleton skeleton--row"></div>
            }
          </div>
        } @else {
          <div class="event-card-grid">
            @for (event of events(); track event.id) {
              <a class="event-card" [routerLink]="['/app/events', event.id]">
                <div class="event-card__top">
                  <span class="date-tile">
                    <strong>{{ event.startDateTime | date: 'dd' }}</strong>
                    <small>{{ event.startDateTime | date: 'MMM' }}</small>
                  </span>
                  <span class="status-chip" [attr.data-status]="event.status">
                    {{ statusLabel(event.status) }}
                  </span>
                </div>
                <h2>{{ event.name }}</h2>
                <p>{{ event.eventType }} · {{ event.city }}</p>
                <div class="event-card__meta">
                  <span>{{ event.startDateTime | date: 'mediumDate' }}</span>
                  <span> {{ event.estimatedGuestCount ?? '—' }} invitados </span>
                </div>
                <span class="event-card__link">Abrir evento →</span>
              </a>
            } @empty {
              <div class="empty-state empty-state--wide">
                <span class="empty-state__icon">◇</span>
                <h3>Aún no hay eventos</h3>
                <p>Crea uno y empieza a reunir clientes, participantes y accesos.</p>
                @if (organization.hasPermission('events.create')) {
                  <a class="btn btn--secondary" routerLink="/app/events/new"> Crear evento </a>
                }
              </div>
            }
          </div>
        }
      </section>
    </div>
  `,
})
export class EventsPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly events = signal<EventListItem[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected search = '';

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.api
      .getEvents(this.organization.requireOrganizationId(), this.search)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (response) => {
          this.events.set(response.items);
          this.totalCount.set(response.totalCount);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Preliminary: 'Preliminar',
      Confirmed: 'Confirmado',
      Planning: 'Planeación',
      Suspended: 'Suspendido',
      Cancelled: 'Cancelado',
      Closed: 'Cerrado',
      Archived: 'Archivado',
    };
    return labels[status] ?? status;
  }
}
