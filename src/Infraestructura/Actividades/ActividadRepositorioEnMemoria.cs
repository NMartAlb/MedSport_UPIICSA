using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Dominio.Actividades;

namespace ServiciosEstudiantiles.Infraestructura.Actividades;

public class ActividadRepositorioEnMemoria : IActividadRepositorio
{
    private readonly List<Actividad> _datos = new()
    {
        new() { Id = 1, Nombre = "Básquetbol", Tipo = TipoActividad.Deportiva,
                Horario = "Lun y Mié 14:00", Coach = "Coach de prueba", CupoMaximo = 30, Inscritos = 12 },
        new() { Id = 2, Nombre = "Taekwondo", Tipo = TipoActividad.Deportiva,
                Horario = "Mar y Jue 16:00", Coach = "Coach de prueba", CupoMaximo = 25, Inscritos = 25 },
        new() { Id = 3, Nombre = "Teatro", Tipo = TipoActividad.Cultural,
                Horario = "Vie 15:00", Coach = "Profesor de prueba", CupoMaximo = 20, Inscritos = 5 },
    };

    public Task<IReadOnlyList<Actividad>> ListarAsync(TipoActividad? tipo)
    {
        IEnumerable<Actividad> consulta = _datos;
        if (tipo.HasValue) consulta = consulta.Where(a => a.Tipo == tipo.Value);
        return Task.FromResult<IReadOnlyList<Actividad>>(consulta.ToList());
    }

    public Task<Actividad?> ObtenerAsync(int id) =>
        Task.FromResult(_datos.FirstOrDefault(a => a.Id == id));
}