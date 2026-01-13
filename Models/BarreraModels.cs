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
}