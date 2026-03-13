using DataparkBarreraAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataparkBarreraAPI.Services
{
    public interface IVisitasService
    {
        // Login
        Task<LoginVisitasResponse> LoginAsync(string username, string password);
        // Catálogos
        Task<List<TipoVisitante>> ListarTiposVisitanteAsync();
        Task<VisitanteResponse> CrearTipoVisitanteAsync(CrearCatalogoRequest request);
        Task<List<AreaDestino>> ListarAreasDestinoAsync();
        Task<VisitanteResponse> CrearAreaDestinoAsync(CrearCatalogoRequest request);

        // Visitantes (catálogo de personas)
        Task<List<Visitante>> BuscarVisitantesAsync(string termino, int? idTipoVisitante = null);
        Task<CrearVisitanteResponse> CrearVisitanteAsync(CrearVisitanteRequest request);
        Task<VisitanteResponse> ActualizarVisitanteAsync(ActualizarVisitanteRequest request);

        // Registro de visitas
        Task<RegistrarEntradaVisitanteResponse> RegistrarEntradaAsync(RegistrarEntradaVisitanteRequest request);
        Task<VisitanteResponse> RegistrarSalidaAsync(int id);
        Task<List<RegistroVisita>> ListarVisitasHoyAsync();
        Task<List<RegistroVisita>> ListarVisitasDentroAsync();
        Task<List<RegistroVisita>> BuscarVisitasAsync(BuscarVisitasRequest request);
        Task<VisitanteResponse> ActualizarObservacionAsync(int id, string observacion);
        Task<EstadisticasVisitas> ObtenerEstadisticasAsync(DateTime? fecha = null);
        Task<List<Dictionary<string, object>>> ReporteVehiculosAsync(ReporteVehiculosRequest request);
        Task<DashboardVehiculosResponse> ObtenerDashboardAsync();

    }

    public class VisitasService : IVisitasService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<VisitasService> _logger;

        public VisitasService(IDatabaseService db, ILogger<VisitasService> logger)
        {
            _db = db;
            _logger = logger;
        }
        // =============================================
        // LOGIN
        // =============================================

        public async Task<LoginVisitasResponse> LoginAsync(string username, string password)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Username", username),
                    new SqlParameter("@Password", password)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ValidarOperador @Username, @Password",
                    parameters
                );

                if (dt.Rows.Count == 0)
                {
                    return new LoginVisitasResponse
                    {
                        Exitoso = false,
                        Mensaje = "Usuario o contraseña incorrectos"
                    };
                }

                var row = dt.Rows[0];
                var tipoUsuario = row["TipoUsuario"]?.ToString() ?? "";

                // Solo permitir ADMINISTRADOR y CAJA
                if (tipoUsuario != "ADMINISTRADOR" && tipoUsuario != "CAJA")
                {
                    return new LoginVisitasResponse
                    {
                        Exitoso = false,
                        Mensaje = "No tiene permisos para acceder a este módulo"
                    };
                }

                var nombre = row["Nombre"]?.ToString() ?? "";
                var apellido = row["Apellido"]?.ToString() ?? "";

                return new LoginVisitasResponse
                {
                    Exitoso = true,
                    Mensaje = "Login exitoso",
                    IdOperador = Convert.ToInt32(row["Id"]),
                    NombreCompleto = $"{nombre} {apellido}".Trim(),
                    Username = row["Username"]?.ToString() ?? "",
                    TipoUsuario = tipoUsuario
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en login para usuario: {Username}", username);
                return new LoginVisitasResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        // =============================================
        // CATÁLOGOS
        // =============================================

        public async Task<List<TipoVisitante>> ListarTiposVisitanteAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ListarTiposVisitante");
                var lista = new List<TipoVisitante>();

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new TipoVisitante
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Nombre = row["Nombre"]?.ToString() ?? "",
                        Descripcion = row["Descripcion"]?.ToString(),
                        Activo = Convert.ToBoolean(row["Activo"])
                    });
                }

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tipos de visitante");
                return new List<TipoVisitante>();
            }
        }

        public async Task<VisitanteResponse> CrearTipoVisitanteAsync(CrearCatalogoRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Nombre", request.Nombre),
                    new SqlParameter("@Descripcion", (object?)request.Descripcion ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearTipoVisitante @Nombre, @Descripcion",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new VisitanteResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value
                            ? Convert.ToInt32(row["Id"]) : null
                    };
                }

                return new VisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tipo de visitante");
                return new VisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<List<AreaDestino>> ListarAreasDestinoAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ListarAreasDestino");
                var lista = new List<AreaDestino>();

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new AreaDestino
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Nombre = row["Nombre"]?.ToString() ?? "",
                        Descripcion = row["Descripcion"]?.ToString(),
                        Activo = Convert.ToBoolean(row["Activo"])
                    });
                }

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar áreas destino");
                return new List<AreaDestino>();
            }
        }

        public async Task<VisitanteResponse> CrearAreaDestinoAsync(CrearCatalogoRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Nombre", request.Nombre),
                    new SqlParameter("@Descripcion", (object?)request.Descripcion ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearAreaDestino @Nombre, @Descripcion",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new VisitanteResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row.Table.Columns.Contains("Id") && row["Id"] != DBNull.Value
                            ? Convert.ToInt32(row["Id"]) : null
                    };
                }

                return new VisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear área destino");
                return new VisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // =============================================
        // VISITANTES (CATÁLOGO DE PERSONAS)
        // =============================================

        public async Task<List<Visitante>> BuscarVisitantesAsync(string termino, int? idTipoVisitante = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Termino", termino),
                    new SqlParameter("@IdTipoVisitante", (object?)idTipoVisitante ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_BuscarVisitantes @Termino, @IdTipoVisitante",
                    parameters
                );

                var lista = new List<Visitante>();

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new Visitante
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        NombreCompleto = row["NombreCompleto"]?.ToString() ?? "",
                        Identificacion = row["Identificacion"]?.ToString(),
                        Telefono = row["Telefono"]?.ToString(),
                        Especialidad = row["Especialidad"]?.ToString(),
                        IdTipoVisitante = Convert.ToInt32(row["IdTipoVisitante"]),
                        TipoVisitante = row["TipoVisitante"]?.ToString(),
                        PlacaFrecuente = row["PlacaFrecuente"]?.ToString(),
                        Empresa = row["Empresa"]?.ToString()
                    });
                }

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar visitantes con término: {Termino}", termino);
                return new List<Visitante>();
            }
        }

        public async Task<CrearVisitanteResponse> CrearVisitanteAsync(CrearVisitanteRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@NombreCompleto", request.NombreCompleto),
                    new SqlParameter("@IdTipoVisitante", request.IdTipoVisitante),
                    new SqlParameter("@Identificacion", (object?)request.Identificacion ?? DBNull.Value),
                    new SqlParameter("@Telefono", (object?)request.Telefono ?? DBNull.Value),
                    new SqlParameter("@Especialidad", (object?)request.Especialidad ?? DBNull.Value),
                    new SqlParameter("@PlacaFrecuente", (object?)request.PlacaFrecuente ?? DBNull.Value),
                    new SqlParameter("@Empresa", (object?)request.Empresa ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearVisitante @NombreCompleto, @IdTipoVisitante, @Identificacion, @Telefono, @Especialidad, @PlacaFrecuente, @Empresa",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);

                    _logger.LogInformation(
                        "Crear visitante - Nombre: {Nombre}, Exitoso: {Exitoso}",
                        request.NombreCompleto, exitoso
                    );

                    return new CrearVisitanteResponse
                    {
                        Exitoso = exitoso,
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null,
                        NombreCompleto = row["NombreCompleto"]?.ToString(),
                        IdTipoVisitante = row["IdTipoVisitante"] != DBNull.Value ? Convert.ToInt32(row["IdTipoVisitante"]) : null,
                        PlacaFrecuente = row["PlacaFrecuente"]?.ToString()
                    };
                }

                return new CrearVisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear visitante: {Nombre}", request.NombreCompleto);
                return new CrearVisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<VisitanteResponse> ActualizarVisitanteAsync(ActualizarVisitanteRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id", request.Id),
                    new SqlParameter("@NombreCompleto", request.NombreCompleto),
                    new SqlParameter("@IdTipoVisitante", request.IdTipoVisitante),
                    new SqlParameter("@Identificacion", (object?)request.Identificacion ?? DBNull.Value),
                    new SqlParameter("@Telefono", (object?)request.Telefono ?? DBNull.Value),
                    new SqlParameter("@Especialidad", (object?)request.Especialidad ?? DBNull.Value),
                    new SqlParameter("@PlacaFrecuente", (object?)request.PlacaFrecuente ?? DBNull.Value),
                    new SqlParameter("@Empresa", (object?)request.Empresa ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarVisitante @Id, @NombreCompleto, @IdTipoVisitante, @Identificacion, @Telefono, @Especialidad, @PlacaFrecuente, @Empresa",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new VisitanteResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? ""
                    };
                }

                return new VisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar visitante ID: {Id}", request.Id);
                return new VisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // =============================================
        // REGISTRO DE VISITAS
        // =============================================

        public async Task<RegistrarEntradaVisitanteResponse> RegistrarEntradaAsync(RegistrarEntradaVisitanteRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdVisitante", (object?)request.IdVisitante ?? DBNull.Value),
                    new SqlParameter("@NombreVisitante", request.NombreVisitante),
                    new SqlParameter("@IdTipoVisitante", request.IdTipoVisitante),
                    new SqlParameter("@Placa", (object?)request.Placa ?? DBNull.Value),
                    new SqlParameter("@IdAreaDestino", (object?)request.IdAreaDestino ?? DBNull.Value),
                    new SqlParameter("@Observacion", (object?)request.Observacion ?? DBNull.Value),
                    new SqlParameter("@IdOperador", (object?)request.IdOperador ?? DBNull.Value),
                    new SqlParameter("@NombreOperador", (object?)request.NombreOperador ?? DBNull.Value),
                    new SqlParameter("@IdDispositivo", (object?)request.IdDispositivo ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_RegistrarEntradaVisitante @IdVisitante, @NombreVisitante, @IdTipoVisitante, @Placa, @IdAreaDestino, @Observacion, @IdOperador, @NombreOperador, @IdDispositivo",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);

                    _logger.LogInformation(
                        "✓ Entrada visitante - Nombre: {Nombre}, Exitoso: {Exitoso}",
                        request.NombreVisitante, exitoso
                    );

                    return new RegistrarEntradaVisitanteResponse
                    {
                        Exitoso = exitoso,
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null,
                        HoraEntrada = row["HoraEntrada"] != DBNull.Value ? Convert.ToDateTime(row["HoraEntrada"]) : null
                    };
                }

                return new RegistrarEntradaVisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada de visitante: {Nombre}", request.NombreVisitante);
                return new RegistrarEntradaVisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<VisitanteResponse> RegistrarSalidaAsync(int id)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id", id)
                };

                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_RegistrarSalidaVisitante @Id", parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);

                    _logger.LogInformation("✓ Salida visitante - ID: {Id}, Exitoso: {Exitoso}", id, exitoso);

                    return new VisitanteResponse
                    {
                        Exitoso = exitoso,
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = id
                    };
                }

                return new VisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar salida de visitante ID: {Id}", id);
                return new VisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<List<RegistroVisita>> ListarVisitasHoyAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ListarVisitasHoy");
                return MapearRegistroVisitas(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar visitas de hoy");
                return new List<RegistroVisita>();
            }
        }

        public async Task<List<RegistroVisita>> ListarVisitasDentroAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ListarVisitasDentro");
                var lista = new List<RegistroVisita>();

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new RegistroVisita
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        IdVisitante = row["IdVisitante"] != DBNull.Value ? Convert.ToInt32(row["IdVisitante"]) : null,
                        NombreVisitante = row["NombreVisitante"]?.ToString() ?? "",
                        IdTipoVisitante = Convert.ToInt32(row["IdTipoVisitante"]),
                        TipoVisitante = row["TipoVisitante"]?.ToString(),
                        Placa = row["Placa"]?.ToString(),
                        IdAreaDestino = row["IdAreaDestino"] != DBNull.Value ? Convert.ToInt32(row["IdAreaDestino"]) : null,
                        AreaDestino = row["AreaDestino"]?.ToString(),
                        HoraEntrada = Convert.ToDateTime(row["HoraEntrada"]),
                        Observacion = row["Observacion"]?.ToString(),
                        Estado = "DENTRO",
                        MinutosEstancia = Convert.ToInt32(row["MinutosDentro"])
                    });
                }

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar visitantes dentro");
                return new List<RegistroVisita>();
            }
        }

        public async Task<List<RegistroVisita>> BuscarVisitasAsync(BuscarVisitasRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@FechaInicio", (object?)request.FechaInicio ?? DBNull.Value),
                    new SqlParameter("@FechaFin", (object?)request.FechaFin ?? DBNull.Value),
                    new SqlParameter("@NombreVisitante", (object?)request.NombreVisitante ?? DBNull.Value),
                    new SqlParameter("@Placa", (object?)request.Placa ?? DBNull.Value),
                    new SqlParameter("@IdTipoVisitante", (object?)request.IdTipoVisitante ?? DBNull.Value),
                    new SqlParameter("@Estado", (object?)request.Estado ?? DBNull.Value),
                    new SqlParameter("@Top", request.Top)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_BuscarVisitas @FechaInicio, @FechaFin, @NombreVisitante, @Placa, @IdTipoVisitante, @Estado, @Top",
                    parameters
                );

                return MapearRegistroVisitas(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al buscar visitas");
                return new List<RegistroVisita>();
            }
        }

        public async Task<VisitanteResponse> ActualizarObservacionAsync(int id, string observacion)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id", id),
                    new SqlParameter("@Observacion", observacion)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarObservacionVisita @Id, @Observacion",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new VisitanteResponse
                    {
                        Exitoso = Convert.ToBoolean(row["Exitoso"]),
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = id
                    };
                }

                return new VisitanteResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar observación de visita ID: {Id}", id);
                return new VisitanteResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<EstadisticasVisitas> ObtenerEstadisticasAsync(DateTime? fecha = null)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Fecha", (object?)fecha ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ObtenerEstadisticasVisitas @Fecha", parameters);

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    return new EstadisticasVisitas
                    {
                        TotalVisitas = Convert.ToInt32(row["TotalVisitas"]),
                        VisitantesDentro = Convert.ToInt32(row["VisitantesDentro"]),
                        VisitantesSalieron = Convert.ToInt32(row["VisitantesSalieron"]),
                        TotalMedicos = Convert.ToInt32(row["TotalMedicos"]),
                        TotalProveedores = Convert.ToInt32(row["TotalProveedores"]),
                        TotalVisitantesGenerales = Convert.ToInt32(row["TotalVisitantesGenerales"])
                    };
                }

                return new EstadisticasVisitas();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas de visitas");
                return new EstadisticasVisitas();
            }
        }

        // =============================================
        // MÉTODO AUXILIAR
        // =============================================

        private List<RegistroVisita> MapearRegistroVisitas(DataTable dt)
        {
            var lista = new List<RegistroVisita>();

            foreach (DataRow row in dt.Rows)
            {
                lista.Add(new RegistroVisita
                {
                    Id = Convert.ToInt32(row["Id"]),
                    IdVisitante = row["IdVisitante"] != DBNull.Value ? Convert.ToInt32(row["IdVisitante"]) : null,
                    NombreVisitante = row["NombreVisitante"]?.ToString() ?? "",
                    IdTipoVisitante = Convert.ToInt32(row["IdTipoVisitante"]),
                    TipoVisitante = row["TipoVisitante"]?.ToString(),
                    Placa = row["Placa"]?.ToString(),
                    IdAreaDestino = row["IdAreaDestino"] != DBNull.Value ? Convert.ToInt32(row["IdAreaDestino"]) : null,
                    AreaDestino = row["AreaDestino"]?.ToString(),
                    FechaVisita = Convert.ToDateTime(row["FechaVisita"]),
                    HoraEntrada = Convert.ToDateTime(row["HoraEntrada"]),
                    HoraSalida = row["HoraSalida"] != DBNull.Value ? Convert.ToDateTime(row["HoraSalida"]) : null,
                    Observacion = row["Observacion"]?.ToString(),
                    Estado = row["Estado"]?.ToString() ?? "",
                    NombreOperador = row.Table.Columns.Contains("NombreOperador") ? row["NombreOperador"]?.ToString() : null,
                    MinutosEstancia = Convert.ToInt32(row["MinutosEstancia"])
                });
            }

            return lista;
        }

        // =============================================
        // REPORTE DE VEHÍCULOS
        // =============================================

        private static readonly HashSet<string> ColumnasPermitidas = new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "Placa", "CodigoBarras", "FechaEntrada", "FechaSalida", "Estado",
            "bitPaid", "Monto", "FechaPago", "strRateKey", "TiempoEstancia",
            "IdDispositivoEntrada", "IdDispositivoSalida", "NombreOperador",
            "UsuarioRegistro", "IdOperador", "bitEntry", "bitExit",
            "IdEntryDevice", "IdExitDevice", "bitCopy"
        };

        private static readonly HashSet<string> CamposFechaPermitidos = new(StringComparer.OrdinalIgnoreCase)
        {
            "FechaEntrada", "FechaSalida", "FechaPago"
        };

        public async Task<List<Dictionary<string, object>>> ReporteVehiculosAsync(ReporteVehiculosRequest request)
        {
            try
            {
                // Validar y construir columnas
                var columnas = new List<string>();
                if (request.Columnas != null && request.Columnas.Count > 0)
                {
                    foreach (var col in request.Columnas)
                    {
                        if (ColumnasPermitidas.Contains(col))
                            columnas.Add(col);
                    }
                }

                var selectCols = columnas.Count > 0 ? string.Join(", ", columnas) : "*";

                // Construir WHERE dinámico
                var condiciones = new List<string>();
                var parametros = new List<SqlParameter>();

                // Validar campo de fecha (whitelist para seguridad)
                var campoFecha = CamposFechaPermitidos.Contains(request.CampoFecha ?? "")
                    ? request.CampoFecha
                    : "FechaEntrada";

                if (!string.IsNullOrWhiteSpace(request.FechaInicio))
                {
                    condiciones.Add($"{campoFecha} >= @FechaInicio");
                    parametros.Add(new SqlParameter("@FechaInicio", request.FechaInicio));
                }

                if (!string.IsNullOrWhiteSpace(request.FechaFin))
                {
                    condiciones.Add($"{campoFecha} <= @FechaFin");
                    parametros.Add(new SqlParameter("@FechaFin", request.FechaFin));
                }

                if (!string.IsNullOrWhiteSpace(request.Estado))
                {
                    condiciones.Add("Estado = @Estado");
                    parametros.Add(new SqlParameter("@Estado", request.Estado));
                }

                if (!string.IsNullOrWhiteSpace(request.Placa))
                {
                    condiciones.Add("Placa LIKE @Placa");
                    parametros.Add(new SqlParameter("@Placa", $"%{request.Placa}%"));
                }

                if (request.SoloPagados.HasValue)
                {
                    condiciones.Add("bitPaid = @BitPaid");
                    parametros.Add(new SqlParameter("@BitPaid", request.SoloPagados.Value ? 1 : 0));
                }

                if (!string.IsNullOrWhiteSpace(request.StrRateKey))
                {
                    condiciones.Add("strRateKey = @StrRateKey");
                    parametros.Add(new SqlParameter("@StrRateKey", request.StrRateKey));
                }

                var whereClause = condiciones.Count > 0 ? "WHERE " + string.Join(" AND ", condiciones) : "";
                var top = request.Top > 0 && request.Top <= 5000 ? request.Top : 500;

                // campoFecha is safe to interpolate: it was validated against CamposFechaPermitidos whitelist above
                var sql = $"SELECT TOP {top} {selectCols} FROM IOT_Vehiculos {whereClause} ORDER BY {campoFecha} DESC";

                _logger.LogInformation("Reporte vehículos SQL: {SQL}", sql);

                var dt = await _db.ExecuteQueryAsync(sql, parametros.ToArray());

                // Convertir a lista de diccionarios (dinámico según columnas)
                var resultado = new List<Dictionary<string, object>>();
                foreach (DataRow row in dt.Rows)
                {
                    var dict = new Dictionary<string, object>();
                    foreach (DataColumn col in dt.Columns)
                    {
                        dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                    }
                    resultado.Add(dict);
                }

                return resultado;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en reporte de vehículos");
                throw;
            }
        }

        // =============================================
        // DASHBOARD DE VEHÍCULOS
        // =============================================

        public async Task<DashboardVehiculosResponse> ObtenerDashboardAsync()
        {
            try
            {
                // Calcular rangos en C# para mayor claridad y evitar expresiones complejas en SQL
                var hoy = DateTime.Today;
                var inicioHoy = hoy;
                var finHoy = hoy.AddDays(1);

                // Semana ISO: Lunes 00:00:00 – Domingo 23:59:59
                // DayOfWeek: Sunday=0, Monday=1, ..., Saturday=6
                var diasDesdeElLunes = hoy.DayOfWeek == DayOfWeek.Sunday ? 6 : (int)hoy.DayOfWeek - 1;
                var inicioSemana = hoy.AddDays(-diasDesdeElLunes);
                var finSemana = inicioSemana.AddDays(7);

                const string sql = @"
SELECT
    COUNT(CASE WHEN Estado = 'DENTRO' THEN 1 END) AS VehiculosDentro,
    COUNT(CASE WHEN FechaEntrada >= @InicioHoy AND FechaEntrada < @FinHoy THEN 1 END) AS TotalVehiculosHoy,
    COUNT(CASE WHEN Estado = 'DENTRO' AND FechaEntrada >= @InicioSemana AND FechaEntrada < @FinSemana THEN 1 END) AS VehiculosDentroSemana,
    AVG(CASE WHEN FechaSalida IS NOT NULL THEN CAST(DATEDIFF(minute, FechaEntrada, FechaSalida) AS FLOAT) END) AS TiempoPromedioEstanciaMin,
    AVG(CASE WHEN bitPaid = 1 AND Monto IS NOT NULL THEN Monto END) AS MontoPromedioCobrado
FROM IOT_Vehiculos";

                var parametros = new SqlParameter[]
                {
                    new SqlParameter("@InicioHoy",    inicioHoy),
                    new SqlParameter("@FinHoy",       finHoy),
                    new SqlParameter("@InicioSemana", inicioSemana),
                    new SqlParameter("@FinSemana",    finSemana)
                };

                var dt = await _db.ExecuteQueryAsync(sql, parametros);

                var response = new DashboardVehiculosResponse
                {
                    GeneratedAt = DateTime.UtcNow
                };

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    response.VehiculosDentro        = row["VehiculosDentro"]        == DBNull.Value ? 0 : Convert.ToInt32(row["VehiculosDentro"]);
                    response.TotalVehiculosHoy       = row["TotalVehiculosHoy"]      == DBNull.Value ? 0 : Convert.ToInt32(row["TotalVehiculosHoy"]);
                    response.VehiculosDentroSemana   = row["VehiculosDentroSemana"]  == DBNull.Value ? 0 : Convert.ToInt32(row["VehiculosDentroSemana"]);
                    response.TiempoPromedioEstanciaMin = row["TiempoPromedioEstanciaMin"] == DBNull.Value ? null : Convert.ToDouble(row["TiempoPromedioEstanciaMin"]);
                    response.MontoPromedioCobrado    = row["MontoPromedioCobrado"]   == DBNull.Value ? null : Convert.ToDecimal(row["MontoPromedioCobrado"]);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener dashboard de vehículos");
                throw;
            }
        }
    }
}