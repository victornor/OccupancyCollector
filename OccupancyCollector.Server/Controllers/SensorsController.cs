using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OccupancyCollector.Services.Zigbee;

namespace OccupancyCollector
{
	[ApiController]
	[Route("[controller]")]
	public class SensorsController : ControllerBase
	{
		private readonly ILogger<SensorsController> _logger;
		private readonly ZigbeeManager _zigbeeManager;
		
		public SensorsController(ILogger<SensorsController> logger, ZigbeeManager zigbeeManager)
		{
			_logger = logger;
			_zigbeeManager = zigbeeManager;
		}

		[HttpGet]
		public async Task<ActionResult> Get()
		{
			return Ok(_zigbeeManager.GetSensors());
		}
	}
}
