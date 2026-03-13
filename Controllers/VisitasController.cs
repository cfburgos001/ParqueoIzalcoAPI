using DataparkBarreraAPI.Models;
using DataparkBarreraAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataparkBarreraAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VisitasController : ControllerBase
    {
        private readonly IVisitasService _visitasService;
        private readonly ILogger<VisitasController> _logger;

        public VisitasController(IVisitasService visitasService, ILogger<VisitasController> logger)
        {
            _visitasService = visitasService;
            _logger = logger;
        }

        // =============================================
        // AUTENTICACIÓN
        // =============================================

        /// <summary>
        /// Valida credenciales del operador para acceso al portal de visitas
        /// Solo permite ADMINISTRADOR y CAJA
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginVisitasResponse>), 200)]
        public async Task<IActionResult> Login([FromBody] LoginVisitasRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                    return BadRequest(new ApiResponse<LoginVisitasResponse> { Exitoso = false, Mensaje = "Usuario y contraseña son requeridos" });

                _logger.LogInformation("Intento de login - Usuario: {Username}", request.Username);

                var resultado = await _visitasService.LoginAsync(request.Username, request.Password);

                if (!resultado.Exitoso)
                {
                    _logger.LogWarning("Login fallido - Usuario: {Username}, Motivo: {Motivo}", request.Username, resultado.Mensaje);
                    return Unauthorized(new ApiResponse<LoginVisitasResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje
                    });
                }

                _logger.LogInformation("✓ Login exitoso - Usuario: {Username}, Tipo: {Tipo}", request.Username, resultado.TipoUsuario);

                return Ok(new ApiResponse<LoginVisitasResponse>
                {
                    Exitoso = true,
                    Mensaje = "Login exitoso",
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login");
                return StatusCode(500, new ApiResponse<LoginVisitasResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }


        // =============================================
        // CATÁLOGOS - TIPOS DE VISITANTE
        // =============================================

        /// <summary>
        /// Lista todos los tipos de visitante activos
        /// </summary>
        [HttpGet("tipos-visitante")]
        [ProducesResponseType(typeof(ApiResponse<List<TipoVisitante>>), 200)]
        public async Task<IActionResult> ListarTiposVisitante()
        {
            try
            {
                var tipos = await _visitasService.ListarTiposVisitanteAsync();
                return Ok(new ApiResponse<List<TipoVisitante>>
                {
                    Exitoso = true,
                    Mensaje = $"{tipos.Count} tipos encontrados",
                    Data = tipos
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tipos de visitante");
                return StatusCode(500, new ApiResponse<List<TipoVisitante>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Crea un nuevo tipo de visitante
        /// </summary>
        [HttpPost("tipos-visitante")]
        [ProducesResponseType(typeof(ApiResponse<VisitanteResponse>), 200)]
        public async Task<IActionResult> CrearTipoVisitante([FromBody] CrearCatalogoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = "El nombre es requerido" });

                var resultado = await _visitasService.CrearTipoVisitanteAsync(request);
                return Ok(new ApiResponse<VisitanteResponse> { Exitoso = resultado.Exitoso, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tipo de visitante");
                return StatusCode(500, new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // CATÁLOGOS - ÁREAS DESTINO
        // =============================================

        /// <summary>
        /// Lista todas las áreas destino activas
        /// </summary>
        [HttpGet("areas-destino")]
        [ProducesResponseType(typeof(ApiResponse<List<AreaDestino>>), 200)]
        public async Task<IActionResult> ListarAreasDestino()
        {
            try
            {
                var areas = await _visitasService.ListarAreasDestinoAsync();
                return Ok(new ApiResponse<List<AreaDestino>>
                {
                    Exitoso = true,
                    Mensaje = $"{areas.Count} áreas encontradas",
                    Data = areas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar áreas destino");
                return StatusCode(500, new ApiResponse<List<AreaDestino>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Crea una nueva área destino
        /// </summary>
        [HttpPost("areas-destino")]
        [ProducesResponseType(typeof(ApiResponse<VisitanteResponse>), 200)]
        public async Task<IActionResult> CrearAreaDestino([FromBody] CrearCatalogoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = "El nombre es requerido" });

                var resultado = await _visitasService.CrearAreaDestinoAsync(request);
                return Ok(new ApiResponse<VisitanteResponse> { Exitoso = resultado.Exitoso, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear área destino");
                return StatusCode(500, new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // VISITANTES (CATÁLOGO DE PERSONAS)
        // =============================================

        /// <summary>
        /// Busca visitantes para autocompletar (escribir nombre, placa o identificación)
        /// </summary>
        [HttpGet("visitantes/buscar")]
        [ProducesResponseType(typeof(ApiResponse<List<Visitante>>), 200)]
        public async Task<IActionResult> BuscarVisitantes([FromQuery] string termino, [FromQuery] int? idTipoVisitante = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(termino) || termino.Length < 2)
                    return Ok(new ApiResponse<List<Visitante>> { Exitoso = true, Mensaje = "Escriba al menos 2 caracteres", Data = new List<Visitante>() });

                var visitantes = await _visitasService.BuscarVisitantesAsync(termino, idTipoVisitante);
                return Ok(new ApiResponse<List<Visitante>>
                {
                    Exitoso = true,
                    Mensaje = $"{visitantes.Count} visitantes encontrados",
                    Data = visitantes
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar visitantes");
                return StatusCode(500, new ApiResponse<List<Visitante>> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Crea un nuevo visitante en el catálogo
        /// </summary>
        [HttpPost("visitantes")]
        [ProducesResponseType(typeof(ApiResponse<CrearVisitanteResponse>), 200)]
        public async Task<IActionResult> CrearVisitante([FromBody] CrearVisitanteRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NombreCompleto))
                    return BadRequest(new ApiResponse<CrearVisitanteResponse> { Exitoso = false, Mensaje = "El nombre es requerido" });

                if (request.IdTipoVisitante <= 0)
                    return BadRequest(new ApiResponse<CrearVisitanteResponse> { Exitoso = false, Mensaje = "El tipo de visitante es requerido" });

                _logger.LogInformation("Creando visitante: {Nombre}", request.NombreCompleto);

                var resultado = await _visitasService.CrearVisitanteAsync(request);
                return Ok(new ApiResponse<CrearVisitanteResponse> { Exitoso = resultado.Exitoso, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear visitante");
                return StatusCode(500, new ApiResponse<CrearVisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Actualiza los datos de un visitante del catálogo
        /// </summary>
        [HttpPut("visitantes")]
        [ProducesResponseType(typeof(ApiResponse<VisitanteResponse>), 200)]
        public async Task<IActionResult> ActualizarVisitante([FromBody] ActualizarVisitanteRequest request)
        {
            try
            {
                if (request.Id <= 0)
                    return BadRequest(new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = "El ID es requerido" });

                var resultado = await _visitasService.ActualizarVisitanteAsync(request);
                return Ok(new ApiResponse<VisitanteResponse> { Exitoso = resultado.Exitoso, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar visitante");
                return StatusCode(500, new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // REGISTRO DE VISITAS
        // =============================================

        /// <summary>
        /// Registra la entrada de un visitante
        /// </summary>
        [HttpPost("entrada")]
        [ProducesResponseType(typeof(ApiResponse<RegistrarEntradaVisitanteResponse>), 200)]
        public async Task<IActionResult> RegistrarEntrada([FromBody] RegistrarEntradaVisitanteRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.NombreVisitante))
                    return BadRequest(new ApiResponse<RegistrarEntradaVisitanteResponse> { Exitoso = false, Mensaje = "El nombre del visitante es requerido" });

                if (request.IdTipoVisitante <= 0)
                    return BadRequest(new ApiResponse<RegistrarEntradaVisitanteResponse> { Exitoso = false, Mensaje = "El tipo de visitante es requerido" });

                _logger.LogInformation("Registrando entrada - Visitante: {Nombre}", request.NombreVisitante);

                var resultado = await _visitasService.RegistrarEntradaAsync(request);
                return Ok(new ApiResponse<RegistrarEntradaVisitanteResponse>
                {
                    Exitoso = resultado.Exitoso,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada de visitante");
                return StatusCode(500, new ApiResponse<RegistrarEntradaVisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Registra la salida de un visitante
        /// </summary>
        [HttpPut("salida/{id}")]
        [ProducesResponseType(typeof(ApiResponse<VisitanteResponse>), 200)]
        public async Task<IActionResult> RegistrarSalida(int id)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = "El ID de la visita es requerido" });

                _logger.LogInformation("Registrando salida - ID Visita: {Id}", id);

                var resultado = await _visitasService.RegistrarSalidaAsync(id);
                return Ok(new ApiResponse<VisitanteResponse> { Exitoso = resultado.Exitoso, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar salida");
                return StatusCode(500, new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lista todas las visitas del día de hoy
        /// </summary>
        [HttpGet("hoy")]
        [ProducesResponseType(typeof(ApiResponse<List<RegistroVisita>>), 200)]
        public async Task<IActionResult> ListarVisitasHoy()
        {
            try
            {
                var visitas = await _visitasService.ListarVisitasHoyAsync();
                return Ok(new ApiResponse<List<RegistroVisita>>
                {
                    Exitoso = true,
                    Mensaje = $"{visitas.Count} visitas del día",
                    Data = visitas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar visitas de hoy");
                return StatusCode(500, new ApiResponse<List<RegistroVisita>> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Lista solo los visitantes que están DENTRO actualmente
        /// </summary>
        [HttpGet("dentro")]
        [ProducesResponseType(typeof(ApiResponse<List<RegistroVisita>>), 200)]
        public async Task<IActionResult> ListarVisitasDentro()
        {
            try
            {
                var visitas = await _visitasService.ListarVisitasDentroAsync();
                return Ok(new ApiResponse<List<RegistroVisita>>
                {
                    Exitoso = true,
                    Mensaje = $"{visitas.Count} visitantes dentro",
                    Data = visitas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar visitantes dentro");
                return StatusCode(500, new ApiResponse<List<RegistroVisita>> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Buscar visitas con filtros (fecha, nombre, placa, tipo, estado)
        /// </summary>
        [HttpPost("buscar")]
        [ProducesResponseType(typeof(ApiResponse<List<RegistroVisita>>), 200)]
        public async Task<IActionResult> BuscarVisitas([FromBody] BuscarVisitasRequest request)
        {
            try
            {
                var visitas = await _visitasService.BuscarVisitasAsync(request);
                return Ok(new ApiResponse<List<RegistroVisita>>
                {
                    Exitoso = true,
                    Mensaje = $"{visitas.Count} visitas encontradas",
                    Data = visitas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar visitas");
                return StatusCode(500, new ApiResponse<List<RegistroVisita>> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Actualiza la observación de una visita
        /// </summary>
        [HttpPut("observacion/{id}")]
        [ProducesResponseType(typeof(ApiResponse<VisitanteResponse>), 200)]
        public async Task<IActionResult> ActualizarObservacion(int id, [FromBody] ActualizarObservacionRequest request)
        {
            try
            {
                if (id <= 0)
                    return BadRequest(new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = "El ID es requerido" });

                var resultado = await _visitasService.ActualizarObservacionAsync(id, request.Observacion);
                return Ok(new ApiResponse<VisitanteResponse> { Exitoso = resultado.Exitoso, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar observación");
                return StatusCode(500, new ApiResponse<VisitanteResponse> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Obtiene estadísticas de visitas del día
        /// </summary>
        [HttpGet("estadisticas")]
        [ProducesResponseType(typeof(ApiResponse<EstadisticasVisitas>), 200)]
        public async Task<IActionResult> ObtenerEstadisticas([FromQuery] DateTime? fecha = null)
        {
            try
            {
                var estadisticas = await _visitasService.ObtenerEstadisticasAsync(fecha);
                return Ok(new ApiResponse<EstadisticasVisitas>
                {
                    Exitoso = true,
                    Mensaje = "Estadísticas obtenidas",
                    Data = estadisticas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return StatusCode(500, new ApiResponse<EstadisticasVisitas> { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // REPORTES DE VEHÍCULOS (IOT_Vehiculos)
        // =============================================

        /// <summary>
        /// Consulta IOT_Vehiculos con filtros dinámicos para reportes de venta
        /// </summary>
        [HttpPost("reporte-vehiculos")]
        [ProducesResponseType(typeof(ApiResponse<List<Dictionary<string, object>>>), 200)]
        public async Task<IActionResult> ReporteVehiculos([FromBody] ReporteVehiculosRequest request)
        {
            try
            {
                _logger.LogInformation("Generando reporte de vehículos - Columnas: {Columnas}", string.Join(",", request.Columnas ?? new List<string>()));

                var resultado = await _visitasService.ReporteVehiculosAsync(request);
                return Ok(new ApiResponse<List<Dictionary<string, object>>>
                {
                    Exitoso = true,
                    Mensaje = $"{resultado.Count} registros encontrados",
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar reporte de vehículos");
                return StatusCode(500, new ApiResponse<List<Dictionary<string, object>>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
        // =============================================
        // DASHBOARD DE VEHÍCULOS
        // =============================================

        /// <summary>
        /// Devuelve los 5 KPIs del dashboard en tiempo real desde IOT_Vehiculos
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<DashboardVehiculosResponse>), 200)]
        public async Task<IActionResult> ObtenerDashboard()
        {
            try
            {
                var resultado = await _visitasService.ObtenerDashboardAsync();
                return Ok(new ApiResponse<DashboardVehiculosResponse>
                {
                    Exitoso = true,
                    Mensaje = "Dashboard obtenido",
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dashboard");
                return StatusCode(500, new ApiResponse<DashboardVehiculosResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }


    }
}