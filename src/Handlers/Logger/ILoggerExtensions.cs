using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace FriendlyMeme.Handlers.Logger;

public static class LoggerExtensions
{
    private static void AddColorConsoleLogger(this ILoggingBuilder builder)
    {
        builder.AddConfiguration();

        builder.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<ILoggerProvider, ColorConsoleLoggerProvider>());

        LoggerProviderOptions.RegisterProviderOptions
            <LoggerConfig, ColorConsoleLoggerProvider>(builder.Services);
    }

    
    public static void AddColorConsoleLogger(this ILoggingBuilder builder,
        Action<LoggerConfig> configure)
    {
        builder.AddColorConsoleLogger();
        builder.Services.Configure(configure);
    }
}