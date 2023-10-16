using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using CsvHelper;
using Microsoft.Extensions.Hosting;

using PnP.Core.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ACSSolutions.CDRRelay
{
	internal class Relay
		: BackgroundService
	{
		private ILogger<Relay> _logger { get; }
		private IServiceProvider _services { get; }
		private Settings _settings { get; }

		public Relay(
			IOptions<Settings> options,
			ILogger<Relay> logger,
			IServiceProvider services
		)
		{
			_logger = logger;
			_services = services;
			_settings = options.Value;
		}



		protected override async Task ExecuteAsync( CancellationToken cancellation )
		{
			var endPoint = new IPEndPoint( IPAddress.Loopback, _settings.Port );
			var listener = new TcpListener( endPoint );
			listener.Start();

			while( !cancellation.IsCancellationRequested )
			{
				var socket = await listener.AcceptSocketAsync( cancellation );
				if( socket != null )
				{
					var handler = _services.GetRequiredService<Handler>();
					_ = handler.Run( cancellation, socket );
				}
			}
		}
	}
}