using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface IMonitoreoService
    {
        Task<List<MonitoreoLogItem>> ObtenerLogsAsync(int top, string? prioridad, int? ultimoId);
    }

    public class MonitoreoService : IMonitoreoService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<MonitoreoService> _logger;

        public MonitoreoService(IDatabaseService db, ILogger<MonitoreoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<MonitoreoLogItem>> ObtenerLogsAsync(int top, string? prioridad, int? ultimoId)
        {
            try
            {
                if (top <= 0 || top > 500) top = 200;

                // Whitelist de prioridad para evitar inyección
                string? prioFinal = prioridad?.ToUpper() switch
                {
                    "ALTA" => "ALTA",
                    "MEDIA" => "MEDIA",
                    "BAJA" => "BAJA",
                    _ => null
                };

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Top",       top),
                    new SqlParameter("@Prioridad", (object?)prioFinal ?? DBNull.Value),
                    new SqlParameter("@UltimoId",  (object?)ultimoId  ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_MonitoreoLogs @Top, @Prioridad, @UltimoId",
                    parameters);

                var lista = new List<MonitoreoLogItem>();

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new MonitoreoLogItem
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        IdTipoLog = Convert.ToInt32(row["IdTipoLog"]),
                        TipoLog = row["TipoLog"]?.ToString() ?? "",
                        Placa = row["Placa"] == DBNull.Value ? null : row["Placa"].ToString(),
                        Datos = row["Datos"] == DBNull.Value ? null : row["Datos"].ToString(),
                        IdDispositivo = row["IdDispositivo"] == DBNull.Value ? null : row["IdDispositivo"].ToString(),
                        NombreDispositivo = row["NombreDispositivo"] == DBNull.Value ? null : row["NombreDispositivo"].ToString(),
                        FechaEvento = Convert.ToDateTime(row["FechaEvento"]),
                        Prioridad = row["Prioridad"]?.ToString() ?? "BAJA"
                    });
                }

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error en MonitoreoService.ObtenerLogsAsync");
                throw;
            }
        }
    }
}