using DataparkBarreraAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataparkBarreraAPI.Services
{
    public interface ITarifasService
    {
        Task<List<Tarifa>> ListarTarifasAsync();
        Task<TarifaResponse> ActualizarTarifaAsync(ActualizarTarifaRequest request);
    }

    public class TarifasService : ITarifasService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<TarifasService> _logger;

        public TarifasService(IDatabaseService db, ILogger<TarifasService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Tarifa>> ListarTarifasAsync()
        {
            try
            {
                const string sql = @"
                    SELECT
                        Id, TipoTarifa, strRateKey, TipoVehiculo, Descripcion,
                        PrecioPorHora, PrecioMinimo,
                        ISNULL(PrecioMax,         0) AS PrecioMax,
                        ISNULL(Precio1Hora,       0) AS Precio1Hora,
                        ISNULL(Precio2Horas,      0) AS Precio2Horas,
                        ISNULL(PrecioDiaCompleto, 0) AS PrecioDiaCompleto,
                        ISNULL(CobroIndefinido,   0) AS CobroIndefinido,
                        ISNULL(Activa,            0) AS Activa
                    FROM dbo.IOT_Tarifas
                    ORDER BY Id";

                var dt = await _db.ExecuteQueryAsync(sql);
                var list = new List<Tarifa>();

                foreach (DataRow row in dt.Rows)
                {
                    list.Add(new Tarifa
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        TipoTarifa = row["TipoTarifa"]?.ToString() ?? "",
                        StrRateKey = row["strRateKey"]?.ToString() ?? "",
                        TipoVehiculo = row["TipoVehiculo"]?.ToString(),
                        Descripcion = row["Descripcion"]?.ToString(),
                        PrecioPorHora = Convert.ToDecimal(row["PrecioPorHora"]),
                        PrecioMinimo = Convert.ToDecimal(row["PrecioMinimo"]),
                        PrecioMax = Convert.ToDecimal(row["PrecioMax"]),
                        Precio1Hora = Convert.ToDecimal(row["Precio1Hora"]),
                        Precio2Horas = Convert.ToDecimal(row["Precio2Horas"]),
                        PrecioDiaCompleto = Convert.ToDecimal(row["PrecioDiaCompleto"]),
                        CobroIndefinido = Convert.ToBoolean(row["CobroIndefinido"]),
                        Activa = Convert.ToBoolean(row["Activa"])
                    });
                }
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tarifas");
                return new List<Tarifa>();
            }
        }

        public async Task<TarifaResponse> ActualizarTarifaAsync(ActualizarTarifaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id",                request.Id),
                    new SqlParameter("@PrecioPorHora",     request.PrecioPorHora),
                    new SqlParameter("@PrecioMinimo",      request.PrecioMinimo),
                    new SqlParameter("@PrecioMax",         request.PrecioMax),
                    new SqlParameter("@Precio1Hora",       request.Precio1Hora),
                    new SqlParameter("@Precio2Horas",      request.Precio2Horas),
                    new SqlParameter("@PrecioDiaCompleto", request.PrecioDiaCompleto),
                    new SqlParameter("@Activa",            request.Activa)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarTarifa @Id, @PrecioPorHora, @PrecioMinimo, @PrecioMax, " +
                    "@Precio1Hora, @Precio2Horas, @PrecioDiaCompleto, @Activa",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);
                    if (exitoso) _logger.LogInformation("✓ Tarifa actualizada ID: {Id}", request.Id);
                    return new TarifaResponse
                    {
                        Exitoso = exitoso,
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null
                    };
                }
                return new TarifaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tarifa ID: {Id}", request.Id);
                return new TarifaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }
    }
}