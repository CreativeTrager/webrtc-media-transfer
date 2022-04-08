using System;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;


namespace CreativeTrager.WebRtcMediaSender.Logging;
internal static class LoggerConfigurator 
{
	private  static ILoggerFactory? Factory { get; set; }
	internal static ILoggerFactory CreateFactory() 
	{
		return Factory = new SerilogLoggerFactory(
			new LoggerConfiguration().Enrich.FromLogContext()
				.MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
				.WriteTo.Console().CreateLogger()
		);
	}
	internal static ILogger GetLogger() 
	{
		return Factory?.CreateLogger<Program>() ??
			throw new ApplicationException(message:
				$"No logger factory found.");
	}
}
