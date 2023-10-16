using System;
using System.ComponentModel.DataAnnotations;

using ACSSolutions.CDRRelay;
using ACSSolutions.Extensions.Serilog;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Systemd;
using Microsoft.Extensions.Options;

using PnP.Core.Auth.Services.Builder.Configuration;
using PnP.Core.Services.Builder.Configuration;

using Serilog;

var cts = new CancellationTokenSource();
var cancellation = cts.Token;

Settings settings;

Serilog.Debugging.SelfLog.Enable( Console.Out );

Log.Logger = new LoggerConfiguration()
	.MinimumLevel.Debug()
	.WriteTo.Console()
	.CreateBootstrapLogger();
Log.Information( "So it begins..." );

try
{
	var builder = Host.CreateDefaultBuilder( args );
	ConfigureLogging( builder );
	ConfigureConfiguration( builder );
	ConfigureServices( builder );
	ConfigureSystemd( builder );
	Log.Debug( "Host Building" );
	var host = builder.Build();
	Log.Debug( "Host Built" );
	settings = host.Services.GetRequiredService<IOptions<Settings>>().Value;
	var validationctx = new ValidationContext( settings );
	Validator.ValidateObject( settings, validationctx );
	Log.Debug( "Settings Valid" );
	await host.RunAsync( cancellation );
}
catch( Exception ex )
{
	Log.Fatal( ex, "Unhandled exception" );
}
finally
{
	Log.Information( "Done & Dusted." );
	Log.CloseAndFlush();
}

//-----------------------------------------------------------------------------------------------------

void ConfigureLogging( IHostBuilder host )
{
	Log.Logger.InMember().Enter();

	host.UseSerilog(
		( ctx, services, conf ) => {
			Log.Debug( "Enter: ConfigureLogging:UseSerilog" );
			conf
				.ReadFrom.Configuration( ctx.Configuration );
			Log.Debug( "Exit: ConfigureLogging:UseSerilog" );
		}
	// .ReadFrom.Services(services)
	);

	Log.Logger.InMember().Exit();
}

//-----------------------------------------------------------------------------------------------------

void ConfigureConfiguration( IHostBuilder host )
{
	Log.Logger.InMember().Enter();

	host.ConfigureAppConfiguration(
		( ctx, configBuilder ) =>
		{
			Log.Debug( "Enter: ConfigureConfiguration:Callback" );

			// get optional config from ../config/appsettings.json
			var siblingConfigFilePath =
				Path.GetFullPath(
					new Uri(
						Path.Join(
							Directory.GetCurrentDirectory(),
							"..",
							"config",
							"appsettings.json"
						)
					)
					.LocalPath
				)
				.TrimEnd(
					Path.DirectorySeparatorChar,
					Path.AltDirectorySeparatorChar
				);

			configBuilder
				.AddJsonFile(
					siblingConfigFilePath,
					true
				);
			
			configBuilder.AddApplicationInsightsSettings(
				connectionString:
					ctx.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
					??
					ctx.Configuration["ApplicationInsights:ConnectionString"],
				developerMode:
					ctx.HostingEnvironment.IsDevelopment()
			);

			Log.Debug( "Exit: ConfigureConfiguration:Callback" );
		}
	);

	Log.Logger.InMember().Exit();
}

//-----------------------------------------------------------------------------------------------------

void ConfigureServices( IHostBuilder builder )
{
	Log.Logger.InMember().Enter();

	// Add services to the container
	builder.ConfigureServices(
		( ctx, services ) =>
		{
			Log.Debug( "Enter: ConfigureServices" );

			// Add the Application Settings configuration from the appsettings.json file
			services.Configure<Settings>( ctx.Configuration.GetSection( "Settings" ) );
			// Add an options wrapper around the Settings
			services.AddOptions<Settings>();
			// Add the PnP Core SDK library services
			services.AddPnPCore();
			// Add the PnP Core SDK library services configuration from the appsettings.json file
			services.Configure<PnPCoreOptions>( ctx.Configuration.GetSection( "PnPCore" ) );
			// Add the PnP Core SDK Authentication Providers
			services.AddPnPCoreAuthentication();
			// Add the PnP Core SDK Authentication Providers configuration from the appsettings.json file
			services.Configure<PnPCoreAuthenticationOptions>( ctx.Configuration.GetSection( "PnPCore" ) );
			// Add our relay as a service
			services.AddHostedService<Relay>();
			// Add our socket handler as transient
			services.AddTransient<Handler>();
			// ApplicationInsights
			//			services.AddApplicationInsightsTelemetryWorkerService();

			Log.Debug( "Exit: ConfigureServices" );
		}
	);

	Log.Logger.InMember().Exit();
}

void ConfigureSystemd( IHostBuilder builder )
{
	// Provide hooks for linux systemd
	Log.Debug( "Adding Systemd Support" );
	builder.UseSystemd();
	Log.Debug( "Added Systemd Support" );
}