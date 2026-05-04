using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccesoController : ControllerBase
    {
        private readonly IAccesoService _accesoService;
        private readonly ILogger<AccesoController> _logger;

        public AccesoController(IAccesoService accesoService, ILogger<AccesoController> logger)
        {
            _accesoService = accesoService;
            _logger = logger;
        }

        /// <summary>
        /// Valida si una tarjeta tiene acceso en un dispositivo (sin registrar movimiento).
        /// Usado por el hardware para pre-verificar antes de abrir la barrera.
        /// </summary>
        [HttpPost("validar")]
        [ProducesResponseType(typeof(ApiResponse<ValidacionAccesoResponse>), 200)]
        public async Task<IActionResult> ValidarAcceso([FromBody] ValidarAccesoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.NumeroTarjeta))
                    return BadRequest(new ApiResponse<ValidacionAccesoResponse>
                    { Exitoso = false, Mensaje = "NumeroTarjeta es requerido" });

                _logger.LogInformation("Validando acceso tarjeta {Tarjeta} en dispositivo {Disp}",
                    request.NumeroTarjeta, request.IdDispositivo);

                var resultado = await _accesoService.ValidarAccesoTarjetaAsync(request);
                return Ok(new ApiResponse<ValidacionAccesoResponse>
                {
                    Exitoso = resultado.Acceso,
                    Mensaje = resultado.Mensaje,
                    Data    = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar acceso");
                return StatusCode(500, new ApiResponse<ValidacionAccesoResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registra la entrada de un vehículo usando tarjeta RFID.
        /// Valida acceso y genera el ticket de entrada.
        /// </summary>
        [HttpPost("entrada")]
        [ProducesResponseType(typeof(ApiResponse<ValidacionAccesoResponse>), 200)]
        public async Task<IActionResult> RegistrarEntrada([FromBody] RegistrarMovimientoTarjetaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.NumeroTarjeta))
                    return BadRequest(new ApiResponse<ValidacionAccesoResponse>
                    { Exitoso = false, Mensaje = "NumeroTarjeta es requerido" });

                _logger.LogInformation("Registrando entrada tarjeta {Tarjeta} en dispositivo {Disp}",
                    request.NumeroTarjeta, request.IdDispositivo);

                var resultado = await _accesoService.RegistrarEntradaTarjetaAsync(request);
                return Ok(new ApiResponse<ValidacionAccesoResponse>
                {
                    Exitoso = resultado.Acceso,
                    Mensaje = resultado.Mensaje,
                    Data    = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada de tarjeta");
                return StatusCode(500, new ApiResponse<ValidacionAccesoResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registra la salida de un vehículo usando tarjeta RFID.
        /// Valida acceso y cierra el ticket de estancia.
        /// </summary>
        [HttpPost("salida")]
        [ProducesResponseType(typeof(ApiResponse<ValidacionAccesoResponse>), 200)]
        public async Task<IActionResult> RegistrarSalida([FromBody] RegistrarMovimientoTarjetaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.NumeroTarjeta))
                    return BadRequest(new ApiResponse<ValidacionAccesoResponse>
                    { Exitoso = false, Mensaje = "NumeroTarjeta es requerido" });

                _logger.LogInformation("Registrando salida tarjeta {Tarjeta} en dispositivo {Disp}",
                    request.NumeroTarjeta, request.IdDispositivo);

                var resultado = await _accesoService.RegistrarSalidaTarjetaAsync(request);
                return Ok(new ApiResponse<ValidacionAccesoResponse>
                {
                    Exitoso = resultado.Acceso,
                    Mensaje = resultado.Mensaje,
                    Data    = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar salida de tarjeta");
                return StatusCode(500, new ApiResponse<ValidacionAccesoResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }
    }
}
