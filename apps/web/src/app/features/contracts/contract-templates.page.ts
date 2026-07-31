import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ContractTemplate } from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-contract-templates-page',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <div>
          <span class="eyebrow">Contratación</span>
          <h1>Plantillas de contrato</h1>
          <p>Construye contenido seguro con variables controladas por Plannyt.</p>
        </div>
        <button class="btn btn--primary" type="button" (click)="newTemplate()">
          Nueva plantilla
        </button>
      </header>

      <div class="split-layout">
        <section class="panel">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Biblioteca</span>
              <h2>Plantillas activas</h2>
            </div>
          </div>
          @if (loading()) {
            <div class="skeleton skeleton--card"></div>
          } @else if (templates().length === 0) {
            <div class="empty-state">
              <h3>Aún no hay plantillas</h3>
              <p>Plannyt incluye un contenido base al crear el primer contrato.</p>
            </div>
          } @else {
            <div class="stack">
              @for (template of templates(); track template.id) {
                <button
                  class="list-card list-card--button"
                  type="button"
                  [class.is-selected]="selectedId() === template.id"
                  (click)="edit(template)"
                >
                  <span>
                    <strong>{{ template.name }}</strong>
                    <small>{{ template.description || 'Sin descripción' }}</small>
                  </span>
                  <span
                    class="status-chip"
                    [attr.data-status]="template.isActive ? 'Active' : 'Inactive'"
                  >
                    {{
                      template.isDefault
                        ? 'Predeterminada'
                        : template.isActive
                          ? 'Activa'
                          : 'Inactiva'
                    }}
                  </span>
                </button>
              }
            </div>
          }
        </section>

        <section class="panel">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Editor sencillo</span>
              <h2>{{ selectedId() ? 'Editar plantilla' : 'Nueva plantilla' }}</h2>
            </div>
          </div>
          <form class="form-grid" [formGroup]="form" (ngSubmit)="save()">
            <label class="field field--wide">
              <span>Nombre</span>
              <input formControlName="name" maxlength="200" />
            </label>
            <label class="field field--wide">
              <span>Descripción</span>
              <input formControlName="description" maxlength="2000" />
            </label>
            <label class="field field--wide">
              <span>Contenido HTML sanitizado</span>
              <textarea formControlName="content" rows="14"></textarea>
              <small> Variables: {{ organizationVariables }} </small>
            </label>
            <label class="check-line">
              <input type="checkbox" formControlName="isDefault" />
              <span>Usar como predeterminada</span>
            </label>
            <label class="check-line">
              <input type="checkbox" formControlName="isActive" />
              <span>Plantilla activa</span>
            </label>
            <div class="form-actions field--wide">
              <button class="btn btn--secondary" type="button" (click)="preview()">
                Previsualizar
              </button>
              @if (selectedId()) {
                <button
                  class="btn btn--quiet"
                  type="button"
                  [disabled]="saving()"
                  (click)="deleteTemplate()"
                >
                  Eliminar plantilla
                </button>
              }
              <button class="btn btn--primary" type="submit" [disabled]="saving() || form.invalid">
                {{ saving() ? 'Guardando…' : 'Guardar plantilla' }}
              </button>
            </div>
          </form>

          @if (previewHtml()) {
            <div class="preview-surface">
              <div class="section-heading">
                <div>
                  <span class="eyebrow">Vista previa segura</span>
                  <h3>Resultado</h3>
                </div>
                <span
                  class="status-chip"
                  [attr.data-status]="canPublish() ? 'Completed' : 'Pending'"
                >
                  {{ canPublish() ? 'Lista' : 'Requiere datos' }}
                </span>
              </div>
              @if (previewIssues().length > 0) {
                <p class="form-error">Revisa: {{ previewIssues().join(', ') }}</p>
              }
              <div class="contract-content" [innerHTML]="previewHtml()"></div>
            </div>
          }
        </section>
      </div>
    </div>
  `,
})
export class ContractTemplatesPage {
  private readonly api = inject(ApiService);
  private readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly templates = signal<ContractTemplate[]>([]);
  protected readonly selectedId = signal<string | null>(null);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly previewHtml = signal('');
  protected readonly previewIssues = signal<string[]>([]);
  protected readonly canPublish = signal(false);
  protected readonly organizationVariables =
    '{{organization.name}}, {{client.displayName}}, {{event.name}}, {{event.date}}, ' +
    '{{proposal.grandTotal}}, {{contract.number}}';

  protected readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    content: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    isDefault: new FormControl(false, { nonNullable: true }),
    isActive: new FormControl(true, { nonNullable: true }),
  });

  constructor() {
    this.load();
  }

  protected newTemplate(): void {
    this.selectedId.set(null);
    this.previewHtml.set('');
    this.form.reset({
      name: '',
      description: '',
      content:
        '<h1>Contrato de prestación de servicios</h1>\n' +
        '<p>Entre <strong>{{organization.name}}</strong> y ' +
        '<strong>{{client.displayName}}</strong> para {{event.name}}.</p>\n' +
        '<p>Total acordado: {{proposal.grandTotal}} {{proposal.currency}}.</p>',
      isDefault: this.templates().length === 0,
      isActive: true,
    });
  }

  protected edit(template: ContractTemplate): void {
    this.selectedId.set(template.id);
    this.previewHtml.set('');
    this.form.reset({
      name: template.name,
      description: template.description ?? '',
      content: template.content,
      isDefault: template.isDefault,
      isActive: template.isActive,
    });
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const organizationId = this.organization.requireOrganizationId();
    const value = this.form.getRawValue();
    const request = {
      name: value.name,
      description: value.description || null,
      content: value.content,
      isDefault: value.isDefault,
      isActive: value.isActive,
    };
    const operation = this.selectedId()
      ? this.api.updateContractTemplate(organizationId, this.selectedId()!, request)
      : this.api.createContractTemplate(organizationId, request);
    this.saving.set(true);
    operation
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (template) => {
          this.toast.success('Plantilla guardada.');
          this.selectedId.set(template.id);
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected deleteTemplate(): void {
    const templateId = this.selectedId();
    if (!templateId || !window.confirm('¿Eliminar esta plantilla?')) {
      return;
    }
    this.saving.set(true);
    this.api
      .archiveContractTemplate(this.organization.requireOrganizationId(), templateId)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.toast.success('Plantilla eliminada.');
          this.newTemplate();
          this.load();
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected preview(): void {
    const content = this.form.controls.content.value;
    if (!content.trim()) {
      return;
    }

    this.api
      .previewContractTemplate(this.organization.requireOrganizationId(), {
        content,
        eventId: null,
        clientId: null,
        proposalVersionId: null,
        contractId: null,
        validUntil: null,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (preview) => {
          this.previewHtml.set(preview.renderedContent);
          this.previewIssues.set([...preview.unknownVariables, ...preview.missingVariables]);
          this.canPublish.set(preview.canPublish);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private load(): void {
    this.loading.set(true);
    this.api
      .getContractTemplates(this.organization.requireOrganizationId())
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (templates) => {
          this.templates.set(templates);
          if (this.form.controls.name.value === '' && !this.selectedId()) {
            this.newTemplate();
          }
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }
}
