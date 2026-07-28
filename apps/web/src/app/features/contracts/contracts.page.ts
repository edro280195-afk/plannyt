import { DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ClientListItem,
  ContractListItem,
  ContractTemplate,
  EventListItem,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-contracts-page',
  imports: [DatePipe, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Contratación</span>
          <h1>Contratos</h1>
          <p>Versiones, firmas y evidencia vinculadas al evento correcto.</p>
        </div>
        <div class="page-header__actions">
          @if (organization.hasPermission('contract-templates.view')) {
            <a class="btn btn--secondary" routerLink="/app/contract-templates">Plantillas</a>
          }
          @if (organization.hasPermission('contracts.create')) {
            <button class="btn btn--primary" type="button" (click)="showCreate.set(true)">
              Nuevo contrato
            </button>
          }
        </div>
      </header>

      @if (showCreate()) {
        <section class="panel">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Desde propuesta aceptada</span>
              <h2>Preparar contrato</h2>
            </div>
            <button
              class="icon-button"
              type="button"
              aria-label="Cerrar"
              (click)="showCreate.set(false)"
            >
              ×
            </button>
          </div>
          <form class="form-grid" [formGroup]="createForm" (ngSubmit)="createFromProposal()">
            <label class="field">
              <span>ID de propuesta aceptada</span>
              <input formControlName="proposalId" />
            </label>
            <label class="field">
              <span>Nombre del contrato</span>
              <input formControlName="name" maxlength="200" />
            </label>
            <label class="field">
              <span>Plantilla</span>
              <select formControlName="templateId">
                <option value="">Contenido predeterminado</option>
                @for (template of templates(); track template.id) {
                  <option [value]="template.id">{{ template.name }}</option>
                }
              </select>
            </label>
            <label class="field">
              <span>Vigencia para firma</span>
              <input type="datetime-local" formControlName="validUntil" />
            </label>
            <label class="field field--wide">
              <span>Texto de consentimiento</span>
              <textarea formControlName="consentText" rows="3"></textarea>
            </label>
            <div class="form-actions field--wide">
              <button
                class="btn btn--primary"
                type="submit"
                [disabled]="saving() || createForm.invalid"
              >
                {{ saving() ? 'Creando…' : 'Crear borrador' }}
              </button>
            </div>
          </form>

          <details class="disclosure">
            <summary>El contrato ya fue firmado fuera de Plannyt</summary>
            <form class="form-grid" [formGroup]="externalForm" (ngSubmit)="createExternal()">
              <label class="field">
                <span>Evento</span>
                <select formControlName="eventId">
                  <option value="">Selecciona</option>
                  @for (event of events(); track event.id) {
                    <option [value]="event.id">{{ event.name }}</option>
                  }
                </select>
              </label>
              <label class="field">
                <span>Cliente</span>
                <select formControlName="clientId">
                  <option value="">Selecciona</option>
                  @for (client of clients(); track client.id) {
                    <option [value]="client.id">{{ client.displayName }}</option>
                  }
                </select>
              </label>
              <label class="field">
                <span>Nombre</span>
                <input formControlName="name" />
              </label>
              <label class="field">
                <span>Total</span>
                <input type="number" min="0" step="0.01" formControlName="total" />
              </label>
              <label class="field">
                <span>Moneda</span>
                <input formControlName="currencyCode" maxlength="3" />
              </label>
              <label class="field">
                <span>PDF firmado externamente</span>
                <input type="file" accept="application/pdf" (change)="selectExternalFile($event)" />
              </label>
              <p class="field--wide helper-text">
                Plannyt conservará el hash y la declaración de carga, pero no afirmará que verificó
                criptográficamente las firmas externas.
              </p>
              <div class="form-actions field--wide">
                <button
                  class="btn btn--secondary"
                  type="submit"
                  [disabled]="saving() || externalForm.invalid || !externalFile()"
                >
                  Cargar contrato externo
                </button>
              </div>
            </form>
          </details>
        </section>
      }

      <section class="panel">
        @if (loading()) {
          <div class="skeleton skeleton--card"></div>
        } @else if (contracts().length === 0) {
          <div class="empty-state">
            <h2>No hay contratos todavía</h2>
            <p>Abre una propuesta aceptada para iniciar la contratación.</p>
          </div>
        } @else {
          <div class="data-list">
            @for (contract of contracts(); track contract.id) {
              <a class="data-row" [routerLink]="['/app/contracts', contract.id]">
                <span>
                  <small
                    >{{ contract.contractNumber }} · v{{ contract.currentVersionNumber }}</small
                  >
                  <strong>{{ contract.name }}</strong>
                  <small>Actualizado {{ contract.updatedAt | date: 'dd MMM yyyy, HH:mm' }}</small>
                </span>
                <span class="status-chip" [attr.data-status]="contract.status">
                  {{ statusLabel(contract.status) }}
                </span>
              </a>
            }
          </div>
        }
      </section>
    </div>
  `,
})
export class ContractsPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly contracts = signal<ContractListItem[]>([]);
  protected readonly templates = signal<ContractTemplate[]>([]);
  protected readonly clients = signal<ClientListItem[]>([]);
  protected readonly events = signal<EventListItem[]>([]);
  protected readonly showCreate = signal(false);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly externalFile = signal<File | null>(null);

  protected readonly createForm = new FormGroup({
    proposalId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('Contrato de prestación de servicios', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    templateId: new FormControl('', { nonNullable: true }),
    validUntil: new FormControl(this.toLocalDateTime(new Date(Date.now() + 7 * 86400000)), {
      nonNullable: true,
    }),
    consentText: new FormControl(
      'Declaro que he revisado el documento mostrado y acepto utilizar medios electrónicos ' +
        'para expresar mi consentimiento y firma respecto de esta versión del contrato.',
      { nonNullable: true, validators: [Validators.required] },
    ),
  });

  protected readonly externalForm = new FormGroup({
    eventId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    clientId: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    name: new FormControl('Contrato firmado externamente', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    total: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    currencyCode: new FormControl('MXN', {
      nonNullable: true,
      validators: [Validators.required, Validators.minLength(3), Validators.maxLength(3)],
    }),
  });

  constructor() {
    const proposalId = this.route.snapshot.queryParamMap.get('proposalId');
    if (proposalId) {
      this.createForm.controls.proposalId.setValue(proposalId);
      this.showCreate.set(true);
    }
    this.load();
  }

  protected createFromProposal(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }
    const value = this.createForm.getRawValue();
    this.saving.set(true);
    this.api
      .createContractFromProposal(this.organization.requireOrganizationId(), {
        proposalId: value.proposalId,
        name: value.name,
        templateId: value.templateId || null,
        content: null,
        consentText: value.consentText,
        validUntil: value.validUntil ? new Date(value.validUntil).toISOString() : null,
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => {
          this.toast.success('Contrato creado desde la versión aceptada.');
          void this.router.navigate(['/app/contracts', contract.id]);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected createExternal(): void {
    const file = this.externalFile();
    if (this.externalForm.invalid || !file) {
      this.externalForm.markAllAsTouched();
      return;
    }
    const value = this.externalForm.getRawValue();
    this.saving.set(true);
    this.api
      .createExternalContract(this.organization.requireOrganizationId(), {
        eventId: value.eventId,
        clientId: value.clientId,
        name: value.name,
        contractGrandTotal: value.total,
        currencyCode: value.currencyCode.toUpperCase(),
        validUntil: null,
        file,
      })
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => {
          this.toast.success('Contrato externo cargado. Registra firmantes y valídalo.');
          void this.router.navigate(['/app/contracts', contract.id]);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected selectExternalFile(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.externalFile.set(input.files?.item(0) ?? null);
  }

  protected statusLabel(status: ContractListItem['status']): string {
    const labels: Record<ContractListItem['status'], string> = {
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

  private load(): void {
    const organizationId = this.organization.requireOrganizationId();
    this.loading.set(true);
    forkJoin({
      contracts: this.api.getContracts(organizationId),
      templates: this.api.getContractTemplates(organizationId),
      clients: this.api.getClients(organizationId, '', 1, 100),
      events: this.api.getEvents(organizationId, '', 1, 100),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ contracts, templates, clients, events }) => {
          this.contracts.set(contracts);
          this.templates.set(templates.filter((template) => template.isActive));
          this.clients.set(clients.items);
          this.events.set(events.items);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private toLocalDateTime(date: Date): string {
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }
}
