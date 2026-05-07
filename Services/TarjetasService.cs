using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface ITarjetasService
    {
        // Niveles
        Task<List<NivelAcceso>> ListarNivelesAccesoAsync();

        // Cuentas
        Task<List<Cuenta>> ListarCuentasAsync(bool soloActivas = false, string? buscar = null);
        Task<CuentaResponse> CrearCuentaAsync(CrearCuentaRequest request);
        Task<CuentaResponse> ActualizarCuentaAsync(ActualizarCuentaRequest request);
        Task<CuentaResponse> EliminarCuentaAsync(int id);

        // Tarjetas
        Task<List<Tarjeta>> ListarTarjetasAsync(int? idCuenta = null, bool soloActivas = false, string? buscar = null);
        Task<TarjetaResponse> CrearTarjetaAsync(CrearTarjetaRequest request);
        Task<TarjetaResponse> ActualizarTarjetaAsync(ActualizarTarjetaRequest request);
        Task<TarjetaResponse> EliminarTarjetaAsync(int id);

        // Validación y entrada
        Task<ValidarTarjetaResponse> ValidarTarjetaAsync(string codigoTarjeta);
        Task<RegistrarEntradaTarjetaResponse> RegistrarEntradaTarjetaAsync(RegistrarEntradaTarjetaRequest request);
    }

    public class TarjetasService : ITarjetasService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<TarjetasService> _logger;

        public TarjetasService(IDatabaseService db, ILogger<TarjetasService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // =============================================
        // NIVELES DE ACCESO
        // =============================================

        public async Task<List<NivelAcceso>> ListarNivelesAccesoAsync()
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@SoloActivos", 1)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ListarNivelesAcceso @SoloActivos", parameters);

                var lista = new List<NivelAcceso>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new NivelAcceso
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Nombre = row["Nombre"]?.ToString() ?? "",
                        Descripcion = row["Descripcion"]?.ToString(),
                        Activo = Convert.ToBoolean(row["Activo"]),
                        HorariosResumen = row["HorariosResumen"]?.ToString()
                    });
                }
                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar niveles de acceso");
                return new List<NivelAcceso>();
            }
        }

        // =============================================
        // CUENTAS
        // =============================================

        public async Task<List<Cuenta>> ListarCuentasAsync(bool soloActivas = false, string? buscar = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@SoloActivas", soloActivas ? 1 : 0),
                    new SqlParameter("@Buscar", (object?)buscar ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ListarCuentas @SoloActivas, @Buscar", parameters);

                var lista = new List<Cuenta>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new Cuenta
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        CodigoUnico = row["CodigoUnico"]?.ToString() ?? "",
                        Nombre = row["Nombre"]?.ToString() ?? "",
                        Descripcion = row["Descripcion"]?.ToString(),
                        Email = row["Email"]?.ToString(),
                        Telefono = row["Telefono"]?.ToString(),
                        Activa = Convert.ToBoolean(row["Activa"]),
                        FechaCreacion = Convert.ToDateTime(row["FechaCreacion"]),
                        FechaModificacion = row["FechaModificacion"] != DBNull.Value
                                            ? Convert.ToDateTime(row["FechaModificacion"]) : null,
                        TotalTarjetas = Convert.ToInt32(row["TotalTarjetas"]),
                        TarjetasVigentes = row["TarjetasVigentes"] != DBNull.Value
                                            ? Convert.ToInt32(row["TarjetasVigentes"]) : 0
                    });
                }
                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar cuentas");
                return new List<Cuenta>();
            }
        }

        public async Task<CuentaResponse> CrearCuentaAsync(CrearCuentaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@CodigoUnico",    request.CodigoUnico),
                    new SqlParameter("@Nombre",         request.Nombre),
                    new SqlParameter("@Descripcion",    (object?)request.Descripcion    ?? DBNull.Value),
                    new SqlParameter("@Email",          (object?)request.Email          ?? DBNull.Value),
                    new SqlParameter("@Telefono",       (object?)request.Telefono       ?? DBNull.Value),
                    new SqlParameter("@IdOperadorCreo", (object?)request.IdOperadorCreo ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearCuenta @CodigoUnico, @Nombre, @Descripcion, @Email, @Telefono, @IdOperadorCreo",
                    parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new CuentaResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null
                    };
                }

                return new CuentaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cuenta: {Nombre}", request.Nombre);
                return new CuentaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<CuentaResponse> ActualizarCuentaAsync(ActualizarCuentaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id",          request.Id),
                    new SqlParameter("@Nombre",      (object?)request.Nombre      ?? DBNull.Value),
                    new SqlParameter("@Descripcion", (object?)request.Descripcion ?? DBNull.Value),
                    new SqlParameter("@Email",       (object?)request.Email       ?? DBNull.Value),
                    new SqlParameter("@Telefono",    (object?)request.Telefono    ?? DBNull.Value),
                    new SqlParameter("@Activa",      request.Activa.HasValue ? (object)request.Activa.Value : DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarCuenta @Id, @Nombre, @Descripcion, @Email, @Telefono, @Activa",
                    parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new CuentaResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = request.Id
                    };
                }

                return new CuentaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cuenta ID: {Id}", request.Id);
                return new CuentaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<CuentaResponse> EliminarCuentaAsync(int id)
        {
            try
            {
                var parameters = new SqlParameter[] { new SqlParameter("@Id", id) };
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_EliminarCuenta @Id", parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new CuentaResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? ""
                    };
                }

                return new CuentaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar cuenta ID: {Id}", id);
                return new CuentaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // =============================================
        // TARJETAS
        // =============================================

        public async Task<List<Tarjeta>> ListarTarjetasAsync(int? idCuenta = null, bool soloActivas = false, string? buscar = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdCuenta",    (object?)idCuenta ?? DBNull.Value),
                    new SqlParameter("@SoloActivas", soloActivas ? 1 : 0),
                    new SqlParameter("@Buscar",      (object?)buscar ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ListarTarjetas @IdCuenta, @SoloActivas, @Buscar", parameters);

                var lista = new List<Tarjeta>();
                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new Tarjeta
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        IdCuenta = Convert.ToInt32(row["IdCuenta"]),
                        CodigoCuenta = row["CodigoCuenta"]?.ToString() ?? "",
                        NombreCuenta = row["NombreCuenta"]?.ToString() ?? "",
                        CuentaActiva = Convert.ToBoolean(row["CuentaActiva"]),
                        IdNivelAcceso = Convert.ToInt32(row["IdNivelAcceso"]),
                        NivelAcceso = row["NivelAcceso"]?.ToString() ?? "",
                        CodigoTarjeta = row["CodigoTarjeta"]?.ToString() ?? "",
                        NombreTitular = row["NombreTitular"]?.ToString(),
                        IdentificacionTitular = row["IdentificacionTitular"]?.ToString(),
                        FechaVigenciaInicio = row["FechaVigenciaInicio"] != DBNull.Value
                                                ? Convert.ToDateTime(row["FechaVigenciaInicio"]) : null,
                        FechaVigenciaFin = row["FechaVigenciaFin"] != DBNull.Value
                                                ? Convert.ToDateTime(row["FechaVigenciaFin"]) : null,
                        Activa = Convert.ToBoolean(row["Activa"]),
                        EstadoVigencia = row["EstadoVigencia"]?.ToString() ?? "",
                        EstaActualmenteDentro = Convert.ToInt32(row["EstaActualmenteDentro"]) == 1,
                        Observacion = row["Observacion"]?.ToString(),
                        FechaCreacion = Convert.ToDateTime(row["FechaCreacion"]),
                        FechaModificacion = row["FechaModificacion"] != DBNull.Value
                                                ? Convert.ToDateTime(row["FechaModificacion"]) : null
                    });
                }
                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tarjetas");
                return new List<Tarjeta>();
            }
        }

        public async Task<TarjetaResponse> CrearTarjetaAsync(CrearTarjetaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdCuenta",              request.IdCuenta),
                    new SqlParameter("@IdNivelAcceso",         request.IdNivelAcceso),
                    new SqlParameter("@CodigoTarjeta",         request.CodigoTarjeta),
                    new SqlParameter("@NombreTitular",         (object?)request.NombreTitular         ?? DBNull.Value),
                    new SqlParameter("@IdentificacionTitular", (object?)request.IdentificacionTitular ?? DBNull.Value),
                    new SqlParameter("@FechaVigenciaInicio",   request.FechaVigenciaInicio.HasValue
                                                               ? (object)request.FechaVigenciaInicio.Value.Date : DBNull.Value),
                    new SqlParameter("@FechaVigenciaFin",      request.FechaVigenciaFin.HasValue
                                                               ? (object)request.FechaVigenciaFin.Value.Date : DBNull.Value),
                    new SqlParameter("@Observacion",           (object?)request.Observacion    ?? DBNull.Value),
                    new SqlParameter("@IdOperadorCreo",        (object?)request.IdOperadorCreo ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearTarjeta @IdCuenta, @IdNivelAcceso, @CodigoTarjeta, " +
                    "@NombreTitular, @IdentificacionTitular, @FechaVigenciaInicio, @FechaVigenciaFin, " +
                    "@Observacion, @IdOperadorCreo",
                    parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new TarjetaResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null
                    };
                }

                return new TarjetaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarjeta: {Codigo}", request.CodigoTarjeta);
                return new TarjetaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<TarjetaResponse> ActualizarTarjetaAsync(ActualizarTarjetaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id",                    request.Id),
                    new SqlParameter("@IdNivelAcceso",         request.IdNivelAcceso.HasValue
                                                               ? (object)request.IdNivelAcceso.Value : DBNull.Value),
                    new SqlParameter("@NombreTitular",         (object?)request.NombreTitular         ?? DBNull.Value),
                    new SqlParameter("@IdentificacionTitular", (object?)request.IdentificacionTitular ?? DBNull.Value),
                    new SqlParameter("@FechaVigenciaInicio",   request.FechaVigenciaInicio.HasValue
                                                               ? (object)request.FechaVigenciaInicio.Value.Date : DBNull.Value),
                    new SqlParameter("@FechaVigenciaFin",      request.FechaVigenciaFin.HasValue
                                                               ? (object)request.FechaVigenciaFin.Value.Date : DBNull.Value),
                    new SqlParameter("@Activa",                request.Activa.HasValue
                                                               ? (object)request.Activa.Value : DBNull.Value),
                    new SqlParameter("@Observacion",           (object?)request.Observacion ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarTarjeta @Id, @IdNivelAcceso, @NombreTitular, " +
                    "@IdentificacionTitular, @FechaVigenciaInicio, @FechaVigenciaFin, @Activa, @Observacion",
                    parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new TarjetaResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = request.Id
                    };
                }

                return new TarjetaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tarjeta ID: {Id}", request.Id);
                return new TarjetaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<TarjetaResponse> EliminarTarjetaAsync(int id)
        {
            try
            {
                var parameters = new SqlParameter[] { new SqlParameter("@Id", id) };
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_EliminarTarjeta @Id", parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new TarjetaResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? ""
                    };
                }

                return new TarjetaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar tarjeta ID: {Id}", id);
                return new TarjetaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // =============================================
        // VALIDACIÓN Y ENTRADA
        // =============================================

        /// <summary>
        /// Valida si la tarjeta puede entrar SIN escribir nada en BD.
        /// El Controllino llama esto primero; si Exitoso=true, él mismo
        /// decide llamar a RegistrarEntradaTarjetaAsync.
        /// </summary>
        public async Task<ValidarTarjetaResponse> ValidarTarjetaAsync(string codigoTarjeta)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@CodigoTarjeta", codigoTarjeta)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ValidarTarjeta @CodigoTarjeta", parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);
                    var motivoRechazo = row["MotivoRechazo"]?.ToString();
                    var nombreTitular = row["NombreTitular"]?.ToString();
                    var nombreCuenta = row["NombreCuenta"]?.ToString();
                    var nivelAcceso = row["NivelAcceso"]?.ToString();

                    _logger.LogInformation(
                        "Validación tarjeta {Codigo} → {Resultado} {Motivo}",
                        codigoTarjeta,
                        exitoso ? "APROBADA" : "RECHAZADA",
                        motivoRechazo ?? "");

                    return new ValidarTarjetaResponse
                    {
                        Exitoso = exitoso,
                        MotivoRechazo = motivoRechazo,
                        NombreTitular = nombreTitular,
                        NombreCuenta = nombreCuenta,
                        NivelAcceso = nivelAcceso,
                        Mensaje = exitoso
                            ? $"✓ Acceso permitido — {nombreTitular ?? nombreCuenta}"
                            : MensajeDesdeMotivo(motivoRechazo)
                    };
                }

                return new ValidarTarjetaResponse
                {
                    Exitoso = false,
                    Mensaje = "Sin respuesta del servidor",
                    MotivoRechazo = "ERROR"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar tarjeta: {Codigo}", codigoTarjeta);
                return new ValidarTarjetaResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}",
                    MotivoRechazo = "ERROR"
                };
            }
        }

        /// <summary>
        /// Registra la entrada en IOT_Vehiculos usando IOT_sp_RegistrarEntrada
        /// con strRateKey='T'. El Controllino lo llama solo si validar devolvió Exitoso=true.
        /// </summary>
        public async Task<RegistrarEntradaTarjetaResponse> RegistrarEntradaTarjetaAsync(
            RegistrarEntradaTarjetaRequest request)
        {
            try
            {
                // Obtener nombre de usuario del operador para el campo UsuarioRegistro
                string usuario = "TARJETA";
                try
                {
                    var dtOp = await _db.ExecuteQueryAsync(
                        "SELECT Username FROM dbo.IOT_Operadores WHERE Id = @Id",
                        new SqlParameter[] { new SqlParameter("@Id", request.IdOperador) });

                    if (dtOp.Rows.Count > 0)
                        usuario = dtOp.Rows[0]["Username"]?.ToString() ?? "TARJETA";
                }
                catch { /* fallback a "TARJETA" */ }

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Placa",         request.CodigoTarjeta),
                    new SqlParameter("@Usuario",       usuario),
                    new SqlParameter("@IdOperador",    request.IdOperador),
                    new SqlParameter("@IdDispositivo", request.IdDispositivo),
                    new SqlParameter("@strRateKey",    "T")
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_RegistrarEntrada @Placa, @Usuario, @IdOperador, @IdDispositivo, @strRateKey",
                    parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    // IOT_sp_RegistrarEntrada devuelve: Id, CodigoBarras  (éxito)
                    //                                  o  Id=-1, ErrorMessage (error)
                    var id = Convert.ToInt32(row["Id"]);

                    if (id > 0)
                    {
                        _logger.LogInformation(
                            "✓ Entrada tarjeta registrada — Código: {Codigo}, Id: {Id}",
                            request.CodigoTarjeta, id);

                        return new RegistrarEntradaTarjetaResponse
                        {
                            Exitoso = true,
                            Mensaje = "Entrada registrada correctamente",
                            Id = id,
                            CodigoBarras = row["CodigoBarras"]?.ToString()
                        };
                    }
                    else
                    {
                        var error = row.Table.Columns.Contains("ErrorMessage")
                            ? row["ErrorMessage"]?.ToString()
                            : "Error al registrar entrada";

                        _logger.LogWarning(
                            "Error en entrada tarjeta {Codigo}: {Error}",
                            request.CodigoTarjeta, error);

                        return new RegistrarEntradaTarjetaResponse
                        {
                            Exitoso = false,
                            Mensaje = error ?? "Error al registrar entrada"
                        };
                    }
                }

                return new RegistrarEntradaTarjetaResponse
                {
                    Exitoso = false,
                    Mensaje = "Sin respuesta del servidor"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada tarjeta: {Codigo}", request.CodigoTarjeta);
                return new RegistrarEntradaTarjetaResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        // =============================================
        // HELPERS
        // =============================================

        private static string MensajeDesdeMotivo(string? motivo) => motivo switch
        {
            "TARJETA_NO_EXISTE" => "Tarjeta no registrada en el sistema",
            "TARJETA_INACTIVA" => "Tarjeta desactivada — contacte al administrador",
            "CUENTA_INACTIVA" => "La cuenta asociada está inactiva",
            "VIGENCIA_EXPIRADA" => "La tarjeta está vencida o aún no tiene vigencia",
            "YA_DENTRO" => "Esta tarjeta ya registró entrada y está dentro del parqueo",
            "FUERA_DE_HORARIO" => "Acceso denegado — fuera del horario permitido",
            _ => "Acceso denegado"
        };
    }
}