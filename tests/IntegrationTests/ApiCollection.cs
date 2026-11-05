namespace Bookly.IntegrationTests;

[CollectionDefinition(nameof(ApiCollection))]
public sealed class ApiCollection : ICollectionFixture<BooklyApiFactory>;
