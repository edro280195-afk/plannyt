import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, expect, it, vi } from 'vitest';
import { ApiService } from '../../core/api/api.service';
import { OrganizationContextService } from '../../core/auth/organization-context.service';
import type {
  EventAccommodationOptionResponse,
  EventMenuResponse,
  EventTransportOptionResponse,
  RsvpFormResponse,
  RsvpFormVersionResponse,
  RsvpQuestion,
  RsvpQuestionCatalog,
  RsvpQuestionType,
} from '../../core/models/api.models';
import { ToastService } from '../../core/ui/toast.service';
import { RsvpFormEditorPage } from './rsvp-form-editor.page';

describe('RsvpFormEditorPage', () => {
  let fixture: ComponentFixture<RsvpFormEditorPage>;
  const catalog = createCatalog();
  const questions = catalog.questionTypes.map((type, index) =>
    createQuestion(type, index));
  const publishedForm: RsvpFormResponse = {
    id: 'form-1',
    status: 'Published',
    currentDraftVersion: 1,
    activePublishedVersionId: 'version-1',
    updatedAt: '2026-07-29T12:00:00Z',
  };
  const draftForm: RsvpFormResponse = {
    ...publishedForm,
    status: 'Draft',
    currentDraftVersion: 2,
  };
  const version: RsvpFormVersionResponse = {
    id: 'version-1',
    rsvpFormId: 'form-1',
    versionNumber: 1,
    settingsSnapshot: '{}',
    questionsSnapshot: JSON.stringify(questions),
    menuSnapshot: '[]',
    transportSnapshot: '[]',
    accommodationSnapshot: '[]',
    createdAt: '2026-07-29T12:00:00Z',
    approvedBy: 'user-1',
    approvedAt: '2026-07-29T12:00:00Z',
    publishedAt: '2026-07-29T12:00:00Z',
  };
  const savedVersion: RsvpFormVersionResponse = {
    ...version,
    id: 'version-2',
    versionNumber: 2,
    approvedBy: null,
    approvedAt: null,
    publishedAt: null,
  };
  const menus: EventMenuResponse[] = [
    {
      id: 'menu-1',
      name: 'Cena adultos',
      description: 'Menú principal',
      menuCategory: 'AdultMeal',
      isActive: true,
      selectionRequired: true,
      minimumSelections: 1,
      maximumSelections: 1,
      sortOrder: 0,
      options: [
        {
          id: 'option-1',
          name: 'Pollo',
          description: null,
          dietaryTags: '',
          isActive: true,
          capacity: null,
          selectionCount: 0,
          sortOrder: 0,
        },
      ],
      updatedAt: '2026-07-29T12:00:00Z',
    },
  ];
  const transport: EventTransportOptionResponse[] = [
    {
      id: 'transport-1',
      name: 'Camión recepción',
      description: null,
      direction: 'RoundTrip',
      pickupPoint: 'Hotel sede',
      departureAt: null,
      returnAt: null,
      capacity: 40,
      allowWaitlist: true,
      isActive: true,
      sortOrder: 0,
      confirmedCount: 0,
      waitlistCount: 0,
    },
  ];
  const accommodation: EventAccommodationOptionResponse[] = [
    {
      id: 'hotel-1',
      name: 'Hotel sede',
      description: null,
      address: null,
      bookingUrl: null,
      bookingCode: null,
      bookingDeadline: null,
      contactInformation: null,
      isActive: true,
      sortOrder: 0,
      interestedCount: 0,
    },
  ];
  const api = {
    getRsvpQuestionCatalog: vi.fn(() => of(catalog)),
    getEventMenus: vi.fn(() => of(menus)),
    getTransportOptions: vi.fn(() => of(transport)),
    getAccommodationOptions: vi.fn(() => of(accommodation)),
    getRsvpForm: vi.fn(() => of(publishedForm)),
    getRsvpFormVersion: vi.fn(() => of(version)),
    getRsvpDraftFormVersion: vi.fn(() => of(savedVersion)),
    createRsvpForm: vi.fn(() => of(draftForm)),
    createRsvpFormDraft: vi.fn(() => of(draftForm)),
    createRsvpFormVersion: vi.fn(() => of(savedVersion)),
    submitRsvpFormReview: vi.fn(() => of({
      ...draftForm,
      status: 'InReview',
    })),
    approveRsvpForm: vi.fn(() => of(savedVersion)),
    publishRsvpForm: vi.fn(() => of({
      ...savedVersion,
      publishedAt: '2026-07-29T13:00:00Z',
    })),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [RsvpFormEditorPage],
      providers: [
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: {
                get: (key: string) =>
                  key === 'id' ? 'event-1' : null,
              },
            },
          },
        },
        {
          provide: OrganizationContextService,
          useValue: {
            requireOrganizationId: () => 'org-1',
          },
        },
        { provide: ApiService, useValue: api },
        {
          provide: ToastService,
          useValue: {
            success: vi.fn(),
            error: vi.fn(),
          },
        },
      ],
    }).compileComponents();
    fixture = TestBed.createComponent(RsvpFormEditorPage);
    fixture.detectChanges();
  });

  it('renderiza los ocho tipos entregados por el catálogo backend', () => {
    const content = fixture.nativeElement.textContent as string;
    const cards = fixture.nativeElement.querySelectorAll(
      '.question-card',
    ) as NodeListOf<HTMLElement>;

    expect(cards).toHaveLength(8);
    expect(content).toContain('Texto corto');
    expect(content).toContain('Texto largo');
    expect(content).toContain('Sí / No');
    expect(content).toContain('Selección única');
    expect(content).toContain('Selección múltiple');
    expect(content).toContain('Número');
    expect(content).toContain('Fecha');
    expect(content).toContain('Consentimiento informativo');
  });

  it('muestra opciones, reglas compatibles y tratamiento sensible', () => {
    const content = fixture.nativeElement.textContent as string;

    expect(content).toContain('Opción A');
    expect(content).toContain('minimumSelections');
    expect(content).toContain('integerOnly');
    expect(content).toContain('La respuesta se oculta de DTO');
  });

  it('muestra los catálogos operativos que se congelarán en la versión', () => {
    const content = fixture.nativeElement.textContent as string;

    expect(api.getEventMenus).toHaveBeenCalledWith('org-1', 'event-1');
    expect(api.getTransportOptions).toHaveBeenCalledWith(
      'org-1',
      'event-1',
    );
    expect(api.getAccommodationOptions).toHaveBeenCalledWith(
      'org-1',
      'event-1',
    );
    expect(content).toContain('Menú, transporte y hospedaje');
    expect(content).toContain('1 opciones activas');
    expect(content).toContain('1 activos');
  });

  it('no bloquea el editor si un catálogo operativo falla', () => {
    api.getEventMenus.mockReturnValueOnce(
      throwError(() => new Error('menús no disponibles')),
    );

    const secondFixture = TestBed.createComponent(RsvpFormEditorPage);
    secondFixture.detectChanges();

    const content = secondFixture.nativeElement.textContent as string;
    expect(content).toContain('Preguntas');
    expect(content).toContain('0 opciones activas');
  });

  it('crea un nuevo borrador sin retirar la versión publicada y guarda el snapshot', () => {
    clickButton(fixture, 'Crear nueva versión');
    fixture.detectChanges();
    clickButton(fixture, 'Guardar versión');

    expect(api.createRsvpFormDraft).toHaveBeenCalledWith(
      'org-1',
      'event-1',
    );
    expect(api.createRsvpFormVersion).toHaveBeenCalledTimes(1);
    const call = api.createRsvpFormVersion.mock.calls[0] as unknown as
      [string, string, string, string, string, string];
    const snapshot = call[2];
    expect(typeof snapshot).toBe('string');
    expect(JSON.parse(snapshot)).toHaveLength(8);
    expect(JSON.parse(call[3])).toEqual(menus);
    expect(JSON.parse(call[4])).toEqual(transport);
    expect(JSON.parse(call[5])).toEqual(accommodation);
  });
});

function clickButton(
  fixture: ComponentFixture<RsvpFormEditorPage>,
  label: string,
): void {
  const nodeList = fixture.nativeElement.querySelectorAll(
    'button',
  ) as NodeListOf<HTMLButtonElement>;
  const buttons = Array.from(nodeList);
  const button = buttons.find((candidate) =>
    candidate.textContent?.includes(label));
  expect(button).toBeDefined();
  button?.click();
  fixture.detectChanges();
}

function createCatalog(): RsvpQuestionCatalog {
  return {
    questionTypes: [
      'ShortText',
      'LongText',
      'YesNo',
      'SingleChoice',
      'MultipleChoice',
      'Number',
      'Date',
      'InformationalConsent',
    ],
    questionScopes: [
      'InvitationGroup',
      'IndividualGuest',
      'PrimaryContact',
    ],
    questionCategories: [
      'General',
      'Dietary',
      'Transportation',
      'Accommodation',
      'Accessibility',
      'Consent',
      'Other',
    ],
    visibilityConditionTypes: [
      'Always',
      'AttendanceStatusEquals',
      'GuestAgeCategoryEquals',
      'GuestTypeEquals',
      'GroupHasTag',
      'PreviousAnswerEquals',
      'PreviousAnswerContains',
      'IsUnnamedCompanion',
      'IsPrimaryContact',
      'All',
      'Any',
    ],
    compatibleRules: {
      ShortText: ['required', 'minLength', 'maxLength'],
      LongText: ['required', 'minLength', 'maxLength'],
      YesNo: ['required'],
      SingleChoice: ['required'],
      MultipleChoice: [
        'required',
        'minimumSelections',
        'maximumSelections',
      ],
      Number: ['required', 'minimum', 'maximum', 'integerOnly'],
      Date: ['required', 'minimumDate', 'maximumDate'],
      InformationalConsent: ['required'],
    },
    maximumQuestions: 100,
    maximumQuestionLabelLength: 200,
    maximumHelpTextLength: 1000,
    maximumOptionLabelLength: 200,
    maximumShortTextLength: 500,
    maximumLongTextLength: 5000,
    maximumVisibilityDepth: 5,
    maximumVisibilityConditions: 32,
  };
}

function createQuestion(
  questionType: RsvpQuestionType,
  sortOrder: number,
): RsvpQuestion {
  const choice = questionType === 'SingleChoice'
    || questionType === 'MultipleChoice';
  const consent = questionType === 'InformationalConsent';
  return {
    id: `question-${sortOrder + 1}`,
    questionType,
    scope: 'InvitationGroup',
    category: consent ? 'Consent' : 'General',
    label: `Pregunta ${sortOrder + 1}`,
    helpText: null,
    isRequired: consent,
    isSensitive: consent,
    isActive: true,
    sortOrder,
    options: choice
      ? [
          {
            key: 'a',
            label: 'Opción A',
            isActive: true,
            sortOrder: 0,
          },
          {
            key: 'b',
            label: 'Opción B',
            isActive: true,
            sortOrder: 1,
          },
        ]
      : [],
    visibilityRule: {
      conditionType: 'Always',
      referenceQuestionId: null,
      expectedValue: null,
      conditions: [],
    },
    validationRules: questionType === 'MultipleChoice'
      ? {
          required: false,
          minimumSelections: 1,
          maximumSelections: 2,
        }
      : questionType === 'Number'
        ? {
            required: false,
            minimum: 0,
            maximum: 10,
            integerOnly: true,
          }
        : { required: consent },
  };
}
