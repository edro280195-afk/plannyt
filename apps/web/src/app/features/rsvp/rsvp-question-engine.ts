import type {
  GuestAttendanceStatus,
  RsvpQuestion,
  RsvpQuestionCatalog,
  RsvpQuestionScope,
  RsvpVisibilityRule,
} from '../../core/models/api.models';

export type RsvpDraftAnswer =
  | string
  | string[]
  | boolean
  | number
  | null;

export interface RsvpVisibilityGuest {
  responseGuestId: string;
  eventGuestId: string | null;
  displayName: string;
  ageCategory: string;
  guestType: string;
  attendanceStatus: GuestAttendanceStatus;
  isUnnamedCompanion: boolean;
  isPrimaryContact: boolean;
}

export interface RsvpVisibilityContext {
  guests: RsvpVisibilityGuest[];
  groupTags: string[];
}

export interface RsvpQuestionInstance {
  question: RsvpQuestion;
  guest: RsvpVisibilityGuest | null;
  answerKey: string;
}

export interface RsvpEditorIssue {
  questionId: string | null;
  code: string;
  message: string;
}

export const groupAnswerTarget = 'group';

export function rsvpAnswerKey(
  questionId: string,
  guestId: string | null,
): string {
  return `${questionId}::${guestId ?? groupAnswerTarget}`;
}

export function questionTargets(
  question: RsvpQuestion,
  context: RsvpVisibilityContext,
): Array<RsvpVisibilityGuest | null> {
  if (question.scope === 'InvitationGroup') return [null];
  if (question.scope === 'PrimaryContact') {
    const primary = context.guests.find((guest) => guest.isPrimaryContact);
    return primary ? [primary] : [];
  }
  return context.guests;
}

export function visibleQuestionInstances(
  questions: RsvpQuestion[],
  context: RsvpVisibilityContext,
  answers: Readonly<Record<string, RsvpDraftAnswer>>,
): RsvpQuestionInstance[] {
  const ordered = [...questions].sort(compareQuestions);
  const byId = new Map(ordered.map((question) => [question.id, question]));
  return ordered.flatMap((question) =>
    questionTargets(question, context)
      .filter((guest) =>
        isQuestionVisible(
          question,
          guest,
          context,
          byId,
          answers,
        ),
      )
      .map((guest) => ({
        question,
        guest,
        answerKey: rsvpAnswerKey(
          question.id,
          guest?.responseGuestId ?? null,
        ),
      })),
  );
}

export function clearHiddenAnswers(
  questions: RsvpQuestion[],
  context: RsvpVisibilityContext,
  answers: Readonly<Record<string, RsvpDraftAnswer>>,
): Record<string, RsvpDraftAnswer> {
  const visibleKeys = new Set(
    visibleQuestionInstances(questions, context, answers)
      .map((instance) => instance.answerKey),
  );
  return Object.fromEntries(
    Object.entries(answers)
      .filter(([key]) => visibleKeys.has(key)),
  );
}

export function serializeDraftAnswer(
  answer: RsvpDraftAnswer,
): string {
  if (typeof answer === 'number') {
    return Number.isFinite(answer) ? String(answer) : 'null';
  }
  return JSON.stringify(answer);
}

export function hasDraftAnswer(answer: RsvpDraftAnswer | undefined): boolean {
  if (answer === undefined || answer === null) return false;
  if (Array.isArray(answer)) return answer.length > 0;
  if (typeof answer === 'string') return answer.trim().length > 0;
  return true;
}

export function validateRsvpQuestions(
  questions: RsvpQuestion[],
  catalog: RsvpQuestionCatalog,
): RsvpEditorIssue[] {
  const issues: RsvpEditorIssue[] = [];
  const ids = new Set<string>();
  const orders = new Set<number>();
  const byId = new Map(questions.map((question) => [
    question.id,
    question,
  ]));

  for (const question of questions) {
    if (!question.id.trim() || ids.has(question.id)) {
      issues.push(issue(
        question.id || null,
        'duplicate_question_id',
        'Cada pregunta requiere un ID estable y único.',
      ));
    }
    ids.add(question.id);
    if (orders.has(question.sortOrder)) {
      issues.push(issue(
        question.id,
        'duplicate_sort_order',
        'El orden de las preguntas no puede repetirse.',
      ));
    }
    orders.add(question.sortOrder);
    if (!question.label.trim()
        || question.label.trim().length
          > catalog.maximumQuestionLabelLength) {
      issues.push(issue(
        question.id,
        'invalid_label',
        `La etiqueta es obligatoria y admite hasta ${catalog.maximumQuestionLabelLength} caracteres.`,
      ));
    }
    validateOptions(question, issues);
    validateRuleCompatibility(question, catalog, issues);
    validateVisibility(
      question,
      question.visibilityRule,
      byId,
      catalog,
      issues,
      1,
      { count: 0 },
    );
  }

  for (const question of questions) {
    if (hasVisibilityCycle(question.id, byId, new Set(), new Set())) {
      issues.push(issue(
        question.id,
        'visibility_cycle',
        'Las condiciones de visibilidad contienen un ciclo.',
      ));
    }
  }

  return uniqueIssues(issues);
}

export function compatibleReferenceQuestions(
  questions: RsvpQuestion[],
  current: RsvpQuestion,
): RsvpQuestion[] {
  return questions
    .filter((question) =>
      question.id !== current.id
      && question.sortOrder < current.sortOrder)
    .sort(compareQuestions);
}

function isQuestionVisible(
  question: RsvpQuestion,
  guest: RsvpVisibilityGuest | null,
  context: RsvpVisibilityContext,
  questions: ReadonlyMap<string, RsvpQuestion>,
  answers: Readonly<Record<string, RsvpDraftAnswer>>,
): boolean {
  return question.isActive
    && evaluateVisibility(
      question.visibilityRule,
      guest,
      context,
      questions,
      answers,
    );
}

function evaluateVisibility(
  rule: RsvpVisibilityRule,
  guest: RsvpVisibilityGuest | null,
  context: RsvpVisibilityContext,
  questions: ReadonlyMap<string, RsvpQuestion>,
  answers: Readonly<Record<string, RsvpDraftAnswer>>,
): boolean {
  const guests = guest ? [guest] : context.guests;
  switch (rule.conditionType) {
    case 'Always':
      return true;
    case 'All':
      return rule.conditions.every((condition) =>
        evaluateVisibility(
          condition,
          guest,
          context,
          questions,
          answers,
        ));
    case 'Any':
      return rule.conditions.some((condition) =>
        evaluateVisibility(
          condition,
          guest,
          context,
          questions,
          answers,
        ));
    case 'AttendanceStatusEquals':
      return guests.some((item) =>
        item.attendanceStatus === rule.expectedValue);
    case 'GuestAgeCategoryEquals':
      return guests.some((item) =>
        item.ageCategory === rule.expectedValue);
    case 'GuestTypeEquals':
      return guests.some((item) =>
        item.guestType === rule.expectedValue);
    case 'GroupHasTag':
      return !!rule.expectedValue
        && context.groupTags.includes(rule.expectedValue);
    case 'IsUnnamedCompanion':
      return guests.some((item) =>
        item.isUnnamedCompanion
        === parseExpectedBoolean(rule.expectedValue));
    case 'IsPrimaryContact':
      return guests.some((item) =>
        item.isPrimaryContact
        === parseExpectedBoolean(rule.expectedValue));
    case 'PreviousAnswerEquals':
      return comparePreviousAnswer(
        rule,
        guest,
        context,
        questions,
        answers,
        false,
      );
    case 'PreviousAnswerContains':
      return comparePreviousAnswer(
        rule,
        guest,
        context,
        questions,
        answers,
        true,
      );
  }
}

function comparePreviousAnswer(
  rule: RsvpVisibilityRule,
  guest: RsvpVisibilityGuest | null,
  context: RsvpVisibilityContext,
  questions: ReadonlyMap<string, RsvpQuestion>,
  answers: Readonly<Record<string, RsvpDraftAnswer>>,
  contains: boolean,
): boolean {
  if (!rule.referenceQuestionId || rule.expectedValue === null) {
    return false;
  }
  const referenced = questions.get(rule.referenceQuestionId);
  if (!referenced) return false;
  const targetId = targetForReferencedQuestion(
    referenced.scope,
    guest,
    context,
  );
  const answer = answers[
    rsvpAnswerKey(referenced.id, targetId)
  ];
  const values = comparableValues(answer);
  return contains
    ? values.includes(rule.expectedValue)
    : values.length === 1 && values[0] === rule.expectedValue;
}

function targetForReferencedQuestion(
  scope: RsvpQuestionScope,
  guest: RsvpVisibilityGuest | null,
  context: RsvpVisibilityContext,
): string | null {
  if (scope === 'InvitationGroup') return null;
  if (scope === 'PrimaryContact') {
    return context.guests.find((item) =>
      item.isPrimaryContact)?.responseGuestId ?? null;
  }
  return guest?.responseGuestId ?? null;
}

function comparableValues(
  answer: RsvpDraftAnswer | undefined,
): string[] {
  if (answer === undefined || answer === null) return [];
  if (Array.isArray(answer)) return answer;
  if (typeof answer === 'boolean') return [answer ? 'true' : 'false'];
  return [String(answer)];
}

function parseExpectedBoolean(value: string | null): boolean {
  return value === 'true';
}

function validateOptions(
  question: RsvpQuestion,
  issues: RsvpEditorIssue[],
): void {
  const isChoice = question.questionType === 'SingleChoice'
    || question.questionType === 'MultipleChoice';
  if (!isChoice && question.options.length > 0) {
    issues.push(issue(
      question.id,
      'unsupported_options',
      'Este tipo de pregunta no admite opciones.',
    ));
    return;
  }
  if (!isChoice) return;
  const keys = new Set<string>();
  const activeCount = question.options.filter((option) =>
    option.isActive).length;
  const requiredActive = question.questionType === 'SingleChoice'
    ? 2
    : 1;
  if (activeCount < requiredActive) {
    issues.push(issue(
      question.id,
      'insufficient_options',
      `La pregunta requiere al menos ${requiredActive} opciones activas.`,
    ));
  }
  for (const option of question.options) {
    if (!option.key.trim() || keys.has(option.key)) {
      issues.push(issue(
        question.id,
        'duplicate_option',
        'Las claves de opción deben ser únicas y no vacías.',
      ));
    }
    keys.add(option.key);
  }
}

function validateRuleCompatibility(
  question: RsvpQuestion,
  catalog: RsvpQuestionCatalog,
  issues: RsvpEditorIssue[],
): void {
  const allowed = new Set(
    catalog.compatibleRules[question.questionType] ?? [],
  );
  const used = Object.entries(question.validationRules)
    .filter(([, value]) => value !== null && value !== undefined)
    .map(([key]) => key);
  for (const rule of used) {
    if (!allowed.has(rule)) {
      issues.push(issue(
        question.id,
        'incompatible_rule',
        `${rule} no es compatible con ${question.questionType}.`,
      ));
    }
  }
}

function validateVisibility(
  question: RsvpQuestion,
  rule: RsvpVisibilityRule,
  questions: ReadonlyMap<string, RsvpQuestion>,
  catalog: RsvpQuestionCatalog,
  issues: RsvpEditorIssue[],
  depth: number,
  counter: { count: number },
): void {
  counter.count += 1;
  if (depth > catalog.maximumVisibilityDepth
      || counter.count > catalog.maximumVisibilityConditions) {
    issues.push(issue(
      question.id,
      'visibility_limit',
      'La condición excede los límites de profundidad o cantidad.',
    ));
    return;
  }
  if (rule.conditionType === 'All' || rule.conditionType === 'Any') {
    if (rule.conditions.length === 0) {
      issues.push(issue(
        question.id,
        'empty_composite_condition',
        'All y Any requieren al menos una condición.',
      ));
    }
    for (const child of rule.conditions) {
      validateVisibility(
        question,
        child,
        questions,
        catalog,
        issues,
        depth + 1,
        counter,
      );
    }
    return;
  }
  if (rule.conditionType === 'PreviousAnswerEquals'
      || rule.conditionType === 'PreviousAnswerContains') {
    const referenced = rule.referenceQuestionId
      ? questions.get(rule.referenceQuestionId)
      : undefined;
    if (!referenced) {
      issues.push(issue(
        question.id,
        'unknown_visibility_reference',
        'La condición referencia una pregunta inexistente.',
      ));
    } else if (referenced.sortOrder >= question.sortOrder) {
      issues.push(issue(
        question.id,
        'forward_visibility_reference',
        'Solo puedes referenciar preguntas anteriores.',
      ));
    }
  }
}

function hasVisibilityCycle(
  questionId: string,
  questions: ReadonlyMap<string, RsvpQuestion>,
  visiting: Set<string>,
  visited: Set<string>,
): boolean {
  if (visited.has(questionId)) return false;
  if (visiting.has(questionId)) return true;
  const question = questions.get(questionId);
  if (!question) return false;
  visiting.add(questionId);
  for (const reference of visibilityReferences(
    question.visibilityRule,
  )) {
    if (hasVisibilityCycle(
      reference,
      questions,
      visiting,
      visited,
    )) {
      return true;
    }
  }
  visiting.delete(questionId);
  visited.add(questionId);
  return false;
}

function visibilityReferences(rule: RsvpVisibilityRule): string[] {
  const own = (
    rule.conditionType === 'PreviousAnswerEquals'
    || rule.conditionType === 'PreviousAnswerContains'
  ) && rule.referenceQuestionId
    ? [rule.referenceQuestionId]
    : [];
  return [
    ...own,
    ...rule.conditions.flatMap(visibilityReferences),
  ];
}

function issue(
  questionId: string | null,
  code: string,
  message: string,
): RsvpEditorIssue {
  return { questionId, code, message };
}

function uniqueIssues(issues: RsvpEditorIssue[]): RsvpEditorIssue[] {
  const seen = new Set<string>();
  return issues.filter((item) => {
    const key = `${item.questionId ?? ''}:${item.code}:${item.message}`;
    if (seen.has(key)) return false;
    seen.add(key);
    return true;
  });
}

function compareQuestions(
  left: RsvpQuestion,
  right: RsvpQuestion,
): number {
  return left.sortOrder - right.sortOrder
    || left.id.localeCompare(right.id);
}
