namespace DataparkBarreraAPI.Models
{
    /// <summary>
    /// Modelo de la barrera
    /// </summary>
    public class Barrera
    {
        public int ID { get; set; }
        public string BarreraSeteo { get; set; } = string.Empty;
        public bool EstadoBarrera { get; set; }
        public bool ComandoBarrera { get; set; }
        public DateTime FechaUltimaActualizacion { get; set; }

        public string EstadoTexto => EstadoBarrera ? "🚧 ABIERTA" : "🔒 CERRADA";
        public int SegundosDesdeActualizacion =>
            (int)(DateTime.Now - FechaUltimaActualizacion).TotalSeconds;
    }

    /// <summary>
    /// Respuesta estándar de la API
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// Request para cerrar la barrera
    /// </summary>
    public class CerrarBarreraRequest
    {
        public string Motivo { get; set; } = "Vehículo pasó";
    }

    /// <summary>
    /// Estadísticas de la barrera
    /// </summary>
    public class BarreraEstadisticas
    {
        public int TotalAperturas { get; set; }
        public int TotalCierres { get; set; }
        public double TiempoPromedioAbierta { get; set; }
        public DateTime? UltimaApertura { get; set; }
        public DateTime? UltimoCierre { get; set; }
    }

    /// <summary>
    /// Log de actividad de la barrera
    /// </summary>
    public class BarreraLog
    {
        public DateTime Fecha { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? PlacaVehiculo { get; set; }
        public string? TipoMovimiento { get; set; }
    }

    /// <summary>
    /// Request para registrar un log desde el Controllino
    /// </summary>
    public class RegistroLogRequest
    {
        public int IdTipoLog { get; set; }
        public string? Placa { get; set; }
        public string IdDispositivo { get; set; } = "CONTROLLINO";
        public string? DatosAdicionales { get; set; }
    }

    /// <summary>
    /// Response del registro de log
    /// </summary>
    public class RegistroLogResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? IdLog { get; set; }
        public DateTime? FechaRegistro { get; set; }
    }

    // =============================================
    // MODELOS PARA PAGO - PAYSTATION
    // =============================================

    /// <summary>
    /// Response de consulta de vehículo para pago (cuando se escanea el ticket)
    /// </summary>
    public class ConsultaPagoResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Datos del vehículo
        public int IdVehiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public DateTime FechaEntrada { get; set; }
        public string? StrRateKey { get; set; }

        // Datos del cálculo
        public int TiempoTotalMinutos { get; set; }
        public int TiempoCobrableMinutos { get; set; }
        public decimal MontoAPagar { get; set; }
        public decimal PrecioPorHora { get; set; }
        public decimal PrecioMinimo { get; set; }

        // Estado
        public bool YaPago { get; set; }
        public string EstadoCobro { get; set; } = string.Empty;
        public DateTime? FechaPago { get; set; }

        // Información formateada para mostrar
        public string TiempoFormateado { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para registrar un pago desde la PayStation
    /// </summary>
    public class RegistrarPagoRequest
    {
        /// <summary>
        /// Placa del vehículo
        /// </summary>
        public string Placa { get; set; } = string.Empty;

        /// <summary>
        /// Monto cobrado
        /// </summary>
        public decimal Monto { get; set; }

        /// <summary>
        /// ID del dispositivo de pago (PayStation)
        /// </summary>
        public int IdPayDevice { get; set; }

        /// <summary>
        /// Tipo de tarifa (A=Auto, M=Moto, C=Carga) - Opcional, mantiene el original
        /// </summary>
        public string? StrRateKey { get; set; } = "A";

        /// <summary>
        /// Tipo de operación (1=Normal, 2=Cortesía, etc.)
        /// </summary>
        public int OperationType { get; set; } = 1;
    }

    /// <summary>
    /// Response del registro de pago
    /// </summary>
    public class RegistrarPagoResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? IdVehiculo { get; set; }
        public decimal? MontoRegistrado { get; set; }
        public DateTime? FechaPago { get; set; }
        public int? IdPayDevice { get; set; }
    }

    /// <summary>
    /// Response de verificación de pago por placa
    /// </summary>
    public class VerificarPagoResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;

        // Datos del vehículo
        public int? IdVehiculo { get; set; }
        public string Placa { get; set; } = string.Empty;
        public string CodigoBarras { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;

        // Estado de pago
        public bool YaPago { get; set; }
        public decimal? MontoPagado { get; set; }
        public DateTime? FechaPago { get; set; }

        // Tiempo de gracia
        public bool DentroDeGracia { get; set; }
        public int? MinutosRestantesGracia { get; set; }

        // Si necesita pagar más
        public bool NecesitaPagarMas { get; set; }
        public decimal? MontoAdicional { get; set; }
    }

    // =============================================
    // MODELO PARA REINGRESO POR GRACIA
    // =============================================

    /// <summary>
    /// Response del proceso de reingreso automático cuando se excede la gracia
    /// </summary>
    public class ReingresoGraciaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? IdRegistroAnterior { get; set; }
        public int? IdNuevoRegistro { get; set; }
        public string? CodigoBarrasAnterior { get; set; }
        public string? NuevoCodigoBarras { get; set; }
        public DateTime? FechaSalida { get; set; }
        public int? TiempoEstancia { get; set; }
    }


    // =============================================
    // MODELOS PARA APERTURA/CIERRE
    // =============================================

    /// <summary>
    /// Request para escribir en IOT_AperturaCierre
    /// </summary>
    public class AperturaCierreRequest
    {
        public string TipoOperacion { get; set; } = string.Empty;
        public int IdOperador { get; set; }
        public string NombreOperador { get; set; } = string.Empty;
        public string IdDispositivo { get; set; } = string.Empty;
        public decimal? MontoTotalCobrado { get; set; }
        public int? CantidadVehiculos { get; set; }
        public int? VehiculosDentro { get; set; }
        public string? VehiculosDetalle { get; set; }
    }

    /// <summary>
    /// Response del registro
    /// </summary>
    public class AperturaCierreResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? IdRegistro { get; set; }
        public DateTime? FechaOperacion { get; set; }
    }
}