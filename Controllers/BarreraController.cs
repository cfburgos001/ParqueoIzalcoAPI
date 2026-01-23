using DataparkBarreraAPI.Models;
using DataparkBarreraAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataparkBarreraAPI.Controllers
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

        /// <summary>
        /// Obtiene el estado actual de la barrera
        /// </summary>
        /// <returns>Estado de la barrera</returns>
        [HttpGet("estado")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> ObtenerEstado()
        {
            try
            {
                var barrera = await _barreraService.ObtenerEstadoBarreraAsync();

                if (barrera == null)
                {
                    return NotFound(new ApiResponse<Barrera>
                    {
                        Exitoso = false,
                        Mensaje = "No se encontró la barrera"
                    });
                }

                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = $"Estado:  {barrera.EstadoTexto}",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de barrera");
                return StatusCode(500, new ApiResponse<Barrera>
                {
                    Exitoso = false,
                    Mensaje = $"Error:  {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Cierra la barrera (usado por el controlino cuando detecta que pasó el vehículo)
        /// </summary>
        /// <param name="request">Motivo del cierre (opcional)</param>
        /// <returns>Confirmación de cierre</returns>
        [HttpPost("cerrar")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> CerrarBarrera([FromBody] CerrarBarreraRequest? request = null)
        {
            try
            {
                var motivo = request?.Motivo ?? "Vehículo pasó";
                var resultado = await _barreraService.CerrarBarreraAsync(motivo);

                if (!resultado)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Exitoso = false,
                        Mensaje = "No se pudo cerrar la barrera"
                    });
                }

                var barrera = await _barreraService.ObtenerEstadoBarreraAsync();

                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = $"✓ Barrera cerrada - {motivo}",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar barrera");
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Abre la barrera manualmente (emergencias o pruebas)
        /// </summary>
        /// <returns>Confirmación de apertura</returns>
        [HttpPost("abrir")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> AbrirBarrera()
        {
            try
            {
                var resultado = await _barreraService.AbrirBarreraManualAsync();

                if (!resultado)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Exitoso = false,
                        Mensaje = "No se pudo abrir la barrera"
                    });
                }

                var barrera = await _barreraService.ObtenerEstadoBarreraAsync();

                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = "✓ Barrera abierta manualmente",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir barrera");
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Resetea la barrera a estado cerrado
        /// </summary>
        /// <returns>Confirmación de reseteo</returns>
        [HttpPost("resetear")]
        [ProducesResponseType(typeof(ApiResponse<Barrera>), 200)]
        public async Task<IActionResult> ResetearBarrera()
        {
            try
            {
                var resultado = await _barreraService.ResetearBarreraAsync();

                if (!resultado)
                {
                    return BadRequest(new ApiResponse<object>
                    {
                        Exitoso = false,
                        Mensaje = "No se pudo resetear la barrera"
                    });
                }

                var barrera = await _barreraService.ObtenerEstadoBarreraAsync();

                return Ok(new ApiResponse<Barrera>
                {
                    Exitoso = true,
                    Mensaje = "✓ Barrera reseteada correctamente",
                    Data = barrera
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear barrera");
                return StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de uso de la barrera
        /// </summary>
        /// <returns>Estadísticas</returns>
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

        /// <summary>
        /// Obtiene el historial de movimientos recientes
        /// </summary>
        /// <param name="limite">Número de registros a obtener (default: 10)</param>
        /// <returns>Lista de movimientos</returns>
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

        /// <summary>
        /// Prueba la conexión a la base de datos
        /// </summary>
        /// <returns>Estado de la conexión</returns>
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
                    Mensaje = conectado
                        ? "✓ Conexión a Datapark exitosa"
                        : "✗ No se pudo conectar a Datapark",
                    Data = new
                    {
                        Servidor = "10.0.1.39: 1433",
                        BaseDatos = "Datapark",
                        Estado = conectado ? "Conectado" : "Desconectado"
                    }
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

        /// <summary>
        /// Registra un log en IOT_Logs - Endpoint genérico para el Controllino
        /// Permite escribir cualquier tipo de log con los datos que se necesiten
        /// </summary>
        /// <param name="request">Datos del log a registrar</param>
        /// <returns>Confirmación del registro</returns>
        [HttpPost("registro-log")]
        [ProducesResponseType(typeof(ApiResponse<RegistroLogResponse>), 200)]
        public async Task<IActionResult> RegistrarLog([FromBody] RegistroLogRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse<RegistroLogResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El request no puede ser nulo"
                    });
                }

                _logger.LogInformation(
                    "Registrando log - TipoLog: {TipoLog}, Dispositivo: {Dispositivo}, Placa: {Placa}, Datos: {Datos}",
                    request.IdTipoLog,
                    request.IdDispositivo,
                    request.Placa ?? "N/A",
                    request.DatosAdicionales ?? "N/A"
                );

                var resultado = await _barreraService.RegistrarLogAsync(request);

                if (resultado.Exitoso)
                {
                    _logger.LogInformation("✓ Log registrado exitosamente - ID: {IdLog}", resultado.IdLog);
                }

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
                    Mensaje = $"Error:  {ex.Message}"
                });
            }
        }
    }
}