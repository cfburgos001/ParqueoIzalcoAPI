using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface ICuentasService
    {
        Task<List<Cuenta>> ListarCuentasAsync();
        Task<Cuenta?> ObtenerCuentaAsync(int id);
        Task<CuentaResponse> CrearCuentaAsync(CrearCuentaRequest request);
        Task<CuentaResponse> ActualizarCuentaAsync(ActualizarCuentaRequest request);
        Task<CuentaResponse> ToggleCuentaAsync(int id);

        Task<List<Tarjeta>> ListarTarjetasDeCuentaAsync(int idCuenta);
        Task<TarjetaResponse> CrearTarjetaAsync(CrearTarjetaRequest request);
        Task<TarjetaResponse> ActualizarTarjetaAsync(ActualizarTarjetaRequest request);
        Task<TarjetaResponse> ToggleTarjetaAsync(int id);

        Task<List<CuentaDispositivo>> ListarDispositivosDeCuentaAsync(int idCuenta);
        Task<DispositivoResponse> AsignarDispositivoAsync(AsignarDispositivoRequest request);
        Task<DispositivoResponse> QuitarDispositivoAsync(int idCuenta, string idDispositivo);

        Task<List<CuentaHorario>> ListarHorariosDeCuentaAsync(int idCuenta);
        Task<HorarioResponse> GuardarHorarioAsync(GuardarHorarioRequest request);
        Task<HorarioResponse> EliminarHorarioAsync(int id);
    }

    public class CuentasService : ICuentasService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<CuentasService> _logger;

        private static readonly string[] _diasSemana =
            ["", "Domingo", "Lunes", "Martes", "Miércoles", "Jueves", "Viernes", "Sábado"];

        public CuentasService(IDatabaseService db, ILogger<CuentasService> logger)
        {
            _db = db;
            _logger = logger;
        }

        // ─────────────────────────────────────────────────────────────────
        // CUENTAS
        // ─────────────────────────────────────────────────────────────────

        public async Task<List<Cuenta>> ListarCuentasAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ListarCuentas");
                var list = new List<Cuenta>();
                foreach (DataRow row in dt.Rows)
                    list.Add(MapCuenta(row));
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar cuentas");
                return new List<Cuenta>();
            }
        }

        public async Task<Cuenta?> ObtenerCuentaAsync(int id)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@Id", id) };
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ObtenerCuenta @Id", parameters);
                return dt.Rows.Count > 0 ? MapCuenta(dt.Rows[0]) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener cuenta ID: {Id}", id);
                return null;
            }
        }

        public async Task<CuentaResponse> CrearCuentaAsync(CrearCuentaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@CodigoUnico", request.CodigoUnico),
                    new("@Nombre",      request.Nombre),
                    new("@Descripcion", (object?)request.Descripcion ?? DBNull.Value)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearCuenta @CodigoUnico, @Nombre, @Descripcion", parameters);
                return ParseCuentaResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear cuenta");
                return new CuentaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<CuentaResponse> ActualizarCuentaAsync(ActualizarCuentaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Id",          request.Id),
                    new("@Nombre",      request.Nombre),
                    new("@Descripcion", (object?)request.Descripcion ?? DBNull.Value)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarCuenta @Id, @Nombre, @Descripcion", parameters);
                return ParseCuentaResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar cuenta ID: {Id}", request.Id);
                return new CuentaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<CuentaResponse> ToggleCuentaAsync(int id)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@Id", id) };
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ToggleCuenta @Id", parameters);
                return ParseCuentaResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al toggle cuenta ID: {Id}", id);
                return new CuentaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // TARJETAS
        // ─────────────────────────────────────────────────────────────────

        public async Task<List<Tarjeta>> ListarTarjetasDeCuentaAsync(int idCuenta)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@IdCuenta", idCuenta) };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ListarTarjetasDeCuenta @IdCuenta", parameters);
                var list = new List<Tarjeta>();
                foreach (DataRow row in dt.Rows)
                    list.Add(MapTarjeta(row));
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar tarjetas de cuenta ID: {Id}", idCuenta);
                return new List<Tarjeta>();
            }
        }

        public async Task<TarjetaResponse> CrearTarjetaAsync(CrearTarjetaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@IdCuenta",      request.IdCuenta),
                    new("@NumeroTarjeta", request.NumeroTarjeta),
                    new("@NombreUsuario", request.NombreUsuario),
                    new("@PlacaVehiculo", (object?)request.PlacaVehiculo ?? DBNull.Value),
                    new("@Telefono",      (object?)request.Telefono      ?? DBNull.Value)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_CrearTarjeta @IdCuenta, @NumeroTarjeta, @NombreUsuario, @PlacaVehiculo, @Telefono",
                    parameters);
                return ParseTarjetaResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear tarjeta");
                return new TarjetaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<TarjetaResponse> ActualizarTarjetaAsync(ActualizarTarjetaRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@Id",            request.Id),
                    new("@NombreUsuario", request.NombreUsuario),
                    new("@PlacaVehiculo", (object?)request.PlacaVehiculo ?? DBNull.Value),
                    new("@Telefono",      (object?)request.Telefono      ?? DBNull.Value)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarTarjeta @Id, @NombreUsuario, @PlacaVehiculo, @Telefono",
                    parameters);
                return ParseTarjetaResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tarjeta ID: {Id}", request.Id);
                return new TarjetaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<TarjetaResponse> ToggleTarjetaAsync(int id)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@Id", id) };
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ToggleTarjeta @Id", parameters);
                return ParseTarjetaResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al toggle tarjeta ID: {Id}", id);
                return new TarjetaResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // DISPOSITIVOS
        // ─────────────────────────────────────────────────────────────────

        public async Task<List<CuentaDispositivo>> ListarDispositivosDeCuentaAsync(int idCuenta)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@IdCuenta", idCuenta) };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ListarDispositivosDeCuenta @IdCuenta", parameters);
                var list = new List<CuentaDispositivo>();
                foreach (DataRow row in dt.Rows)
                    list.Add(MapDispositivo(row));
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar dispositivos de cuenta ID: {Id}", idCuenta);
                return new List<CuentaDispositivo>();
            }
        }

        public async Task<DispositivoResponse> AsignarDispositivoAsync(AsignarDispositivoRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@IdCuenta",     request.IdCuenta),
                    new("@IdDispositivo", request.IdDispositivo)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_AsignarDispositivoCuenta @IdCuenta, @IdDispositivo", parameters);
                return ParseDispositivoResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar dispositivo");
                return new DispositivoResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<DispositivoResponse> QuitarDispositivoAsync(int idCuenta, string idDispositivo)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@IdCuenta",     idCuenta),
                    new("@IdDispositivo", idDispositivo)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_QuitarDispositivoCuenta @IdCuenta, @IdDispositivo", parameters);
                return ParseDispositivoResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al quitar dispositivo");
                return new DispositivoResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // HORARIOS
        // ─────────────────────────────────────────────────────────────────

        public async Task<List<CuentaHorario>> ListarHorariosDeCuentaAsync(int idCuenta)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@IdCuenta", idCuenta) };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ListarHorariosDeCuenta @IdCuenta", parameters);
                var list = new List<CuentaHorario>();
                foreach (DataRow row in dt.Rows)
                    list.Add(MapHorario(row));
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al listar horarios de cuenta ID: {Id}", idCuenta);
                return new List<CuentaHorario>();
            }
        }

        public async Task<HorarioResponse> GuardarHorarioAsync(GuardarHorarioRequest request)
        {
            try
            {
                var parameters = new SqlParameter[]
                {
                    new("@IdCuenta",   request.IdCuenta),
                    new("@DiaSemana",  request.DiaSemana),
                    new("@HoraInicio", request.HoraInicio),
                    new("@HoraFin",    request.HoraFin)
                };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_GuardarHorarioCuenta @IdCuenta, @DiaSemana, @HoraInicio, @HoraFin",
                    parameters);
                return ParseHorarioResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar horario");
                return new HorarioResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        public async Task<HorarioResponse> EliminarHorarioAsync(int id)
        {
            try
            {
                var parameters = new[] { new SqlParameter("@Id", id) };
                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_EliminarHorarioCuenta @Id", parameters);
                return ParseHorarioResponse(dt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar horario ID: {Id}", id);
                return new HorarioResponse { Exitoso = false, Mensaje = $"Error: {ex.Message}" };
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // MAPPERS
        // ─────────────────────────────────────────────────────────────────

        private static Cuenta MapCuenta(DataRow row) => new()
        {
            Id               = Convert.ToInt32(row["Id"]),
            CodigoUnico      = row["CodigoUnico"]?.ToString() ?? "",
            Nombre           = row["Nombre"]?.ToString() ?? "",
            Descripcion      = row["Descripcion"] == DBNull.Value ? null : row["Descripcion"].ToString(),
            Activo           = Convert.ToBoolean(row["Activo"]),
            FechaCreacion    = Convert.ToDateTime(row["FechaCreacion"]),
            FechaModificacion= row.Table.Columns.Contains("FechaModificacion") && row["FechaModificacion"] != DBNull.Value
                                   ? Convert.ToDateTime(row["FechaModificacion"]) : DateTime.MinValue,
            TotalTarjetas    = row.Table.Columns.Contains("TotalTarjetas") && row["TotalTarjetas"] != DBNull.Value
                                   ? Convert.ToInt32(row["TotalTarjetas"]) : 0
        };

        private static Tarjeta MapTarjeta(DataRow row) => new()
        {
            Id             = Convert.ToInt32(row["Id"]),
            IdCuenta       = Convert.ToInt32(row["IdCuenta"]),
            NombreCuenta   = row.Table.Columns.Contains("NombreCuenta") ? row["NombreCuenta"]?.ToString() ?? "" : "",
            NumeroTarjeta  = row["NumeroTarjeta"]?.ToString() ?? "",
            NombreUsuario  = row["NombreUsuario"]?.ToString() ?? "",
            PlacaVehiculo  = row["PlacaVehiculo"] == DBNull.Value ? null : row["PlacaVehiculo"].ToString(),
            Telefono       = row["Telefono"] == DBNull.Value ? null : row["Telefono"].ToString(),
            Activo         = Convert.ToBoolean(row["Activo"]),
            FechaCreacion  = Convert.ToDateTime(row["FechaCreacion"]),
            FechaUltimoUso = row["FechaUltimoUso"] == DBNull.Value ? null : Convert.ToDateTime(row["FechaUltimoUso"])
        };

        private static CuentaDispositivo MapDispositivo(DataRow row) => new()
        {
            IdCuenta         = Convert.ToInt32(row["IdCuenta"]),
            NombreCuenta     = row.Table.Columns.Contains("NombreCuenta") ? row["NombreCuenta"]?.ToString() ?? "" : "",
            IdDispositivo    = row["IdDispositivo"]?.ToString() ?? "",
            NombreDispositivo= row.Table.Columns.Contains("NombreDispositivo") ? row["NombreDispositivo"]?.ToString() ?? "" : "",
            TipoDispositivo  = row.Table.Columns.Contains("TipoDispositivo") && row["TipoDispositivo"] != DBNull.Value
                                   ? row["TipoDispositivo"].ToString() : null
        };

        private CuentaHorario MapHorario(DataRow row)
        {
            var dia = Convert.ToInt32(row["DiaSemana"]);
            return new CuentaHorario
            {
                Id           = Convert.ToInt32(row["Id"]),
                IdCuenta     = Convert.ToInt32(row["IdCuenta"]),
                NombreCuenta = row.Table.Columns.Contains("NombreCuenta") ? row["NombreCuenta"]?.ToString() ?? "" : "",
                DiaSemana    = dia,
                NombreDia    = dia >= 1 && dia <= 7 ? _diasSemana[dia] : dia.ToString(),
                HoraInicio   = row["HoraInicio"] == DBNull.Value ? TimeSpan.Zero : (TimeSpan)row["HoraInicio"],
                HoraFin      = row["HoraFin"]    == DBNull.Value ? TimeSpan.Zero : (TimeSpan)row["HoraFin"]
            };
        }

        private static CuentaResponse ParseCuentaResponse(DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return new CuentaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            var row = dt.Rows[0];
            return new CuentaResponse
            {
                Exitoso = dt.Columns.Contains("Exitoso") ? Convert.ToBoolean(row["Exitoso"]) : true,
                Mensaje = dt.Columns.Contains("Mensaje") ? row["Mensaje"]?.ToString() ?? "" : "Operación completada",
                Id      = dt.Columns.Contains("Id") && row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null
            };
        }

        private static TarjetaResponse ParseTarjetaResponse(DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return new TarjetaResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            var row = dt.Rows[0];
            return new TarjetaResponse
            {
                Exitoso = dt.Columns.Contains("Exitoso") ? Convert.ToBoolean(row["Exitoso"]) : true,
                Mensaje = dt.Columns.Contains("Mensaje") ? row["Mensaje"]?.ToString() ?? "" : "Operación completada",
                Id      = dt.Columns.Contains("Id") && row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null
            };
        }

        private static DispositivoResponse ParseDispositivoResponse(DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return new DispositivoResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            var row = dt.Rows[0];
            return new DispositivoResponse
            {
                Exitoso = dt.Columns.Contains("Exitoso") ? Convert.ToBoolean(row["Exitoso"]) : true,
                Mensaje = dt.Columns.Contains("Mensaje") ? row["Mensaje"]?.ToString() ?? "" : "Operación completada"
            };
        }

        private static HorarioResponse ParseHorarioResponse(DataTable dt)
        {
            if (dt.Rows.Count == 0)
                return new HorarioResponse { Exitoso = false, Mensaje = "Sin respuesta del servidor" };
            var row = dt.Rows[0];
            return new HorarioResponse
            {
                Exitoso = dt.Columns.Contains("Exitoso") ? Convert.ToBoolean(row["Exitoso"]) : true,
                Mensaje = dt.Columns.Contains("Mensaje") ? row["Mensaje"]?.ToString() ?? "" : "Operación completada",
                Id      = dt.Columns.Contains("Id") && row["Id"] != DBNull.Value ? Convert.ToInt32(row["Id"]) : null
            };
        }
    }
}
