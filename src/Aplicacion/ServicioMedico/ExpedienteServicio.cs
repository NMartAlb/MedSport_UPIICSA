using ServiciosEstudiantiles.Aplicacion.Auditoria;
using ServiciosEstudiantiles.Aplicacion.Auth;
using ServiciosEstudiantiles.Aplicacion.Comun;
using ServiciosEstudiantiles.Aplicacion.Usuarios;
using ServiciosEstudiantiles.Dominio.ServicioMedico;

namespace ServiciosEstudiantiles.Aplicacion.ServicioMedico;

public record EntregaInsumoDto(string Descripcion, int Cantidad);
public record ConsultaDto(int Id, DateTimeOffset Fecha, string Motivo, string? Diagnostico,
    bool EsLesionDeportiva, bool GeneraAusencia, IReadOnlyList<EntregaInsumoDto> Entregas);
public record ExpedienteDto(int AlumnoId, string AlumnoNombre, string? TipoSangre, string? AntecedentesAlergicos,
    string? AntecedentesCronicos, string? AntecedentesPatologicos, IReadOnlyList<ConsultaDto> Consultas);
public record GuardarExpedienteSolicitud(string? TipoSangre, string? AntecedentesAlergicos,
    string? AntecedentesCronicos, string? AntecedentesPatologicos);
public record RegistrarConsultaSolicitud(string Motivo, string? Diagnostico, bool EsLesionDeportiva,
    bool GeneraAusencia, List<EntregaInsumoDto>? Entregas);

/// Expediente clínico electrónico (RF16) y consultas con entrega de insumos (RF19). Todo acceso queda en bitácora.
public class ExpedienteServicio
{
    private static readonly string[] TiposSangre = { "A+", "A-", "B+", "B-", "AB+", "AB-", "O+", "O-" };

    private readonly IExpedienteRepositorio _expedientes;
    private readonly IConsultaRepositorio _consultas;
    private readonly IUsuarioRepositorio _usuarios;
    private readonly AuditoriaServicio _auditoria;
    private readonly TimeProvider _reloj;

    public ExpedienteServicio(IExpedienteRepositorio expedientes, IConsultaRepositorio consultas,
        IUsuarioRepositorio usuarios, AuditoriaServicio auditoria, TimeProvider reloj)
    {
        _expedientes = expedientes;
        _consultas = consultas;
        _usuarios = usuarios;
        _auditoria = auditoria;
        _reloj = reloj;
    }

    public async Task<ExpedienteDto> ObtenerAsync(UsuarioActual medico, int alumnoId)
    {
        var alumno = await _usuarios.ObtenerAlumnoAsync(alumnoId);
        await _auditoria.RegistrarAsync(medico.Id, "Consulta expediente", $"Alumno {alumnoId}");
        return await ConstruirAsync(alumno.Id, alumno.NombreCompleto);
    }

    public async Task<ExpedienteDto> GuardarAsync(UsuarioActual medico, int alumnoId, GuardarExpedienteSolicitud s)
    {
        var alumno = await _usuarios.ObtenerAlumnoAsync(alumnoId);

        var sangre = string.IsNullOrWhiteSpace(s.TipoSangre) ? null : s.TipoSangre.Trim().ToUpperInvariant();
        if (sangre is not null && !TiposSangre.Contains(sangre))
            throw new ReglaNegocioException("Tipo de sangre no válido (A+, A-, B+, B-, AB+, AB-, O+, O-).");
        foreach (var texto in new[] { s.AntecedentesAlergicos, s.AntecedentesCronicos, s.AntecedentesPatologicos })
            if ((texto?.Length ?? 0) > 1000) throw new ReglaNegocioException("Cada antecedente admite máximo 1000 caracteres.");

        var expediente = (await _expedientes.BuscarAsync(e => e.AlumnoId == alumnoId)).FirstOrDefault();
        var esNuevo = expediente is null;
        expediente ??= new ExpedienteClinico { AlumnoId = alumnoId };

        expediente.TipoSangre = sangre;
        expediente.AntecedentesAlergicos = s.AntecedentesAlergicos?.Trim();
        expediente.AntecedentesCronicos = s.AntecedentesCronicos?.Trim();
        expediente.AntecedentesPatologicos = s.AntecedentesPatologicos?.Trim();

        if (esNuevo) await _expedientes.AgregarAsync(expediente);
        else await _expedientes.ActualizarAsync(expediente);

        await _auditoria.RegistrarAsync(medico.Id, "Modifica expediente", $"Alumno {alumnoId}");
        return await ConstruirAsync(alumno.Id, alumno.NombreCompleto);
    }

    public async Task<ConsultaDto> RegistrarConsultaAsync(UsuarioActual medico, int alumnoId, RegistrarConsultaSolicitud s)
    {
        await _usuarios.ObtenerAlumnoAsync(alumnoId);
        if (string.IsNullOrWhiteSpace(s.Motivo) || s.Motivo.Trim().Length > 200)
            throw new ReglaNegocioException("El motivo de la consulta es obligatorio (máximo 200 caracteres).");
        if ((s.Diagnostico?.Length ?? 0) > 1000)
            throw new ReglaNegocioException("El diagnóstico no puede exceder 1000 caracteres.");

        var entregas = (s.Entregas ?? new List<EntregaInsumoDto>()).ToList();
        if (entregas.Count > 20) throw new ReglaNegocioException("Máximo 20 entregas por consulta.");
        if (entregas.Any(e => string.IsNullOrWhiteSpace(e.Descripcion) || e.Descripcion.Length > 100 || e.Cantidad is < 1 or > 1000))
            throw new ReglaNegocioException("Cada entrega necesita descripción y una cantidad entre 1 y 1000.");

        var consulta = await _consultas.AgregarAsync(new Consulta
        {
            AlumnoId = alumnoId,
            MedicoId = medico.Id,
            Fecha = _reloj.GetUtcNow(),
            Motivo = s.Motivo.Trim(),
            Diagnostico = s.Diagnostico?.Trim(),
            EsLesionDeportiva = s.EsLesionDeportiva,
            GeneraAusencia = s.GeneraAusencia,
            Entregas = entregas.Select(e => new EntregaInsumo { Descripcion = e.Descripcion.Trim(), Cantidad = e.Cantidad }).ToList()
        });

        await _auditoria.RegistrarAsync(medico.Id, "Registra consulta", $"Alumno {alumnoId}");
        return Mapear(consulta);
    }

    private async Task<ExpedienteDto> ConstruirAsync(int alumnoId, string nombre)
    {
        var expediente = (await _expedientes.BuscarAsync(e => e.AlumnoId == alumnoId)).FirstOrDefault();
        var consultas = await _consultas.BuscarAsync(c => c.AlumnoId == alumnoId);
        return new ExpedienteDto(alumnoId, nombre, expediente?.TipoSangre, expediente?.AntecedentesAlergicos,
            expediente?.AntecedentesCronicos, expediente?.AntecedentesPatologicos,
            consultas.OrderByDescending(c => c.Fecha).Select(Mapear).ToList());
    }

    private static ConsultaDto Mapear(Consulta c) =>
        new(c.Id, c.Fecha, c.Motivo, c.Diagnostico, c.EsLesionDeportiva, c.GeneraAusencia,
            c.Entregas.Select(e => new EntregaInsumoDto(e.Descripcion, e.Cantidad)).ToList());
}
