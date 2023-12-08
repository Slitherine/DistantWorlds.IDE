using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace DistantWorlds.IDE.Logging;

internal sealed class ConsoleOptionsMonitor : IOptionsMonitor<ConsoleLoggerOptions> {

  private readonly ConsoleLoggerOptions _consoleLoggerOptions;

  public ConsoleOptionsMonitor()
    => _consoleLoggerOptions = new ConsoleLoggerOptions() {
      LogToStandardErrorThreshold = LogLevel.Trace
    };

  public ConsoleLoggerOptions CurrentValue => _consoleLoggerOptions;

  public ConsoleLoggerOptions Get(string name) => _consoleLoggerOptions;

  public IDisposable OnChange(Action<ConsoleLoggerOptions, string> listener)
    => null;

}