using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PnP.Core.Services;

using Serilog;

namespace ACSSolutions.CDRRelay
{
	// There's an instance of this for each connected client.
	internal class Handler
	{
		protected IServiceScopeFactory _serviceScopeFactory { get; }
		protected Settings _settings;
		protected CDR _record = new CDR(); // avoid allocation through re-use

		public Handler(
			IServiceScopeFactory serviceScopeFactory,
			IOptions<Settings> options
		)
		{
			_serviceScopeFactory = serviceScopeFactory;
			_settings = options.Value;
		}

		static CsvConfiguration __config = new CsvConfiguration( CultureInfo.InvariantCulture )
		{
			HasHeaderRecord = false
		};

		public async Task Run( CancellationToken cancellation, Socket socket )
		{
			try
			{
				// Build  a pipeline to connect the CsvReader to the TCP socket
				Log.Debug( "Client Connected on socket {handle}", socket.Handle );
				using( socket )
				using( var stm = new NetworkStream( socket ) )
				using( var stmreader = new StreamReader( stm, encoding: Encoding.ASCII, bufferSize: 8192 ) )
				using( var csvreader = new CsvReader( stmreader, __config ) )
				// Now connect up to SharePoint and get a reference to the target list 
				using( var scope = _serviceScopeFactory.CreateScope() )
				{
					// Obtain a PnP Context factory
					var pnpContextFactory = scope.ServiceProvider.GetRequiredService<IPnPContextFactory>();
					// Use the PnP Context factory to get a PnPContext for the given configuration
					using( var context = await pnpContextFactory.CreateAsync( "site" ) )
					{
						var list = await context.Web.Lists.GetByTitleAsync( _settings.ListName );
						{
							// This is IAsyncEnum
							var records = csvreader.EnumerateRecordsAsync<CDR>( _record, cancellation );
							await foreach( var record in records )
							{
								var item = new Dictionary<String, object?> {
									{ "Title", record.historyid },
									{ "Started", record.started },
									{ "Answered", record.answered },
									{ "Ended", record.ended },
									{ "Termination", record.end_reason },
									{ "FromNumber", record.from_number },
									{ "FromName", record.from_name },
									{ "FromDN", record.from_dn },
									{ "ToNumber", record.to_number },
									{ "ToName", record.to_name },
									{ "ToDN", record.to_dn },
									{ "DialNumber", record.dial_number },
									{ "Chain", record.chain }
								};

								Log.Debug( "Adding item {item}", item );
								await list.Items.AddAsync( item );
								Log.Information( "Added item {item}", record.historyid );
							}
						}
					}
				}

			}
			catch( Exception ex )
			{
				Log.Error( ex, "Unhandled Exception in Handler" );
			}
		}
	}
}
