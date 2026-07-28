import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { EventListItem } from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header page-header--hero">
        <div>
          <span class="eyebrow">Tu panorama de hoy</span>
          <h1>Todo en su lugar.</h1>
          <p>Mantén el pulso de tus eventos y avanza con intención.</p>
        </div>
        @if (organization.hasPermission('events.create')) {
          <a class="btn btn--primary" routerLink="/app/events/new">
            <span aria-hidden="true">＋</span> Crear evento
          </a>
        }
      </header>

      @if (loading()) {
        <div class="metric-grid">
          @for (item of [1, 2, 3]; track item) {
            <div class="card skeleton skeleton--metric"></div>
          }
        </div>
      } @else {
        <section class="metric-grid" aria-label="Resumen">
          <article class="metric-card metric-card--primary">
            <span>Eventos próximos</span>
            <strong>{{ upcomingEvents().length }}</strong>
            <small>En tu calendario</small>
          </article>
          <article class="metric-card">
            <span>Clientes activos</span>
            <strong>{{ clientCount() }}</strong>
            <small>Relaciones en movimiento</small>
          </article>
          <article class="metric-card">
            <span>Atención inmediata</span>
            <strong>{{ attentionCount() }}</strong>
            <small>Preliminares o suspendidos</small>
          </article>
        </section>
      }

      <section class="content-grid content-grid--dashboard">
        <article class="card card--padded">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Agenda</span>
              <h2>Próximos eventos</h2>
            </div>
            <a routerLink="/app/events">Ver todos</a>
          </div>

          @for (event of upcomingEvents(); track event.id) {
            <a class="event-row" [routerLink]="['/app/events', event.id]">
              <span class="date-tile">
                <strong>{{ event.startDateTime | date: 'dd' }}</strong>
                <small>{{ event.startDateTime | date: 'MMM' }}</small>
              </span>
              <span class="event-row__body">
                <strong>{{ event.name }}</strong>
                <small>{{ event.eventType }} · {{ event.city }}</small>
              </span>
              <span class="status-chip" [attr.data-status]="event.status">
                {{ statusLabel(event.status) }}
              </span>
              <span aria-hidden="true">→</span>
            </a>
          } @empty {
            <div class="empty-state">
              <span class="empty-state__icon">◇</span>
              <h3>Tu agenda está lista para comenzar</h3>
              <p>Crea el primer evento y conviértelo en un plan claro.</p>
              @if (organization.hasPermission('events.create')) {
                <a class="btn btn--secondary" routerLink="/app/events/new">
                  Crear mi primer evento
                </a>
              }
            </div>
          }
        </article>

        <aside class="card card--padded focus-card">
          <span class="eyebrow">Siguiente paso</span>
          <h2>Construye una base sólida</h2>
          <p>
            Registra a tus clientes antes de crear su evento. Así podrás relacionarlos sin duplicar
            información.
          </p>
          @if (organization.hasPermission('clients.create')) {
            <a class="btn btn--secondary btn--full" routerLink="/app/clients/new">
              Agregar cliente
            </a>
          }
          <div class="focus-card__quote">
            <span>✦</span>
            <p>Una operación tranquila empieza con información confiable.</p>
          </div>
        </aside>
      </section>
    </div>
  `,
})
export class DashboardPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly loading = signal(true);
  protected readonly upcomingEvents = signal<EventListItem[]>([]);
  protected readonly clientCount = signal(0);
  protected readonly attentionCount = signal(0);

  constructor() {
    const organizationId = this.organization.requireOrganizationId();
    forkJoin({
      events: this.organization.hasPermission('events.view')
        ? this.api.getEvents(organizationId)
        : of({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
      clients: this.organization.hasPermission('clients.view')
        ? this.api.getClients(organizationId)
        : of({ items: [], page: 1, pageSize: 50, totalCount: 0 }),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ events, clients }) => {
          const ordered = [...events.items]
            .filter((event) => new Date(event.startDateTime) >= new Date())
            .slice(0, 5);
          this.upcomingEvents.set(ordered);
          this.clientCount.set(clients.totalCount);
          this.attentionCount.set(
            events.items.filter(
              (event) => event.status === 'Preliminary' || event.status === 'Suspended',
            ).length,
          );
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
      Planning: 'En planeación',
      Suspended: 'Suspendido',
      Cancelled: 'Cancelado',
      Closed: 'Cerrado',
      Archived: 'Archivado',
    };
    return labels[status] ?? status;
  }
}
