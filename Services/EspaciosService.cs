using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface IEspaciosService
    {
        Task<EspaciosDisponiblesResponse> ObtenerDisponibilidadAsync();
    }

    public class EspaciosService : IEspaciosService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<EspaciosService> _logger;

        public EspaciosService(IDatabaseService db, ILogger<EspaciosService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EspaciosDisponiblesResponse> ObtenerDisponibilidadAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ObtenerEspaciosDisponibles");

                if (dt.Rows.Count == 0)
                {
                    _logger.LogWarning("IOT_sp_ObtenerEspaciosDisponibles no retornó filas");
                    return new EspaciosDisponiblesResponse();
                }

                var row = dt.Rows[0];

                return new EspaciosDisponiblesResponse
                {
                    TotalEspacios = Convert.ToInt32(row["TotalEspacios"]),
                    VehiculosDentro = Convert.ToInt32(row["VehiculosDentro"]),
                    EspaciosDisponibles = Convert.ToInt32(row["EspaciosDisponibles"]),
                    EstadoCapacidad = row["EstadoCapacidad"]?.ToString() ?? "DISPONIBLE"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener disponibilidad de espacios");
                throw;
            }
        }
    }
}