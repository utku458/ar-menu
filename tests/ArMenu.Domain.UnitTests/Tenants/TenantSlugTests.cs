using ArMenu.Domain.Tenants;
using ArMenu.Domain.UnitTests.TestSupport;

namespace ArMenu.Domain.UnitTests.Tenants;

public sealed class TenantSlugTests
{
    [Theory]
    [InlineData("abc")]
    [InlineData("kadikoy-burger-lab")]
    [InlineData("cafe-1907")]
    [InlineData("1907")]
    public void Create_accepts_dns_label_compatible_slugs(string value)
    {
        TenantSlug.Create(value).ShouldSucceed().Value.ShouldBe(value);
    }

    [Fact]
    public void Create_normalizes_case_and_surrounding_whitespace()
    {
        TenantSlug.Create("  Kadikoy-Burger-Lab ").ShouldSucceed().Value.ShouldBe("kadikoy-burger-lab");
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("double--hyphen")]
    [InlineData("under_score")]
    [InlineData("with space")]
    [InlineData("kadıköy")]
    [InlineData("İstanbul")]
    public void Create_rejects_slugs_that_are_not_url_and_dns_safe(string value)
    {
        TenantSlug.Create(value).ShouldFailWith(TenantErrors.SlugInvalid);
    }

    [Fact]
    public void Create_rejects_slugs_longer_than_a_dns_label()
    {
        TenantSlug.Create(new string('a', TenantSlug.MaxLength + 1)).ShouldFailWith(TenantErrors.SlugInvalid);
        TenantSlug.Create(new string('a', TenantSlug.MaxLength)).ShouldSucceed();
    }

    [Theory]
    [InlineData("admin", true)]
    [InlineData("api", true)]
    [InlineData("www", true)]
    [InlineData("kadikoy-burger-lab", false)]
    public void IsReserved_flags_names_used_by_the_platform(string value, bool reserved)
    {
        Make.Slug(value).IsReserved.ShouldBe(reserved);
    }
}
