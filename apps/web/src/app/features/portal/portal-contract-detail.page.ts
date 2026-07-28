import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { PortalContract } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-contract-detail-page',
  imports: [DatePipe, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <a class="back-link" routerLink="/portal/contracts">← Volver a contratos</a>
      @if (loading()) {
        <div class="skeleton skeleton--hero"></div>
      } @else if (error()) {
        <div class="notice notice--error">{{ error() }}</div>
      } @else if (contract(); as current) {
        <header class="portal-page__header">
          <div>
            <span class="eyebrow">{{ current.contractNumber }}</span>
            <h1>{{ current.name }}</h1>
            <p>
              Versión {{ current.version.versionNumber }}
              @if (current.version.validUntil) {
                · Vigente hasta {{ current.version.validUntil | date: 'dd MMM yyyy, HH:mm' }}
              }
            </p>
          </div>
          <span class="status-chip" [attr.data-status]="current.status">{{ current.status }}</span>
        </header>

        <div class="portal-detail-grid">
          <section class="panel">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Documento completo</span>
                <h2>Contenido contractual</h2>
              </div>
              <button class="btn btn--secondary" type="button" (click)="download(false)">
                Descargar PDF
              </button>
            </div>
            <article
              class="paper-sheet contract-content"
              [innerHTML]="current.version.renderedContent"
            ></article>
            <div class="hash-box">
              <span>SHA-256 de la versión</span>
              <code>{{ current.version.documentSha256 }}</code>
            </div>
          </section>

          <aside class="stack">
            <section class="panel">
              <span class="eyebrow">Partes</span>
              @for (party of current.parties; track party.id) {
                <div class="summary-row">
                  <span>{{
                    party.partyType === 'PlannerOrganization' ? 'Organización' : 'Cliente'
                  }}</span>
                  <strong>{{ party.displayName }}</strong>
                </div>
              }
            </section>
            <section class="panel">
              <span class="eyebrow">Estado de firmas</span>
              @for (signer of current.signers; track signer.signerRole) {
                <div class="summary-row">
                  <span>{{ signer.signerRole }}</span>
                  <span class="status-chip" [attr.data-status]="signer.status">{{
                    signer.status
                  }}</span>
                </div>
              }
              @if (current.pendingSignerId && current.pendingSignerName) {
                <div class="consent-box">
                  <p>{{ current.version.consentText }}</p>
                  <button
                    class="btn btn--primary btn--full"
                    type="button"
                    [disabled]="signing()"
                    (click)="sign()"
                  >
                    {{ signing() ? 'Firmando…' : 'Firmar con mi sesión' }}
                  </button>
                </div>
              }
              @if (current.hasFinalDocument) {
                <button class="btn btn--secondary btn--full" type="button" (click)="download(true)">
                  Descargar contrato final
                </button>
              }
            </section>
          </aside>
        </div>
      }
    </div>
  `,
})
export class PortalContractDetailPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly contractId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un contrato.');
    })();
  protected readonly contract = signal<PortalContract | null>(null);
  protected readonly loading = signal(true);
  protected readonly signing = signal(false);
  protected readonly error = signal('');

  constructor() {
    this.load();
  }

  protected sign(): void {
    const contract = this.contract();
    if (
      !contract?.pendingSignerId ||
      !contract.pendingSignerName ||
      !window.confirm(
        `¿Firmar la versión ${contract.version.versionNumber} como ${contract.pendingSignerName}?`,
      )
    ) {
      return;
    }
    this.signing.set(true);
    this.api
      .signPortalContract(contract.id, contract.pendingSignerId, contract.pendingSignerName)
      .pipe(
        finalize(() => this.signing.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Firma registrada con tu sesión.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected download(finalDocument: boolean): void {
    const operation = finalDocument
      ? this.api.downloadPortalFinalContract(this.contractId)
      : this.api.downloadPortalContract(this.contractId);
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener,noreferrer');
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }

  private load(): void {
    this.api
      .getPortalContract(this.contractId)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => this.contract.set(contract),
        error: (error: unknown) => this.error.set(getApiErrorMessage(error)),
      });
  }
}
