using ParqueoIzalcoAPI.Services;
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
        Title = "IOT API",
        Version = "v1",
        Description = "API para control de barrera de parqueo - Sistema IOT",
        Contact = new OpenApiContact
        {
            Name = "Sistema IOT",
            Email = "cfburgos001@gmail.com"
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
builder.Services.AddSingleton<IPagoService, PagoService>();
builder.Services.AddSingleton<IVisitasService, VisitasService>();  
builder.Services.AddSingleton<ITicketsService, TicketsService>();
builder.Services.AddSingleton<ITarifasService, TarifasService>();
// ===== CONSTRUCCIÓN DE LA APLICACIÓN =====
var app = builder.Build();

// Configurar el pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Datapark Barrera API v1");
    c.RoutePrefix = "swagger"; // ← CAMBIO: Swagger ahora en /swagger para liberar la raíz
});

// ===== NUEVO: Servir archivos estáticos desde wwwroot =====
app.UseStaticFiles();

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// ===== NUEVO: Redirigir la raíz al portal de visitas =====
app.MapGet("/", () => Results.Redirect("/visitas/login.html"));

// Mensaje de inicio - Obtener URL y puerto de la configuración
var urls = builder.Configuration["urls"] ?? "http://localhost:5225";
Console.WriteLine("════════════════════════════════════════════════");
Console.WriteLine("🚀 DATAPARK BARRERA API - INICIANDO");
Console.WriteLine("════════════════════════════════════════════════");
Console.WriteLine($"🌐 Servidor: {urls}");
Console.WriteLine($"📚 Swagger: {urls}/swagger");
Console.WriteLine($"🌐 Portal Visitas: {urls}/visitas/index.html");
Console.WriteLine($"🗄️  Base de Datos: Datapark");
Console.WriteLine("════════════════════════════════════════════════");

app.Run();