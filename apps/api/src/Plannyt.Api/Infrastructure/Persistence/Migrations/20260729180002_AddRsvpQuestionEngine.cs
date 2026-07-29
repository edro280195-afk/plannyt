using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Plannyt.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRsvpQuestionEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "response_guest_id",
                table: "rsvp_submission_guests",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "guest_display_name_snapshot",
                table: "rsvp_submission_answers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_sensitive",
                table: "rsvp_submission_answers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "option_labels_snapshot",
                table: "rsvp_submission_answers",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "question_label_snapshot",
                table: "rsvp_submission_answers",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "question_type_snapshot",
                table: "rsvp_submission_answers",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "ShortText");

            migrationBuilder.Sql(
                """
                UPDATE rsvp_form_versions AS version
                SET questions_snapshot = normalized.questions
                FROM (
                    SELECT
                        source.id,
                        jsonb_agg(
                            question.value
                            || jsonb_build_object(
                                'isSensitive',
                                COALESCE(
                                    (
                                        question.value
                                        ->> 'isSensitive'
                                    )::boolean,
                                    question.value
                                        ->> 'questionType'
                                        = 'InformationalConsent'
                                    OR (
                                        question.value
                                            ->> 'category'
                                            IN (
                                                'Dietary',
                                                'Accessibility'
                                            )
                                        AND question.value
                                            ->> 'questionType'
                                            IN (
                                                'ShortText',
                                                'LongText'
                                            )
                                    )
                                ),
                                'options',
                                COALESCE(
                                    (
                                        SELECT jsonb_agg(
                                            CASE
                                                WHEN jsonb_typeof(
                                                    option.value)
                                                    = 'string'
                                                THEN jsonb_build_object(
                                                    'key',
                                                    option.value #>> '{}',
                                                    'label',
                                                    option.value #>> '{}',
                                                    'isActive',
                                                    true,
                                                    'sortOrder',
                                                    option.ordinality - 1)
                                                ELSE option.value
                                            END
                                            ORDER BY option.ordinality)
                                        FROM jsonb_array_elements(
                                            COALESCE(
                                                question.value
                                                    -> 'options',
                                                '[]'::jsonb))
                                            WITH ORDINALITY
                                            AS option(
                                                value,
                                                ordinality)
                                    ),
                                    '[]'::jsonb
                                ),
                                'visibilityRule',
                                CASE
                                    WHEN question.value
                                        -> 'visibilityRule'
                                        IS NULL
                                    THEN jsonb_build_object(
                                        'conditionType',
                                        'Always',
                                        'referenceQuestionId',
                                        NULL,
                                        'expectedValue',
                                        NULL,
                                        'conditions',
                                        '[]'::jsonb)
                                    WHEN question.value
                                        -> 'visibilityRule'
                                        ? 'conditionType'
                                    THEN question.value
                                        -> 'visibilityRule'
                                    ELSE jsonb_build_object(
                                        'conditionType',
                                        'PreviousAnswerEquals',
                                        'referenceQuestionId',
                                        question.value
                                            -> 'visibilityRule'
                                            ->> 'dependsOnQuestionId',
                                        'expectedValue',
                                        question.value
                                            -> 'visibilityRule'
                                            ->> 'expectedValue',
                                        'conditions',
                                        '[]'::jsonb)
                                END,
                                'validationRules',
                                (
                                    COALESCE(
                                        question.value
                                            -> 'validationRules',
                                        '{}'::jsonb)
                                    - 'allowedOptions'
                                )
                                || jsonb_build_object(
                                    'required',
                                    COALESCE(
                                        (
                                            question.value
                                            ->> 'isRequired'
                                        )::boolean,
                                        false))
                            )
                            ORDER BY question.ordinality
                        ) AS questions
                    FROM rsvp_form_versions AS source
                    CROSS JOIN LATERAL jsonb_array_elements(
                        source.questions_snapshot)
                        WITH ORDINALITY
                        AS question(value, ordinality)
                    GROUP BY source.id
                ) AS normalized
                WHERE version.id = normalized.id;

                UPDATE rsvp_submission_guests
                SET response_guest_id = COALESCE(event_guest_id, id)
                WHERE response_guest_id IS NULL;

                UPDATE rsvp_submission_answers AS answer
                SET question_label_snapshot =
                        COALESCE(question.value ->> 'label', answer.question_id),
                    question_type_snapshot =
                        COALESCE(
                            question.value ->> 'questionType',
                            'ShortText'),
                    option_labels_snapshot =
                        CASE
                            WHEN question.value
                                ->> 'questionType'
                                = 'SingleChoice'
                            THEN COALESCE(
                                (
                                    SELECT jsonb_agg(
                                        jsonb_build_object(
                                            'Key',
                                            option.value ->> 'key',
                                            'Label',
                                            option.value ->> 'label')
                                        ORDER BY option.ordinality)
                                    FROM jsonb_array_elements(
                                        COALESCE(
                                            question.value -> 'options',
                                            '[]'::jsonb))
                                        WITH ORDINALITY
                                        AS option(value, ordinality)
                                    WHERE option.value ->> 'key'
                                        = answer.answer_value #>> '{}'
                                ),
                                '[]'::jsonb)
                            WHEN question.value
                                ->> 'questionType'
                                = 'MultipleChoice'
                            THEN COALESCE(
                                (
                                    SELECT jsonb_agg(
                                        jsonb_build_object(
                                            'Key',
                                            option.value ->> 'key',
                                            'Label',
                                            option.value ->> 'label')
                                        ORDER BY option.ordinality)
                                    FROM jsonb_array_elements(
                                        COALESCE(
                                            question.value -> 'options',
                                            '[]'::jsonb))
                                        WITH ORDINALITY
                                        AS option(value, ordinality)
                                    WHERE answer.answer_value
                                        ? (option.value ->> 'key')
                                ),
                                '[]'::jsonb)
                            ELSE '[]'::jsonb
                        END,
                    guest_display_name_snapshot =
                        (
                            SELECT guest.display_name
                            FROM rsvp_submission_guests AS guest
                            WHERE guest.rsvp_submission_id
                                = answer.rsvp_submission_id
                              AND guest.response_guest_id
                                = answer.guest_id
                            LIMIT 1
                        ),
                    is_sensitive =
                        COALESCE(
                            (question.value ->> 'isSensitive')::boolean,
                            false)
                FROM rsvp_submissions AS submission
                INNER JOIN rsvp_form_versions AS version
                    ON version.id = submission.rsvp_form_version_id
                CROSS JOIN LATERAL jsonb_array_elements(
                    version.questions_snapshot::jsonb) AS question(value)
                WHERE answer.rsvp_submission_id = submission.id
                  AND question.value ->> 'id' = answer.question_id;

                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                        FROM rsvp_submission_answers
                        WHERE guest_id IS NULL
                        GROUP BY rsvp_submission_id, question_id
                        HAVING COUNT(*) > 1
                    ) THEN
                        RAISE EXCEPTION
                            'No puede aplicarse AddRsvpQuestionEngine: existen respuestas de grupo duplicadas.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "response_guest_id",
                table: "rsvp_submission_guests",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "ak_rsvp_submission_guests_rsvp_submission_id_response_guest_id",
                table: "rsvp_submission_guests",
                columns: new[] { "rsvp_submission_id", "response_guest_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submission_answers_rsvp_submission_id_guest_id",
                table: "rsvp_submission_answers",
                columns: new[] { "rsvp_submission_id", "guest_id" });

            migrationBuilder.CreateIndex(
                name: "ix_rsvp_submission_answers_rsvp_submission_id_question_id",
                table: "rsvp_submission_answers",
                columns: new[] { "rsvp_submission_id", "question_id" },
                unique: true,
                filter: "guest_id IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_rsvp_submission_answers_rsvp_submission_guests_rsvp_submiss",
                table: "rsvp_submission_answers",
                columns: new[] { "rsvp_submission_id", "guest_id" },
                principalTable: "rsvp_submission_guests",
                principalColumns: new[] { "rsvp_submission_id", "response_guest_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE rsvp_form_versions AS version
                SET questions_snapshot = legacy.questions
                FROM (
                    SELECT
                        source.id,
                        jsonb_agg(
                            (
                                question.value
                                - 'isSensitive'
                                - 'visibilityRule'
                                - 'validationRules'
                                - 'options'
                            )
                            || jsonb_build_object(
                                'options',
                                COALESCE(
                                    (
                                        SELECT jsonb_agg(
                                            CASE
                                                WHEN jsonb_typeof(
                                                    option.value)
                                                    = 'object'
                                                THEN to_jsonb(
                                                    option.value
                                                        ->> 'key')
                                                ELSE option.value
                                            END
                                            ORDER BY option.ordinality)
                                        FROM jsonb_array_elements(
                                            COALESCE(
                                                question.value
                                                    -> 'options',
                                                '[]'::jsonb))
                                            WITH ORDINALITY
                                            AS option(
                                                value,
                                                ordinality)
                                    ),
                                    '[]'::jsonb
                                ),
                                'visibilityRule',
                                CASE
                                    WHEN question.value
                                        -> 'visibilityRule'
                                        ->> 'conditionType'
                                        = 'PreviousAnswerEquals'
                                    THEN jsonb_build_object(
                                        'dependsOnQuestionId',
                                        question.value
                                            -> 'visibilityRule'
                                            ->> 'referenceQuestionId',
                                        'expectedValue',
                                        question.value
                                            -> 'visibilityRule'
                                            ->> 'expectedValue')
                                    ELSE NULL
                                END,
                                'validationRules',
                                jsonb_build_object(
                                    'minLength',
                                    question.value
                                        -> 'validationRules'
                                        -> 'minLength',
                                    'maxLength',
                                    question.value
                                        -> 'validationRules'
                                        -> 'maxLength',
                                    'minimum',
                                    question.value
                                        -> 'validationRules'
                                        -> 'minimum',
                                    'maximum',
                                    question.value
                                        -> 'validationRules'
                                        -> 'maximum',
                                    'required',
                                    COALESCE(
                                        (
                                            question.value
                                            ->> 'isRequired'
                                        )::boolean,
                                        false),
                                    'allowedOptions',
                                    COALESCE(
                                        (
                                            SELECT jsonb_agg(
                                                option.value
                                                    ->> 'key'
                                                ORDER BY
                                                    option.ordinality)
                                            FROM jsonb_array_elements(
                                                COALESCE(
                                                    question.value
                                                        -> 'options',
                                                    '[]'::jsonb))
                                                WITH ORDINALITY
                                                AS option(
                                                    value,
                                                    ordinality)
                                        ),
                                        '[]'::jsonb))
                            )
                            ORDER BY question.ordinality
                        ) AS questions
                    FROM rsvp_form_versions AS source
                    CROSS JOIN LATERAL jsonb_array_elements(
                        source.questions_snapshot)
                        WITH ORDINALITY
                        AS question(value, ordinality)
                    GROUP BY source.id
                ) AS legacy
                WHERE version.id = legacy.id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_rsvp_submission_answers_rsvp_submission_guests_rsvp_submiss",
                table: "rsvp_submission_answers");

            migrationBuilder.DropUniqueConstraint(
                name: "ak_rsvp_submission_guests_rsvp_submission_id_response_guest_id",
                table: "rsvp_submission_guests");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submission_answers_rsvp_submission_id_guest_id",
                table: "rsvp_submission_answers");

            migrationBuilder.DropIndex(
                name: "ix_rsvp_submission_answers_rsvp_submission_id_question_id",
                table: "rsvp_submission_answers");

            migrationBuilder.DropColumn(
                name: "response_guest_id",
                table: "rsvp_submission_guests");

            migrationBuilder.DropColumn(
                name: "guest_display_name_snapshot",
                table: "rsvp_submission_answers");

            migrationBuilder.DropColumn(
                name: "is_sensitive",
                table: "rsvp_submission_answers");

            migrationBuilder.DropColumn(
                name: "option_labels_snapshot",
                table: "rsvp_submission_answers");

            migrationBuilder.DropColumn(
                name: "question_label_snapshot",
                table: "rsvp_submission_answers");

            migrationBuilder.DropColumn(
                name: "question_type_snapshot",
                table: "rsvp_submission_answers");
        }
    }
}
