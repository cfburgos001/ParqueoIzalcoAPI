using ParqueoIzalcoAPI.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ===== SERVICIOS =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IOT API",
        Version = "v1",
        Description = "API para control de parqueo — Sistema IOT",
        Contact = new OpenApiContact { Name = "Sistema IOT", Email = "cfburgos001@gmail.com" }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Registrar todos los servicios
builder.Services.AddSingleton<IDatabaseService, DatabaseService>();
builder.Services.AddSingleton<ISitioService, SitioService>();
builder.Services.AddSingleton<IBarreraService, BarreraService>();
builder.Services.AddSingleton<IPagoService, PagoService>();
builder.Services.AddSingleton<IVisitasService, VisitasService>();
builder.Services.AddSingleton<ITicketsService, TicketsService>();
builder.Services.AddSingleton<ITarifasService, TarifasService>();
builder.Services.AddSingleton<IVehiculosService, VehiculosService>();
builder.Services.AddSingleton<ITarjetasService, TarjetasService>();
builder.Services.AddSingleton<IEspaciosService, EspaciosService>();
builder.Services.AddSingleton<IMonitoreoService, MonitoreoService>();
builder.Services.AddScoped<IAsistenciaService, AsistenciaService>();

// ===== BUILD =====
var app = builder.Build();

// Cargar config del sitio al iniciar
using (var scope = app.Services.CreateScope())
{
    var sitioService = scope.ServiceProvider.GetRequiredService<ISitioService>();
    await sitioService.CargarDesdeDBAsync();
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Datapark IOT API v1");
    c.RoutePrefix = "swagger";
});

app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/visitas/login.html"));

var urls = builder.Configuration["urls"] ?? "http://localhost:5225";
Console.WriteLine("════════════════════════════════════════════════");
Console.WriteLine("🚀 DATAPARK IOT API — INICIANDO");
Console.WriteLine("════════════════════════════════════════════════");
Console.WriteLine($"🌐 Servidor  : {urls}");
Console.WriteLine($"📚 Swagger   : {urls}/swagger");
Console.WriteLine($"🌐 Portal    : {urls}/visitas/index.html");
Console.WriteLine($"💳 Tarjetas  : {urls}/visitas/tarjetas.html");
Console.WriteLine("════════════════════════════════════════════════");

app.Run();