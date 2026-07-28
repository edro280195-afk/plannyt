import { DatePipe } from '@angular/common';
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  ViewChild,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { PublicSignatureContract, SigningMethod } from '../../core/models/api.models';

@Component({
  selector: 'app-public-signature-page',
  imports: [DatePipe, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="public-document-page">
      <header class="public-document-header">
        <a class="brand brand--compact" href="/">
          <span class="brand__mark">P</span>
          <span>
            <strong>Plannyt</strong>
            <small>Firma electrónica simple</small>
          </span>
        </a>
        <span class="security-note">Enlace privado · No lo compartas</span>
      </header>

      @if (loading()) {
        <div class="public-document-layout">
          <div class="skeleton skeleton--hero"></div>
        </div>
      } @else if (error()) {
        <section class="public-state-card">
          <span class="state-icon">!</span>
          <h1>Este enlace no está disponible</h1>
          <p>{{ error() }}</p>
        </section>
      } @else if (contract(); as current) {
        <div class="public-document-layout">
          <section class="public-document-main">
            <div class="document-title">
              <div>
                <span class="eyebrow">{{ current.organizationName }}</span>
                <h1>{{ current.name }}</h1>
                <p>
                  {{ current.contractNumber }} · Versión {{ current.versionNumber }}
                  @if (current.validUntil) {
                    · Vigente hasta {{ current.validUntil | date: 'dd MMM yyyy, HH:mm' }}
                  }
                </p>
              </div>
              <button class="btn btn--secondary" type="button" (click)="downloadPdf()">
                Descargar PDF
              </button>
            </div>

            <div class="document-parties">
              <span>Partes</span>
              <strong>{{ current.parties.join(' · ') }}</strong>
            </div>
            <article
              class="paper-sheet contract-content"
              [innerHTML]="current.renderedContent"
            ></article>
            <div class="hash-box">
              <span>SHA-256 del documento presentado</span>
              <code>{{ current.documentSha256 }}</code>
            </div>
          </section>

          <aside class="public-sign-panel">
            @if (!current.canSign) {
              <div class="success-panel">
                <div>
                  <strong>Respuesta registrada</strong>
                  <p>Este enlace ya no admite otra firma.</p>
                </div>
              </div>
            } @else {
              <span class="eyebrow">Tu firma</span>
              <h2>{{ current.signerName }}</h2>
              <p>{{ current.signerEmail }}</p>

              <div class="signature-methods" role="group" aria-label="Método de firma">
                <button
                  type="button"
                  [class.is-active]="method() === 'Typed'"
                  (click)="method.set('Typed')"
                >
                  Escribir
                </button>
                <button
                  type="button"
                  [class.is-active]="method() === 'Drawn'"
                  (click)="selectDrawn()"
                >
                  Dibujar
                </button>
              </div>

              <form [formGroup]="form" (ngSubmit)="sign()">
                <label class="field">
                  <span>Confirma tu nombre completo</span>
                  <input formControlName="declaredSignerName" autocomplete="name" />
                </label>

                @if (method() === 'Typed') {
                  <div class="typed-signature" aria-label="Vista previa de firma escrita">
                    {{ form.controls.declaredSignerName.value || current.signerName }}
                  </div>
                } @else {
                  <div class="drawn-signature">
                    <canvas
                      #signatureCanvas
                      width="720"
                      height="240"
                      aria-label="Área para dibujar la firma"
                      (pointerdown)="startDrawing($event)"
                      (pointermove)="draw($event)"
                      (pointerup)="stopDrawing($event)"
                      (pointercancel)="stopDrawing($event)"
                    ></canvas>
                    <button class="btn btn--quiet" type="button" (click)="clearCanvas()">
                      Limpiar
                    </button>
                  </div>
                }

                <div class="consent-box">
                  <p>{{ current.consentText }}</p>
                  <label class="check-line">
                    <input type="checkbox" formControlName="acceptElectronicMeans" />
                    <span
                      >Acepto utilizar medios electrónicos para expresar mi consentimiento.</span
                    >
                  </label>
                  <label class="check-line">
                    <input type="checkbox" formControlName="confirmDisplayedVersion" />
                    <span
                      >Confirmo que deseo firmar la versión
                      {{ current.versionNumber }} mostrada.</span
                    >
                  </label>
                </div>

                @if (submitError()) {
                  <p class="form-error">{{ submitError() }}</p>
                }
                <button class="btn btn--primary btn--full" type="submit" [disabled]="submitting()">
                  {{ submitting() ? 'Registrando firma…' : 'Firmar esta versión' }}
                </button>
                <button class="btn btn--quiet btn--full" type="button" (click)="decline()">
                  No acepto el contrato
                </button>
              </form>
              <small class="legal-note">
                Plannyt registra una firma electrónica simple. No se presenta como firma electrónica
                avanzada, e.firma, NOM-151 ni verificación oficial de identidad.
              </small>
            }

            <div class="signer-progress">
              <strong>Estado de firmas</strong>
              @for (signer of current.signers; track signer.signerRole) {
                <div class="summary-row">
                  <span>{{ signer.signerRole }}</span>
                  <span class="status-chip" [attr.data-status]="signer.status">{{
                    signer.status
                  }}</span>
                </div>
              }
            </div>
          </aside>
        </div>
      }
    </main>
  `,
})
export class PublicSignaturePage implements AfterViewInit {
  @ViewChild('signatureCanvas')
  private canvas?: ElementRef<HTMLCanvasElement>;

  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly token =
    this.route.snapshot.paramMap.get('token') ??
    (() => {
      throw new Error('La ruta requiere un token.');
    })();
  private drawing = false;
  private canvasReady = false;

  protected readonly contract = signal<PublicSignatureContract | null>(null);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly error = signal('');
  protected readonly submitError = signal('');
  protected readonly method = signal<Extract<SigningMethod, 'Typed' | 'Drawn'>>('Typed');
  protected readonly form = new FormGroup({
    declaredSignerName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    acceptElectronicMeans: new FormControl(false, {
      nonNullable: true,
      validators: [Validators.requiredTrue],
    }),
    confirmDisplayedVersion: new FormControl(false, {
      nonNullable: true,
      validators: [Validators.requiredTrue],
    }),
  });

  constructor() {
    this.load();
  }

  ngAfterViewInit(): void {
    this.configureCanvas();
  }

  protected selectDrawn(): void {
    this.method.set('Drawn');
    queueMicrotask(() => this.configureCanvas());
  }

  protected startDrawing(event: PointerEvent): void {
    const canvas = this.canvas?.nativeElement;
    const context = canvas?.getContext('2d');
    if (!canvas || !context) {
      return;
    }
    canvas.setPointerCapture(event.pointerId);
    const point = this.canvasPoint(event, canvas);
    context.beginPath();
    context.moveTo(point.x, point.y);
    this.drawing = true;
    this.canvasReady = true;
  }

  protected draw(event: PointerEvent): void {
    const canvas = this.canvas?.nativeElement;
    const context = canvas?.getContext('2d');
    if (!this.drawing || !canvas || !context) {
      return;
    }
    const point = this.canvasPoint(event, canvas);
    context.lineTo(point.x, point.y);
    context.stroke();
  }

  protected stopDrawing(event: PointerEvent): void {
    const canvas = this.canvas?.nativeElement;
    if (canvas?.hasPointerCapture(event.pointerId)) {
      canvas.releasePointerCapture(event.pointerId);
    }
    this.drawing = false;
  }

  protected clearCanvas(): void {
    const canvas = this.canvas?.nativeElement;
    const context = canvas?.getContext('2d');
    if (canvas && context) {
      context.clearRect(0, 0, canvas.width, canvas.height);
      this.canvasReady = false;
    }
  }

  protected sign(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.submitError.set('Confirma tu nombre y ambas declaraciones.');
      return;
    }
    if (this.method() === 'Drawn' && !this.canvasReady) {
      this.submitError.set('Dibuja tu firma antes de continuar.');
      return;
    }
    this.submitError.set('');
    const value = this.form.getRawValue();
    const signatureDataUrl =
      this.method() === 'Drawn'
        ? (this.canvas?.nativeElement.toDataURL('image/png') ?? null)
        : null;
    this.submitting.set(true);
    this.api
      .submitPublicSignature(this.token, {
        signingMethod: this.method(),
        declaredSignerName: value.declaredSignerName,
        acceptElectronicMeans: value.acceptElectronicMeans,
        confirmDisplayedVersion: value.confirmDisplayedVersion,
        signatureDataUrl,
      })
      .pipe(
        finalize(() => this.submitting.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => this.contract.set(contract),
        error: (error: unknown) => this.submitError.set(getApiErrorMessage(error)),
      });
  }

  protected decline(): void {
    const reason = window.prompt('Si deseas, indica el motivo del rechazo:');
    if (reason === null) {
      return;
    }
    this.api
      .declinePublicSignature(this.token, reason || null)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          const current = this.contract();
          if (current) {
            this.contract.set({ ...current, canSign: false });
          }
        },
        error: (error: unknown) => this.submitError.set(getApiErrorMessage(error)),
      });
  }

  protected downloadPdf(): void {
    this.api
      .downloadPublicContractPdf(this.token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (blob) => {
          const url = URL.createObjectURL(blob);
          window.open(url, '_blank', 'noopener,noreferrer');
          window.setTimeout(() => URL.revokeObjectURL(url), 60_000);
        },
        error: (error: unknown) => this.submitError.set(getApiErrorMessage(error)),
      });
  }

  private load(): void {
    this.api
      .getPublicSignature(this.token)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (contract) => {
          this.contract.set(contract);
          this.form.controls.declaredSignerName.setValue(contract.signerName);
        },
        error: (error: unknown) => this.error.set(getApiErrorMessage(error)),
      });
  }

  private configureCanvas(): void {
    const context = this.canvas?.nativeElement.getContext('2d');
    if (context) {
      context.strokeStyle = '#2f2b35';
      context.lineWidth = 4;
      context.lineCap = 'round';
      context.lineJoin = 'round';
    }
  }

  private canvasPoint(event: PointerEvent, canvas: HTMLCanvasElement): { x: number; y: number } {
    const rect = canvas.getBoundingClientRect();
    return {
      x: ((event.clientX - rect.left) / rect.width) * canvas.width,
      y: ((event.clientY - rect.top) / rect.height) * canvas.height,
    };
  }
}
