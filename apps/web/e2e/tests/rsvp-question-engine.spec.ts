import { Page, Route } from '@playwright/test';
import { expect, test } from '../fixtures/plannyt.fixture';

test.describe('Sprint 2B.3 · motor de preguntas RSVP', () => {
  test.describe.configure({ timeout: 60_000 });

  test('1. exige una pregunta corta requerida', async ({ page }) => {
    const recorder = await installQuestionRsvpMock(page, [
      question('short', 'ShortText', {
        label: 'Nombre para gafete',
        isRequired: true,
      }),
    ]);
    await openQuestionStep(page, 'short-required');
    const next = page.getByRole('button', { name: 'Siguiente' });

    await expect(next).toBeDisabled();
    await page.locator('.question-instance input[type="text"]')
      .fill('  Elena  ');
    await expect(next).toBeEnabled();
    await next.click();
    await page.getByRole('button', { name: 'Enviar respuesta' })
      .click();

    await expect(
      page.getByRole('heading', { name: '¡Respuesta registrada!' }),
    ).toBeVisible();
    expect(recorder.bodies[0]?.answers[0]?.answerValue)
      .toBe('"  Elena  "');
  });

  test('2. muestra el error backend para un número fuera de rango', async ({
    page,
  }) => {
    await installQuestionRsvpMock(
      page,
      [
        question('age', 'Number', {
          label: 'Cantidad',
          isRequired: true,
          validationRules: {
            required: true,
            minimum: 1,
            maximum: 5,
          },
        }),
      ],
      (body) => {
        const value = Number(
          body.answers.find((answer) =>
            answer.questionId === 'age')?.answerValue,
        );
        return value > 5
          ? [validationError(
              'age',
              null,
              'value_above_maximum',
              'La respuesta no puede ser mayor que 5.',
            )]
          : [];
      },
    );
    await openQuestionStep(page, 'number-range');
    await page.locator('.question-instance input[type="number"]')
      .fill('11');
    await submitFromQuestionStep(page);

    await expect(
      page.locator('.question-instance [role="alert"]').filter({
        hasText: 'La respuesta no puede ser mayor que 5.',
      }),
    ).toBeVisible();
  });

  test('3. rechaza una selección inexistente enviada mediante request alterado', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(
      page,
      [
        question('meal', 'SingleChoice', {
          options: [option('a', 'A', 0), option('b', 'B', 1)],
        }),
      ],
      (body) => body.answers.some((answer) =>
        answer.answerValue === '"hacked"')
        ? [validationError(
            'meal',
            null,
            'invalid_option',
            'La opción seleccionada no está disponible.',
          )]
        : [],
    );
    await page.goto('/rsvp/altered-option');

    const status = await alteredSubmission(page, {
      questionId: 'meal',
      guestId: null,
      answerValue: '"hacked"',
      displayValue: 'Hack',
    });

    expect(status).toBe(400);
    expect(recorder.persisted).toBe(0);
  });

  test('4. rechaza una pregunta individual para invitado ajeno', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(
      page,
      [
        question('individual', 'ShortText', {
          scope: 'IndividualGuest',
        }),
      ],
      (body) => body.answers.some((answer) =>
        answer.guestId === 'foreign-guest')
        ? [validationError(
            'individual',
            'foreign-guest',
            'guest_not_in_group',
            'El invitado no pertenece al grupo.',
          )]
        : [],
    );
    await page.goto('/rsvp/foreign-guest');

    const status = await alteredSubmission(page, {
      questionId: 'individual',
      guestId: 'foreign-guest',
      answerValue: '"Ajeno"',
      displayValue: 'Ajeno',
    });

    expect(status).toBe(400);
    expect(recorder.persisted).toBe(0);
  });

  test('5. muestra una pregunta condicional al cumplirse la respuesta previa', async ({
    page,
  }) => {
    await installQuestionRsvpMock(page, conditionalQuestions());
    await openQuestionStep(page, 'conditional-visible');

    await page.locator('.question-instance select')
      .selectOption('true');

    await expect(
      page.getByText('Explica tu respuesta'),
    ).toBeVisible();
  });

  test('6. mantiene oculta una pregunta condicional no aplicable', async ({
    page,
  }) => {
    await installQuestionRsvpMock(page, conditionalQuestions());
    await openQuestionStep(page, 'conditional-hidden');

    await page.locator('.question-instance select')
      .selectOption('false');

    await expect(
      page.getByText('Explica tu respuesta'),
    ).toHaveCount(0);
  });

  test('7. rechaza una respuesta oculta enviada maliciosamente', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(
      page,
      conditionalQuestions(),
      (body) => body.answers.some((answer) =>
        answer.questionId === 'detail')
        ? [validationError(
            'detail',
            null,
            'hidden_question_answered',
            'No se admite una respuesta para una pregunta oculta.',
          )]
        : [],
    );
    await page.goto('/rsvp/hidden-malicious');

    const status = await alteredSubmission(page, {
      questionId: 'detail',
      guestId: null,
      answerValue: '"Contenido oculto"',
      displayValue: 'Contenido oculto',
    });

    expect(status).toBe(400);
    expect(recorder.persisted).toBe(0);
  });

  test('8. muestra el error de MultipleChoice con demasiadas opciones', async ({
    page,
  }) => {
    await installQuestionRsvpMock(
      page,
      [
        question('activities', 'MultipleChoice', {
          label: 'Actividades',
          isRequired: true,
          options: [
            option('a', 'A', 0),
            option('b', 'B', 1),
            option('c', 'C', 2),
          ],
          validationRules: {
            required: true,
            maximumSelections: 2,
          },
        }),
      ],
      (body) => {
        const raw = body.answers.find((answer) =>
          answer.questionId === 'activities')?.answerValue ?? '[]';
        const selected = JSON.parse(raw) as string[];
        return selected.length > 2
          ? [validationError(
              'activities',
              null,
              'too_many_selections',
              'Selecciona como máximo 2 opciones.',
            )]
          : [];
      },
    );
    await openQuestionStep(page, 'multiple-limit');
    const checks = page.locator(
      '.question-instance input[type="checkbox"]',
    );
    await checks.nth(0).check();
    await checks.nth(1).check();
    await checks.nth(2).check();
    await submitFromQuestionStep(page);

    await expect(
      page.locator('.question-instance [role="alert"]').filter({
        hasText: 'Selecciona como máximo 2 opciones.',
      }),
    ).toBeVisible();
  });

  test('9. exige confirmación explícita del consentimiento', async ({
    page,
  }) => {
    await installQuestionRsvpMock(page, [
      question('consent', 'InformationalConsent', {
        label: 'Autorizo el tratamiento',
        category: 'Consent',
        isRequired: true,
        isSensitive: true,
      }),
    ]);
    await openQuestionStep(page, 'required-consent');
    const next = page.getByRole('button', { name: 'Siguiente' });

    await expect(next).toBeDisabled();
    await page.locator('.question-instance input[type="checkbox"]')
      .check();
    await expect(next).toBeEnabled();
  });

  test('10. identifica la pregunta sensible sin persistirla en storage', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(page, [
      question('allergy', 'LongText', {
        label: 'Alergias',
        category: 'Dietary',
        isSensitive: true,
      }),
    ]);
    await openQuestionStep(page, 'sensitive-question');

    await expect(
      page.getByText(/tratamiento restringido/),
    ).toBeVisible();
    await page.locator('.question-instance textarea')
      .fill('Nueces');
    await submitFromQuestionStep(page);

    expect(recorder.bodies[0]?.answers[0]?.answerValue)
      .toBe('"Nueces"');
    const stored = await page.evaluate(() => [
      ...Object.values(localStorage),
      ...Object.values(sessionStorage),
    ].join('|'));
    expect(stored).not.toContain('Nueces');
  });

  test('11. envía exclusivamente la nueva versión presentada', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(
      page,
      [
        question('new-question', 'ShortText', {
          label: 'Pregunta de versión 2',
          isRequired: true,
        }),
      ],
      undefined,
      'version-2',
    );
    await openQuestionStep(page, 'new-version');
    await page.locator('.question-instance input').fill('Respuesta');
    await submitFromQuestionStep(page);

    expect(recorder.bodies[0]?.rsvpFormVersionId).toBe('version-2');
    expect(recorder.bodies[0]?.answers[0]?.questionId)
      .toBe('new-question');
  });

  test('12. conserva la versión anterior en una edición histórica', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(
      page,
      [
        question('historic', 'ShortText', {
          label: 'Pregunta histórica',
          isRequired: true,
        }),
      ],
      undefined,
      'version-1',
      1,
    );
    await openQuestionStep(page, 'historic-version');
    await page.locator('.question-instance input')
      .fill('Corrección histórica');
    await submitFromQuestionStep(page);

    expect(recorder.bodies[0]?.rsvpFormVersionId).toBe('version-1');
    expect(recorder.bodies[0]?.expectedRevision).toBe(1);
  });

  test('13. una solicitud maliciosa no crea entrega parcial', async ({
    page,
  }) => {
    const recorder = await installQuestionRsvpMock(
      page,
      [question('known', 'ShortText')],
      (body) => body.answers.some((answer) =>
        answer.questionId === 'unknown')
        ? [validationError(
            'unknown',
            null,
            'unknown_question',
            'La pregunta no pertenece a la versión presentada.',
          )]
        : [],
    );
    await page.goto('/rsvp/no-partial');

    const status = await alteredSubmission(page, {
      questionId: 'unknown',
      guestId: null,
      answerValue: '"Ataque"',
      displayValue: 'Ataque',
    });

    expect(status).toBe(400);
    expect(recorder.persisted).toBe(0);
    expect(recorder.bodies).toHaveLength(1);
  });
});

interface QuestionDefinition {
  id: string;
  questionType: string;
  scope: string;
  category: string;
  label: string;
  helpText: string | null;
  isRequired: boolean;
  isSensitive: boolean;
  isActive: boolean;
  sortOrder: number;
  options: QuestionOption[];
  visibilityRule: VisibilityRule;
  validationRules: Record<string, string | number | boolean | null>;
}

interface QuestionOption {
  key: string;
  label: string;
  isActive: boolean;
  sortOrder: number;
}

interface VisibilityRule {
  conditionType: string;
  referenceQuestionId: string | null;
  expectedValue: string | null;
  conditions: VisibilityRule[];
}

interface SubmittedAnswer {
  questionId: string;
  guestId: string | null;
  answerValue: string;
  displayValue: string | null;
}

interface SubmissionBody {
  rsvpFormVersionId: string;
  expectedRevision: number;
  answers: SubmittedAnswer[];
}

interface ValidationItem {
  questionId: string | null;
  guestId: string | null;
  code: string;
  message: string;
}

interface Recorder {
  bodies: SubmissionBody[];
  persisted: number;
}

async function installQuestionRsvpMock(
  page: Page,
  questions: QuestionDefinition[],
  validate: (
    (body: SubmissionBody) => ValidationItem[]
  ) | undefined = undefined,
  versionId = 'version-1',
  revision = 0,
): Promise<Recorder> {
  const recorder: Recorder = {
    bodies: [],
    persisted: 0,
  };
  await page.route(
    'https://localhost:7139/api/guest/rsvp/**',
    async (route) => {
      if (route.request().method() === 'GET') {
        await json(
          route,
          rsvpState(questions, versionId, revision),
        );
        return;
      }
      const body = route.request().postDataJSON() as SubmissionBody;
      recorder.bodies.push(body);
      const errors = validate?.(body) ?? [];
      if (errors.length > 0) {
        await route.fulfill({
          status: 400,
          contentType: 'application/problem+json',
          body: JSON.stringify({
            type: 'https://plannyt.com/problems/rsvp-validation',
            title: 'La respuesta RSVP contiene errores',
            status: 400,
            detail: 'Corrige las respuestas indicadas.',
            errors,
          }),
        });
        return;
      }
      recorder.persisted += 1;
      await json(route, submissionResponse(revision + 1));
    },
  );
  return recorder;
}

async function openQuestionStep(
  page: Page,
  token: string,
): Promise<void> {
  await page.goto(`/rsvp/${token}`);
  await expect(
    page.getByRole('heading', { name: 'Familia Luna' }),
  ).toBeVisible();
  await page.getByRole('button', { name: 'Siguiente' }).click();
  await page.locator('.attendance-list select')
    .selectOption('Attending');
  for (let step = 0; step < 6; step += 1) {
    await page.getByRole('button', { name: 'Siguiente' }).click();
  }
  await expect(
    page.getByRole('heading', { name: 'Preguntas adicionales' }),
  ).toBeVisible();
}

async function submitFromQuestionStep(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Siguiente' }).click();
  const response = page.waitForResponse((candidate) =>
    candidate.url().includes('/api/guest/rsvp/')
    && candidate.url().endsWith('/submit'));
  await page.getByRole('button', { name: 'Enviar respuesta' }).click();
  await response;
}

async function alteredSubmission(
  page: Page,
  answer: SubmittedAnswer,
): Promise<number> {
  return page.evaluate(async (submittedAnswer) => {
    const response = await fetch(
      'https://localhost:7139/api/guest/rsvp/altered/submit',
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Idempotency-Key': crypto.randomUUID(),
        },
        body: JSON.stringify({
          rsvpFormVersionId: 'version-1',
          expectedRevision: 0,
          overallStatus: 'Confirmed',
          contactName: 'Familia Luna',
          contactEmail: null,
          contactPhone: null,
          guests: [
            {
              responseGuestId: 'guest-1',
              eventGuestId: 'guest-1',
              displayName: 'Elena Luna',
              ageCategory: 'Adult',
              attendanceStatus: 'Attending',
              menuSelectionsJson: '{}',
              transportSelectionJson: '{}',
              accommodationSelectionJson: '{}',
              dietaryJson: '{}',
              isUnnamedCompanion: false,
            },
          ],
          answers: [submittedAnswer],
          consentSnapshot: null,
        }),
      },
    );
    return response.status;
  }, answer);
}

function rsvpState(
  questions: QuestionDefinition[],
  versionId: string,
  revision: number,
): object {
  return {
    groupId: 'group-1',
    groupName: 'Familia Luna',
    allowedGuestCount: 1,
    maxUnnamedCompanions: 0,
    allowUnnamedCompanions: false,
    canRespond: true,
    canModify: true,
    closedMessage: null,
    settings: {
      id: 'settings-1',
      status: 'Open',
      opensAt: null,
      closesAt: null,
      timeZone: 'America/Matamoros',
      allowChangesAfterSubmission: true,
      changesCloseAt: null,
      allowTentativeResponse: false,
      allowGroupDecline: true,
      requireResponseForEveryNamedGuest: true,
      requireCompanionNames: false,
      allowContactInformationUpdate: true,
      showAttendanceSummaryAfterSubmission: true,
      confirmationTitle: null,
      confirmationMessage: null,
      declineMessage: null,
      closedMessage: null,
      privacyNotice: null,
      sensitiveDataConsentText: null,
      updatedAt: '2026-07-29T12:00:00Z',
    },
    activeForm: {
      id: versionId,
      rsvpFormId: 'form-1',
      versionNumber: versionId === 'version-2' ? 2 : 1,
      settingsSnapshot: '{}',
      questionsSnapshot: JSON.stringify(questions),
      menuSnapshot: '[]',
      transportSnapshot: '[]',
      accommodationSnapshot: '[]',
      createdAt: '2026-07-29T12:00:00Z',
      approvedBy: 'user-1',
      approvedAt: '2026-07-29T12:00:00Z',
      publishedAt: '2026-07-29T12:00:00Z',
    },
    currentResponse: null,
    revisionVersion: revision,
    guests: [
      {
        eventGuestId: 'guest-1',
        displayName: 'Elena Luna',
        ageCategory: 'Adult',
        guestType: 'Family',
        isPrimaryContact: true,
      },
    ],
    groupTags: ['VIP'],
  };
}

function submissionResponse(revision: number): object {
  return {
    id: `submission-${revision}`,
    invitationGroupId: 'group-1',
    revisionNumber: revision,
    source: 'GuestPrivateLink',
    overallStatus: 'Confirmed',
    submittedAt: '2026-07-29T12:00:00Z',
    contactNameSnapshot: 'Familia Luna',
    contactEmailSnapshot: null,
    contactPhoneSnapshot: null,
    confirmationCode: `RSVP-${revision}`,
    guests: [
      {
        responseGuestId: 'guest-1',
        eventGuestId: 'guest-1',
        displayName: 'Elena Luna',
        ageCategory: 'Adult',
        attendanceStatus: 'Attending',
        menuSelectionsJson: '{}',
        transportSelectionJson: '{}',
        accommodationSelectionJson: '{}',
        dietaryJson: '{}',
        isUnnamedCompanion: false,
      },
    ],
    answers: [],
  };
}

function conditionalQuestions(): QuestionDefinition[] {
  return [
    question('attending-party', 'YesNo', {
      label: '¿Participarás?',
      sortOrder: 0,
    }),
    question('detail', 'ShortText', {
      label: 'Explica tu respuesta',
      sortOrder: 1,
      visibilityRule: {
        conditionType: 'PreviousAnswerEquals',
        referenceQuestionId: 'attending-party',
        expectedValue: 'true',
        conditions: [],
      },
    }),
  ];
}

function question(
  id: string,
  questionType: string,
  patch: Partial<QuestionDefinition> = {},
): QuestionDefinition {
  return {
    id,
    questionType,
    scope: 'InvitationGroup',
    category: 'General',
    label: id,
    helpText: null,
    isRequired: false,
    isSensitive: false,
    isActive: true,
    sortOrder: 0,
    options: [],
    visibilityRule: {
      conditionType: 'Always',
      referenceQuestionId: null,
      expectedValue: null,
      conditions: [],
    },
    validationRules: { required: false },
    ...patch,
  };
}

function option(
  key: string,
  label: string,
  sortOrder: number,
): QuestionOption {
  return {
    key,
    label,
    isActive: true,
    sortOrder,
  };
}

function validationError(
  questionId: string | null,
  guestId: string | null,
  code: string,
  message: string,
): ValidationItem {
  return { questionId, guestId, code, message };
}

async function json(
  route: Route,
  value: object,
): Promise<void> {
  await route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify(value),
  });
}
