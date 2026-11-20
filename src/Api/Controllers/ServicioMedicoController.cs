using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.ServicioMedico;

namespace ServiciosEstudiantiles.Api.Controllers;

/// Trámite de nuevo ingreso al servicio médico (alumno).
[Route("api/servicio-medico/nuevo-ingreso")]
[Authorize(Roles = "Alumno")]
public class NuevoIngresoController : ApiControladorBase
{
    private readonly NuevoIngresoServicio _servicio;

    public NuevoIngresoController(NuevoIngresoServicio servicio) => _servicio = servicio;

    [HttpPost]
    public async Task<ActionResult<TramiteNuevoIngresoDto>> Iniciar() => Ok(await _servicio.IniciarAsync(Actual));

    [HttpGet]
    public async Task<IActionResult> MiTramite()
    {
        var tramite = await _servicio.MiTramiteAsync(Actual);
        return tramite is null ? NoContent() : Ok(tramite);
    }
}

/// Valoración médica: la carga el personal médico y la consulta el alumno.
[Route("api/servicio-medico/valoraciones")]
[Authorize]
public class ValoracionesController : ApiControladorBase
{
    private readonly ValoracionServicio _servicio;

    public ValoracionesController(ValoracionServicio servicio) => _servicio = servicio;

    [HttpGet("mias")]
    [Authorize(Roles = "Alumno")]
    public async Task<ActionResult<IReadOnlyList<ValoracionDto>>> Mias() =>
        Ok(await _servicio.MisValoracionesAsync(Actual));

    [HttpPost]
    [Authorize(Roles = "PersonalMedico")]
    public async Task<ActionResult<ValoracionDto>> Cargar(CargarValoracionSolicitud solicitud) =>
        Ok(await _servicio.CargarAsync(Actual, solicitud));

    [HttpGet("alumno/{alumnoId:int}")]
    [Authorize(Roles = "PersonalMedico")]
    public async Task<ActionResult<IReadOnlyList<ValoracionDto>>> PorAlumno(int alumnoId) =>
        Ok(await _servicio.PorAlumnoAsync(Actual, alumnoId));
}

/// Expediente clínico electrónico (solo personal médico).
[Route("api/servicio-medico/expedientes")]
[Authorize(Roles = "PersonalMedico")]
public class ExpedientesController : ApiControladorBase
{
    private readonly ExpedienteServicio _servicio;

    public ExpedientesController(ExpedienteServicio servicio) => _servicio = servicio;

    [HttpGet("{alumnoId:int}")]
    public async Task<ActionResult<ExpedienteDto>> Obtener(int alumnoId) =>
        Ok(await _servicio.ObtenerAsync(Actual, alumnoId));

    [HttpPut("{alumnoId:int}")]
    public async Task<ActionResult<ExpedienteDto>> Guardar(int alumnoId, GuardarExpedienteSolicitud solicitud) =>
        Ok(await _servicio.GuardarAsync(Actual, alumnoId, solicitud));

    [HttpPost("{alumnoId:int}/consultas")]
    public async Task<ActionResult<ConsultaDto>> RegistrarConsulta(int alumnoId, RegistrarConsultaSolicitud solicitud) =>
        Ok(await _servicio.RegistrarConsultaAsync(Actual, alumnoId, solicitud));
}

/// Primeros auxilios y urgencias.
[Route("api/servicio-medico")]
[Authorize(Roles = "PersonalMedico")]
public class AtencionesController : ApiControladorBase
{
    private readonly AtencionesServicio _servicio;

    public AtencionesController(AtencionesServicio servicio) => _servicio = servicio;

    [HttpGet("primeros-auxilios")]
    public async Task<ActionResult<IReadOnlyList<PrimerosAuxiliosDto>>> ListarPrimerosAuxilios() =>
        Ok(await _servicio.ListarPrimerosAuxiliosAsync());

    [HttpPost("primeros-auxilios")]
    public async Task<ActionResult<PrimerosAuxiliosDto>> RegistrarPrimerosAuxilios(RegistrarPrimerosAuxiliosSolicitud solicitud) =>
        Ok(await _servicio.RegistrarPrimerosAuxiliosAsync(Actual, solicitud));

    [HttpGet("urgencias")]
    public async Task<ActionResult<IReadOnlyList<UrgenciaDto>>> ListarUrgencias() =>
        Ok(await _servicio.ListarUrgenciasAsync());

    [HttpPost("urgencias")]
    public async Task<ActionResult<UrgenciaDto>> RegistrarUrgencia(RegistrarUrgenciaSolicitud solicitud) =>
        Ok(await _servicio.RegistrarUrgenciaAsync(Actual, solicitud));
}

/// Campañas de salud: las publica el personal médico y las ve cualquier usuario con sesión.
[Route("api/campanas")]
[Authorize]
public class CampanasController : ApiControladorBase
{
    private readonly CampanaServicio _servicio;

    public CampanasController(CampanaServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CampanaDto>>> Vigentes() => Ok(await _servicio.ListarVigentesAsync());

    [HttpPost]
    [Authorize(Roles = "PersonalMedico")]
    public async Task<ActionResult<CampanaDto>> Crear(CrearCampanaSolicitud solicitud) =>
        Ok(await _servicio.CrearAsync(Actual, solicitud));
}
