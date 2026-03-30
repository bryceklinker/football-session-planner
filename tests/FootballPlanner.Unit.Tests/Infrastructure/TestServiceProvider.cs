using FootballPlanner.Application;
using FootballPlanner.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FootballPlanner.Unit.Tests.Infrastructure;

public static class TestServiceProvider
{
    public static IMediator CreateMediator()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(
            configuration,
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return services.BuildServiceProvider().GetRequiredService<IMediator>();
    }
}
