using ArMenu.Application.Tenants.SignUp;

namespace ArMenu.Application.UnitTests.Tenants;

public sealed class SignUpCommandValidatorTests
{
    private static readonly SignUpCommand ValidCommand = new(
        BusinessName: "Moda Kahve",
        Slug: "moda-kahve",
        DefaultCulture: "tr",
        Currency: "TRY",
        OwnerFullName: "Ayşe Tan",
        OwnerEmail: "ayse@moda-kahve.test",
        Password: "correct-horse-battery");

    private readonly SignUpCommandValidator _validator = new();

    [Fact]
    public async Task A_complete_sign_up_is_valid()
    {
        var result = await _validator.ValidateAsync(ValidCommand, TestContext.Current.CancellationToken);

        result.IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("admin", "tenant.slug_reserved")]
    [InlineData("Moda Kahve", "tenant.slug_invalid")]
    public async Task Slugs_must_be_well_formed_and_not_reserved(string slug, string expectedCode)
    {
        var result = await _validator.ValidateAsync(ValidCommand with { Slug = slug }, TestContext.Current.CancellationToken);

        result.Errors.ShouldHaveSingleItem().ErrorCode.ShouldBe(expectedCode);
    }

    [Fact]
    public async Task Short_passwords_are_rejected()
    {
        var result = await _validator.ValidateAsync(ValidCommand with { Password = "short-pass" }, TestContext.Current.CancellationToken);

        result.Errors.ShouldHaveSingleItem().PropertyName.ShouldBe(nameof(SignUpCommand.Password));
    }

    [Fact]
    public async Task Every_invalid_field_is_reported_at_once_with_domain_error_codes()
    {
        var command = ValidCommand with { OwnerEmail = "not-an-email", DefaultCulture = "turkish", Currency = "TL" };

        var result = await _validator.ValidateAsync(command, TestContext.Current.CancellationToken);

        result.Errors.Select(error => error.ErrorCode).ShouldBe(
            ["culture_code.invalid", "currency.invalid", "user.email_invalid"],
            ignoreOrder: true);
    }

    [Fact]
    public void Commands_never_print_credentials()
    {
        ValidCommand.ToString().ShouldNotContain(ValidCommand.Password);
        ValidCommand.ToString().ShouldNotContain(ValidCommand.OwnerEmail);
    }
}
