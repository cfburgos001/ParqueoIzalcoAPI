namespace ParqueoIzalcoAPI.Models
{
    public class ConfigSitio
    {
        public int Id { get; set; }
        public string NombreComercial { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? NIT { get; set; }
        public string? NRC { get; set; }
        public string? Direccion { get; set; }
        public string? Municipio { get; set; }
        public string? Departamento { get; set; }
        public string Pais { get; set; } = "El Salvador";
        public string? Telefono { get; set; }
        public string? Telefono2 { get; set; }
        public string? Email { get; set; }
        public string? GiroActividad { get; set; }
        public string? CodigoActividad { get; set; }
        public string? RepresentanteLegal { get; set; }
        public string? LogoUrl { get; set; }
        public string ColorPrimario { get; set; } = "#3b82f6";
        public string? Slogan { get; set; }
        public string? SitioWeb { get; set; }
    }

    public class ActualizarSitioRequest
    {
        public int Id { get; set; }
        public string NombreComercial { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? NIT { get; set; }
        public string? NRC { get; set; }
        public string? Direccion { get; set; }
        public string? Municipio { get; set; }
        public string? Departamento { get; set; }
        public string Pais { get; set; } = "El Salvador";
        public string? Telefono { get; set; }
        public string? Telefono2 { get; set; }
        public string? Email { get; set; }
        public string? GiroActividad { get; set; }
        public string? CodigoActividad { get; set; }
        public string? RepresentanteLegal { get; set; }
        public string? LogoUrl { get; set; }
        public string ColorPrimario { get; set; } = "#3b82f6";
        public string? Slogan { get; set; }
        public string? SitioWeb { get; set; }
    }
}