import { DatePipe, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ApiService } from '../../core/api/api.service';
import { IdempotencyAttempt } from '../../core/api/idempotency-attempt';
import { getApiErrorMessage, requiresReload } from '../../core/errors/api-error';
import type {
  EventAccommodationOptionResponse,
  EventMenuResponse,
  EventTransportOptionResponse,
  GuestAttendanceStatus,
  GuestRsvpStateResponse,
  RsvpQuestion,
  RsvpOverallStatus,
  RsvpSubmissionAnswerRequest,
  RsvpSubmissionGuestRequest,
  RsvpSubmissionRequest,
  RsvpSubmissionResponse,
  AccommodationSelectionStatus,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';

interface WizardGuest {
  eventGuestId: string | null;
  displayName: string;
  ageCategory: string;
  isNamed: boolean;
}

function safeJsonParse<T>(json: string | null | undefined, fallback: T): T {
  if (!json || json === '[]') return fallback;
  try {
    return JSON.parse(json) as T;
  } catch {
    return fallback;
  }
}

@Component({
  selector: 'app-public-rsvp-page',
  standalone: true,
  imports: [ReactiveFormsModule, DatePipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="rsvp-wizard">
      <header class="rsvp-wizard__header">
        @if (state(); as s) {
          <h1>{{ s.groupName }}</h1>
          <p>Hasta {{ s.allowedGuestCount }} invitado(s) en este grupo.</p>
        }
        <div class="rsvp-wizard__steps">
          @for (label of stepLabels(); track label; let i = $index) {
            <span
              class="step-dot"
              [class.active]="activeStep() === i"
              [class.done]="activeStep() > i"
            ></span>
          }
        </div>
      </header>

      <main class="rsvp-wizard__body">
        @if (loading()) {
          <p class="loading">Cargando formulario de confirmación…</p>
        } @else if (error()) {
          <div class="error-state">
            <h2>{{ error() }}</h2>
            @if (closedMessage()) {
              <p class="closed-msg">{{ closedMessage() }}</p>
            }
          </div>
        } @else if (confirmation(); as conf) {
          <div class="confirmation">
            <span class="confirmation__icon">&#10003;</span>
            <h2>¡Respuesta registrada!</h2>
            @if (conf.confirmationCode) {
              <p class="confirmation__code">Código: <strong>{{ conf.confirmationCode }}</strong></p>
            }
            <p>
              {{ conf.guests.length }} invitado(s) procesado(s)
              &middot;
              {{ attendingCount(conf) }} confirmado(s)
            </p>
            <button class="btn-secondary" (click)="resetWizard()">Modificar respuesta</button>
          </div>
        } @else {
          @switch (activeStep()) {
            @case (0) {
              <section class="wizard-step">
                <h2>Bienvenida</h2>
                @if (state(); as s) {
                  <p>
                    Has recibido <strong>{{ s.allowedGuestCount }}</strong> lugar(es) en
                    <em>{{ s.groupName }}</em>.
                  </p>
                  @if (wizardGuests().length > 0) {
                    <p>Invitados nombrados:</p>
                    <ul class="guest-list">
                      @for (g of wizardGuests(); track g.eventGuestId ?? g.displayName) {
                        <li>{{ g.displayName }} &middot; <small>{{ ageLabel(g.ageCategory) }}</small></li>
                      }
                    </ul>
                  } @else {
                    <p>Tu grupo no tiene invitados nombrados. Puedes registrar acompañantes si están permitidos.</p>
                  }
                  @if (s.allowUnnamedCompanions && s.maxUnnamedCompanions > 0) {
                    <p class="companion-note">
                      Puedes agregar hasta <strong>{{ availableCompanionSlots() }}</strong> acompañante(s) sin nombre previo.
                    </p>
                  }
                  @if (s.settings?.allowContactInformationUpdate) {
                    <div class="form-group">
                      <label>Nombre de contacto</label>
                      <input type="text" [formControl]="contactName" placeholder="Tu nombre completo" />
                    </div>
                    <div class="form-group">
                      <label>Correo electrónico</label>
                      <input type="email" [formControl]="contactEmail" placeholder="correo@ejemplo.com" />
                    </div>
                    <div class="form-group">
                      <label>Teléfono</label>
                      <input type="tel" [formControl]="contactPhone" placeholder="+52 555 123 4567" />
                    </div>
                  }
                }
              </section>
            }
            @case (1) {
              <section class="wizard-step">
                <h2>Asistencia</h2>
                @if (wizardGuests().length > 0) {
                  <p>Indica quién asiste y quién no:</p>
                  <ul class="attendance-list">
                    @for (g of wizardGuests(); track g.eventGuestId ?? g.displayName) {
                      <li>
                        <span class="attendance-list__name">{{ g.displayName }}</span>
                        <select
                          [value]="attendanceStatus()[g.eventGuestId ?? g.displayName] ?? 'Pending'"
                          (change)="setAttendance(g.eventGuestId ?? g.displayName, eventValue($event))"
                        >
                          <option value="Pending">Pendiente</option>
                          <option value="Attending">Asistirá</option>
                          <option value="NotAttending">No asistirá</option>
                          @if (state()?.settings?.allowTentativeResponse) {
                            <option value="Tentative">Tal vez</option>
                          }
                        </select>
                      </li>
                    }
                  </ul>
                } @else {
                  <p>No hay invitados nombrados en este grupo.</p>
                }
                @if (state()?.settings?.allowGroupDecline) {
                  <button class="btn-text" (click)="declineAll()">Ninguno asistirá</button>
                }
              </section>
            }
            @case (2) {
              <section class="wizard-step">
                <h2>Acompañantes</h2>
                <p>Puedes agregar hasta <strong>{{ availableCompanionSlots() }}</strong> acompañante(s).</p>
                @for (name of companions(); track name; let i = $index) {
                  <div class="form-group companion-row">
                    <label>Acompañante {{ i + 1 }}</label>
                    <div class="companion-row__inputs">
                      <input
                        type="text"
                        [value]="name"
                        (input)="updateCompanionName(i, eventValue($event))"
                        placeholder="Nombre del acompañante"
                      />
                      <button class="btn-icon" type="button" (click)="removeCompanion(i)" aria-label="Quitar acompañante">
                        &times;
                      </button>
                    </div>
                  </div>
                }
                @if (companions().length < availableCompanionSlots()) {
                  <button class="btn-secondary" type="button" (click)="addCompanion()">
                    + Agregar acompañante
                  </button>
                }
              </section>
            }
            @case (3) {
              <section class="wizard-step">
                <h2>Menú</h2>
                @for (menu of parsedMenus(); track menu.id) {
                  <fieldset class="menu-group">
                    <legend>
                      {{ menu.name }}
                      @if (menu.selectionRequired) { <small class="required-mark">*</small> }
                      @if (menu.minimumSelections > 0 || menu.maximumSelections > 1) {
                        <small>(mín. {{ menu.minimumSelections }}, máx. {{ menu.maximumSelections }})</small>
                      }
                    </legend>
                    @if (menu.description) {
                      <p class="menu-description">{{ menu.description }}</p>
                    }
                    @for (wGuest of attendingWizardGuests(); track wGuest.eventGuestId ?? wGuest.displayName) {
                      <div class="guest-menu-row">
                        <span class="guest-menu-row__label">{{ wGuest.displayName }}</span>
                        <div class="menu-options">
                          @for (option of menu.options; track option.id) {
                            <label class="menu-option" [class.menu-option--disabled]="option.selectionCount >= (option.capacity ?? 999999)">
                              <input
                                type="checkbox"
                                [checked]="isMenuOptionSelected(wGuest.eventGuestId ?? wGuest.displayName, menu.id, option.id)"
                                (change)="toggleMenuOption(wGuest.eventGuestId ?? wGuest.displayName, menu.id, option.id)"
                              />
                              <span>{{ option.name }}</span>
                              @if (option.dietaryTags) {
                                <small class="dietary-tag">{{ option.dietaryTags }}</small>
                              }
                            </label>
                          }
                        </div>
                      </div>
                    }
                  </fieldset>
                } @empty {
                  <p>Este evento no tiene menú configurado.</p>
                }
              </section>
            }
            @case (4) {
              <section class="wizard-step">
                <h2>Alergias y necesidades</h2>
                <form [formGroup]="dietaryForm">
                  <div class="form-group">
                    <label>Alergias alimentarias</label>
                    <textarea formControlName="allergies" placeholder="Ej. cacahuates, mariscos, lácteos…"></textarea>
                  </div>
                  <div class="form-group">
                    <label>Restricciones alimentarias</label>
                    <textarea formControlName="dietaryRestrictions" placeholder="Ej. vegetariano, sin gluten, halal…"></textarea>
                  </div>
                  <div class="form-group">
                    <label>Requerimientos de accesibilidad</label>
                    <textarea formControlName="accessibilityRequirements" placeholder="Ej. silla de ruedas, rampa, intérprete…"></textarea>
                  </div>
                  <div class="form-group">
                    <label>Notas adicionales</label>
                    <textarea formControlName="additionalNotes" placeholder="Cualquier otra información relevante…"></textarea>
                  </div>
                  @if (state()?.settings?.privacyNotice; as notice) {
                    <div class="privacy-notice">
                      <p>{{ notice }}</p>
                    </div>
                  }
                  @if (state()?.settings?.sensitiveDataConsentText; as consentText) {
                    <label class="consent-check">
                      <input type="checkbox" formControlName="consentGranted" />
                      <span>{{ consentText }}</span>
                    </label>
                  }
                </form>
              </section>
            }
            @case (5) {
              <section class="wizard-step">
                <h2>Transporte</h2>
                @for (option of parsedTransportOptions(); track option.id) {
                  <label class="transport-option" [class.transport-option--selected]="transportSelection() === option.id">
                    <input
                      type="radio"
                      name="transport"
                      [value]="option.id"
                      [checked]="transportSelection() === option.id"
                      (change)="transportSelection.set(option.id)"
                    />
                    <div>
                      <strong>{{ option.name }}</strong>
                      @if (option.direction) {
                        <small>{{ directionLabel(option.direction) }}</small>
                      }
                      @if (option.pickupPoint) {
                        <small>Punto: {{ option.pickupPoint }}</small>
                      }
                      @if (option.departureAt) {
                        <small>Sale: {{ option.departureAt | date: 'short' }}</small>
                      }
                      @if (option.returnAt) {
                        <small>Regresa: {{ option.returnAt | date: 'short' }}</small>
                      }
                      @if (option.description) {
                        <p class="option-desc">{{ option.description }}</p>
                      }
                      @if (option.capacity) {
                        <small>Capacidad: {{ option.confirmedCount }}/{{ option.capacity }}</small>
                      }
                    </div>
                  </label>
                }
                <label class="transport-option">
                  <input
                    type="radio"
                    name="transport"
                    value=""
                    [checked]="transportSelection() === null"
                    (change)="transportSelection.set(null)"
                  />
                  <strong>No necesito transporte</strong>
                </label>
              </section>
            }
            @case (6) {
              <section class="wizard-step">
                <h2>Hospedaje</h2>
                <form [formGroup]="accommodationForm">
                  <div class="form-group">
                    <label>Estatus de hospedaje</label>
                    <select formControlName="status">
                      <option value="NotNeeded">No necesito hospedaje</option>
                      <option value="Interested">Me interesa</option>
                      <option value="PlanningToBook">Planeo reservar</option>
                      <option value="Booked">Ya reservé</option>
                      <option value="NeedAssistance">Necesito ayuda</option>
                    </select>
                  </div>
                  @if (accommodationForm.controls.status.value === 'Booked') {
                    <div class="form-group">
                      <label>Nombre de la reservación</label>
                      <input type="text" formControlName="reservationName" placeholder="A nombre de…" />
                    </div>
                    <div class="form-group">
                      <label>Referencia de confirmación</label>
                      <input type="text" formControlName="confirmationReference" placeholder="# de confirmación" />
                    </div>
                  }
                  @for (option of parsedAccommodationOptions(); track option.id) {
                    <div class="accommodation-card">
                      <strong>{{ option.name }}</strong>
                      @if (option.description) {
                        <p>{{ option.description }}</p>
                      }
                      @if (option.address) {
                        <small>{{ option.address }}</small>
                      }
                      @if (option.bookingUrl) {
                        <a [href]="option.bookingUrl" target="_blank" rel="noopener noreferrer nofollow">
                          Reservar &nearr;
                        </a>
                      }
                      @if (option.bookingCode) {
                        <small>Código: {{ option.bookingCode }}</small>
                      }
                      @if (option.bookingDeadline) {
                        <small>Reservar antes de: {{ option.bookingDeadline | date: 'mediumDate' }}</small>
                      }
                      @if (option.contactInformation) {
                        <small>{{ option.contactInformation }}</small>
                      }
                    </div>
                  }
                </form>
              </section>
            }
            @case (7) {
              <section class="wizard-step">
                <h2>Preguntas adicionales</h2>
                @for (question of parsedQuestions(); track question.id) {
                  <div class="form-group">
                    <label>
                      {{ question.label }}
                      @if (question.isRequired) { <small class="required-mark">*</small> }
                    </label>
                    @if (question.helpText) {
                      <small class="help-text">{{ question.helpText }}</small>
                    }
                    @switch (question.questionType) {
                      @case ('ShortText') {
                        <input
                          type="text"
                          [value]="questionAnswers()[question.id] ?? ''"
                          (input)="setAnswer(question.id, eventValue($event))"
                          [placeholder]="'Tu respuesta'"
                        />
                      }
                      @case ('LongText') {
                        <textarea
                          [value]="questionAnswers()[question.id] ?? ''"
                          (input)="setAnswer(question.id, eventValue($event))"
                          [placeholder]="'Tu respuesta'"
                        ></textarea>
                      }
                      @case ('YesNo') {
                        <select
                          [value]="questionAnswers()[question.id] ?? ''"
                          (change)="setAnswer(question.id, eventValue($event))"
                        >
                          <option value="">Selecciona…</option>
                          <option value="true">Sí</option>
                          <option value="false">No</option>
                        </select>
                      }
                      @case ('SingleChoice') {
                        <select
                          [value]="questionAnswers()[question.id] ?? ''"
                          (change)="setAnswer(question.id, eventValue($event))"
                        >
                          <option value="">Selecciona…</option>
                          @for (opt of question.options; track opt) {
                            <option [value]="opt">{{ opt }}</option>
                          }
                        </select>
                      }
                      @case ('MultipleChoice') {
                        <div class="checkbox-group">
                          @for (opt of question.options; track opt) {
                            <label>
                              <input
                                type="checkbox"
                                [value]="opt"
                                [checked]="isAnswerOptionSelected(question.id, opt)"
                                (change)="toggleAnswerOption(question.id, opt)"
                              />
                              {{ opt }}
                            </label>
                          }
                        </div>
                      }
                      @case ('Number') {
                        <input
                          type="number"
                          [value]="questionAnswers()[question.id] ?? ''"
                          (input)="setAnswer(question.id, eventValue($event))"
                          [min]="question.validationRules?.minimum"
                          [max]="question.validationRules?.maximum"
                        />
                      }
                      @case ('Date') {
                        <input
                          type="date"
                          [value]="questionAnswers()[question.id] ?? ''"
                          (input)="setAnswer(question.id, eventValue($event))"
                        />
                      }
                      @case ('InformationalConsent') {
                        <label class="consent-check">
                          <input
                            type="checkbox"
                            [checked]="questionAnswers()[question.id] === 'true'"
                            (change)="setAnswer(question.id, eventChecked($event) ? 'true' : 'false')"
                          />
                          <span>{{ question.label }}</span>
                        </label>
                      }
                    }
                  </div>
                } @empty {
                  <p>No hay preguntas adicionales configuradas.</p>
                }
              </section>
            }
            @case (8) {
              <section class="wizard-step">
                <h2>Revisión</h2>
                <div class="summary-section">
                  <h3>Contacto</h3>
                  <p>{{ contactName.value || state()?.groupName || '—' }}</p>
                  @if (contactEmail.value) { <p>{{ contactEmail.value }}</p> }
                  @if (contactPhone.value) { <p>{{ contactPhone.value }}</p> }
                </div>
                <div class="summary-section">
                  <h3>Asistencia</h3>
                  @for (g of wizardGuests(); track g.eventGuestId ?? g.displayName) {
                    <p>{{ g.displayName }}: <strong>{{ attendanceLabel(attendanceStatus()[g.eventGuestId ?? g.displayName] ?? 'Pending') }}</strong></p>
                  } @empty {
                    <p>Sin invitados nombrados.</p>
                  }
                  @for (name of companions(); track name) {
                    <p>{{ name || '(sin nombre)' }}: <strong>Acompañante</strong></p>
                  }
                </div>
                @if (parsedMenus().length > 0) {
                  <div class="summary-section">
                    <h3>Menú</h3>
                    @for (wGuest of attendingWizardGuests(); track wGuest.eventGuestId ?? wGuest.displayName) {
                      <p>{{ wGuest.displayName }}:</p>
                      <ul>
                        @for (menu of parsedMenus(); track menu.id) {
                          @for (optId of guestMenuSelections()[wGuest.eventGuestId ?? wGuest.displayName]?.[menu.id] ?? []; track optId) {
                            <li>{{ menuNameAndOption(menu, optId) }}</li>
                          }
                        }
                      </ul>
                    }
                  </div>
                }
                <div class="summary-section">
                  <h3>Alergias y necesidades</h3>
                  @if (dietaryForm.value.allergies) { <p>Alergias: {{ dietaryForm.value.allergies }}</p> }
                  @if (dietaryForm.value.dietaryRestrictions) { <p>Restricciones: {{ dietaryForm.value.dietaryRestrictions }}</p> }
                  @if (dietaryForm.value.accessibilityRequirements) { <p>Accesibilidad: {{ dietaryForm.value.accessibilityRequirements }}</p> }
                  @if (dietaryForm.value.additionalNotes) { <p>Notas: {{ dietaryForm.value.additionalNotes }}</p> }
                  <p>Consentimiento: {{ dietaryForm.value.consentGranted ? 'Otorgado' : 'No otorgado' }}</p>
                </div>
                @if (parsedTransportOptions().length > 0) {
                  <div class="summary-section">
                    <h3>Transporte</h3>
                    <p>{{ transportSelection() ? transportName(transportSelection()!) : 'No necesita transporte' }}</p>
                  </div>
                }
                @if (parsedAccommodationOptions().length > 0) {
                  <div class="summary-section">
                    <h3>Hospedaje</h3>
                    <p>Estatus: {{ accommodationStatusLabel(accommodationForm.controls.status.value) }}</p>
                    @if (accommodationForm.value.reservationName) { <p>Reservación: {{ accommodationForm.value.reservationName }}</p> }
                    @if (accommodationForm.value.confirmationReference) { <p>Referencia: {{ accommodationForm.value.confirmationReference }}</p> }
                  </div>
                }
                @if (parsedQuestions().length > 0) {
                  <div class="summary-section">
                    <h3>Preguntas</h3>
                    @for (question of parsedQuestions(); track question.id) {
                      <p>{{ question.label }}: <strong>{{ answerDisplayValue(question) }}</strong></p>
                    }
                  </div>
                }
              </section>
            }
          }
        }
      </main>

      <footer class="rsvp-wizard__footer">
        @if (!loading() && !error() && !confirmation()) {
          @if (activeStep() > 0) {
            <button class="btn-secondary" (click)="prevStep()">Atrás</button>
          } @else {
            <span></span>
          }
          @if (!isLastStep()) {
            <button class="btn-primary" (click)="nextStep()" [disabled]="!canProceed()">Siguiente</button>
          } @else {
            <button class="btn-primary" (click)="submit()" [disabled]="submitting() || !canProceed()">
              @if (submitting()) {
                Enviando…
              } @else {
                Enviar respuesta
              }
            </button>
          }
        }
      </footer>
    </div>
  `,
  styles: [`
    :host {
      display: block;
      max-width: 480px;
      margin: 0 auto;
      padding: 16px;
      font-family: system-ui, -apple-system, sans-serif;
    }

    .rsvp-wizard__header {
      text-align: center;
      margin-bottom: 24px;
    }

    .rsvp-wizard__header h1 {
      margin: 0;
      font-size: 22px;
    }

    .rsvp-wizard__header p {
      margin: 4px 0 0;
      color: #666;
      font-size: 14px;
    }

    .rsvp-wizard__steps {
      display: flex;
      gap: 8px;
      justify-content: center;
      margin-top: 16px;
    }

    .step-dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: #ddd;
      transition: background 0.2s, transform 0.2s;
    }

    .step-dot.active {
      background: #1a73e8;
      width: 12px;
      height: 12px;
    }

    .step-dot.done {
      background: #34a853;
    }

    .rsvp-wizard__body {
      min-height: 320px;
    }

    .rsvp-wizard__footer {
      display: flex;
      gap: 12px;
      justify-content: space-between;
      margin-top: 24px;
      padding-top: 16px;
      border-top: 1px solid #eee;
    }

    .btn-primary {
      background: #1a73e8;
      color: white;
      border: none;
      padding: 12px 24px;
      border-radius: 8px;
      font-size: 16px;
      cursor: pointer;
      min-width: 120px;
      text-align: center;
    }

    .btn-primary:disabled {
      background: #a0c4f1;
      cursor: not-allowed;
    }

    .btn-secondary {
      background: transparent;
      color: #1a73e8;
      border: 1px solid #1a73e8;
      padding: 12px 24px;
      border-radius: 8px;
      font-size: 16px;
      cursor: pointer;
    }

    .btn-text {
      background: none;
      border: none;
      color: #1a73e8;
      cursor: pointer;
      font-size: 14px;
      padding: 8px 0;
      text-decoration: underline;
    }

    .btn-icon {
      background: none;
      border: 1px solid #ddd;
      border-radius: 4px;
      cursor: pointer;
      font-size: 18px;
      padding: 4px 10px;
      color: #d93025;
    }

    .wizard-step h2 {
      margin: 0 0 16px;
      font-size: 20px;
    }

    .loading {
      text-align: center;
      color: #666;
      padding: 48px 0;
    }

    .error-state {
      text-align: center;
      padding: 32px 16px;
      color: #d93025;
    }

    .error-state h2 {
      color: #d93025;
      font-size: 18px;
    }

    .closed-msg {
      color: #666;
      font-size: 14px;
      margin-top: 8px;
    }

    .confirmation {
      text-align: center;
      padding: 32px 16px;
    }

    .confirmation__icon {
      font-size: 48px;
      color: #34a853;
    }

    .confirmation h2 {
      color: #34a853;
    }

    .confirmation__code {
      font-family: monospace;
      background: #f1f3f4;
      padding: 8px 16px;
      border-radius: 6px;
      display: inline-block;
    }

    .confirmation button {
      margin-top: 16px;
    }

    .guest-list {
      list-style: none;
      padding: 0;
      margin: 8px 0;
    }

    .guest-list li {
      padding: 8px 0;
      border-bottom: 1px solid #f0f0f0;
    }

    .companion-note {
      background: #e8f0fe;
      padding: 10px 14px;
      border-radius: 8px;
      font-size: 14px;
      color: #1a73e8;
    }

    .form-group {
      margin-bottom: 16px;
    }

    .form-group label {
      display: block;
      margin-bottom: 4px;
      font-weight: 600;
      font-size: 14px;
    }

    .form-group input,
    .form-group select,
    .form-group textarea {
      width: 100%;
      padding: 10px;
      border: 1px solid #ddd;
      border-radius: 6px;
      font-size: 16px;
      box-sizing: border-box;
    }

    .form-group textarea {
      min-height: 80px;
      resize: vertical;
    }

    .attendance-list {
      list-style: none;
      padding: 0;
    }

    .attendance-list li {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;
      padding: 12px;
      border-bottom: 1px solid #eee;
    }

    .attendance-list__name {
      flex: 1;
      font-weight: 500;
    }

    .attendance-list select {
      padding: 6px 10px;
      border: 1px solid #ddd;
      border-radius: 4px;
      font-size: 14px;
    }

    .companion-row__inputs {
      display: flex;
      gap: 8px;
      align-items: center;
    }

    .companion-row__inputs input {
      flex: 1;
    }

    .menu-group {
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 14px;
      margin-bottom: 16px;
    }

    .menu-group legend {
      font-weight: 600;
      padding: 0 6px;
    }

    .menu-description {
      color: #666;
      font-size: 14px;
      margin: 4px 0 12px;
    }

    .required-mark {
      color: #d93025;
    }

    .guest-menu-row {
      margin-bottom: 12px;
      padding-bottom: 12px;
      border-bottom: 1px solid #f0f0f0;
    }

    .guest-menu-row__label {
      font-weight: 500;
      display: block;
      margin-bottom: 6px;
      font-size: 14px;
    }

    .menu-options {
      display: flex;
      flex-direction: column;
      gap: 6px;
    }

    .menu-option {
      display: flex;
      align-items: flex-start;
      gap: 8px;
      cursor: pointer;
      font-size: 14px;
      padding: 4px 0;
    }

    .menu-option--disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .dietary-tag {
      background: #f1f3f4;
      padding: 1px 6px;
      border-radius: 4px;
      font-size: 11px;
      color: #666;
      margin-left: 6px;
    }

    .privacy-notice {
      background: #fef7e0;
      border-radius: 8px;
      padding: 12px 14px;
      font-size: 13px;
      color: #5f4b00;
      margin-bottom: 16px;
    }

    .consent-check {
      display: flex;
      align-items: flex-start;
      gap: 10px;
      cursor: pointer;
      font-size: 14px;
      padding: 10px 0;
    }

    .consent-check input {
      margin-top: 3px;
    }

    .transport-option {
      display: flex;
      gap: 12px;
      padding: 14px;
      border: 1px solid #ddd;
      border-radius: 8px;
      margin-bottom: 10px;
      cursor: pointer;
      transition: border-color 0.15s;
      align-items: flex-start;
    }

    .transport-option input {
      margin-top: 4px;
    }

    .transport-option--selected {
      border-color: #1a73e8;
      background: #e8f0fe;
    }

    .transport-option small {
      display: block;
      color: #666;
      font-size: 13px;
      margin-top: 2px;
    }

    .option-desc {
      margin: 4px 0 0;
      font-size: 14px;
      color: #333;
    }

    .accommodation-card {
      border: 1px solid #ddd;
      border-radius: 8px;
      padding: 14px;
      margin-bottom: 10px;
    }

    .accommodation-card p {
      margin: 4px 0;
      font-size: 14px;
    }

    .accommodation-card small {
      display: block;
      color: #666;
      font-size: 13px;
      margin-top: 4px;
    }

    .accommodation-card a {
      display: inline-block;
      margin-top: 8px;
      color: #1a73e8;
      text-decoration: none;
      font-size: 14px;
    }

    .checkbox-group {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .checkbox-group label {
      display: flex;
      align-items: center;
      gap: 8px;
      font-weight: 400;
      cursor: pointer;
    }

    .help-text {
      display: block;
      color: #666;
      font-size: 13px;
      margin-bottom: 4px;
    }

    .summary-section {
      margin-bottom: 16px;
      padding: 12px 14px;
      background: #f8f9fa;
      border-radius: 8px;
    }

    .summary-section h3 {
      margin: 0 0 8px;
      font-size: 13px;
      color: #666;
      text-transform: uppercase;
      letter-spacing: .5px;
    }

    .summary-section p {
      margin: 4px 0;
      font-size: 14px;
    }

    .summary-section ul {
      margin: 4px 0 0 20px;
      padding: 0;
      font-size: 14px;
    }

    .error {
      color: #d93025;
      font-size: 13px;
      margin-top: 4px;
    }
  `],
})
export class PublicRsvpPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);

  protected readonly state = signal<GuestRsvpStateResponse | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly closedMessage = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly confirmation = signal<RsvpSubmissionResponse | null>(null);
  protected readonly activeStep = signal(0);
  private readonly idempotencyAttempt = new IdempotencyAttempt();

  protected readonly stepLabels = signal<string[]>([]);

  protected readonly attendanceStatus = signal<Record<string, GuestAttendanceStatus>>({});
  protected readonly companions = signal<string[]>([]);
  protected readonly guestMenuSelections = signal<Record<string, Record<string, string[]>>>({});

  protected readonly transportSelection = signal<string | null>(null);

  protected readonly dietaryForm = new FormGroup({
    allergies: new FormControl(''),
    dietaryRestrictions: new FormControl(''),
    accessibilityRequirements: new FormControl(''),
    additionalNotes: new FormControl(''),
    consentGranted: new FormControl(false, { nonNullable: true }),
  });

  protected readonly accommodationForm = new FormGroup({
    status: new FormControl<AccommodationSelectionStatus>('NotNeeded', { nonNullable: true }),
    reservationName: new FormControl(''),
    confirmationReference: new FormControl(''),
  });

  protected readonly contactName = new FormControl('');
  protected readonly contactEmail = new FormControl('');
  protected readonly contactPhone = new FormControl('');

  protected readonly questionAnswers = signal<Record<string, string>>({});

  protected readonly parsedMenus = signal<EventMenuResponse[]>([]);
  protected readonly parsedTransportOptions = signal<EventTransportOptionResponse[]>([]);
  protected readonly parsedAccommodationOptions = signal<EventAccommodationOptionResponse[]>([]);
  protected readonly parsedQuestions = signal<RsvpQuestion[]>([]);

  protected readonly wizardGuests = computed<WizardGuest[]>(() => {
    const s = this.state();
    if (!s) return [];
    if (s.currentResponse) {
      return s.currentResponse.guests
        .filter((g) => !g.isUnnamedCompanion && g.eventGuestId)
        .map((g) => ({
          eventGuestId: g.eventGuestId!,
          displayName: g.displayName,
          ageCategory: g.ageCategory,
          isNamed: true,
        }));
    }
    return s.guests.map((guest) => ({
      eventGuestId: guest.eventGuestId,
      displayName: guest.displayName,
      ageCategory: guest.ageCategory,
      isNamed: true,
    }));
  });

  protected readonly attendingWizardGuests = computed<WizardGuest[]>(() => {
    return this.wizardGuests().filter(
      (g) => (this.attendanceStatus()[g.eventGuestId ?? g.displayName] ?? 'Pending') === 'Attending',
    );
  });

  protected readonly availableCompanionSlots = computed<number>(() => {
    const s = this.state();
    if (!s || !s.allowUnnamedCompanions) return 0;
    const namedCount = this.wizardGuests().length;
    const attendingCount = this.attendingWizardGuests().length;
    const slots = Math.min(s.maxUnnamedCompanions, s.allowedGuestCount - attendingCount);
    return Math.max(0, slots - this.companions().length);
  });

  constructor() {
    this.configurePrivacyMetadata();
    const token = this.route.snapshot.paramMap.get('token') ?? '';
    if (!token) {
      this.error.set('Enlace inválido.');
      this.loading.set(false);
      return;
    }
    this.init(token);
  }

  private init(token: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.api
      .getGuestRsvpState(token)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (s) => {
          this.state.set(s);
          this.closedMessage.set(s.closedMessage);
          if (!s.canRespond && !s.currentResponse) {
            this.error.set('El RSVP no está disponible en este momento.');
          }
          this.parseSnapshots(s);
          this.buildStepList();
          this.loading.set(false);
          if (s.currentResponse) {
            this.loadExistingResponse(s.currentResponse);
          }
        },
        error: (err) => {
          this.error.set(getApiErrorMessage(err));
          this.loading.set(false);
        },
      });
  }

  private parseSnapshots(s: GuestRsvpStateResponse): void {
    if (s.activeForm) {
      this.parsedMenus.set(safeJsonParse<EventMenuResponse[]>(s.activeForm.menuSnapshot, []));
      this.parsedTransportOptions.set(
        safeJsonParse<EventTransportOptionResponse[]>(s.activeForm.transportSnapshot, []),
      );
      this.parsedAccommodationOptions.set(
        safeJsonParse<EventAccommodationOptionResponse[]>(
          s.activeForm.accommodationSnapshot,
          [],
        ),
      );
      this.parsedQuestions.set(
        safeJsonParse<RsvpQuestion[]>(s.activeForm.questionsSnapshot, []),
      );
    }
  }

  private buildStepList(): void {
    this.stepLabels.set([
      'Bienvenida',
      'Asistencia',
      'Acompañantes',
      'Menú',
      'Necesidades',
      'Transporte',
      'Hospedaje',
      'Preguntas',
      'Revisión',
    ]);
  }

  private loadExistingResponse(r: RsvpSubmissionResponse): void {
    for (const g of r.guests) {
      if (!g.isUnnamedCompanion && g.eventGuestId) {
        this.attendanceStatus.update((map) => ({
          ...map,
          [g.eventGuestId!]: g.attendanceStatus,
        }));
      }
    }

    const unnamed = r.guests.filter((g) => g.isUnnamedCompanion);
    if (unnamed.length > 0) {
      this.companions.set(unnamed.map((g) => g.displayName));
    }

    const menuMap: Record<string, Record<string, string[]>> = {};
    for (const g of r.guests) {
      const key = g.eventGuestId ?? g.displayName;
      if (g.menuSelectionsJson && g.menuSelectionsJson !== '{}') {
        try {
          menuMap[key] = JSON.parse(g.menuSelectionsJson);
        } catch {
          menuMap[key] = {};
        }
      }
    }
    this.guestMenuSelections.set(menuMap);

    const guest = r.guests[0];
    if (guest) {
      if (guest.transportSelectionJson && guest.transportSelectionJson !== '{}') {
        try {
          const transportData = JSON.parse(guest.transportSelectionJson);
          if (transportData.transportOptionId) {
            this.transportSelection.set(transportData.transportOptionId);
          }
        } catch {
          // ignore parse errors
        }
      }

      if (guest.accommodationSelectionJson && guest.accommodationSelectionJson !== '{}') {
        try {
          const accData = JSON.parse(guest.accommodationSelectionJson);
          if (accData.status) {
            this.accommodationForm.patchValue({
              status: accData.status,
              reservationName: accData.reservationName ?? '',
              confirmationReference: accData.confirmationReference ?? '',
            });
          }
        } catch {
          // ignore parse errors
        }
      }

      if (guest.dietaryJson && guest.dietaryJson !== '{}') {
        try {
          const dietaryData = JSON.parse(guest.dietaryJson);
          this.dietaryForm.patchValue({
            allergies: dietaryData.allergies ?? '',
            dietaryRestrictions: dietaryData.dietaryRestrictions ?? '',
            accessibilityRequirements: dietaryData.accessibilityRequirements ?? '',
            additionalNotes: dietaryData.additionalNotes ?? '',
            consentGranted: dietaryData.consentGranted ?? false,
          });
        } catch {
          // ignore parse errors
        }
      }
    }

    this.contactName.setValue(r.contactNameSnapshot ?? '');
    this.contactEmail.setValue(r.contactEmailSnapshot ?? '');
    this.contactPhone.setValue(r.contactPhoneSnapshot ?? '');

    for (const a of r.answers) {
      this.questionAnswers.update((map) => ({ ...map, [a.questionId]: a.answerValue }));
    }
  }

  protected isLastStep(): boolean {
    return this.activeStep() >= this.stepLabels().length - 1;
  }

  protected prevStep(): void {
    if (this.activeStep() > 0) {
      this.activeStep.update((s) => s - 1);
    }
  }

  protected nextStep(): void {
    if (this.activeStep() < this.stepLabels().length - 1) {
      this.activeStep.update((s) => s + 1);
    }
  }

  protected setAttendance(guestKey: string, status: string): void {
    this.attendanceStatus.update((map) => ({
      ...map,
      [guestKey]: status as GuestAttendanceStatus,
    }));
  }

  protected eventValue(event: Event): string {
    const target = event.target;
    return target instanceof HTMLInputElement
           || target instanceof HTMLSelectElement
           || target instanceof HTMLTextAreaElement
      ? target.value
      : '';
  }

  protected eventChecked(event: Event): boolean {
    return event.target instanceof HTMLInputElement
      ? event.target.checked
      : false;
  }

  protected declineAll(): void {
    const map: Record<string, GuestAttendanceStatus> = {};
    for (const g of this.wizardGuests()) {
      map[g.eventGuestId ?? g.displayName] = 'NotAttending';
    }
    this.attendanceStatus.set(map);
  }

  protected addCompanion(): void {
    if (this.availableCompanionSlots() <= 0) return;
    this.companions.update((list) => [...list, '']);
  }

  protected removeCompanion(index: number): void {
    this.companions.update((list) => list.filter((_, i) => i !== index));
  }

  protected updateCompanionName(index: number, name: string): void {
    this.companions.update((list) => list.map((n, i) => (i === index ? name : n)));
  }

  protected isMenuOptionSelected(guestKey: string, menuId: string, optionId: string): boolean {
    return !!this.guestMenuSelections()[guestKey]?.[menuId]?.includes(optionId);
  }

  protected toggleMenuOption(guestKey: string, menuId: string, optionId: string): void {
    this.guestMenuSelections.update((map) => {
      const guestMenus = { ...(map[guestKey] ?? {}) };
      const currentOptions = [...(guestMenus[menuId] ?? [])];
      const idx = currentOptions.indexOf(optionId);
      if (idx >= 0) {
        currentOptions.splice(idx, 1);
      } else {
        currentOptions.push(optionId);
      }
      guestMenus[menuId] = currentOptions;
      return { ...map, [guestKey]: guestMenus };
    });
  }

  protected setAnswer(questionId: string, value: string): void {
    this.questionAnswers.update((map) => ({ ...map, [questionId]: value }));
  }

  protected isAnswerOptionSelected(questionId: string, option: string): boolean {
    const raw = this.questionAnswers()[questionId] ?? '';
    return raw.split(',').includes(option);
  }

  protected toggleAnswerOption(questionId: string, option: string): void {
    const raw = this.questionAnswers()[questionId] ?? '';
    const selected = raw ? raw.split(',') : [];
    const idx = selected.indexOf(option);
    if (idx >= 0) {
      selected.splice(idx, 1);
    } else {
      selected.push(option);
    }
    this.questionAnswers.update((map) => ({ ...map, [questionId]: selected.join(',') }));
  }

  protected canProceed(): boolean {
    const step = this.activeStep();
    const s = this.state();

    if (step === 0) return true;

    if (step === 1) {
      const guests = this.wizardGuests();
      if (guests.length === 0) return true;
      const requireAll = s?.settings?.requireResponseForEveryNamedGuest ?? false;
      if (requireAll) {
        return guests.every(
          (g) => !!this.attendanceStatus()[g.eventGuestId ?? g.displayName],
        );
      }
      return guests.some((g) => {
        const status = this.attendanceStatus()[g.eventGuestId ?? g.displayName];
        return status === 'Attending' || status === 'NotAttending' || status === 'Tentative';
      });
    }

    if (step === 2) {
      if (!s?.allowUnnamedCompanions) return true;
      if (s.settings?.requireCompanionNames) {
        return this.companions().every((name) => name.trim().length > 0);
      }
      return true;
    }

    if (step === 3) {
      const menus = this.parsedMenus();
      if (menus.length === 0) return true;
      for (const menu of menus) {
        if (!menu.selectionRequired) continue;
        for (const wGuest of this.attendingWizardGuests()) {
          const key = wGuest.eventGuestId ?? wGuest.displayName;
          const selected = this.guestMenuSelections()[key]?.[menu.id] ?? [];
          if (selected.length < menu.minimumSelections) return false;
        }
      }
      return true;
    }

    if (step === 4) {
      if (s?.settings?.sensitiveDataConsentText && !this.dietaryForm.controls.consentGranted.value) {
        return false;
      }
      return true;
    }

    if (step === 5 || step === 6) return true;

    if (step === 7) {
      for (const q of this.parsedQuestions()) {
        if (q.isRequired) {
          const answer = this.questionAnswers()[q.id];
          if (!answer || answer.trim() === '') return false;
        }
      }
      return true;
    }

    return true;
  }

  protected attendingCount(conf: RsvpSubmissionResponse): number {
    return conf.guests.filter((g) => g.attendanceStatus === 'Attending').length;
  }

  protected ageLabel(age: string): string {
    const labels: Record<string, string> = {
      Adult: 'Adulto',
      Teen: 'Adolescente',
      Child: 'Niño/a',
      Infant: 'Bebé',
      Unknown: 'Sin especificar',
    };
    return labels[age] ?? age;
  }

  protected attendanceLabel(status: GuestAttendanceStatus): string {
    const labels: Record<GuestAttendanceStatus, string> = {
      Pending: 'Pendiente',
      Attending: 'Asistirá',
      NotAttending: 'No asistirá',
      Tentative: 'Tal vez',
      CancelledAfterConfirmation: 'Cancelado',
    };
    return labels[status] ?? status;
  }

  protected directionLabel(direction: string): string {
    const labels: Record<string, string> = {
      ToCeremony: 'A la ceremonia',
      ToReception: 'A la recepción',
      Return: 'De regreso',
      RoundTrip: 'Ida y vuelta',
      Other: 'Otro',
    };
    return labels[direction] ?? direction;
  }

  protected accommodationStatusLabel(status: string): string {
    const labels: Record<string, string> = {
      NotNeeded: 'No necesita',
      Interested: 'Interesado/a',
      PlanningToBook: 'Planea reservar',
      Booked: 'Reservado',
      NeedAssistance: 'Necesita ayuda',
    };
    return labels[status] ?? status;
  }

  protected transportName(optionId: string): string {
    return this.parsedTransportOptions().find((o) => o.id === optionId)?.name ?? optionId;
  }

  protected menuNameAndOption(menu: EventMenuResponse, optionId: string): string {
    const opt = menu.options.find((o) => o.id === optionId);
    return opt ? `${menu.name}: ${opt.name}` : `${menu.name}: ${optionId}`;
  }

  protected answerDisplayValue(question: RsvpQuestion): string {
    const answer = this.questionAnswers()[question.id] ?? '';
    if (!answer) return '—';
    if (question.questionType === 'YesNo') {
      return answer === 'true' ? 'Sí' : 'No';
    }
    if (question.questionType === 'InformationalConsent') {
      return answer === 'true' ? 'Aceptado' : 'No aceptado';
    }
    return answer;
  }

  protected submit(): void {
    const s = this.state();
    if (!s || this.submitting()) return;
    const token = this.route.snapshot.paramMap.get('token') ?? '';
    this.submitting.set(true);

    const overallStatus = this.computeOverallStatus();
    const guests: RsvpSubmissionGuestRequest[] = [];

    for (const wGuest of this.wizardGuests()) {
      const key = wGuest.eventGuestId ?? wGuest.displayName;
      const status = this.attendanceStatus()[key] ?? 'Pending';
      const menuJson = JSON.stringify(this.guestMenuSelections()[key] ?? {});
      guests.push({
        eventGuestId: wGuest.eventGuestId,
        displayName: wGuest.displayName,
        ageCategory: wGuest.ageCategory,
        attendanceStatus: status,
        menuSelectionsJson: menuJson,
        transportSelectionJson: this.buildTransportJson(),
        accommodationSelectionJson: this.buildAccommodationJson(),
        dietaryJson: JSON.stringify(this.dietaryForm.value),
        isUnnamedCompanion: false,
      });
    }

    for (const name of this.companions()) {
      guests.push({
        eventGuestId: null,
        displayName: name.trim() || 'Acompañante',
        ageCategory: 'Adult',
        attendanceStatus: 'Attending',
        menuSelectionsJson: JSON.stringify({}),
        transportSelectionJson: this.buildTransportJson(),
        accommodationSelectionJson: this.buildAccommodationJson(),
        dietaryJson: JSON.stringify(this.dietaryForm.value),
        isUnnamedCompanion: true,
      });
    }

    const answers: RsvpSubmissionAnswerRequest[] = [];
    for (const q of this.parsedQuestions()) {
      const value = this.questionAnswers()[q.id] ?? '';
      answers.push({
        questionId: q.id,
        guestId: null,
        answerValue: value,
        displayValue: value,
      });
    }

    const request: RsvpSubmissionRequest = {
      expectedRevision: s.revisionVersion,
      overallStatus,
      contactName: this.contactName.value || s.groupName,
      contactEmail: this.contactEmail.value || null,
      contactPhone: this.contactPhone.value || null,
      guests,
      answers,
      consentSnapshot: this.dietaryForm.controls.consentGranted.value
        ? JSON.stringify(this.dietaryForm.value)
        : null,
    };
    const payload = JSON.stringify(request);
    const idempotencyKey = this.idempotencyAttempt.keyFor(payload);

    this.api
      .submitGuestRsvp(token, request, idempotencyKey)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (resp) => {
          this.idempotencyAttempt.complete();
          this.confirmation.set(resp);
          this.submitting.set(false);
          this.activeStep.set(this.stepLabels().length - 1);
          this.toast.success('¡Respuesta enviada con éxito!');
        },
        error: (err) => {
          if (requiresReload(err)) {
            this.toast.error(
              'La respuesta cambió o la disponibilidad se actualizó. Recargamos los datos más recientes.',
            );
            this.init(token);
          } else {
            this.toast.error(getApiErrorMessage(err));
          }
          this.submitting.set(false);
        },
      });
  }

  private computeOverallStatus(): RsvpOverallStatus {
    const guests = this.wizardGuests();
    const companionsCount = this.companions().length;
    if (guests.length === 0 && companionsCount === 0) return 'Incomplete';

    const statuses: GuestAttendanceStatus[] = guests.map(
      (g) => this.attendanceStatus()[g.eventGuestId ?? g.displayName] ?? 'Pending',
    );

    const allAttending = statuses.every((s) => s === 'Attending') && companionsCount > 0
      ? true
      : statuses.length === 0 && companionsCount > 0
        ? true
        : statuses.length > 0 && statuses.every((s) => s === 'Attending');

    const allNotAttending =
      statuses.length > 0 &&
      statuses.every((s) => s === 'NotAttending') &&
      companionsCount === 0;

    const hasTentative = statuses.some((s) => s === 'Tentative');
    const hasPending = statuses.some((s) => s === 'Pending');

    if (allAttending) return 'Confirmed';
    if (allNotAttending) return 'Declined';
    if (hasTentative) return 'Tentative';
    if (hasPending) return 'Incomplete';
    return 'Mixed';
  }

  private buildTransportJson(): string {
    const selection = this.transportSelection();
    if (!selection) return '{}';
    return JSON.stringify({ transportOptionId: selection });
  }

  private buildAccommodationJson(): string {
    return JSON.stringify(this.accommodationForm.value);
  }

  protected resetWizard(): void {
    this.confirmation.set(null);
    this.activeStep.set(0);
  }

  private configurePrivacyMetadata(): void {
    const values: Array<[string, string, string]> = [
      ['name', 'robots', 'noindex, nofollow, noarchive'],
      ['name', 'referrer', 'no-referrer'],
    ];
    for (const [attribute, key, content] of values) {
      let element = this.document.head.querySelector<HTMLMetaElement>(
        `meta[${attribute}="${key}"]`,
      );
      if (!element) {
        element = this.document.createElement('meta');
        element.setAttribute(attribute, key);
        this.document.head.appendChild(element);
      }
      element.content = content;
    }
  }
}
