namespace DataparkBarreraAPI.Models
{
    // =============================================
    // MODELOS PARA CONTROL DE VISITANTES
    // =============================================

    /// <summary>
    /// Tipo de visitante (Médico, Proveedor, Visitante)
    /// </summary>
    public class TipoVisitante
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>
    /// Área destino (Sala de Operaciones, Diagnóstico, etc.)
    /// </summary>
    public class AreaDestino
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    /// <summary>
    /// Visitante registrado en el catálogo (persona)
    /// </summary>
    public class Visitante
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? Especialidad { get; set; }
        public int IdTipoVisitante { get; set; }
        public string? TipoVisitante { get; set; }
        public string? PlacaFrecuente { get; set; }
        public string? Empresa { get; set; }
    }

    /// <summary>
    /// Registro de una visita (una fila del historial)
    /// </summary>
    public class RegistroVisita
    {
        public int Id { get; set; }
        public int? IdVisitante { get; set; }
        public string NombreVisitante { get; set; } = string.Empty;
        public int IdTipoVisitante { get; set; }
        public string? TipoVisitante { get; set; }
        public string? Placa { get; set; }
        public int? IdAreaDestino { get; set; }
        public string? AreaDestino { get; set; }
        public DateTime FechaVisita { get; set; }
        public DateTime HoraEntrada { get; set; }
        public DateTime? HoraSalida { get; set; }
        public string? Observacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? NombreOperador { get; set; }
        public int MinutosEstancia { get; set; }
    }

    /// <summary>
    /// Estadísticas de visitas del día
    /// </summary>
    public class EstadisticasVisitas
    {
        public int TotalVisitas { get; set; }
        public int VisitantesDentro { get; set; }
        public int VisitantesSalieron { get; set; }
        public int TotalMedicos { get; set; }
        public int TotalProveedores { get; set; }
        public int TotalVisitantesGenerales { get; set; }
    }

    // =============================================
    // REQUESTS
    // =============================================

    /// <summary>
    /// Request para registrar entrada de un visitante
    /// </summary>
    public class RegistrarEntradaVisitanteRequest
    {
        public int? IdVisitante { get; set; }
        public string NombreVisitante { get; set; } = string.Empty;
        public int IdTipoVisitante { get; set; }
        public string? Placa { get; set; }
        public int? IdAreaDestino { get; set; }
        public string? Observacion { get; set; }
        public int? IdOperador { get; set; }
        public string? NombreOperador { get; set; }
        public string? IdDispositivo { get; set; }
    }

    /// <summary>
    /// Request para crear un visitante nuevo en el catálogo
    /// </summary>
    public class CrearVisitanteRequest
    {
        public string NombreCompleto { get; set; } = string.Empty;
        public int IdTipoVisitante { get; set; }
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? Especialidad { get; set; }
        public string? PlacaFrecuente { get; set; }
        public string? Empresa { get; set; }
    }

    /// <summary>
    /// Request para actualizar un visitante del catálogo
    /// </summary>
    public class ActualizarVisitanteRequest
    {
        public int Id { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public int IdTipoVisitante { get; set; }
        public string? Identificacion { get; set; }
        public string? Telefono { get; set; }
        public string? Especialidad { get; set; }
        public string? PlacaFrecuente { get; set; }
        public string? Empresa { get; set; }
    }

    /// <summary>
    /// Request para crear tipo de visitante o área destino
    /// </summary>
    public class CrearCatalogoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    /// <summary>
    /// Request para actualizar observación de una visita
    /// </summary>
    public class ActualizarObservacionRequest
    {
        public string Observacion { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request para buscar visitas con filtros
    /// </summary>
    public class BuscarVisitasRequest
    {
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
        public string? NombreVisitante { get; set; }
        public string? Placa { get; set; }
        public int? IdTipoVisitante { get; set; }
        public string? Estado { get; set; }
        public int Top { get; set; } = 100;
    }

    // =============================================
    // RESPONSES
    // =============================================

    /// <summary>
    /// Response genérico para operaciones de visitantes
    /// </summary>
    public class VisitanteResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }

    /// <summary>
    /// Response para registro de entrada
    /// </summary>
    public class RegistrarEntradaVisitanteResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
        public DateTime? HoraEntrada { get; set; }
    }

    /// <summary>
    /// Response para crear visitante en catálogo
    /// </summary>
    public class CrearVisitanteResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? NombreCompleto { get; set; }
        public int? IdTipoVisitante { get; set; }
        public string? PlacaFrecuente { get; set; }
    }

    // =============================================
    // MODELOS DE LOGIN
    // =============================================

    /// <summary>
    /// Request para login en el portal de visitas
    /// </summary>
    public class LoginVisitasRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Response del login
    /// </summary>
    public class LoginVisitasResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int IdOperador { get; set; }
        public string NombreCompleto { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string TipoUsuario { get; set; } = string.Empty;
    }

    // =============================================
    // REPORTE DE VEHÍCULOS
    // =============================================

    public class ReporteVehiculosRequest
    {
        /// <summary>
        /// Lista de columnas a incluir. Si está vacío, devuelve todas.
        /// Columnas válidas: Id, Placa, CodigoBarras, FechaEntrada, FechaSalida, Estado,
        /// bitPaid, Monto, FechaPago, strRateKey, TiempoEstancia, IdDispositivoEntrada,
        /// IdDispositivoSalida, NombreOperador, UsuarioRegistro
        /// </summary>
        public List<string>? Columnas { get; set; }

        public string? FechaInicio { get; set; }
        public string? FechaFin { get; set; }
        public string? Estado { get; set; }
        public string? Placa { get; set; }
        public bool? SoloPagados { get; set; }
        public string? StrRateKey { get; set; }
        public int Top { get; set; } = 500;
    }
}