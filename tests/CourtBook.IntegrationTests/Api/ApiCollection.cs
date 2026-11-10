using Xunit;

namespace CourtBook.IntegrationTests.Api;

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<CourtBookApiFactory>
{
    public const string Name = "Api";
}