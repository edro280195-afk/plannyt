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
import { finalize, forkJoin, switchMap } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import {
  ContractListItem,
  ContractingReadiness,
  PaymentMethod,
  InstallmentType,
  PaymentPlan,
  PaymentRecord,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-event-contracting-page',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <a class="back-link" [routerLink]="['/app/events', eventId]">← Volver al evento</a>
      <header class="page-header">
        <div>
          <span class="eyebrow">Flujo central</span>
          <h1>Contratación del evento</h1>
          <p>Una sola vista para contrato, firmas, anticipo y confirmación.</p>
        </div>
        @if (
          readiness()?.readyForConfirmation &&
          readiness()?.eventStatus === 'Preliminary' &&
          readiness()?.confirmationMode === 'ManualAfterRequirements' &&
          organization.hasPermission('events.confirm')
        ) {
          <button
            class="btn btn--primary"
            type="button"
            [disabled]="working()"
            (click)="confirmEvent()"
          >
            Confirmar evento
          </button>
        }
      </header>

      @if (loading()) {
        <div class="skeleton skeleton--hero"></div>
      } @else if (readiness(); as state) {
        <ol class="contracting-steps contracting-steps--wide" aria-label="Estado de contratación">
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

        @if (state.missingRequirements.length > 0) {
          <div class="notice notice--warning">
            <strong>Aún faltan requisitos</strong>
            <span>{{ state.missingRequirements.join(' · ') }}</span>
          </div>
        } @else if (state.eventStatus === 'Preliminary') {
          <div class="notice notice--success">
            <strong>Listo para confirmar</strong>
            <span>El backend volvió a validar propuesta, contrato y anticipo.</span>
          </div>
        } @else {
          <div class="notice notice--success">
            <strong>Evento confirmado</strong>
            <span>La transición quedó registrada en el historial y auditoría.</span>
          </div>
        }

        <div class="dashboard-grid">
          <section class="panel">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Contrato y firmas</span>
                <h2>Contratos del evento</h2>
              </div>
              <a class="btn btn--quiet" routerLink="/app/contracts">Ver todos</a>
            </div>
            @if (contracts().length === 0) {
              <div class="empty-state">
                <h3>Contrato pendiente</h3>
                <p>Genera el contrato desde la propuesta aceptada.</p>
              </div>
            }
            @for (contract of contracts(); track contract.id) {
              <a class="data-row" [routerLink]="['/app/contracts', contract.id]">
                <span>
                  <small>{{ contract.contractNumber }}</small>
                  <strong>{{ contract.name }}</strong>
                </span>
                <span class="status-chip" [attr.data-status]="contract.status">
                  {{ contract.status }}
                </span>
              </a>
            }
          </section>

          <section class="panel metric-panel">
            <span class="eyebrow">Anticipo requerido</span>
            <strong class="metric-value">
              {{ state.requiredDepositAmount | currency: contracts()[0]?.currencyCode || 'MXN' }}
            </strong>
            <div class="progress-track">
              <span [style.width.%]="depositProgress()"></span>
            </div>
            <p>
              Recibido:
              {{ state.approvedDepositAmount | currency: contracts()[0]?.currencyCode || 'MXN' }}
            </p>
            <small>Solo cuentan pagos aprobados asignados a parcialidades tipo Anticipo.</small>
          </section>
        </div>

        <div class="split-layout">
          <section class="panel">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Cuentas por cobrar</span>
                <h2>Plan de pagos</h2>
              </div>
            </div>
            @if (plans().length === 0 && contracts()[0]; as contract) {
              <form class="form-grid" [formGroup]="planForm" (ngSubmit)="createPlan(contract)">
                <label class="field">
                  <span>Anticipo</span>
                  <input type="number" min="0" step="0.01" formControlName="depositAmount" />
                </label>
                <label class="field">
                  <span>Fecha límite del anticipo</span>
                  <input type="date" formControlName="depositDueDate" />
                </label>
                <label class="field">
                  <span>Fecha del pago final</span>
                  <input type="date" formControlName="finalDueDate" />
                </label>
                <div class="field">
                  <span>Total congelado</span>
                  <strong>{{
                    contract.contractGrandTotal | currency: contract.currencyCode
                  }}</strong>
                </div>
                <button class="btn btn--primary field--wide" type="submit" [disabled]="working()">
                  Crear plan en borrador
                </button>
              </form>
            }
            @for (plan of plans(); track plan.id) {
              <article class="plan-card">
                <div class="section-heading">
                  <div>
                    <strong>{{ plan.totalAmount | currency: plan.currencyCode }}</strong>
                    <small>Pagado {{ plan.approvedAmount | currency: plan.currencyCode }}</small>
                  </div>
                  <span class="status-chip" [attr.data-status]="plan.status">{{
                    plan.status
                  }}</span>
                </div>
                @for (installment of plan.installments; track installment.id) {
                  <div class="summary-row">
                    <span>
                      {{ installment.description }}
                      <small>{{ installment.dueDate | date: 'dd MMM yyyy' }}</small>
                    </span>
                    <strong>
                      {{ installment.pendingAmount | currency: plan.currencyCode }}
                      <small>pendiente</small>
                    </strong>
                  </div>
                }
                @if (
                  plan.status === 'Draft' && organization.hasPermission('payment-plans.activate')
                ) {
                  <button
                    class="btn btn--primary btn--full"
                    type="button"
                    (click)="activatePlan(plan)"
                  >
                    Activar plan
                  </button>
                }
              </article>
            }
          </section>

          <section class="panel">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Pagos manuales</span>
                <h2>Registro y revisión</h2>
              </div>
            </div>
            @if (activePlan(); as plan) {
              @if (organization.hasPermission('payments.create')) {
                <form class="form-grid" [formGroup]="paymentForm" (ngSubmit)="recordPayment(plan)">
                  <label class="field">
                    <span>Fecha</span>
                    <input type="date" formControlName="paymentDate" />
                  </label>
                  <label class="field">
                    <span>Importe</span>
                    <input type="number" min="0.01" step="0.01" formControlName="amount" />
                  </label>
                  <label class="field">
                    <span>Método</span>
                    <select formControlName="method">
                      <option value="BankTransfer">Transferencia</option>
                      <option value="Cash">Efectivo</option>
                      <option value="Deposit">Depósito</option>
                      <option value="CardExternal">Tarjeta externa</option>
                      <option value="Check">Cheque</option>
                      <option value="Other">Otro</option>
                    </select>
                  </label>
                  <label class="field">
                    <span>Referencia</span>
                    <input formControlName="reference" />
                  </label>
                  <button
                    class="btn btn--secondary field--wide"
                    type="submit"
                    [disabled]="working()"
                  >
                    Registrar pago pendiente
                  </button>
                </form>
              }
            } @else {
              <p class="helper-text">Activa un plan para registrar pagos.</p>
            }

            <div class="stack">
              @for (payment of payments(); track payment.id) {
                <article class="payment-card">
                  <div>
                    <strong>{{ payment.amount | currency: payment.currencyCode }}</strong>
                    <small
                      >{{ payment.paymentDate | date: 'dd MMM yyyy' }} ·
                      {{ payment.reference || payment.method }}</small
                    >
                  </div>
                  <span class="status-chip" [attr.data-status]="payment.status">{{
                    payment.status
                  }}</span>
                  @if (
                    payment.status === 'PendingReview' &&
                    organization.hasPermission('payments.approve')
                  ) {
                    <button
                      class="btn btn--primary"
                      type="button"
                      (click)="approveAndAllocate(payment)"
                    >
                      Aprobar y asignar
                    </button>
                  }
                  @if (
                    payment.status === 'PendingReview' &&
                    organization.hasPermission('payments.reject')
                  ) {
                    <button class="btn btn--quiet" type="button" (click)="reject(payment)">
                      Rechazar
                    </button>
                  }
                </article>
              }
            </div>
          </section>
        </div>
      }
    </div>
  `,
})
export class EventContractingPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly eventId =
    this.route.snapshot.paramMap.get('id') ??
    (() => {
      throw new Error('La ruta requiere un evento.');
    })();

  protected readonly readiness = signal<ContractingReadiness | null>(null);
  protected readonly contracts = signal<ContractListItem[]>([]);
  protected readonly plans = signal<PaymentPlan[]>([]);
  protected readonly payments = signal<PaymentRecord[]>([]);
  protected readonly loading = signal(true);
  protected readonly working = signal(false);
  protected readonly activePlan = computed(() =>
    this.plans().find((plan) => plan.status === 'Active'),
  );
  protected readonly depositProgress = computed(() => {
    const state = this.readiness();
    if (!state || state.requiredDepositAmount <= 0) {
      return 100;
    }
    return Math.min(100, (state.approvedDepositAmount / state.requiredDepositAmount) * 100);
  });
  protected readonly steps = computed(() => {
    const state = this.readiness();
    if (!state) {
      return [];
    }
    const prepared = this.contracts().some((contract) => contract.status !== 'Draft');
    const signatures =
      state.contractCompleted ||
      (this.contracts().length > 0 && state.missingRequiredSigners === 0);
    return [
      {
        label: 'Propuesta aceptada',
        detail: state.proposalAccepted ? 'Lista' : 'Pendiente',
        complete: state.proposalAccepted,
        current: !state.proposalAccepted,
      },
      {
        label: 'Contrato preparado',
        detail: prepared ? 'Publicado' : 'Pendiente',
        complete: prepared,
        current: state.proposalAccepted && !prepared,
      },
      {
        label: 'Firmas',
        detail: state.missingRequiredSigners
          ? `${state.missingRequiredSigners} pendientes`
          : 'Completas',
        complete: signatures,
        current: prepared && !signatures,
      },
      {
        label: 'Anticipo',
        detail: state.depositSatisfied ? 'Cubierto' : 'Pendiente',
        complete: state.depositSatisfied,
        current: signatures && !state.depositSatisfied,
      },
      {
        label: 'Evento confirmado',
        detail: state.eventStatus === 'Confirmed' ? 'Confirmado' : 'Preliminar',
        complete: state.eventStatus === 'Confirmed',
        current: state.readyForConfirmation && state.eventStatus === 'Preliminary',
      },
    ];
  });

  protected readonly planForm = new FormGroup({
    depositAmount: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0)],
    }),
    depositDueDate: new FormControl(this.dateValue(7), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    finalDueDate: new FormControl(this.dateValue(180), {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });
  protected readonly paymentForm = new FormGroup({
    paymentDate: new FormControl(this.dateValue(0), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    amount: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    method: new FormControl<PaymentMethod>('BankTransfer', { nonNullable: true }),
    reference: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    this.load();
  }

  protected createPlan(contract: ContractListItem): void {
    if (this.planForm.invalid) {
      return;
    }
    const value = this.planForm.getRawValue();
    if (value.depositAmount > contract.contractGrandTotal) {
      this.toast.error('El anticipo no puede exceder el total.');
      return;
    }
    const installments: {
      sequenceNumber: number;
      description: string;
      dueDate: string;
      amount: number;
      installmentType: InstallmentType;
    }[] = [];
    if (value.depositAmount > 0) {
      installments.push({
        sequenceNumber: 1,
        description: 'Anticipo de contratación',
        dueDate: value.depositDueDate,
        amount: value.depositAmount,
        installmentType: 'Deposit' as const,
      });
    }
    installments.push({
      sequenceNumber: installments.length + 1,
      description: 'Pago final',
      dueDate: value.finalDueDate,
      amount: contract.contractGrandTotal - value.depositAmount,
      installmentType: 'FinalPayment' as const,
    });
    this.working.set(true);
    this.api
      .createPaymentPlan(this.organization.requireOrganizationId(), {
        eventId: contract.eventId,
        clientId: contract.clientId,
        contractId: contract.id,
        proposalVersionId: null,
        currencyCode: contract.currencyCode,
        totalAmount: contract.contractGrandTotal,
        installments,
      })
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Plan de pagos creado en borrador.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected activatePlan(plan: PaymentPlan): void {
    this.working.set(true);
    this.api
      .activatePaymentPlan(this.organization.requireOrganizationId(), plan.id)
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Plan activado y total congelado.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected recordPayment(plan: PaymentPlan): void {
    if (this.paymentForm.invalid) {
      return;
    }
    const value = this.paymentForm.getRawValue();
    this.working.set(true);
    this.api
      .createPayment(this.organization.requireOrganizationId(), {
        eventId: plan.eventId,
        clientId: plan.clientId,
        paymentPlanId: plan.id,
        paymentDate: value.paymentDate,
        amount: value.amount,
        currencyCode: plan.currencyCode,
        method: value.method,
        reference: value.reference || null,
        notesShared: null,
        internalNotes: null,
      })
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Pago registrado para revisión.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected approveAndAllocate(payment: PaymentRecord): void {
    const plan = this.plans().find((item) => item.id === payment.paymentPlanId);
    const installment = plan?.installments.find(
      (item) => item.installmentType === 'Deposit' && item.pendingAmount > 0,
    );
    if (!installment) {
      this.toast.error('No existe una parcialidad de anticipo pendiente.');
      return;
    }
    const amount = Math.min(payment.amount, installment.pendingAmount);
    this.working.set(true);
    this.api
      .approvePayment(this.organization.requireOrganizationId(), payment.id)
      .pipe(
        switchMap(() =>
          this.api.allocatePayment(this.organization.requireOrganizationId(), payment.id, [
            { paymentInstallmentId: installment.id, amount },
          ]),
        ),
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Pago aprobado y asignado al anticipo.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected reject(payment: PaymentRecord): void {
    const reason = window.prompt('Motivo del rechazo:');
    if (!reason?.trim()) {
      return;
    }
    this.api
      .rejectPayment(this.organization.requireOrganizationId(), payment.id, reason)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toast.success('Pago rechazado.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected confirmEvent(): void {
    if (!window.confirm('¿Confirmar el evento después de revalidar todos los requisitos?')) {
      return;
    }
    this.working.set(true);
    this.api
      .confirmContractedEvent(this.organization.requireOrganizationId(), this.eventId)
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Evento confirmado.');
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private load(): void {
    const organizationId = this.organization.requireOrganizationId();
    this.loading.set(true);
    forkJoin({
      readiness: this.api.getContractingReadiness(organizationId, this.eventId),
      contracts: this.api.getContracts(organizationId, this.eventId),
      plans: this.api.getPaymentPlans(organizationId, this.eventId),
      payments: this.api.getPayments(organizationId, this.eventId),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ readiness, contracts, plans, payments }) => {
          this.readiness.set(readiness);
          this.contracts.set(contracts);
          this.plans.set(plans);
          this.payments.set(payments);
          if (this.planForm.controls.depositAmount.value === 0) {
            this.planForm.controls.depositAmount.setValue(readiness.requiredDepositAmount);
          }
          if (this.paymentForm.controls.amount.value === 0) {
            this.paymentForm.controls.amount.setValue(readiness.requiredDepositAmount);
          }
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private dateValue(daysFromNow: number): string {
    return new Date(Date.now() + daysFromNow * 86400000).toISOString().slice(0, 10);
  }
}
