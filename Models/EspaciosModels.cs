namespace ParqueoIzalcoAPI.Models
{
    public class EspaciosDisponiblesResponse
    {
        public int TotalEspacios { get; set; }
        public int VehiculosDentro { get; set; }
        public int EspaciosDisponibles { get; set; }
        /// <summary>DISPONIBLE | CASI_LLENO | LLENO</summary>
        public string EstadoCapacidad { get; set; } = string.Empty;
    }
}