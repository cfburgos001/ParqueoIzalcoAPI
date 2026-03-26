using DataparkBarreraAPI.Models;
using DataparkBarreraAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace DataparkBarreraAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarifasController : ControllerBase
    {
        private readonly ITarifasService _tarifasService;
        private readonly ILogger<TarifasController> _logger;

        public TarifasController(ITarifasService tarifasService, ILogger<TarifasController> logger)
        {
            _tarifasService = tarifasService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> ListarTarifas()
        {
            try
            {
                var tarifas = await _tarifasService.ListarTarifasAsync();
                return Ok(new ApiResponse<List<Tarifa>>
                {
                    Exitoso = true,
                    Mensaje = $"{tarifas.Count} tarifas",
                    Data = tarifas
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<List<Tarifa>>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> ActualizarTarifa(int id, [FromBody] ActualizarTarifaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<TarifaResponse>
                    { Exitoso = false, Mensaje = "Request nulo" });

                request.Id = id;
                _logger.LogInformation("Actualizando tarifa ID: {Id}", id);

                var resultado = await _tarifasService.ActualizarTarifaAsync(request);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarifaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<TarifaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<TarifaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }
    }
}