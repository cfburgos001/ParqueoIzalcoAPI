namespace DataparkBarreraAPI.Models
{
    public class Tarifa
    {
        public int Id { get; set; }
        public string TipoTarifa { get; set; } = string.Empty;
        public string StrRateKey { get; set; } = string.Empty;
        public string? TipoVehiculo { get; set; }
        public string? Descripcion { get; set; }
        public decimal PrecioPorHora { get; set; }
        public decimal PrecioMinimo { get; set; }
        public decimal PrecioMax { get; set; }
        public decimal Precio1Hora { get; set; }
        public decimal Precio2Horas { get; set; }
        public decimal PrecioDiaCompleto { get; set; }
        public bool CobroIndefinido { get; set; }
        public bool Activa { get; set; }
    }

    public class ActualizarTarifaRequest
    {
        public int Id { get; set; }
        public decimal PrecioPorHora { get; set; }
        public decimal PrecioMinimo { get; set; }
        public decimal PrecioMax { get; set; }
        public decimal Precio1Hora { get; set; }
        public decimal Precio2Horas { get; set; }
        public decimal PrecioDiaCompleto { get; set; }
        public bool Activa { get; set; }
    }

    public class TarifaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }
}