using ParqueoIzalcoAPI.Models;
using ParqueoIzalcoAPI.Models.ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface ITicketsService
    {
        Task<List<TicketAntiguo>> ListarTicketsAntiguosAsync();
        Task<CerrarTicketResponse> CerrarTicketAsync(CerrarTicketRequest request);
        Task<CerrarTicketsMasivosResponse> CerrarTicketsMasivosAsync(CerrarTicketsMasivosRequest request);
    }

    public class TicketsService : ITicketsService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<TicketsService> _logger;

        public TicketsService(IDatabaseService db, ILogger<TicketsService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Lista tickets con Estado='DENTRO' cuya FechaEntrada es anterior a hoy.
        /// </summary>
        public async Task<List<TicketAntiguo>> ListarTicketsAntiguosAsync()
        {
            try
            {
                const string sql = @"
                    SELECT
                        Id,
                        Placa,
                        CodigoBarras,
                        FechaEntrada,
                        DATEDIFF(DAY, FechaEntrada, GETDATE()) AS DiasAdentro,
                        NombreOperador,
                        strRateKey,
                        IdDispositivoEntrada,
                        IdOperador
                    FROM dbo.IOT_Vehiculos
                    WHERE Estado = 'DENTRO'
                      AND CAST(FechaEntrada AS DATE) < CAST(GETDATE() AS DATE)
                    ORDER BY FechaEntrada ASC";

                var dt = await _db.ExecuteQueryAsync(sql);
                var lista = new List<TicketAntiguo>();

                foreach (DataRow row in dt.Rows)
                {
                    lista.Add(new TicketAntiguo
                    {
                        Id = Convert.ToInt32(row["Id"]),
                        Placa = row["Placa"]?.ToString() ?? "",
                        CodigoBarras = row["CodigoBarras"]?.ToString() ?? "",
                        FechaEntrada = Convert.ToDateTime(row["FechaEntrada"]),
                        DiasAdentro = Convert.ToInt32(row["DiasAdentro"]),
                        NombreOperador = row["NombreOperador"]?.ToString(),
                        StrRateKey = row["strRateKey"]?.ToString(),
                        IdDispositivoEntrada = row["IdDispositivoEntrada"]?.ToString(),
                        IdOperador = row["IdOperador"] != DBNull.Value
                                            ? Convert.ToInt32(row["IdOperador"])
                                            : null
                    });
                }

                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tickets antiguos");
                return new List<TicketAntiguo>();
            }
        }

        /// <summary>
        /// Cierra un ticket antiguo individual usando el SP IOT_sp_CerrarTicketAntiguo.
        /// </summary>
        public async Task<CerrarTicketResponse> CerrarTicketAsync(CerrarTicketRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Id",             request.Id),
                    new SqlParameter("@Monto",          request.Monto),
                    new SqlParameter("@IdOperador",     request.IdOperador),
                    new SqlParameter("@NombreOperador", (object?)request.NombreOperador ?? DBNull.Value)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CerrarTicketAntiguo @Id, @Monto, @IdOperador, @NombreOperador",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);

                    if (exitoso)
                    {
                        _logger.LogInformation(
                            "✓ Ticket cerrado - ID: {Id}, Placa: {Placa}, Monto: ${Monto}",
                            request.Id,
                            row["Placa"]?.ToString(),
                            request.Monto
                        );
                    }

                    return new CerrarTicketResponse
                    {
                        Exitoso = exitoso,
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        IdVehiculo = row["IdVehiculo"] != DBNull.Value ? Convert.ToInt32(row["IdVehiculo"]) : null,
                        Placa = row["Placa"]?.ToString(),
                        Monto = row["Monto"] != DBNull.Value ? Convert.ToDecimal(row["Monto"]) : null,
                        FechaSalida = row["FechaSalida"] != DBNull.Value ? Convert.ToDateTime(row["FechaSalida"]) : null,
                        TiempoEstancia = row["TiempoEstancia"] != DBNull.Value ? Convert.ToInt32(row["TiempoEstancia"]) : null
                    };
                }

                return new CerrarTicketResponse
                {
                    Exitoso = false,
                    Mensaje = "Sin respuesta del procedimiento"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cerrar ticket ID: {Id}", request.Id);
                return new CerrarTicketResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Cierra múltiples tickets en una sola operación.
        /// Procesa cada uno individualmente y acumula resultados.
        /// </summary>
        public async Task<CerrarTicketsMasivosResponse> CerrarTicketsMasivosAsync(CerrarTicketsMasivosRequest request)
        {
            var resultados = new List<CerrarTicketResponse>();
            int exitosos = 0;
            int fallidos = 0;

            foreach (var item in request.Tickets)
            {
                var ticketRequest = new CerrarTicketRequest
                {
                    Id = item.Id,
                    Monto = item.Monto,
                    IdOperador = request.IdOperador,
                    NombreOperador = request.NombreOperador
                };

                var resultado = await CerrarTicketAsync(ticketRequest);
                resultados.Add(resultado);

                if (resultado.Exitoso)
                    exitosos++;
                else
                    fallidos++;
            }

            var totalProcesados = request.Tickets.Count;
            string mensaje;

            if (fallidos == 0)
                mensaje = $"✅ {exitosos} tickets cerrados correctamente";
            else if (exitosos == 0)
                mensaje = $"❌ No se pudo cerrar ningún ticket ({fallidos} fallidos)";
            else
                mensaje = $"⚠️ {exitosos} cerrados, {fallidos} con error";

            return new CerrarTicketsMasivosResponse
            {
                Exitoso = exitosos > 0,
                Mensaje = mensaje,
                TotalProcesados = totalProcesados,
                TotalExitosos = exitosos,
                TotalFallidos = fallidos,
                Resultados = resultados
            };
        }
    }
}