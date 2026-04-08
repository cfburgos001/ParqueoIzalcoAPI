using System.Data;
using Microsoft.Data.SqlClient;
using ParqueoIzalcoAPI.Models;

namespace ParqueoIzalcoAPI.Services
{
    public class VehiculosService : IVehiculosService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<VehiculosService> _logger;

        public VehiculosService(IDatabaseService db, ILogger<VehiculosService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<EntradaAutomaticaResponse> RegistrarEntradaAutomaticaAsync(EntradaAutomaticaRequest request)
        {
            try
            {
                var rateKey = string.IsNullOrWhiteSpace(request.StrRateKey) ? "A" : request.StrRateKey;
                var idDispositivo = string.IsNullOrWhiteSpace(request.IdDispositivo) ? "" : request.IdDispositivo;

                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@IdDispositivo", idDispositivo),
                    new SqlParameter("@strRateKey", rateKey),
                    new SqlParameter("@IdOperador", request.IdOperador) 
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_RegistrarEntradaAutomatica @IdDispositivo, @strRateKey, @IdOperador",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);

                    return new EntradaAutomaticaResponse
                    {
                        Exitoso = exitoso,
                        Mensaje = row["Mensaje"]?.ToString() ?? "",
                        Id = row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null,
                        CodigoBarras = row["CodigoBarras"]?.ToString()
                    };
                }

                return new EntradaAutomaticaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar entrada automática desde dispositivo {Dispositivo}. Detalle: {Error}", request.IdDispositivo, ex.Message);
                return new EntradaAutomaticaResponse { Exitoso = false, Mensaje = $"Error de BD: {ex.Message}" };
            }
        }
        public async Task<SalidaAutomaticaResponse> ProcesarSalidaAutomaticaAsync(SalidaAutomaticaRequest request)
        {
            try
            {
                // 1. Consultar el estado del monto y tiempo de gracia
                var paramCalcular = new SqlParameter[] { new SqlParameter("@Placa", request.Placa) };
                var dtCalculo = await _db.ExecuteQueryAsync("EXEC IOT_sp_CalcularMonto @Placa", paramCalcular);

                if (dtCalculo.Rows.Count == 0)
                {
                    return new SalidaAutomaticaResponse { Exitoso = false, Mensaje = "No se pudo consultar el ticket", EstadoCobro = "ERROR" };
                }

                var row = dtCalculo.Rows[0];
                var estadoCobro = row["EstadoCobro"]?.ToString() ?? "ERROR";
                var yaPago = Convert.ToBoolean(row["YaPago"]);

                // 2. TOMA DE DECISIONES BASADA EN EL ESTADO
                if (estadoCobro == "GRATIS" || estadoCobro == "GRACIA_ENTRADA" || estadoCobro == "GRACIA_SALIDA")
                {
                    // TODO ESTÁ BIEN: Registramos salida normal (Abre barrera)
                    var paramSalida = new SqlParameter[]
                    {
                        new SqlParameter("@Placa", request.Placa),
                        new SqlParameter("@IdDispositivo", request.IdDispositivo)
                    };

                    var dtSalida = await _db.ExecuteQueryAsync("EXEC IOT_sp_RegistrarSalida @Placa, @IdDispositivo", paramSalida);

                    return new SalidaAutomaticaResponse
                    {
                        Exitoso = true,
                        Mensaje = "Salida autorizada. ¡Buen viaje!",
                        EstadoCobro = estadoCobro
                    };
                }
                else if (estadoCobro == "DEBE_PAGAR")
                {
                    if (!yaPago)
                    {
                        // NO HA PAGADO NUNCA (NO ABRE BARRERA)
                        return new SalidaAutomaticaResponse
                        {
                            Exitoso = false,
                            Mensaje = "El ticket no ha sido pagado. Favor pasar a la estación de pago.",
                            EstadoCobro = "DEBE_PAGAR"
                        };
                    }
                    else
                    {
                        // YA PAGÓ, PERO SE TARDÓ MÁS DE 15 MINUTOS (REINGRESO) (NO ABRE BARRERA)
                        var paramReingreso = new SqlParameter[]
                        {
                            new SqlParameter("@Placa", request.Placa),
                            new SqlParameter("@IdDispositivoSalida", request.IdDispositivo)
                        };

                        var dtReingreso = await _db.ExecuteQueryAsync("EXEC IOT_sp_RegistrarSalidaYReingresoPorGracia @Placa, @IdDispositivoSalida", paramReingreso);

                        string mensajeReingreso = "Tiempo de gracia excedido. Debe pagar penalidad.";
                        if (dtReingreso.Rows.Count > 0)
                        {
                            mensajeReingreso = dtReingreso.Rows[0]["Mensaje"]?.ToString() ?? mensajeReingreso;
                        }

                        return new SalidaAutomaticaResponse
                        {
                            Exitoso = false, // Es false porque la barrera NO se debe abrir, el cliente debe ir a pagar la penalidad
                            Mensaje = mensajeReingreso,
                            EstadoCobro = "REINGRESO_GENERADO"
                        };
                    }
                }

                // Si llega aquí es porque devolvió 'ERROR' (ej. ticket no existe)
                return new SalidaAutomaticaResponse { Exitoso = false, Mensaje = "Ticket no válido o ya procesado.", EstadoCobro = "ERROR" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al procesar salida automática para placa {Placa}", request.Placa);
                return new SalidaAutomaticaResponse { Exitoso = false, Mensaje = $"Error interno: {ex.Message}", EstadoCobro = "ERROR" };
            }
        }
    }
}