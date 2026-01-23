using DataparkBarreraAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DataparkBarreraAPI.Services
{
    public interface IPagoService
    {
        Task<ConsultaPagoResponse> ConsultarPorCodigoBarrasAsync(string codigoBarras);
        Task<RegistrarPagoResponse> RegistrarPagoAsync(RegistrarPagoRequest request);
        Task<VerificarPagoResponse> VerificarPagoPorPlacaAsync(string placa);
    }

    public class PagoService : IPagoService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<PagoService> _logger;

        public PagoService(IDatabaseService db, ILogger<PagoService> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Consulta un vehículo por código de barras y calcula el monto a pagar
        /// Usado por la PayStation cuando se escanea el ticket
        /// </summary>
        public async Task<ConsultaPagoResponse> ConsultarPorCodigoBarrasAsync(string codigoBarras)
        {
            try
            {
                // 1. Buscar el vehículo por código de barras
                var sqlBuscar = @"
                    SELECT 
                        Id,
                        Placa,
                        CodigoBarras,
                        FechaEntrada,
                        strRateKey,
                        Estado,
                        bitPaid,
                        Monto,
                        FechaPago
                    FROM IOT_Vehiculos
                    WHERE CodigoBarras = @CodigoBarras
                    AND Estado = 'DENTRO'";

                var paramBuscar = new SqlParameter[]
                {
                    new SqlParameter("@CodigoBarras", codigoBarras)
                };

                var dtVehiculo = await _db.ExecuteQueryAsync(sqlBuscar, paramBuscar);

                if (dtVehiculo.Rows.Count == 0)
                {
                    _logger.LogWarning("No se encontró vehículo con código de barras: {CodigoBarras}", codigoBarras);
                    return new ConsultaPagoResponse
                    {
                        Exitoso = false,
                        Mensaje = "No se encontró el vehículo con ese código de barras o ya salió del parqueo"
                    };
                }

                var rowVehiculo = dtVehiculo.Rows[0];
                var placa = rowVehiculo["Placa"].ToString() ?? "";
                var idVehiculo = Convert.ToInt32(rowVehiculo["Id"]);
                var fechaEntrada = Convert.ToDateTime(rowVehiculo["FechaEntrada"]);
                var strRateKey = rowVehiculo["strRateKey"]?.ToString() ?? "A";
                var yaPago = rowVehiculo["bitPaid"] != DBNull.Value && Convert.ToInt32(rowVehiculo["bitPaid"]) == 1;
                var fechaPago = rowVehiculo["FechaPago"] != DBNull.Value
                    ? Convert.ToDateTime(rowVehiculo["FechaPago"])
                    : (DateTime?)null;

                // 2. Calcular el monto usando el SP existente
                var paramCalcular = new SqlParameter[]
                {
                    new SqlParameter("@Placa", placa)
                };

                var dtCalculo = await _db.ExecuteQueryAsync("EXEC IOT_sp_CalcularMonto @Placa", paramCalcular);

                if (dtCalculo.Rows.Count == 0)
                {
                    return new ConsultaPagoResponse
                    {
                        Exitoso = false,
                        Mensaje = "Error al calcular el monto"
                    };
                }

                var rowCalculo = dtCalculo.Rows[0];
                var tiempoTotalMinutos = Convert.ToInt32(rowCalculo["TiempoTotalMinutos"]);
                var tiempoCobrableMinutos = Convert.ToInt32(rowCalculo["TiempoCobrableMinutos"]);
                var montoCalculado = Convert.ToDecimal(rowCalculo["MontoCalculado"]);
                var precioPorHora = Convert.ToDecimal(rowCalculo["PrecioPorHora"]);
                var precioMinimo = Convert.ToDecimal(rowCalculo["PrecioMinimo"]);
                var estadoCobro = rowCalculo["EstadoCobro"]?.ToString() ?? "";

                // 3. Formatear tiempo para mostrar
                var horas = tiempoTotalMinutos / 60;
                var minutos = tiempoTotalMinutos % 60;
                var tiempoFormateado = horas > 0
                    ? $"{horas}h {minutos}min"
                    : $"{minutos} minutos";

                _logger.LogInformation(
                    "Consulta de pago - Placa: {Placa}, Tiempo: {Tiempo}, Monto: {Monto}, Estado: {Estado}",
                    placa, tiempoFormateado, montoCalculado, estadoCobro
                );

                return new ConsultaPagoResponse
                {
                    Exitoso = true,
                    Mensaje = estadoCobro == "GRACIA_ENTRADA" || estadoCobro == "GRACIA_SALIDA" || estadoCobro == "GRATIS"
                        ? "El vehículo no tiene monto a pagar"
                        : $"Monto a pagar: ${montoCalculado:F2}",

                    // Datos del vehículo
                    IdVehiculo = idVehiculo,
                    Placa = placa,
                    CodigoBarras = codigoBarras,
                    FechaEntrada = fechaEntrada,
                    StrRateKey = strRateKey,

                    // Datos del cálculo
                    TiempoTotalMinutos = tiempoTotalMinutos,
                    TiempoCobrableMinutos = tiempoCobrableMinutos,
                    MontoAPagar = montoCalculado,
                    PrecioPorHora = precioPorHora,
                    PrecioMinimo = precioMinimo,

                    // Estado
                    YaPago = yaPago,
                    EstadoCobro = estadoCobro,
                    FechaPago = fechaPago,

                    // Formateado
                    TiempoFormateado = tiempoFormateado
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al consultar pago por código de barras: {CodigoBarras}", codigoBarras);
                return new ConsultaPagoResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Registra un pago desde la PayStation
        /// </summary>
        public async Task<RegistrarPagoResponse> RegistrarPagoAsync(RegistrarPagoRequest request)
        {
            try
            {
                // Validaciones
                if (string.IsNullOrWhiteSpace(request.Placa))
                {
                    return new RegistrarPagoResponse
                    {
                        Exitoso = false,
                        Mensaje = "La placa es requerida"
                    };
                }

                if (request.Monto < 0)
                {
                    return new RegistrarPagoResponse
                    {
                        Exitoso = false,
                        Mensaje = "El monto no puede ser negativo"
                    };
                }

                // Ejecutar el SP existente
                var parameters = new SqlParameter[]
                {
                    new SqlParameter("@Placa", request.Placa),
                    new SqlParameter("@Monto", request.Monto),
                    new SqlParameter("@IdPayDevice", request.IdPayDevice),
                    new SqlParameter("@strRateKey", request.StrRateKey ?? "A"),
                    new SqlParameter("@OperationType", request.OperationType)
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_RegistrarPagoDesdeApp @Placa, @Monto, @IdPayDevice, @strRateKey, @OperationType",
                    parameters
                );

                if (dt.Rows.Count > 0)
                {
                    var row = dt.Rows[0];
                    var exitoso = Convert.ToBoolean(row["Exitoso"]);
                    var mensaje = row["Mensaje"]?.ToString() ?? "";

                    if (exitoso)
                    {
                        _logger.LogInformation(
                            "✓ Pago registrado - Placa: {Placa}, Monto: ${Monto}, Device: {Device}",
                            request.Placa, request.Monto, request.IdPayDevice
                        );

                        return new RegistrarPagoResponse
                        {
                            Exitoso = true,
                            Mensaje = mensaje,
                            IdVehiculo = row["IdVehiculo"] != DBNull.Value ? Convert.ToInt32(row["IdVehiculo"]) : null,
                            MontoRegistrado = row["MontoRegistrado"] != DBNull.Value ? Convert.ToDecimal(row["MontoRegistrado"]) : null,
                            FechaPago = row["FechaPago"] != DBNull.Value ? Convert.ToDateTime(row["FechaPago"]) : null,
                            IdPayDevice = row["IdPayDevice"] != DBNull.Value ? Convert.ToInt32(row["IdPayDevice"]) : null
                        };
                    }
                    else
                    {
                        _logger.LogWarning("Pago no registrado - Placa: {Placa}, Motivo: {Motivo}", request.Placa, mensaje);
                        return new RegistrarPagoResponse
                        {
                            Exitoso = false,
                            Mensaje = mensaje
                        };
                    }
                }

                return new RegistrarPagoResponse
                {
                    Exitoso = false,
                    Mensaje = "No se obtuvo respuesta del procedimiento"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar pago - Placa: {Placa}", request.Placa);
                return new RegistrarPagoResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Verifica el estado de pago de un vehículo por placa
        /// </summary>
        public async Task<VerificarPagoResponse> VerificarPagoPorPlacaAsync(string placa)
        {
            try
            {
                // 1. Buscar el vehículo por placa
                var sqlBuscar = @"
                    SELECT 
                        Id,
                        Placa,
                        CodigoBarras,
                        FechaEntrada,
                        Estado,
                        bitPaid,
                        Monto,
                        FechaPago
                    FROM IOT_Vehiculos
                    WHERE Placa = @Placa
                    AND Estado = 'DENTRO'";

                var paramBuscar = new SqlParameter[]
                {
                    new SqlParameter("@Placa", placa)
                };

                var dtVehiculo = await _db.ExecuteQueryAsync(sqlBuscar, paramBuscar);

                if (dtVehiculo.Rows.Count == 0)
                {
                    _logger.LogWarning("No se encontró vehículo con placa: {Placa}", placa);
                    return new VerificarPagoResponse
                    {
                        Exitoso = false,
                        Mensaje = "No se encontró el vehículo con esa placa o ya salió del parqueo",
                        Placa = placa
                    };
                }

                var rowVehiculo = dtVehiculo.Rows[0];
                var idVehiculo = Convert.ToInt32(rowVehiculo["Id"]);
                var codigoBarras = rowVehiculo["CodigoBarras"]?.ToString() ?? "";
                var estado = rowVehiculo["Estado"]?.ToString() ?? "";
                var yaPago = rowVehiculo["bitPaid"] != DBNull.Value && Convert.ToInt32(rowVehiculo["bitPaid"]) == 1;
                var montoPagado = rowVehiculo["Monto"] != DBNull.Value ? Convert.ToDecimal(rowVehiculo["Monto"]) : (decimal?)null;
                var fechaPago = rowVehiculo["FechaPago"] != DBNull.Value
                    ? Convert.ToDateTime(rowVehiculo["FechaPago"])
                    : (DateTime?)null;

                // 2. Calcular si está dentro del tiempo de gracia y si necesita pagar más
                bool dentroDeGracia = false;
                int? minutosRestantesGracia = null;
                bool necesitaPagarMas = false;
                decimal? montoAdicional = null;

                if (yaPago && fechaPago.HasValue)
                {
                    var minutosDesdePago = (int)(DateTime.Now - fechaPago.Value).TotalMinutes;
                    var tiempoGracia = 15; // 15 minutos de gracia después de pagar

                    if (minutosDesdePago <= tiempoGracia)
                    {
                        dentroDeGracia = true;
                        minutosRestantesGracia = tiempoGracia - minutosDesdePago;
                    }
                    else
                    {
                        // Excedió el tiempo de gracia, calcular monto adicional
                        var paramCalcular = new SqlParameter[]
                        {
                            new SqlParameter("@Placa", placa)
                        };

                        var dtCalculo = await _db.ExecuteQueryAsync("EXEC IOT_sp_CalcularMonto @Placa", paramCalcular);

                        if (dtCalculo.Rows.Count > 0)
                        {
                            var rowCalculo = dtCalculo.Rows[0];
                            var montoCalculado = Convert.ToDecimal(rowCalculo["MontoCalculado"]);

                            if (montoCalculado > 0)
                            {
                                necesitaPagarMas = true;
                                montoAdicional = montoCalculado;
                            }
                        }
                    }
                }
                else if (!yaPago)
                {
                    // No ha pagado, calcular monto
                    var paramCalcular = new SqlParameter[]
                    {
                        new SqlParameter("@Placa", placa)
                    };

                    var dtCalculo = await _db.ExecuteQueryAsync("EXEC IOT_sp_CalcularMonto @Placa", paramCalcular);

                    if (dtCalculo.Rows.Count > 0)
                    {
                        var rowCalculo = dtCalculo.Rows[0];
                        var montoCalculado = Convert.ToDecimal(rowCalculo["MontoCalculado"]);

                        if (montoCalculado > 0)
                        {
                            necesitaPagarMas = true;
                            montoAdicional = montoCalculado;
                        }
                    }
                }

                _logger.LogInformation(
                    "Verificación de pago - Placa: {Placa}, YaPagó: {YaPago}, DentroDeGracia: {Gracia}, NecesitaPagarMás: {NecesitaPagar}",
                    placa, yaPago, dentroDeGracia, necesitaPagarMas
                );

                return new VerificarPagoResponse
                {
                    Exitoso = true,
                    Mensaje = yaPago
                        ? (dentroDeGracia
                            ? $"Vehículo pagado. Tiene {minutosRestantesGracia} minutos para salir"
                            : "Vehículo pagado pero excedió tiempo de gracia")
                        : "Vehículo no ha pagado",

                    // Datos del vehículo
                    IdVehiculo = idVehiculo,
                    Placa = placa,
                    CodigoBarras = codigoBarras,
                    Estado = estado,

                    // Estado de pago
                    YaPago = yaPago,
                    MontoPagado = montoPagado,
                    FechaPago = fechaPago,

                    // Tiempo de gracia
                    DentroDeGracia = dentroDeGracia,
                    MinutosRestantesGracia = minutosRestantesGracia,

                    // Si necesita pagar más
                    NecesitaPagarMas = necesitaPagarMas,
                    MontoAdicional = montoAdicional
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al verificar pago por placa: {Placa}", placa);
                return new VerificarPagoResponse
                {
                    Exitoso = false,
                    Mensaje = $"Error: {ex.Message}",
                    Placa = placa
                };
            }
        }
    }
}