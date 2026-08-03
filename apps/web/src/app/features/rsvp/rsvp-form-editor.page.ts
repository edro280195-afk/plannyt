import { HttpErrorResponse } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { ApiService } from '../../core/api/api.service';
import { getApiErrorMessage } from '../../core/errors/api-error';
import type {
  EventAccommodationOptionResponse,
  EventMenuResponse,
  EventTransportOptionResponse,
  RsvpFormResponse,
  RsvpFormVersionResponse,
  GuestAttendanceStatus,
  RsvpQuestion,
  RsvpQuestionCatalog,
  RsvpQuestionOption,
  RsvpQuestionType,
  RsvpVisibilityConditionType,
  RsvpVisibilityRule,
} from '../../core/models/api.models';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import { ToastService } from '../../core/ui/toast.service';
import {
  type RsvpDraftAnswer,
  type RsvpEditorIssue,
  compatibleReferenceQuestions,
  rsvpAnswerKey,
  validateRsvpQuestions,
  visibleQuestionInstances,
} from './rsvp-question-engine';

@Component({
  selector: 'app-rsvp-form-editor-page',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="editor-page">
      <header class="page-header">
        <div>
          <a
            class="back-link"
            [routerLink]="['/app/events', eventId(), 'rsvp']"
          >← Volver a RSVP</a>
          <p class="eyebrow">Formulario versionado</p>
          <h1>Editor de preguntas RSVP</h1>
          <p>
            El editor anticipa errores; la API vuelve a validar toda la
            definición al guardar y publicar.
          </p>
        </div>
        @if (form(); as currentForm) {
          <span class="status">
            {{ statusLabel(currentForm.status) }}
            · borrador {{ currentForm.currentDraftVersion }}
          </span>
        }
      </header>

      @if (loading()) {
        <section class="panel">Cargando catálogo controlado…</section>
      } @else if (!form()) {
        <section class="empty-state">
          <h2>Este evento aún no tiene formulario RSVP</h2>
          <p>Créalo para comenzar con una definición controlada.</p>
          <button
            class="btn-primary"
            type="button"
            [disabled]="busy()"
            (click)="createForm()"
          >Crear formulario</button>
        </section>
      } @else {
        <section class="workflow panel">
          <div>
            <strong>Flujo de publicación</strong>
            <p>
              Guarda una versión, envíala a revisión, apruébala y publícala.
              La versión anterior permanece activa mientras editas.
            </p>
          </div>
          <div class="workflow__actions">
            @if (form()?.status === 'Published') {
              <button
                type="button"
                class="btn-secondary"
                [disabled]="busy()"
                (click)="createNewDraft()"
              >Crear nueva versión</button>
            }
            @if (form()?.status === 'Draft'
              || form()?.status === 'ChangesRequested') {
              <button
                type="button"
                class="btn-primary"
                [disabled]="busy() || issues().length > 0 || !!versionId()"
                (click)="saveVersion()"
              >Guardar versión</button>
              <button
                type="button"
                class="btn-secondary"
                [disabled]="busy() || !versionId()"
                (click)="submitForReview()"
              >Enviar a revisión</button>
            }
            @if (form()?.status === 'InReview') {
              <button
                type="button"
                class="btn-primary"
                [disabled]="busy() || !versionId()"
                (click)="approve()"
              >Aprobar</button>
            }
            @if (form()?.status === 'Approved') {
              <button
                type="button"
                class="btn-primary"
                [disabled]="busy() || !versionId()"
                (click)="publish()"
              >Publicar</button>
            }
          </div>
        </section>

        @if (issues().length > 0) {
          <section class="validation-summary" aria-live="polite">
            <strong>
              Corrige {{ issues().length }} problema(s) antes de guardar:
            </strong>
            <ul>
              @for (issue of issues(); track issueKey(issue)) {
                <li>
                  @if (issue.questionId) {
                    <code>{{ issue.questionId }}</code>:
                  }
                  {{ issue.message }}
                </li>
              }
            </ul>
          </section>
        }

        <section class="snapshot-panel panel" aria-label="Catálogos incluidos en el RSVP">
          <div>
            <span class="eyebrow">Snapshot operativo</span>
            <h2>Menú, transporte y hospedaje</h2>
            <p>
              La versión guardada congela los catálogos activos actuales para
              que el enlace público use una definición estable.
            </p>
          </div>
          <div class="snapshot-grid">
            <article>
              <strong>{{ eventMenus().length }}</strong>
              <span>menús</span>
              <small>{{ menuOptionCount() }} opciones activas</small>
            </article>
            <article>
              <strong>{{ transportOptions().length }}</strong>
              <span>transportes</span>
              <small>{{ activeTransportCount() }} activos</small>
            </article>
            <article>
              <strong>{{ accommodationOptions().length }}</strong>
              <span>hospedajes</span>
              <small>{{ activeAccommodationCount() }} activos</small>
            </article>
          </div>
        </section>

        <div class="editor-layout">
          <section class="question-column">
            <div class="section-heading">
              <div>
                <h2>Preguntas</h2>
                <p>
                  {{ questions().length }} de
                  {{ catalog()?.maximumQuestions ?? 0 }}
                </p>
              </div>
              <button
                type="button"
                class="btn-primary"
                [disabled]="!canEdit()
                  || questions().length >= (catalog()?.maximumQuestions ?? 0)"
                (click)="addQuestion()"
              >+ Agregar pregunta</button>
            </div>

            @for (
              question of orderedQuestions();
              track question.id;
              let questionIndex = $index
            ) {
              <article class="question-card">
                <header class="question-card__header">
                  <div>
                    <span class="question-number">
                      Pregunta {{ questionIndex + 1 }}
                    </span>
                    <code>{{ question.id }}</code>
                  </div>
                  <button
                    type="button"
                    class="btn-danger-text"
                    [disabled]="!canEdit()"
                    (click)="removeQuestion(question.id)"
                  >Eliminar</button>
                </header>

                <div class="form-grid">
                  <label>
                    ID estable
                    <input
                      [value]="question.id"
                      [disabled]="!canEdit()"
                      (change)="renameQuestion(
                        question.id,
                        eventValue($event)
                      )"
                    />
                  </label>
                  <label>
                    Tipo
                    <select
                      [value]="question.questionType"
                      [disabled]="!canEdit()"
                      (change)="changeQuestionType(
                        question.id,
                        eventValue($event)
                      )"
                    >
                      @for (type of catalog()?.questionTypes ?? []; track type) {
                        <option [value]="type">{{ typeLabel(type) }}</option>
                      }
                    </select>
                  </label>
                  <label>
                    Alcance
                    <select
                      [value]="question.scope"
                      [disabled]="!canEdit()"
                      (change)="patchQuestion(
                        question.id,
                        { scope: eventValue($event) }
                      )"
                    >
                      @for (scope of catalog()?.questionScopes ?? []; track scope) {
                        <option [value]="scope">{{ scopeLabel(scope) }}</option>
                      }
                    </select>
                  </label>
                  <label>
                    Categoría
                    <select
                      [value]="question.category"
                      [disabled]="!canEdit()"
                      (change)="patchQuestion(
                        question.id,
                        { category: eventValue($event) }
                      )"
                    >
                      @for (
                        category of catalog()?.questionCategories ?? [];
                        track category
                      ) {
                        <option [value]="category">
                          {{ categoryLabel(category) }}
                        </option>
                      }
                    </select>
                  </label>
                  <label class="span-two">
                    Etiqueta
                    <input
                      [value]="question.label"
                      [maxLength]="catalog()?.maximumQuestionLabelLength ?? 200"
                      [disabled]="!canEdit()"
                      (input)="patchQuestion(
                        question.id,
                        { label: eventValue($event) }
                      )"
                    />
                  </label>
                  <label class="span-two">
                    Ayuda opcional
                    <textarea
                      [value]="question.helpText ?? ''"
                      [maxLength]="catalog()?.maximumHelpTextLength ?? 1000"
                      [disabled]="!canEdit()"
                      (input)="patchQuestion(
                        question.id,
                        { helpText: nullableText(eventValue($event)) }
                      )"
                    ></textarea>
                  </label>
                </div>

                <div class="checks">
                  <label>
                    <input
                      type="checkbox"
                      [checked]="question.isRequired"
                      [disabled]="!canEdit()"
                      (change)="setRequired(
                        question.id,
                        eventChecked($event)
                      )"
                    />
                    Obligatoria
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      [checked]="question.isSensitive"
                      [disabled]="!canEdit()"
                      (change)="patchQuestion(
                        question.id,
                        { isSensitive: eventChecked($event) }
                      )"
                    />
                    Respuesta sensible
                  </label>
                  <label>
                    <input
                      type="checkbox"
                      [checked]="question.isActive"
                      [disabled]="!canEdit()"
                      (change)="patchQuestion(
                        question.id,
                        { isActive: eventChecked($event) }
                      )"
                    />
                    Activa
                  </label>
                </div>

                @if (isChoice(question)) {
                  <section class="subsection">
                    <div class="subsection__heading">
                      <strong>Opciones</strong>
                      <button
                        type="button"
                        class="btn-text"
                        [disabled]="!canEdit()"
                        (click)="addOption(question.id)"
                      >+ Opción</button>
                    </div>
                    @for (
                      option of question.options;
                      track option.sortOrder;
                      let optionIndex = $index
                    ) {
                      <div class="option-row">
                        <input
                          aria-label="Clave estable"
                          [value]="option.key"
                          [disabled]="!canEdit()"
                          (input)="patchOption(
                            question.id,
                            optionIndex,
                            { key: eventValue($event) }
                          )"
                        />
                        <input
                          aria-label="Texto visible"
                          [value]="option.label"
                          [disabled]="!canEdit()"
                          (input)="patchOption(
                            question.id,
                            optionIndex,
                            { label: eventValue($event) }
                          )"
                        />
                        <label>
                          <input
                            type="checkbox"
                            [checked]="option.isActive"
                            [disabled]="!canEdit()"
                            (change)="patchOption(
                              question.id,
                              optionIndex,
                              { isActive: eventChecked($event) }
                            )"
                          />
                          Activa
                        </label>
                        <button
                          type="button"
                          class="btn-danger-text"
                          [disabled]="!canEdit()"
                          (click)="removeOption(
                            question.id,
                            optionIndex
                          )"
                        >Quitar</button>
                      </div>
                    }
                  </section>
                }

                <section class="subsection">
                  <strong>Reglas compatibles</strong>
                  <p class="rule-hint">
                    {{
                      (catalog()?.compatibleRules?.[
                        question.questionType
                      ] ?? []).join(', ')
                    }}
                  </p>
                  <div class="rules-grid">
                    @if (supportsRule(question, 'minLength')) {
                      <label>
                        Mínimo de caracteres
                        <input
                          type="number"
                          min="0"
                          [value]="question.validationRules.minLength ?? ''"
                          [disabled]="!canEdit()"
                          (input)="patchRuleNumber(
                            question.id,
                            'minLength',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'maxLength')) {
                      <label>
                        Máximo de caracteres
                        <input
                          type="number"
                          min="1"
                          [value]="question.validationRules.maxLength ?? ''"
                          [disabled]="!canEdit()"
                          (input)="patchRuleNumber(
                            question.id,
                            'maxLength',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'minimumSelections')) {
                      <label>
                        Selecciones mínimas
                        <input
                          type="number"
                          min="0"
                          [value]="
                            question.validationRules.minimumSelections ?? ''
                          "
                          [disabled]="!canEdit()"
                          (input)="patchRuleNumber(
                            question.id,
                            'minimumSelections',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'maximumSelections')) {
                      <label>
                        Selecciones máximas
                        <input
                          type="number"
                          min="1"
                          [value]="
                            question.validationRules.maximumSelections ?? ''
                          "
                          [disabled]="!canEdit()"
                          (input)="patchRuleNumber(
                            question.id,
                            'maximumSelections',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'minimum')) {
                      <label>
                        Valor mínimo
                        <input
                          type="number"
                          [value]="question.validationRules.minimum ?? ''"
                          [disabled]="!canEdit()"
                          (input)="patchRuleNumber(
                            question.id,
                            'minimum',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'maximum')) {
                      <label>
                        Valor máximo
                        <input
                          type="number"
                          [value]="question.validationRules.maximum ?? ''"
                          [disabled]="!canEdit()"
                          (input)="patchRuleNumber(
                            question.id,
                            'maximum',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'integerOnly')) {
                      <label class="inline-check">
                        <input
                          type="checkbox"
                          [checked]="
                            question.validationRules.integerOnly ?? false
                          "
                          [disabled]="!canEdit()"
                          (change)="patchRuleBoolean(
                            question.id,
                            'integerOnly',
                            eventChecked($event)
                          )"
                        />
                        Solo enteros
                      </label>
                    }
                    @if (supportsRule(question, 'minimumDate')) {
                      <label>
                        Fecha mínima
                        <input
                          type="date"
                          [value]="question.validationRules.minimumDate ?? ''"
                          [disabled]="!canEdit()"
                          (input)="patchRuleText(
                            question.id,
                            'minimumDate',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                    @if (supportsRule(question, 'maximumDate')) {
                      <label>
                        Fecha máxima
                        <input
                          type="date"
                          [value]="question.validationRules.maximumDate ?? ''"
                          [disabled]="!canEdit()"
                          (input)="patchRuleText(
                            question.id,
                            'maximumDate',
                            eventValue($event)
                          )"
                        />
                      </label>
                    }
                  </div>
                </section>

                <section class="subsection">
                  <strong>Visibilidad</strong>
                  <div class="visibility-row">
                    <label>
                      Condición
                      <select
                        [value]="question.visibilityRule.conditionType"
                        [disabled]="!canEdit()"
                        (change)="setVisibilityType(
                          question.id,
                          eventValue($event)
                        )"
                      >
                        @for (
                          type of catalog()?.visibilityConditionTypes ?? [];
                          track type
                        ) {
                          <option [value]="type">
                            {{ visibilityLabel(type) }}
                          </option>
                        }
                      </select>
                    </label>
                    @if (usesPreviousAnswer(question.visibilityRule)) {
                      <label>
                        Pregunta anterior
                        <select
                          [value]="
                            question.visibilityRule.referenceQuestionId ?? ''
                          "
                          [disabled]="!canEdit()"
                          (change)="patchVisibility(
                            question.id,
                            {
                              referenceQuestionId: nullableText(
                                eventValue($event)
                              )
                            }
                          )"
                        >
                          <option value="">Selecciona…</option>
                          @for (
                            reference of referencesFor(question);
                            track reference.id
                          ) {
                            <option [value]="reference.id">
                              {{ reference.label }}
                            </option>
                          }
                        </select>
                      </label>
                    }
                    @if (requiresExpectedValue(question.visibilityRule)) {
                      <label>
                        Valor esperado
                        <input
                          [value]="
                            question.visibilityRule.expectedValue ?? ''
                          "
                          [disabled]="!canEdit()"
                          (input)="patchVisibility(
                            question.id,
                            {
                              expectedValue: nullableText(
                                eventValue($event)
                              )
                            }
                          )"
                        />
                      </label>
                    }
                  </div>
                  @if (isComposite(question.visibilityRule)) {
                    <div class="condition-tree">
                      @for (
                        child of question.visibilityRule.conditions;
                        track $index;
                        let childIndex = $index
                      ) {
                        <div class="child-condition">
                          <select
                            [value]="child.conditionType"
                            [disabled]="!canEdit()"
                            (change)="setChildVisibilityType(
                              question.id,
                              childIndex,
                              eventValue($event)
                            )"
                          >
                            @for (
                              type of simpleConditionTypes();
                              track type
                            ) {
                              <option [value]="type">
                                {{ visibilityLabel(type) }}
                              </option>
                            }
                          </select>
                          @if (usesPreviousAnswer(child)) {
                            <select
                              [value]="child.referenceQuestionId ?? ''"
                              [disabled]="!canEdit()"
                              (change)="patchChildVisibility(
                                question.id,
                                childIndex,
                                {
                                  referenceQuestionId: nullableText(
                                    eventValue($event)
                                  )
                                }
                              )"
                            >
                              <option value="">Pregunta anterior…</option>
                              @for (
                                reference of referencesFor(question);
                                track reference.id
                              ) {
                                <option [value]="reference.id">
                                  {{ reference.label }}
                                </option>
                              }
                            </select>
                          }
                          @if (requiresExpectedValue(child)) {
                            <input
                              aria-label="Valor esperado"
                              [value]="child.expectedValue ?? ''"
                              [disabled]="!canEdit()"
                              (input)="patchChildVisibility(
                                question.id,
                                childIndex,
                                {
                                  expectedValue: nullableText(
                                    eventValue($event)
                                  )
                                }
                              )"
                            />
                          }
                          <button
                            type="button"
                            class="btn-danger-text"
                            [disabled]="!canEdit()"
                            (click)="removeChildCondition(
                              question.id,
                              childIndex
                            )"
                          >Quitar</button>
                        </div>
                      }
                      <button
                        type="button"
                        class="btn-text"
                        [disabled]="!canEdit()"
                        (click)="addChildCondition(question.id)"
                      >+ Agregar condición</button>
                    </div>
                  }
                  <p class="condition-preview">
                    Vista previa: {{ visibilitySummary(
                      question.visibilityRule
                    ) }}
                  </p>
                </section>

                @if (question.isSensitive) {
                  <p class="sensitive-note">
                    🔒 La respuesta se oculta de DTO y exportaciones generales.
                  </p>
                }
                @for (
                  questionIssue of questionIssues(question.id);
                  track issueKey(questionIssue)
                ) {
                  <p class="field-error">{{ questionIssue.message }}</p>
                }
              </article>
            } @empty {
              <div class="empty-state">
                <h3>Sin preguntas</h3>
                <p>Agrega la primera pregunta del formulario.</p>
              </div>
            }
          </section>

          <aside class="preview-column">
            <section class="panel sticky">
              <h2>Simulación de visibilidad</h2>
              <p>
                Cambia el contexto para comprobar qué preguntas quedan
                visibles. La simulación no sustituye la evaluación de la API.
              </p>
              <label>
                Asistencia
                <select
                  [value]="simulationAttendance()"
                  (change)="setSimulationAttendance(
                    eventValue($event)
                  )"
                >
                  <option value="Pending">Pendiente</option>
                  <option value="Attending">Asistirá</option>
                  <option value="NotAttending">No asistirá</option>
                  <option value="Tentative">Tal vez</option>
                </select>
              </label>
              <label>
                Edad
                <select
                  [value]="simulationAge()"
                  (change)="simulationAge.set(eventValue($event))"
                >
                  <option value="Adult">Adulto</option>
                  <option value="Teen">Adolescente</option>
                  <option value="Child">Niño</option>
                  <option value="Infant">Bebé</option>
                </select>
              </label>
              <label>
                Tipo de invitado
                <input
                  [value]="simulationGuestType()"
                  (input)="simulationGuestType.set(eventValue($event))"
                />
              </label>
              <label>
                Etiquetas del grupo
                <input
                  [value]="simulationTags()"
                  (input)="simulationTags.set(eventValue($event))"
                  placeholder="VIP, Familia"
                />
              </label>
              <div class="checks vertical">
                <label>
                  <input
                    type="checkbox"
                    [checked]="simulationPrimary()"
                    (change)="simulationPrimary.set(
                      eventChecked($event)
                    )"
                  />
                  Contacto principal
                </label>
                <label>
                  <input
                    type="checkbox"
                    [checked]="simulationUnnamed()"
                    (change)="simulationUnnamed.set(
                      eventChecked($event)
                    )"
                  />
                  Acompañante sin nombre previo
                </label>
              </div>
              <h3>Respuestas previas simuladas</h3>
              @for (question of orderedQuestions(); track question.id) {
                <label>
                  {{ question.label }}
                  <input
                    [value]="simulationAnswer(question.id)"
                    (input)="setSimulationAnswer(
                      question.id,
                      eventValue($event)
                    )"
                  />
                </label>
              }
              <h3>Resultado</h3>
              <ul class="simulation-list">
                @for (question of orderedQuestions(); track question.id) {
                  <li [class.hidden-question]="
                    !simulationVisibleIds().has(question.id)
                  ">
                    <span>
                      {{
                        simulationVisibleIds().has(question.id)
                          ? 'Visible'
                          : 'Oculta'
                      }}
                    </span>
                    {{ question.label }}
                  </li>
                }
              </ul>
            </section>
          </aside>
        </div>
      }
    </main>
  `,
  styles: [`
    :host {
      display: block;
      color: #202124;
    }
    .editor-page {
      max-width: 1440px;
      margin: 0 auto;
      padding: 28px;
    }
    .page-header,
    .section-heading,
    .question-card__header,
    .subsection__heading,
    .workflow {
      display: flex;
      align-items: flex-start;
      justify-content: space-between;
      gap: 20px;
    }
    .page-header { margin-bottom: 22px; }
    .page-header h1 { margin: 4px 0 8px; }
    .page-header p,
    .workflow p,
    .section-heading p,
    .preview-column p {
      color: #5f6368;
      margin: 4px 0;
    }
    .eyebrow {
      color: #7b5e32 !important;
      font-size: 12px;
      font-weight: 700;
      letter-spacing: .08em;
      text-transform: uppercase;
    }
    .back-link { color: #6a4b23; text-decoration: none; }
    .status {
      background: #f5efe5;
      border-radius: 999px;
      padding: 8px 12px;
      white-space: nowrap;
    }
    .panel,
    .question-card,
    .empty-state {
      background: #fff;
      border: 1px solid #e2ddd4;
      border-radius: 14px;
      box-shadow: 0 4px 18px rgb(56 45 30 / 5%);
    }
    .panel { padding: 18px; }
    .workflow { margin-bottom: 18px; }
    .workflow__actions {
      display: flex;
      flex-wrap: wrap;
      gap: 8px;
      justify-content: flex-end;
    }
    button {
      border: 0;
      border-radius: 8px;
      cursor: pointer;
      font: inherit;
      padding: 9px 14px;
    }
    button:disabled { cursor: not-allowed; opacity: .55; }
    .btn-primary { background: #6a4b23; color: #fff; }
    .btn-secondary { background: #eee7dc; color: #4c351b; }
    .btn-text { background: transparent; color: #6a4b23; padding: 4px; }
    .btn-danger-text {
      background: transparent;
      color: #b3261e;
      padding: 4px;
    }
    .validation-summary {
      background: #fff4f2;
      border: 1px solid #f4c7c3;
      border-radius: 12px;
      color: #8c1d18;
      margin-bottom: 18px;
      padding: 14px 18px;
    }
    .validation-summary ul { margin-bottom: 0; }
    .editor-layout {
      display: grid;
      grid-template-columns: minmax(0, 1fr) 360px;
      gap: 20px;
    }
    .question-column { min-width: 0; }
    .section-heading { margin: 4px 0 14px; }
    .section-heading h2 { margin: 0; }
    .question-card { margin-bottom: 16px; padding: 18px; }
    .snapshot-panel {
      align-items: start;
      display: grid;
      gap: 16px;
      grid-template-columns: minmax(0, 1fr) minmax(300px, 520px);
      margin-bottom: 20px;
    }
    .snapshot-panel h2 { margin: 2px 0 8px; }
    .snapshot-panel p {
      color: #6b6f73;
      margin: 0;
    }
    .snapshot-grid {
      display: grid;
      gap: 10px;
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }
    .snapshot-grid article {
      background: #f8f6f2;
      border-radius: 8px;
      padding: 12px;
    }
    .snapshot-grid strong {
      display: block;
      font-size: 24px;
    }
    .snapshot-grid span,
    .snapshot-grid small {
      display: block;
    }
    .snapshot-grid small {
      color: #6b6f73;
      font-size: 12px;
      margin-top: 3px;
    }
    .question-card__header {
      border-bottom: 1px solid #eee8de;
      margin-bottom: 16px;
      padding-bottom: 12px;
    }
    .question-number {
      color: #5f6368;
      display: block;
      font-size: 12px;
      margin-bottom: 3px;
      text-transform: uppercase;
    }
    code {
      background: #f6f3ef;
      border-radius: 4px;
      padding: 2px 5px;
    }
    .form-grid,
    .rules-grid {
      display: grid;
      grid-template-columns: repeat(2, minmax(0, 1fr));
      gap: 12px;
    }
    label {
      color: #4a4d50;
      display: grid;
      font-size: 13px;
      gap: 5px;
    }
    .span-two { grid-column: span 2; }
    input,
    select,
    textarea {
      background: #fff;
      border: 1px solid #c9c3ba;
      border-radius: 7px;
      box-sizing: border-box;
      color: #202124;
      font: inherit;
      min-width: 0;
      padding: 9px 10px;
      width: 100%;
    }
    textarea { min-height: 68px; resize: vertical; }
    input:focus,
    select:focus,
    textarea:focus {
      border-color: #6a4b23;
      outline: 2px solid rgb(106 75 35 / 15%);
    }
    .checks {
      display: flex;
      flex-wrap: wrap;
      gap: 16px;
      margin: 14px 0;
    }
    .checks label,
    .inline-check {
      align-items: center;
      display: flex;
      gap: 7px;
    }
    .checks input,
    .inline-check input { width: auto; }
    .subsection {
      border-top: 1px solid #eee8de;
      margin-top: 16px;
      padding-top: 14px;
    }
    .rule-hint,
    .condition-preview {
      color: #6b6f73;
      font-size: 12px;
    }
    .option-row,
    .visibility-row,
    .child-condition {
      align-items: end;
      display: grid;
      gap: 8px;
      margin-top: 9px;
    }
    .option-row {
      grid-template-columns: 150px minmax(180px, 1fr) auto auto;
    }
    .visibility-row {
      grid-template-columns: repeat(3, minmax(0, 1fr));
    }
    .child-condition {
      background: #f8f6f2;
      border-left: 3px solid #b99768;
      grid-template-columns: repeat(3, minmax(0, 1fr)) auto;
      padding: 10px;
    }
    .sensitive-note {
      background: #fff8e7;
      border-radius: 8px;
      color: #795400;
      font-size: 13px;
      padding: 9px 11px;
    }
    .field-error { color: #b3261e; font-size: 12px; }
    .empty-state {
      padding: 32px;
      text-align: center;
    }
    .sticky { position: sticky; top: 18px; }
    .preview-column label { margin: 10px 0; }
    .vertical { display: grid; }
    .simulation-list { list-style: none; padding: 0; }
    .simulation-list li {
      border-bottom: 1px solid #eee8de;
      padding: 9px 0;
    }
    .simulation-list span {
      color: #137333;
      display: inline-block;
      font-size: 11px;
      font-weight: 700;
      text-transform: uppercase;
      width: 52px;
    }
    .simulation-list .hidden-question { color: #9aa0a6; }
    .simulation-list .hidden-question span { color: #b3261e; }
    @media (max-width: 1000px) {
      .editor-layout { grid-template-columns: 1fr; }
      .sticky { position: static; }
    }
    @media (max-width: 680px) {
      .editor-page { padding: 16px; }
      .page-header,
      .section-heading,
      .workflow { flex-direction: column; }
      .form-grid,
      .rules-grid,
      .snapshot-panel,
      .snapshot-grid,
      .visibility-row,
      .option-row,
      .child-condition { grid-template-columns: 1fr; }
      .span-two { grid-column: span 1; }
      .workflow__actions { justify-content: flex-start; }
    }
  `],
})
export class RsvpFormEditorPage {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly organization = inject(OrganizationContextService);
  private readonly toast = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly eventId = signal('');
  protected readonly loading = signal(true);
  protected readonly busy = signal(false);
  protected readonly catalog = signal<RsvpQuestionCatalog | null>(null);
  protected readonly form = signal<RsvpFormResponse | null>(null);
  protected readonly versionId = signal<string | null>(null);
  protected readonly questions = signal<RsvpQuestion[]>([]);
  protected readonly eventMenus = signal<EventMenuResponse[]>([]);
  protected readonly transportOptions =
    signal<EventTransportOptionResponse[]>([]);
  protected readonly accommodationOptions =
    signal<EventAccommodationOptionResponse[]>([]);
  protected readonly simulationAttendance =
    signal<GuestAttendanceStatus>('Attending');
  protected readonly simulationAge = signal('Adult');
  protected readonly simulationGuestType = signal('Standard');
  protected readonly simulationTags = signal('');
  protected readonly simulationPrimary = signal(true);
  protected readonly simulationUnnamed = signal(false);
  protected readonly simulationAnswers =
    signal<Record<string, RsvpDraftAnswer>>({});

  protected readonly orderedQuestions = computed(() =>
    [...this.questions()].sort((left, right) =>
      left.sortOrder - right.sortOrder));
  protected readonly issues = computed<RsvpEditorIssue[]>(() => {
    const currentCatalog = this.catalog();
    return currentCatalog
      ? validateRsvpQuestions(this.questions(), currentCatalog)
      : [];
  });
  protected readonly simpleConditionTypes = computed(() =>
    (this.catalog()?.visibilityConditionTypes ?? [])
      .filter((type) => type !== 'All' && type !== 'Any'));
  protected readonly menuOptionCount = computed(() =>
    this.eventMenus()
      .reduce((total, menu) =>
        total + menu.options.filter((option) => option.isActive).length, 0));
  protected readonly activeTransportCount = computed(() =>
    this.transportOptions().filter((option) => option.isActive).length);
  protected readonly activeAccommodationCount = computed(() =>
    this.accommodationOptions().filter((option) => option.isActive).length);
  protected readonly simulationVisibleIds = computed(() => {
    const guestId = 'simulation-guest';
    return new Set(
      visibleQuestionInstances(
        this.questions(),
        {
          guests: [
            {
              responseGuestId: guestId,
              eventGuestId: guestId,
              displayName: 'Invitado de simulación',
              ageCategory: this.simulationAge(),
              guestType: this.simulationGuestType(),
              attendanceStatus: this.simulationAttendance(),
              isUnnamedCompanion: this.simulationUnnamed(),
              isPrimaryContact: this.simulationPrimary(),
            },
          ],
          groupTags: this.simulationTags()
            .split(',')
            .map((tag) => tag.trim())
            .filter(Boolean),
        },
        this.simulationAnswers(),
      ).map((instance) => instance.question.id),
    );
  });

  constructor() {
    this.eventId.set(
      this.route.snapshot.paramMap.get('id') ?? '',
    );
    this.load();
  }

  protected canEdit(): boolean {
    return this.form()?.status === 'Draft'
      || this.form()?.status === 'ChangesRequested';
  }

  protected createForm(): void {
    this.runMutation(() =>
      this.api.createRsvpForm(
        this.organization.requireOrganizationId(),
        this.eventId(),
      ), (created) => {
      this.form.set(created);
      this.versionId.set(null);
      this.questions.set([]);
      this.toast.success('Formulario creado.');
    });
  }

  protected createNewDraft(): void {
    this.runMutation(() =>
      this.api.createRsvpFormDraft(
        this.organization.requireOrganizationId(),
        this.eventId(),
      ), (draft) => {
      this.form.set(draft);
      this.versionId.set(null);
      this.toast.success(
        'Nueva versión iniciada; la publicada sigue activa.',
      );
    });
  }

  protected saveVersion(): void {
    if (!this.canEdit() || this.issues().length > 0) return;
    this.runMutation(() =>
      this.api.createRsvpFormVersion(
        this.organization.requireOrganizationId(),
        this.eventId(),
        JSON.stringify(this.orderedQuestions()),
        JSON.stringify(this.eventMenus()),
        JSON.stringify(this.transportOptions()),
        JSON.stringify(this.accommodationOptions()),
      ), (version) => {
      this.versionId.set(version.id);
      this.toast.success('Versión guardada y validada por la API.');
    });
  }

  protected submitForReview(): void {
    this.runMutation(() =>
      this.api.submitRsvpFormReview(
        this.organization.requireOrganizationId(),
        this.eventId(),
      ), (updated) => {
      this.form.set(updated);
      this.toast.success('Versión enviada a revisión.');
    });
  }

  protected approve(): void {
    const currentVersionId = this.versionId();
    if (!currentVersionId) return;
    this.runMutation(() =>
      this.api.approveRsvpForm(
        this.organization.requireOrganizationId(),
        this.eventId(),
        currentVersionId,
      ), () => {
      this.refreshForm();
      this.toast.success('Versión aprobada.');
    });
  }

  protected publish(): void {
    const currentVersionId = this.versionId();
    if (!currentVersionId) return;
    this.runMutation(() =>
      this.api.publishRsvpForm(
        this.organization.requireOrganizationId(),
        this.eventId(),
        currentVersionId,
      ), () => {
      this.refreshForm();
      this.toast.success('Formulario publicado.');
    });
  }

  protected addQuestion(): void {
    if (!this.canEdit()) return;
    const existingIds = new Set(
      this.questions().map((question) => question.id),
    );
    let sequence = this.questions().length + 1;
    let id = `question-${sequence}`;
    while (existingIds.has(id)) {
      sequence += 1;
      id = `question-${sequence}`;
    }
    const type = this.catalog()?.questionTypes[0] ?? 'ShortText';
    this.questions.update((items) => [
      ...items,
      this.newQuestion(id, type, items.length),
    ]);
  }

  protected removeQuestion(questionId: string): void {
    if (!this.canEdit()) return;
    this.questions.update((items) =>
      items
        .filter((question) => question.id !== questionId)
        .map((question, index) => ({
          ...question,
          sortOrder: index,
        })));
  }

  protected renameQuestion(
    currentId: string,
    nextId: string,
  ): void {
    if (!this.canEdit()) return;
    const normalized = nextId.trim();
    this.questions.update((items) =>
      items.map((question) => ({
        ...question,
        id: question.id === currentId ? normalized : question.id,
        visibilityRule: replaceVisibilityReference(
          question.visibilityRule,
          currentId,
          normalized,
        ),
      })));
  }

  protected patchQuestion(
    questionId: string,
    patch: Record<string, unknown>,
  ): void {
    if (!this.canEdit()) return;
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? { ...question, ...patch } as RsvpQuestion
          : question));
  }

  protected changeQuestionType(
    questionId: string,
    typeValue: string,
  ): void {
    const type = typeValue as RsvpQuestionType;
    this.questions.update((items) =>
      items.map((question) => {
        if (question.id !== questionId) return question;
        const options = type === 'SingleChoice'
          ? [
              this.newOption('option-a', 'Opción A', 0),
              this.newOption('option-b', 'Opción B', 1),
            ]
          : type === 'MultipleChoice'
            ? [this.newOption('option-a', 'Opción A', 0)]
            : [];
        const isConsent = type === 'InformationalConsent';
        return {
          ...question,
          questionType: type,
          category: isConsent ? 'Consent' : question.category,
          isSensitive: isConsent || question.isSensitive,
          options,
          validationRules: {
            required: question.isRequired,
          },
        };
      }));
  }

  protected setRequired(questionId: string, required: boolean): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              isRequired: required,
              validationRules: {
                ...question.validationRules,
                required,
              },
            }
          : question));
  }

  protected addOption(questionId: string): void {
    this.questions.update((items) =>
      items.map((question) => {
        if (question.id !== questionId) return question;
        const index = question.options.length;
        return {
          ...question,
          options: [
            ...question.options,
            this.newOption(
              `option-${index + 1}`,
              `Opción ${index + 1}`,
              index,
            ),
          ],
        };
      }));
  }

  protected patchOption(
    questionId: string,
    optionIndex: number,
    patch: Partial<RsvpQuestionOption>,
  ): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              options: question.options.map((option, index) =>
                index === optionIndex
                  ? { ...option, ...patch }
                  : option),
            }
          : question));
  }

  protected removeOption(
    questionId: string,
    optionIndex: number,
  ): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              options: question.options
                .filter((_, index) => index !== optionIndex)
                .map((option, index) => ({
                  ...option,
                  sortOrder: index,
                })),
            }
          : question));
  }

  protected supportsRule(
    question: RsvpQuestion,
    rule: string,
  ): boolean {
    return this.catalog()
      ?.compatibleRules[question.questionType]
      ?.includes(rule) ?? false;
  }

  protected patchRuleNumber(
    questionId: string,
    rule: 'minLength' | 'maxLength'
      | 'minimumSelections' | 'maximumSelections'
      | 'minimum' | 'maximum',
    value: string,
  ): void {
    const parsed = value.trim() === '' ? null : Number(value);
    this.patchValidationRule(
      questionId,
      rule,
      parsed !== null && Number.isFinite(parsed) ? parsed : null,
    );
  }

  protected patchRuleBoolean(
    questionId: string,
    rule: 'integerOnly',
    value: boolean,
  ): void {
    this.patchValidationRule(questionId, rule, value);
  }

  protected patchRuleText(
    questionId: string,
    rule: 'minimumDate' | 'maximumDate',
    value: string,
  ): void {
    this.patchValidationRule(
      questionId,
      rule,
      value.trim() || null,
    );
  }

  protected setVisibilityType(
    questionId: string,
    value: string,
  ): void {
    this.patchQuestion(questionId, {
      visibilityRule: this.newVisibilityRule(
        value as RsvpVisibilityConditionType,
      ),
    });
  }

  protected patchVisibility(
    questionId: string,
    patch: Partial<RsvpVisibilityRule>,
  ): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              visibilityRule: {
                ...question.visibilityRule,
                ...patch,
              },
            }
          : question));
  }

  protected addChildCondition(questionId: string): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              visibilityRule: {
                ...question.visibilityRule,
                conditions: [
                  ...question.visibilityRule.conditions,
                  this.newVisibilityRule('Always'),
                ],
              },
            }
          : question));
  }

  protected removeChildCondition(
    questionId: string,
    childIndex: number,
  ): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              visibilityRule: {
                ...question.visibilityRule,
                conditions: question.visibilityRule.conditions
                  .filter((_, index) => index !== childIndex),
              },
            }
          : question));
  }

  protected setChildVisibilityType(
    questionId: string,
    childIndex: number,
    value: string,
  ): void {
    this.patchChildVisibility(
      questionId,
      childIndex,
      this.newVisibilityRule(
        value as RsvpVisibilityConditionType,
      ),
    );
  }

  protected patchChildVisibility(
    questionId: string,
    childIndex: number,
    patch: Partial<RsvpVisibilityRule>,
  ): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              visibilityRule: {
                ...question.visibilityRule,
                conditions: question.visibilityRule.conditions.map(
                  (condition, index) =>
                    index === childIndex
                      ? { ...condition, ...patch }
                      : condition,
                ),
              },
            }
          : question));
  }

  protected referencesFor(question: RsvpQuestion): RsvpQuestion[] {
    return compatibleReferenceQuestions(this.questions(), question);
  }

  protected questionIssues(questionId: string): RsvpEditorIssue[] {
    return this.issues().filter((issue) =>
      issue.questionId === questionId);
  }

  protected isChoice(question: RsvpQuestion): boolean {
    return question.questionType === 'SingleChoice'
      || question.questionType === 'MultipleChoice';
  }

  protected isComposite(rule: RsvpVisibilityRule): boolean {
    return rule.conditionType === 'All'
      || rule.conditionType === 'Any';
  }

  protected usesPreviousAnswer(rule: RsvpVisibilityRule): boolean {
    return rule.conditionType === 'PreviousAnswerEquals'
      || rule.conditionType === 'PreviousAnswerContains';
  }

  protected requiresExpectedValue(rule: RsvpVisibilityRule): boolean {
    return ![
      'Always',
      'All',
      'Any',
    ].includes(rule.conditionType);
  }

  protected visibilitySummary(rule: RsvpVisibilityRule): string {
    if (rule.conditionType === 'Always') return 'Siempre visible';
    if (this.isComposite(rule)) {
      const connector = rule.conditionType === 'All' ? ' Y ' : ' O ';
      return `(${rule.conditions
        .map((condition) => this.visibilitySummary(condition))
        .join(connector) || 'sin condiciones'})`;
    }
    if (this.usesPreviousAnswer(rule)) {
      return `${rule.referenceQuestionId ?? 'pregunta pendiente'} `
        + `${rule.conditionType === 'PreviousAnswerContains'
          ? 'contiene'
          : 'es'} ${rule.expectedValue ?? '…'}`;
    }
    return `${this.visibilityLabel(rule.conditionType)}: `
      + `${rule.expectedValue ?? '…'}`;
  }

  protected simulationAnswer(questionId: string): string {
    const value = this.simulationAnswers()[
      rsvpAnswerKey(questionId, null)
    ];
    return Array.isArray(value) ? value.join(',') : String(value ?? '');
  }

  protected setSimulationAnswer(
    questionId: string,
    value: string,
  ): void {
    const question = this.questions().find((item) =>
      item.id === questionId);
    if (!question) return;
    const parsed: RsvpDraftAnswer =
      question.questionType === 'MultipleChoice'
        ? value.split(',').map((item) => item.trim()).filter(Boolean)
        : question.questionType === 'YesNo'
          || question.questionType === 'InformationalConsent'
          ? value === 'true'
          : question.questionType === 'Number'
            ? Number(value)
            : value;
    this.simulationAnswers.update((answers) => ({
      ...answers,
      [rsvpAnswerKey(questionId, null)]: parsed,
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

  protected setSimulationAttendance(value: string): void {
    if ([
      'Pending',
      'Attending',
      'NotAttending',
      'Tentative',
      'CancelledAfterConfirmation',
    ].includes(value)) {
      this.simulationAttendance.set(
        value as GuestAttendanceStatus,
      );
    }
  }

  protected nullableText(value: string): string | null {
    return value.trim() || null;
  }

  protected issueKey(issue: RsvpEditorIssue): string {
    return `${issue.questionId ?? ''}:${issue.code}:${issue.message}`;
  }

  protected typeLabel(value: string): string {
    return labels['questionType']?.[value] ?? value;
  }

  protected scopeLabel(value: string): string {
    return labels['scope']?.[value] ?? value;
  }

  protected categoryLabel(value: string): string {
    return labels['category']?.[value] ?? value;
  }

  protected visibilityLabel(value: string): string {
    return labels['visibility']?.[value] ?? value;
  }

  protected statusLabel(value: string): string {
    return labels['status']?.[value] ?? value;
  }

  private load(): void {
    const organizationId =
      this.organization.requireOrganizationId();
    forkJoin({
      catalog: this.api.getRsvpQuestionCatalog(
        organizationId,
        this.eventId(),
      ),
      menus: this.api.getEventMenus(
        organizationId,
        this.eventId(),
      ).pipe(catchError(() => of([]))),
      transport: this.api.getTransportOptions(
        organizationId,
        this.eventId(),
      ).pipe(catchError(() => of([]))),
      accommodation: this.api.getAccommodationOptions(
        organizationId,
        this.eventId(),
      ).pipe(catchError(() => of([]))),
    })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: ({ catalog, menus, transport, accommodation }) => {
          this.catalog.set(catalog);
          this.eventMenus.set(menus);
          this.transportOptions.set(transport);
          this.accommodationOptions.set(accommodation);
          this.loadForm();
        },
        error: (error) => {
          this.toast.error(getApiErrorMessage(error));
          this.loading.set(false);
        },
      });
  }

  private loadForm(): void {
    const organizationId =
      this.organization.requireOrganizationId();
    this.api.getRsvpForm(organizationId, this.eventId())
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (form) => {
          this.form.set(form);
          this.loadRelevantVersion(form);
        },
        error: (error) => {
          if (error instanceof HttpErrorResponse
              && error.status === 404) {
            this.form.set(null);
          } else {
            this.toast.error(getApiErrorMessage(error));
          }
          this.loading.set(false);
        },
      });
  }

  private loadRelevantVersion(form: RsvpFormResponse): void {
    const organizationId =
      this.organization.requireOrganizationId();
    if (form.status === 'Published'
        && form.activePublishedVersionId) {
      this.api.getRsvpFormVersion(
        organizationId,
        this.eventId(),
        form.activePublishedVersionId,
      )
        .pipe(takeUntilDestroyed(this.destroyRef))
        .subscribe({
          next: (version) => {
            this.applyVersion(version, false);
            this.loading.set(false);
          },
          error: (error) => {
            this.toast.error(getApiErrorMessage(error));
            this.loading.set(false);
          },
        });
      return;
    }

    this.api.getRsvpDraftFormVersion(
      organizationId,
      this.eventId(),
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (version) => {
          this.applyVersion(version, true);
          this.loading.set(false);
        },
        error: (error) => {
          if (error instanceof HttpErrorResponse
              && error.status === 404
              && form.activePublishedVersionId) {
            this.loadPublishedAsDraftSeed(
              organizationId,
              form.activePublishedVersionId,
            );
            return;
          }
          if (!(error instanceof HttpErrorResponse)
              || error.status !== 404) {
            this.toast.error(getApiErrorMessage(error));
          }
          this.loading.set(false);
        },
      });
  }

  private loadPublishedAsDraftSeed(
    organizationId: string,
    publishedVersionId: string,
  ): void {
    this.api.getRsvpFormVersion(
      organizationId,
      this.eventId(),
      publishedVersionId,
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (version) => {
          this.applyVersion(version, false);
          this.versionId.set(null);
          this.loading.set(false);
        },
        error: (error) => {
          this.toast.error(getApiErrorMessage(error));
          this.loading.set(false);
        },
      });
  }

  private applyVersion(
    version: RsvpFormVersionResponse,
    editableVersion: boolean,
  ): void {
    try {
      const parsed = JSON.parse(version.questionsSnapshot) as unknown;
      this.questions.set(
        Array.isArray(parsed) ? parsed as RsvpQuestion[] : [],
      );
      this.versionId.set(editableVersion ? version.id : null);
    } catch {
      this.questions.set([]);
      this.toast.error(
        'La versión contiene un snapshot que no puede mostrarse.',
      );
    }
  }

  private refreshForm(): void {
    this.api.getRsvpForm(
      this.organization.requireOrganizationId(),
      this.eventId(),
    )
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (form) => this.form.set(form),
        error: (error) => this.toast.error(getApiErrorMessage(error)),
      });
  }

  private runMutation<T>(
    operation: () => import('rxjs').Observable<T>,
    onSuccess: (value: T) => void,
  ): void {
    if (this.busy()) return;
    this.busy.set(true);
    operation()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (value) => {
          onSuccess(value);
          this.busy.set(false);
        },
        error: (error) => {
          this.toast.error(getApiErrorMessage(error));
          this.busy.set(false);
        },
      });
  }

  private patchValidationRule(
    questionId: string,
    key: keyof RsvpQuestion['validationRules'],
    value: string | number | boolean | null,
  ): void {
    this.questions.update((items) =>
      items.map((question) =>
        question.id === questionId
          ? {
              ...question,
              validationRules: {
                ...question.validationRules,
                [key]: value,
              },
            }
          : question));
  }

  private newQuestion(
    id: string,
    questionType: RsvpQuestionType,
    sortOrder: number,
  ): RsvpQuestion {
    return {
      id,
      questionType,
      scope: 'InvitationGroup',
      category: 'General',
      label: `Pregunta ${sortOrder + 1}`,
      helpText: null,
      isRequired: false,
      isSensitive: false,
      isActive: true,
      sortOrder,
      options: [],
      visibilityRule: this.newVisibilityRule('Always'),
      validationRules: { required: false },
    };
  }

  private newOption(
    key: string,
    label: string,
    sortOrder: number,
  ): RsvpQuestionOption {
    return {
      key,
      label,
      isActive: true,
      sortOrder,
    };
  }

  private newVisibilityRule(
    conditionType: RsvpVisibilityConditionType,
  ): RsvpVisibilityRule {
    const composite = conditionType === 'All'
      || conditionType === 'Any';
    const previous = conditionType === 'PreviousAnswerEquals'
      || conditionType === 'PreviousAnswerContains';
    const expectedValue = [
      'Always',
      'All',
      'Any',
    ].includes(conditionType)
      ? null
      : conditionType === 'AttendanceStatusEquals'
        ? 'Attending'
        : conditionType === 'GuestAgeCategoryEquals'
          ? 'Adult'
          : conditionType === 'GuestTypeEquals'
            ? 'Standard'
            : conditionType === 'IsUnnamedCompanion'
              || conditionType === 'IsPrimaryContact'
              ? 'true'
              : '';
    return {
      conditionType,
      referenceQuestionId: previous ? null : null,
      expectedValue,
      conditions: composite
        ? [this.newVisibilityRule('Always')]
        : [],
    };
  }
}

function replaceVisibilityReference(
  rule: RsvpVisibilityRule,
  previousId: string,
  nextId: string,
): RsvpVisibilityRule {
  return {
    ...rule,
    referenceQuestionId:
      rule.referenceQuestionId === previousId
        ? nextId
        : rule.referenceQuestionId,
    conditions: rule.conditions.map((condition) =>
      replaceVisibilityReference(condition, previousId, nextId)),
  };
}

const labels: Record<string, Record<string, string>> = {
  questionType: {
    ShortText: 'Texto corto',
    LongText: 'Texto largo',
    YesNo: 'Sí / No',
    SingleChoice: 'Selección única',
    MultipleChoice: 'Selección múltiple',
    Number: 'Número',
    Date: 'Fecha',
    InformationalConsent: 'Consentimiento informativo',
  },
  scope: {
    InvitationGroup: 'Grupo de invitación',
    IndividualGuest: 'Cada invitado',
    PrimaryContact: 'Contacto principal',
  },
  category: {
    General: 'General',
    Dietary: 'Alimentación',
    Transportation: 'Transporte',
    Accommodation: 'Hospedaje',
    Accessibility: 'Accesibilidad',
    Consent: 'Consentimiento',
    Other: 'Otra',
  },
  visibility: {
    Always: 'Siempre',
    AttendanceStatusEquals: 'Estado de asistencia',
    GuestAgeCategoryEquals: 'Categoría de edad',
    GuestTypeEquals: 'Tipo de invitado',
    GroupHasTag: 'Etiqueta del grupo',
    PreviousAnswerEquals: 'Respuesta previa igual a',
    PreviousAnswerContains: 'Respuesta previa contiene',
    IsUnnamedCompanion: 'Es acompañante sin nombre',
    IsPrimaryContact: 'Es contacto principal',
    All: 'Todas las condiciones',
    Any: 'Cualquier condición',
  },
  status: {
    Draft: 'Borrador',
    InReview: 'En revisión',
    ChangesRequested: 'Cambios solicitados',
    Approved: 'Aprobado',
    Published: 'Publicado',
    Archived: 'Archivado',
  },
};
