using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
    {
        private readonly ICuentasService _cuentasService;
        private readonly ILogger<TarjetasController> _logger;

        public TarjetasController(ICuentasService cuentasService, ILogger<TarjetasController> logger)
        {
            _cuentasService = cuentasService;
            _logger = logger;
        }

        /// <summary>Crea una nueva tarjeta para una cuenta</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TarjetaResponse>), 200)]
        public async Task<IActionResult> CrearTarjeta([FromBody] CrearTarjetaRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.NumeroTarjeta) || string.IsNullOrWhiteSpace(request.NombreUsuario))
                    return BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = "NumeroTarjeta y NombreUsuario son requeridos" });

                _logger.LogInformation("Creando tarjeta para cuenta {Cuenta}", request.IdCuenta);
                var resultado = await _cuentasService.CrearTarjetaAsync(request);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarjetaResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Actualiza los datos de una tarjeta</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TarjetaResponse>), 200)]
        public async Task<IActionResult> ActualizarTarjeta(int id, [FromBody] ActualizarTarjetaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = "Request nulo" });

                request.Id = id;
                var resultado = await _cuentasService.ActualizarTarjetaAsync(request);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarjetaResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Activa o desactiva una tarjeta</summary>
        [HttpPatch("{id}/toggle")]
        [ProducesResponseType(typeof(ApiResponse<TarjetaResponse>), 200)]
        public async Task<IActionResult> ToggleTarjeta(int id)
        {
            try
            {
                var resultado = await _cuentasService.ToggleTarjetaAsync(id);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarjetaResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<TarjetaResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }
    }
}
