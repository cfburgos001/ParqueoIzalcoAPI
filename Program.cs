using DataparkBarreraAPI.Services;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== CONFIGURACIÓN DE SERVICIOS =====

// Agregar controladores
builder.Services.AddControllers();

// Swagger para documentación
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Datapark Barrera API",
        Version = "v1",
        Description = "API para control de barrera de parqueo - Sistema CEPA",
        Contact = new OpenApiContact
        {
            Name = "Sistema CEPA",
            Email = "soporte@cepa.com"
        }
    });
});

// Configurar CORS (permitir acceso desde cualquier origen)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Registrar servicios personalizados
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<IBarreraService, BarreraService>();

// ===== CONSTRUCCIÓN DE LA APLICACIÓN =====
var app = builder.Build();

// Configurar el pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Datapark Barrera API v1");
    c.RoutePrefix = string.Empty; // Swagger en la raíz (http://localhost:5000)
});

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Mensaje de inicio
Console.WriteLine("════════════════════════════════════════════════");
Console.WriteLine("🚀 DATAPARK BARRERA API - INICIANDO");
Console.WriteLine("════════════════════════════════════════════════");
Console.WriteLine($"🌐 Servidor: http://localhost:5000");
Console.WriteLine($"📚 Swagger: http://localhost:5000");
Console.WriteLine($"🗄️  Base de Datos: Datapark");
Console.WriteLine("════════════════════════════════════════════════");

app.Run();