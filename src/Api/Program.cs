using System.Text.Json.Serialization;
using ServiciosEstudiantiles.Api.Configuracion;
using ServiciosEstudiantiles.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Conexión entre capas (ver Configuracion/RegistroDeServicios.cs)
var jwt = ConfiguracionSeguridad.CrearOpcionesJwt(builder.Configuration, builder.Environment);
builder.Services.AgregarAplicacion();
builder.Services.AgregarInfraestructura(builder.Configuration, builder.Environment, jwt);
builder.Services.AgregarSeguridad(builder.Configuration, jwt);

var app = builder.Build();

app.UseMiddleware<ManejadorErroresMiddleware>();

// Encabezados de seguridad para todas las respuestas
app.Use(async (contexto, siguiente) =>
{
    contexto.Response.Headers["X-Content-Type-Options"] = "nosniff";
    contexto.Response.Headers["X-Frame-Options"] = "DENY";
    contexto.Response.Headers["Referrer-Policy"] = "no-referrer";
    contexto.Response.Headers["Cache-Control"] = "no-store";
    await siguiente();
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // documentación OpenAPI en /openapi/v1.json
}

app.UseHttpsRedirection();
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
