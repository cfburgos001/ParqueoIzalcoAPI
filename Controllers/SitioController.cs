using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SitioController : ControllerBase
    {
        private readonly ISitioService _sitio;
        private readonly ILogger<SitioController> _logger;

        public SitioController(ISitioService sitio, ILogger<SitioController> logger)
        {
            _sitio = sitio;
            _logger = logger;
        }

        /// <summary>
        /// Devuelve la configuración del sitio (desde cache en memoria).
        /// El frontend lo llama al iniciar para personalizar toda la UI.
        /// </summary>
        [HttpGet]
        public IActionResult ObtenerConfig()
        {
            var config = _sitio.ObtenerConfig();
            return Ok(new ApiResponse<ConfigSitio>
            {
                Exitoso = true,
                Mensaje = "Configuración obtenida",
                Data = config
            });
        }

        /// <summary>
        /// Recarga la config desde la BD (útil sin reiniciar la app).
        /// </summary>
        [HttpPost("recargar")]
        public async Task<IActionResult> Recargar()
        {
            var config = await _sitio.CargarDesdeDBAsync();
            return Ok(new ApiResponse<ConfigSitio>
            {
                Exitoso = true,
                Mensaje = "Configuración recargada desde la base de datos",
                Data = config
            });
        }

        /// <summary>
        /// Actualiza los datos del sitio (solo ADMINISTRADOR).
        /// </summary>
        [HttpPut]
        public async Task<IActionResult> Actualizar([FromBody] ActualizarSitioRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NombreComercial) ||
                string.IsNullOrWhiteSpace(request.RazonSocial))
            {
                return BadRequest(new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = "NombreComercial y RazonSocial son requeridos"
                });
            }

            var ok = await _sitio.ActualizarAsync(request);
            return ok
                ? Ok(new ApiResponse<ConfigSitio>
                {
                    Exitoso = true,
                    Mensaje = "Configuración actualizada correctamente",
                    Data = _sitio.ObtenerConfig()
                })
                : StatusCode(500, new ApiResponse<object>
                {
                    Exitoso = false,
                    Mensaje = "Error al actualizar la configuración"
                });
        }
    }
}