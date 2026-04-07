using FootballPlanner.Application;
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace FootballPlanner.Integration.Tests.Infrastructure;

public class TestApplication : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04").Build();

    public IMediator Mediator { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _container.GetConnectionString()
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        var provider = services.BuildServiceProvider();

        var db = provider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        Mediator = provider.GetRequiredService<IMediator>();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();
}
