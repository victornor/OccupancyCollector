using System;
using System.Threading;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OccupancyCollector.Services.Zigbee;
using Serilog;
using Serilog.Events;
using Uno.Wasm.Bootstrap.Server;
using ZigBeeNet;
using ZigBeeNet.Util;

namespace OccupancyCollector
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			ConfigureLogging(builder);
			
			// Check required args
			if (builder.Configuration.GetValue<string>("port") == null)
				throw new Exception("Missing argument: port");
			
			// Add services to the container.
			builder.Services.AddControllers();
			builder.Services.AddSingleton<ZigbeeManager>();

			var app = builder.Build();
			
			
			// Initialize zigbee network
			LogManager.SetFactory(app.Services.GetRequiredService<ILoggerFactory>());
			var zigbeeManager = app.Services.GetRequiredService<ZigbeeManager>();
			var dongle = zigbeeManager.PrepareDongle();
			var networkStatus = zigbeeManager.Initialize(dongle);

			if (networkStatus != ZigBeeStatus.SUCCESS)
				throw new Exception($"Failed to start zigbee network: {networkStatus}");
			
			// TODO: Remove and call through api
			Thread.Sleep(30000);
			zigbeeManager.PermitNetworkJoining(true);

			// Configure the HTTP request pipeline.
			app.UseAuthorization();

			app.UseSerilogRequestLogging();

			app.MapControllers();
			app.UseStaticFiles();

			app.Run();
		}

		private static void ConfigureLogging(WebApplicationBuilder builder)
		{
			builder.Host.UseSerilog((ctx, lc) => lc
				.ReadFrom.Configuration(ctx.Configuration)
				.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
				.Enrich.FromLogContext()
				.WriteTo.Console());
		}
	}
}
