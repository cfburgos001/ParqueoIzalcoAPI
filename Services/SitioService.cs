using ParqueoIzalcoAPI.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ParqueoIzalcoAPI.Services
{
    public interface ISitioService
    {
        ConfigSitio ObtenerConfig();            // desde memoria
        Task<ConfigSitio?> CargarDesdeDBAsync(); // carga / recarga desde BD
        Task<bool> ActualizarAsync(ActualizarSitioRequest request);
    }

    public class SitioService : ISitioService
    {
        private readonly IDatabaseService _db;
        private readonly ILogger<SitioService> _logger;

        // Cache en memoria — se llena al iniciar la app y al actualizar
        private ConfigSitio _cache = new ConfigSitio
        {
            NombreComercial = "Parqueo IOT",
            RazonSocial = "Parqueo IOT, S.A. DE C.V."
        };

        public SitioService(IDatabaseService db, ILogger<SitioService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public ConfigSitio ObtenerConfig() => _cache;

        public async Task<ConfigSitio?> CargarDesdeDBAsync()
        {
            try
            {
                var dt = await _db.ExecuteQueryAsync("EXEC IOT_sp_ObtenerConfigSitio");

                if (dt.Rows.Count == 0)
                {
                    _logger.LogWarning("IOT_ControlDeSitio no tiene registros activos.");
                    return _cache; // devuelve defaults
                }

                var r = dt.Rows[0];

                _cache = new ConfigSitio
                {
                    Id = Convert.ToInt32(r["Id"]),
                    NombreComercial = r["NombreComercial"]?.ToString() ?? "Parqueo IOT",
                    RazonSocial = r["RazonSocial"]?.ToString() ?? "Parqueo IOT",
                    NIT = r["NIT"] == DBNull.Value ? null : r["NIT"].ToString(),
                    NRC = r["NRC"] == DBNull.Value ? null : r["NRC"].ToString(),
                    Direccion = r["Direccion"] == DBNull.Value ? null : r["Direccion"].ToString(),
                    Municipio = r["Municipio"] == DBNull.Value ? null : r["Municipio"].ToString(),
                    Departamento = r["Departamento"] == DBNull.Value ? null : r["Departamento"].ToString(),
                    Pais = r["Pais"]?.ToString() ?? "El Salvador",
                    Telefono = r["Telefono"] == DBNull.Value ? null : r["Telefono"].ToString(),
                    Telefono2 = r["Telefono2"] == DBNull.Value ? null : r["Telefono2"].ToString(),
                    Email = r["Email"] == DBNull.Value ? null : r["Email"].ToString(),
                    GiroActividad = r["GiroActividad"] == DBNull.Value ? null : r["GiroActividad"].ToString(),
                    CodigoActividad = r["CodigoActividad"] == DBNull.Value ? null : r["CodigoActividad"].ToString(),
                    RepresentanteLegal = r["RepresentanteLegal"] == DBNull.Value ? null : r["RepresentanteLegal"].ToString(),
                    LogoUrl = r["LogoUrl"] == DBNull.Value ? null : r["LogoUrl"].ToString(),
                    ColorPrimario = r["ColorPrimario"]?.ToString() ?? "#3b82f6",
                    Slogan = r["Slogan"] == DBNull.Value ? null : r["Slogan"].ToString(),
                    SitioWeb = r["SitioWeb"] == DBNull.Value ? null : r["SitioWeb"].ToString(),
                };

                _logger.LogInformation("✓ Config sitio cargada: {Nombre}", _cache.NombreComercial);
                return _cache;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cargar configuración del sitio");
                return _cache;
            }
        }

        public async Task<bool> ActualizarAsync(ActualizarSitioRequest req)
        {
            try
            {
                var p = new SqlParameter[]
                {
                    new("@Id",                req.Id),
                    new("@NombreComercial",   req.NombreComercial),
                    new("@RazonSocial",       req.RazonSocial),
                    new("@NIT",               (object?)req.NIT               ?? DBNull.Value),
                    new("@NRC",               (object?)req.NRC               ?? DBNull.Value),
                    new("@Direccion",         (object?)req.Direccion         ?? DBNull.Value),
                    new("@Municipio",         (object?)req.Municipio         ?? DBNull.Value),
                    new("@Departamento",      (object?)req.Departamento      ?? DBNull.Value),
                    new("@Pais",              req.Pais),
                    new("@Telefono",          (object?)req.Telefono          ?? DBNull.Value),
                    new("@Telefono2",         (object?)req.Telefono2         ?? DBNull.Value),
                    new("@Email",             (object?)req.Email             ?? DBNull.Value),
                    new("@GiroActividad",     (object?)req.GiroActividad     ?? DBNull.Value),
                    new("@CodigoActividad",   (object?)req.CodigoActividad   ?? DBNull.Value),
                    new("@RepresentanteLegal",(object?)req.RepresentanteLegal?? DBNull.Value),
                    new("@LogoUrl",           (object?)req.LogoUrl           ?? DBNull.Value),
                    new("@ColorPrimario",     req.ColorPrimario),
                    new("@Slogan",            (object?)req.Slogan            ?? DBNull.Value),
                    new("@SitioWeb",          (object?)req.SitioWeb          ?? DBNull.Value),
                };

                var dt = await _db.ExecuteQueryAsync(
                    "EXEC IOT_sp_ActualizarConfigSitio @Id,@NombreComercial,@RazonSocial," +
                    "@NIT,@NRC,@Direccion,@Municipio,@Departamento,@Pais," +
                    "@Telefono,@Telefono2,@Email,@GiroActividad,@CodigoActividad," +
                    "@RepresentanteLegal,@LogoUrl,@ColorPrimario,@Slogan,@SitioWeb",
                    p);

                var exito = dt.Rows.Count > 0 && Convert.ToBoolean(dt.Rows[0]["Exitoso"]);

                if (exito)
                    await CargarDesdeDBAsync(); // refrescar cache

                return exito;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar config sitio");
                return false;
            }
        }
    }
}