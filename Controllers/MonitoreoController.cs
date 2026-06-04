using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MonitoreoController : ControllerBase
    {
        private readonly IMonitoreoService _monitoreoService;
        private readonly ILogger<MonitoreoController> _logger;

        public MonitoreoController(IMonitoreoService monitoreoService, ILogger<MonitoreoController> logger)
        {
            _monitoreoService = monitoreoService;
            _logger = logger;
        }

        /// <summary>
        /// Feed de IOT_Logs para el módulo de monitoreo web.
        /// Llama a IOT_sp_MonitoreoLogs.
        /// Parámetros opcionales:
        ///   top       = cuántos registros (default 200, máx 500)
        ///   prioridad = ALTA | MEDIA | BAJA  (sin filtro = todos)
        ///   ultimoId  = devuelve solo logs con Id > ultimoId (polling incremental)
        /// </summary>
        [HttpGet("logs")]
        [ProducesResponseType(typeof(ApiResponse<List<MonitoreoLogItem>>), 200)]
        public async Task<IActionResult> ObtenerLogs(
            [FromQuery] int top = 200,
            [FromQuery] string? prioridad = null,
            [FromQuery] int? ultimoId = null)
        {
            try
            {
                var logs = await _monitoreoService.ObtenerLogsAsync(top, prioridad, ultimoId);
                return Ok(new ApiResponse<List<MonitoreoLogItem>>
                {
                    Exitoso = true,
                    Mensaje = $"{logs.Count} evento(s)",
                    Data = logs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener logs de monitoreo");
                return StatusCode(500, new ApiResponse<List<MonitoreoLogItem>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
    }
}