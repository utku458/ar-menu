using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using ArMenu.Application.Abstractions.Accounts;
using ArMenu.Application.Abstractions.Assets;
using ArMenu.Application.Abstractions.Authentication;
using ArMenu.Application.Abstractions.Email;
using ArMenu.Application.Abstractions.Statistics;
using ArMenu.Application.ArModels;
using ArMenu.Application.Emails;
using ArMenu.Application.MultiTenancy;
using ArMenu.Domain.ArModels;
using ArMenu.Domain.Common;
using ArMenu.Domain.Memberships;
using ArMenu.Domain.Menus;
using ArMenu.Domain.Sessions;
using ArMenu.Domain.Tenants;
using ArMenu.Domain.Users;
using ArMenu.Infrastructure.Accounts;
using ArMenu.Infrastructure.ArModels;
using ArMenu.Infrastructure.Assets;
using ArMenu.Infrastructure.Auditing;
using ArMenu.Infrastructure.Authentication;
using ArMenu.Infrastructure.Mail;
using ArMenu.Infrastructure.Maintenance;
using ArMenu.Infrastructure.MultiTenancy;
using ArMenu.Infrastructure.Persistence;
using ArMenu.Infrastructure.Persistence.Interceptors;
using ArMenu.Infrastructure.Persistence.Repositories;
using ArMenu.Infrastructure.Retention;
using ArMenu.Infrastructure.Statistics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ArMenu.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        services.TryAddSingleton(TimeProvider.System);

        return services
            .AddMultiTenancy()
            .AddPersistence(configuration)
            .AddAuthenticationServices(configuration)
            .AddAssets(configuration)
            .AddArModelProcessing(configuration)
            .AddEmail(configuration);
    }

    private static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        // One scoped instance, exposed through a read-only and a write-only interface (interface segregation).
        services.AddScoped<TenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<TenantContext>());
        services.AddScoped<ITenantContextSetter>(provider => provider.GetRequiredService<TenantContext>());

        services.AddHybridCache();
        services.AddSingleton<ITenantLookup, TenantLookup>();

        return services;
    }

    private static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString(ConnectionStringNames.Runtime);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"Connection string '{ConnectionStringNames.Runtime}' is not configured.");
        }

        // Pooled connections must be reset when returned (DISCARD ALL), otherwise a physical connection could carry the
        // previous request's app.current_tenant into code that does not go through our connection interceptor.
        if (new NpgsqlConnectionStringBuilder(connectionString).NoResetOnClose)
        {
            throw new InvalidOperationException(
                "'No Reset On Close' must not be enabled: session state, including the current tenant, has to be reset between pooled connection uses.");
        }

        services.TryAddScoped<ICurrentUser, NoCurrentUser>();
        services.AddScoped<AuditTrailSaveChangesInterceptor>();
        services.AddScoped<TenantIsolationSaveChangesInterceptor>();
        services.AddScoped<TenantSessionConnectionInterceptor>();
        services.AddScoped<MenuCacheInvalidationInterceptor>();
        services.AddSingleton<SoftDeleteSaveChangesInterceptor>();
        services.AddSingleton<AuditingSaveChangesInterceptor>();

        services.AddDbContext<ArMenuDbContext>((provider, options) => options
            .UseArMenuDatabase(connectionString)
            .AddInterceptors(
                // Order matters: write the history while deletions are still deletions, validate tenant ownership of the
                // raw changes (history included), then rewrite deletes, then stamp, and only then record which cached
                // menus the (successful) save invalidates.
                provider.GetRequiredService<AuditTrailSaveChangesInterceptor>(),
                provider.GetRequiredService<TenantIsolationSaveChangesInterceptor>(),
                provider.GetRequiredService<SoftDeleteSaveChangesInterceptor>(),
                provider.GetRequiredService<AuditingSaveChangesInterceptor>(),
                provider.GetRequiredService<MenuCacheInvalidationInterceptor>(),
                provider.GetRequiredService<TenantSessionConnectionInterceptor>()));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITenantMembershipRepository, TenantMembershipRepository>();
        services.AddScoped<ITenantInvitationRepository, TenantInvitationRepository>();
        services.AddScoped<IUserTokenRepository, UserTokenRepository>();
        services.AddScoped<IMenuStatisticsRecorder, MenuStatisticsRecorder>();
        services.AddSingleton<IAccountErasure, AccountErasure>();
        services.AddScoped<IUserSessionRepository, UserSessionRepository>();
        services.AddScoped<IMenuCategoryRepository, MenuCategoryRepository>();
        services.AddScoped<IMenuItemRepository, MenuItemRepository>();
        services.AddScoped<IArModelProcessingRepository, ArModelProcessingRepository>();

        services.AddHealthChecks()
            .AddDbContextCheck<ArMenuDbContext>("database", tags: [HealthCheckTags.Ready]);

        return services;
    }

    private static IServiceCollection AddAuthenticationServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<JwtOptions>, JwtOptionsValidator>();

        services.AddOptions<PasswordHashingOptions>()
            .Bind(configuration.GetSection(PasswordHashingOptions.SectionName))
            .Validate(options => options.Iterations > 0, "Password hashing iterations must be positive.")
            .ValidateOnStart();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IAccessTokenIssuer, JwtAccessTokenIssuer>();

        services.AddOptions<Platform.PlatformAdministratorOptions>()
            .Bind(configuration.GetSection(Platform.PlatformAdministratorOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.InitialPassword), "PlatformAdministrator:InitialPassword cannot be empty.")
            .ValidateOnStart();
        services.AddSingleton<IRefreshTokenCodec, RefreshTokenCodec>();
        services.AddSingleton<IInvitationTokenCodec, InvitationTokenCodec>();
        services.AddSingleton<IUserTokenCodec, UserTokenCodec>();

        return services;
    }

    private static IServiceCollection AddAssets(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AssetOptions>()
            .Bind(configuration.GetSection(AssetOptions.SectionName))
            .Validate(
                options => options.PublicBaseUrl is { IsAbsoluteUri: true } && options.PublicBaseUrl.AbsolutePath.EndsWith('/'),
                $"{AssetOptions.SectionName}:PublicBaseUrl must be an absolute URL ending with '/'.")
            .ValidateOnStart();

        services.AddSingleton<IAssetUrlResolver, AssetUrlResolver>();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<StorageOptions>, StorageOptionsValidator>();
        services.AddSingleton<IAmazonS3>(provider => CreateS3Client(provider.GetRequiredService<IOptions<StorageOptions>>().Value));
        services.AddKeyedSingleton<IAmazonS3>(StorageOptions.InternalClientKey, (provider, _) =>
        {
            var options = provider.GetRequiredService<IOptions<StorageOptions>>().Value;
            return options.InternalServiceUrl is null
                ? provider.GetRequiredService<IAmazonS3>()
                : CreateS3Client(options, options.InternalServiceUrl);
        });
        services.AddKeyedSingleton<IAmazonS3>(StorageOptions.PublicClientKey, (provider, _) =>
        {
            var options = provider.GetRequiredService<IOptions<StorageOptions>>().Value;
            return options.PublicServiceUrl is null
                ? provider.GetRequiredService<IAmazonS3>()
                : CreateS3Client(options, options.PublicServiceUrl);
        });
        services.AddSingleton<IAssetStorage, S3AssetStorage>();

        services.AddOptions<AssetCleanupOptions>()
            .Bind(configuration.GetSection(AssetCleanupOptions.SectionName))
            .Validate(
                options => options.Interval > TimeSpan.Zero && options.GracePeriod >= TimeSpan.Zero,
                "AssetCleanup:Interval must be positive and AssetCleanup:GracePeriod cannot be negative.")
            .ValidateOnStart();
        services.AddSingleton<AssetCleanup>();
        services.AddHostedService<AssetCleanupWorker>();

        services.AddOptions<RetentionOptions>()
            .Bind(configuration.GetSection(RetentionOptions.SectionName))
            .Validate(
                options => options.Interval > TimeSpan.Zero && options.ClosedBusinessRetention >= TimeSpan.Zero,
                "Retention:Interval must be positive and Retention:ClosedBusinessRetention cannot be negative.")
            .ValidateOnStart();
        services.AddSingleton<TenantPurge>();
        services.AddHostedService<TenantPurgeWorker>();

        services.AddOptions<BacklogMetricsOptions>()
            .Bind(configuration.GetSection(BacklogMetricsOptions.SectionName))
            .Validate(options => options.Interval > TimeSpan.Zero, "BacklogMetrics:Interval must be positive.")
            .ValidateOnStart();
        services.AddSingleton<OperationalBacklog>();
        services.AddSingleton<BacklogGauges>();
        services.AddHostedService<BacklogMetricsWorker>();

        return services;
    }

    private static IServiceCollection AddArModelProcessing(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ArModelProcessingOptions>().Bind(configuration.GetSection(ArModelProcessingOptions.SectionName));

        services.AddOptions<AssetProcessorOptions>()
            .Bind(configuration.GetSection(AssetProcessorOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AssetProcessorOptions>, AssetProcessorOptionsValidator>();

        services.AddHttpClient<IModelProcessor, HttpModelProcessor>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptions<AssetProcessorOptions>>().Value;
            if (options.Url is not null)
            {
                client.BaseAddress = options.Url;
            }

            // Jobs are bounded by AssetProcessor:Timeout, per request; the client-wide default would cut them at 100 s.
            client.Timeout = Timeout.InfiniteTimeSpan;
        });

        services.AddSingleton<ArModelProcessingSignal>();
        services.AddScoped<IArModelProcessingScheduler, ArModelProcessingScheduler>();
        services.AddSingleton<ArModelProcessingDispatcher>();
        services.AddHostedService<ArModelProcessingWorker>();

        return services;
    }

    private static IServiceCollection AddEmail(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<EmailOptions>, EmailOptionsValidator>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddSingleton<EmailOutboxSignal>();
        services.AddScoped<IEmailOutbox, EmailOutbox>();
        services.AddSingleton<EmailDelivery>();
        services.AddHostedService<EmailDeliveryWorker>();

        services.AddOptions<DashboardOptions>().Bind(configuration.GetSection(DashboardOptions.SectionName));

        return services;
    }

    private static AmazonS3Client CreateS3Client(StorageOptions options, Uri? serviceUrl = null)
    {
        serviceUrl ??= options.ServiceUrl;
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.ForcePathStyle,
            // Checksums only where S3 requires them: presigned browser uploads cannot send SDK-computed checksums,
            // and several S3-compatible services reject the newer default checksum headers.
            RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
            ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        };

        if (serviceUrl is not null)
        {
            config.ServiceURL = serviceUrl.ToString();
            config.AuthenticationRegion = options.Region;
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(options.Region);
        }

        return string.IsNullOrEmpty(options.AccessKey)
            ? new AmazonS3Client(config)
            : new AmazonS3Client(new BasicAWSCredentials(options.AccessKey, options.SecretKey), config);
    }
}
