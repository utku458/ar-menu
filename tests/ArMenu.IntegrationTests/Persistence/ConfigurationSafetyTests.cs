using ArMenu.IntegrationTests.TestSupport;

namespace ArMenu.IntegrationTests.Persistence;

public sealed class ConfigurationSafetyTests(PostgresDatabaseFixture database)
{
    [Fact]
    public void Connection_strings_that_keep_session_state_across_pooled_uses_are_refused()
    {
        var unsafeConnectionString = database.RuntimeConnectionString + ";No Reset On Close=true";

        Should.Throw<InvalidOperationException>(() => new TestServices(unsafeConnectionString))
            .Message.ShouldContain("No Reset On Close");
    }
}
