using ServiciosEstudiantiles.Dominio.Actividades;

namespace ServiciosEstudiantiles.Aplicacion.Actividades;

public interface IActividadRepositorio
{
    Task<IReadOnlyList<Actividad>> ListarAsync(TipoActividad? tipo);
    Task<Actividad?> ObtenerAsync(int id);
}