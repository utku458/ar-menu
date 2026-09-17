using System.Reflection;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Persistence.Conventions;
using Microsoft.EntityFrameworkCore;

namespace ArMenu.Infrastructure.Persistence;

public sealed class ArMenuDbContext(DbContextOptions<ArMenuDbContext> options, ITenantContext tenantContext)
    : DbContext(options)
{
    private static readonly MethodInfo ApplyTenantFilterMethod = typeof(ArMenuDbContext)
        .GetMethod(nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;

    private static readonly MethodInfo ApplySoftDeleteFilterMethod = typeof(ArMenuDbContext)
        .GetMethod(nameof(ApplySoftDeleteFilter), BindingFlags.Static | BindingFlags.NonPublic)!;

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    public DbSet<User> Users => Set<User>();

    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<TenantInvitation> TenantInvitations => Set<TenantInvitation>();

    public DbSet<UserToken> UserTokens => Set<UserToken>();

    public DbSet<ArModelProcessing> ArModelProcessings => Set<ArModelProcessing>();

    /// <summary>
    /// Referenced by the tenant query filter. EF Core evaluates it on every query execution, not when the (cached) model
    /// is built, so the filter always reflects the scope's tenant. Reading it without a bound tenant throws
    /// <see cref="TenantNotResolvedException"/>: tenant-scoped queries fail loudly instead of returning wrong data.
    /// </summary>
    private TenantId CurrentTenantId => tenantContext.TenantId;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.AddDomainTypeConventions();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ArMenuDbContext).Assembly);

        foreach (var clrType in modelBuilder.Model.GetEntityTypes().Select(entityType => entityType.ClrType).ToList())
        {
            if (typeof(IAggregateRoot).IsAssignableFrom(clrType))
            {
                var entity = modelBuilder.Entity(clrType);
                entity.Ignore(nameof(IAggregateRoot.DomainEvents));

                // PostgreSQL's system column xmin changes on every row update: free optimistic concurrency, no extra column.
                entity.Property<uint>(ShadowProperties.RowVersion).IsRowVersion();
            }

            if (typeof(ITenantScoped).IsAssignableFrom(clrType))
            {
                ApplyTenantFilterMethod.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            }

            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                ApplySoftDeleteFilterMethod.MakeGenericMethod(clrType).Invoke(null, [modelBuilder]);
            }
        }
    }

    // Named filters (EF Core 10) can be disabled independently: an "include deleted" admin query can switch off
    // SoftDelete while the Tenant filter keeps protecting it. With a single anonymous filter that was impossible.
    private void ApplyTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ITenantScoped =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(QueryFilters.Tenant, entity => entity.TenantId == CurrentTenantId);

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable =>
        modelBuilder.Entity<TEntity>().HasQueryFilter(QueryFilters.SoftDelete, entity => !entity.IsDeleted);
}
