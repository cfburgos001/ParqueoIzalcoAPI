using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Models.ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TicketsController : ControllerBase
    {
        private readonly ITicketsService _ticketsService;
        private readonly ILogger<TicketsController> _logger;

        public TicketsController(ITicketsService ticketsService, ILogger<TicketsController> logger)
        {
            _ticketsService = ticketsService;
            _logger = logger;
        }

        /// <summary>
        /// Lista todos los tickets que están DENTRO del parqueo
        /// y cuya fecha de entrada es anterior al día de hoy.
        /// Solo accesible para ADMINISTRADOR.
        /// </summary>
        [HttpGet("antiguos")]
        [ProducesResponseType(typeof(ApiResponse<List<TicketAntiguo>>), 200)]
        public async Task<IActionResult> ListarTicketsAntiguos()
        {
            try
            {
                var tickets = await _ticketsService.ListarTicketsAntiguosAsync();

                return Ok(new ApiResponse<List<TicketAntiguo>>
                {
                    Exitoso = true,
                    Mensaje = $"{tickets.Count} tickets antiguos pendientes",
                    Data = tickets
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tickets antiguos");
                return StatusCode(500, new ApiResponse<List<TicketAntiguo>>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Cierra un ticket antiguo individual con el monto indicado.
        /// </summary>
        [HttpPost("cerrar")]
        [ProducesResponseType(typeof(ApiResponse<CerrarTicketResponse>), 200)]
        public async Task<IActionResult> CerrarTicket([FromBody] CerrarTicketRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<CerrarTicketResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El request no puede ser nulo"
                    });

                if (request.Monto < 0)
                    return BadRequest(new ApiResponse<CerrarTicketResponse>
                    {
                        Exitoso = false,
                        Mensaje = "El monto no puede ser negativo"
                    });

                _logger.LogInformation(
                    "Cerrando ticket ID: {Id}, Monto: ${Monto}, Operador: {Operador}",
                    request.Id, request.Monto, request.NombreOperador);

                var resultado = await _ticketsService.CerrarTicketAsync(request);

                if (!resultado.Exitoso)
                    return BadRequest(new ApiResponse<CerrarTicketResponse>
                    {
                        Exitoso = false,
                        Mensaje = resultado.Mensaje,
                        Data = resultado
                    });

                return Ok(new ApiResponse<CerrarTicketResponse>
                {
                    Exitoso = true,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar ticket ID: {Id}", request?.Id);
                return StatusCode(500, new ApiResponse<CerrarTicketResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Cierra múltiples tickets antiguos en una sola operación.
        /// Cada ticket tiene su propio monto.
        /// </summary>
        [HttpPost("cerrar-masivo")]
        [ProducesResponseType(typeof(ApiResponse<CerrarTicketsMasivosResponse>), 200)]
        public async Task<IActionResult> CerrarTicketsMasivos([FromBody] CerrarTicketsMasivosRequest request)
        {
            try
            {
                if (request == null || request.Tickets == null || request.Tickets.Count == 0)
                    return BadRequest(new ApiResponse<CerrarTicketsMasivosResponse>
                    {
                        Exitoso = false,
                        Mensaje = "Debe seleccionar al menos un ticket"
                    });

                _logger.LogInformation(
                    "Cierre masivo de {Count} tickets, Operador: {Operador}",
                    request.Tickets.Count, request.NombreOperador);

                var resultado = await _ticketsService.CerrarTicketsMasivosAsync(request);

                return Ok(new ApiResponse<CerrarTicketsMasivosResponse>
                {
                    Exitoso = resultado.Exitoso,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en cierre masivo de tickets");
                return StatusCode(500, new ApiResponse<CerrarTicketsMasivosResponse>
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                });
            }
        }
    }
}