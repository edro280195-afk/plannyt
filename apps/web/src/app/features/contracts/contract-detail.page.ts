import { CurrencyPipe, DatePipe } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ContractResponse,
  ContractSigner,
  SignatureEvidenceSummary,
  SigningMethod,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-contract-detail-page',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <a class="back-link" routerLink="/app/contracts">← Volver a contratos</a>
      @if (loading()) {
        <div class="skeleton skeleton--hero"></div>
      } @else if (contract(); as current) {
        <header class="event-hero">
          <div>
            <div class="event-hero__meta">
              <span class="eyebrow">{{ current.contractNumber }}</span>
              <span class="status-chip" [attr.data-status]="current.status">
                {{ statusLabel(current.status) }}
              </span>
            </div>
            <h1>{{ current.name }}</h1>
            <p>
              {{ current.contractGrandTotal | currency: current.currencyCode }} ·
              {{ sourceLabel(current.sourceType) }}
            </p>
            @if (current.cancelledAt) {
              <p class="helper-text">Motivo de cancelación: {{ current.cancellationReason }}</p>
            }
          </div>
          <div class="page-header__actions">
            @if (isCancellable(current) && organization.hasPermission('contracts.cancel')) {
              <button class="btn btn--quiet" type="button" [disabled]="working()" (click)="cancel()">
                Cancelar contrato
              </button>
            }
            <a
              class="btn btn--secondary"
              [routerLink]="['/app/events', current.eventId, 'contracting']"
            >
              Ver contratación del evento
            </a>
          </div>
        </header>

        <ol class="contracting-steps" aria-label="Etapas de contratación">
          @for (step of steps(); track step.label; let index = $index) {
            <li [class.is-complete]="step.complete" [class.is-current]="step.current">
              <span>{{ index + 1 }}</span>
              <div>
                <strong>{{ step.label }}</strong>
                <small>{{ step.detail }}</small>
              </div>
            </li>
          }
        </ol>

        <div class="split-layout">
          <section class="panel">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Versión {{ current.currentVersionNumber }}</span>
                <h2>Documento contractual</h2>
              </div>
              @if (current.versions[0]?.publishedAt) {
                <button class="btn btn--quiet" type="button" (click)="downloadVersion()">
                  Descargar PDF
                </button>
              }
            </div>

            @if (current.status === 'Draft') {
              <form class="form-grid" [formGroup]="draftForm" (ngSubmit)="saveDraft()">
                <label class="field field--wide">
                  <span>Nombre</span>
                  <input formControlName="name" />
                </label>
                <label class="field field--wide">
                  <span>Contenido</span>
                  <textarea formControlName="content" rows="14"></textarea>
                </label>
                <label class="field field--wide">
                  <span>Consentimiento</span>
                  <textarea formControlName="consentText" rows="4"></textarea>
                </label>
                <div class="form-actions field--wide">
                  <button class="btn btn--secondary" type="submit" [disabled]="working()">
                    Guardar borrador
                  </button>
                  @if (organization.hasPermission('contracts.publish')) {
                    <button
                      class="btn btn--primary"
                      type="button"
                      [disabled]="working()"
                      (click)="publish()"
                    >
                      Publicar versión inmutable
                    </button>
                  }
                </div>
              </form>
            } @else {
              <div
                class="contract-content"
                [innerHTML]="current.versions[0]?.renderedContent"
              ></div>
              @if (current.versions[0]?.documentSha256; as hash) {
                <div class="hash-box">
                  <span>SHA-256 del documento presentado</span>
                  <code>{{ hash }}</code>
                </div>
              }
            }

            @if (current.completedAt) {
              <div class="success-panel">
                <div>
                  <strong>Contrato completado</strong>
                  <p>
                    El documento original y el PDF final con anexo de evidencia se conservan por
                    separado.
                  </p>
                </div>
                <button class="btn btn--primary" type="button" (click)="downloadFinal()">
                  Descargar PDF firmado
                </button>
              </div>
            }
          </section>

          <aside class="stack">
            <section class="panel">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Partes legales</span>
                  <h2>Partes</h2>
                </div>
              </div>
              @for (party of current.parties; track party.id) {
                <div class="summary-row">
                  <span>{{
                    party.partyType === 'PlannerOrganization'
                      ? 'Organización'
                      : party.partyType === 'Client'
                        ? 'Cliente'
                        : 'Otra parte'
                  }}</span>
                  <strong>{{ party.displayName }}</strong>
                </div>
              }
            </section>

            <section class="panel">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Firma electrónica simple</span>
                  <h2>Firmantes</h2>
                </div>
              </div>
              @if (current.signers.length === 0) {
                <p class="helper-text">Agrega las personas que deben firmar esta versión.</p>
              }
              @for (signer of current.signers; track signer.id) {
                <article class="signer-card">
                  <div>
                    <strong>{{ signer.name }}</strong>
                    <small>{{ signer.signerRole }} · {{ signer.email }}</small>
                  </div>
                  <span class="status-chip" [attr.data-status]="signer.status">
                    {{ signerStatusLabel(signer.status) }}
                  </span>
                  @if (signer.status !== 'Signed' && isSignable(current)) {
                    <div class="card-actions">
                      @if (organization.hasPermission('signatures.create-request')) {
                        <button class="btn btn--quiet" type="button" (click)="createLink(signer)">
                          Crear enlace
                        </button>
                      }
                      @if (organization.hasPermission('signatures.countersign')) {
                        <button class="btn btn--quiet" type="button" (click)="countersign(signer)">
                          Firmar aquí
                        </button>
                      }
                      @if (
                        signer.activeSignatureRequestId &&
                        organization.hasPermission('signatures.revoke-request')
                      ) {
                        <button
                          class="btn btn--quiet"
                          type="button"
                          [disabled]="working()"
                          (click)="revokeRequest(signer)"
                        >
                          Revocar enlace
                        </button>
                      }
                    </div>
                  }
                  @if (signingLinks()[signer.id]; as link) {
                    <div class="copy-box">
                      <input [value]="link" readonly aria-label="Enlace privado de firma" />
                      <button class="btn btn--quiet" type="button" (click)="copyLink(link)">
                        Copiar
                      </button>
                    </div>
                  }
                </article>
              }

              @if (
                organization.hasPermission('signatures.manage-signers') &&
                current.status !== 'Completed' &&
                current.status !== 'Cancelled'
              ) {
                <details class="disclosure">
                  <summary>Agregar firmante</summary>
                  <form class="form-grid" [formGroup]="signerForm" (ngSubmit)="addSigner()">
                    <label class="field field--wide">
                      <span>Parte representada</span>
                      <select formControlName="contractPartyId">
                        @for (party of current.parties; track party.id) {
                          <option [value]="party.id">{{ party.displayName }}</option>
                        }
                      </select>
                    </label>
                    <label class="field">
                      <span>Nombre completo</span>
                      <input formControlName="name" />
                    </label>
                    <label class="field">
                      <span>Correo</span>
                      <input type="email" formControlName="email" />
                    </label>
                    <label class="field">
                      <span>Rol</span>
                      <input formControlName="signerRole" />
                    </label>
                    <label class="check-line">
                      <input type="checkbox" formControlName="isRequired" />
                      <span>Firma requerida</span>
                    </label>
                    <button
                      class="btn btn--secondary field--wide"
                      type="submit"
                      [disabled]="working()"
                    >
                      Agregar firmante
                    </button>
                  </form>
                </details>
              }

              @if (
                current.sourceType === 'ExternalUpload' &&
                current.status === 'Ready' &&
                current.signers.length > 0 &&
                organization.hasPermission('contracts.validate-external')
              ) {
                <button
                  class="btn btn--primary btn--full"
                  type="button"
                  (click)="validateExternal()"
                >
                  Validar carga externa
                </button>
                <small class="helper-text">
                  Esta acción certifica la carga del documento recibido; no verifica la autenticidad
                  de sus firmas.
                </small>
              }
            </section>

            @if (organization.hasPermission('signatures.view-evidence')) {
              <section class="panel">
                <div class="section-heading">
                  <div>
                    <span class="eyebrow">Auditoría de firma</span>
                    <h2>Evidencia</h2>
                  </div>
                </div>
                @if (evidence().length === 0) {
                  <p class="helper-text">Aún no hay firmas registradas para esta versión.</p>
                }
                @for (item of evidence(); track item.id) {
                  <div class="summary-row">
                    <span>
                      {{ item.declaredSignerName }}
                      <small>
                        {{ signingMethodLabel(item.signingMethod) }} ·
                        {{ item.signedAt | date: 'medium' }}
                      </small>
                    </span>
                  </div>
                  <div class="hash-box">
                    <code>{{ item.documentSha256 }}</code>
                  </div>
                }
              </section>
            }
          </aside>
        </div>
      }
    </div>
  `,
})
export class ContractDetailPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly contractId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un contrato.');
    })();

  protected readonly contract = signal<ContractResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly working = signal(false);
  protected readonly signingLinks = signal<Record<string, string>>({});
  protected readonly evidence = signal<SignatureEvidenceSummary[]>([]);
  protected readonly steps = computed(() => {
    const contract = this.contract();
    if (!contract) {
      return [];
    }
    const published = contract.versions.some((version) => version.publishedAt !== null);
    const required = contract.signers.filter((signer) => signer.isRequired);
    const signed = required.length > 0 && required.every((signer) => signer.status === 'Signed');
    return [
      {
        label: 'Propuesta aceptada',
        detail: contract.acceptedProposalId ? 'Versión vinculada' : 'Contrato manual',
        complete: !!contract.acceptedProposalId,
        current: !contract.acceptedProposalId,
      },
      {
        label: 'Contrato preparado',
        detail: published ? 'PDF publicado' : 'Borrador editable',
        complete: published,
        current: !published,
      },
      {
        label: 'Firmas',
        detail: `${required.filter((item) => item.status === 'Signed').length} de ${required.length}`,
        complete: signed,
        current: published && !signed,
      },
      {
        label: 'Anticipo',
        detail: `${contract.requirements.requiredDepositAmount} ${contract.currencyCode}`,
        complete: false,
        current: signed,
      },
      {
        label: 'Evento confirmado',
        detail: 'Se valida desde el evento',
        complete: false,
        current: false,
      },
    ];
  });

  protected readonly draftForm = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    content: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    consentText: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  protected readonly signerForm = new FormGroup({
    contractPartyId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    signerRole: new FormControl('Cliente contratante', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    isRequired: new FormControl(true, { nonNullable: true }),
  });

  constructor() {
    this.load();
  }

  protected saveDraft(): void {
    const contract = this.contract();
    if (!contract || this.draftForm.invalid) {
      return;
    }
    const value = this.draftForm.getRawValue();
    this.run(
      this.api.updateContractDraft(this.organization.requireOrganizationId(), contract.id, {
        name: value.name,
        templateId: contract.versions[0]?.templateId ?? null,
        content: value.content,
        consentText: value.consentText,
        validUntil: contract.versions[0]?.validUntil ?? null,
      }),
      'Borrador actualizado.',
    );
  }

  protected publish(): void {
    const contract = this.contract();
    if (!contract) {
      return;
    }
    this.working.set(true);
    this.api
      .publishContract(this.organization.requireOrganizationId(), contract.id)
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Versión publicada e inmutable.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected addSigner(): void {
    const contract = this.contract();
    if (!contract || this.signerForm.invalid) {
      this.signerForm.markAllAsTouched();
      return;
    }
    const value = this.signerForm.getRawValue();
    this.working.set(true);
    this.api
      .addContractSigner(this.organization.requireOrganizationId(), contract.id, {
        contractPartyId: value.contractPartyId,
        personId: null,
        userAccountId: null,
        name: value.name,
        email: value.email,
        signerRole: value.signerRole,
        signingOrder: contract.signers.length + 1,
        isRequired: value.isRequired,
      })
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Firmante agregado.');
          this.signerForm.patchValue({ name: '', email: '' });
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected createLink(signer: ContractSigner): void {
    const contract = this.contract();
    if (!contract) {
      return;
    }
    this.api
      .createSignatureRequest(this.organization.requireOrganizationId(), contract.id, signer.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (request) => {
          this.signingLinks.update((links) => ({ ...links, [signer.id]: request.signingUrl }));
          this.toast.success('Enlace privado generado por 7 días.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected countersign(signer: ContractSigner): void {
    const contract = this.contract();
    if (!contract || !window.confirm(`¿Firmar como ${signer.name}?`)) {
      return;
    }
    this.run(
      this.api.signAsOrganization(
        this.organization.requireOrganizationId(),
        contract.id,
        signer.id,
        signer.name,
      ),
      'Firma registrada con la sesión autenticada.',
    );
  }

  protected revokeRequest(signer: ContractSigner): void {
    const contract = this.contract();
    const requestId = signer.activeSignatureRequestId;
    if (!contract || !requestId || !window.confirm(`¿Revocar el enlace de ${signer.name}?`)) {
      return;
    }
    this.working.set(true);
    this.api
      .revokeSignatureRequest(this.organization.requireOrganizationId(), contract.id, requestId)
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.signingLinks.update((links) => {
            const rest = { ...links };
            delete rest[signer.id];
            return rest;
          });
          this.toast.success('Enlace de firma revocado.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected isCancellable(contract: ContractResponse): boolean {
    return (
      contract.status !== 'Completed' &&
      contract.status !== 'FullySigned' &&
      contract.status !== 'Cancelled'
    );
  }

  protected isSignable(contract: ContractResponse): boolean {
    return (
      contract.status !== 'Draft' &&
      contract.status !== 'Completed' &&
      contract.status !== 'Declined' &&
      contract.status !== 'Expired' &&
      contract.status !== 'Cancelled'
    );
  }

  protected cancel(): void {
    const contract = this.contract();
    if (!contract) {
      return;
    }
    const reason = window.prompt('Motivo de la cancelación:');
    if (!reason?.trim()) {
      return;
    }
    this.working.set(true);
    this.api
      .cancelContract(this.organization.requireOrganizationId(), contract.id, reason.trim())
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Contrato cancelado.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected validateExternal(): void {
    const contract = this.contract();
    if (!contract || !window.confirm('¿Confirmas que cargaste el documento externo recibido?')) {
      return;
    }
    this.run(
      this.api.validateExternalContract(
        this.organization.requireOrganizationId(),
        contract.id,
        new Date().toISOString(),
      ),
      'Contrato externo validado como documento recibido.',
    );
  }

  protected copyLink(link: string): void {
    void navigator.clipboard.writeText(link).then(
      () => this.toast.success('Enlace copiado.'),
      () => this.toast.error('No fue posible copiar el enlace.'),
    );
  }

  protected downloadVersion(): void {
    const contract = this.contract();
    const version = contract?.versions.find(
      (item) => item.publishedAt !== null && item.supersededAt === null,
    );
    if (!contract || !version) {
      return;
    }
    this.openBlob(
      this.api.downloadContractVersion(
        this.organization.requireOrganizationId(),
        contract.id,
        version.id,
      ),
    );
  }

  protected downloadFinal(): void {
    const contract = this.contract();
    if (contract) {
      this.openBlob(
        this.api.downloadFinalContract(this.organization.requireOrganizationId(), contract.id),
      );
    }
  }

  protected statusLabel(status: ContractResponse['status']): string {
    const labels: Record<ContractResponse['status'], string> = {
      Draft: 'Borrador',
      Ready: 'Listo',
      Sent: 'Enviado',
      Viewed: 'Visto',
      PartiallySigned: 'Firma parcial',
      FullySigned: 'Firmado',
      Completed: 'Completado',
      Declined: 'Rechazado',
      Expired: 'Vencido',
      Cancelled: 'Cancelado',
    };
    return labels[status];
  }

  protected signerStatusLabel(status: ContractSigner['status']): string {
    const labels: Record<ContractSigner['status'], string> = {
      Pending: 'Pendiente',
      Invited: 'Invitado',
      Viewed: 'Visto',
      Signed: 'Firmado',
      Declined: 'Rechazado',
      Expired: 'Vencido',
      Revoked: 'Revocado',
    };
    return labels[status];
  }

  protected sourceLabel(source: ContractResponse['sourceType']): string {
    return source === 'GeneratedFromProposal'
      ? 'Desde propuesta aceptada'
      : source === 'ExternalUpload'
        ? 'Documento externo'
        : 'Contrato manual';
  }

  protected signingMethodLabel(method: SigningMethod): string {
    const labels: Record<SigningMethod, string> = {
      Drawn: 'Firma dibujada',
      Typed: 'Firma escrita',
      AuthenticatedConfirmation: 'Confirmación con sesión autenticada',
      External: 'Documento externo',
    };
    return labels[method];
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .getContract(this.organization.requireOrganizationId(), this.contractId)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => {
          this.contract.set(contract);
          const version = contract.versions[0];
          this.draftForm.reset({
            name: contract.name,
            content: version?.renderedContent ?? '',
            consentText: version?.consentText ?? '',
          });
          if (!this.signerForm.controls.contractPartyId.value && contract.parties[0]) {
            this.signerForm.controls.contractPartyId.setValue(contract.parties[0].id);
          }
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
    if (this.organization.hasPermission('signatures.view-evidence')) {
      this.api
        .getContractEvidence(this.organization.requireOrganizationId(), this.contractId)
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (evidence) => this.evidence.set(evidence),
          error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
        });
    }
  }

  private run(operation: ReturnType<ApiService['updateContractDraft']>, message: string): void {
    this.working.set(true);
    operation
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => {
          this.contract.set(contract);
          this.toast.success(message);
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private openBlob(operation: ReturnType<ApiService['downloadContractVersion']>): void {
    operation.pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        window.open(url, '_blank', 'noopener,noreferrer');
        window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
      },
      error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
    });
  }
}
