using FlowDesk.Application.CaseFlow;
using FlowDesk.Infrastructure.CaseFlow;
using FlowDesk.Infrastructure.Identity;
using FlowDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FlowDesk.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        services.AddDbContext<FlowDeskDbContext>(options => options.UseNpgsql(connectionString));
        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<FlowDeskDbContext>();

        services.AddCaseFlowClient(configuration);

        return services;
    }

    private static void AddCaseFlowClient(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection("CaseFlow").Get<CaseFlowOptions>() ?? new CaseFlowOptions();
        var baseUrl = configuration["CASEFLOW_BASE_URL"] ?? options.BaseUrl;

        if (string.Equals(options.ClientMode, "Http", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<ICaseFlowClient>(_ =>
                new HttpCaseFlowClient(new HttpClient
                {
                    BaseAddress = new Uri(baseUrl, UriKind.Absolute),
                }));

            return;
        }

        services.AddSingleton<ICaseFlowClient, FakeCaseFlowClient>();
    }

    public static async Task ApplyDatabaseMigrationsAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<FlowDeskDbContext>();

        await dbContext.Database.MigrateAsync();
        await dbContext.Database.CanConnectAsync();
    }
}
