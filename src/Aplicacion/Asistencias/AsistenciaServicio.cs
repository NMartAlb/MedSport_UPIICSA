using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Dominio.Asistencias;
using ServiciosEstudiantiles.Dominio.Comun;

namespace ServiciosEstudiantiles.Aplicacion.Asistencias;

public interface IAsistenciaRepositorio : IRepositorio<Asistencia> { }

public record AsistenciaItem(int AlumnoId, bool Presente);
public record RegistrarAsistenciaSolicitud(DateOnly Fecha, List<AsistenciaItem>? Asistencias);
public record AsistenciaAlumnoDto(int AlumnoId, string Nombre, string Correo, bool? Presente);

/// Pase de lista digital (RF12).
public class AsistenciaServicio
{
    private readonly IAsistenciaRepositorio _asistencias;
    private readonly IActividadRepositorio _actividades;
    private readonly IInscripcionRepositorio _inscripciones;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly TimeProvider _reloj;

    public AsistenciaServicio(IAsistenciaRepositorio asistencias, IActividadRepositorio actividades,
        IInscripcionRepositorio inscripciones, IUsuarioRepositorio usuarios, TimeProvider reloj)
    {
        _asistencias = asistencias;
        _actividades = actividades;
        _inscripciones = inscripciones;
        _usuarios = usuarios;
        _reloj = reloj;
    }

    public async Task<IReadOnlyList<AsistenciaAlumnoDto>> ListaAsync(UsuarioActual coach, int actividadId, DateOnly fecha)
    {
        await ValidarActividadDelCoachAsync(coach, actividadId);
        var inscripciones = await _inscripciones.BuscarAsync(i => i.ActividadId == actividadId && !i.Cancelada);
        var registradas = await _asistencias.BuscarAsync(a => a.ActividadId == actividadId && a.Fecha == fecha);

        var lista = new List<AsistenciaAlumnoDto>();
        foreach (var inscripcion in inscripciones.OrderBy(i => i.Fecha))
        {
            var alumno = await _usuarios.ObtenerAsync(inscripcion.AlumnoId);
            if (alumno is null) continue;
            var registro = registradas.FirstOrDefault(a => a.AlumnoId == alumno.Id);
            lista.Add(new AsistenciaAlumnoDto(alumno.Id, alumno.NombreCompleto, alumno.Correo, registro?.Presente));
        }
        return lista;
    }

    public async Task RegistrarAsync(UsuarioActual coach, int actividadId, RegistrarAsistenciaSolicitud solicitud)
    {
        await ValidarActividadDelCoachAsync(coach, actividadId);

        var hoy = DateOnly.FromDateTime(_reloj.GetUtcNow().UtcDateTime);
        if (solicitud.Fecha > hoy) throw new ReglaNegocioException("No se puede pasar lista en una fecha futura.");
        if (solicitud.Asistencias is null || solicitud.Asistencias.Count == 0)
            throw new ReglaNegocioException("No hay alumnos en la lista.");

        var inscripciones = await _inscripciones.BuscarAsync(i => i.ActividadId == actividadId && !i.Cancelada);
        var idsInscritos = inscripciones.Select(i => i.AlumnoId).ToHashSet();
        var fecha = solicitud.Fecha;
        var existentes = await _asistencias.BuscarAsync(a => a.ActividadId == actividadId && a.Fecha == fecha);

        foreach (var item in solicitud.Asistencias.GroupBy(a => a.AlumnoId).Select(g => g.Last()))
        {
            if (!idsInscritos.Contains(item.AlumnoId))
                throw new ReglaNegocioException("Hay alumnos que no están inscritos en esta actividad.");

            var existente = existentes.FirstOrDefault(a => a.AlumnoId == item.AlumnoId);
            if (existente is null)
            {
                await _asistencias.AgregarAsync(new Asistencia
                {
                    ActividadId = actividadId,
                    AlumnoId = item.AlumnoId,
                    Fecha = fecha,
                    Presente = item.Presente,
                    RegistradoPorId = coach.Id
                });
            }
            else
            {
                existente.Presente = item.Presente;
                existente.RegistradoPorId = coach.Id;
                await _asistencias.ActualizarAsync(existente);
            }
        }
    }

    private async Task ValidarActividadDelCoachAsync(UsuarioActual coach, int actividadId)
    {
        if (coach.Rol != Rol.Coach) throw new AccesoDenegadoException("Solo el coach puede pasar lista.");
        var actividad = await _actividades.ObtenerAsync(actividadId);
        if (actividad is null || !actividad.Activa) throw new NoEncontradoException("La actividad no existe.");
        if (actividad.CoachId != coach.Id) throw new AccesoDenegadoException("Esta actividad pertenece a otro coach.");
    }
}
