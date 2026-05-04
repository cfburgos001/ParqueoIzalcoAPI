using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface IAccesoService
    {
        Task<ValidacionAccesoResponse> ValidarAccesoTarjetaAsync(ValidarAccesoRequest request);
        Task<ValidacionAccesoResponse> RegistrarEntradaTarjetaAsync(RegistrarMovimientoTarjetaRequest request);
        Task<ValidacionAccesoResponse> RegistrarSalidaTarjetaAsync(RegistrarMovimientoTarjetaRequest request);
    }

    public class AccesoService : IAccesoService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<AccesoService> _logger;

        public AccesoService(IDatabaseService db, ILogger<AccesoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ValidacionAccesoResponse> ValidarAccesoTarjetaAsync(ValidarAccesoRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@NumeroTarjeta", request.NumeroTarjeta),
                    new("@IdDispositivo", request.IdDispositivo)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ValidarAccesoTarjeta @NumeroTarjeta, @IdDispositivo", parameters);
                return ParseAccesoResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar acceso de tarjeta");
                return new ValidacionAccesoResponse { Acceso = false, Mensaje = "Error interno al procesar la solicitud", MotivoRechazo = "Error interno" };
            }
        }

        public async Task<ValidacionAccesoResponse> RegistrarEntradaTarjetaAsync(RegistrarMovimientoTarjetaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@NumeroTarjeta", request.NumeroTarjeta),
                    new("@IdDispositivo", request.IdDispositivo)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_RegistrarEntradaTarjeta @NumeroTarjeta, @IdDispositivo", parameters);
                return ParseAccesoResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada tarjeta");
                return new ValidacionAccesoResponse { Acceso = false, Mensaje = "Error interno al procesar la solicitud", MotivoRechazo = "Error interno" };
            }
        }

        public async Task<ValidacionAccesoResponse> RegistrarSalidaTarjetaAsync(RegistrarMovimientoTarjetaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@NumeroTarjeta", request.NumeroTarjeta),
                    new("@IdDispositivo", request.IdDispositivo)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_RegistrarSalidaTarjeta @NumeroTarjeta, @IdDispositivo", parameters);
                return ParseAccesoResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar salida tarjeta");
                return new ValidacionAccesoResponse { Acceso = false, Mensaje = "Error interno al procesar la solicitud", MotivoRechazo = "Error interno" };
            }
        }

        private static ValidacionAccesoResponse ParseAccesoResponse(DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return new ValidacionAccesoResponse { Acceso = false, Mensaje = "Sin respuesta del servidor", MotivoRechazo = "Sin respuesta" };

            var row = dt.Rows[0];
            bool acceso = dt.Columns.Contains("Acceso") && Convert.ToBoolean(row["Acceso"]);

            return new ValidacionAccesoResponse
            {
                Acceso        = acceso,
                Mensaje       = dt.Columns.Contains("Mensaje")        ? row["Mensaje"]?.ToString()       ?? "" : (acceso ? "Acceso permitido" : "Acceso denegado"),
                NombreUsuario = dt.Columns.Contains("NombreUsuario")  ? row["NombreUsuario"]?.ToString() : null,
                NombreCuenta  = dt.Columns.Contains("NombreCuenta")   ? row["NombreCuenta"]?.ToString()  : null,
                PlacaVehiculo = dt.Columns.Contains("PlacaVehiculo")  ? row["PlacaVehiculo"]?.ToString() : null,
                CodigoBarras  = dt.Columns.Contains("CodigoBarras")   ? row["CodigoBarras"]?.ToString()  : null,
                MotivoRechazo = dt.Columns.Contains("MotivoRechazo")  ? row["MotivoRechazo"]?.ToString() : (!acceso ? "Acceso denegado" : null)
            };
        }
    }
}
