using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CuentasController : ControllerBase
    {
        private readonly ICuentasService _cuentasService;
        private readonly ILogger<CuentasController> _logger;

        public CuentasController(ICuentasService cuentasService, ILogger<CuentasController> logger)
        {
            _cuentasService = cuentasService;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // CUENTAS
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Lista todas las cuentas corporativas</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Cuenta>>), 200)]
        public async Task<IActionResult> ListarCuentas()
        {
            try
            {
                var cuentas = await _cuentasService.ListarCuentasAsync();
                return Ok(new ApiResponse<List<Cuenta>>
                {
                    Exitoso = true,
                    Mensaje = $"{cuentas.Count} cuentas encontradas",
                    Data    = cuentas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar cuentas");
                return StatusCode(500, new ApiResponse<List<Cuenta>>
                { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Obtiene los datos de una cuenta por ID</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<Cuenta>), 200)]
        public async Task<IActionResult> ObtenerCuenta(int id)
        {
            try
            {
                var cuenta = await _cuentasService.ObtenerCuentaAsync(id);
                if (cuenta == null)
                    return NotFound(new ApiResponse<Cuenta> { Exitoso = false, Mensaje = "Cuenta no encontrada" });

                return Ok(new ApiResponse<Cuenta> { Exitoso = true, Mensaje = "Cuenta encontrada", Data = cuenta });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<Cuenta> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Crea una nueva cuenta corporativa</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CuentaResponse>), 200)]
        public async Task<IActionResult> CrearCuenta([FromBody] CrearCuentaRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.CodigoUnico) || string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = "CodigoUnico y Nombre son requeridos" });

                _logger.LogInformation("Creando cuenta nueva");
                var resultado = await _cuentasService.CrearCuentaAsync(request);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<CuentaResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Actualiza los datos de una cuenta</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<CuentaResponse>), 200)]
        public async Task<IActionResult> ActualizarCuenta(int id, [FromBody] ActualizarCuentaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = "Request nulo" });

                request.Id = id;
                var resultado = await _cuentasService.ActualizarCuentaAsync(request);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<CuentaResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Activa o desactiva una cuenta</summary>
        [HttpPatch("{id}/toggle")]
        [ProducesResponseType(typeof(ApiResponse<CuentaResponse>), 200)]
        public async Task<IActionResult> ToggleCuenta(int id)
        {
            try
            {
                var resultado = await _cuentasService.ToggleCuentaAsync(id);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<CuentaResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<CuentaResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // TARJETAS DE LA CUENTA
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Lista todas las tarjetas de una cuenta</summary>
        [HttpGet("{id}/tarjetas")]
        [ProducesResponseType(typeof(ApiResponse<List<Tarjeta>>), 200)]
        public async Task<IActionResult> ListarTarjetas(int id)
        {
            try
            {
                var tarjetas = await _cuentasService.ListarTarjetasDeCuentaAsync(id);
                return Ok(new ApiResponse<List<Tarjeta>>
                {
                    Exitoso = true,
                    Mensaje = $"{tarjetas.Count} tarjetas",
                    Data    = tarjetas
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<List<Tarjeta>> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // DISPOSITIVOS DE LA CUENTA
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Lista los dispositivos permitidos para una cuenta</summary>
        [HttpGet("{id}/dispositivos")]
        [ProducesResponseType(typeof(ApiResponse<List<CuentaDispositivo>>), 200)]
        public async Task<IActionResult> ListarDispositivos(int id)
        {
            try
            {
                var dispositivos = await _cuentasService.ListarDispositivosDeCuentaAsync(id);
                return Ok(new ApiResponse<List<CuentaDispositivo>>
                {
                    Exitoso = true,
                    Mensaje = $"{dispositivos.Count} dispositivos",
                    Data    = dispositivos
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<List<CuentaDispositivo>> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Asigna un dispositivo a una cuenta</summary>
        [HttpPost("{id}/dispositivos")]
        [ProducesResponseType(typeof(ApiResponse<DispositivoResponse>), 200)]
        public async Task<IActionResult> AsignarDispositivo(int id, [FromBody] AsignarDispositivoRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.IdDispositivo))
                    return BadRequest(new ApiResponse<DispositivoResponse> { Exitoso = false, Mensaje = "IdDispositivo es requerido" });

                request.IdCuenta = id;
                var resultado = await _cuentasService.AsignarDispositivoAsync(request);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<DispositivoResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<DispositivoResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<DispositivoResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Quita un dispositivo de una cuenta</summary>
        [HttpDelete("{id}/dispositivos/{idDispositivo}")]
        [ProducesResponseType(typeof(ApiResponse<DispositivoResponse>), 200)]
        public async Task<IActionResult> QuitarDispositivo(int id, string idDispositivo)
        {
            try
            {
                var resultado = await _cuentasService.QuitarDispositivoAsync(id, idDispositivo);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<DispositivoResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<DispositivoResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<DispositivoResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // HORARIOS DE LA CUENTA
        // ─────────────────────────────────────────────────────────────────

        /// <summary>Lista los horarios de acceso de una cuenta</summary>
        [HttpGet("{id}/horarios")]
        [ProducesResponseType(typeof(ApiResponse<List<CuentaHorario>>), 200)]
        public async Task<IActionResult> ListarHorarios(int id)
        {
            try
            {
                var horarios = await _cuentasService.ListarHorariosDeCuentaAsync(id);
                return Ok(new ApiResponse<List<CuentaHorario>>
                {
                    Exitoso = true,
                    Mensaje = $"{horarios.Count} horarios",
                    Data    = horarios
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<List<CuentaHorario>> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Guarda (crea o actualiza) un horario de acceso para una cuenta</summary>
        [HttpPost("{id}/horarios")]
        [ProducesResponseType(typeof(ApiResponse<HorarioResponse>), 200)]
        public async Task<IActionResult> GuardarHorario(int id, [FromBody] GuardarHorarioRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<HorarioResponse> { Exitoso = false, Mensaje = "Request nulo" });

                request.IdCuenta = id;
                var resultado = await _cuentasService.GuardarHorarioAsync(request);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<HorarioResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<HorarioResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<HorarioResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }

        /// <summary>Elimina un horario de acceso</summary>
        [HttpDelete("{id}/horarios/{idHorario}")]
        [ProducesResponseType(typeof(ApiResponse<HorarioResponse>), 200)]
        public async Task<IActionResult> EliminarHorario(int id, int idHorario)
        {
            try
            {
                var resultado = await _cuentasService.EliminarHorarioAsync(idHorario);
                return resultado.Exitoso
                    ? Ok(new ApiResponse<HorarioResponse> { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<HorarioResponse> { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception)
            {
                return StatusCode(500, new ApiResponse<HorarioResponse> { Exitoso = false, Mensaje = "Error interno al procesar la solicitud" });
            }
        }
    }
}
