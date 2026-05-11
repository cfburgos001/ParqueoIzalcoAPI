using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface IBarreraService
    {
        Task<List<Barrera>> ListarBarrerasAsync();
        Task<Barrera?> ObtenerEstadoBarreraAsync(int idBarrera = 1);
        Task<bool> CerrarBarreraAsync(string motivo = "Vehículo pasó", int idBarrera = 1);
        Task<bool> AbrirBarreraManualAsync(int idBarrera = 1);
        Task<bool> ResetearBarreraAsync(int idBarrera = 1);
        Task<BarreraEstadisticas> ObtenerEstadisticasAsync();
        Task<List<BarreraLog>> ObtenerHistorialAsync(int ultimosRegistros = 10);
        Task<RegistroLogResponse> RegistrarLogAsync(RegistroLogRequest request);
    }

    public class BarreraService : IBarreraService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<BarreraService> _logger;

        public BarreraService(IDatabaseService db, ILogger<BarreraService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Barrera>> ListarBarrerasAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC dbo.IOT_sp_ListarBarreras");
                var barreras = new List<Barrera>();

                foreach (DataRow row in dt.Rows)
                {
                    barreras.Add(new Barrera
                    {
                        ID = Convert.ToInt32(row["ID"]),
                        BarreraSeteo = row["BarreraSeteo"].ToString() ?? "",
                        EstadoBarrera = Convert.ToBoolean(row["EstadoBarrera"]),
                        ComandoBarrera = Convert.ToBoolean(row["ComandoBarrera"]),
                        FechaUltimaActualizacion = Convert.ToDateTime(row["FechaUltimaActualizacion"])
                    });
                }

                return barreras;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar barreras");
                throw;
            }
        }

        public async Task<Barrera?> ObtenerEstadoBarreraAsync(int idBarrera = 1)
        {
            try
            {
                var sql = @"
                    SELECT ID, BarreraSeteo, EstadoBarrera, ComandoBarrera, FechaUltimaActualizacion
                    FROM IOT_Barrera
                    WHERE ID = @IdBarrera";

                var dt = await _db.ExecuteQueryAsync(sql,
                    new SqlParameter[] { new SqlParameter("@IdBarrera", idBarrera) });

                if (dt.Rows.Count == 0)
                {
                    _logger.LogWarning("No se encontró la barrera con ID = {IdBarrera}", idBarrera);
                    return null;
                }

                var row = dt.Rows[0];
                return new Barrera
                {
                    ID = Convert.ToInt32(row["ID"]),
                    BarreraSeteo = row["BarreraSeteo"].ToString() ?? "",
                    EstadoBarrera = Convert.ToBoolean(row["EstadoBarrera"]),
                    ComandoBarrera = Convert.ToBoolean(row["ComandoBarrera"]),
                    FechaUltimaActualizacion = Convert.ToDateTime(row["FechaUltimaActualizacion"])
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estado de barrera {IdBarrera}", idBarrera);
                throw;
            }
        }

        public async Task<bool> CerrarBarreraAsync(string motivo = "Vehículo pasó", int idBarrera = 1)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "EXEC IOT_sp_CerrarBarreraManual @IdBarrera",
                    new SqlParameter[] { new SqlParameter("@IdBarrera", idBarrera) });

                _logger.LogInformation("✓ Barrera {IdBarrera} cerrada - Motivo: {Motivo}", idBarrera, motivo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar barrera {IdBarrera}", idBarrera);
                return false;
            }
        }

        public async Task<bool> AbrirBarreraManualAsync(int idBarrera = 1)
        {
            try
            {
                var sql = @"
                    UPDATE IOT_Barrera
                    SET EstadoBarrera = 1, ComandoBarrera = 1, FechaUltimaActualizacion = GETDATE()
                    WHERE ID = @IdBarrera";

                await _db.ExecuteNonQueryAsync(sql,
                    new SqlParameter[] { new SqlParameter("@IdBarrera", idBarrera) });

                _logger.LogInformation("✓ Barrera {IdBarrera} abierta manualmente", idBarrera);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir barrera {IdBarrera} manualmente", idBarrera);
                return false;
            }
        }

        public async Task<bool> ResetearBarreraAsync(int idBarrera = 1)
        {
            try
            {
                await _db.ExecuteNonQueryAsync(
                    "EXEC IOT_sp_ResetearBarrera @IdBarrera",
                    new SqlParameter[] { new SqlParameter("@IdBarrera", idBarrera) });

                _logger.LogInformation("✓ Barrera {IdBarrera} reseteada", idBarrera);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear barrera {IdBarrera}", idBarrera);
                return false;
            }
        }

        public async Task<BarreraEstadisticas> ObtenerEstadisticasAsync()
        {
            try
            {
                var sql = @"
                    SELECT COUNT(*) as TotalMovimientos, MAX(FechaUltimaActualizacion) as UltimaActividad
                    FROM IOT_Barrera";

                var dt = await _db.ExecuteQueryAsync(sql);
                var row = dt.Rows[0];

                var sqlHoy = @"
                    SELECT
                        SUM(CASE WHEN bitEntry = 1 THEN 1 ELSE 0 END) as Entradas,
                        SUM(CASE WHEN bitExit  = 1 THEN 1 ELSE 0 END) as Salidas
                    FROM IOT_Vehiculos
                    WHERE CAST(FechaEntrada AS DATE) = CAST(GETDATE() AS DATE)";

                var dtHoy = await _db.ExecuteQueryAsync(sqlHoy);
                var rowHoy = dtHoy.Rows[0];

                return new BarreraEstadisticas
                {
                    TotalAperturas = Convert.ToInt32(rowHoy["Entradas"] ?? 0) +
                                     Convert.ToInt32(rowHoy["Salidas"]  ?? 0),
                    TotalCierres   = Convert.ToInt32(rowHoy["Entradas"] ?? 0) +
                                     Convert.ToInt32(rowHoy["Salidas"]  ?? 0),
                    UltimaApertura = row["UltimaActividad"] != DBNull.Value
                        ? Convert.ToDateTime(row["UltimaActividad"])
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener estadísticas");
                return new BarreraEstadisticas();
            }
        }

        public async Task<List<BarreraLog>> ObtenerHistorialAsync(int ultimosRegistros = 10)
        {
            try
            {
                var sql = $@"
                    SELECT TOP {ultimosRegistros}
                        CASE WHEN bitEntry = 1 THEN FechaEntrada WHEN bitExit = 1 THEN FechaSalida END as Fecha,
                        CASE WHEN bitEntry = 1 THEN 'ENTRADA'    WHEN bitExit = 1 THEN 'SALIDA'    END as Accion,
                        Placa,
                        CASE WHEN bitEntry = 1 THEN 'Entrada'    WHEN bitExit = 1 THEN 'Salida'    END as TipoMovimiento
                    FROM IOT_Vehiculos
                    WHERE (bitEntry = 1 OR bitExit = 1)
                    ORDER BY CASE WHEN bitEntry = 1 THEN FechaEntrada WHEN bitExit = 1 THEN FechaSalida END DESC";

                var dt = await _db.ExecuteQueryAsync(sql);
                var logs = new List<BarreraLog>();

                foreach (DataRow row in dt.Rows)
                {
                    logs.Add(new BarreraLog
                    {
                        Fecha          = Convert.ToDateTime(row["Fecha"]),
                        Accion         = row["Accion"].ToString() ?? "",
                        PlacaVehiculo  = row["Placa"].ToString(),
                        TipoMovimiento = row["TipoMovimiento"].ToString()
                    });
                }

                return logs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener historial");
                return new List<BarreraLog>();
            }
        }

        public async Task<RegistroLogResponse> RegistrarLogAsync(RegistroLogRequest request)
        {
            try
            {
                if (request.IdTipoLog <= 0)
                    return new RegistroLogResponse { Exitoso = false, Mensaje = "IdTipoLog debe ser mayor a 0" };

                if (string.IsNullOrWhiteSpace(request.IdDispositivo))
                    request.IdDispositivo = "CONTROLLINO";

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdTipoLog",        request.IdTipoLog),
                    new SqlParameter("@Placa",            (object?)request.Placa            ?? DBNull.Value),
                    new SqlParameter("@IdDispositivo",    request.IdDispositivo),
                    new SqlParameter("@DatosAdicionales", (object?)request.DatosAdicionales ?? DBNull.Value)
                };

                await _db.ExecuteNonQueryAsync(
                    "EXEC IOT_sp_RegistroLog @IdTipoLog, @Placa, @IdDispositivo, @DatosAdicionales",
                    parameters);

                var dtLastId = await _db.ExecuteQueryAsync(
                    "SELECT TOP 1 Id, FechaEvento FROM IOT_Logs WHERE IdDispositivo = @IdDispositivo ORDER BY Id DESC",
                    new SqlParameter[] { new SqlParameter("@IdDispositivo", request.IdDispositivo) });

                int? idLog = null;
                DateTime? fechaRegistro = null;

                if (dtLastId.Rows.Count > 0)
                {
                    idLog = Convert.ToInt32(dtLastId.Rows[0]["Id"]);
                    fechaRegistro = Convert.ToDateTime(dtLastId.Rows[0]["FechaEvento"]);
                }

                _logger.LogInformation(
                    "✓ Log registrado - TipoLog: {TipoLog}, Dispositivo: {Dispositivo}, Placa: {Placa}",
                    request.IdTipoLog, request.IdDispositivo, request.Placa ?? "N/A");

                return new RegistroLogResponse
                {
                    Exitoso = true,
                    Mensaje = "Log registrado correctamente",
                    IdLog = idLog,
                    FechaRegistro = fechaRegistro ?? DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar log");
                return new RegistroLogResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }
    }
}
