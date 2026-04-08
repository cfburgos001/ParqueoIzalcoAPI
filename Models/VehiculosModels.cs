namespace ParqueoIzalcoAPI.Models
{
    public class EntradaAutomaticaRequest
    {
        public string IdDispositivo { get; set; } = string.Empty;
        public string StrRateKey { get; set; } = "A";
        public int IdOperador { get; set; }
    }

    public class EntradaAutomaticaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? CodigoBarras { get; set; }
    }

    // Lo que enviará la máquina Exit
    public class SalidaAutomaticaRequest
    {
        public string Placa { get; set; } = string.Empty; // El código de barras escaneado
        public string IdDispositivo { get; set; } = string.Empty;
    }

    // Lo que le respondemos a la máquina Exit
    public class SalidaAutomaticaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string EstadoCobro { get; set; } = string.Empty; // Para que la máquina sepa si fue gratis, reingreso, o falta pagar
    }
}