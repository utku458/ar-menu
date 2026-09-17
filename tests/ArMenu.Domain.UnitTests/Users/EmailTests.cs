using ArMenu.Domain.UnitTests.TestSupport;
using ArMenu.Domain.Users;

namespace ArMenu.Domain.UnitTests.Users;

public sealed class EmailTests
{
    [Fact]
    public void Create_normalizes_case_and_whitespace_so_lookups_cannot_be_bypassed()
    {
        Email.Create("  Owner@Kadikoy-Burger-Lab.TEST ").ShouldSucceed().Value.ShouldBe("owner@kadikoy-burger-lab.test");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("missing-domain@")]
    [InlineData("@missing-local.test")]
    [InlineData("no-dot@domain")]
    [InlineData("two@@signs.test")]
    [InlineData("with space@armenu.test")]
    public void Create_rejects_malformed_addresses(string value)
    {
        Email.Create(value).ShouldFailWith(UserErrors.EmailInvalid);
    }

    [Fact]
    public void Create_rejects_addresses_longer_than_the_rfc_limit()
    {
        var tooLong = new string('a', 64) + "@" + new string('b', Email.MaxLength - 64 - 5) + ".test";

        Email.Create(tooLong).ShouldFailWith(UserErrors.EmailInvalid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    public void Create_requires_a_value(string? value)
    {
        Email.Create(value).ShouldFailWith(UserErrors.EmailRequired);
    }
}
