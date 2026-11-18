using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Infraestructura.Actividades;

var builder = WebApplication.CreateBuilder(args);

// Servicios de la aplicación
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Conexión entre capas: cuando se pida el repositorio, se usa el de memoria.
// Cuando la BD esté lista, solo se cambia esta línea.
builder.Services.AddSingleton<IActividadRepositorio, ActividadRepositorioEnMemoria>();
builder.Services.AddScoped<ActividadServicio>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

app.Run();
