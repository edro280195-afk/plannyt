namespace Plannyt.Api.IntegrationTests.Infrastructure;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "Plannyt API";
}
