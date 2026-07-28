import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { PortalEvent } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-events-page',
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <header class="portal-welcome">
        <span class="eyebrow">Tus eventos</span>
        <h1>Todo lo que necesitas, sin el ruido.</h1>
        <p>Consulta la información que tu planner ha preparado para ti.</p>
      </header>

      @if (loading()) {
        <div class="portal-event-grid">
          @for (item of [1, 2]; track item) {
            <div class="card skeleton skeleton--portal"></div>
          }
        </div>
      } @else {
        <section class="portal-event-grid">
          @for (event of events(); track event.id) {
            <a class="portal-event-card" [routerLink]="['/portal/events', event.id]">
              <div class="portal-event-card__date">
                <span>{{ event.startDateTime | date: 'MMM' }}</span>
                <strong>{{ event.startDateTime | date: 'dd' }}</strong>
                <small>{{ event.startDateTime | date: 'yyyy' }}</small>
              </div>
              <div>
                <span class="eyebrow">{{ event.eventType }}</span>
                <h2>{{ event.name }}</h2>
                <p>{{ event.city }}, {{ event.countryCode }}</p>
                <span class="portal-event-card__link">Ver detalles →</span>
              </div>
            </a>
          } @empty {
            <div class="empty-state empty-state--wide">
              <span class="empty-state__icon">✦</span>
              <h3>No hay eventos compartidos</h3>
              <p>Cuando aceptes una invitación, aparecerá aquí.</p>
            </div>
          }
        </section>
      }
    </div>
  `,
})
export class PortalEventsPage {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly events = signal<PortalEvent[]>([]);
  protected readonly loading = signal(true);

  constructor() {
    this.api
      .getPortalEvents()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (events) => {
          this.events.set(events);
          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.loading.set(false);
          this.toast.error(getApiErrorMessage(error));
        },
      });
  }
}
