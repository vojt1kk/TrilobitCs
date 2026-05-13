using Xunit;

namespace TrilobitCS.Tests;

// Shares one PostgreSQL container across all test classes.
[CollectionDefinition("Api")]
public class ApiCollection : ICollectionFixture<TrilobitWebApplicationFactory> { }
