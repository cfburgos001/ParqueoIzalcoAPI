namespace ParqueoIzalcoAPI.Models
{
    // =============================================
    // MODELOS PARA CUENTAS CORPORATIVAS
    // =============================================

    /// <summary>
    /// Representa una cuenta corporativa (empresa, familia o departamento)
    /// </summary>
    public class Cuenta
    {
        public int Id { get; set; }
        public string CodigoUnico { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime FechaModificacion { get; set; }
        public int TotalTarjetas { get; set; }
    }

    public class CrearCuentaRequest
    {
        public string CodigoUnico { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class ActualizarCuentaRequest
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
    }

    public class CuentaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }

    // =============================================
    // MODELOS PARA TARJETAS (USUARIOS FINALES)
    // =============================================

    /// <summary>
    /// Representa a una persona física y su tarjeta RFID/proximidad
    /// </summary>
    public class Tarjeta
    {
        public int Id { get; set; }
        public int IdCuenta { get; set; }
        public string NombreCuenta { get; set; } = string.Empty;
        public string NumeroTarjeta { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? PlacaVehiculo { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaUltimoUso { get; set; }
    }

    public class CrearTarjetaRequest
    {
        public int IdCuenta { get; set; }
        public string NumeroTarjeta { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string? PlacaVehiculo { get; set; }
        public string? Telefono { get; set; }
    }

    public class ActualizarTarjetaRequest
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string? PlacaVehiculo { get; set; }
        public string? Telefono { get; set; }
    }

    public class TarjetaResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }

    // =============================================
    // MODELOS PARA DISPOSITIVOS DE CUENTA
    // =============================================

    /// <summary>
    /// Dispositivo (pluma/barrera) asignado a una cuenta
    /// </summary>
    public class CuentaDispositivo
    {
        public int IdCuenta { get; set; }
        public string NombreCuenta { get; set; } = string.Empty;
        public string IdDispositivo { get; set; } = string.Empty;
        public string NombreDispositivo { get; set; } = string.Empty;
        public string? TipoDispositivo { get; set; }
    }

    public class AsignarDispositivoRequest
    {
        public int IdCuenta { get; set; }
        public string IdDispositivo { get; set; } = string.Empty;
    }

    public class DispositivoResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
    }

    // =============================================
    // MODELOS PARA HORARIOS DE CUENTA
    // =============================================

    /// <summary>
    /// Horario de acceso permitido por cuenta y día de la semana
    /// </summary>
    public class CuentaHorario
    {
        public int Id { get; set; }
        public int IdCuenta { get; set; }
        public string NombreCuenta { get; set; } = string.Empty;
        /// <summary>1=Domingo, 2=Lunes, 3=Martes, 4=Miércoles, 5=Jueves, 6=Viernes, 7=Sábado</summary>
        public int DiaSemana { get; set; }
        public string NombreDia { get; set; } = string.Empty;
        public TimeSpan HoraInicio { get; set; }
        public TimeSpan HoraFin { get; set; }
    }

    public class GuardarHorarioRequest
    {
        public int IdCuenta { get; set; }
        /// <summary>1=Domingo, 2=Lunes … 7=Sábado</summary>
        public int DiaSemana { get; set; }
        public string HoraInicio { get; set; } = string.Empty;
        public string HoraFin { get; set; } = string.Empty;
    }

    public class HorarioResponse
    {
        public bool Exitoso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int? Id { get; set; }
    }

    // =============================================
    // MODELOS PARA VALIDACIÓN DE ACCESO
    // =============================================

    /// <summary>
    /// Request del hardware (Arduino) al validar una tarjeta
    /// </summary>
    public class ValidarAccesoRequest
    {
        public string NumeroTarjeta { get; set; } = string.Empty;
        public string IdDispositivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// Respuesta completa de la validación de acceso
    /// </summary>
    public class ValidacionAccesoResponse
    {
        public bool Acceso { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public string? NombreUsuario { get; set; }
        public string? NombreCuenta { get; set; }
        public string? PlacaVehiculo { get; set; }
        public string? CodigoBarras { get; set; }
        public string? MotivoRechazo { get; set; }
    }

    /// <summary>
    /// Request para registrar entrada o salida de tarjeta
    /// </summary>
    public class RegistrarMovimientoTarjetaRequest
    {
        public string NumeroTarjeta { get; set; } = string.Empty;
        public string IdDispositivo { get; set; } = string.Empty;
    }
}
