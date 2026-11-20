using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Dominio.Actividades;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Dominio.Documentos;
using ServiciosEstudiantiles.Dominio.Inscripciones;

namespace ServiciosEstudiantiles.Aplicacion.Inscripciones;

public interface IInscripcionRepositorio : IRepositorio<Inscripcion> { }

public record InscribirseSolicitud(int ActividadId);
public record InscripcionDto(int Id, int ActividadId, string Actividad, DateTimeOffset Fecha,
    string EstadoTramite, IReadOnlyList<RequisitoDto> Requisitos);
public record InscritoDto(int InscripcionId, int AlumnoId, string Correo, string Nombre,
    DateTimeOffset Fecha, string EstadoTramite);

public class InscripcionServicio
{
    // Evita que dos alumnos tomen el último lugar al mismo tiempo.
    // Con PostgreSQL esto se resuelve con una transacción en la base de datos.
    private static readonly SemaphoreSlim Candado = new(1, 1);

    private readonly IInscripcionRepositorio _inscripciones;
    private readonly IActividadRepositorio _actividades;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly IDocumentoRepositorio _documentos;
    private readonly ITipoDocumentoRepositorio _tipos;
    private readonly TimeProvider _reloj;

    public InscripcionServicio(IInscripcionRepositorio inscripciones, IActividadRepositorio actividades,
        IUsuarioRepositorio usuarios, IDocumentoRepositorio documentos, ITipoDocumentoRepositorio tipos,
        TimeProvider reloj)
    {
        _inscripciones = inscripciones;
        _actividades = actividades;
        _usuarios = usuarios;
        _documentos = documentos;
        _tipos = tipos;
        _reloj = reloj;
    }

    public async Task<InscripcionDto> InscribirseAsync(UsuarioActual alumno, int actividadId)
    {
        await Candado.WaitAsync();
        try
        {
            var actividad = await _actividades.ObtenerAsync(actividadId);
            if (actividad is null || !actividad.Activa) throw new NoEncontradoException("La actividad no existe.");

            var inscripciones = await _inscripciones.BuscarAsync(i => i.ActividadId == actividadId && !i.Cancelada);
            if (inscripciones.Any(i => i.AlumnoId == alumno.Id))
                throw new ReglaNegocioException("Ya estás inscrito en esta actividad.");
            if (inscripciones.Count >= actividad.CupoMaximo)
                throw new ReglaNegocioException("La actividad ya no tiene cupo disponible.");

            var nueva = await _inscripciones.AgregarAsync(new Inscripcion
            {
                AlumnoId = alumno.Id,
                ActividadId = actividadId,
                Fecha = _reloj.GetUtcNow()
            });
            return await ConstruirDtoAsync(nueva, actividad);
        }
        finally
        {
            Candado.Release();
        }
    }

    public async Task<IReadOnlyList<InscripcionDto>> MisInscripcionesAsync(UsuarioActual alumno)
    {
        var mias = await _inscripciones.BuscarAsync(i => i.AlumnoId == alumno.Id && !i.Cancelada);
        var resultado = new List<InscripcionDto>();
        foreach (var inscripcion in mias.OrderByDescending(i => i.Fecha))
        {
            var actividad = await _actividades.ObtenerAsync(inscripcion.ActividadId);
            if (actividad is not null) resultado.Add(await ConstruirDtoAsync(inscripcion, actividad));
        }
        return resultado;
    }

    public async Task CancelarAsync(UsuarioActual alumno, int inscripcionId)
    {
        var inscripcion = await _inscripciones.ObtenerAsync(inscripcionId);
        if (inscripcion is null || inscripcion.AlumnoId != alumno.Id || inscripcion.Cancelada)
            throw new NoEncontradoException("Inscripción no encontrada.");
        inscripcion.Cancelada = true;
        await _inscripciones.ActualizarAsync(inscripcion);
    }

    public async Task<IReadOnlyList<InscritoDto>> InscritosDeActividadAsync(UsuarioActual quien, int actividadId)
    {
        var actividad = await _actividades.ObtenerAsync(actividadId);
        if (actividad is null || !actividad.Activa) throw new NoEncontradoException("La actividad no existe.");
        if (quien.Rol == Rol.Coach && actividad.CoachId != quien.Id)
            throw new AccesoDenegadoException("Esta actividad pertenece a otro coach.");
        if (quien.Rol != Rol.Coach && quien.Rol != Rol.Administrador)
            throw new AccesoDenegadoException("No tienes permiso para ver los inscritos.");

        var inscripciones = await _inscripciones.BuscarAsync(i => i.ActividadId == actividadId && !i.Cancelada);
        var resultado = new List<InscritoDto>();
        foreach (var inscripcion in inscripciones.OrderBy(i => i.Fecha))
        {
            var alumno = await _usuarios.ObtenerAsync(inscripcion.AlumnoId);
            if (alumno is null) continue;
            var dto = await ConstruirDtoAsync(inscripcion, actividad);
            resultado.Add(new InscritoDto(inscripcion.Id, alumno.Id, alumno.Correo, alumno.NombreCompleto,
                inscripcion.Fecha, dto.EstadoTramite));
        }
        return resultado;
    }

    private async Task<InscripcionDto> ConstruirDtoAsync(Inscripcion inscripcion, Actividad actividad)
    {
        var inscripcionId = inscripcion.Id;
        var idsRequeridos = actividad.TiposDocumentoRequeridos.ToList();

        var tipos = await _tipos.BuscarAsync(t => idsRequeridos.Contains(t.Id));
        var documentos = await _documentos.BuscarAsync(d =>
            d.Contexto == ContextoTramite.Actividad && d.ReferenciaId == inscripcionId);

        var (estado, requisitos) = RequisitosTramite.Evaluar(tipos.OrderBy(t => t.Id), documentos);
        return new InscripcionDto(inscripcion.Id, actividad.Id, actividad.Nombre, inscripcion.Fecha,
            estado.ToString(), requisitos);
    }
}
