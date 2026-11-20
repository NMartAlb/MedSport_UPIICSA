using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Notificaciones;

namespace ServiciosEstudiantiles.Api.Controllers;

[Route("api/notificaciones")]
[Authorize]
public class NotificacionesController : ApiControladorBase
{
    private readonly NotificacionServicio _servicio;

    public NotificacionesController(NotificacionServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<NotificacionDto>>> Listar() =>
        Ok(await _servicio.MisNotificacionesAsync(Actual));

    [HttpPost("{id:int}/leida")]
    public async Task<IActionResult> MarcarLeida(int id)
    {
        await _servicio.MarcarLeidaAsync(Actual, id);
        return NoContent();
    }
}
