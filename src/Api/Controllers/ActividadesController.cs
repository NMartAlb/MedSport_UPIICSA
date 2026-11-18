using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Dominio.Actividades;

namespace ServiciosEstudiantiles.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ActividadesController : ControllerBase
{
    private readonly ActividadServicio _servicio;

    public ActividadesController(ActividadServicio servicio) => _servicio = servicio;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ActividadDto>>> Get([FromQuery] TipoActividad? tipo)
        => Ok(await _servicio.ListarAsync(tipo));
}