using Microsoft.Extensions.Logging;

namespace FootballPlanner.Unit.Tests.Infrastructure;

public record LogRecord(LogLevel Level, EventId EventId, Exception? Exception, string Message);

public class CapturingLoggerProvider : ILoggerProvider
{
    private readonly List<LogRecord> _records = [];
    public IReadOnlyList<LogRecord> Records => _records;

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(_records);
    public void Dispose() { }
}

public class CapturingLogger(List<LogRecord> records) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        records.Add(new LogRecord(logLevel, eventId, exception, formatter(state, exception)));
    }
}
