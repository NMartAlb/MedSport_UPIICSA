using ServiciosEstudiantiles.Aplicacion.Auditoria;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Usuarios;
using ServiciosEstudiantiles.Dominio.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.ServicioMedico;

public record RegistrarPrimerosAuxiliosSolicitud(string Lugar, string Motivo, string AtencionBrindada, int? AlumnoId);
public record PrimerosAuxiliosDto(int Id, DateTimeOffset Fecha, string Lugar, string Motivo, string AtencionBrindada, int? AlumnoId);
public record RegistrarUrgenciaSolicitud(NivelTriage Triage, string DiagnosticoPreliminar, string? Derivacion, int? AlumnoId);
public record UrgenciaDto(int Id, DateTimeOffset Fecha, string Triage, string DiagnosticoPreliminar, string? Derivacion, int? AlumnoId);

/// Bitácora de primeros auxilios (RF17) y registro de urgencias con triage (RF18).
public class AtencionesServicio
{
    private readonly IPrimerosAuxiliosRepositorio _primerosAuxilios;
    private readonly IUrgenciaRepositorio _urgencias;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly AuditoriaServicio _auditoria;
    private readonly TimeProvider _reloj;

    public AtencionesServicio(IPrimerosAuxiliosRepositorio primerosAuxilios, IUrgenciaRepositorio urgencias,
        IUsuarioRepositorio usuarios, AuditoriaServicio auditoria, TimeProvider reloj)
    {
        _primerosAuxilios = primerosAuxilios;
        _urgencias = urgencias;
        _usuarios = usuarios;
        _auditoria = auditoria;
        _reloj = reloj;
    }

    public async Task<PrimerosAuxiliosDto> RegistrarPrimerosAuxiliosAsync(UsuarioActual medico, RegistrarPrimerosAuxiliosSolicitud s)
    {
        Exigir(s.Lugar, "El lugar", 100);
        Exigir(s.Motivo, "El motivo", 200);
        Exigir(s.AtencionBrindada, "La atención brindada", 500);
        if (s.AlumnoId.HasValue) await _usuarios.ObtenerAlumnoAsync(s.AlumnoId.Value);

        var atencion = await _primerosAuxilios.AgregarAsync(new AtencionPrimerosAuxilios
        {
            Fecha = _reloj.GetUtcNow(),
            Lugar = s.Lugar.Trim(),
            Motivo = s.Motivo.Trim(),
            AtencionBrindada = s.AtencionBrindada.Trim(),
            AlumnoId = s.AlumnoId,
            MedicoId = medico.Id
        });
        if (s.AlumnoId.HasValue) await _auditoria.RegistrarAsync(medico.Id, "Registra primeros auxilios", $"Alumno {s.AlumnoId}");
        return Mapear(atencion);
    }

    public async Task<IReadOnlyList<PrimerosAuxiliosDto>> ListarPrimerosAuxiliosAsync() =>
        (await _primerosAuxilios.ListarAsync()).OrderByDescending(a => a.Fecha).Take(100).Select(Mapear).ToList();

    public async Task<UrgenciaDto> RegistrarUrgenciaAsync(UsuarioActual medico, RegistrarUrgenciaSolicitud s)
    {
        if (!Enum.IsDefined(s.Triage)) throw new ReglaNegocioException("Nivel de triage no válido.");
        Exigir(s.DiagnosticoPreliminar, "El diagnóstico preliminar", 500);
        if ((s.Derivacion?.Length ?? 0) > 200) throw new ReglaNegocioException("La derivación no puede exceder 200 caracteres.");
        if (s.AlumnoId.HasValue) await _usuarios.ObtenerAlumnoAsync(s.AlumnoId.Value);

        var urgencia = await _urgencias.AgregarAsync(new Urgencia
        {
            Fecha = _reloj.GetUtcNow(),
            AlumnoId = s.AlumnoId,
            MedicoId = medico.Id,
            Triage = s.Triage,
            DiagnosticoPreliminar = s.DiagnosticoPreliminar.Trim(),
            Derivacion = string.IsNullOrWhiteSpace(s.Derivacion) ? null : s.Derivacion.Trim()
        });
        if (s.AlumnoId.HasValue) await _auditoria.RegistrarAsync(medico.Id, "Registra urgencia", $"Alumno {s.AlumnoId}");
        return Mapear(urgencia);
    }

    public async Task<IReadOnlyList<UrgenciaDto>> ListarUrgenciasAsync() =>
        (await _urgencias.ListarAsync()).OrderByDescending(u => u.Fecha).Take(100).Select(Mapear).ToList();

    private static void Exigir(string? texto, string campo, int maximo)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length > maximo)
            throw new ReglaNegocioException($"{campo} es obligatorio (máximo {maximo} caracteres).");
    }

    private static PrimerosAuxiliosDto Mapear(AtencionPrimerosAuxilios a) =>
        new(a.Id, a.Fecha, a.Lugar, a.Motivo, a.AtencionBrindada, a.AlumnoId);

    private static UrgenciaDto Mapear(Urgencia u) =>
        new(u.Id, u.Fecha, u.Triage.ToString(), u.DiagnosticoPreliminar, u.Derivacion, u.AlumnoId);
}
