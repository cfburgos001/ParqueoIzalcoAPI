using ParqueoIzalcoAPI.Models;

namespace ParqueoIzalcoAPI.Services
{
    public interface IVehiculosService
    {
        Task<EntradaAutomaticaResponse> RegistrarEntradaAutomaticaAsync(EntradaAutomaticaRequest request);


        Task<SalidaAutomaticaResponse> ProcesarSalidaAutomaticaAsync(SalidaAutomaticaRequest request);
    }
}