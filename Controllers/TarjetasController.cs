using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace ParqueoIzalcoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TarjetasController : ControllerBase
    {
        private readonly ITarjetasService _tarjetasService;
        private readonly ILogger<TarjetasController> _logger;

        public TarjetasController(ITarjetasService tarjetasService, ILogger<TarjetasController> logger)
        {
            _tarjetasService = tarjetasService;
            _logger = logger;
        }

        // =============================================
        // NIVELES DE ACCESO
        // =============================================

        /// <summary>Lista los niveles de acceso disponibles (para selects del frontend)</summary>
        [HttpGet("niveles")]
        [ProducesResponseType(typeof(ApiResponse<List<NivelAcceso>>), 200)]
        public async Task<IActionResult> ListarNiveles()
        {
            try
            {
                var niveles = await _tarjetasService.ListarNivelesAccesoAsync();
                return Ok(new ApiResponse<List<NivelAcceso>>
                {
                    Exitoso = true,
                    Mensaje = $"{niveles.Count} niveles",
                    Data = niveles
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar niveles");
                return StatusCode(500, new ApiResponse<List<NivelAcceso>>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // CUENTAS
        // =============================================

        /// <summary>Lista todas las cuentas con conteo de tarjetas</summary>
        [HttpGet("cuentas")]
        [ProducesResponseType(typeof(ApiResponse<List<Cuenta>>), 200)]
        public async Task<IActionResult> ListarCuentas(
            [FromQuery] bool soloActivas = false,
            [FromQuery] string? buscar = null)
        {
            try
            {
                var cuentas = await _tarjetasService.ListarCuentasAsync(soloActivas, buscar);
                return Ok(new ApiResponse<List<Cuenta>>
                {
                    Exitoso = true,
                    Mensaje = $"{cuentas.Count} cuentas",
                    Data = cuentas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar cuentas");
                return StatusCode(500, new ApiResponse<List<Cuenta>>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>Crea una nueva cuenta</summary>
        [HttpPost("cuentas")]
        [ProducesResponseType(typeof(ApiResponse<CuentaResponse>), 200)]
        public async Task<IActionResult> CrearCuenta([FromBody] CrearCuentaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = "Request nulo" });

                if (string.IsNullOrWhiteSpace(request.CodigoUnico))
                    return BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = "El código único es requerido" });

                if (string.IsNullOrWhiteSpace(request.Nombre))
                    return BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = "El nombre es requerido" });

                _logger.LogInformation("Creando cuenta: {Codigo} — {Nombre}",
                    request.CodigoUnico, request.Nombre);

                var resultado = await _tarjetasService.CrearCuentaAsync(request);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<CuentaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cuenta");
                return StatusCode(500, new ApiResponse<CuentaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>Actualiza datos o estado (activa/inactiva) de una cuenta</summary>
        [HttpPut("cuentas/{id}")]
        [ProducesResponseType(typeof(ApiResponse<CuentaResponse>), 200)]
        public async Task<IActionResult> ActualizarCuenta(int id, [FromBody] ActualizarCuentaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = "Request nulo" });

                request.Id = id;
                var resultado = await _tarjetasService.ActualizarCuentaAsync(request);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<CuentaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cuenta ID: {Id}", id);
                return StatusCode(500, new ApiResponse<CuentaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>Elimina una cuenta (solo si no tiene tarjetas)</summary>
        [HttpDelete("cuentas/{id}")]
        [ProducesResponseType(typeof(ApiResponse<CuentaResponse>), 200)]
        public async Task<IActionResult> EliminarCuenta(int id)
        {
            try
            {
                var resultado = await _tarjetasService.EliminarCuentaAsync(id);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<CuentaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje })
                    : BadRequest(new ApiResponse<CuentaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cuenta ID: {Id}", id);
                return StatusCode(500, new ApiResponse<CuentaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // TARJETAS
        // =============================================

        /// <summary>
        /// Lista tarjetas. Filtra por cuenta si se pasa idCuenta.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<Tarjeta>>), 200)]
        public async Task<IActionResult> ListarTarjetas(
            [FromQuery] int? idCuenta = null,
            [FromQuery] bool soloActivas = false,
            [FromQuery] string? buscar = null)
        {
            try
            {
                var tarjetas = await _tarjetasService.ListarTarjetasAsync(idCuenta, soloActivas, buscar);
                return Ok(new ApiResponse<List<Tarjeta>>
                {
                    Exitoso = true,
                    Mensaje = $"{tarjetas.Count} tarjetas",
                    Data = tarjetas
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tarjetas");
                return StatusCode(500, new ApiResponse<List<Tarjeta>>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>Crea una nueva tarjeta asociada a una cuenta</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TarjetaResponse>), 200)]
        public async Task<IActionResult> CrearTarjeta([FromBody] CrearTarjetaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = "Request nulo" });

                if (string.IsNullOrWhiteSpace(request.CodigoTarjeta))
                    return BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = "El código de tarjeta es requerido" });

                if (request.IdCuenta <= 0)
                    return BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = "La cuenta es requerida" });

                if (request.IdNivelAcceso <= 0)
                    return BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = "El nivel de acceso es requerido" });

                _logger.LogInformation("Creando tarjeta: {Codigo} para cuenta ID {Cuenta}",
                    request.CodigoTarjeta, request.IdCuenta);

                var resultado = await _tarjetasService.CrearTarjetaAsync(request);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarjetaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarjeta");
                return StatusCode(500, new ApiResponse<TarjetaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>Actualiza datos o estado (activa/inactiva) de una tarjeta</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TarjetaResponse>), 200)]
        public async Task<IActionResult> ActualizarTarjeta(int id, [FromBody] ActualizarTarjetaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = "Request nulo" });

                request.Id = id;
                var resultado = await _tarjetasService.ActualizarTarjetaAsync(request);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarjetaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tarjeta ID: {Id}", id);
                return StatusCode(500, new ApiResponse<TarjetaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>Elimina una tarjeta (solo si no está actualmente DENTRO)</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse<TarjetaResponse>), 200)]
        public async Task<IActionResult> EliminarTarjeta(int id)
        {
            try
            {
                var resultado = await _tarjetasService.EliminarTarjetaAsync(id);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<TarjetaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje })
                    : BadRequest(new ApiResponse<TarjetaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar tarjeta ID: {Id}", id);
                return StatusCode(500, new ApiResponse<TarjetaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        // =============================================
        // VALIDACIÓN Y ENTRADA (CONTROLLINO)
        // =============================================

        /// <summary>
        /// PASO 1 — Controllino valida la tarjeta ANTES de registrar entrada.
        /// Solo consulta, NO escribe nada en BD.
        /// Responde Exitoso=true si puede entrar, o Exitoso=false con MotivoRechazo.
        /// </summary>
        [HttpPost("validar")]
        [ProducesResponseType(typeof(ApiResponse<ValidarTarjetaResponse>), 200)]
        public async Task<IActionResult> ValidarTarjeta([FromBody] ValidarTarjetaRequest request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.CodigoTarjeta))
                    return BadRequest(new ApiResponse<ValidarTarjetaResponse>
                    { Exitoso = false, Mensaje = "El código de tarjeta es requerido" });

                _logger.LogInformation("Validando tarjeta: {Codigo}", request.CodigoTarjeta);

                var resultado = await _tarjetasService.ValidarTarjetaAsync(request.CodigoTarjeta);

                // Siempre 200 — el Controllino decide qué hacer con Exitoso y MotivoRechazo
                return Ok(new ApiResponse<ValidarTarjetaResponse>
                {
                    Exitoso = resultado.Exitoso,
                    Mensaje = resultado.Mensaje,
                    Data = resultado
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar tarjeta: {Codigo}", request?.CodigoTarjeta);
                return StatusCode(500, new ApiResponse<ValidarTarjetaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }

        /// <summary>
        /// PASO 2 — Controllino registra la entrada en IOT_Vehiculos (strRateKey='T').
        /// Solo llamar si /validar devolvió Exitoso=true.
        /// La PS calculará $0.00 al detectar tarifa T y abrirá la barrera.
        /// </summary>
        [HttpPost("registrar-entrada")]
        [ProducesResponseType(typeof(ApiResponse<RegistrarEntradaTarjetaResponse>), 200)]
        public async Task<IActionResult> RegistrarEntrada([FromBody] RegistrarEntradaTarjetaRequest request)
        {
            try
            {
                if (request == null)
                    return BadRequest(new ApiResponse<RegistrarEntradaTarjetaResponse>
                    { Exitoso = false, Mensaje = "Request nulo" });

                if (string.IsNullOrWhiteSpace(request.CodigoTarjeta))
                    return BadRequest(new ApiResponse<RegistrarEntradaTarjetaResponse>
                    { Exitoso = false, Mensaje = "El código de tarjeta es requerido" });

                if (string.IsNullOrWhiteSpace(request.IdDispositivo))
                    return BadRequest(new ApiResponse<RegistrarEntradaTarjetaResponse>
                    { Exitoso = false, Mensaje = "El IdDispositivo es requerido" });

                _logger.LogInformation(
                    "Registrando entrada tarjeta: {Codigo} — Dispositivo: {Dispositivo}",
                    request.CodigoTarjeta, request.IdDispositivo);

                var resultado = await _tarjetasService.RegistrarEntradaTarjetaAsync(request);

                return resultado.Exitoso
                    ? Ok(new ApiResponse<RegistrarEntradaTarjetaResponse>
                    { Exitoso = true, Mensaje = resultado.Mensaje, Data = resultado })
                    : BadRequest(new ApiResponse<RegistrarEntradaTarjetaResponse>
                    { Exitoso = false, Mensaje = resultado.Mensaje, Data = resultado });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada tarjeta: {Codigo}", request?.CodigoTarjeta);
                return StatusCode(500, new ApiResponse<RegistrarEntradaTarjetaResponse>
                { Exitoso = false, Mensaje = $"Error: {ex.Message}" });
            }
        }
    }
}