import { Component, ChangeDetectionStrategy, DestroyRef, signal, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { ApiService } from '../../core/api/api.service';
import { ToastService } from '../../core/ui/toast.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import type { RsvpDashboardResponse } from '../../core/models/api.models';

@Component({
  selector: 'app-portal-rsvp-dashboard-page',
  standalone: true,
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <a class="back-link" [routerLink]="['/portal/events', eventId]">← Volver al evento</a>
      <h1>RSVP del evento</h1>
      @if (loading()) {
        <p>Cargando resumen...</p>
      } @else if (dash(); as d) {
        <div class="stats-row">
          <div class="stat">
            <span class="stat__value">{{ d.guestsConfirmed }}</span>
            <span class="stat__label">Confirmados</span>
          </div>
          <div class="stat">
            <span class="stat__value">{{ d.guestsNotAttending }}</span>
            <span class="stat__label">No asistirán</span>
          </div>
          <div class="stat">
            <span class="stat__value">{{ d.guestsPending }}</span>
            <span class="stat__label">Pendientes</span>
          </div>
        </div>
        <div class="groups-section">
          <h2>Grupos ({{ d.groups.length }})</h2>
          @for (g of d.groups; track g.groupId) {
            <div class="group-row">
              <span class="group-row__name">{{ g.groupName }}</span>
              <span class="group-row__status">{{ g.status || 'Sin respuesta' }}</span>
              <span class="group-row__count">
                {{ g.confirmedCount }} ✓ / {{ g.declinedCount }} ✗ / {{ g.pendingCount }} ?
              </span>
              @if (g.lastResponseAt) {
                <small>{{ g.lastResponseAt | date:'short' }}</small>
              }
              <div class="group-row__tags">
                @if (g.hasMenuSelection) { <span class="tag">Menú ✓</span> }
                @if (g.hasTransport) { <span class="tag">Transporte ✓</span> }
                @if (g.hasAccommodation) { <span class="tag">Hospedaje ✓</span> }
              </div>
            </div>
          }
        </div>
        @if (d.closesAt) {
          <p class="close-info">El RSVP cierra: {{ d.closesAt | date:'medium' }}</p>
        }
      }
    </div>
  `,
  styles: [`
    :host { display: block; padding: 24px; max-width: 800px; margin: 0 auto; }
    .back-link { display: inline-block; margin-bottom: 16px; color: #1a73e8; text-decoration: none; font-size: 14px; }
    .stats-row { display: flex; gap: 16px; margin-bottom: 32px; }
    .stat { background: #f8f9fa; padding: 20px; border-radius: 8px; text-align: center; flex: 1; }
    .stat__value { font-size: 32px; font-weight: 700; display: block; }
    .stat__label { font-size: 13px; color: #666; }
    .groups-section { margin-top: 16px; }
    .group-row { padding: 12px; border-bottom: 1px solid #eee; display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
    .group-row__name { font-weight: 600; min-width: 150px; }
    .group-row__status { color: #1a73e8; font-size: 13px; }
    .group-row__count { color: #666; font-size: 13px; }
    .group-row__tags { display: flex; gap: 4px; }
    .tag { padding: 2px 6px; border-radius: 4px; font-size: 11px; background: #e8f5e9; color: #1e8e3e; }
    .close-info { margin-top: 24px; color: #e37400; font-size: 14px; }
  `]
})
export class PortalRsvpDashboardPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly eventId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un evento.');
    })();
  protected readonly dash = signal<RsvpDashboardResponse | null>(null);
  protected readonly loading = signal(true);

  constructor() {
    this.route.params.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(params => {
      const eventId = params['id'] as string;
      this.loadData(eventId);
    });
  }

  private loadData(eventId: string): void {
    this.api.getPortalRsvpDashboard(eventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (d) => { this.dash.set(d); this.loading.set(false); },
        error: (err) => { this.toast.error(getApiErrorMessage(err)); this.loading.set(false); },
      });
  }
}
