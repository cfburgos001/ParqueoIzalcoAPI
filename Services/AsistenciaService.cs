using Microsoft.Data.SqlClient;
using ParqueoIzalcoAPI.Controllers;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface IAsistenciaService
    {
        Task<TicketExtraviadoResult> CrearTicketExtraviadoAsync(string idDispositivo, int idOperador);
        Task<TicketExtraviadoResult> CrearTicketExtraviadoPesadoAsync(string idDispositivo, int idOperador);
    }

    public class AsistenciaService : IAsistenciaService
    {
        private readonly IDatabaseService _databaseService;

        public AsistenciaService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<TicketExtraviadoResult> CrearTicketExtraviadoAsync(string idDispositivo, int idOperador)
        {
            return await EjecutarSP("IOT_sp_CrearTicketExtraviado", idDispositivo, idOperador);
        }

        public async Task<TicketExtraviadoResult> CrearTicketExtraviadoPesadoAsync(string idDispositivo, int idOperador)
        {
            return await EjecutarSP("IOT_sp_CrearTicketExtraviadoPesado", idDispositivo, idOperador);
        }

        private async Task<TicketExtraviadoResult> EjecutarSP(string sp, string idDispositivo, int idOperador)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdDispositivo", idDispositivo),
                    new SqlParameter("@IdOperador",    idOperador)
                };

                DataTable dt = await _databaseService.ExecuteQueryAsync(
                    $"EXEC dbo.{sp} @IdDispositivo, @IdOperador",
                    parameters);

                if (dt == null || dt.Rows.Count == 0)
                    return Error("Sin respuesta del servidor.");

                DataRow row = dt.Rows[0];

                return new TicketExtraviadoResult
                {
                    Exitoso = Convert.ToBoolean(row["Exitoso"]),
                    Mensaje = row["Mensaje"]?.ToString() ?? string.Empty,
                    Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null,
                    Placa = row["Placa"]?.ToString(),
                    CodigoBarras = row["CodigoBarras"]?.ToString(),
                    Monto = row["Monto"] != DBNull.Value ? Convert.ToDecimal(row["Monto"]) : null
                };
            }
            catch (Exception ex)
            {
                return Error($"Error interno: {ex.Message}");
            }
        }

        private static TicketExtraviadoResult Error(string msg)
        {
            return new TicketExtraviadoResult { Exitoso = false, Mensaje = msg };
        }
    }
}