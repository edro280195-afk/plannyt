import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { PortalContractListItem } from '../../core/models/api.models';

@Component({
  selector: 'app-portal-contracts-page',
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <header class="portal-page__header">
        <div>
          <span class="eyebrow">Documentos</span>
          <h1>Mis contratos</h1>
          <p>Consulta la versión presentada, tus firmas pendientes y el documento final.</p>
        </div>
      </header>
      @if (loading()) {
        <div class="skeleton skeleton--hero"></div>
      } @else if (error()) {
        <div class="notice notice--error">{{ error() }}</div>
      } @else if (contracts().length === 0) {
        <div class="empty-state">
          <h2>No tienes contratos compartidos</h2>
          <p>Cuando la planner publique uno, aparecerá en esta sección.</p>
        </div>
      } @else {
        <div class="portal-card-grid">
          @for (contract of contracts(); track contract.id) {
            <a class="portal-card" [routerLink]="['/portal/contracts', contract.id]">
              <div class="portal-card__top">
                <span class="eyebrow">{{ contract.contractNumber }}</span>
                <span class="status-chip" [attr.data-status]="contract.status">{{
                  contract.status
                }}</span>
              </div>
              <h2>{{ contract.name }}</h2>
              <p>Versión {{ contract.currentVersionNumber }}</p>
              @if (contract.hasPendingSignature) {
                <span class="action-badge">Tu firma está pendiente</span>
              } @else if (contract.hasFinalDocument) {
                <span class="action-badge action-badge--complete">Documento final disponible</span>
              }
            </a>
          }
        </div>
      }
    </div>
  `,
})
export class PortalContractsPage {
  private readonly api = inject(ApiService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly contracts = signal<PortalContractListItem[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal('');

  constructor() {
    this.api
      .getPortalContracts()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contracts) => this.contracts.set(contracts),
        error: (error: unknown) => this.error.set(getApiErrorMessage(error)),
      });
  }
}
