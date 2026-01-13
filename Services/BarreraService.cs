using DataparkBarreraAPI.Models;
using System.Data;

namespace DataparkBarreraAPI.Services
{
    public interface IBarreraService
    {
        Task<Barrera?> ObtenerEstadoBarreraAsync();
        Task<bool> CerrarBarreraAsync(string motivo = "Vehículo pasó");
        Task<bool> AbrirBarreraManualAsync();
        Task<bool> ResetearBarreraAsync();
        Task<BarreraEstadisticas> ObtenerEstadisticasAsync();
        Task<List<BarreraLog>> ObtenerHistorialAsync(int ultimosRegistros = 10);
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

        /// <summary>
        /// Obtiene el estado actual de la barrera
        /// </summary>
        public async Task<Barrera?> ObtenerEstadoBarreraAsync()
        {
            try
            {
                var sql = @"
                    SELECT 
                        ID,
                        BarreraSeteo,
                        EstadoBarrera,
                        ComandoBarrera,
                        FechaUltimaActualizacion
                    FROM IOT_Barrera
                    WHERE ID = 1";

                var dt = await _db.ExecuteQueryAsync(sql);

                if (dt.Rows.Count == 0)
                {
                    _logger.LogWarning("No se encontró la barrera con ID = 1");
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
                _logger.LogError(ex, "Error al obtener estado de barrera");
                throw;
            }
        }

        /// <summary>
        /// Cierra la barrera (simula que el controlino detectó que pasó el vehículo)
        /// </summary>
        public async Task<bool> CerrarBarreraAsync(string motivo = "Vehículo pasó")
        {
            try
            {
                // Ejecutar el SP que cierra la barrera
                var sql = "EXEC IOT_sp_CerrarBarreraManual";
                var result = await _db.ExecuteNonQueryAsync(sql);

                _logger.LogInformation("✓ Barrera cerrada - Motivo: {Motivo}", motivo);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar barrera");
                return false;
            }
        }

        /// <summary>
        /// Abre la barrera manualmente (emergencias)
        /// </summary>
        public async Task<bool> AbrirBarreraManualAsync()
        {
            try
            {
                var sql = @"
                    UPDATE IOT_Barrera
                    SET 
                        EstadoBarrera = 1,
                        ComandoBarrera = 1,
                        FechaUltimaActualizacion = GETDATE()
                    WHERE ID = 1";

                await _db.ExecuteNonQueryAsync(sql);
                _logger.LogInformation("✓ Barrera abierta manualmente");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al abrir barrera manualmente");
                return false;
            }
        }

        /// <summary>
        /// Resetea la barrera a estado cerrado
        /// </summary>
        public async Task<bool> ResetearBarreraAsync()
        {
            try
            {
                var sql = "EXEC IOT_sp_ResetearBarrera";
                await _db.ExecuteNonQueryAsync(sql);
                _logger.LogInformation("✓ Barrera reseteada");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al resetear barrera");
                return false;
            }
        }

        /// <summary>
        /// Obtiene estadísticas de uso de la barrera
        /// </summary>
        public async Task<BarreraEstadisticas> ObtenerEstadisticasAsync()
        {
            try
            {
                var sql = @"
                    SELECT 
                        COUNT(*) as TotalMovimientos,
                        MAX(FechaUltimaActualizacion) as UltimaActividad
                    FROM IOT_Barrera
                    WHERE ID = 1";

                var dt = await _db.ExecuteQueryAsync(sql);
                var row = dt.Rows[0];

                // Contar entradas y salidas del día actual
                var sqlHoy = @"
                    SELECT 
                        SUM(CASE WHEN bitEntry = 1 THEN 1 ELSE 0 END) as Entradas,
                        SUM(CASE WHEN bitExit = 1 THEN 1 ELSE 0 END) as Salidas
                    FROM IOT_Vehiculos
                    WHERE CAST(FechaEntrada AS DATE) = CAST(GETDATE() AS DATE)";

                var dtHoy = await _db.ExecuteQueryAsync(sqlHoy);
                var rowHoy = dtHoy.Rows[0];

                return new BarreraEstadisticas
                {
                    TotalAperturas = Convert.ToInt32(rowHoy["Entradas"] ?? 0) +
                                    Convert.ToInt32(rowHoy["Salidas"] ?? 0),
                    TotalCierres = Convert.ToInt32(rowHoy["Entradas"] ?? 0) +
                                  Convert.ToInt32(rowHoy["Salidas"] ?? 0),
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

        /// <summary>
        /// Obtiene el historial de movimientos
        /// </summary>
        public async Task<List<BarreraLog>> ObtenerHistorialAsync(int ultimosRegistros = 10)
        {
            try
            {
                var sql = $@"
                    SELECT TOP {ultimosRegistros}
                        CASE 
                            WHEN bitEntry = 1 THEN FechaEntrada
                            WHEN bitExit = 1 THEN FechaSalida
                        END as Fecha,
                        CASE 
                            WHEN bitEntry = 1 THEN 'ENTRADA'
                            WHEN bitExit = 1 THEN 'SALIDA'
                        END as Accion,
                        Placa,
                        CASE 
                            WHEN bitEntry = 1 THEN 'Entrada'
                            WHEN bitExit = 1 THEN 'Salida'
                        END as TipoMovimiento
                    FROM IOT_Vehiculos
                    WHERE (bitEntry = 1 OR bitExit = 1)
                    ORDER BY CASE 
                        WHEN bitEntry = 1 THEN FechaEntrada
                        WHEN bitExit = 1 THEN FechaSalida
                    END DESC";

                var dt = await _db.ExecuteQueryAsync(sql);
                var logs = new List<BarreraLog>();

                foreach (DataRow row in dt.Rows)
                {
                    logs.Add(new BarreraLog
                    {
                        Fecha = Convert.ToDateTime(row["Fecha"]),
                        Accion = row["Accion"].ToString() ?? "",
                        PlacaVehiculo = row["Placa"].ToString(),
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
    }
}