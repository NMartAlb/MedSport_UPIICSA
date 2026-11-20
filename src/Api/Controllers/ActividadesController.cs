using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiciosEstudiantiles.Aplicacion.Actividades;
using ServiciosEstudiantiles.Aplicacion.Inscripciones;
using ServiciosEstudiantiles.Dominio.Actividades;

namespace ServiciosEstudiantiles.Api.Controllers;

[Route("api/actividades")]
[Authorize]
public class ActividadesController : ApiControladorBase
{
    private readonly ActividadServicio _servicio;
    private readonly InscripcionServicio _inscripciones;

    public ActividadesController(ActividadServicio servicio, InscripcionServicio inscripciones)
    {
        _servicio = servicio;
        _inscripciones = inscripciones;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ActividadDto>>> Listar([FromQuery] TipoActividad? tipo) =>
        Ok(await _servicio.ListarAsync(tipo));

    [HttpPost]
    [Authorize(Roles = "Coach,Administrador")]
    public async Task<ActionResult<ActividadDto>> Crear(GuardarActividadSolicitud solicitud) =>
        Ok(await _servicio.CrearAsync(Actual, solicitud));

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Coach,Administrador")]
    public async Task<ActionResult<ActividadDto>> Editar(int id, GuardarActividadSolicitud solicitud) =>
        Ok(await _servicio.EditarAsync(Actual, id, solicitud));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Coach,Administrador")]
    public async Task<IActionResult> Eliminar(int id)
    {
        await _servicio.EliminarAsync(Actual, id);
        return NoContent();
    }

    [HttpGet("{id:int}/inscritos")]
    [Authorize(Roles = "Coach,Administrador")]
    public async Task<ActionResult<IReadOnlyList<InscritoDto>>> Inscritos(int id) =>
        Ok(await _inscripciones.InscritosDeActividadAsync(Actual, id));
}
