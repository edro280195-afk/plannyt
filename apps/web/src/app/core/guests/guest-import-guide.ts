import { GuestImportTemplateFormat, GuestImportTemplateLanguage } from '../models/api.models';

export interface GuestImportFieldGuideEntry {
  label: string;
  required: boolean;
  description: string;
  validValues: string;
}

export const GUEST_IMPORT_FORMAT_OPTIONS: { value: GuestImportTemplateFormat; label: string }[] = [
  { value: 'csv', label: 'CSV' },
  { value: 'xlsx', label: 'Excel (.xlsx)' },
];

export const GUEST_IMPORT_LANGUAGE_OPTIONS: {
  value: GuestImportTemplateLanguage;
  label: string;
}[] = [
  { value: 'es', label: 'Español' },
  { value: 'en', label: 'English' },
];

const FIELD_GUIDE_ES: GuestImportFieldGuideEntry[] = [
  {
    label: 'Nombre del grupo',
    required: true,
    description: 'Identifica al grupo o familia. Las filas con el mismo nombre se agrupan.',
    validValues: 'Cualquier texto, por ejemplo "Familia García".',
  },
  {
    label: 'Tipo de grupo',
    required: true,
    description: 'Qué clase de grupo es.',
    validValues: 'Individual, Pareja, Familia, Grupo, Empresa, Mesa corporativa u Otro.',
  },
  {
    label: 'Invitados permitidos',
    required: true,
    description: 'Cuántas personas puede incluir este grupo en total.',
    validValues: 'Un número entero mayor a 0, igual en todas las filas del mismo grupo.',
  },
  {
    label: 'Nombre de contacto',
    required: false,
    description: 'Nombre de la persona de contacto del grupo.',
    validValues: 'Texto libre, opcional.',
  },
  {
    label: 'Teléfono de contacto',
    required: false,
    description: 'Teléfono de la persona de contacto.',
    validValues: 'Texto libre, opcional.',
  },
  {
    label: 'Correo de contacto',
    required: false,
    description: 'Correo de la persona de contacto.',
    validValues: 'Debe ser un correo válido si se llena; opcional.',
  },
  {
    label: 'Nombre del invitado',
    required: true,
    description: 'Nombre de pila de esta persona.',
    validValues: 'Texto libre. Debe llenarse el nombre o el apellido.',
  },
  {
    label: 'Apellido del invitado',
    required: true,
    description: 'Apellido de esta persona.',
    validValues: 'Texto libre. Debe llenarse el nombre o el apellido.',
  },
  {
    label: 'Categoría de edad',
    required: true,
    description: 'Rango de edad de esta persona.',
    validValues: 'Adulto, Adolescente, Niño, Bebé o Sin especificar.',
  },
  {
    label: 'Contacto principal',
    required: false,
    description: 'Si esta persona es el contacto principal del grupo. Solo una por grupo.',
    validValues: 'Sí o No (vacío se toma como No).',
  },
  {
    label: 'VIP',
    required: false,
    description: 'Si esta persona debe marcarse como invitado VIP.',
    validValues: 'Sí o No (vacío se toma como No).',
  },
  {
    label: 'Etiquetas',
    required: false,
    description: 'Etiquetas libres para organizar invitados, separadas por "|".',
    validValues: 'Texto libre separado por "|", por ejemplo "Familia|VIP".',
  },
];

const FIELD_GUIDE_EN: GuestImportFieldGuideEntry[] = [
  {
    label: 'Group name',
    required: true,
    description: 'Identifies the group or family. Rows sharing the same name are grouped together.',
    validValues: 'Any text, e.g. "García Family".',
  },
  {
    label: 'Group type',
    required: true,
    description: 'What kind of group this is.',
    validValues: 'Individual, Couple, Family, Group, Company, Corporate table or Other.',
  },
  {
    label: 'Allowed guests',
    required: true,
    description: 'How many people this group may include in total.',
    validValues: 'A whole number greater than 0, the same on every row of the group.',
  },
  {
    label: 'Contact name',
    required: false,
    description: "Name of the group's contact person.",
    validValues: 'Free text, optional.',
  },
  {
    label: 'Contact phone',
    required: false,
    description: 'Phone number of the contact person.',
    validValues: 'Free text, optional.',
  },
  {
    label: 'Contact email',
    required: false,
    description: 'Email address of the contact person.',
    validValues: 'Must be a valid email if filled in; optional.',
  },
  {
    label: 'Guest first name',
    required: true,
    description: "This person's first name.",
    validValues: 'Free text. Either the first or last name must be filled in.',
  },
  {
    label: 'Guest last name',
    required: true,
    description: "This person's last name.",
    validValues: 'Free text. Either the first or last name must be filled in.',
  },
  {
    label: 'Age category',
    required: true,
    description: "This person's age range.",
    validValues: 'Adult, Teen, Child, Infant or Unknown.',
  },
  {
    label: 'Primary contact',
    required: false,
    description: "Whether this person is the group's primary contact. Only one per group.",
    validValues: 'Yes or No (blank is treated as No).',
  },
  {
    label: 'VIP',
    required: false,
    description: 'Whether this person should be marked as a VIP guest.',
    validValues: 'Yes or No (blank is treated as No).',
  },
  {
    label: 'Tags',
    required: false,
    description: 'Free-form tags to organize guests, separated by "|".',
    validValues: 'Free text separated by "|", e.g. "Family|VIP".',
  },
];

export function guestImportFieldGuide(
  language: GuestImportTemplateLanguage,
): GuestImportFieldGuideEntry[] {
  return language === 'en' ? FIELD_GUIDE_EN : FIELD_GUIDE_ES;
}

export function templateFileName(
  format: GuestImportTemplateFormat,
  language: GuestImportTemplateLanguage,
): string {
  const base = language === 'en' ? 'guest-import-template' : 'plantilla-invitados';
  return `${base}.${format}`;
}
