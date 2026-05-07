namespace ParqueoIzalcoAPI.Models
{
    // =============================================
    // NIVELES DE ACCESO
    // =============================================

    public class NivelAcceso
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public string? HorariosResumen { get; set; }
    }

    // =============================================
    // CUENTAS
    // =============================================

    public class Cuenta
    {
        public int Id { get; set; }
        public string CodigoUnico { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public bool Activa { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public int TotalTarjetas { get; set; }
        public int TarjetasVigentes { get; set; }
    }

    public class CrearCuentaRequest
    {
        public string CodigoUnico { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public int? IdOperadorCreo { get; set; }
    }

    public class ActualizarCuentaRequest
    {
        public int Id { get; set; }
        public string? Nombre { get; set; }
        public string? Descripcion { get; set; }
        public string? Email { get; set; }
        public string? Telefono { get; set; }
        public bool? Activa { get; set; }
    }

    public class CuentaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }

    // =============================================
    // TARJETAS
    // =============================================

    public class Tarjeta
    {
        public int Id { get; set; }
        public int IdCuenta { get; set; }
        public string CodigoCuenta { get; set; } = string.Empty;
        public string NombreCuenta { get; set; } = string.Empty;
        public bool CuentaActiva { get; set; }
        public int IdNivelAcceso { get; set; }
        public string NivelAcceso { get; set; } = string.Empty;
        public string CodigoTarjeta { get; set; } = string.Empty;
        public string? NombreTitular { get; set; }
        public string? IdentificacionTitular { get; set; }
        public DateTime? FechaVigenciaInicio { get; set; }
        public DateTime? FechaVigenciaFin { get; set; }
        public bool Activa { get; set; }
        public string EstadoVigencia { get; set; } = string.Empty;
        public bool EstaActualmenteDentro { get; set; }
        public string? Observacion { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaModificacion { get; set; }
    }

    public class CrearTarjetaRequest
    {
        public int IdCuenta { get; set; }
        public int IdNivelAcceso { get; set; }
        public string CodigoTarjeta { get; set; } = string.Empty;
        public string? NombreTitular { get; set; }
        public string? IdentificacionTitular { get; set; }
        public DateTime? FechaVigenciaInicio { get; set; }
        public DateTime? FechaVigenciaFin { get; set; }
        public string? Observacion { get; set; }
        public int? IdOperadorCreo { get; set; }
    }

    public class ActualizarTarjetaRequest
    {
        public int Id { get; set; }
        public int? IdNivelAcceso { get; set; }
        public string? NombreTitular { get; set; }
        public string? IdentificacionTitular { get; set; }
        public DateTime? FechaVigenciaInicio { get; set; }
        public DateTime? FechaVigenciaFin { get; set; }
        public bool? Activa { get; set; }
        public string? Observacion { get; set; }
    }

    public class TarjetaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }

    // =============================================
    // VALIDACIÓN Y ENTRADA POR TARJETA
    // =============================================

    public class ValidarTarjetaRequest
    {
        /// <summary>Código leído por el lector físico (RFID, QR, etc.)</summary>
        public string CodigoTarjeta { get; set; } = string.Empty;
    }

    public class ValidarTarjetaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        /// <summary>
        /// TARJETA_NO_EXISTE | TARJETA_INACTIVA | CUENTA_INACTIVA |
        /// VIGENCIA_EXPIRADA | YA_DENTRO | FUERA_DE_HORARIO
        /// </summary>
        public string? MotivoRechazo { get; set; }
        public string? NombreTitular { get; set; }
        public string? NombreCuenta { get; set; }
        public string? NivelAcceso { get; set; }
    }

    public class RegistrarEntradaTarjetaRequest
    {
        /// <summary>Código de la tarjeta — se usará como Placa en IOT_Vehiculos</summary>
        public string CodigoTarjeta { get; set; } = string.Empty;
        public string IdDispositivo { get; set; } = string.Empty;
        public int IdOperador { get; set; }
    }

    public class RegistrarEntradaTarjetaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
        public string? CodigoBarras { get; set; }
    }
}