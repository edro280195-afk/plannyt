import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import {
  ClientContact,
  ClientResponse,
  ClientType,
  CreateClientRequest,
} from '../../core/models/api.models';
import { getApiErrorMessage } from '../../core/errors/api-error';
import { ToastService } from '../../core/ui/toast.service';

@Component({
  selector: 'app-client-editor-page',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page page--narrow">
      <a class="back-link" routerLink="/app/clients">← Volver a clientes</a>
      <header class="page-header">
        <div>
          <span class="eyebrow">{{ isEditing() ? 'Editar cliente' : 'Nuevo cliente' }}</span>
          <h1>{{ isEditing() ? client()?.displayName : 'Registra un cliente' }}</h1>
          <p>Conserva sus datos dentro del contexto privado de tu organización.</p>
        </div>
        @if (
          isEditing() &&
          client()?.status === 'Active' &&
          organization.hasPermission('clients.archive')
        ) {
          <button class="btn btn--danger-quiet" type="button" (click)="archive()">Archivar</button>
        }
      </header>

      <form class="card card--padded form-stack" [formGroup]="form" (ngSubmit)="save()">
        <div class="segmented">
          <label>
            <input type="radio" formControlName="clientType" value="Person" />
            <span>Persona</span>
          </label>
          <label>
            <input type="radio" formControlName="clientType" value="Company" />
            <span>Empresa</span>
          </label>
        </div>

        <div class="form-grid">
          <label class="span-2">
            Nombre visible
            <input formControlName="displayName" placeholder="Ana Martínez" />
          </label>
          @if (form.controls.clientType.value === 'Company') {
            <label class="span-2">
              Razón social o nombre de empresa
              <input formControlName="companyName" />
            </label>
          } @else {
            <label>
              Nombre
              <input formControlName="firstName" />
            </label>
            <label>
              Apellido
              <input formControlName="lastName" />
            </label>
            <label>
              Correo de contacto
              <input type="email" formControlName="contactEmail" />
            </label>
            <label>
              Teléfono
              <input formControlName="contactPhone" />
            </label>
          }
          <label>
            ¿Cómo llegó contigo?
            <input formControlName="source" placeholder="Recomendación" />
          </label>
          <label>
            Zona horaria
            <input formControlName="timeZone" />
          </label>
        </div>

        <div class="form-actions">
          <a class="btn btn--quiet" routerLink="/app/clients">Cancelar</a>
          @if (
            (!isEditing() && organization.hasPermission('clients.create')) ||
            (isEditing() && organization.hasPermission('clients.update'))
          ) {
            <button class="btn btn--primary" type="submit" [disabled]="form.invalid || saving()">
              {{ saving() ? 'Guardando…' : 'Guardar cliente' }}
            </button>
          }
        </div>
      </form>

      @if (
        isEditing() &&
        client()?.clientType === 'Company' &&
        organization.hasPermission('clients.update')
      ) {
        <section class="card card--padded section-gap">
          <div class="section-heading">
            <div>
              <span class="eyebrow">Contactos</span>
              <h2>Personas relacionadas</h2>
            </div>
          </div>
          @for (contact of contacts(); track contact.id) {
            <div class="contact-row">
              <span class="avatar avatar--soft">{{ contact.displayName.charAt(0) }}</span>
              <span>
                <strong>{{ contact.displayName }}</strong>
                <small
                  >{{ contact.contactRole }} · {{ contact.contactEmail ?? 'Sin correo' }}</small
                >
              </span>
              @if (contact.isPrimary) {
                <span class="status-chip">Principal</span>
              }
            </div>
          } @empty {
            <p class="muted">Aún no has agregado contactos para esta empresa.</p>
          }

          <form class="form-grid contact-form" [formGroup]="contactForm" (ngSubmit)="addContact()">
            <label>Nombre <input formControlName="firstName" /></label>
            <label>Apellido <input formControlName="lastName" /></label>
            <label>Correo <input type="email" formControlName="contactEmail" /></label>
            <label>Teléfono <input formControlName="contactPhone" /></label>
            <label>Rol <input formControlName="contactRole" placeholder="Compras" /></label>
            <label class="checkbox-row checkbox-row--end">
              <input type="checkbox" formControlName="isPrimary" />
              <span>Contacto principal</span>
            </label>
            <button
              class="btn btn--secondary span-2"
              type="submit"
              [disabled]="contactForm.invalid"
            >
              Agregar contacto
            </button>
          </form>
        </section>
      }
    </div>
  `,
})
export class ClientEditorPage {
  private readonly api = inject(ApiService);
  protected readonly organization = inject(OrganizationContextService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly clientId = this.route.snapshot.paramMap.get('id');
  protected readonly isEditing = signal(this.clientId !== null);
  protected readonly client = signal<ClientResponse | null>(null);
  protected readonly contacts = signal<ClientContact[]>([]);
  protected readonly saving = signal(false);
  protected readonly form = new FormGroup({
    clientType: new FormControl<ClientType>('Person', { nonNullable: true }),
    displayName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(200)],
    }),
    companyName: new FormControl('', { nonNullable: true }),
    source: new FormControl('', { nonNullable: true }),
    firstName: new FormControl('', { nonNullable: true }),
    lastName: new FormControl('', { nonNullable: true }),
    contactEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email],
    }),
    contactPhone: new FormControl('', { nonNullable: true }),
    preferredLanguage: new FormControl('es', { nonNullable: true }),
    timeZone: new FormControl(
      Intl.DateTimeFormat().resolvedOptions().timeZone || 'America/Matamoros',
      { nonNullable: true, validators: [Validators.required] },
    ),
  });
  protected readonly contactForm = new FormGroup({
    firstName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    lastName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    contactEmail: new FormControl('', {
      nonNullable: true,
      validators: [Validators.email],
    }),
    contactPhone: new FormControl('', { nonNullable: true }),
    contactRole: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    isPrimary: new FormControl(false, { nonNullable: true }),
  });

  constructor() {
    if (this.clientId) {
      this.loadClient(this.clientId);
    }
  }

  protected save(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const organizationId = this.organization.requireOrganizationId();
    const request = this.buildRequest();
    const operation = this.clientId
      ? this.api.updateClient(organizationId, this.clientId, request)
      : this.api.createClient(organizationId, request);
    this.saving.set(true);
    operation
      .pipe(
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.saving.set(false)),
      )
      .subscribe({
        next: (client) => {
          this.toast.success('Cliente guardado correctamente.');
          void this.router.navigate(['/app/clients', client.id]);
          this.client.set(client);
          this.contacts.set(client.contacts);
          this.isEditing.set(true);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected archive(): void {
    if (!this.clientId || !window.confirm('¿Archivar este cliente?')) {
      return;
    }

    this.api
      .archiveClient(this.organization.requireOrganizationId(), this.clientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.toast.success('Cliente archivado.');
          void this.router.navigate(['/app/clients']);
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  protected addContact(): void {
    if (!this.clientId || this.contactForm.invalid) {
      return;
    }

    const value = this.contactForm.getRawValue();
    this.api
      .addClientContact(this.organization.requireOrganizationId(), this.clientId, {
        ...value,
        contactEmail: value.contactEmail || null,
        contactPhone: value.contactPhone || null,
        preferredLanguage: 'es',
        timeZone: this.form.controls.timeZone.value,
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (contact) => {
          this.contacts.update((contacts) => [...contacts, contact]);
          this.contactForm.reset({
            firstName: '',
            lastName: '',
            contactEmail: '',
            contactPhone: '',
            contactRole: '',
            isPrimary: false,
          });
          this.toast.success('Contacto agregado.');
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private loadClient(clientId: string): void {
    this.api
      .getClient(this.organization.requireOrganizationId(), clientId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (client) => {
          this.client.set(client);
          this.contacts.set(client.contacts);
          this.form.patchValue({
            clientType: client.clientType,
            displayName: client.displayName,
            companyName: client.companyName ?? '',
            source: client.source ?? '',
            firstName: client.person?.firstName ?? '',
            lastName: client.person?.lastName ?? '',
            contactEmail: client.person?.contactEmail ?? '',
            contactPhone: client.person?.contactPhone ?? '',
            preferredLanguage: client.person?.preferredLanguage ?? 'es',
            timeZone: client.person?.timeZone ?? Intl.DateTimeFormat().resolvedOptions().timeZone,
          });
        },
        error: (error: unknown) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private buildRequest(): CreateClientRequest {
    const value = this.form.getRawValue();
    const isPerson = value.clientType === 'Person';
    return {
      clientType: value.clientType,
      displayName: value.displayName,
      companyName: isPerson ? null : value.companyName,
      source: value.source || null,
      person: isPerson
        ? {
            firstName: value.firstName,
            lastName: value.lastName,
            contactEmail: value.contactEmail || null,
            contactPhone: value.contactPhone || null,
            preferredLanguage: value.preferredLanguage,
            timeZone: value.timeZone,
          }
        : null,
    };
  }
}
