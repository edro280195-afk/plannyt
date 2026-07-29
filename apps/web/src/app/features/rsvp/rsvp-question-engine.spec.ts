import { describe, expect, it } from 'vitest';
import type {
  RsvpQuestion,
  RsvpQuestionCatalog,
  RsvpVisibilityConditionType,
  RsvpVisibilityRule,
} from '../../core/models/api.models';
import {
  clearHiddenAnswers,
  compatibleReferenceQuestions,
  hasDraftAnswer,
  questionTargets,
  rsvpAnswerKey,
  serializeDraftAnswer,
  validateRsvpQuestions,
  visibleQuestionInstances,
} from './rsvp-question-engine';

const catalog: RsvpQuestionCatalog = {
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

const context = {
  guests: [
    {
      responseGuestId: 'primary',
      eventGuestId: 'primary',
      displayName: 'Principal',
      ageCategory: 'Adult',
      guestType: 'Family',
      attendanceStatus: 'Attending' as const,
      isUnnamedCompanion: false,
      isPrimaryContact: true,
    },
    {
      responseGuestId: 'companion',
      eventGuestId: null,
      displayName: 'Acompañante',
      ageCategory: 'Child',
      guestType: 'Other',
      attendanceStatus: 'Attending' as const,
      isUnnamedCompanion: true,
      isPrimaryContact: false,
    },
  ],
  groupTags: ['VIP'],
};

describe('motor frontend de preguntas RSVP', () => {
  it('genera una instancia grupal sin guestId', () => {
    const question = createQuestion('group');

    const targets = questionTargets(question, context);
    const instances = visibleQuestionInstances(
      [question],
      context,
      {},
    );

    expect(targets).toEqual([null]);
    expect(instances[0]?.answerKey).toBe('group::group');
  });

  it('genera una instancia por invitado para alcance individual', () => {
    const question = createQuestion('individual', {
      scope: 'IndividualGuest',
    });

    expect(
      visibleQuestionInstances([question], context, {}),
    ).toHaveLength(2);
  });

  it('limita el alcance al contacto principal', () => {
    const question = createQuestion('primary', {
      scope: 'PrimaryContact',
    });

    const instance = visibleQuestionInstances(
      [question],
      context,
      {},
    )[0];

    expect(instance?.guest?.responseGuestId).toBe('primary');
  });

  it.each([
    ['AttendanceStatusEquals', 'Attending'],
    ['GuestAgeCategoryEquals', 'Adult'],
    ['GuestTypeEquals', 'Family'],
    ['GroupHasTag', 'VIP'],
    ['IsPrimaryContact', 'true'],
  ] as Array<[RsvpVisibilityConditionType, string]>)(
    'evalúa la condición %s',
    (conditionType, expectedValue) => {
      const question = createQuestion('conditional', {
        visibilityRule: rule(conditionType, expectedValue),
      });

      expect(
        visibleQuestionInstances([question], context, {}),
      ).toHaveLength(1);
    },
  );

  it('evalúa IsUnnamedCompanion por cada invitado', () => {
    const question = createQuestion('unnamed', {
      scope: 'IndividualGuest',
      visibilityRule: rule('IsUnnamedCompanion', 'true'),
    });

    const instances = visibleQuestionInstances(
      [question],
      context,
      {},
    );

    expect(instances).toHaveLength(1);
    expect(instances[0]?.guest?.responseGuestId).toBe('companion');
  });

  it('compone condiciones All y Any', () => {
    const all = createQuestion('all', {
      visibilityRule: composite('All', [
        rule('GroupHasTag', 'VIP'),
        rule('AttendanceStatusEquals', 'Attending'),
      ]),
    });
    const any = createQuestion('any', {
      sortOrder: 1,
      visibilityRule: composite('Any', [
        rule('GroupHasTag', 'OTHER'),
        rule('AttendanceStatusEquals', 'Attending'),
      ]),
    });

    expect(
      visibleQuestionInstances([all, any], context, {}),
    ).toHaveLength(2);
  });

  it('evalúa igualdad y pertenencia de una respuesta previa', () => {
    const first = createQuestion('first', {
      questionType: 'MultipleChoice',
      options: [
        option('a', 0),
        option('b', 1),
      ],
    });
    const equals = createQuestion('equals', {
      sortOrder: 1,
      visibilityRule: previous(
        'PreviousAnswerEquals',
        'first',
        'a',
      ),
    });
    const contains = createQuestion('contains', {
      sortOrder: 2,
      visibilityRule: previous(
        'PreviousAnswerContains',
        'first',
        'b',
      ),
    });

    expect(
      visibleQuestionInstances(
        [first, equals, contains],
        context,
        {
          [rsvpAnswerKey('first', null)]: ['a', 'b'],
        },
      ).map((instance) => instance.question.id),
    ).toEqual(['first', 'contains']);
  });

  it('elimina respuestas que dejan de ser visibles', () => {
    const first = createQuestion('first', {
      questionType: 'YesNo',
    });
    const conditional = createQuestion('conditional', {
      sortOrder: 1,
      visibilityRule: previous(
        'PreviousAnswerEquals',
        'first',
        'true',
      ),
    });
    const hiddenAnswers = {
      [rsvpAnswerKey('first', null)]: false,
      [rsvpAnswerKey('conditional', null)]: 'No debe enviarse',
    };

    expect(
      clearHiddenAnswers(
        [first, conditional],
        context,
        hiddenAnswers,
      ),
    ).toEqual({
      [rsvpAnswerKey('first', null)]: false,
    });
  });

  it('serializa valores tipados sin convertir booleanos ni números a texto', () => {
    expect(serializeDraftAnswer(true)).toBe('true');
    expect(serializeDraftAnswer(2.5)).toBe('2.5');
    expect(serializeDraftAnswer(['b', 'a'])).toBe('["b","a"]');
    expect(serializeDraftAnswer('2026-10-10'))
      .toBe('"2026-10-10"');
  });

  it('distingue respuestas vacías de false y cero', () => {
    expect(hasDraftAnswer('')).toBe(false);
    expect(hasDraftAnswer([])).toBe(false);
    expect(hasDraftAnswer(false)).toBe(true);
    expect(hasDraftAnswer(0)).toBe(true);
  });

  it('detecta IDs, órdenes y opciones duplicadas', () => {
    const first = createQuestion('same', {
      questionType: 'SingleChoice',
      options: [option('a', 0), option('a', 1)],
    });
    const second = createQuestion('same', { sortOrder: 0 });

    const codes = validateRsvpQuestions(
      [first, second],
      catalog,
    ).map((item) => item.code);

    expect(codes).toContain('duplicate_question_id');
    expect(codes).toContain('duplicate_sort_order');
    expect(codes).toContain('duplicate_option');
  });

  it('detecta reglas incompatibles con el tipo', () => {
    const question = createQuestion('yes', {
      questionType: 'YesNo',
      validationRules: {
        required: false,
        minLength: 2,
      },
    });

    expect(
      validateRsvpQuestions([question], catalog),
    ).toContainEqual(expect.objectContaining({
      code: 'incompatible_rule',
    }));
  });

  it('detecta referencias posteriores y ciclos', () => {
    const first = createQuestion('first', {
      visibilityRule: previous(
        'PreviousAnswerEquals',
        'second',
        'true',
      ),
    });
    const second = createQuestion('second', {
      sortOrder: 1,
      visibilityRule: previous(
        'PreviousAnswerEquals',
        'first',
        'true',
      ),
    });
    const codes = validateRsvpQuestions(
      [first, second],
      catalog,
    ).map((item) => item.code);

    expect(codes).toContain('forward_visibility_reference');
    expect(codes).toContain('visibility_cycle');
  });

  it('detecta profundidad excesiva', () => {
    let visibility = rule('Always', null);
    for (let index = 0; index < 7; index += 1) {
      visibility = composite('All', [visibility]);
    }
    const question = createQuestion('deep', {
      visibilityRule: visibility,
    });

    expect(
      validateRsvpQuestions([question], catalog),
    ).toContainEqual(expect.objectContaining({
      code: 'visibility_limit',
    }));
  });

  it('ofrece únicamente preguntas anteriores como referencias', () => {
    const first = createQuestion('first');
    const current = createQuestion('current', { sortOrder: 1 });
    const later = createQuestion('later', { sortOrder: 2 });

    expect(
      compatibleReferenceQuestions(
        [first, current, later],
        current,
      ).map((question) => question.id),
    ).toEqual(['first']);
  });
});

function createQuestion(
  id: string,
  patch: Partial<RsvpQuestion> = {},
): RsvpQuestion {
  return {
    id,
    questionType: 'ShortText',
    scope: 'InvitationGroup',
    category: 'General',
    label: `Pregunta ${id}`,
    helpText: null,
    isRequired: false,
    isSensitive: false,
    isActive: true,
    sortOrder: 0,
    options: [],
    visibilityRule: rule('Always', null),
    validationRules: { required: false },
    ...patch,
  };
}

function option(key: string, sortOrder: number) {
  return {
    key,
    label: `Opción ${key}`,
    isActive: true,
    sortOrder,
  };
}

function rule(
  conditionType: RsvpVisibilityConditionType,
  expectedValue: string | null,
): RsvpVisibilityRule {
  return {
    conditionType,
    referenceQuestionId: null,
    expectedValue,
    conditions: [],
  };
}

function previous(
  conditionType:
    | 'PreviousAnswerEquals'
    | 'PreviousAnswerContains',
  referenceQuestionId: string,
  expectedValue: string,
): RsvpVisibilityRule {
  return {
    conditionType,
    referenceQuestionId,
    expectedValue,
    conditions: [],
  };
}

function composite(
  conditionType: 'All' | 'Any',
  conditions: RsvpVisibilityRule[],
): RsvpVisibilityRule {
  return {
    conditionType,
    referenceQuestionId: null,
    expectedValue: null,
    conditions,
  };
}
