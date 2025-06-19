using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Fluxify;

public abstract class LoggerBase
{
    private ILogger? _logger;

    public static ILoggerFactory LoggerFactory { get; set; } = NullLoggerFactory.Instance;

    protected ILogger Logger => _logger ??= LoggerFactory.CreateLogger(GetType());
}