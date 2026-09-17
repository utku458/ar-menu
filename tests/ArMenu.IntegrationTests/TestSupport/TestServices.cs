using ArMenu.Application;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure;
using ArMenu.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ArMenu.IntegrationTests.TestSupport;

/// <summary>The real infrastructure composition (<c>AddInfrastructure</c>), without a web host.</summary>
public sealed class TestServices : IAsyncDisposable
{
    private readonly ServiceProvider _provider;

    public TestServices(string connectionString, TimeProvider? timeProvider = null, Action<IServiceCollection>? configureServices = null, StorageFixture? storageFixture = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(TestConfiguration.For(connectionString, storageFixture))
            .Build();

        var services = new ServiceCollection().AddLogging().AddMetrics();
        if (timeProvider is not null)
        {
            services.AddSingleton(timeProvider);
        }

        services.AddApplication().AddInfrastructure(configuration);
        configureServices?.Invoke(services);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    /// <summary>Starts a unit of work, optionally bound to a tenant, like an HTTP request after tenant resolution.</summary>
    public TestScope BeginScope(TenantInfo? tenant = null)
    {
        var scope = new TestScope(_provider.CreateAsyncScope());
        if (tenant is not null)
        {
            scope.Get<ITenantContextSetter>().SetTenant(tenant);
        }

        return scope;
    }

    /// <summary>Persists a new tenant with one category and one item, through the regular write pipeline.</summary>
    public async Task<SeededTenant> SeedTenantAsync(CancellationToken cancellationToken, Action<Tenant>? configure = null)
    {
        var tenant = TestData.NewTenant();
        configure?.Invoke(tenant);

        var tenantInfo = TenantInfo.From(tenant);
        var category = TestData.NewCategory(tenant.Id);
        var item = TestData.NewItem(tenant.Id, category.Id);

        await using var scope = BeginScope(tenantInfo);
        scope.Db.Tenants.Add(tenant);
        scope.Db.MenuCategories.Add(category);
        scope.Db.MenuItems.Add(item);
        await scope.Db.SaveChangesAsync(cancellationToken);

        return new SeededTenant(tenantInfo, category.Id, item.Id);
    }

    /// <summary>Creates a user account (password <see cref="TestConfiguration.Password"/>) that is a member of <paramref name="tenant"/>.</summary>
    public async Task<SeededMember> SeedMemberAsync(TenantInfo tenant, TenantRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        await using var scope = BeginScope(tenant);
        var passwordHasher = scope.Get<IPasswordHasher>();

        var email = $"{role.ToString().ToLowerInvariant()}-{Guid.NewGuid():N}@armenu.test";
        var user = User.Register(Email.Create(email).Value, $"Test {role}", passwordHasher.Hash(TestConfiguration.Password)).Value;
        user.MarkEmailVerified(DateTimeOffset.UtcNow);

        scope.Db.Users.Add(user);
        scope.Db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, user.Id, role));
        await scope.Db.SaveChangesAsync(cancellationToken);

        return new SeededMember(user.Id, email, role);
    }

    /// <summary>Makes an existing user a member of another tenant as well.</summary>
    public async Task AddMembershipAsync(TenantInfo tenant, UserId userId, TenantRole role, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tenant);

        await using var scope = BeginScope(tenant);
        scope.Db.TenantMemberships.Add(TenantMembership.Create(tenant.Id, userId, role));
        await scope.Db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>A singleton of the composition, such as a worker's job.</summary>
    public T Get<T>()
        where T : notnull => _provider.GetRequiredService<T>();

    public ValueTask DisposeAsync() => _provider.DisposeAsync();
}

public sealed class TestScope(AsyncServiceScope scope) : IAsyncDisposable
{
    public ArMenuDbContext Db => Get<ArMenuDbContext>();

    public T Get<T>()
        where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    public ValueTask DisposeAsync() => scope.DisposeAsync();
}

public sealed record SeededTenant(TenantInfo Tenant, MenuCategoryId CategoryId, MenuItemId ItemId);

public sealed record SeededMember(UserId UserId, string Email, TenantRole Role);
