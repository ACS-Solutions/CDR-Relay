
using System;
using System.Runtime.CompilerServices;

using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.ApplicationInsights;

namespace ACSSolutions.Extensions.Serilog
{

	public static class SerilogExtensions
	{

		public static ILogger AtSource(
			this ILogger logger,
			[CallerFilePath] String caller_file_path = null,
			[CallerLineNumber] Int32? caller_line_number = null
		)
		{
			return logger?
				.ForContext( "SourceFilePath", caller_file_path )
				.ForContext( "SourceLineNumber", caller_line_number );
		}

		public static ILogger Enter( this ILogger logger, Object args = null )
		{
			if( null != args )
				logger?.Debug( "Enter {@Arguments}", args );
			else
				logger?.Debug( "Enter" );
			return logger;
		}

		public static ILogger Exit( this ILogger logger, Boolean fault = false )
		{
			logger?.Debug( fault ? "Fault" : "Exit" );
			return logger;
		}

		public static ILogger Fault( this ILogger logger )
		{
			logger?.Debug( "Fault" );
			return logger;
		}

		public static ILogger ForType( this ILogger logger, Type type )
		{
			return logger?.ForContext( "SourceContext", type );
		}

		public static ILogger InMember(
			this ILogger logger,
			Action<ILogger> action = null,
			[CallerMemberName] String caller_member_name = null
		)
		{
			return logger.InMember( null, action, caller_member_name );
		}

		public static ILogger InMember(
			this ILogger logger,
			Object args,
			Action<ILogger> action = null,
			[CallerMemberName] String caller_member_name = null
		)
		{
			var member_logger = logger?.ForContext( "MemberName", caller_member_name );
			if( null != action )
			{
				member_logger.Enter( args );
				Boolean fault = true;
				try
				{
					action( member_logger );
					fault = false;
				}
				finally
				{
					member_logger.Exit( fault );
				}
			}
			return member_logger;
		}

		public static TRet InMember<TRet>(
			this ILogger logger,
			Func<ILogger, TRet> func,
			[CallerMemberName] String caller_member_name = null
		)
		{
			return logger.InMember( null, func, caller_member_name );
		}

		public static TRet InMember<TRet>(
			this ILogger logger,
			Object args,
			Func<ILogger, TRet> func,
			[CallerMemberName] String caller_member_name = null
		)
		{
			if( null == func )
				throw new ArgumentNullException( nameof( func ) );
			logger = logger?.ForContext( "MemberName", caller_member_name );
			Boolean fault = true;
			logger?.Enter( args );
			try
			{
				var ret = func( logger );
				fault = false;

				return ret;
			}
			finally
			{
				logger?.Exit( fault );
			}
		}

		public static ILogger PrintMember(
			this ILogger logger,
			[CallerMemberName] String caller_member_name = null
		)
		{
			logger?.Debug( "In {MemberName}", caller_member_name );
			return logger;
		}

		public static ILogger PrintSource(
			this ILogger logger,
			[CallerFilePath] String caller_file_path = null,
			[CallerLineNumber] Int32? caller_line_number = null
		)
		{
			logger?.Debug( "{SourceFilePath}:{SourceLineNumber}", caller_file_path, caller_line_number );
			return logger;
		}

		/// <summary>
		/// Adds a Serilog sink that writes <see cref="LogEvent">log events</see> to Microsoft Application Insights 
		/// using a custom <see cref="ITelemetry"/> converter / constructor.
		/// </summary>
		/// <param name="loggerConfiguration">The logger configuration.</param>
		/// <param name="telemetryConfiguration">Required Application Insights configuration settings.</param>
		/// <param name="telemetryConverter">Required telemetry converter.</param>
		/// <param name="restrictedToMinimumLevel">The minimum log event level required in order to write an event to the sink.</param>
		/// <returns></returns>
		public static LoggerConfiguration AcsApplicationInsights(
			this LoggerSinkConfiguration loggerConfiguration,
			LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum,
			// this is magically injected by the activation logic in Serilog.Settings.Configuration.
			// IConfiguration is the ONLY such parameter to be injected automatically.
			IConfiguration configuration = default
		)
		{
			TelemetryConfiguration telemetryConfiguration = TelemetryConfiguration.CreateDefault();
			telemetryConfiguration.ConnectionString = configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"] ?? configuration["ApplicationInsights:ConnectionString"];
			var client = new TelemetryClient( telemetryConfiguration );

			return loggerConfiguration.Sink(
				new ApplicationInsightsSink(
					client,
					TelemetryConverter.Traces
				),
				restrictedToMinimumLevel
			);
		}

	}

	public class TaskIdEnricher
		: ILogEventEnricher
	{

		public void Enrich( LogEvent log_event, ILogEventPropertyFactory property_factory )
		{
			log_event.AddPropertyIfAbsent( property_factory.CreateProperty( "TaskId", Task.CurrentId ?? 0 ) );
		}

	}

}
