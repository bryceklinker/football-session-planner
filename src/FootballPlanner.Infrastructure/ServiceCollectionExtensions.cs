using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? configureDb = null)
    {
        var dbOptions = configureDb
            ?? (options => options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        services.AddDbContext<AppDbContext>(dbOptions);
        return services;
    }
}
