using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Plannyt.Api.BuildingBlocks.Errors;

namespace Plannyt.Api.Modules.Rsvp.Application;

public static partial class RsvpRequestFingerprint
{
    public const int MaximumIdempotencyKeyLength = 128;

    public static string ValidateIdempotencyKey(string? value)
    {
        var key = value?.Trim();
        if (string.IsNullOrEmpty(key)
            || key.Length > MaximumIdempotencyKeyLength
            || !IdempotencyKeyPattern().IsMatch(key))
        {
            throw new RequestValidationException(
                new Dictionary<string, string[]>
                {
                    ["Idempotency-Key"] =
                    [
                        "Envía una llave de 16 a 128 caracteres usando solo letras, números, punto, guion, guion bajo o dos puntos."
                    ]
                });
        }

        return key;
    }

    public static string Compute(
        RsvpSubmissionRequest request,
        string operationScope)
    {
        var normalizedGuests = request.Guests
            .Select((guest, index) => new
            {
                SortKey = guest.EventGuestId?.ToString("N")
                          ?? $"companion:{index:D4}",
                EventGuestId = guest.EventGuestId,
                DisplayName = Normalize(guest.DisplayName),
                AgeCategory = Normalize(guest.AgeCategory),
                AttendanceStatus = guest.AttendanceStatus.ToString(),
                Menu = CanonicalizeJson(guest.MenuSelectionsJson),
                Transport = CanonicalizeJson(guest.TransportSelectionJson),
                Accommodation = CanonicalizeJson(
                    guest.AccommodationSelectionJson),
                Dietary = CanonicalizeJson(guest.DietaryJson),
                guest.IsUnnamedCompanion
            })
            .OrderBy(guest => guest.SortKey, StringComparer.Ordinal)
            .ToList();
        var normalizedAnswers = request.Answers
            .Select(answer => new
            {
                QuestionId = Normalize(answer.QuestionId),
                answer.GuestId,
                AnswerValue = CanonicalizeJson(answer.AnswerValue),
                DisplayValue = Normalize(answer.DisplayValue)
            })
            .OrderBy(answer => answer.QuestionId, StringComparer.Ordinal)
            .ThenBy(answer => answer.GuestId)
            .ToList();
        var normalized = new
        {
            OperationScope = operationScope,
            request.ExpectedRevision,
            OverallStatus = request.OverallStatus.ToString(),
            ContactName = Normalize(request.ContactName),
            ContactEmail = Normalize(request.ContactEmail)?.ToLowerInvariant(),
            ContactPhone = Normalize(request.ContactPhone),
            Guests = normalizedGuests,
            Answers = normalizedAnswers,
            Consent = CanonicalizeJson(request.ConsentSnapshot)
        };
        var json = JsonSerializer.Serialize(normalized);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json)));
    }

    public static string CanonicalizeJson(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        try
        {
            using var document = JsonDocument.Parse(value);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                WriteCanonical(document.RootElement, writer);
            }

            return Encoding.UTF8.GetString(stream.ToArray());
        }
        catch (JsonException)
        {
            return value.Trim();
        }
    }

    private static void WriteCanonical(
        JsonElement element,
        Utf8JsonWriter writer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var property in element.EnumerateObject()
                             .OrderBy(
                                 property => property.Name,
                                 StringComparer.Ordinal))
                {
                    writer.WritePropertyName(property.Name);
                    WriteCanonical(property.Value, writer);
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteCanonical(item, writer);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String:
                writer.WriteStringValue(element.GetString());
                break;
            case JsonValueKind.Number:
                writer.WriteRawValue(element.GetRawText());
                break;
            case JsonValueKind.True:
                writer.WriteBooleanValue(true);
                break;
            case JsonValueKind.False:
                writer.WriteBooleanValue(false);
                break;
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                writer.WriteNullValue();
                break;
            default:
                throw new InvalidOperationException(
                    "Tipo JSON no soportado.");
        }
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(
        "^[A-Za-z0-9][A-Za-z0-9._:-]{15,127}$",
        RegexOptions.CultureInvariant)]
    private static partial Regex IdempotencyKeyPattern();
}
