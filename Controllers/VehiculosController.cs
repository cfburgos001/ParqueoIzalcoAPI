using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VehiculosController : ControllerBase
    {
        private readonly IVehiculosService _vehiculosService;
        private readonly ILogger<VehiculosController> _logger;

        public VehiculosController(IVehiculosService vehiculosService, ILogger<VehiculosController> logger)
        {
            _vehiculosService = vehiculosService;
            _logger = logger;
        }

        /// <summary>
        /// Endpoint para la máquina Entry (Genera QR automáticamente)
        /// </summary>
        [HttpPost("entrada-automatica")]
        [ProducesResponseType(typeof(ApiResponse<EntradaAutomaticaResponse>), 200)]
        public async Task<IActionResult> RegistrarEntradaAutomatica([FromBody] EntradaAutomaticaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.IdDispositivo))
                {
                    return BadRequest(new ApiResponse<EntradaAutomaticaResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El IdDispositivo es requerido"
                    });
                }

                _logger.LogInformation("Registrando entrada automática desde {Dispositivo} con tarifa {Tarifa}", request.IdDispositivo, request.StrRateKey);

                var resultado = await _vehiculosService.RegistrarEntradaAutomaticaAsync(request);

                if (!resultado.Exitoso)
                {
                    return BadRequest(new ApiResponse<EntradaAutomaticaResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }

                return Ok(new ApiResponse<EntradaAutomaticaResponse>
                {
                    Exitoso = true,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en entrada automática");
                return StatusCode(500, new ApiResponse<EntradaAutomaticaResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Procesa la salida de un vehículo desde un Exit (Máquina lectora de QR)
        /// Valida tiempos de gracia. Si todo está en regla da éxito (para abrir pluma), sino deniega.
        /// </summary>
        [HttpPost("salida-automatica")]
        [ProducesResponseType(typeof(ApiResponse<SalidaAutomaticaResponse>), 200)]
        public async Task<IActionResult> ProcesarSalidaAutomatica([FromBody] SalidaAutomaticaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Placa) || string.IsNullOrWhiteSpace(request.IdDispositivo))
                {
                    return BadRequest(new ApiResponse<SalidaAutomaticaResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El código de barras (Placa) y el IdDispositivo son requeridos"
                    });
                }

                _logger.LogInformation("Procesando salida automática - Dispositivo: {Dispositivo}, Ticket: {Placa}", request.IdDispositivo, request.Placa);

                var resultado = await _vehiculosService.ProcesarSalidaAutomaticaAsync(request);

                // Si fue exitoso, es porque se le da salida libre (se abre barrera)
                if (resultado.Exitoso)
                {
                    return Ok(new ApiResponse<SalidaAutomaticaResponse>
                    {
                        Exitoso = true,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }
                else
                {
                    // Si es falso, es porque debe pagar o se generó reingreso (no abrir barrera)
                    return BadRequest(new ApiResponse<SalidaAutomaticaResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico en salida automática");
                return StatusCode(500, new ApiResponse<SalidaAutomaticaResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
    }
}