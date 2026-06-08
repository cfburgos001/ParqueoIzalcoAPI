using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PagoController : ControllerBase
    {
        private readonly IPagoService _pagoService;
        private readonly ILogger<PagoController> _logger;

        public PagoController(IPagoService pagoService, ILogger<PagoController> logger)
        {
            _pagoService = pagoService;
            _logger = logger;
        }

        /// <summary>
        /// Consulta un vehículo por placa (código de barras del ticket) y calcula el monto a pagar
        /// Usado por la PayStation cuando se escanea el ticket
        /// NOTA: El código de barras impreso en el ticket ES la placa del vehículo
        /// </summary>
        /// <param name="placa">Placa del vehículo (código de barras del ticket)</param>
        /// <returns>Información del vehículo y monto a pagar</returns>
        [HttpGet("consultar/{placa}")]
        [ProducesResponseType(typeof(ApiResponse<ConsultaPagoResponse>), 200)]
        public async Task<IActionResult> ConsultarPorPlaca( string placa,[FromQuery] string idDispositivo = "PS_05")
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placa))
                {
                    return BadRequest(new ApiResponse<ConsultaPagoResponse>
                    {
                        Exitoso = false,
                        Mensaje = "La placa es requerida"
                    });
                }

                _logger.LogInformation("Consultando pago por placa (ticket escaneado): {Placa}", placa);

                var resultado = await _pagoService.ConsultarPorPlacaAsync(placa, idDispositivo);

                if (!resultado.Exitoso)
                {
                    return NotFound(new ApiResponse<ConsultaPagoResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }

                return Ok(new ApiResponse<ConsultaPagoResponse>
                {
                    Exitoso = true,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar pago por placa");
                return StatusCode(500, new ApiResponse<ConsultaPagoResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Registra un pago desde la PayStation
        /// </summary>
        /// <param name="request">Datos del pago</param>
        /// <returns>Confirmación del pago registrado</returns>
        [HttpPost("registrar")]
        [ProducesResponseType(typeof(ApiResponse<RegistrarPagoResponse>), 200)]
        public async Task<IActionResult> RegistrarPago([FromBody] RegistrarPagoRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse<RegistrarPagoResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El request no puede ser nulo"
                    });
                }

                _logger.LogInformation(
                    "Registrando pago - Placa: {Placa}, Monto: ${Monto}, Device: {Device}",
                    request.Placa, request.Monto, request.IdPayDevice
                );

                var resultado = await _pagoService.RegistrarPagoAsync(request);

                if (!resultado.Exitoso)
                {
                    return BadRequest(new ApiResponse<RegistrarPagoResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }

                return Ok(new ApiResponse<RegistrarPagoResponse>
                {
                    Exitoso = true,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar pago");
                return StatusCode(500, new ApiResponse<RegistrarPagoResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Verifica el estado de pago de un vehículo por placa
        /// </summary>
        /// <param name="placa">Placa del vehículo</param>
        /// <returns>Estado de pago del vehículo</returns>
        [HttpGet("verificar/{placa}")]
        [ProducesResponseType(typeof(ApiResponse<VerificarPagoResponse>), 200)]
        public async Task<IActionResult> VerificarPagoPorPlaca(string placa)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(placa))
                {
                    return BadRequest(new ApiResponse<VerificarPagoResponse>
                    {
                        Exitoso = false,
                        Mensaje = "La placa es requerida"
                    });
                }

                _logger.LogInformation("Verificando pago por placa: {Placa}", placa);

                var resultado = await _pagoService.VerificarPagoPorPlacaAsync(placa);

                if (!resultado.Exitoso)
                {
                    return NotFound(new ApiResponse<VerificarPagoResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }

                return Ok(new ApiResponse<VerificarPagoResponse>
                {
                    Exitoso = true,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar pago por placa");
                return StatusCode(500, new ApiResponse<VerificarPagoResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
        /// <summary>
        /// Registra una operación en IOT_AperturaCierre
        /// </summary>
        [HttpPost("apertura-cierre")]
        [ProducesResponseType(typeof(ApiResponse<AperturaCierreResponse>), 200)]
        public async Task<IActionResult> RegistrarAperturaCierre([FromBody] AperturaCierreRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new ApiResponse<AperturaCierreResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El request no puede ser nulo"
                    });
                }

                _logger.LogInformation("Registrando {Tipo} - Dispositivo: {Dispositivo}",
                    request.TipoOperacion, request.IdDispositivo);

                var resultado = await _pagoService.RegistrarAperturaCierreAsync(request);

                if (!resultado.Exitoso)
                {
                    return BadRequest(new ApiResponse<AperturaCierreResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });
                }

                return Ok(new ApiResponse<AperturaCierreResponse>
                {
                    Exitoso = true,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar apertura/cierre");
                return StatusCode(500, new ApiResponse<AperturaCierreResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
    }
}