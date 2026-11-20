using ServiciosEstudiantiles.Aplicacion.Auditoria;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Notificaciones;
using ServiciosEstudiantiles.Aplicacion.Usuarios;
using ServiciosEstudiantiles.Dominio.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.ServicioMedico;

public record CargarValoracionSolicitud(int AlumnoId, string Resultado, string? Observaciones);
public record ValoracionDto(int Id, int AlumnoId, DateTimeOffset Fecha, string Resultado, string? Observaciones, string Medico);

/// Valoración médica: la carga el personal médico y la consulta el alumno (RF14, RF15).
public class ValoracionServicio
{
    private readonly IValoracionRepositorio _valoraciones;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly NotificacionServicio _notificaciones;
    private readonly AuditoriaServicio _auditoria;
    private readonly TimeProvider _reloj;

    public ValoracionServicio(IValoracionRepositorio valoraciones, IUsuarioRepositorio usuarios,
        NotificacionServicio notificaciones, AuditoriaServicio auditoria, TimeProvider reloj)
    {
        _valoraciones = valoraciones;
        _usuarios = usuarios;
        _notificaciones = notificaciones;
        _auditoria = auditoria;
        _reloj = reloj;
    }

    public async Task<ValoracionDto> CargarAsync(UsuarioActual medico, CargarValoracionSolicitud solicitud)
    {
        var alumno = await _usuarios.ObtenerAlumnoAsync(solicitud.AlumnoId);
        if (string.IsNullOrWhiteSpace(solicitud.Resultado) || solicitud.Resultado.Length > 500)
            throw new ReglaNegocioException("El resultado es obligatorio (máximo 500 caracteres).");
        if ((solicitud.Observaciones?.Length ?? 0) > 1000)
            throw new ReglaNegocioException("Las observaciones no pueden exceder 1000 caracteres.");

        var valoracion = await _valoraciones.AgregarAsync(new Valoracion
        {
            AlumnoId = alumno.Id,
            MedicoId = medico.Id,
            Fecha = _reloj.GetUtcNow(),
            Resultado = solicitud.Resultado.Trim(),
            Observaciones = solicitud.Observaciones?.Trim()
        });

        await _auditoria.RegistrarAsync(medico.Id, "Carga valoración médica", $"Alumno {alumno.Id}");
        await _notificaciones.NotificarAsync(alumno.Id, "Ya está disponible tu valoración médica.");
        return await MapearAsync(valoracion);
    }

    public async Task<IReadOnlyList<ValoracionDto>> MisValoracionesAsync(UsuarioActual alumno) =>
        await ListarDeAlumnoAsync(alumno.Id);

    public async Task<IReadOnlyList<ValoracionDto>> PorAlumnoAsync(UsuarioActual medico, int alumnoId)
    {
        await _usuarios.ObtenerAlumnoAsync(alumnoId);
        await _auditoria.RegistrarAsync(medico.Id, "Consulta valoraciones", $"Alumno {alumnoId}");
        return await ListarDeAlumnoAsync(alumnoId);
    }

    private async Task<IReadOnlyList<ValoracionDto>> ListarDeAlumnoAsync(int alumnoId)
    {
        var lista = await _valoraciones.BuscarAsync(v => v.AlumnoId == alumnoId);
        var resultado = new List<ValoracionDto>();
        foreach (var v in lista.OrderByDescending(v => v.Fecha)) resultado.Add(await MapearAsync(v));
        return resultado;
    }

    private async Task<ValoracionDto> MapearAsync(Valoracion v)
    {
        var medico = await _usuarios.ObtenerAsync(v.MedicoId);
        return new ValoracionDto(v.Id, v.AlumnoId, v.Fecha, v.Resultado, v.Observaciones,
            medico?.NombreCompleto ?? "Personal médico");
    }
}
