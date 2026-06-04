namespace ParqueoIzalcoAPI.Models
{
    /// <summary>
    /// Fila de IOT_Logs devuelta por IOT_sp_MonitoreoLogs
    /// Campos: Id, IdTipoLog, TipoLog (join), Placa, Datos,
    ///         IdDispositivo, NombreDispositivo (join), FechaEvento, Prioridad
    /// </summary>
    public class MonitoreoLogItem
    {
        public int Id { get; set; }
        public int IdTipoLog { get; set; }
        public string TipoLog { get; set; } = string.Empty;
        public string? Placa { get; set; }
        public string? Datos { get; set; }
        public string? IdDispositivo { get; set; }
        public string? NombreDispositivo { get; set; }
        public DateTime FechaEvento { get; set; }
        /// <summary>BAJA | MEDIA | ALTA</summary>
        public string Prioridad { get; set; } = "BAJA";
    }
}