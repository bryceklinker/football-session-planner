using FootballPlanner.Application;
using FootballPlanner.Application.Activity;
using FootballPlanner.Infrastructure;
using FootballPlanner.Unit.Tests.Infrastructure;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FootballPlanner.Unit.Tests.Common;

public class LoggingBehaviourTests
{
    private static (IMediator mediator, CapturingLoggerProvider logs) CreateMediatorWithLogging()
    {
        var logProvider = new CapturingLoggerProvider();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        services.AddLogging(b => b.AddProvider(logProvider).SetMinimumLevel(LogLevel.Information));
        services.AddApplication();
        services.AddInfrastructure(
            configuration,
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        return (services.BuildServiceProvider().GetRequiredService<IMediator>(), logProvider);
    }

    [Fact]
    public async Task Send_LogsHandlingAtInformationLevel_WhenRequestIsSent()
    {
        var (mediator, logs) = CreateMediatorWithLogging();

        await mediator.Send(new CreateActivityCommand("Rondo", "Description", null, 10));

        Assert.Contains(logs.Records, r =>
            r.Level == LogLevel.Information &&
            r.Message.Contains("Handling") &&
            r.Message.Contains("CreateActivityCommand"));
    }

    [Fact]
    public async Task Send_LogsHandledWithDurationAtInformationLevel_WhenRequestSucceeds()
    {
        var (mediator, logs) = CreateMediatorWithLogging();

        await mediator.Send(new CreateActivityCommand("Rondo", "Description", null, 10));

        Assert.Contains(logs.Records, r =>
            r.Level == LogLevel.Information &&
            r.Message.Contains("Handled") &&
            r.Message.Contains("CreateActivityCommand") &&
            r.Message.Contains("ms"));
    }

    [Fact]
    public async Task Send_LogsFailedAtErrorLevel_WhenRequestThrows()
    {
        var (mediator, logs) = CreateMediatorWithLogging();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(
            () => mediator.Send(new CreateActivityCommand("", "Description", null, 10)));

        Assert.Contains(logs.Records, r =>
            r.Level == LogLevel.Error &&
            r.Message.Contains("failed") &&
            r.Message.Contains("CreateActivityCommand") &&
            r.Message.Contains("ms") &&
            r.Exception is FluentValidation.ValidationException);
    }
}
