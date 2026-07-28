namespace Plannyt.Api.BuildingBlocks.Domain;

public sealed class DomainRuleException(string message) : Exception(message);
