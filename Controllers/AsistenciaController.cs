using Microsoft.AspNetCore.Mvc;
using ParqueoIzalcoAPI.Services;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AsistenciaController : ControllerBase
    {
        private readonly IAsistenciaService _asistenciaService;

        public AsistenciaController(IAsistenciaService asistenciaService)
        {
            _asistenciaService = asistenciaService;
        }

        /// <summary>
        /// POST /api/asistencia/ticket-extraviado
        /// Genera un ticket de reemplazo ($10 fijo).
        /// Body: { "idDispositivo": "PS_05", "idOperador": 1 }
        /// </summary>
        [HttpPost("ticket-extraviado")]
        public async Task<IActionResult> CrearTicketExtraviado([FromBody] TicketExtraviadoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdDispositivo) || request.IdOperador <= 0)
                return BadRequest(new { exitoso = false, mensaje = "IdDispositivo e IdOperador son requeridos." });

            var resultado = await _asistenciaService.CrearTicketExtraviadoAsync(request.IdDispositivo, request.IdOperador);

            if (!resultado.Exitoso)
                return BadRequest(resultado);

            return Ok(resultado);
        }

        /// <summary>
        /// POST /api/asistencia/ticket-extraviado-pesado
        /// Genera un ticket de reemplazo con tarifa especial ($25 fijo).
        /// Body: { "idDispositivo": "PS_05", "idOperador": 1 }
        /// </summary>
        [HttpPost("ticket-extraviado-pesado")]
        public async Task<IActionResult> CrearTicketExtraviadoPesado([FromBody] TicketExtraviadoRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.IdDispositivo) || request.IdOperador <= 0)
                return BadRequest(new { exitoso = false, mensaje = "IdDispositivo e IdOperador son requeridos." });

            var resultado = await _asistenciaService.CrearTicketExtraviadoPesadoAsync(request.IdDispositivo, request.IdOperador);

            if (!resultado.Exitoso)
                return BadRequest(resultado);

            return Ok(resultado);
        }
    }

    // ── Modelos ───────────────────────────────────────────────────────────────
    public class TicketExtraviadoRequest
    {
        public string IdDispositivo { get; set; } = string.Empty;
        public int IdOperador { get; set; }
    }

    public class TicketExtraviadoResult
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? Placa { get; set; }
        public string? CodigoBarras { get; set; }
        public decimal? Monto { get; set; }
    }
}