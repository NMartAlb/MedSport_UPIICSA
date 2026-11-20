using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;

namespace ServiciosEstudiantiles.Api.Controllers;

[Route("api/inscripciones")]
[Authorize(Roles = "Alumno")]
public class InscripcionesController : ApiControladorBase
{
    private readonly InscripcionServicio _servicio;

    public InscripcionesController(InscripcionServicio servicio) => _servicio = servicio;

    [HttpPost]
    public async Task<ActionResult<InscripcionDto>> Inscribirse(InscribirseSolicitud solicitud) =>
        Ok(await _servicio.InscribirseAsync(Actual, solicitud.ActividadId));

    [HttpGet("mias")]
    public async Task<ActionResult<IReadOnlyList<InscripcionDto>>> Mias() =>
        Ok(await _servicio.MisInscripcionesAsync(Actual));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancelar(int id)
    {
        await _servicio.CancelarAsync(Actual, id);
        return NoContent();
    }
}
