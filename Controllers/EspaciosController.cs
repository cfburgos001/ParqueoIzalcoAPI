using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EspaciosController : ControllerBase
    {
        private readonly IEspaciosService _espaciosService;
        private readonly ILogger<EspaciosController> _logger;

        public EspaciosController(IEspaciosService espaciosService, ILogger<EspaciosController> logger)
        {
            _espaciosService = espaciosService;
            _logger = logger;
        }

        /// <summary>
        /// Retorna la disponibilidad actual del parqueo:
        /// TotalEspacios, VehiculosDentro, EspaciosDisponibles y EstadoCapacidad.
        /// </summary>
        [HttpGet("disponibilidad")]
        [ProducesResponseType(typeof(ApiResponse<EspaciosDisponiblesResponse>), 200)]
        public async Task<IActionResult> ObtenerDisponibilidad()
        {
            try
            {
                var resultado = await _espaciosService.ObtenerDisponibilidadAsync();

                return Ok(new ApiResponse<EspaciosDisponiblesResponse>
                {
                    Exitoso = true,
                    Mensaje = $"{resultado.EspaciosDisponibles} espacio(s) disponible(s) de {resultado.TotalEspacios}",
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad de espacios");
                return StatusCode(500, new ApiResponse<EspaciosDisponiblesResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
    }
}