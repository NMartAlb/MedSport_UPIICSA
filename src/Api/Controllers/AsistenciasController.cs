using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Asistencias;

namespace ServiciosEstudiantiles.Api.Controllers;

/// Pase de lista digital del coach.
[Route("api/actividades/{actividadId:int}/asistencia")]
[Authorize(Roles = "Coach")]
public class AsistenciasController : ApiControladorBase
{
    private readonly AsistenciaServicio _servicio;

    public AsistenciasController(AsistenciaServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AsistenciaAlumnoDto>>> Lista(int actividadId, [FromQuery] DateOnly fecha) =>
        Ok(await _servicio.ListaAsync(Actual, actividadId, fecha));

    [HttpPost]
    public async Task<IActionResult> Registrar(int actividadId, RegistrarAsistenciaSolicitud solicitud)
    {
        await _servicio.RegistrarAsync(Actual, actividadId, solicitud);
        return NoContent();
    }
}
