using Xunit;

namespace BProMax.E2ETests;

/// <summary>
/// Collection definition to serialize E2E tests that share a single test database.
/// Prevents concurrent tests from interfering with each other's data.
/// Each test class creates its own connection via E2ETestBase.
/// </summary>
[CollectionDefinition("E2E Database")]
public class E2EDatabaseFixture
{
}
