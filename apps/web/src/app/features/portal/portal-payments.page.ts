import { CurrencyPipe, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize, forkJoin } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { PaymentMethod, PaymentPlan, PortalPaymentRecord } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-portal-payments-page',
  imports: [CurrencyPipe, DatePipe, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="portal-page">
      <header class="portal-page__header">
        <div>
          <span class="eyebrow">Cuenta del evento</span>
          <h1>Mis pagos</h1>
          <p>Consulta parcialidades, registra un pago y adjunta su comprobante.</p>
        </div>
      </header>

      @if (loading()) {
        <div class="skeleton skeleton--hero"></div>
      } @else if (error()) {
        <div class="notice notice--error">{{ error() }}</div>
      } @else {
        <div class="portal-detail-grid">
          <section class="panel">
            <div class="section-heading">
              <div>
                <span class="eyebrow">Calendario</span>
                <h2>Planes de pago</h2>
              </div>
            </div>
            @for (plan of plans(); track plan.id) {
              <article class="plan-card">
                <div class="section-heading">
                  <div>
                    <strong>{{ plan.totalAmount | currency: plan.currencyCode }}</strong>
                    <small>
                      Pendiente {{ plan.pendingAmount | currency: plan.currencyCode }}
                    </small>
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
                      <small>{{ installment.status }}</small>
                    </strong>
                  </div>
                }
                @if (plan.status === 'Active' && plan.pendingAmount > 0) {
                  <button
                    class="btn btn--primary btn--full"
                    type="button"
                    (click)="selectPlan(plan)"
                  >
                    Registrar pago para este plan
                  </button>
                }
              </article>
            } @empty {
              <div class="empty-state">
                <h3>No hay planes compartidos</h3>
                <p>La planner publicará aquí las parcialidades acordadas.</p>
              </div>
            }
          </section>

          <aside class="stack">
            @if (selectedPlan(); as plan) {
              <section class="panel">
                <span class="eyebrow">Nuevo movimiento</span>
                <h2>Reportar pago</h2>
                <form class="form-stack" [formGroup]="paymentForm" (ngSubmit)="submitPayment(plan)">
                  <label>
                    Fecha
                    <input type="date" formControlName="paymentDate" />
                  </label>
                  <label>
                    Importe ({{ plan.currencyCode }})
                    <input type="number" min="0.01" step="0.01" formControlName="amount" />
                  </label>
                  <label>
                    Método
                    <select formControlName="method">
                      <option value="BankTransfer">Transferencia</option>
                      <option value="Deposit">Depósito</option>
                      <option value="Cash">Efectivo</option>
                      <option value="CardExternal">Tarjeta externa</option>
                      <option value="Check">Cheque</option>
                      <option value="Other">Otro</option>
                    </select>
                  </label>
                  <label>
                    Referencia
                    <input formControlName="reference" maxlength="120" />
                  </label>
                  <label>
                    Nota para la planner
                    <textarea formControlName="notesShared" rows="3"></textarea>
                  </label>
                  <button class="btn btn--primary btn--full" type="submit" [disabled]="working()">
                    {{ working() ? 'Registrando…' : 'Enviar para revisión' }}
                  </button>
                </form>
              </section>
            }

            <section class="panel">
              <span class="eyebrow">Historial</span>
              <h2>Pagos reportados</h2>
              @for (payment of payments(); track payment.id) {
                <article class="payment-card payment-card--portal">
                  <div>
                    <strong>{{ payment.amount | currency: payment.currencyCode }}</strong>
                    <small>
                      {{ payment.paymentDate | date: 'dd MMM yyyy' }}
                      · {{ payment.reference || payment.method }}
                    </small>
                  </div>
                  <span class="status-chip" [attr.data-status]="payment.status">
                    {{ statusLabel(payment.status) }}
                  </span>
                  @if (payment.rejectionReason) {
                    <p class="notice notice--error">{{ payment.rejectionReason }}</p>
                  }
                  <label class="receipt-upload">
                    <span>Adjuntar comprobante PDF o imagen</span>
                    <input
                      type="file"
                      accept="application/pdf,image/jpeg,image/png,image/webp"
                      [disabled]="working()"
                      (change)="uploadReceipt(payment, $event)"
                    />
                  </label>
                  @for (receipt of payment.receipts; track receipt.documentId) {
                    <small class="receipt-line">✓ {{ receipt.fileName }}</small>
                  }
                </article>
              } @empty {
                <p class="helper-text">Todavía no has reportado pagos.</p>
              }
            </section>
          </aside>
        </div>
      }
    </div>
  `,
})
export class PortalPaymentsPage {
  private readonly api = inject(ApiService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly plans = signal<PaymentPlan[]>([]);
  protected readonly payments = signal<PortalPaymentRecord[]>([]);
  protected readonly selectedPlan = signal<PaymentPlan | null>(null);
  protected readonly loading = signal(true);
  protected readonly working = signal(false);
  protected readonly error = signal('');

  protected readonly paymentForm = new FormGroup({
    paymentDate: new FormControl(new Date().toISOString().slice(0, 10), {
      nonNullable: true,
      validators: [Validators.required],
    }),
    amount: new FormControl(0, {
      nonNullable: true,
      validators: [Validators.required, Validators.min(0.01)],
    }),
    method: new FormControl<PaymentMethod>('BankTransfer', { nonNullable: true }),
    reference: new FormControl('', { nonNullable: true }),
    notesShared: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    this.load();
  }

  protected selectPlan(plan: PaymentPlan): void {
    this.selectedPlan.set(plan);
    const deposit = plan.installments.find(
      (installment) => installment.installmentType === 'Deposit' && installment.pendingAmount > 0,
    );
    const nextInstallment =
      deposit ?? plan.installments.find((installment) => installment.pendingAmount > 0);
    this.paymentForm.controls.amount.setValue(nextInstallment?.pendingAmount ?? plan.pendingAmount);
  }

  protected submitPayment(plan: PaymentPlan): void {
    if (this.paymentForm.invalid) {
      this.paymentForm.markAllAsTouched();
      return;
    }
    const value = this.paymentForm.getRawValue();
    if (value.amount > plan.pendingAmount) {
      this.toast.error('El importe no puede exceder el saldo pendiente del plan.');
      return;
    }
    this.working.set(true);
    this.api
      .createPortalPayment({
        paymentPlanId: plan.id,
        paymentDate: value.paymentDate,
        amount: value.amount,
        method: value.method,
        reference: value.reference.trim() || null,
        notesShared: value.notesShared.trim() || null,
      })
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (payment) => {
          this.toast.success('Pago enviado para revisión.');
          this.payments.update((payments) => [payment, ...payments]);
          this.selectedPlan.set(null);
          this.paymentForm.reset({
            paymentDate: new Date().toISOString().slice(0, 10),
            amount: 0,
            method: 'BankTransfer',
            reference: '',
            notesShared: '',
          });
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected uploadReceipt(payment: PortalPaymentRecord, event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.item(0);
    if (!file) {
      return;
    }
    this.working.set(true);
    this.api
      .uploadPortalPaymentReceipt(payment.id, file)
      .pipe(
        finalize(() => this.working.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (receipt) => {
          this.payments.update((payments) =>
            payments.map((item) =>
              item.id === payment.id ? { ...item, receipts: [...item.receipts, receipt] } : item,
            ),
          );
          input.value = '';
          this.toast.success('Comprobante adjuntado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected statusLabel(status: PortalPaymentRecord['status']): string {
    const labels: Record<PortalPaymentRecord['status'], string> = {
      PendingReview: 'En revisión',
      Approved: 'Aprobado',
      Rejected: 'Rechazado',
      Cancelled: 'Cancelado',
      Refunded: 'Reembolsado',
    };
    return labels[status];
  }

  private load(): void {
    forkJoin({
      plans: this.api.getPortalPaymentPlans(),
      payments: this.api.getPortalPayments(),
    })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: ({ plans, payments }) => {
          this.plans.set(plans);
          this.payments.set(payments);
        },
        error: (error: unknown) => this.error.set(getApiErrorMessage(error)),
      });
  }
}
