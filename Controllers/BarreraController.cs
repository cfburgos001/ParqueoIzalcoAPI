using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BarreraController : ControllerBase
    {
        private readonly IBarreraService _barreraService;
        private readonly IDatabaseService _databaseService;
        private readonly ILogger<BarreraController> _logger;

        public BarreraController(
            IBarreraService barreraService,
            IDatabaseService databaseService,
            ILogger<BarreraController> logger)
        {
            _barreraService = barreraService;
            _databaseService = databaseService;
            _logger = logger;
        }

        /// <summary>Lista todas las barreras registradas en IOT_Barrera</summary>
        [HttpGet("listar")]
        [ProducesResponseType(typeof(ApiResponse<List<Barrera>>), 200)]
        public async Task<IActionResult> ListarBarreras()
        {
            try
            {
                var barreras = await _barreraService.ListarBarrerasAsync();
                return Ok(new ApiResponse<List<Barrera>>
                {
                    Exitoso = true,
                    Mensaje = $"{barreras.Count} barrera(s) encontrada(s)",
                    Data = barreras
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar barreras");
                return StatusCode(500, new ApiResponse<List<Barrera>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Obtiene el estado de una barrera. ?idBarrera=1 por defecto.</summary>
        [HttpGet("estado")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> ObtenerEstado([FromQuery] int idBarrera = 1)
        {
            try
            {
                var barrera = await _barreraService.ObtenerEstadoBarreraAsync(idBarrera);

                if (barrera == null)
                    return NotFound(new ApiResponse<Barrera>
                    {
                        Exitoso = false,
                        Mensaje = $"No se encontró la barrera con ID {idBarrera}"
                    });

                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = $"Estado: {barrera.EstadoTexto}",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de barrera {IdBarrera}", idBarrera);
                return StatusCode(500, new ApiResponse<Barrera>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Cierra una barrera. ?idBarrera=1 por defecto.</summary>
        [HttpPost("cerrar")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> CerrarBarrera(
            [FromBody] CerrarBarreraRequest? request = null,
            [FromQuery] int idBarrera = 1)
        {
            try
            {
                var motivo = request?.Motivo ?? "Vehículo pasó";
                var resultado = await _barreraService.CerrarBarreraAsync(motivo, idBarrera);

                if (!resultado)
                    return BadRequest(new ApiResponse<object>
                    {
                        Exitoso = false,
                        Mensaje = $"No se pudo cerrar la barrera {idBarrera}"
                    });

                var barrera = await _barreraService.ObtenerEstadoBarreraAsync(idBarrera);
                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = $"✓ Barrera {idBarrera} cerrada - {motivo}",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar barrera {IdBarrera}", idBarrera);
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Abre una barrera manualmente. ?idBarrera=1 por defecto.</summary>
        [HttpPost("abrir")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> AbrirBarrera([FromQuery] int idBarrera = 1)
        {
            try
            {
                var resultado = await _barreraService.AbrirBarreraManualAsync(idBarrera);

                if (!resultado)
                    return BadRequest(new ApiResponse<object>
                    {
                        Exitoso = false,
                        Mensaje = $"No se pudo abrir la barrera {idBarrera}"
                    });

                var barrera = await _barreraService.ObtenerEstadoBarreraAsync(idBarrera);
                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = $"✓ Barrera {idBarrera} abierta manualmente",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir barrera {IdBarrera}", idBarrera);
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Resetea una barrera a estado cerrado. ?idBarrera=1 por defecto.</summary>
        [HttpPost("resetear")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> ResetearBarrera([FromQuery] int idBarrera = 1)
        {
            try
            {
                var resultado = await _barreraService.ResetearBarreraAsync(idBarrera);

                if (!resultado)
                    return BadRequest(new ApiResponse<object>
                    {
                        Exitoso = false,
                        Mensaje = $"No se pudo resetear la barrera {idBarrera}"
                    });

                var barrera = await _barreraService.ObtenerEstadoBarreraAsync(idBarrera);
                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = $"✓ Barrera {idBarrera} reseteada correctamente",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear barrera {IdBarrera}", idBarrera);
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Obtiene estadísticas de uso de las barreras</summary>
        [HttpGet("estadisticas")]
        [ProducesResponseType(typeof(ApiResponse<BarreraEstadisticas>), 200)]
        public async Task<IActionResult> ObtenerEstadisticas()
        {
            try
            {
                var estadisticas = await _barreraService.ObtenerEstadisticasAsync();
                return Ok(new ApiResponse<BarreraEstadisticas>
                {
                    Exitoso = true,
                    Mensaje = "Estadísticas obtenidas",
                    Data = estadisticas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return StatusCode(500, new ApiResponse<BarreraEstadisticas>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Obtiene el historial de movimientos recientes</summary>
        [HttpGet("historial")]
        [ProducesResponseType(typeof(ApiResponse<List<BarreraLog>>), 200)]
        public async Task<IActionResult> ObtenerHistorial([FromQuery] int limite = 10)
        {
            try
            {
                var historial = await _barreraService.ObtenerHistorialAsync(limite);
                return Ok(new ApiResponse<List<BarreraLog>>
                {
                    Exitoso = true,
                    Mensaje = $"{historial.Count} registros encontrados",
                    Data = historial
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial");
                return StatusCode(500, new ApiResponse<List<BarreraLog>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Prueba la conexión a la base de datos</summary>
        [HttpGet("test-conexion")]
        [ProducesResponseType(typeof(ApiResponse<object>), 200)]
        public async Task<IActionResult> TestConexion()
        {
            try
            {
                var conectado = await _databaseService.TestConnectionAsync();
                return Ok(new ApiResponse<object>
                {
                    Exitoso = conectado,
                    Mensaje = conectado ? "✓ Conexión a Datapark exitosa" : "✗ No se pudo conectar a Datapark",
                    Data = new { Estado = conectado ? "Conectado" : "Desconectado" }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al probar conexión");
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>Registra un log genérico en IOT_Logs (para el Controllino)</summary>
        [HttpPost("registro-log")]
        [ProducesResponseType(typeof(ApiResponse<RegistroLogResponse>), 200)]
        public async Task<IActionResult> RegistrarLog([FromBody] RegistroLogRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<RegistroLogResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El request no puede ser nulo"
                    });

                _logger.LogInformation(
                    "Registrando log - TipoLog: {TipoLog}, Dispositivo: {Dispositivo}, Placa: {Placa}",
                    request.IdTipoLog, request.IdDispositivo, request.Placa ?? "N/A");

                var resultado = await _barreraService.RegistrarLogAsync(request);

                return Ok(new ApiResponse<RegistroLogResponse>
                {
                    Exitoso = resultado.Exitoso,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar log");
                return StatusCode(500, new ApiResponse<RegistroLogResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
    }
}
