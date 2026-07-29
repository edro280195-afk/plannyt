using System.Text.Json;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Plannyt.Api.Modules.Rsvp.Application;

namespace Plannyt.Api.UnitTests.BuildingBlocks;

public sealed class OpenApiSchemaTests
{
    [Fact]
    public void RequestContracts_CanGenerateJsonSchemas()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        serializerOptions.Converters.Add(new JsonStringEnumConverter());

        var requestTypes = typeof(RsvpSubmissionRequest).Assembly
            .GetTypes()
            .Where(type =>
                type.IsPublic
                && !type.IsAbstract
                && !type.ContainsGenericParameters
                && type.Name.EndsWith("Request", StringComparison.Ordinal))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToList();
        var errors = new List<string>();

        foreach (var requestType in requestTypes)
        {
            try
            {
                serializerOptions.GetJsonSchemaAsNode(requestType);
            }
            catch (Exception exception)
            {
                errors.Add($"{requestType.FullName}: {exception.Message}");
            }
        }

        Assert.True(
            errors.Count == 0,
            $"No fue posible generar el esquema JSON de:{Environment.NewLine}"
            + string.Join(Environment.NewLine, errors));
    }
}
