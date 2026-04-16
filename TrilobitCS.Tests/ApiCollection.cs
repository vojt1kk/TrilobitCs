using Xunit;

namespace TrilobitCS.Tests;

// Sdílí jeden PostgreSQL kontejner napříč všemi testovacími třídami
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<TrilobitWebApplicationFactory> { }
