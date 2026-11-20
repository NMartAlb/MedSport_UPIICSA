using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Documentos;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Dominio.Actividades;
using ServiciosEstudiantiles.Dominio.Comun;
using ServiciosEstudiantiles.Dominio.Documentos;

namespace ServiciosEstudiantiles.Aplicacion.Actividades;

public class ActividadServicio
{
    private readonly IActividadRepositorio _actividades;
    private readonly IInscripcionRepositorio _inscripciones;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly ITipoDocumentoRepositorio _tipos;

    public ActividadServicio(IActividadRepositorio actividades, IInscripcionRepositorio inscripciones,
        IUsuarioRepositorio usuarios, ITipoDocumentoRepositorio tipos)
    {
        _actividades = actividades;
        _inscripciones = inscripciones;
        _usuarios = usuarios;
        _tipos = tipos;
    }

    public async Task<IReadOnlyList<ActividadDto>> ListarAsync(TipoActividad? tipo)
    {
        var actividades = await _actividades.BuscarAsync(a => a.Activa);
        var inscripciones = await _inscripciones.BuscarAsync(i => !i.Cancelada);
        var coaches = await NombresDeCoachesAsync();

        return actividades
            .Where(a => tipo is null || a.Tipo == tipo)
            .OrderBy(a => a.Nombre)
            .Select(a => Mapear(a, inscripciones.Count(i => i.ActividadId == a.Id), coaches))
            .ToList();
    }

    public async Task<ActividadDto> CrearAsync(UsuarioActual quien, GuardarActividadSolicitud solicitud)
    {
        ValidarRol(quien);
        await ValidarSolicitudAsync(solicitud);
        var coachId = await ResolverCoachAsync(quien, solicitud.CoachId);

        var nueva = await _actividades.AgregarAsync(new Actividad
        {
            Nombre = solicitud.Nombre.Trim(),
            Tipo = solicitud.Tipo,
            Horario = solicitud.Horario.Trim(),
            CupoMaximo = solicitud.CupoMaximo,
            CoachId = coachId,
            TiposDocumentoRequeridos = (solicitud.TiposDocumentoRequeridos ?? new List<int>()).Distinct().ToList()
        });
        return await MapearAsync(nueva);
    }

    public async Task<ActividadDto> EditarAsync(UsuarioActual quien, int id, GuardarActividadSolicitud solicitud)
    {
        ValidarRol(quien);
        var actividad = await ObtenerPropiaAsync(quien, id);
        await ValidarSolicitudAsync(solicitud);

        var inscritos = (await _inscripciones.BuscarAsync(i => i.ActividadId == id && !i.Cancelada)).Count;
        if (solicitud.CupoMaximo < inscritos)
            throw new ReglaNegocioException($"El cupo no puede ser menor a los {inscritos} alumnos ya inscritos.");

        actividad.Nombre = solicitud.Nombre.Trim();
        actividad.Tipo = solicitud.Tipo;
        actividad.Horario = solicitud.Horario.Trim();
        actividad.CupoMaximo = solicitud.CupoMaximo;
        actividad.TiposDocumentoRequeridos = (solicitud.TiposDocumentoRequeridos ?? new List<int>()).Distinct().ToList();
        if (quien.Rol == Rol.Administrador && solicitud.CoachId.HasValue)
            actividad.CoachId = await ResolverCoachAsync(quien, solicitud.CoachId);

        await _actividades.ActualizarAsync(actividad);
        return await MapearAsync(actividad);
    }

    public async Task EliminarAsync(UsuarioActual quien, int id)
    {
        ValidarRol(quien);
        var actividad = await ObtenerPropiaAsync(quien, id);
        actividad.Activa = false; // baja lógica: se conserva el historial
        await _actividades.ActualizarAsync(actividad);
    }

    private static void ValidarRol(UsuarioActual quien)
    {
        if (quien.Rol != Rol.Coach && quien.Rol != Rol.Administrador)
            throw new AccesoDenegadoException("No tienes permiso para administrar actividades.");
    }

    /// El coach solo puede tocar sus actividades; el administrador, cualquiera.
    private async Task<Actividad> ObtenerPropiaAsync(UsuarioActual quien, int id)
    {
        var actividad = await _actividades.ObtenerAsync(id);
        if (actividad is null || !actividad.Activa) throw new NoEncontradoException("La actividad no existe.");
        if (quien.Rol == Rol.Coach && actividad.CoachId != quien.Id)
            throw new AccesoDenegadoException("Esta actividad pertenece a otro coach.");
        return actividad;
    }

    private async Task<int> ResolverCoachAsync(UsuarioActual quien, int? coachId)
    {
        if (quien.Rol == Rol.Coach) return quien.Id;
        if (coachId is null) throw new ReglaNegocioException("Indica el coach responsable.");
        var coach = await _usuarios.ObtenerAsync(coachId.Value);
        if (coach is null || coach.Rol != Rol.Coach) throw new ReglaNegocioException("El coach indicado no existe.");
        return coach.Id;
    }

    private async Task ValidarSolicitudAsync(GuardarActividadSolicitud s)
    {
        if (string.IsNullOrWhiteSpace(s.Nombre) || s.Nombre.Trim().Length is < 3 or > 80)
            throw new ReglaNegocioException("El nombre debe tener entre 3 y 80 caracteres.");
        if (string.IsNullOrWhiteSpace(s.Horario) || s.Horario.Length > 80)
            throw new ReglaNegocioException("El horario es obligatorio (máximo 80 caracteres).");
        if (s.CupoMaximo is < 1 or > 500)
            throw new ReglaNegocioException("El cupo debe estar entre 1 y 500.");
        if (!Enum.IsDefined(s.Tipo))
            throw new ReglaNegocioException("Tipo de actividad no válido.");

        var ids = (s.TiposDocumentoRequeridos ?? new List<int>()).Distinct().ToList();
        if (ids.Count > 0)
        {
            var validos = await _tipos.BuscarAsync(t => ids.Contains(t.Id) && t.Contexto == ContextoTramite.Actividad);
            if (validos.Count != ids.Count)
                throw new ReglaNegocioException("Alguno de los documentos requeridos no existe.");
        }
    }

    private async Task<ActividadDto> MapearAsync(Actividad a)
    {
        var inscritos = (await _inscripciones.BuscarAsync(i => i.ActividadId == a.Id && !i.Cancelada)).Count;
        return Mapear(a, inscritos, await NombresDeCoachesAsync());
    }

    private async Task<Dictionary<int, string>> NombresDeCoachesAsync()
    {
        var coaches = await _usuarios.BuscarAsync(u => u.Rol == Rol.Coach);
        return coaches.ToDictionary(c => c.Id, c => c.NombreCompleto);
    }

    private static ActividadDto Mapear(Actividad a, int inscritos, Dictionary<int, string> coaches) =>
        new(a.Id, a.Nombre, a.Tipo.ToString(), a.Horario, a.CoachId,
            coaches.TryGetValue(a.CoachId, out var nombre) ? nombre : "Sin asignar",
            a.CupoMaximo, Math.Max(0, a.CupoMaximo - inscritos), a.TiposDocumentoRequeridos);
}
